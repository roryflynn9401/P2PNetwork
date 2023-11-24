using Newtonsoft.Json;
using System.Net;

namespace P2PProject.Client.Models
{
    public class ConnectionNotification : ISendableItem
    {
        public Guid Id { get; set; }
        public Guid SenderId { get; set; }
        public string IP { get; set; }
        public int Port { get; set; }
        public DateTime? Timestamp { get; set; }
        public bool SendData { get; set; }
        public string NodeName { get; set; }

        [JsonIgnore]
        public IPEndPoint ConnectionInformation => new IPEndPoint(IPAddress.Parse(IP), Port);
    }
}
