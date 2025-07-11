using System;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace Kosync.Extensions;

public static class ConfigurationExtensions
{
    /// <summary>
    /// Gets a configuration sub-section with the specified key and binds it to the specified type.
    /// Throws an exception if the section doesn't exist or is empty.
    /// </summary>
    /// <typeparam name="T">The type to bind the configuration section to</typeparam>
    /// <param name="configuration">The configuration instance</param>
    /// <param name="sectionName">The name of the configuration section</param>
    /// <returns>The bound configuration object</returns>
    /// <exception cref="InvalidOperationException">Thrown when the section is missing or binding fails</exception>
    private static T GetRequiredSection<T>(this IConfiguration configuration, string sectionName)
        where T : class
    {
        if (string.IsNullOrEmpty(sectionName))
            throw new ArgumentNullException(nameof(sectionName));

        IConfigurationSection section = configuration.GetSection(sectionName);

        if (!section.Exists())
            throw new InvalidOperationException($"Configuration section '{sectionName}' is missing");

        T? result = section.Get<T>();

        if (result == null)
            throw new InvalidOperationException(
                $"Unable to bind configuration section '{sectionName}' to type '{typeof(T).Name}'"
            );

        return result;
    }

    /// <summary>
    /// Gets a configuration sub-section using the section name from the type's SectionName constant/property.
    /// Throws an exception if the section doesn't exist or is empty.
    /// </summary>
    /// <typeparam name="T">The type to bind the configuration section to (must have a SectionName constant/property)</typeparam>
    /// <param name="configuration">The configuration instance</param>
    /// <returns>The bound configuration object</returns>
    /// <exception cref="InvalidOperationException">Thrown when the section is missing, SectionName is not found, or binding fails</exception>
    public static T GetRequiredSection<T>(this IConfiguration configuration)
        where T : class
    {
        // Try to get SectionName from the type
        FieldInfo? sectionNameField = typeof(T).GetField("SectionName", BindingFlags.Public | BindingFlags.Static);
        PropertyInfo? sectionNameProperty = typeof(T).GetProperty(
            "SectionName",
            BindingFlags.Public | BindingFlags.Static
        );

        string? sectionName = null;

        if (sectionNameField != null && sectionNameField.FieldType == typeof(string))
        {
            sectionName = sectionNameField.GetValue(null) as string;
        }
        else if (sectionNameProperty != null && sectionNameProperty.PropertyType == typeof(string))
        {
            sectionName = sectionNameProperty.GetValue(null) as string;
        }

        if (string.IsNullOrEmpty(sectionName))
            throw new InvalidOperationException(
                $"Type '{typeof(T).Name}' must have a public static 'SectionName' field or property"
            );

        return configuration.GetRequiredSection<T>(sectionName);
    }
}
