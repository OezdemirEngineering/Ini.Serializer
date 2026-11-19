using Oeeng.Ini.Serializer.Attributes;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Oeeng.Ini.Serializer.Extensions;

internal static class PropertyInfoExtensions
{
    internal static bool IsTemplatedProperty(this PropertyInfo prop)
    {
        return prop.GetCustomAttribute<TemplateAttribute>() is not null;
    }

    internal static string GetTemplatePlaceholder(this PropertyInfo prop)
    {
        var placeholder = prop.GetCustomAttribute<TemplateAttribute>()?.Placeholder ?? string.Empty;
        placeholder = string.IsNullOrWhiteSpace(placeholder) ? string.Format("%{0}%", prop.Name) : $" {placeholder}";
        return placeholder;
    }
}
