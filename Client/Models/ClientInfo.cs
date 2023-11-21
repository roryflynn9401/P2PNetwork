using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace P2PProject.Client.Models
{
    public class ClientInfo
    {
        public ClientInfo() { }

        public string LocalClientIP { get; set; }
        public int Port { get; set; }
        public Guid ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;

        [JsonIgnore]
        public IPEndPoint LocalIPEndPoint => new IPEndPoint(IPAddress.Parse(LocalClientIP), Port);
    }
}
