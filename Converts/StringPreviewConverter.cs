using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;
using System.Globalization;

namespace raindesk.Converts;

public sealed class StringPreviewConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        string text = value?.ToString() ?? string.Empty;
        int limit = int.TryParse(parameter?.ToString(), out int parsed) && parsed > 1 ? parsed : 5;
        StringInfo info = new(text);
        if (info.LengthInTextElements <= limit)
        {
            return text;
        }

        return info.SubstringByTextElements(0, limit);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        DependencyProperty.UnsetValue;
}
