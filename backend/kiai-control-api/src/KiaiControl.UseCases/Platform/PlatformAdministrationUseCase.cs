using System.Net.Mail;
using KiaiControl.Contracts.Auth;
using KiaiControl.Contracts.Common;
using KiaiControl.Contracts.Organizations;
using KiaiControl.Contracts.Platform;
using KiaiControl.Core.Common;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.UseCases.Organizations;

namespace KiaiControl.UseCases.Platform;

public sealed class PlatformAdministrationUseCase(IPlatformAdministrationRepository repository, IPasswordHasher passwordHasher)
{
    private static readonly string[] AllowedRoles = ["owner", "admin", "reception", "financial", "client", "teacher"];

    public async Task<PagedResponse<OrganizationResponse>> ListOrganizationsAsync(int page, int pageSize, string? search, string? status, CancellationToken ct, string archive = "active")
    {
        ValidatePage(page, pageSize);
        status = Normalize(status);
        if (status is not null) ValidateStatus(status);
        if (archive is not ("active" or "archived" or "all")) throw new ArgumentException("Filtro de arquivamento inválido.");
        var result = await repository.ListOrganizationsAsync(page, pageSize, Normalize(search), status, ct, archive);
        return new(result.Items.Select(OrganizationUseCase.ToResponse).ToArray(), result.Total);
    }

    public async Task<OrganizationResponse?> GetOrganizationAsync(Guid id, CancellationToken ct)
    {
        ValidateTarget(id);
        var organization = await repository.GetOrganizationAsync(id, ct);
        return organization is null ? null : OrganizationUseCase.ToResponse(organization);
    }

    public Task<bool> ChangeArchiveAsync(Guid id, bool archived, PlatformArchiveRequest request, AuditLog audit, CancellationToken ct)
    {
        ValidateTarget(id);
        ValidateReason(request.Reason);
        if (string.IsNullOrWhiteSpace(request.ConfirmName) || request.ConfirmName.Length > 160)
            throw new ArgumentException("Confirme o nome da academia.");
        if (archived && request.Reactivate)
            throw new ArgumentException("A reativação só pode ser solicitada ao restaurar uma academia.");
        return repository.ChangeArchiveAsync(id, archived, request.ConfirmName, audit, ct, request.Reactivate);
    }

    public Task<bool> SetPrivacyHoldAsync(Guid id, PlatformPrivacyHoldRequest request, AuditLog audit, CancellationToken ct)
    {
        ValidateTarget(id);
        ValidateReason(request.Reason);
        return repository.SetPrivacyHoldAsync(id, request.Hold, audit, ct);
    }

    public async Task<PlatformAuditResponse?> GetAuditDetailsAsync(Guid id, PlatformAuditDetailsRequest request, CancellationToken ct)
    {
        if (id == Guid.Empty || request.OrganizationId == Guid.Empty)
            throw new ArgumentException("Informe o evento e a academia para consultar os detalhes.");
        var entry = await repository.GetAuditAsync(request.OrganizationId, id, ct);
        return entry is null ? null : new PlatformAuditResponse
        {
            Id = entry.Id, OrganizationId = entry.OrganizationId, OrganizationName = entry.OrganizationName ?? "",
            ActorUserId = entry.ActorUserAccountId, ActorName = entry.ActorName, ActorEmail = entry.ActorEmail,
            Action = entry.Action, EntityName = entry.EntityName, EntityId = entry.EntityId,
            Metadata = PlatformAuditPrivacy.SafeMetadata(entry.Metadata), IpAddress = entry.IpAddress, CreatedAt = entry.CreatedAt
        };
    }

