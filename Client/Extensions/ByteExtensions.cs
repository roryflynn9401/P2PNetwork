using Newtonsoft.Json;
using System.Text;
using System.Text.Json;

namespace P2PProject.Client.Extensions
{
    public static class ByteExtensions
    {
        public static event EventHandler MalformedData;

        private static JsonSerializerSettings _serializationSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
        };

        public static byte[] GetByteArray<T>(T item)
        {

            string jsonString = JsonConvert.SerializeObject(item, _serializationSettings);
            return Encoding.UTF8.GetBytes(jsonString);
        }

        public static T DecodeByteArray<T>(byte[] bytes)
        {
            string data = Encoding.UTF8.GetString(bytes);
            var obj = JsonConvert.DeserializeObject<T>(data, _serializationSettings);

            if (obj == null)
            {
                MalformedData.Invoke(typeof(ByteExtensions), EventArgs.Empty);
                //Event to Report malformed data needs implementation and subscribers
            }

            return obj!; // Assuming for now data is correct
        }
    }
}
