using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace UIConsole.Pages
{
    public static class UI2ConfigHelper
    {
        public static bool HandleUiElementChangedEvent(object uiElement, object obj)
        {
            var box = uiElement as TextBox;
            if (box != null)
            {
                var propName = box.Name.Substring(2);
                return SetValueByName(propName, box.Text, obj);
            }
            var chB = uiElement as CheckBox;
            if (chB != null)
            {
                var propName = chB.Name.Substring(2);
                return SetValueByName(propName, chB.IsChecked, obj);
            }
            var passBox = uiElement as PasswordBox;
            if (passBox != null)
            {
                var propName = passBox.Name.Substring(2);
                return SetValueByName(propName, passBox.Password, obj);
            }
            var comboBox = uiElement as ComboBox;
            if (comboBox != null)
            {
                var propName = comboBox.Name.Substring(2);
                if (!string.IsNullOrEmpty(comboBox.SelectedValuePath))
                {
                    var targetType = comboBox.SelectedItem.GetType();
                    var Property = targetType.GetProperty(comboBox.SelectedValuePath);
                    var v = Property.GetValue(comboBox.SelectedItem);
                    return SetValueByName(propName, v.ToString(), obj);
                }
                else
                {
                    return SetValueByName(propName, comboBox.SelectedItem, obj);
                }
            }
            var slider = uiElement as Slider;
            if (slider != null)
            {
                var propName = slider.Name.Substring(2);
                return SetValueByName(propName, slider.Value, obj);
            }
            return false;
        }

        public static List<T> GetLogicalChildCollection<T>(this UIElement parent) where T : DependencyObject
        {
            var logicalCollection = new List<T>();
            GetLogicalChildCollection(parent, logicalCollection);
            return logicalCollection;
        }

        public static void ConfigToGrid(Grid grid, object config)
        {
            foreach (var uiElem in grid.GetLogicalChildCollection<TextBox>())
            {
                string val;
                if (uiElem.Name.Length == 0) continue;
                if (GetValueByName(uiElem.Name.Substring(2), config, out val))
                    uiElem.Text = val;
            }

            foreach (var uiElem in grid.GetLogicalChildCollection<PasswordBox>())
            {
                string val;
                if (uiElem.Name.Length == 0) continue;
                if (GetValueByName(uiElem.Name.Substring(2), config, out val))
                    uiElem.Password = val;
            }

            foreach (var uiElem in grid.GetLogicalChildCollection<CheckBox>())
            {
                bool val;
                if (uiElem.Name.Length == 0) continue;
                if (GetValueByName(uiElem.Name.Substring(2), config, out val))
                    uiElem.IsChecked = val;
            }

            foreach (var uiElem in grid.GetLogicalChildCollection<ComboBox>())
            {

                Enum val;
                if (uiElem.Name.Length == 0) continue;
                if (GetValueByName(uiElem.Name.Substring(2), config, out val))
                {
                    var valType = val.GetType();
                    uiElem.ItemsSource = Enum.GetValues(valType);
                    uiElem.SelectedItem = val;
                }else
                {
                    string StringValue;
                    if (uiElem.Name.Length == 0) continue;
                    if (GetValueByName(uiElem.Name.Substring(2), config, out StringValue))
                    {
                        if (uiElem.ItemsSource == null)
                        {
                            uiElem.Items.Clear();
                            uiElem.Items.Add(StringValue);
                            uiElem.SelectedIndex = 0;
                        }
                        else
                        {
                            uiElem.SelectedValue = StringValue;
                        }
                    }
                }


            }

            foreach (var uiElem in grid.GetLogicalChildCollection<Slider>())
            {
                double val;
                if (uiElem.Name.Length == 0) continue;
                if (GetValueByName(uiElem.Name.Substring(2), config, out val))
                    uiElem.Value = val;
            }
        }

        private static void GetLogicalChildCollection<T>(DependencyObject parent, List<T> logicalCollection) where T : DependencyObject
        {
            IEnumerable children = LogicalTreeHelper.GetChildren(parent);
            foreach (object child in children)
            {
                if (child is DependencyObject)
                {
                    DependencyObject depChild = child as DependencyObject;
                    if (child is T)
                    {
                        logicalCollection.Add(child as T);
                    }
                    GetLogicalChildCollection(depChild, logicalCollection);
                }
            }
        }

        public static bool SetValueByName(string propertyName, object value, object obj)
        {
            if (!SetPropertyRecursive(propertyName, value, obj))
                return SetFieldRecursive(propertyName, value, obj);
            return true;
        }

        internal static bool IsMyInterface(this Type propertyType)
        {
            return propertyType.Assembly.GetName().Name != "mscorlib" && propertyType.Name != "Entity";
        }

        private static string Conversion(string str1, string str2)
        {

            if (str1.Contains(".") && (str2 != "."))
                return str1.Replace('.', ',');
            if (str1.Contains(",") && (str2 != ","))
                return str1.Replace(',', '.');
            return str1;
        }

        public static bool GetVal<T>(this string value, out T resultVal) where T : IConvertible
        {
            resultVal = default(T);
            if (resultVal == null) return false;
            var typeCode = resultVal.GetTypeCode();
            switch (typeCode)
            {
                case TypeCode.Double:
                    {
                        double result;
                        var nfi = NumberFormatInfo.CurrentInfo;
                        var currentDecimalSeparator = nfi.CurrencyDecimalSeparator;
                        value = Conversion(value, currentDecimalSeparator);
                        var res = double.TryParse(value, out result);
                        if (!res) return false;
                        var changeType = Convert.ChangeType(result, typeCode);
                        if (changeType != null)
                            resultVal = (T)changeType;
                        return true;
                    }
                case TypeCode.Single:
                    {
                        float result;
                        //var res = float.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out result)
                        //          || float.TryParse(value, NumberStyles.Any, CultureInfo.GetCultureInfo("en-US"), out result)
                        //          || float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
                        var nfi = NumberFormatInfo.CurrentInfo;
                        var currentDecimalSeparator = nfi.CurrencyDecimalSeparator;
                        value = Conversion(value, currentDecimalSeparator);
                        var res = float.TryParse(value, out result);
                        if (!res) return false;
                        var changeType = Convert.ChangeType(result, typeCode);
                        if (changeType != null)
                            resultVal = (T)changeType;
                        return true;
                    }
                case TypeCode.UInt32:
                case TypeCode.Int32:
                    {
                        ulong result;
                        var res = ulong.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out result)
                                  || ulong.TryParse(value, NumberStyles.Any, CultureInfo.GetCultureInfo("en-US"), out result)
                                  || ulong.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
                        if (!res) return false;
                        var changeType = Convert.ChangeType(result, typeCode);
                        if (changeType != null)
                            resultVal = (T)changeType;
                        return true;
                    }
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    {
                        ulong result;
                        var res = ulong.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out result)
                                  || ulong.TryParse(value, NumberStyles.Any, CultureInfo.GetCultureInfo("en-US"), out result)
                                  || ulong.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
                        if (!res) return false;
                        var changeType = Convert.ChangeType(result, typeCode);
                        if (changeType != null)
                            resultVal = (T)changeType;
                        return true;
                    }
            }
            return false;
        }

        private static bool SetPropertyRecursive(string propertyName, object value, object obj)
        {
            if (obj == null) return false;
            var objType = obj.GetType();
            if (CheckObservable(objType))
            {
                return false;
            }
            foreach (var property in objType.GetProperties())
            {
                if (property.PropertyType == objType) continue;
                if (property.PropertyType.IsClass && property.PropertyType.IsMyInterface())
                {
                    var nextObj = property.GetValue(obj);
                    if (SetPropertyRecursive(propertyName, value, nextObj))
                        return true;
                    if (SetFieldRecursive(propertyName, value, nextObj))
                        return true;
                }
                if (property.Name != propertyName) continue;
                
                if (property.PropertyType == typeof(int))
                {
                    int val;
                    if (value.ToString().GetVal(out val))
                        property.SetValue(obj, val);
                }
                else if (property.PropertyType == typeof(double))
                {
                    double val;
                    if (value.ToString().GetVal(out val))
                        property.SetValue(obj, val);
                }
                else if (property.PropertyType == typeof(float))
                {
                    float val;
                    if (value.ToString().GetVal(out val))
                        property.SetValue(obj, val);
                }
                else if (property.PropertyType == typeof(ulong))
                {
                    ulong val;
                    if (value.ToString().GetVal(out val))
                        property.SetValue(obj, val);
                }
                else if (property.PropertyType == typeof(long))
                {
                    long val;
                    if (value.ToString().GetVal(out val))
                        property.SetValue(obj, val);
                }
                else if (property.PropertyType == typeof(string))
                {
                    var val = value.ToString().Trim();
                    property.SetValue(obj, val);
                }
                else
                    property.SetValue(obj, value);
                return true;
            }
            return false;
        }

        private static bool SetFieldRecursive(string propertyName, object value, object obj)
        {
            if (obj == null) return false;
            var objType = obj.GetType();
            if (CheckObservable(objType))
            {
                return false;
            }
            foreach (var property in objType.GetFields())
            {
                if (property.FieldType == objType) continue;
                if (property.FieldType.IsClass && property.FieldType.IsMyInterface())
                {
                    var nextObj = property.GetValue(obj);
                    if (SetFieldRecursive(propertyName, value, nextObj))
                        return true;
                    if (SetPropertyRecursive(propertyName, value, nextObj))
                        return true;
                }
                if (property.Name != propertyName) continue;
                if (property.FieldType == typeof(int))
                {
                    int val;
                    if (value.ToString().GetVal(out val))
                        property.SetValue(obj, val);
                }
                else if (property.FieldType == typeof(uint))
                {
                    uint val;
                    if (value.ToString().GetVal(out val))
                        property.SetValue(obj, val);
                }
                else if (property.FieldType == typeof(double))
                {
                    double val;
                    if (value.ToString().GetVal(out val))
                        property.SetValue(obj, val);
                }
                else if (property.FieldType == typeof(float))
                {
                    float val;
                    if (value.ToString().GetVal(out val))
                        property.SetValue(obj, val);
                }
                else if (property.FieldType == typeof(ulong))
                {
                    ulong val;
                    if (value.ToString().GetVal(out val))
                        property.SetValue(obj, val);
                }
                else if (property.FieldType == typeof(long))
                {
                    long val;
                    if (value.ToString().GetVal(out val))
                        property.SetValue(obj, val);
                }
                else if (property.FieldType == typeof(string))
                {
                    var val = value.ToString().Trim();
                    property.SetValue(obj, val);
                }
                else
                    property.SetValue(obj, value);
                return true;
            }
            return false;
        }

        private static bool CheckObservable(Type objType)
        {
            return objType.IsGenericType && objType.GetGenericTypeDefinition() == typeof(ObservableCollection<>);
        }

        private static bool GetFieldRecursive<T>(string propertyName, object obj, out T value)
        {
            if (obj == null)
            {
                value = default(T);
                return false;
            }
            var objType = obj.GetType();
            if (CheckObservable(objType))
            {
                value = default(T);
                return false;
            }
            foreach (var property in objType.GetFields())
            {
                if (property.FieldType == objType) continue;
                if (property.FieldType.IsMyInterface() && property.FieldType.IsClass)
                {
                    var nextObj = property.GetValue(obj);
                    if (GetFieldRecursive(propertyName, nextObj, out value))
                        return true;
                    if (GetPropertyRecursive(propertyName, nextObj, out value))
                        return true;
                }
                if (property.Name != propertyName) continue;
                value = (T)Convert.ChangeType(property.GetValue(obj), typeof(T));
                return true;
            }
            value = default(T);
            return false;
        }

        public static bool GetValueByName<T>(string propertyName, object obj, out T val)
        {
            return GetPropertyRecursive(propertyName, obj, out val) || GetFieldRecursive(propertyName, obj, out val);
        }

        private static bool GetPropertyRecursive<T>(string propertyName, object obj, out T value)
        {
            if (obj == null)
            {
                value = default(T);
                return false;
            }
            var objType = obj.GetType();
            if (objType.IsGenericType && objType.GetGenericTypeDefinition() == typeof(ObservableCollection<>))
            {
                value = default(T);
                return false;
            }
            foreach (var property in objType.GetProperties())
            {
                if (property.PropertyType == objType) continue;
                if (property.PropertyType.IsMyInterface() && property.PropertyType.IsClass)
                {
                    var nextObj = property.GetValue(obj);
                    if (nextObj == obj) continue;
                    if (GetPropertyRecursive(propertyName, nextObj, out value))
                        return true;
                    if (GetFieldRecursive(propertyName, nextObj, out value))
                        return true;
                }
                if (property.Name != propertyName) continue;
                var v = property.GetValue(obj);
                if (typeof(T).FullName == "System.Enum")
                {
                    if ((v == null) || (v.GetType().BaseType != typeof(T)))
                    {
                        value = default(T);
                        return false;
                    }
                }
                value = (T)Convert.ChangeType(v, typeof(T));
                if (value == null) return false;
                return true;
            }
            value = default(T);
            return false;
        }
    }
}
