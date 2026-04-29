using Microsoft.Extensions.DependencyInjection;
using RDS.ExpenseTracker.Application.Services;
using RDS.ExpenseTracker.Application.Architecture;
using RDS.ExpenseTracker.Domain.Common;
using System.Reflection;

namespace RDS.ExpenseTracker.Application.Extensions
{
    public static class AddServicesExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            var assembly = typeof(ApplicationAssemblyMarker).Assembly;

            services.AddAutoMapper(assembly);
            RegisterServicesFromAssembly(services, assembly);

            return services;
        }

        private static void RegisterServicesFromAssembly(IServiceCollection services, Assembly assembly)
        {
            var serviceInterfaces = typeof(IService).Assembly.GetTypes()
                .Where(t =>
                    t is { IsInterface: true, IsAbstract: false } &&
                    typeof(IService).IsAssignableFrom(t) &&
                    t != typeof(IService));

            foreach (var serviceInterface in serviceInterfaces)
            {
                var implementation = assembly.GetTypes()
                    .FirstOrDefault(t =>
                        t is { IsClass: true, IsAbstract: false } &&
                        serviceInterface.IsAssignableFrom(t));

                if (implementation is not null)
                {
                    services.AddScoped(serviceInterface, implementation);
                }
            }
        }
    }
}
