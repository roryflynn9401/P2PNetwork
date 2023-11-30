using P2PProject.Client;
using P2PProject.Client.EventHandlers;
using P2PProject.Client.Extensions;
using P2PProject.Client.Models;
using P2PProject.Data;
using System.Collections.Concurrent;
using System.Net;

namespace P2PProject
{
    public static class Program
    {
        public enum SendTypes { String = 1, Notification = 2,};
        public static bool Connected = false; 

        private static List<string> _commands = new() { "Connect to network via IP", "Connect to network via discovery service", "Initalise Network", "Add Data", 
            "View Nodes on Network", "View Data", "Disconnect from network", "Generate malformed data", "Check for inactive nodes", "Get File from path", "Add file and notify network", "Get file on network", "Sync Data from network" };
        private static bool _quit = false;
        private static Node _localClient = new();
        private static Action _inputError = () => { Console.WriteLine("Input not recognised, returning to menu\n");};

        public static async Task Main(string[] args)
        {
            //Set client information
            NotificationHandler _notificationHandler = new(_localClient);
            _localClient.InitialiseNode();
            Console.WriteLine($"Your information is {_localClient.LocalClientInfo.LocalNodeIP}:{_localClient.LocalClientInfo.Port}\n");
            Console.WriteLine("Set the nodes nickname");

            var nickname = Console.ReadLine();
            _localClient.LocalClientInfo.ClientName = nickname ?? _localClient.LocalClientInfo.LocalNodeIP;
            var connectedInvalid = new[] { 0, 1 };

            Action _printNodes = () =>
            {
                Console.WriteLine("Nodes currently on the network:");
                int i = 1;
                foreach (var node in DataStore.NodeMap.Select(x => x.Value))
                {
                    Console.WriteLine($"{i}. {node.ClientName}  {node.LocalNodeIP}:{node.Port}");
                    i++;
                }
            };

            while (!_quit)
            { 

                Console.WriteLine("Hello! Here are the supported features:\n");
                for (int i = 0; i < _commands.Count; i++)
                {
                    var isConnected = Connected && connectedInvalid.Contains(i) ? "- ALREADY CONNECTED" : "";
                    Console.WriteLine($"{i + 1}. {_commands[i]} {isConnected}");
                }
                if (int.TryParse(Console.ReadLine(), out int command))
                {
                    switch (command)
                    {
                        case 1:
                            if (Connected)
                            {
                                Console.WriteLine("You are already connected to a network\n");
                                break;
                            }

                            Console.WriteLine("Enter the IP of a node on the network");
                            if (IPAddress.TryParse(Console.ReadLine(), out IPAddress? ipAddress))
                            {
                                Console.WriteLine("Enter the Port of the node");
                                if (int.TryParse(Console.ReadLine(), out int port))
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
                            foreach (var type in Enum.GetNames(typeof(SendTypes)))
                            {
                                Console.WriteLine($"{i}. {type}");
                                i++;
                            }
                            if (int.TryParse(Console.ReadLine(), out int messageType))
                            {
                                switch (messageType)
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
                            _printNodes.Invoke();
                            break;
                        case 6:
                            Console.WriteLine("Data currently stored:");
                            foreach (var dataPair in DataStore.NetworkData.OrderBy(x => x.GetType()))
                            {
                                Console.WriteLine($"{dataPair.Key}: {dataPair.Value.GetType().Name}");
                            }
                            break;
                        case 7:
                            Console.WriteLine("Disconnecting from network...");
                            await _localClient.DisconnectFromNetwork();
                            _quit = true;
                            break;
                        case 8:
                            var data = new StringNotification
                            {
                                Id = Guid.NewGuid(),
                                Content = string.Empty,
                                SenderId = _localClient.LocalClientInfo.ClientId,
                                Timestamp = DateTime.Now,
                            };
                            DataStore.NetworkData.Add(data.Id, data);

                            var sendData = ByteExtensions.GetByteArray(data);
                            await _localClient.SendMalformedUDP(sendData.Take(sendData.Length / 2).ToArray(), DataStore.NodeMap.First().Value.LocalIPEndPoint);
                            break;
                        case 9:
                            _localClient.PingService = new PingService(_localClient);
                            await _localClient.PingService.InitaliseSync();
                            break;
                        case 10:
                            Console.WriteLine("Enter the name or path of the file you want");
                            var fileName = Console.ReadLine();
                            Console.WriteLine("Enter the location you want to save the file in");
                            var saveLocation = Console.ReadLine();
                            if (fileName != null)
                            {
                                var request = new RequestPacket
                                {
                                    Id = Guid.NewGuid(),
                                    SenderId = _localClient.LocalClientInfo.ClientId,
                                    FileName = fileName,
                                    FromPath = true,
                                };
                                _localClient.FileTransferClient = new UDPFileTransferClient(_localClient, saveLocation);
                                await _localClient.FileTransferClient.SendUDPToNodes(DataStore.NodeMap.Select(x => x.Key).ToList(), request);
                            }
                            break;
                        case 11:
                            Console.WriteLine("Enter the path of the file you want to add");
                            var filePath = Console.ReadLine();
                            if (File.Exists(filePath))
                            {
                                string workingDirectory = Environment.CurrentDirectory;
                                var directory = $"{workingDirectory}\\{_localClient.LocalClientInfo.ClientName}";
                                if (!Directory.Exists(directory))
                                {
                                    Directory.CreateDirectory(directory);
                                }
                                File.Copy(filePath, directory + "\\" +Path.GetFileName(filePath));
                                var fileNotification = new FileNotification
                                {
                                    Id = Guid.NewGuid(),
                                    SenderId = _localClient.LocalClientInfo.ClientId,
                                    FileName = Path.GetFileName(filePath),
                                    Timestamp = DateTime.UtcNow,
                                };
                                DataStore.NetworkData.Add(fileNotification.Id, fileNotification);
                                await _localClient.SendUDPToNodes(DataStore.NodeMap.Select(x => x.Key).ToList(), fileNotification);
                            }
                            else Console.WriteLine("File does not exist");
                            break;
                        case 12:
                            Console.WriteLine("Enter the name of the file you want\n");
                            var networkFiles = DataStore.NetworkData.Where(x => x.Value is FileNotification).Select(x => x.Value as FileNotification).ToList();
                            if (networkFiles.Any())
                            {
                                for (int y = 0; y < networkFiles.Count; y++)
                                {
                                    Console.WriteLine($"{y + 1}. {networkFiles[y]?.FileName}");
                                }
                                var fileSelected = Console.ReadLine();
                                if (int.TryParse(fileSelected, out int fileNumber))
                                {
                                    string workingDirectory = Environment.CurrentDirectory;
                                    var directory = $"{workingDirectory}\\{_localClient.LocalClientInfo.ClientName}";
                                    if (!Directory.Exists(directory))
                                    {
                                        Directory.CreateDirectory(directory);
                                    }
                                    var file = networkFiles[fileNumber - 1];
                                    var request = new RequestPacket
                                    {
                                        Id = Guid.NewGuid(),
                                        SenderId = _localClient.LocalClientInfo.ClientId,
                                        FileName = file.FileName,
                                        FromPath = false,
                                    };
                                    _localClient.FileTransferClient = new UDPFileTransferClient(_localClient, $"{directory}\\{file.FileName}");
                                    await _localClient.FileTransferClient.SendUDPToNodes(DataStore.NodeMap.Select(x => x.Key).ToList(), request);
                                }
                                else
                                {
                                    _inputError.Invoke();
                                    continue;
                                }
                            }
                            else
                            {
                                Console.WriteLine("No files currently on the network\n");
                            }
                            
                            break;

                        case 13:
                            _localClient.SyncService = new DataSyncService(_localClient, true);
                            await _localClient.SyncService.InitaliseSync();
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