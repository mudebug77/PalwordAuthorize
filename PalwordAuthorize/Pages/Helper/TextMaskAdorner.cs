using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ConsoleApp.Pages
{
    public class TextMask
    {
        public static readonly DependencyProperty MaskLengthProperty = DependencyProperty.RegisterAttached("MaskLength", typeof(int), typeof(TextMask), new PropertyMetadata(0));

        public static int GetMaskLength(DependencyObject obj)
        {
            return (int)obj.GetValue(MaskLengthProperty);
        }

        public static void SetMaskLength(DependencyObject obj, int value)
        {
            obj.SetValue(MaskLengthProperty, value);
        }
    }

    public class MaskedTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string text = value as string;
            int maskLength = System.Convert.ToInt32(parameter);

            if (!string.IsNullOrEmpty(text) && text.Length > maskLength)
            {
                string maskedText = new string('*', text.Length - maskLength) + text.Substring(text.Length - maskLength);
                return maskedText;
            }

            return text;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
