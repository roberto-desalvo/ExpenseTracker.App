using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RDS.ExpenseTracker.Domain.Common;
using RDS.ExpenseTracker.Domain.Repositories;
using RDS.ExpenseTracker.Infrastructure.EFCore;
using RDS.ExpenseTracker.Infrastructure.Repositories;
using System.Reflection;

namespace RDS.ExpenseTracker.Infrastructure
{
    public static class AddServicesExtensions
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, string connectionString)
        {
            services.AddDbContext<ExpenseTrackerContext>(optBuilder =>
            {
                optBuilder.UseSqlServer(connectionString, sqlServerBuilder => sqlServerBuilder.EnableRetryOnFailure());
            });

            RegisterRepositoriesFromAssembly(services, typeof(InfrastructureAssemblyMarker).Assembly);
            return services;
        }

        private static void RegisterRepositoriesFromAssembly(IServiceCollection services, Assembly assembly)
        {
            var repositoryInterfaces = typeof(IRepository).Assembly.GetTypes()
                .Where(t =>
                    t is { IsInterface: true, IsAbstract: false } &&
                    typeof(IRepository).IsAssignableFrom(t) &&
                    t != typeof(IRepository) &&
                    t != typeof(IRepositoryBase));

            foreach (var repositoryInterface in repositoryInterfaces)
            {
                var implementation = assembly.GetTypes()
                    .FirstOrDefault(t =>
                        t is { IsClass: true, IsAbstract: false } &&
                        repositoryInterface.IsAssignableFrom(t));

                if (implementation is not null)
                {
                    services.AddScoped(repositoryInterface, implementation);
                }
            }
        }
    }
}
