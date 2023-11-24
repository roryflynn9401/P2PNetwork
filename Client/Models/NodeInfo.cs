using Newtonsoft.Json;
using System.Net;

namespace P2PProject.Client.Models
{
    public class NodeInfo
    {
        public NodeInfo() { }
        public NodeInfo(string ip, int port, Guid id, string? name = null) 
        {
            LocalNodeIP = ip;
            Port = port;
            ClientId = id;
            ClientName = name ?? string.Empty;
        }

        public string LocalNodeIP { get; set; } = string.Empty;
        public int Port { get; set; }
        public Guid ClientId { get; set; }
        public string ClientName { get; set; }

        [JsonIgnore]
        public IPEndPoint LocalIPEndPoint => new IPEndPoint(IPAddress.Parse(LocalNodeIP), Port);
    }
}
