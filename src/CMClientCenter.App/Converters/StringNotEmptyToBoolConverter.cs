using Microsoft.UI.Xaml.Data;

namespace CMClientCenter.App.Converters;

// Prueft, ob ein String nicht null/leer ist. Verwendet u.a. fuer den
// "Install"-Button auf der Updates-Page: aktiv nur, wenn InstallableUpdateId
// gesetzt ist (= Title/Article-Abgleich gegen CCM_SoftwareUpdate war erfolgreich,
// siehe Get-CCMSoftwareUpdates.ps1).
public sealed class StringNotEmptyToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => !string.IsNullOrEmpty(value as string);

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}
