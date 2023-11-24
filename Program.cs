using P2PProject.Client;
using P2PProject.Client.EventHandlers;
using P2PProject.Client.Extensions;
using P2PProject.Client.Models;
using P2PProject.Data;
using System.Net;

namespace P2PProject
{
    public static class Program
    {
        public enum SendTypes { String = 1, Notification = 2,};

        private static List<string> _commands = new() { "Connect to network via IP", "Connect to network via discovery service", "Initalise Network", "Add Data", "View Nodes on Network", "View Data", "Disconnect from network" };
        private static bool _quit = false;
        private static Node _localClient = new();
        private static Action _inputError = () => { Console.WriteLine("Input not recognised, returning to menu");};
        public static bool Connected = false; 

        public static async Task Main(string[] args)
        {
            //Set client information
            NotificationHandler _notificationHandler = new(_localClient);
            _localClient.InitialiseNode();
            Console.WriteLine($"Your information is {_localClient.LocalClientInfo.LocalNodeIP}:{_localClient.LocalClientInfo.Port}");
            Console.WriteLine("Set the nodes nickname");

            var nickname = Console.ReadLine();
            _localClient.LocalClientInfo.ClientName = nickname ?? string.Empty;
            var connectedInvalid = new[] { 0, 1 };

            while (!_quit)
            {
                Console.WriteLine("Hello! Here are the supported features:");
                for(int i = 0; i < _commands.Count; i++)
                {
                    var isConnected = Connected && connectedInvalid.Contains(i) ? "- ALREADY CONNECTED" : ""; 
                    Console.WriteLine($"{i+1}. {_commands[i]} {isConnected}");
                }
                if(int.TryParse(Console.ReadLine(), out int command))
                {
                    switch(command)
                    {
                        case 1:
                            if (Connected)
                            {
                                Console.WriteLine("You are already connected to a network"); 
                                break;
                            }

                            Console.WriteLine("Enter the IP of a node on the network");
                            if(IPAddress.TryParse(Console.ReadLine(), out IPAddress? ipAddress))
                            {
                                Console.WriteLine("Enter the Port of the node");
                                if(int.TryParse(Console.ReadLine(), out int port))
                                {
                                    var connectEndpoint = new IPEndPoint(ipAddress, port);
                                    Console.WriteLine($"Attempting connection to {ipAddress}:{port}");
                                    var connectionMessage = new ConnectionNotification
                                    {
                                        IP = _localClient.LocalClientInfo.LocalNodeIP.ToString(),
                                        Port = _localClient.LocalClientInfo.Port,
                                        Id = Guid.NewGuid(),
                                        SenderId = _localClient.LocalClientInfo.ClientId,
                                        SendData = true,
                                        Timestamp = DateTime.UtcNow,
                                        NodeName = _localClient.LocalClientInfo.ClientName,
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
                                            await _localClient.SendUDPToNodes(DataStore.NodeMap.Select(x => x.Key).ToList(), message);
                                        }
                                        else
                                        {
                                            Console.WriteLine("You are not connected to a network, data has been saved");
                                        }
                                        break;
                                }
                               
                            }
                            break;
                        case 5:
                            Console.WriteLine("Nodes currently on the network:");
                            foreach(var node in DataStore.NodeMap.Select(x => x.Value))
                            {
                                Console.WriteLine($"{node.LocalNodeIP}:{node.Port}");
                            }
                            break;
                        case 6:
                            Console.WriteLine("Data currently stored:");
                            foreach (var dataPair in DataStore.NetworkData.OrderBy(x => x.GetType()))
                            {
                                Console.WriteLine($"{dataPair.Key}: {dataPair.GetType().Name}");
                            }
                            break;
                        case 7:
                            Console.WriteLine("Disconnecting from network...");
                            await _localClient.DisconnectFromNetwork();
                            _quit = true;
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
        }
    }
}