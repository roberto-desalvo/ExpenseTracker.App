using RDS.ExpenseTracker.Domain.Common;
using System.Reflection;

namespace RDS.ExpenseTracker.Api.Configuration;

public static class OptionsRegistrationExtensions
{
    private static readonly MethodInfo ConfigureTypedOptionsMethod =
        typeof(OptionsRegistrationExtensions)
            .GetMethod(nameof(ConfigureTypedOptions), BindingFlags.NonPublic | BindingFlags.Static)!;

    public static IServiceCollection AddOptions(this IServiceCollection services, IConfiguration configuration)
    {
        var optionTypes = typeof(IAppOptions).Assembly
            .GetTypes()
            .Where(t =>
                t is { IsClass: true, IsAbstract: false } &&
                typeof(IAppOptions).IsAssignableFrom(t) &&
                t.GetConstructor(Type.EmptyTypes) is not null)
            .ToList();

        foreach (var optionType in optionTypes)
        {
            var instance = (IAppOptions)Activator.CreateInstance(optionType)!;
            var section = configuration.GetSection(instance.SectionName);
            ConfigureTypedOptionsMethod
                .MakeGenericMethod(optionType)
                .Invoke(null, [services, section]);
        }

        return services;
    }

    public static T BindFromSectionName<T>(this IConfiguration configuration)
        where T : class, IAppOptions, new()
    {
        var sectionName = configuration.GetSectionName<T>();
        var options = new T();
        configuration.GetSection(sectionName).Bind(options);
        return options;
    }

    public static string GetSectionName<T>(this IConfiguration _)
        where T : class, IAppOptions, new()
    {
        var options = new T();
        return options.SectionName;
    }

    private static void ConfigureTypedOptions<T>(IServiceCollection services, IConfigurationSection section)
        where T : class
    {
        services.Configure<T>(section);
    }
}