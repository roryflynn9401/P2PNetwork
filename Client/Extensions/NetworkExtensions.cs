using P2PProject.Client.Models;
using System.Net;
using System.Net.Sockets;

namespace P2PProject.Client.Extensions
{
    public static class NetworkExtensions
    {        
        public static IPAddress GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        public static NodeInfo ToNodeInfo(this IPEndPoint ep, Guid id) => new NodeInfo(ep.Address.ToString(), ep.Port, id);
    }
}
