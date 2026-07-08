using KiaiControl.Contracts.Teachers;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;

namespace KiaiControl.UseCases.Teachers;

public sealed class TeacherUseCase(ITeacherRepository teacherRepository)
{
    public async Task<TeacherResponse> CreateAsync(CreateTeacherRequest request, CancellationToken cancellationToken)
    {
        var teacher = new Teacher
        {
            OrganizationId = request.OrganizationId,
            FullName = request.FullName,
            Status = request.Status
        };

        var created = await teacherRepository.CreateAsync(teacher, cancellationToken);

        return new TeacherResponse
        {
            Id = created.Id,
            OrganizationId = created.OrganizationId,
            FullName = created.FullName,
            Status = created.Status,
            CreatedAt = created.CreatedAt
        };
    }

    public async Task<IReadOnlyList<TeacherResponse>> ListAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var teachers = await teacherRepository.ListByOrganizationAsync(organizationId, cancellationToken);

        return teachers.Select(teacher => new TeacherResponse
        {
            Id = teacher.Id,
            OrganizationId = teacher.OrganizationId,
            FullName = teacher.FullName,
            Status = teacher.Status,
            CreatedAt = teacher.CreatedAt
        }).ToList();
    }
}
