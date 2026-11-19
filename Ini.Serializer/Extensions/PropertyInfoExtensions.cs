using Oeeng.Ini.Serializer.Attributes;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Oeeng.Ini.Serializer.Extensions;

/// <summary>
/// Provides helper methods for reading template metadata from <see cref="PropertyInfo"/>.
/// </summary>
internal static class PropertyInfoExtensions
{
    /// <summary>
    /// Determines whether the property is annotated with <see cref="TemplateAttribute"/>.
    /// </summary>
    /// <param name="prop">The property to inspect.</param>
    /// <returns><see langword="true"/> when the property is templated.</returns>
    internal static bool IsTemplatedProperty(this PropertyInfo prop)
    {
        return prop.GetCustomAttribute<TemplateAttribute>() is not null;
    }

    /// <summary>
    /// Gets the placeholder value for a templated property.
    /// </summary>
    /// <param name="prop">The property to inspect.</param>
    /// <returns>The configured placeholder or the default <c>%PropertyName%</c> format.</returns>
    internal static string GetTemplatePlaceholder(this PropertyInfo prop)
    {
        var placeholder = prop.GetCustomAttribute<TemplateAttribute>()?.Placeholder ?? string.Empty;
        placeholder = string.IsNullOrWhiteSpace(placeholder) ? string.Format("%{0}%", prop.Name) : $" {placeholder}";
        return placeholder;
    }
}
