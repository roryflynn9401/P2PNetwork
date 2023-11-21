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

        public static Models.EndPoint ToEndpoint(this IPEndPoint ep) => new Models.EndPoint(ep.Address.ToString(), ep.Port);
    }
}
