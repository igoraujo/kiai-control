using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using KiaiControl.Api.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using Dapper;
using KiaiControl.Api.Controllers;
using KiaiControl.Contracts.Auth;
using KiaiControl.Core.Common;
using KiaiControl.Core.Interfaces;
using KiaiControl.Repositories.DependencyInjection;
using KiaiControl.Services.Auth;
using KiaiControl.UseCases.Attendance;
using KiaiControl.UseCases.Lessons;
using KiaiControl.UseCases.Mobile;
using KiaiControl.UseCases.Teachers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace KiaiControl.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class MobileRepositoryTests(PostgreSqlFixture fixture)
{
    private ServiceProvider Provider()
    {
        var services = new ServiceCollection();
        services.AddKiaiRepositories(fixture.ConnectionString);
        return services.BuildServiceProvider();
    }

    private async Task<(Guid Org, Guid Person, Guid Client, Guid Modality)> SeedAsync()
    {
        var org = Guid.NewGuid(); var person = Guid.NewGuid(); var client = Guid.NewGuid(); var modality = Guid.NewGuid();
        await using var c = new NpgsqlConnection(fixture.ConnectionString);
        await c.ExecuteAsync("""
            insert into organizations(id,name,status) values(@org,'Academia Mobile','active');
            insert into organization_settings(id,organization_id,timezone,allow_client_checkin,require_teacher_confirmation)
                values(gen_random_uuid(),@org,'America/Sao_Paulo',true,false);
            insert into persons(id,organization_id,full_name) values(@person,@org,'Aluno Mobile');
            insert into clients(id,organization_id,person_id,joined_at) values(@client,@org,@person,'2000-01-01');
            insert into modalities(id,organization_id,name,type) values(@modality,@org,'Treino','fitness');
            insert into person_modalities(person_id,modality_id) values(@person,@modality);
            """, new { org, person, client, modality });
        return (org, person, client, modality);
    }

    [Fact]
    public async Task AccountPhotoAndPreferences_ShouldBePersonalAndTenantScoped()
    {
        var (org, person, _, _) = await SeedAsync();
        using var provider = Provider();
        var repository = provider.GetRequiredService<IMobileRepository>();
        var useCase = new MobileUseCase(repository);
        var administration = new KiaiControl.UseCases.Administration.AdministrationUseCase(provider.GetRequiredService<IAdministrationRepository>(), new Pbkdf2PasswordHasher());
        await Assert.ThrowsAsync<ArgumentException>(() => administration.SaveSettingsAsync(org, new() { RequireTeacherConfirmation = false }, default));
        Assert.NotNull(await repository.AccountAsync(org, person, default));
        Assert.Null(await repository.AccountAsync(Guid.NewGuid(), person, default));
        Assert.False(await useCase.SaveAccountAsync(Guid.NewGuid(), person, new() { FullName = "Outra pessoa" }, default));
        Assert.True(await useCase.SaveAccountAsync(org, person, new() { FullName = "Nome atualizado", PreferredName = "Ana", Email = " ANA@example.test ", HideFromRanking = true, PushEnabled = true }, default));
        var account = await useCase.AccountAsync(org, person, default);
        Assert.Equal("ana@example.test", account!.Email);
        Assert.True(account.PushEnabled); Assert.True(account.HideFromRanking);
        await Assert.ThrowsAsync<ArgumentException>(() => useCase.SavePhotoAsync(org, person, new() { Base64 = "não é base64" }, default));
        await Assert.ThrowsAsync<ArgumentException>(() => useCase.SavePhotoAsync(org, person, new() { Base64 = Convert.ToBase64String("<svg/>"u8.ToArray()), ContentType = "image/png" }, default));
        var jpeg = Convert.FromBase64String("/9j/2Q==");
        Assert.True(await repository.SavePhotoAsync(org, person, jpeg, "image/jpeg", default));
        Assert.Equal(Convert.ToBase64String(jpeg), (await useCase.AccountAsync(org, person, default))!.PhotoBase64);
        Assert.True(await useCase.RemovePhotoAsync(org, person, default));
        Assert.Null((await useCase.AccountAsync(org, person, default))!.PhotoBase64);
    }

    [Fact]
    public async Task Context_ShouldCombineMembershipAndEffectivePermissionsWithoutRestrictingUnknownRoles()
    {
        var (org, person, _, _) = await SeedAsync();
        using var provider = Provider();
        var useCase = new MobileUseCase(provider.GetRequiredService<IMobileRepository>());
        var custom = await useCase.ContextAsync(org, person, new() { Roles = ["custom"], Permissions = [PermissionCodes.NotificationsRead] }, default);
        Assert.Equal(new[] { "notifications.read" }, custom!.Capabilities);
        var student = await useCase.ContextAsync(org, person, new() { Roles = ["client"], Permissions = [] }, default);
        Assert.Contains("student.progress", student!.Capabilities);
        Assert.DoesNotContain("attendance.validate", student.Capabilities);
        var owner = await useCase.ContextAsync(org, person, new() { Roles = ["owner"], Permissions = [PermissionCodes.AttendanceManage, PermissionCodes.ReportsRead] }, default);
        Assert.Contains("attendance.validate", owner!.Capabilities); Assert.Contains("reports.dashboard", owner.Capabilities);
        Assert.Null(await useCase.ContextAsync(Guid.NewGuid(), person, new(), default));
    }

    [Fact]
    public async Task ProgressAndRanking_ShouldIgnorePendingSelfApprovedAndUnvalidatedAttendanceAndUseAcademyYear()
    {
        var (org, person, client, modality) = await SeedAsync();
        using var provider = Provider();
        var repo = provider.GetRequiredService<IMobileRepository>();
        await using var c = new NpgsqlConnection(fixture.ConnectionString);
        var validator = Guid.NewGuid();
        await c.ExecuteAsync("insert into persons(id,organization_id,full_name) values(@validator,@org,'Validador')", new { validator, org });
        var year = await c.QuerySingleAsync<int>("select extract(year from now() at time zone 'America/Sao_Paulo')::int");
        foreach (var (status, validated, self, start) in new[] {
            ("present", true, false, new DateTimeOffset(year, 1, 1, 2, 0, 0, TimeSpan.Zero)), // still last year in São Paulo
            ("present", true, false, new DateTimeOffset(year, 1, 2, 12, 0, 0, TimeSpan.Zero)),
            ("pending_confirmation", false, false, new DateTimeOffset(year, 1, 3, 12, 0, 0, TimeSpan.Zero)),
            ("present", false, false, new DateTimeOffset(year, 1, 4, 12, 0, 0, TimeSpan.Zero)),
            ("present", true, true, new DateTimeOffset(year, 1, 5, 12, 0, 0, TimeSpan.Zero)),
        }) {
            var lesson = Guid.NewGuid();
            await c.ExecuteAsync("""
                insert into lessons(id,organization_id,modality_id,starts_at,ends_at,status) values(@lesson,@org,@modality,@start,@end,'completed');
                insert into attendances(id,organization_id,lesson_id,client_id,status,validated_at,validated_by_person_id)
                    values(gen_random_uuid(),@org,@lesson,@client,cast(@status as attendance_status),@validatedAt,@by);
                """, new { lesson, org, modality, start, end = start.AddHours(1), client, status, validatedAt = validated ? (DateTimeOffset?)start : null, by = validated ? (self ? person : validator) : (Guid?)null });
        }
        var progress = await repo.ProgressAsync(org, client, default);
        Assert.Equal(2, progress.TotalClasses); Assert.Equal(1, progress.YearClasses); Assert.Equal(0, progress.CurrentStreak);
        Assert.Equal(1, progress.YearPosition);
        var ranking = Assert.Single(await repo.RankingAsync(org, client, true, 1, 25, default));
        Assert.Equal(1, ranking.AttendanceCount); Assert.True(ranking.IsMe);
        Assert.Empty(await repo.RankingAsync(Guid.NewGuid(), client, false, 1, 25, default));
        await c.ExecuteAsync("update persons set hide_from_ranking=true where id=@person", new { person });
        Assert.Empty(await repo.RankingAsync(org, client, false, 1, 25, default));
        Assert.Null((await repo.ProgressAsync(org, client, default)).TotalPosition);
    }

    [Fact]
    public async Task CheckIn_ShouldRemainPendingAndTeacherValidationMustStayWithinAssignedLessons()
    {
        var (org, person, client, modality) = await SeedAsync();
        using var provider = Provider();
        var teacherPerson = Guid.NewGuid(); var teacher = Guid.NewGuid(); var lesson = Guid.NewGuid();
        var starts = DateTimeOffset.UtcNow.AddHours(1);
        await using var c = new NpgsqlConnection(fixture.ConnectionString);
        await c.ExecuteAsync("""
            insert into persons(id,organization_id,full_name) values(@teacherPerson,@org,'Professor Mobile');
            insert into teachers(id,organization_id,person_id) values(@teacher,@org,@teacherPerson);
            insert into lessons(id,organization_id,modality_id,starts_at,ends_at,status) values(@lesson,@org,@modality,@starts,@ends,'scheduled');
            """, new { teacherPerson, org, teacher, lesson, modality, starts, ends = starts.AddHours(1) });
        var attendance = provider.GetRequiredService<IAttendanceRepository>();
        var record = await attendance.CreateCheckInAsync(org, person, lesson, DateTimeOffset.UtcNow, default);
        Assert.Equal("pending_confirmation", record!.Status); Assert.Null(record.ValidatedAt);
        Assert.Null(await attendance.ValidateCheckInAsync(org, record.Id, person, "present", DateTimeOffset.UtcNow, default));
        Assert.Null(await attendance.CreateCheckInAsync(org, person, lesson, starts.AddSeconds(1), default));
        var controller = new AttendanceController(new OrganizationContext { OrganizationId = org }, new(attendance),
            new TeacherUseCase(provider.GetRequiredService<ITeacherRepository>(), provider.GetRequiredService<IBranchRepository>()),
            new LessonUseCase(provider.GetRequiredService<ILessonRepository>())) {
            ControllerContext = new() { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] {
                new Claim(ClaimTypes.Role, "teacher"), new Claim("person_id", teacherPerson.ToString()), new Claim("permission", PermissionCodes.AttendanceManage)
            }, "test")) } }
        };
        Assert.IsType<NotFoundResult>((await controller.ValidateCheckInAsync(record.Id, new() { Status = "present" }, default)).Result);
        Assert.Empty(await attendance.ListPendingByOrganizationAsync(org, 1, 25, default, teacher));
        Assert.Empty(await attendance.ListByOrganizationAsync(org, 1, 25, default, teacher));
        await c.ExecuteAsync("insert into lesson_instructors(id,lesson_id,teacher_id) values(gen_random_uuid(),@lesson,@teacher)", new { lesson, teacher });
        Assert.Single(await attendance.ListPendingByOrganizationAsync(org, 1, 25, default, teacher));
        Assert.IsType<OkObjectResult>((await controller.ValidateCheckInAsync(record.Id, new() { Status = "present" }, default)).Result);
        Assert.IsType<ConflictObjectResult>((await controller.ValidateCheckInAsync(record.Id, new() { Status = "present" }, default)).Result);
        Assert.False(await attendance.CancelClientCheckInAsync(org, person, lesson, DateTimeOffset.UtcNow, default));
        await c.ExecuteAsync("update lessons set status='completed' where id=@lesson", new { lesson });
        Assert.Equal(1, (await provider.GetRequiredService<IMobileRepository>().ProgressAsync(org, client, default)).TotalClasses);
    }

    [Fact]
    public async Task HttpContracts_ShouldEnforceTemporaryPasswordPermissionsAndPersonalOwnership()
    {
        var (org, person, _, _) = await SeedAsync();
        var (_, otherPerson, _, _) = await SeedAsync();
        var userId = Guid.NewGuid(); var email = $"{userId:N}@example.test";
        await using var c = new NpgsqlConnection(fixture.ConnectionString);
        await c.ExecuteAsync("""
            insert into user_accounts(id,person_id,email,password_hash,must_change_password) values(@userId,@person,@email,@hash,true);
            insert into person_roles(person_id,role_id) select @person,id from roles where code='client';
            """, new { userId, person, email, hash = new Pbkdf2PasswordHasher().Hash("Initial!123") });
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development", ApplicationName = typeof(MobileController).Assembly.FullName });
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> {
            ["ConnectionStrings:PostgreSql"] = fixture.ConnectionString,
            ["Jwt:Issuer"] = "Kiai.Mobile.Tests", ["Jwt:Audience"] = "Kiai.Mobile.Tests",
            ["Jwt:SigningKey"] = "mobile-integration-signing-key-at-least-32-bytes",
            ["RateLimiting:AuthPermitLimit"] = "100"
        });
        builder.Services.AddKiaiApi(builder.Configuration, builder.Environment);
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        await using var app = builder.Build();
        app.UseKiaiApi();
        await app.StartAsync();
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var http = new HttpClient { BaseAddress = new Uri(address) };
            var login = await http.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Initial!123" });
            login.EnsureSuccessStatusCode();
            var session = (await login.Content.ReadFromJsonAsync<SessionTokenResponse>())!;
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
            var context = await http.GetFromJsonAsync<KiaiControl.Contracts.Mobile.MobileContextResponse>("/api/v1/mobile/context");
            Assert.True(context!.MustChangePassword);
            Assert.Equal(HttpStatusCode.Forbidden, (await http.GetAsync("/api/v1/mobile/account")).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await http.PostAsJsonAsync("/api/v1/auth/change-password", new { currentPassword = "Initial!123", newPassword = "Changed!123" })).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await http.GetAsync("/api/v1/mobile/account")).StatusCode);
            var next = await http.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Changed!123" });
            next.EnsureSuccessStatusCode();
            session = (await next.Content.ReadFromJsonAsync<SessionTokenResponse>())!;
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
            context = await http.GetFromJsonAsync<KiaiControl.Contracts.Mobile.MobileContextResponse>("/api/v1/mobile/context");
            Assert.False(context!.MustChangePassword);
            Assert.Contains("student.progress", context.Capabilities);
            Assert.Equal(HttpStatusCode.Forbidden, (await http.GetAsync("/api/v1/attendance/pending")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await http.PatchAsJsonAsync($"/api/v1/attendance/{Guid.NewGuid()}/validate", new { status = "present" })).StatusCode);
            foreach (var path in new[] { "/api/v1/mobile/account", "/api/v1/mobile/progress", "/api/v1/mobile/rankings", "/api/v1/mobile/lessons", "/api/v1/portal/journey" })
                Assert.Equal(HttpStatusCode.OK, (await http.GetAsync(path)).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await http.PutAsJsonAsync("/api/v1/mobile/account", new { fullName = "Perfil pessoal", personId = otherPerson, organizationId = Guid.NewGuid(), pushEnabled = false })).StatusCode);
            Assert.Equal("Perfil pessoal", await c.QuerySingleAsync<string>("select full_name from persons where id=@person", new { person }));
            Assert.Equal("Aluno Mobile", await c.QuerySingleAsync<string>("select full_name from persons where id=@otherPerson", new { otherPerson }));
        }
        finally { await app.StopAsync(); }
    }

    [Fact]
    public async Task PushQueue_ShouldHonorOptInScheduleOwnershipLeaseAndInvalidation()
    {
        var (org, person, _, _) = await SeedAsync();
        using var provider = Provider();
        var mobile = provider.GetRequiredService<IMobileRepository>();
        var queue = provider.GetRequiredService<IPushDeliveryRepository>();
        var notifications = provider.GetRequiredService<INotificationRepository>();
        var installation = Guid.NewGuid().ToString();
        await using (var seed = new NpgsqlConnection(fixture.ConnectionString))
            await seed.ExecuteAsync("insert into user_accounts(id,person_id,email,password_hash) values(gen_random_uuid(),@person,@email,'test-hash')", new { person, email = $"{person:N}@example.test" });
        Assert.True(await mobile.RegisterDeviceAsync(org, person, installation, "test-target", "android", default));
        var notification = await notifications.CreateAsync(new() { OrganizationId = org, PersonId = person, Title = "Aviso", Body = "Seu treino", Channel = "push" });
        Assert.Equal("pending", notification!.Status);
        Assert.DoesNotContain(await queue.ClaimAsync(default), d => d.NotificationId == notification.Id);
        var account = (await mobile.AccountAsync(org, person, default))!; account.PushEnabled = true;
        await mobile.SaveAccountAsync(org, person, account, default);
        var delivery = Assert.Single(await queue.ClaimAsync(default), d => d.NotificationId == notification.Id);
        Assert.DoesNotContain(await queue.ClaimAsync(default), d => d.NotificationId == notification.Id);
        await queue.CompleteAsync(delivery, PushSendResult.Accepted, default);
        Assert.Equal("sent", (await notifications.GetMineAsync(org, person, notification.Id))!.Status);
        Assert.Null(await notifications.GetMineAsync(Guid.NewGuid(), person, notification.Id));
        Assert.True(await notifications.MarkAsReadAsync(org, person, notification.Id));
        Assert.True(await notifications.MarkAsReadAsync(org, person, notification.Id));
        await queue.CompleteAsync(delivery, PushSendResult.InvalidToken, default);
        await using var c = new NpgsqlConnection(fixture.ConnectionString);
        Assert.Equal(0, await c.QuerySingleAsync<int>("select count(1)::int from mobile_devices where installation_id=@installation", new { installation }));
        Assert.True(await mobile.RegisterDeviceAsync(org, person, installation, "test-target", "android", default));
        var revokedNotification = await notifications.CreateAsync(new() { OrganizationId = org, PersonId = person, Title = "Revogada", Body = "Não enviar", Channel = "push" });
        await c.ExecuteAsync("update user_accounts set session_version=session_version+1 where person_id=@person", new { person });
        Assert.DoesNotContain(await queue.ClaimAsync(default), d => d.NotificationId == revokedNotification!.Id);
        Assert.True(await mobile.RegisterDeviceAsync(org, person, installation, "test-target", "android", default));
        var revokedDelivery = Assert.Single(await queue.ClaimAsync(default), d => d.NotificationId == revokedNotification!.Id);
        await queue.CompleteAsync(revokedDelivery, PushSendResult.Retry, default);
        account.PushEnabled = false;
        await mobile.SaveAccountAsync(org, person, account, default);
        await c.ExecuteAsync("update push_deliveries set available_at=now()-interval '1 second' where notification_id=@id", new { id = revokedNotification!.Id });
        Assert.DoesNotContain(await queue.ClaimAsync(default), d => d.NotificationId == revokedNotification.Id);
        var future = await notifications.CreateAsync(new() { OrganizationId = org, PersonId = person, Title = "Agendado", Body = "Depois", Channel = "push", ScheduledAt = DateTimeOffset.UtcNow.AddHours(1) });
        Assert.Null(await notifications.GetMineAsync(org, person, future!.Id));
    }
}
