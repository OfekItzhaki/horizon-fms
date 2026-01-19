using System.Globalization;
using System.Windows.Data;

namespace FileManagementSystem.Presentation.Converters;

public class TagsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is List<string> tags && tags.Any())
        {
            return string.Join(", ", tags);
        }
        return string.Empty;
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str && !string.IsNullOrWhiteSpace(str))
        {
            return str.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .ToList();
        }
        return new List<string>();
    }
}
