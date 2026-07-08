using KiaiControl.Contracts.Students;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;

namespace KiaiControl.UseCases.Students;

public sealed class StudentUseCase(IStudentRepository studentRepository)
{
    public async Task<StudentResponse> CreateAsync(CreateStudentRequest request, CancellationToken cancellationToken)
    {
        var student = new Student
        {
            OrganizationId = request.OrganizationId,
            FullName = request.FullName,
            Status = request.Status
        };

        var created = await studentRepository.CreateAsync(student, cancellationToken);

        return new StudentResponse
        {
            Id = created.Id,
            OrganizationId = created.OrganizationId,
            FullName = created.FullName,
            Status = created.Status,
            CreatedAt = created.CreatedAt
        };
    }

    public async Task<IReadOnlyList<StudentResponse>> ListAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var students = await studentRepository.ListByOrganizationAsync(organizationId, cancellationToken);

        return students.Select(student => new StudentResponse
        {
            Id = student.Id,
            OrganizationId = student.OrganizationId,
            FullName = student.FullName,
            Status = student.Status,
            CreatedAt = student.CreatedAt
        }).ToList();
    }
}
