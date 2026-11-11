using KiaiControl.Contracts.Administration;
using KiaiControl.Contracts.Auth;
using KiaiControl.Contracts.Common;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.UseCases.Catalog;
using KiaiControl.UseCases.Common;

namespace KiaiControl.UseCases.Administration;
public sealed class AdministrationUseCase(IAdministrationRepository repository, IPasswordHasher hasher)
{
    public async Task<PagedResponse<DirectoryPersonResponse>> ListPeopleAsync(Guid org, int page, int size, string? search, CancellationToken ct)
    {
        CatalogUseCase.Validate(org, page, size, search);
        var r = await repository.ListPeopleAsync(org, page, size, search, ct);
        return new(r.Items.Select(p => new DirectoryPersonResponse { PersonId = p.PersonId, FullName = p.FullName, Email = p.Email, Phone = p.Phone, Status = p.Status, ClientId = p.ClientId, TeacherId = p.TeacherId, UserId = p.UserId, LoginEmail = p.LoginEmail, IsActive = p.IsActive, MustChangePassword = p.MustChangePassword, Roles = p.Roles }).ToArray(), r.Total);
    }

    public async Task<IReadOnlyList<PersonOptionResponse>> ProfileCandidatesAsync(Guid org,string profile,bool owner,CancellationToken ct)
    {
        var all=new List<DirectoryPersonResponse>();
        for(var page=1;;page++){var result=await ListPeopleAsync(org,page,200,null,ct);all.AddRange(result.Items);if(all.Count>=result.Total)break;}
        return all.Where(p=>(profile=="client"?p.ClientId is null:p.TeacherId is null) && (owner||!p.Roles.Any(r=>r is "owner" or "admin"))).Select(p=>new PersonOptionResponse(p.PersonId,p.FullName)).ToArray();
    }
    public Task<Guid> SavePersonAsync(Guid org, Guid? id, SavePersonRequest request, CancellationToken ct)
    {
        CatalogUseCase.Validate(org, 1, 1, null);
        return repository.SavePersonAsync(org, id, PersonDataValidator.NormalizeName(request.FullName), PersonDataValidator.NormalizeEmail(request.Email), PersonDataValidator.NormalizeOptional(request.Phone, "Telefone", 30), ct);
    }

    public Task<bool> AddProfileAsync(Guid org, Guid person, string profile, Guid actor, CancellationToken ct)
    {
        if (org == Guid.Empty || person == Guid.Empty || actor == Guid.Empty || profile is not ("client" or "teacher"))
            throw new ArgumentException("Pessoa ou perfil inválido.");
        return repository.AddProfileAsync(org, person, profile, actor, ct);
    }

    public Task<bool> ManageAccountAsync(Guid org, Guid person, Guid actor, bool owner, bool? active, string? password, CancellationToken ct, string? email = null)
    {
        if (org == Guid.Empty || person == Guid.Empty || actor == Guid.Empty)
            throw new ArgumentException("Contexto inválido.");
        if (password is not null && !PasswordPolicy.IsValid(password))
            throw new ArgumentException(PasswordPolicy.Requirements);
        if (password?.Length > 256)
            throw new ArgumentException("A senha temporária deve ter no máximo 256 caracteres.");
        return repository.ManageAccountAsync(org, person, actor, owner, active, password is null ? null : hasher.Hash(password), ct, email is null ? null : PersonDataValidator.NormalizeEmail(email));
    }

    public async Task<OrganizationSettingsResponse> GetSettingsAsync(Guid org, CancellationToken ct)
    {
        CatalogUseCase.Validate(org, 1, 1, null);
        var s = await repository.GetSettingsAsync(org, ct);
        return new()
        {
            Timezone = s.Timezone,
            CurrencyCode = s.CurrencyCode,
            AllowClientCheckin = s.AllowClientCheckin,
            RequireTeacherConfirmation = s.RequireTeacherConfirmation,
            DefaultLateToleranceMinutes = s.DefaultLateToleranceMinutes
        };
    }

    public Task SaveSettingsAsync(Guid org, OrganizationSettingsResponse s, CancellationToken ct)
    {
        CatalogUseCase.Validate(org, 1, 1, null);
        if (s.DefaultLateToleranceMinutes is < 0 or > 180 || s.CurrencyCode != "BRL")
            throw new ArgumentException("Informe uma tolerância entre 0 e 180 minutos e moeda BRL.");
        if (s.Timezone != "UTC" && !s.Timezone.Contains('/'))
            throw new ArgumentException("Use um fuso IANA, como America/Sao_Paulo.");
        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(s.Timezone);
        }
        catch (Exception e)when (e is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            throw new ArgumentException("Fuso horário inválido.");
        }

        return repository.SaveSettingsAsync(org, new() { Timezone = s.Timezone, CurrencyCode = s.CurrencyCode, AllowClientCheckin = s.AllowClientCheckin, RequireTeacherConfirmation = s.RequireTeacherConfirmation, DefaultLateToleranceMinutes = s.DefaultLateToleranceMinutes }, ct);
    }
}
