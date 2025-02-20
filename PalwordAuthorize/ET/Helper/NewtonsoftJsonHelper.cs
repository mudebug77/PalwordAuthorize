using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;

namespace ET
{
    //讓所有集合先調用Clear
    public class ConfigurationListConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(IList).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var List = (existingValue as IList ?? (IList)serializer.ContractResolver.ResolveContract(objectType).DefaultCreator());
            if (List != null)
                List.Clear();
            serializer.Populate(reader, List);
            return List;
        }

        public override bool CanWrite { get { return false; } }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    public static class NewtonsoftJsonHelper
    {

        private static void HandleDeserializationError(object sender, Newtonsoft.Json.Serialization.ErrorEventArgs errorArgs)
        {
            // 忽略错误，继续解析
            errorArgs.ErrorContext.Handled = true;
        }

        private static JsonSerializerSettings CreateSettings()
        {
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new ConfigurationListConverter());
            settings.Converters.Add(new StringEnumConverter());
            settings.StringEscapeHandling = StringEscapeHandling.EscapeNonAscii;
            settings.Error = HandleDeserializationError;
            return settings;
        }

        public static T LoadJsonFile<T>(string path)
        {
            var input = File.ReadAllText(path);
            var settings = CreateSettings();
            return JsonConvert.DeserializeObject<T>(input, settings);
        }

        public static void LoadJsonFile(this object target, string path)
        {
            if (!File.Exists(path)) return;
            var input = File.ReadAllText(path);
            var settings = CreateSettings();
            JsonConvert.PopulateObject(input, target, settings);
        }

        public static void LoadJsonString(this object target, string json)
        {
            var settings = CreateSettings();
            JsonConvert.PopulateObject(json, target, settings);
        }

        public static T LoadJsonString<T>(string json)
        {
            var settings = CreateSettings();
            return JsonConvert.DeserializeObject<T>(json, settings);
        }

        public static void SaveJsonFile(this object target, string path, bool IsUnicode = true)
        {
            var settings = CreateSettings();
            if (IsUnicode) settings.StringEscapeHandling = StringEscapeHandling.EscapeNonAscii;
            var outText = JsonConvert.SerializeObject(target, Formatting.Indented, settings);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, outText, Encoding.Unicode);
        }

        public static string ToJsonString(this object target, bool IsUnicode = true)
        {
            var settings = CreateSettings();
            if (IsUnicode) settings.StringEscapeHandling = StringEscapeHandling.EscapeNonAscii;
            var outText = JsonConvert.SerializeObject(target, Formatting.Indented, settings);
            return outText;
        }
    }
}
