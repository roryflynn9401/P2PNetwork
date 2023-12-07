using P2PProject.Client.Models;
using System.Net;
using System.Net.Sockets;

namespace P2PProject.Client.Extensions
{
    public static class NetworkExtensions
    {        
        public static IEnumerable<IPAddress> GetLocalIPAddress()
        {
            var ips = new List<IPAddress>();
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        ips.Add(ip);
                    }
                }
            }
            catch(Exception e) {  }
            finally
            {
                if(ips.Count == 0)
                {
                    bool validIp = false;
                    while (!validIp)
                    {
                        Console.WriteLine("Error getting local IP address.\nPlease enter your local IP address");
                        var ipString = Console.ReadLine();
                        if(IPAddress.TryParse(ipString, out var ip))
                        {
                            ips.Add(ip);
                            validIp = true;
                        }
                    }
                }
            }
            return ips;
        }

        public static NodeInfo ToNodeInfo(this IPEndPoint ep, Guid id, string nodeName) => new NodeInfo(ep.Address.ToString(), ep.Port, id, nodeName);
    }
}
