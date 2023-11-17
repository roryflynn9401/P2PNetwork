using P2PProject.Client;
using P2PProject.Client.Extensions;
using P2PProject.Client.Models;
using System.Net;

namespace P2PProject
{
    public class Program
    {
        public enum SendTypes { String = 1, Notification = 2,};

        private static List<string> _commands = new() { "Connect to network via IP", "Connect to network via discovery network", "Initalise Network", "Add Data" };
        private static bool _quit = false;
        private static Node _localClient = new();
        private static Action _inputError = () => { Console.WriteLine("Input not recognised, returning to menu");};

        public static async Task Main(string[] args)
        {
            //Set client information
            _localClient.InitialiseNode();
            Console.WriteLine($"Your information is {_localClient.LocalClientInfo.LocalClientIP}:{_localClient.LocalClientInfo.Port}");

            while (!_quit)
            {
                Console.WriteLine("Hello! Here are the supported features:");
                for(int i = 0; i < _commands.Count; i++)
                {
                    Console.WriteLine($"{i+1}. {_commands[i]}");
                }
                if(int.TryParse(Console.ReadLine(), out int command))
                {
                    switch(command)
                    {
                        case 1:
                            Console.WriteLine("Enter the IP of a node on the network");
                            if(IPAddress.TryParse(Console.ReadLine(), out IPAddress ipAddress))
                            {
                                Console.WriteLine("Enter the Port of the node");
                                if(int.TryParse(Console.ReadLine(), out int port))
                                {
                                    var connectEndpoint = new IPEndPoint(ipAddress, port);
                                    Console.WriteLine($"Attempting connection to {ipAddress}:{port}");
                                    var connectionMessage = new ConnectionNotification
                                    {
                                        IP = _localClient.LocalClientInfo.LocalClientIP.ToString(), 
                                        Port = _localClient.LocalClientInfo.Port,
                                        Id = Guid.NewGuid(),
                                        SenderId = _localClient.LocalClientInfo.ClientId
                                    };
                                    await _localClient.IPInitialConnection(connectEndpoint, connectionMessage);
                                    Console.WriteLine("Connection request made, waiting for response...");                                    

                                }
                                else
                                {
                                    _inputError.Invoke();
                                    continue;
                                }
                            }
                            else
                            { 
                                _inputError.Invoke(); 
                                continue; 
                            }
                            break;

                        case 2: 
                            break;

                        case 3:
                            Console.WriteLine("New network initalised, listening for new connections");
                            break;

                        case 4:
                            Console.WriteLine("What kind of data do you want to add?");
                            int i = 1;
                            foreach(var type in Enum.GetNames(typeof(SendTypes)))
                            {
                                Console.WriteLine($"{i}. {type}");
                                i++;
                            }
                            if (int.TryParse(Console.ReadLine(), out int messageType))
                            {
                               switch(messageType)
                                {
                                    case 1:
                                        Console.WriteLine("What is the string content?");
                                        var content = Console.ReadLine();
                                        var message = new StringNotification
                                        {
                                            Id = Guid.NewGuid(),
                                            Content = content ?? string.Empty,
                                            SenderId = _localClient.LocalClientInfo.ClientId,
                                            Timestamp = DateTime.Now,
                                        };
                                        DataStore.NetworkData.Add(message.Id, message);
                                        if (DataStore.NodeMap.Any())
                                        {
                                            await _localClient.SendUDPToAllNodes(DataStore.NodeMap.Select(x => x.Key).ToList(), message);
                                        }
                                        else
                                        {
                                            Console.WriteLine("You are not connected to a network, data has been saved");
                                        }
                                        break;
                                }
                               
                            }
                            break;

                        default:
                            _inputError.Invoke();
                            continue;
                    }
                }
                else
                {
                    _inputError.Invoke();
                    continue;
                }

            }

            //Console.WriteLine("Hello!, Write the port you want to use!");
            //var senderPort = Console.ReadLine();
            //var port = int.Parse(senderPort);

            

            //Console.WriteLine("Hello!, Write the port of the recipient");
            //var recipientinput = Console.ReadLine();
            //var recipientPort = int.Parse(recipientinput);


            //var localClient = new Client.Client();
            //localClient.UDPListen = true;
            //localClient.RecieveUDP(port);
            //localClient.NodeMap.Add(recipientPort, new IPEndPoint(IPAddress.Parse("192.168.1.109"), recipientPort));

            //var message = new Message
            //{
            //    Id = Guid.NewGuid(),
            //    Content = "Testing Content",
            //    SenderId = Guid.NewGuid()
            //};

            //Console.WriteLine("Hello!, Write the Id of the client you want to send to");
            //var nodeIds = localClient.NodeMap.Select(x => x.Key);
            //nodeIds.ToList().ForEach(x => Console.WriteLine(x));

            //var recipientIdStr = Console.ReadLine();
            //var recipientId = int.Parse(recipientIdStr);

            //localClient.SendUDP(recipientId, message);

            //Console.ReadLine();
        }
    }
}