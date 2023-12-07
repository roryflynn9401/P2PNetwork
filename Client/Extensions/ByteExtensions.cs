using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

namespace P2PProject.Client.Extensions
{
    public static class ByteExtensions
    {
        private static JsonSerializerSettings _serializationSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Objects,
        };

        public static byte[] GetByteArray<T>(T item)
        {

            string jsonString = JsonConvert.SerializeObject(item, _serializationSettings);
            return Encoding.UTF8.GetBytes(jsonString);
        }

        public static T? DecodeByteArray<T>(byte[] bytes, bool includeTypes = true)
        {
            string data = Encoding.UTF8.GetString(bytes);
            try
            {
                var obj = JsonConvert.DeserializeObject<T>(data, includeTypes ? _serializationSettings : new JsonSerializerSettings());
                if (obj != null) return obj;
            }
            catch { }

            return default;

        }
        
        public static string GetChecksumString(this byte[] bytes)
        {
            using (var md5 = MD5.Create())
            {
                var byteChecksum = md5.ComputeHash(bytes);
                return Encoding.UTF8.GetString(byteChecksum);
            }
        }

        public static bool ContainsType(byte[] bytes) => Encoding.UTF8.GetString(bytes).Contains("$type");
    }
}