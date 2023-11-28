using Newtonsoft.Json;
using P2PProject.Client.EventHandlers;
using P2PProject.Data;
using System.Text;
using System.Text.Json;

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

        public static T? DecodeByteArray<T>(byte[] bytes)
        {
            string data = Encoding.UTF8.GetString(bytes);
            try
            {
                var obj = JsonConvert.DeserializeObject<T>(data, _serializationSettings);
                if (obj != null) return obj;    
            }
            catch 
            {
                Console.WriteLine("Malformed Data detected! \nRequesting network synchronization\n");
            }

            return default;
        }        
    }
}