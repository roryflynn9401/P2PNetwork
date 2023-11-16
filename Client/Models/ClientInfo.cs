using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace P2PProject.Client.Models
{
    public class ClientInfo
    {
        public ClientInfo() { }

        public IPEndPoint LocalCLientEndpoint { get; set; }
        public IPEndPoint ExternalCLientEndpoint { get; set; }

        public long ClientId { get; set; }
    }
}
