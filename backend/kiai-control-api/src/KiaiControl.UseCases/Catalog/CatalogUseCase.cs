using KiaiControl.Contracts.Common;
using KiaiControl.Contracts.Clients;
using KiaiControl.Contracts.Teachers;
using KiaiControl.Contracts.Branches;
using KiaiControl.Contracts.Modalities;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.UseCases.Clients;
using KiaiControl.UseCases.Teachers;
using KiaiControl.UseCases.Branches;
using KiaiControl.UseCases.Modalities;

namespace KiaiControl.UseCases.Catalog;
public sealed class CatalogUseCase(ICatalogRepository repository)
{
    public async Task<PagedResponse<KiaiControl.Contracts.Lessons.LessonResponse>> LessonsAsync(Guid org, int page, int size, DateTimeOffset? from, DateTimeOffset? to, string? status, Guid? teacherId, CancellationToken ct, string? area = null)
    {
        Validate(org, page, size, null);
        if (from is not null && to is not null && (to <= from || to - from > TimeSpan.FromDays(367)))
            throw new ArgumentException("Selecione um período válido de até 367 dias.");
        return Map(await repository.LessonsAsync(org, page, size, from, to, status, teacherId, ct, AcademyArea.Normalize(area)), KiaiControl.UseCases.Lessons.LessonUseCase.ToResponse);
    }

    public async Task<PagedResponse<ClientResponse>> ClientsAsync(Guid org, int page, int size, string? search, CancellationToken ct, string? area = null)
    {
        Validate(org, page, size, search);
        return Map(await repository.ClientsAsync(org, page, size, search, ct, AcademyArea.Normalize(area)), ClientUseCase.Map);
    }

    public async Task<PagedResponse<TeacherResponse>> TeachersAsync(Guid org, int page, int size, string? search, CancellationToken ct, string? area = null)
    {
        Validate(org, page, size, search);
        return Map(await repository.TeachersAsync(org, page, size, search, ct, AcademyArea.Normalize(area)), TeacherUseCase.Map);
    }

    public async Task<PagedResponse<BranchResponse>> BranchesAsync(Guid org, int page, int size, string? search, CancellationToken ct)
    {
        Validate(org, page, size, search);
        return Map(await repository.BranchesAsync(org, page, size, search, ct), BranchUseCase.ToResponse);
    }

    public async Task<PagedResponse<ModalityResponse>> ModalitiesAsync(Guid org, int page, int size, string? search, CancellationToken ct, string? area = null)
    {
        Validate(org, page, size, search);
        return Map(await repository.ModalitiesAsync(org, page, size, search, ct, AcademyArea.Normalize(area)), ModalityUseCase.ToResponse);
    }

    private static PagedResponse<TOut> Map<TIn, TOut>(PagedResult<TIn> result, Func<TIn, TOut> map) => new(result.Items.Select(map).ToArray(), result.Total);
    public static void Validate(Guid org, int page, int size, string? search)
    {
        if (org == Guid.Empty || page < 1 || page > 1_000_000 || size is < 1 or > 200 || search?.Length > 200)
            throw new ArgumentException("Informe uma organização e paginação válidas (até 200 registros por página).");
    }
}
