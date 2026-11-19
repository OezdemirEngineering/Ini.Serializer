using System;
using System.Collections.Generic;
using System.Text;

namespace Oeeng.Ini.Serializer.Attributes;

/// <summary>
/// Marks a property as a template placeholder field during INI serialization and deserialization.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class TemplateAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the placeholder token to emit instead of the real value.
    /// When empty, the default format <c>%PropertyName%</c> is used.
    /// </summary>
    public string Placeholder { set; get; } = string.Empty;
}
