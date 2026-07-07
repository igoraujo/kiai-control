using Microsoft.Extensions.DependencyInjection;

namespace KiaiControl.UseCases.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKiaiUseCases(this IServiceCollection services)
    {
        return services;
    }
}
