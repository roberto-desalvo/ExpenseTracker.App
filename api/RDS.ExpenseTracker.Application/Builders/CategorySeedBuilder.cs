using RDS.ExpenseTracker.Domain.Entities;
using RDS.ExpenseTracker.Domain.Enums;

namespace RDS.ExpenseTracker.Application.Builders;

public static class CategorySeedBuilder
{
    public static Category Build(CategoryEnum categoryEnum, int userId)
    {
        return categoryEnum switch
        {
            CategoryEnum.Default => Create(categoryEnum, userId, "Default", "Other", "default", true),
            CategoryEnum.MoneyTransfers => Create(categoryEnum, userId, "Money tranfers", "Money transfers"),
            CategoryEnum.WorkIncomes => Create(categoryEnum, userId, "Work incomes", "Salary or other work incomes"),
            CategoryEnum.Housing => Create(categoryEnum, userId, "Housing", "Rent, utilities, home maintenance, etc"),
            CategoryEnum.HealthAndFitness => Create(categoryEnum, userId, "Health & Fitness", "Health expenses, gym, sports, etc."),
            CategoryEnum.FoodAndBeverage => Create(categoryEnum, userId, "Food and bevarage", "Food and bevarage"),
            CategoryEnum.Transportation => Create(categoryEnum, userId, "Transportation", "Transportation, car maintenance and insurance, fuel, etc."),
            CategoryEnum.Entertainment => Create(categoryEnum, userId, "Entertainment", "Entertainment"),
            CategoryEnum.Clothes => Create(categoryEnum, userId, "Clothes", "Clothes and accessories"),
            CategoryEnum.SavingsAndInvestments => Create(categoryEnum, userId, "Savings and investments", "Savings and investments"),
            CategoryEnum.Gifts => Create(categoryEnum, userId, "Gifts", "Gifts"),
            _ => throw new ArgumentOutOfRangeException(nameof(categoryEnum), categoryEnum, "Unsupported category enum value")
        };
    }

    private static Category Create(
        CategoryEnum categoryEnum,
        int userId,
        string name,
        string description,
        string? tags = null,
        bool? isDefault = null)
    {
        return new Category
        {
            Name = name,
            Description = description,
            Tags = tags,
            IsDefault = isDefault,
            UserId = userId,
            Kind = categoryEnum
        };
    }
}
