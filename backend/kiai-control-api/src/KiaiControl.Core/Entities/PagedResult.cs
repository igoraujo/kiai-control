namespace KiaiControl.Core.Entities;
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total);
