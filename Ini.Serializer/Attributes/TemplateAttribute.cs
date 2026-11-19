using System;
using System.Collections.Generic;
using System.Text;

namespace Oeeng.Ini.Serializer.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class TemplateAttribute : Attribute
{
    public string Placeholder { set; get; } = string.Empty;
}
