using System;

namespace Ini.Serializer.Converters;

public interface IIniValueConverter
{
    bool CanConvert(Type targetType);
    string Serialize(object? value, Type declaredType, IniSerializer serializer);
    object? Deserialize(string rawValue, Type targetType, IniSerializer serializer);
}
