using grzyClothTool.Helpers;
using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace grzyClothTool.Converters;

/// <summary>
/// Formats a binding value with a translated template. It supports both a
/// regular binding and a MultiBinding (which is passed as object[]).
/// </summary>
public sealed class LocalizedFormatConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter is not string template)
        {
            return value;
        }

        object?[] arguments = value is object[] values
            ? values.Cast<object?>().ToArray()
            : [value];

        return LocalizationHelper.Format(template, arguments);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
