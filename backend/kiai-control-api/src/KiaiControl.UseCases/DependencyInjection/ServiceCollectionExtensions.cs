using KiaiControl.UseCases.Attendance;
using KiaiControl.UseCases.Auth;
using KiaiControl.UseCases.Billing;
using KiaiControl.UseCases.Branches;
using KiaiControl.UseCases.Lessons;
using KiaiControl.UseCases.Clients;
using KiaiControl.UseCases.Modalities;
using KiaiControl.UseCases.Organizations;
using KiaiControl.UseCases.Notifications;
using KiaiControl.UseCases.Audit;
using KiaiControl.UseCases.Graduation;
using KiaiControl.UseCases.Fitness;
using KiaiControl.UseCases.Ranking;
using KiaiControl.UseCases.Reports;
using KiaiControl.UseCases.Teachers;
using Microsoft.Extensions.DependencyInjection;

namespace KiaiControl.UseCases.DependencyInjection;
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKiaiUseCases(this IServiceCollection services)
    {
        services.AddScoped<AuthUseCase>();
        services.AddScoped<KiaiControl.UseCases.Administration.AdministrationUseCase>();
        services.AddScoped<KiaiControl.UseCases.Catalog.CatalogUseCase>();
        services.AddScoped<AccessControlUseCase>();
        services.AddScoped<ClientUseCase>();
        services.AddScoped<ClientJourneyUseCase>();
        services.AddScoped<TeacherUseCase>();
        services.AddScoped<ModalityUseCase>();
        services.AddScoped<ModalityScheduleUseCase>();
        services.AddScoped<LessonUseCase>();
        services.AddScoped<AttendanceUseCase>();
        services.AddScoped<BillingUseCase>();
        services.AddScoped<BillingCatalogUseCase>();
        services.AddScoped<OrganizationUseCase>();
        services.AddScoped<BranchUseCase>();
        services.AddScoped<NotificationUseCase>();
        services.AddScoped<AuditLogUseCase>();
        services.AddScoped<GraduationUseCase>();
        services.AddScoped<FitnessUseCase>();
        services.AddScoped<RankingUseCase>();
        services.AddScoped<ReportUseCase>();
        return services;
    }
}
