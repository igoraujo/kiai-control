using KiaiControl.Contracts.Teachers;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;

namespace KiaiControl.UseCases.Teachers;

public sealed class TeacherUseCase(ITeacherRepository teacherRepository)
{
    public async Task<TeacherResponse> CreateAsync(CreateTeacherRequest request, CancellationToken cancellationToken)
    {
        ValidateName(request.FullName);
        ValidateStatus(request.Status);

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
            CreatedAt = created.CreatedAt,
            UpdatedAt = created.UpdatedAt
        };
    }

    public async Task<IReadOnlyList<TeacherResponse>> ListAsync(Guid organizationId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var teachers = await teacherRepository.ListByOrganizationAsync(organizationId, page, pageSize, cancellationToken);

        return teachers.Select(teacher => new TeacherResponse
        {
            Id = teacher.Id,
            OrganizationId = teacher.OrganizationId,
            FullName = teacher.FullName,
            Status = teacher.Status,
            CreatedAt = teacher.CreatedAt,
            UpdatedAt = teacher.UpdatedAt
        }).ToList();
    }

    public async Task<TeacherResponse?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        var teacher = await teacherRepository.GetByIdAsync(organizationId, id, cancellationToken);
        if (teacher is null)
        {
            return null;
        }

        return new TeacherResponse
        {
            Id = teacher.Id,
            OrganizationId = teacher.OrganizationId,
            FullName = teacher.FullName,
            Status = teacher.Status,
            CreatedAt = teacher.CreatedAt,
            UpdatedAt = teacher.UpdatedAt
        };
    }

    public async Task<TeacherResponse?> UpdateAsync(Guid organizationId, Guid id, UpdateTeacherRequest request, CancellationToken cancellationToken)
    {
        ValidateName(request.FullName);
        ValidateStatus(request.Status);

        var teacher = new Teacher
        {
            Id = id,
            OrganizationId = organizationId,
            FullName = request.FullName,
            Status = request.Status
        };

        var updated = await teacherRepository.UpdateAsync(teacher, cancellationToken);
        if (updated is null)
        {
            return null;
        }

        return new TeacherResponse
        {
            Id = updated.Id,
            OrganizationId = updated.OrganizationId,
            FullName = updated.FullName,
            Status = updated.Status,
            CreatedAt = updated.CreatedAt,
            UpdatedAt = updated.UpdatedAt
        };
    }

    public Task<bool> DeleteAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        return teacherRepository.DeleteAsync(organizationId, id, cancellationToken);
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("FullName is required.");
        }
    }

    private static void ValidateStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            throw new ArgumentException("Status is required.");
        }
    }
}
