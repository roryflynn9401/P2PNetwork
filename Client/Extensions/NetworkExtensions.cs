using P2PProject.Client.Models;
using System.Net;
using System.Net.Sockets;

namespace P2PProject.Client.Extensions
{
    public static class NetworkExtensions
    {        
        public static IEnumerable<IPAddress> GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    yield return ip;
                }
            }   
        }

        public static NodeInfo ToNodeInfo(this IPEndPoint ep, Guid id, string nodeName) => new NodeInfo(ep.Address.ToString(), ep.Port, id, nodeName);
    }
}
