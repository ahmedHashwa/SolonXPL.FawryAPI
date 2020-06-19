using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace SolonXpl.FawryAPI
{
    public static class Utilities
    {
        public static T Parse<T>(this string value, bool ignoreCase = true)
        {
            return (T)Enum.Parse(typeof(T), value, ignoreCase);
        }
        public static string Sha256(this string s)
        {
            using var provider = SHA256.Create();
            var builder = new StringBuilder();
            foreach (var b in provider.ComputeHash(Encoding.UTF8.GetBytes(s)))
                builder.Append(b.ToString("x2").ToLower());
            return builder.ToString();
        }
        public static string Md5(this string s)
        {
            using var provider = MD5.Create();
            var builder = new StringBuilder();
            foreach (var b in provider.ComputeHash(Encoding.UTF8.GetBytes(s)))
                builder.Append(b.ToString("x2").ToLower());
            return builder.ToString();
        }
        public static string Join(this IEnumerable<string> strings, string separator = ",")
        {
            return string.Join(separator, strings);
        }
        public static IDictionary<string, object> AsDictionary(this object source, BindingFlags bindingAttr = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance)
        {
            return source.GetType().GetProperties(bindingAttr).ToDictionary
            (
                propInfo => propInfo.Name,
                propInfo => propInfo.PropertyType == typeof(byte[]) ? null : propInfo.GetValue(source, null)
            );
        }
        public static string Serialize(this object item, JsonSerializerSettings serializerSettings = null)
        {
            var defaultSettings = serializerSettings ?? new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto, NullValueHandling = NullValueHandling.Ignore };
            return JsonConvert.SerializeObject(item, defaultSettings);
        }
        public static double TotalMilliseconds(this DateTime dateTime)
        {
            return dateTime.ToUniversalTime().Subtract(
                new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            ).TotalMilliseconds;
        }
        public static string ToFormData(this IDictionary<string, object> data)
        {
            return string.Join("&", data.Select(d => $"{d.Key}={d.Value}"));
        }
        public static TResult Deserialize<TResult>(this string data)
        {
            if (data == null)
                return default;
            var defaultSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };
            return JsonConvert.DeserializeObject<TResult>(data, defaultSettings);
        }



    }
}
