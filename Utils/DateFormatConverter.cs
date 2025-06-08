using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace BlackSync.Views
{
    public class DateFormatConverter : IValueConverter
    {
        // Converte a data para o formato desejado
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime date)
            {
                return date.ToString("dd/MM/yyyy");
            }
            return value;
        }

        // Não será necessário implementar o ConvertBack para este caso
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