    public Task<bool> UpdateOrganizationAsync(Guid id, PlatformOrganizationUpdateRequest request, AuditLog audit, CancellationToken ct)
    {
        ValidateTarget(id);
        ValidateReason(request.Reason);
        OrganizationUseCase.ValidateOrganization(request.Name, request.TradeName, request.Document, request.Email, request.Phone);
        return repository.UpdateOrganizationAsync(new Organization
        {
            Id = id, Name = request.Name.Trim(), TradeName = Normalize(request.TradeName),
            Document = Normalize(request.Document), Email = Normalize(request.Email)?.ToLowerInvariant(), Phone = Normalize(request.Phone)
        }, audit, ct);
    }

    public Task<bool> SetOrganizationStatusAsync(Guid id, OrganizationStatusRequest request, AuditLog audit, CancellationToken ct)
    {
        ValidateTarget(id);
        ValidateStatus(request.Status);
        ValidateReason(request.Reason);
        return repository.SetOrganizationStatusAsync(id, request.Status, audit, ct);
    }

    public async Task<PagedResponse<PlatformAccountResponse>> ListAccountsAsync(Guid id, int page, int pageSize, string? search, CancellationToken ct)
    {
        ValidateTarget(id);
        ValidatePage(page, pageSize);
        var result = await repository.ListAccountsAsync(id, page, pageSize, Normalize(search), ct);
        return new(result.Items.Select(x => new PlatformAccountResponse
        {
            UserId = x.UserId, PersonId = x.PersonId, FullName = x.FullName, Email = x.Email,
            IsActive = x.IsActive, MustChangePassword = x.MustChangePassword, Roles = x.Roles
        }).ToArray(), result.Total);
    }

    public Task<bool> CreateAccountAsync(Guid id, CreatePlatformAccountRequest request, AuditLog audit, CancellationToken ct)
    {
        ValidateTarget(id);
        ValidateReason(request.Reason);
        var email = ValidateEmail(request.Email);
        if (string.IsNullOrWhiteSpace(request.FullName) || request.FullName.Trim().Length > 180)
            throw new ArgumentException("Informe um nome com até 180 caracteres.");
        if (request.Role is not ("owner" or "admin"))
            throw new ArgumentException("O novo responsável deve ter o perfil owner ou admin.");
        ValidatePassword(request.TemporaryPassword);
        return repository.CreateAccountAsync(id, request.FullName.Trim(), email, passwordHasher.Hash(request.TemporaryPassword), request.Role, audit, ct);
    }

    public Task<bool> UpdateAccountAsync(Guid id, Guid userId, UpdatePlatformAccountRequest request, AuditLog audit, CancellationToken ct)
    {
        ValidateAccount(id, userId);
        ValidateReason(request.Reason);
        var email = ValidateEmail(request.Email);
        if (request.Roles is null || request.Roles.Length == 0 || request.Roles.Any(x => !AllowedRoles.Contains(x)))
            throw new ArgumentException("Selecione ao menos um perfil válido. O acesso à plataforma não pode ser concedido por esta operação.");
        return repository.UpdateAccountAsync(id, userId, email, request.Roles.Distinct().ToArray(), request.IsActive, audit, ct);
    }

    public Task<bool> ResetPasswordAsync(Guid id, Guid userId, PlatformPasswordRequest request, AuditLog audit, CancellationToken ct)
    {
        ValidateAccount(id, userId);
        ValidateReason(request.Reason);
        ValidatePassword(request.TemporaryPassword);
        return repository.ResetPasswordAsync(id, userId, passwordHasher.Hash(request.TemporaryPassword), audit, ct);
    }

    public Task<bool> RevokeSessionsAsync(Guid id, Guid userId, string reason, AuditLog audit, CancellationToken ct)
    {
        ValidateAccount(id, userId);
        ValidateReason(reason);
        return repository.RevokeSessionsAsync(id, userId, audit, ct);
    }

