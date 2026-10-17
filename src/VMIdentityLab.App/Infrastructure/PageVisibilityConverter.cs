using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace VMIdentityLab.App.Infrastructure;

public sealed class PageVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is int page && parameter is string index && int.TryParse(index, out var expected) && page == expected ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
