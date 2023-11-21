using Newtonsoft.Json;
using System.Net;

namespace P2PProject.Client.Models
{
    public class EndPoint
    {
        public EndPoint(string ip, int port) 
        { 
            IP = ip;
            Port = port;
        }

        public string IP { get; set; }
        public int Port { get; set; }

        [JsonIgnore]
        public IPEndPoint IPEndPoint => new IPEndPoint(IPAddress.Parse(IP), Port);
    }
}
