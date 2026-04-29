using System.ComponentModel;
using System.Reflection;

namespace RDS.ExpenseTracker.Infrastructure.Utilities;

public static class EnumHelper
{
    public static string GetDescription<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        var field = typeof(TEnum).GetField(value.ToString());
        if (field is null)
        {
            return value.ToString();
        }

        var attribute = field.GetCustomAttribute<DescriptionAttribute>();
        return attribute?.Description ?? value.ToString();
    }
}

public static class EnumExtensions
{
    public static string GetDescription<TEnum>(this TEnum value)
        where TEnum : struct, Enum
    {
        return EnumHelper.GetDescription(value);
    }
}
