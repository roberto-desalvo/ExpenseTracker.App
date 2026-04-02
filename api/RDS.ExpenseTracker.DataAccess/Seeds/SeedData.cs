using RDS.ExpenseTracker.DataAccess.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDS.ExpenseTracker.DataAccess.Seeds
{
    public static class SeedData
    {
        public static IEnumerable<Category> GetSeedCategories()
        {
            yield return CategoryBuilder.Create()
                .WithName("Default").WithDescription("Other").WithTags("default").WithIsDefault(true)
                .Build();

            yield return CategoryBuilder.Create()
                .WithName("Money tranfers").WithDescription("Money transfers")
                .Build();

            yield return CategoryBuilder.Create()
                .WithName("Work incomes").WithDescription("Salary or other work incomes")
                .Build();

            yield return CategoryBuilder.Create()
                .WithName("Housing").WithDescription("Rent, utilities, home maintenance, etc")
                .Build();

            yield return CategoryBuilder.Create()
                .WithName("Health & Fitness").WithDescription("Health expenses, gym, sports, etc.")
                .Build();

            yield return CategoryBuilder.Create()
                .WithName("Food and bevarage").WithDescription("Food and bevarage")
                .Build();

            yield return CategoryBuilder.Create()
                .WithName("Transportation").WithDescription("Transportation, car maintenance and insurance, fuel, etc.")
                .Build();

            yield return CategoryBuilder.Create()
                .WithName("Entertainment").WithDescription("Entertainment")
                .Build();

            yield return CategoryBuilder.Create()
                .WithName("Clothes").WithDescription("Clothes and accessories")
                .Build();

            yield return CategoryBuilder.Create()
                .WithName("Savings and investments").WithDescription("Savings and investments")
                .Build();            

            yield return CategoryBuilder.Create()
                .WithName("Gifts").WithDescription("Gifts")
                .Build();

        }

        private class CategoryBuilder
        {
            private readonly Category category;

            protected CategoryBuilder()
            {
                category = new Category();
            }

            public static CategoryBuilder Create() => new();

            public CategoryBuilder WithId(int id)
            {
                category.Id = id;
                return this;
            }

            public CategoryBuilder WithName(string name)
            {
                category.Name = name;
                return this;
            }

            public CategoryBuilder WithDescription(string description)
            {
                category.Description = description;
                return this;
            }

            public CategoryBuilder WithTags(string tags)
            {
                category.Tags = tags;
                return this;
            }

            public CategoryBuilder WithIsDefault(bool isDefault)
            {
                category.IsDefault = isDefault;
                return this;
            }

            public Category Build() => category;

        }
    }
}
