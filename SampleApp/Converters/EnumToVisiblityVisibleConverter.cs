using System;
using Windows.UI.Xaml.Data;

namespace SampleApp
{
    public sealed class EnumToVisiblityVisibleConverter : IValueConverter 
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, string language)
        {
            return value.ToString().Equals(parameter as string) ? Windows.UI.Xaml.Visibility.Visible : Windows.UI.Xaml.Visibility.Collapsed;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
