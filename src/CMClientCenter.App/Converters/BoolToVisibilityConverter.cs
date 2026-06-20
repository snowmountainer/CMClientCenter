using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace CMClientCenter.App.Converters;

// Minimaler bool→Visibility Converter. x:Bind kann bool nicht implizit nach
// Visibility konvertieren, sobald eine Methode (statt direkter Property)
// im Spiel ist; ein klassischer IValueConverter ist hier der robusteste Weg.
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var flag = value is bool b && b;
        return flag ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}