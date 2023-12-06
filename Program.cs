using Newtonsoft.Json;
using P2PProject.Client;
using P2PProject.Client.Extensions;
using P2PProject.Client.Models;
using P2PProject.Data;
using System.Net;

namespace P2PProject
{
    public static class Program
    {
        public enum SendTypes { String = 1, Object = 2,};
        public static bool Connected => _localClient.NetworkId.HasValue;

        private static List<string> _commands = new() { "Connect to network via IP", "Connect to network via discovery service", "Initalise Network", "Add Data", 
            "View Nodes on Network", "View Data", "Disconnect from network", "Generate malformed data", "Check for inactive nodes", "Get File from path", 
            "Add file and notify network", "Get file on network", "Sync Data from network", "Shutdown Network", "Generate example data", "Simulate unreliable connection", "Add data locally" };

        private static bool _quit = false;
        private static Node _localClient = new();
        private static Action _inputError = () => { Console.WriteLine("Input not recognised, returning to menu\n");};
        private static DirectoryService? _directoryService;

        public static async Task Main(string[] args)
        {
            //Set client information
            _localClient.InitialiseNode();
            Console.WriteLine($"Your information is {_localClient.LocalClientInfo.LocalNodeIP}:{_localClient.LocalClientInfo.Port}\n");
            Console.WriteLine("Set the nodes nickname");

            var nickname = Console.ReadLine();
            _localClient.LocalClientInfo.ClientName = nickname ?? _localClient.LocalClientInfo.LocalNodeIP;
            var connectedInvalid = new[] { 1, 2, 3 };
            _directoryService = new DirectoryService(_localClient);
            _localClient.DirectoryService = _directoryService;

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


            //Show Networks before menu
            await ViewNetworks();

            while (!_quit)
            { 

                Console.WriteLine("Hello! Here are the supported features:\n");
                for (int i = 0; i < _commands.Count; i++)
                {
                    if (Connected && connectedInvalid.Contains(i+1)) continue;
                    var y = Connected ? i - 2 : i + 1;

                    Console.WriteLine($"{y}. {_commands[i]}");
                }

                if (int.TryParse(Console.ReadLine(), out int command))
                {
                    command = Connected ? command + 3 : command;

                    switch (command)
                    {
                        case 1:
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
                            await ViewNetworks();
                            break;

                        case 3:
                            Console.WriteLine("Initialising network...");
                            var id = await _directoryService.InitialiseNetwork();
                            if (id == default) { Console.WriteLine("Error initialising network"); break; }
                            _localClient.NetworkId = id.Value;
                            break;

                        case 4:
                            await AddData();
                            break;
                        case 5:
                            _printNodes.Invoke();
                            break;
                        case 6:
                            Console.WriteLine("Data currently stored:");
                            foreach (var dataPair in DataStore.NetworkData.OrderBy(x => x.Value.Timestamp))
                            {
                                var content = dataPair.Value is StringNotification sn ? sn.Content : 
                                              dataPair.Value is SendableItem si ? JsonConvert.SerializeObject(si.Item) : string.Empty;

                                Console.WriteLine($"({dataPair.Value.Timestamp}) {dataPair.Key}: {dataPair.Value.GetType().Name} \n{content}");
                            }
                            break;
                        case 7:
                            Console.WriteLine("Disconnecting from network...");
                            await _localClient.DisconnectFromNetwork();
                            _quit = true;
                            Environment.Exit(0);
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


                                if(!File.Exists(directory + "\\" + Path.GetFileName(filePath)))
                                {
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
                                else if(DataStore.NetworkData.Any(x => x.Value is FileNotification file && file.FileName == Path.GetFileName(filePath)))
                                {
                                    Console.WriteLine("Network already knows about this file");
                                }
                                else
                                {
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
                        case 14:
                            Console.WriteLine("Shutting down network...");
                            var shutdown = new NetworkNotification
                            {
                                Id = Guid.NewGuid(),
                                SenderId = _localClient.LocalClientInfo.ClientId,
                                Timestamp = DateTime.UtcNow,
                                Type = NotificationType.NetworkShutdown,
                            };
                            _quit = true;
                            await _localClient.SendUDPToNodes(DataStore.NodeMap.Select(x => x.Key).ToList(), shutdown);
                            await _directoryService.NetworkShutdown(_localClient.NetworkId.Value);
                            Console.WriteLine("Network shutdown, Shutting down node");
                            Environment.Exit(0);
                            break;
                        case 15:
                            await GenerateExampleData();
                            break;
                        case 16:
                            break;
                        case 17:
                            await AddData(true);
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

        private static async Task ViewNetworks()
        {
            var networks = await _directoryService.GetNetworks();
            if (networks == null || networks.Count == 0)
            {
                Console.WriteLine("No networks currently available to join\n");
                return;
            }

            Console.WriteLine("Networks currently available:");
            int i = 1;
            foreach (var network in networks)
            {
                Console.WriteLine($"{i}. Network: {network.Id} \nConnectedNodes: {network.NodeCount}");
                i++;
            }

            Console.WriteLine("Select the network to connect to or type N to go to menu\n");
            var networkSelected = Console.ReadLine();
            if (networkSelected?.ToLower() == "n")
            {
                return;
            }
            else if (int.TryParse(networkSelected, out int networkNumber))
            {
                if (networkNumber > networks.Count || networkNumber < 1)
                {
                    _inputError.Invoke();
                    return;
                }
                var network = networks[networkNumber - 1];
                var id = await _directoryService.ConnectViaDirectory(network.Id);
                if (id == default || id == Guid.Empty) { Console.WriteLine("Error connecting to network"); return; }
                _localClient.NetworkId = id.Value;
                return;
            }
            else
            {
                _inputError.Invoke();
                return;
            }
        }

        private static async Task AddData(bool localOnly = false)
        {
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
                        if (DataStore.NodeMap.Any() && !localOnly)
                        {
                            await _localClient.SendUDPToNodes(DataStore.NodeMap.Select(x => x.Key).ToList(), message);
                        }
                        else
                        {
                            Console.WriteLine("You are not connected to a network, data has been saved");
                        }
                        break;

                    case 2:
                        Console.WriteLine("What is the object content as JSON?");
                        var objectContent = Console.ReadLine();
                        var obj = JsonConvert.DeserializeObject(objectContent ?? string.Empty);
                        var sendableItem = new SendableItem
                        {
                            Id = Guid.NewGuid(),
                            Item = obj,
                            SenderId = _localClient.LocalClientInfo.ClientId,
                            Timestamp = DateTime.Now,
                        };
                        DataStore.NetworkData.Add(sendableItem.Id, sendableItem);
                        if (DataStore.NodeMap.Any() && !localOnly)
                        {
                            await _localClient.SendUDPToNodes(DataStore.NodeMap.Select(x => x.Key).ToList(), sendableItem);
                        }
                        else
                        {
                            Console.WriteLine("You are not connected to a network, data has been saved");
                        }
                        break;
                }
            }
            else { _inputError.Invoke(); return;}
        }

        private static async Task GenerateExampleData(bool localOnly = false)
        {
            var items = new List<ISendableItem>();
            for(int i =0; i<10; i++)
            {
                var message = new StringNotification
                {
                    Id = Guid.NewGuid(),
                    Content = "Example String",
                    SenderId = _localClient.LocalClientInfo.ClientId,
                    Timestamp = DateTime.Now,
                };
                items.Add(message);

                var sendableItem = new SendableItem
                {
                    Id = Guid.NewGuid(),
                    Item = new { Example = "Example Object", Age = 19, Name = "Rory", University = "Queen's University Belfast" },
                    SenderId = _localClient.LocalClientInfo.ClientId,
                    Timestamp = DateTime.Now,
                };
                items.Add(sendableItem);
            }
            foreach(var item in items)
            {
                DataStore.NetworkData.Add(item.Id, item);
                if (DataStore.NodeMap.Any() && !localOnly)
                {
                    var itemTasks = new List<Task>();
                    var ids = DataStore.NodeMap.Select(x => x.Key).ToList();
                    itemTasks.Add(_localClient.SendUDPToNodes(ids, item)); 
                    await Task.WhenAll(itemTasks);
                }
            }
        }
    }
}