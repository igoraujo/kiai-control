using KiaiControl.Contracts.Graduation;
using KiaiControl.Core.Entities;
using KiaiControl.Core.Interfaces;

namespace KiaiControl.UseCases.Graduation;

public sealed class GraduationUseCase(IProgressionRepository repository)
{
    public async Task<GraduationSystemResponse?> CreateSystemAsync(
        Guid organizationId,
        CreateGraduationSystemRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty || request.ModalityId == Guid.Empty)
        {
            throw new ArgumentException(SystemMessages.OrganizationIdAndModalityIdAreRequired);
        }

        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 120 || request.Description?.Length > 5000)
        {
            throw new ArgumentException(SystemMessages.NameIsRequiredAndMustNotExceed120CharactersDescriptionMustNotExceed5000Characters);
        }

        var system = await repository.CreateGraduationSystemAsync(new GraduationSystem
        {
            OrganizationId = organizationId,
            ModalityId = request.ModalityId,
            Name = request.Name.Trim(),
            Description = Normalize(request.Description)
        }, cancellationToken);
        return system is null ? null : ToResponse(system);
    }

    public async Task<IReadOnlyList<GraduationSystemResponse>> ListSystemsAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        EnsureOrganization(organizationId);
        var systems = await repository.ListGraduationSystemsAsync(organizationId, cancellationToken);
        return systems.Select(ToResponse).ToList();
    }

    public async Task<IReadOnlyList<GraduationBeltResponse>> ListBeltsAsync(
        Guid organizationId,
        Guid systemId,
        CancellationToken cancellationToken)
    {
        EnsureOrganization(organizationId);
        if (systemId == Guid.Empty)
        {
            throw new ArgumentException(SystemMessages.GraduationSystemIdIsRequired);
        }

        var belts = await repository.ListGraduationBeltsAsync(organizationId, systemId, cancellationToken);
        return belts.Select(ToResponse).ToList();
    }

    public async Task<GraduationBeltResponse?> CreateBeltAsync(
        Guid organizationId,
        Guid systemId,
        CreateGraduationBeltRequest request,
        CancellationToken cancellationToken)
    {
        EnsureOrganization(organizationId);
        if (systemId == Guid.Empty || string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 80
            || request.DegreeCount is < 0 or > 99 || request.MinLessons < 0 || request.MinTrainingDays < 0
            || request.MinAttendancePercentage is < 0 or > 100)
        {
            throw new ArgumentException(SystemMessages.OsDadosDaFaixaSaoInvalidosInformeONomeAte99GrausECriteriosNaoNegativos);
        }

        var belt = await repository.CreateGraduationBeltAsync(organizationId, systemId, new GraduationBelt
        {
            Name = request.Name.Trim(),
            DegreeCount = request.DegreeCount,
            MinLessons = request.MinLessons,
            MinTrainingDays = request.MinTrainingDays,
            MinAttendancePercentage = request.MinAttendancePercentage,
            RequiresManualEvaluation = request.RequiresManualEvaluation
        }, cancellationToken);
        return belt is null ? null : ToResponse(belt);
    }

    public async Task<bool> UpdateBeltAsync(
        Guid organizationId,
        Guid systemId,
        Guid beltId,
        CreateGraduationBeltRequest request,
        CancellationToken cancellationToken)
    {
        EnsureOrganization(organizationId);
        ValidateBeltRequest(systemId, request);
        if (beltId == Guid.Empty)
        {
            throw new ArgumentException(SystemMessages.FaixaInvalida);
        }

        return await repository.UpdateGraduationBeltAsync(organizationId, systemId, new GraduationBelt
        {
            Id = beltId,
            Name = request.Name.Trim(),
            DegreeCount = request.DegreeCount,
            MinLessons = request.MinLessons,
            MinTrainingDays = request.MinTrainingDays,
            MinAttendancePercentage = request.MinAttendancePercentage,
            RequiresManualEvaluation = request.RequiresManualEvaluation
        }, cancellationToken);
    }

    public async Task<bool> DeleteBeltAsync(
        Guid organizationId,
        Guid systemId,
        Guid beltId,
        CancellationToken cancellationToken)
    {
        EnsureOrganization(organizationId);
        if (systemId == Guid.Empty || beltId == Guid.Empty)
        {
            throw new ArgumentException(SystemMessages.SistemaEFaixaSaoObrigatorios);
        }

        return await repository.DeleteGraduationBeltAsync(organizationId, systemId, beltId, cancellationToken);
    }

    public async Task<GraduationLevelResponse?> CreateDegreeAsync(
        Guid organizationId,
        Guid systemId,
        Guid beltId,
        CreateGraduationDegreeRequest request,
        CancellationToken cancellationToken)
    {
        EnsureOrganization(organizationId);
        ValidateDegreeRequest(systemId, beltId, request);

        var degree = await repository.CreateGraduationDegreeAsync(organizationId, systemId, beltId, new GraduationLevel
        {
            MinLessons = request.MinLessons,
            MinTrainingDays = request.MinTrainingDays,
            MinAttendancePercentage = request.MinAttendancePercentage,
            MinAge = request.MinAge,
            RequiresManualEvaluation = request.RequiresManualEvaluation
        }, cancellationToken);
        return degree is null ? null : ToResponse(degree);
    }

    public async Task<bool> UpdateDegreeAsync(
        Guid organizationId,
        Guid systemId,
        Guid beltId,
        Guid degreeId,
        CreateGraduationDegreeRequest request,
        CancellationToken cancellationToken)
    {
        EnsureOrganization(organizationId);
        ValidateDegreeRequest(systemId, beltId, request);
        if (degreeId == Guid.Empty)
        {
            throw new ArgumentException(SystemMessages.GrauInvalido);
        }

        return await repository.UpdateGraduationDegreeAsync(organizationId, systemId, beltId, new GraduationLevel
        {
            Id = degreeId,
            MinLessons = request.MinLessons,
            MinTrainingDays = request.MinTrainingDays,
            MinAttendancePercentage = request.MinAttendancePercentage,
            MinAge = request.MinAge,
            RequiresManualEvaluation = request.RequiresManualEvaluation
        }, cancellationToken);
    }

    public async Task<bool> DeleteDegreeAsync(
        Guid organizationId,
        Guid systemId,
        Guid beltId,
        Guid degreeId,
        CancellationToken cancellationToken)
    {
        EnsureOrganization(organizationId);
        if (systemId == Guid.Empty || beltId == Guid.Empty || degreeId == Guid.Empty)
        {
            throw new ArgumentException(SystemMessages.SistemaFaixaEGrauSaoObrigatorios);
        }

        return await repository.DeleteGraduationDegreeAsync(organizationId, systemId, beltId, degreeId, cancellationToken);
    }

    public async Task<GraduationLevelResponse?> CreateLevelAsync(
        Guid organizationId,
        Guid systemId,
        CreateGraduationLevelRequest request,
        CancellationToken cancellationToken)
    {
        EnsureOrganization(organizationId);
        if (systemId == Guid.Empty || string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 80
            || request.HierarchyOrder <= 0 || request.MinTrainingDays < 0 || request.MinLessons < 0
            || request.MinAge < 0 || request.MinAttendancePercentage is < 0 or > 100)
        {
            throw new ArgumentException(SystemMessages.LevelFieldsAreInvalidOrderMustBePositiveAndCriteriaNonNegative);
        }

        var level = await repository.CreateGraduationLevelAsync(organizationId, new GraduationLevel
        {
            GraduationSystemId = systemId,
            Name = request.Name.Trim(),
            HierarchyOrder = request.HierarchyOrder,
            MinTrainingDays = request.MinTrainingDays,
            MinAttendancePercentage = request.MinAttendancePercentage,
            MinLessons = request.MinLessons,
            MinAge = request.MinAge,
            RequiresManualEvaluation = request.RequiresManualEvaluation
        }, cancellationToken);
        return level is null ? null : ToResponse(level);
    }

    public async Task<IReadOnlyList<GraduationLevelResponse>> ListLevelsAsync(
        Guid organizationId,
        Guid systemId,
        CancellationToken cancellationToken)
    {
        EnsureOrganization(organizationId);
        if (systemId == Guid.Empty)
        {
            throw new ArgumentException(SystemMessages.GraduationSystemIdIsRequired);
        }

        var levels = await repository.ListGraduationLevelsAsync(organizationId, systemId, cancellationToken);
        return levels.Select(ToResponse).ToList();
    }

    public async Task<ClientGraduationResponse?> SetInitialAsync(
        Guid organizationId,
        Guid clientId,
        SetClientGraduationRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty || clientId == Guid.Empty || request.ModalityId == Guid.Empty
            || request.LevelId == Guid.Empty || request.GrantedAt == default || request.Notes?.Length > 255)
        {
            throw new ArgumentException(SystemMessages.GraduationDataIsInvalid);
        }

        var graduation = await repository.SetClientGraduationAsync(new ClientGraduation
        {
            OrganizationId = organizationId,
            ClientId = clientId,
            ModalityId = request.ModalityId,
            CurrentLevelId = request.LevelId,
            GrantedAt = request.GrantedAt,
            GrantedByTeacherId = request.TeacherId,
            Notes = Normalize(request.Notes)
        }, cancellationToken);
        return graduation is null ? null : new ClientGraduationResponse
        {
            Id = graduation.Id,
            OrganizationId = graduation.OrganizationId,
            ClientId = graduation.ClientId,
            ModalityId = graduation.ModalityId,
            CurrentLevelId = graduation.CurrentLevelId,
            GrantedAt = graduation.GrantedAt,
            GrantedByTeacherId = graduation.GrantedByTeacherId,
            Notes = graduation.Notes
        };
    }

    public async Task<GraduationEventResponse?> PromoteAsync(
        Guid organizationId,
        Guid clientId,
        PromoteClientRequest request,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty || clientId == Guid.Empty || request.ModalityId == Guid.Empty
            || request.ToLevelId == Guid.Empty || request.EventDate == default || request.Justification?.Length > 255)
        {
            throw new ArgumentException(SystemMessages.PromotionDataIsInvalid);
        }

        var result = await repository.PromoteClientAsync(new GraduationEvent
        {
            OrganizationId = organizationId,
            ClientId = clientId,
            ModalityId = request.ModalityId,
            ToLevelId = request.ToLevelId,
            EventDate = request.EventDate,
            TeacherId = request.TeacherId,
            Justification = Normalize(request.Justification)
        }, request.ManualEvaluationPassed, cancellationToken);
        return result is null ? null : ToResponse(result);
    }

    public async Task<IReadOnlyList<GraduationEventResponse>> ListEventsAsync(
        Guid organizationId,
        Guid clientId,
        Guid modalityId,
        CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty || clientId == Guid.Empty || modalityId == Guid.Empty)
        {
            throw new ArgumentException(SystemMessages.OrganizationIdClientIdAndModalityIdAreRequired);
        }

        var events = await repository.ListClientGraduationEventsAsync(organizationId, clientId, modalityId, cancellationToken);
        return events.Select(ToResponse).ToList();
    }

    public async Task<IReadOnlyList<EligibleClientResponse>> ListEligibleAsync(
        Guid organizationId,
        Guid systemId,
        CancellationToken cancellationToken)
    {
        EnsureOrganization(organizationId);
        if (systemId == Guid.Empty)
        {
            throw new ArgumentException(SystemMessages.GraduationSystemIdIsRequired);
        }

        var clients = await repository.ListEligibleClientsAsync(organizationId, systemId, cancellationToken);
        return clients.Select(client => new EligibleClientResponse
        {
            ClientId = client.ClientId,
            FullName = client.FullName,
            CurrentLevelId = client.CurrentLevelId,
            NextLevelId = client.NextLevelId,
            AttendanceCount = client.AttendanceCount,
            AttendancePercentage = client.AttendancePercentage,
            TrainingDays = client.TrainingDays
        }).ToList();
    }

    private static void EnsureOrganization(Guid organizationId)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException(SystemMessages.OrganizationIdIsRequired);
        }
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void ValidateBeltRequest(Guid systemId, CreateGraduationBeltRequest request)
    {
        if (systemId == Guid.Empty || string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 80
            || request.DegreeCount is < 0 or > 99 || request.MinLessons < 0 || request.MinTrainingDays < 0
            || request.MinAttendancePercentage is < 0 or > 100)
        {
            throw new ArgumentException(SystemMessages.OsDadosDaFaixaSaoInvalidosInformeONomeAte99GrausECriteriosNaoNegativos);
        }
    }

    private static void ValidateDegreeRequest(Guid systemId, Guid beltId, CreateGraduationDegreeRequest request)
    {
        if (systemId == Guid.Empty || beltId == Guid.Empty || request.MinLessons < 0
            || request.MinTrainingDays < 0 || request.MinAge < 0 || request.MinAttendancePercentage is < 0 or > 100)
        {
            throw new ArgumentException(SystemMessages.OsCriteriosDePromocaoDoGrauSaoInvalidos);
        }
    }

    private static GraduationSystemResponse ToResponse(GraduationSystem value) => new()
    {
        Id = value.Id,
        OrganizationId = value.OrganizationId,
        ModalityId = value.ModalityId,
        Name = value.Name,
        Description = value.Description,
        IsActive = value.IsActive,
        CreatedAt = value.CreatedAt
    };

    private static GraduationBeltResponse ToResponse(GraduationBelt value) => new()
    {
        Id = value.Id,
        GraduationSystemId = value.GraduationSystemId,
        Name = value.Name,
        HierarchyOrder = value.HierarchyOrder,
        DegreeCount = value.DegreeCount,
        MinLessons = value.MinLessons,
        MinTrainingDays = value.MinTrainingDays,
        MinAttendancePercentage = value.MinAttendancePercentage,
        RequiresManualEvaluation = value.RequiresManualEvaluation
    };

    private static GraduationLevelResponse ToResponse(GraduationLevel value) => new()
    {
        Id = value.Id,
        GraduationSystemId = value.GraduationSystemId,
        BeltId = value.BeltId,
        BeltName = value.BeltName,
        DegreeNumber = value.DegreeNumber,
        Name = value.Name,
        HierarchyOrder = value.HierarchyOrder,
        MinTrainingDays = value.MinTrainingDays,
        MinAttendancePercentage = value.MinAttendancePercentage,
        MinLessons = value.MinLessons,
        MinAge = value.MinAge,
        RequiresManualEvaluation = value.RequiresManualEvaluation
    };

    private static GraduationEventResponse ToResponse(GraduationEvent value) => new()
    {
        Id = value.Id,
        OrganizationId = value.OrganizationId,
        ClientId = value.ClientId,
        ModalityId = value.ModalityId,
        FromLevelId = value.FromLevelId,
        ToLevelId = value.ToLevelId,
        EventType = value.EventType,
        EventDate = value.EventDate,
        TeacherId = value.TeacherId,
        Justification = value.Justification
    };
}