    public async Task<PagedResponse<PlatformAuditResponse>> ListAuditAsync(PlatformLogFilter filter, CancellationToken ct)
    {
        ValidateFilter(filter);
        if (filter.OrganizationId is Guid id && id != PlatformIdentity.OrganizationId
            && await repository.GetOrganizationAsync(id, ct) is null)
            throw new KeyNotFoundException("Academia não encontrada.");
        var result = await repository.ListAuditAsync(filter, ct);
        return new(result.Items.Select(x => new PlatformAuditResponse
        {
            Id = x.Id, OrganizationId = x.OrganizationId, OrganizationName = x.OrganizationName ?? "",
            ActorName = PlatformAuditPrivacy.ActorReference(x.ActorUserAccountId),
            Action = x.Action, EntityName = x.EntityName, CreatedAt = x.CreatedAt
        }).ToArray(), result.Total);
    }

    public async Task<PagedResponse<PlatformIncidentResponse>> ListIncidentsAsync(PlatformLogFilter filter, CancellationToken ct)
    {
        ValidateFilter(filter);
        if (filter.Action is not null && filter.Action is not ("request_failure" or "authentication_failure" or "audit_failure"))
            throw new ArgumentException("Categoria de falha inválida.");
        var result = await repository.ListIncidentsAsync(filter, ct);
        return new(result.Items.Select(x => new PlatformIncidentResponse
        {
            Id = x.Id, OrganizationId = x.OrganizationId, OrganizationName = x.OrganizationName,
            Category = x.Category, Method = x.Method, Route = x.Route,
            StatusCode = x.StatusCode, CorrelationId = x.CorrelationId, CreatedAt = x.CreatedAt
        }).ToArray(), result.Total);
    }

    public async Task<PlatformOverviewResponse> GetOverviewAsync(CancellationToken ct)
    {
        var result = await repository.GetOverviewAsync(ct);
        return new()
        {
            Organizations = result.Organizations, ActiveOrganizations = result.ActiveOrganizations,
            SuspendedOrganizations = result.SuspendedOrganizations, RequestFailures = result.RequestFailures,
            AuthenticationFailures = result.AuthenticationFailures, AuditFailures = result.AuditFailures
        };
    }

    public static void ValidateFilter(PlatformLogFilter filter)
    {
        ValidatePage(filter.Page, filter.PageSize);
        if (filter.To <= filter.From || filter.To - filter.From > TimeSpan.FromDays(366))
            throw new ArgumentException("Informe um período válido de até 366 dias.");
        if (filter.Action?.Length > 120 || filter.ActorSearch?.Length > 160)
            throw new ArgumentException("Os filtros excedem o tamanho permitido.");
    }

    private static void ValidatePage(int page, int pageSize)
    {
        if (page < 1 || page > 1_000_000 || pageSize is < 1 or > 200)
            throw new ArgumentException("Informe uma página válida e entre 1 e 200 registros por página.");
    }

    private static void ValidateTarget(Guid id)
    {
        if (id == Guid.Empty || id == PlatformIdentity.OrganizationId)
            throw new ArgumentException("Selecione uma academia válida.");
    }

    private static void ValidateAccount(Guid id, Guid userId)
    {
        ValidateTarget(id);
        if (userId == Guid.Empty) throw new ArgumentException("Selecione uma conta válida.");
    }

    private static void ValidateStatus(string status)
    {
        if (status is not ("active" or "inactive" or "suspended"))
            throw new ArgumentException("O status deve ser ativo, inativo ou suspenso.");
    }

    private static void ValidateReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length is < 5 or > 500)
            throw new ArgumentException("Informe o motivo da alteração, com 5 a 500 caracteres.");
    }

    private static string ValidateEmail(string email)
    {
        var normalized = Normalize(email);
        if (normalized is null || normalized.Length > 160 || !MailAddress.TryCreate(normalized, out var parsed) || parsed.Address != normalized)
            throw new ArgumentException("Informe um e-mail de acesso válido, com até 160 caracteres.");
        return normalized.ToLowerInvariant();
    }

    private static void ValidatePassword(string password)
    {
        if (password?.Length > 256 || !PasswordPolicy.IsValid(password))
            throw new ArgumentException(PasswordPolicy.Requirements);
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
