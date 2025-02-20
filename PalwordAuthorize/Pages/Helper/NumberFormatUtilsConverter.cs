using OfficeOpenXml.FormulaParsing.Excel.Functions.Database;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ConsoleApp.Pages
{
    public class NumberFormatUtilsConverter : IValueConverter
    {
        protected string GetDoubleString(double num)
        {
            string formattedString = num.ToString("F2");
            // 如果最后一位是零，则删除
            if (formattedString.EndsWith("0"))
            {
                formattedString = formattedString.Substring(0, formattedString.Length - 1);
            }

            // 如果最后两位是小数点加零，则删除
            if (formattedString.EndsWith(".0"))
            {
                formattedString = formattedString.Substring(0, formattedString.Length - 2);
            }
            return formattedString;
        }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double.TryParse(value.ToString(), out double number);
            if (number > 100000000)
            {
                double num = number / 100000000;
                return GetDoubleString(num) + "億";
            }
            else if (number > 10000000)
            {
                double num = number / 10000000;
                return GetDoubleString(num) + "千萬";
            }
            else if (number > 1000000)
            {
                double num = number / 1000000;
                return GetDoubleString(num) + "百萬";
            }
            else if (number > 10000)
            {
                double num = number / 10000;
                return GetDoubleString(num) + "萬";
            }
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NumberFormatColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double.TryParse(value.ToString(), out double number);

            if (number == 0)
            {
                return "#000000";
            }

            if (number > 100000000)//億
            {
                return "#EE6363";//紅色
            }
            else if (number > 10000000)//千萬
            {
                return "#FF1493";//紫色
            }
            else if (number > 1000000)//百萬
            {
                return "#006400";//深綠色
            }
            else if (number > 10000)//萬
            {
                return "#3CB371";//綠
            }
            return "#000000";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
