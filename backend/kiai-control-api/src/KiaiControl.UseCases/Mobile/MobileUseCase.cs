using KiaiControl.Contracts.Auth;
using KiaiControl.Contracts.Mobile;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.UseCases.Catalog;
using KiaiControl.UseCases.Common;

namespace KiaiControl.UseCases.Mobile;

public sealed class MobileUseCase(IMobileRepository repository)
{
    public async Task<MobileContextResponse?> ContextAsync(Guid org, Guid person, AuthUserProfileResponse user, CancellationToken ct)
    {
        var membership = await repository.MembershipAsync(org, person, ct);
        if (membership is null) return null;
        var capabilities = new List<string>();
        bool Role(params string[] roles) => user.Roles.Intersect(roles, StringComparer.OrdinalIgnoreCase).Any();
        bool Permission(string code) => user.Permissions.Contains(code, StringComparer.OrdinalIgnoreCase);
        if (membership.ClientId is not null && Role("client")) capabilities.AddRange(["student.lessons", "student.progress"]);
        if (membership.ClientId is not null && membership.HasMartialArts && Role("client")) capabilities.Add("student.graduation");
        if (membership.TeacherId is not null && Role("teacher") && Permission(PermissionCodes.LessonsManage)) capabilities.Add("teacher.lessons");
        if (Role("owner", "admin", "teacher", "reception") && Permission(PermissionCodes.AttendanceManage)) capabilities.Add("attendance.validate");
        if (Role("owner", "admin", "financial") && Permission(PermissionCodes.ReportsRead)) capabilities.Add("reports.dashboard");
        if (Permission(PermissionCodes.NotificationsRead)) capabilities.Add("notifications.read");
        return new(membership.OrganizationName, membership.Timezone, capabilities, user.Roles, user.Permissions, user.MustChangePassword);
    }

    public async Task<MobileAccountResponse?> AccountAsync(Guid org, Guid person, CancellationToken ct)
    {
        var a = await repository.AccountAsync(org, person, ct);
        return a is null ? null : new() { FullName = a.FullName, PreferredName = a.PreferredName, Email = a.Email, Phone = a.Phone,
            HideFromRanking = a.HideFromRanking, PushEnabled = a.PushEnabled, PhotoBase64 = a.Photo is null ? null : Convert.ToBase64String(a.Photo), PhotoContentType = a.PhotoContentType };
    }

    public Task<bool> SaveAccountAsync(Guid org, Guid person, SaveMobileAccountRequest r, CancellationToken ct) =>
        repository.SaveAccountAsync(org, person, new MobileAccount { FullName = PersonDataValidator.NormalizeName(r.FullName),
            PreferredName = PersonDataValidator.NormalizeOptional(r.PreferredName, "Nome de preferência", 100),
            Email = PersonDataValidator.NormalizeEmail(r.Email), Phone = PersonDataValidator.NormalizeOptional(r.Phone, "Telefone", 30),
            HideFromRanking = r.HideFromRanking, PushEnabled = r.PushEnabled }, ct);

    public Task<bool> SavePhotoAsync(Guid org, Guid person, SaveMobilePhotoRequest r, CancellationToken ct)
    {
        byte[] bytes;
        try { bytes = Convert.FromBase64String(r.Base64); }
        catch (FormatException) { throw new ArgumentException("Foto inválida. Envie JPEG ou PNG de até 512 KB."); }
        var jpeg = bytes.Length >= 4 && bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[^2] == 0xff && bytes[^1] == 0xd9;
        var png = bytes.Length >= 24 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        if (bytes.Length > 512 * 1024 || !(r.ContentType == "image/jpeg" && jpeg || r.ContentType == "image/png" && png))
            throw new ArgumentException("Foto inválida. Envie JPEG ou PNG de até 512 KB.");
        return repository.SavePhotoAsync(org, person, bytes, r.ContentType, ct);
    }

    public Task<bool> RemovePhotoAsync(Guid org, Guid person, CancellationToken ct) => repository.SavePhotoAsync(org, person, null, null, ct);

    public async Task<Guid?> ClientAsync(Guid org, Guid person, CancellationToken ct) => (await repository.MembershipAsync(org, person, ct))?.ClientId;

    public async Task<MobileProgressResponse> ProgressAsync(Guid org, Guid client, CancellationToken ct)
    {
        var p = await repository.ProgressAsync(org, client, ct);
        return new(p.Year, p.TotalClasses, p.YearClasses, p.CurrentStreak, p.TotalPosition, p.YearPosition,
            p.Months.Select(m => new AttendanceMonthResponse(m.Month, m.AttendanceCount)).ToArray(),
            p.Graduations.Select(g => new GraduationProgressResponse(g.ModalityName, g.BeltName, g.DegreeNumber, g.NextLevelName, g.LessonsRemaining, g.RequiresManualEvaluation)).ToArray());
    }

    public async Task<IReadOnlyList<AttendanceRankingResponse>> RankingAsync(Guid org, Guid client, bool yearOnly, int page, int size, CancellationToken ct)
    {
        CatalogUseCase.Validate(org, page, size, null);
        return (await repository.RankingAsync(org, client, yearOnly, page, size, ct)).Select(r => new AttendanceRankingResponse(r.ClientName, r.Position, r.AttendanceCount, r.IsMe)).ToArray();
    }

    public Task<bool> RegisterDeviceAsync(Guid org, Guid person, RegisterDeviceRequest r, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(r.InstallationId) || r.InstallationId.Length > 200 || string.IsNullOrWhiteSpace(r.Token) || r.Token.Length > 4096 || r.Platform is not ("android" or "ios"))
            throw new ArgumentException("Registro do dispositivo inválido.");
        return repository.RegisterDeviceAsync(org, person, r.InstallationId, r.Token, r.Platform, ct);
    }

    public Task RemoveDeviceAsync(Guid org, Guid person, string installation, CancellationToken ct) => repository.RemoveDeviceAsync(org, person, installation, ct);
}
