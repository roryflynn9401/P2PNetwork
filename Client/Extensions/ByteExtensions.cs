using Newtonsoft.Json;
using P2PProject.Client.EventHandlers;
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

                Console.WriteLine("Malformed Data detected! \n Requesting network synchronization");                    
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return default;
        }        
    }
}
