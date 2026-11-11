namespace KiaiControl.Contracts.Common;
public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Total);
