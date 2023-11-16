using P2PProject.Client.Models;
using System.Net;

namespace P2PProject
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Hello!, Write the port you want to use!");
            var senderPort = Console.ReadLine();
            var port = int.Parse(senderPort);

            Console.WriteLine("Hello!, Write the port of the recipient");
            var recipientinput = Console.ReadLine();
            var recipientPort = int.Parse(recipientinput);


            var localClient = new Client.Client(port);
            localClient.UDPListen = true;
            localClient.RecieveUDP(port);
            localClient.NodeMap.Add(recipientPort, new IPEndPoint(IPAddress.Parse("192.168.1.109"), recipientPort));

            var message = new Message
            {
                Id = 1,
                Content = "Testing Content",
                SenderId = 2
            };

            Console.WriteLine("Hello!, Write the Id of the client you want to send to");
            var nodeIds = localClient.NodeMap.Select(x => x.Key);
            nodeIds.ToList().ForEach(x => Console.WriteLine(x));

            var recipientIdStr = Console.ReadLine();
            var recipientId = int.Parse(recipientIdStr);

            localClient.SendUDP(recipientId, message);

            Console.ReadLine();
        }
    }
}