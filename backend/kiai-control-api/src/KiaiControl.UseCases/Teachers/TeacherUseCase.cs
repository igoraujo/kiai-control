using KiaiControl.Contracts.Teachers;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;
using KiaiControl.UseCases.Common;

namespace KiaiControl.UseCases.Teachers;

public sealed class TeacherUseCase(ITeacherRepository teacherRepository, IBranchRepository branchRepository)
{
    public async Task<TeacherResponse> CreateAsync(CreateTeacherRequest request, CancellationToken cancellationToken)
    {
        await PersonDataValidator.ValidateBranchAsync(branchRepository, request.OrganizationId, request.BranchId, cancellationToken);

        var teacher = new Teacher
        {
            OrganizationId = request.OrganizationId,
            BranchId = request.BranchId,
            FullName = PersonDataValidator.NormalizeName(request.FullName),
            PreferredName = PersonDataValidator.NormalizeOptional(request.PreferredName, "PreferredName", 100),
            Email = PersonDataValidator.NormalizeEmail(request.Email),
            Phone = PersonDataValidator.NormalizeOptional(request.Phone, "Phone", 30),
            BirthDate = request.BirthDate,
            Document = PersonDataValidator.NormalizeDocument(request.Document),
            MedicalNotes = PersonDataValidator.NormalizeOptional(request.MedicalNotes, "MedicalNotes", int.MaxValue),
            EmergencyContactName = PersonDataValidator.NormalizeOptional(request.EmergencyContactName, "EmergencyContactName", 160),
            EmergencyContactPhone = PersonDataValidator.NormalizeOptional(request.EmergencyContactPhone, "EmergencyContactPhone", 30),
            HideFromRanking = request.HideFromRanking,
            HiredAt = request.HiredAt,
            ActiveFrom = request.ActiveFrom,
            Status = PersonDataValidator.NormalizeStatus(request.Status)
        };

        return Map(await teacherRepository.CreateAsync(teacher, cancellationToken));
    }

    public async Task<IReadOnlyList<TeacherResponse>> ListAsync(Guid organizationId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var teachers = await teacherRepository.ListByOrganizationAsync(organizationId, page, pageSize, cancellationToken);
        return teachers.Select(Map).ToList();
    }

    public async Task<TeacherResponse?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        var teacher = await teacherRepository.GetByIdAsync(organizationId, id, cancellationToken);
        return teacher is null ? null : Map(teacher);
    }

    public async Task<TeacherResponse?> UpdateAsync(Guid organizationId, Guid id, UpdateTeacherRequest request, CancellationToken cancellationToken)
    {
        await PersonDataValidator.ValidateBranchAsync(branchRepository, organizationId, request.BranchId, cancellationToken);

        var teacher = new Teacher
        {
            Id = id,
            OrganizationId = organizationId,
            BranchId = request.BranchId,
            FullName = PersonDataValidator.NormalizeName(request.FullName),
            PreferredName = PersonDataValidator.NormalizeOptional(request.PreferredName, "PreferredName", 100),
            Email = PersonDataValidator.NormalizeEmail(request.Email),
            Phone = PersonDataValidator.NormalizeOptional(request.Phone, "Phone", 30),
            BirthDate = request.BirthDate,
            Document = PersonDataValidator.NormalizeDocument(request.Document),
            MedicalNotes = PersonDataValidator.NormalizeOptional(request.MedicalNotes, "MedicalNotes", int.MaxValue),
            EmergencyContactName = PersonDataValidator.NormalizeOptional(request.EmergencyContactName, "EmergencyContactName", 160),
            EmergencyContactPhone = PersonDataValidator.NormalizeOptional(request.EmergencyContactPhone, "EmergencyContactPhone", 30),
            HideFromRanking = request.HideFromRanking,
            HiredAt = request.HiredAt,
            ActiveFrom = request.ActiveFrom,
            Status = PersonDataValidator.NormalizeStatus(request.Status)
        };

        var updated = await teacherRepository.UpdateAsync(teacher, cancellationToken);
        return updated is null ? null : Map(updated);
    }

    public Task<bool> DeleteAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        return teacherRepository.DeleteAsync(organizationId, id, cancellationToken);
    }

    private static TeacherResponse Map(Teacher teacher) => new()
    {
        Id = teacher.Id,
        OrganizationId = teacher.OrganizationId,
        PersonId = teacher.PersonId,
        BranchId = teacher.BranchId,
        FullName = teacher.FullName,
        PreferredName = teacher.PreferredName,
        Email = teacher.Email,
        Phone = teacher.Phone,
        BirthDate = teacher.BirthDate,
        Document = teacher.Document,
        MedicalNotes = teacher.MedicalNotes,
        EmergencyContactName = teacher.EmergencyContactName,
        EmergencyContactPhone = teacher.EmergencyContactPhone,
        HideFromRanking = teacher.HideFromRanking,
        HiredAt = teacher.HiredAt,
        ActiveFrom = teacher.ActiveFrom,
        Status = teacher.Status,
        CreatedAt = teacher.CreatedAt,
        UpdatedAt = teacher.UpdatedAt
    };
}