using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace MergePDF
{
    class StatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType == typeof(Brush))
            {
                return ConvertToColor(value);
            }
            else if (targetType == typeof(string))
            {
                return ConvertToString(value);
            }
            else
            {
                return Binding.DoNothing;
            }
        }

        private static object ConvertToColor(object value)
        {
            switch ((InputFileStatus)value)
            {
                case InputFileStatus.Pending:
                    return Brushes.Black;
                case InputFileStatus.OK:
                    return Brushes.Green;
                case InputFileStatus.Error:
                    return Brushes.Red;
                default:
                    return Brushes.Black;
            }
        }

        private static object ConvertToString(object value)
        {
            switch ((InputFileStatus)value)
            {
                case InputFileStatus.Pending:
                    return Properties.Resources.InputStatusPending;
                case InputFileStatus.OK:
                    return Properties.Resources.InputStatusOK;
                case InputFileStatus.Error:
                    return Properties.Resources.InputStatusError;
                default:
                    return string.Empty;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
