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

        public IPAddress LocalClientIP { get; set; }
        public int Port { get; set; }
        public Guid ClientId { get; set; }
    }
}
