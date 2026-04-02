using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RDS.ExpenseTracker.DataAccess;
using RDS.ExpenseTracker.DataAccess.Entities;
using RDS.ExpenseTracker.DataAccess.Seeds;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDS.ExpenseTracker.Business.Extensions
{
    public static class ServiceProviderExtensions
    {
        public static void AddSeedData(this IServiceProvider service)
        {
            using var scope = service.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ExpenseTrackerContext>();

            if (!context.Set<Category>().AsNoTracking().Any())
            {
                var seedCategories = SeedData.GetSeedCategories();
                context.Set<Category>().AddRange(seedCategories);
                context.SaveChanges();
            }
        }
    }
}
