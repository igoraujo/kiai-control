namespace KiaiControl.Contracts.Common;

public sealed record ContextStatusResponse(string Context, string Status, Guid? OrganizationId);
