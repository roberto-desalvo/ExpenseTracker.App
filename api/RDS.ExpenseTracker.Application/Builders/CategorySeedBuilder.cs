using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Enums;

namespace RDS.ExpenseTracker.Application.Builders;

public static class CategorySeedBuilder
{
    public static Category Build(CategoryEnum categoryEnum)
    {
        return categoryEnum switch
        {
            CategoryEnum.Default => Create(categoryEnum, "Default", "Other", "default", true),
            CategoryEnum.MoneyTransfers => Create(categoryEnum, "Money tranfers", "Money transfers"),
            CategoryEnum.WorkIncomes => Create(categoryEnum, "Work incomes", "Salary or other work incomes"),
            CategoryEnum.Housing => Create(categoryEnum, "Housing", "Rent, utilities, home maintenance, etc"),
            CategoryEnum.HealthAndFitness => Create(categoryEnum, "Health & Fitness", "Health expenses, gym, sports, etc."),
            CategoryEnum.FoodAndBeverage => Create(categoryEnum, "Food and bevarage", "Food and bevarage"),
            CategoryEnum.Transportation => Create(categoryEnum, "Transportation", "Transportation, car maintenance and insurance, fuel, etc."),
            CategoryEnum.Entertainment => Create(categoryEnum, "Entertainment", "Entertainment"),
            CategoryEnum.Clothes => Create(categoryEnum, "Clothes", "Clothes and accessories"),
            CategoryEnum.SavingsAndInvestments => Create(categoryEnum, "Savings and investments", "Savings and investments"),
            CategoryEnum.Gifts => Create(categoryEnum, "Gifts", "Gifts"),
            _ => throw new ArgumentOutOfRangeException(nameof(categoryEnum), categoryEnum, "Unsupported category enum value")
        };
    }

    private static Category Create(
        CategoryEnum categoryEnum,
        string name,
        string description,
        string? tags = null,
        bool? isDefault = null)
    {
        return new Category
        {
            Id = (int)categoryEnum,
            Name = name,
            Description = description,
            Tags = tags,
            IsDefault = isDefault
        };
    }
}
