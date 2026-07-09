using KiaiControl.Core.Interfaces;
using KiaiControl.Repositories.Implementations;
using KiaiControl.UseCases.Attendance;
using KiaiControl.UseCases.Billing;
using KiaiControl.UseCases.Clients;
using KiaiControl.UseCases.Teachers;
using Microsoft.Extensions.DependencyInjection;

namespace KiaiControl.UseCases.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKiaiUseCases(this IServiceCollection services)
    {
        services.AddScoped<IClientRepository, InMemoryClientRepository>();
        services.AddScoped<ITeacherRepository, InMemoryTeacherRepository>();
        services.AddScoped<IAttendanceRepository, InMemoryAttendanceRepository>();
        services.AddScoped<IBillingRepository, InMemoryBillingRepository>();

        services.AddScoped<ClientUseCase>();
        services.AddScoped<TeacherUseCase>();
        services.AddScoped<AttendanceUseCase>();
        services.AddScoped<BillingUseCase>();

        return services;
    }
}
