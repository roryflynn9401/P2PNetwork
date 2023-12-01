using P2PProject.Client.Extensions;
using P2PProject.Client.Models;
using P2PProject.Data;
using System.Net;
using System.Net.Sockets;

namespace P2PProject.Client
{
    public class Node
    {
        private Thread? UDPListenerThread;
        public NodeInfo LocalClientInfo { get; set; } = new();
        public bool UDPListen { get; set; }
        public bool TransferringFile { get; set; }
        public DataSyncService? SyncService;
        public PingService? PingService;
        public UDPFileTransferClient? FileTransferClient;
        public DirectoryService? DirectoryService;

        public Guid? NetworkId { get; set; }

        public Node(bool _udpListen = false) 
        {
            UDPListen = _udpListen;
        }

        #region UDP Communication

        public void RecieveUDP(int receivingPort)
        {
            //Start Listener thread for incoming UDP packets
            UDPListenerThread = new Thread(new ThreadStart(async delegate
            {
                //Creates a UdpClient for reading incoming data.
                UdpClient receivingUdpClient = new UdpClient(receivingPort);
                IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
                try
                {
                    while (UDPListen)
                    {
                        var receiveBytes = receivingUdpClient.Receive(ref RemoteIpEndPoint);
                        var returnData = ByteExtensions.DecodeByteArray<ISendableItem>(receiveBytes);

                        if (returnData != null && returnData != default(ISendableItem))
                        {
                            await ProcessItem(returnData, RemoteIpEndPoint);
                        }
                        else
                        {
                            var ftData = ByteExtensions.DecodeByteArray<IPacket>(receiveBytes);
                            if (ftData != null && ftData != default(IPacket))
                            {
                                FileTransferClient ??= new UDPFileTransferClient(this);
                                await FileTransferClient.ProcessData(ftData);
                            }
                            else if(!TransferringFile)
                            {
                                SyncService = new DataSyncService(this);
                                await SyncService.InitaliseSync();
                            }
                        }
                    }
                }
                catch(SocketException)
                {
                    RetryDifferentPort();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }));
            UDPListenerThread.Start();
        }

        public async Task SendUDP(Guid recipientId, ISendableItem item)
        {
            try
            {    
                if (DataStore.NodeMap.TryGetValue(recipientId, out NodeInfo? send))
                {
                    Console.WriteLine($"Sending data to node {send.ClientName} {send.LocalNodeIP}:{send.Port}\n");
                    await SendUDP(send.LocalIPEndPoint, item);
                }
                    
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public async Task SendUDP(IPEndPoint recipient, ISendableItem item)
        {           
            var sendData = ByteExtensions.GetByteArray(item);

            if (sendData != null )
            {
                using var client = new UdpClient();
                await client.SendAsync(sendData, sendData.Length, recipient);
            }            
        }

        public async Task SendUDPToNodes(List<Guid> ids, ISendableItem item)
        {
            var tasks = new List<Task>();
            ids.ForEach(x => tasks.Add(SendUDP(x, item)));
            await Task.WhenAll(tasks);
        }

        public async Task SendMalformedUDP(byte[] data, IPEndPoint? ep = null)
        {
            using var client = new UdpClient();       
            await client.SendAsync(data, data.Length, ep ?? LocalClientInfo.LocalIPEndPoint);
        }

        public async Task SyncNetwork()
        {
            var syncNotification = new NetworkNotification
            {
                Id = Guid.NewGuid(),
                SenderId = LocalClientInfo.ClientId,
                Timestamp = DateTime.UtcNow,
                Type = NotificationType.Sync
            };
            await SendUDPToNodes(DataStore.NodeMap.Select(x => x.Key).ToList(), syncNotification);
        }

        #endregion

        #region Connetion/Disconnection

        public async Task IPInitialConnection(IPEndPoint ep, ConnectionNotification connectionInfo)
        {            
            await SendUDP(ep, connectionInfo);
            UDPListen = true;
        }

        public async Task DisconnectFromNetwork()
        {
            var disconnectionNotification = new NetworkNotification
            {
                Id = Guid.NewGuid(),
                SenderId = LocalClientInfo.ClientId,
                Timestamp = DateTime.UtcNow,
                Type = NotificationType.Disconnection,
            };
            if(DirectoryService != null && NetworkId.HasValue)
            {

                await DirectoryService.RemoveNodeFromNetwork(NetworkId.Value);
                await SendUDPToNodes(DataStore.NodeMap.Select(x => x.Key).ToList(), disconnectionNotification);                           
                DataStore.ClearAllData();
                NetworkId = null;
                Console.WriteLine("Disconnected from network\n");
            }

        }

        #endregion

        #region Processing

        private async Task ProcessItem(ISendableItem item, IPEndPoint senderEP)
        {
            //Safe cast sendable items to decode what is sent
            if (item is StringNotification message)
            {               
                ProcessStringNotification(message);
            }
            else if(item is ConnectionNotification notification)
            {
                await ProcessConnectionNotification(notification);
                
            }
            else if(item is InitialDataNotification dataNotification)
            {
                await ProcessInitialDataSync(dataNotification);
            }
            else if(item is NetworkNotification networkNotification)
            {
                await ProcessNetworkNotification(networkNotification);
            }
            else if(item is DataSyncNotification dataSyncNotification)
            {
                if (!dataSyncNotification.IsSyncingNode)
                {
                    Console.WriteLine("Rebuilding data stores from network sync\n");
                    DataStore.ClearAllData();
                    dataSyncNotification.NodeMap.Remove(LocalClientInfo.ClientId);
                    DataStore.NodeMap = dataSyncNotification.NodeMap;
                    DataStore.NetworkData = dataSyncNotification.NetworkData;

                    Console.WriteLine($"Data store rebuilt \n Connected Nodes: {DataStore.NodeMap.Count} \n Data Items: {DataStore.NetworkData.Count}\n");
                }
                else
                {
                    if (SyncService == null) return;                  
                    SyncService.SyncNotifications.Add(dataSyncNotification);
                }
                
            }
            else if(item is FileNotification fileNotification)
            {
               Console.WriteLine($"File {fileNotification.FileName} is available on node {DataStore.GetNodeName(fileNotification.SenderId)}\n");
               DataStore.NetworkData.Add(fileNotification.Id, fileNotification);
            }
        }

        #region Readability Methods 

        private void ProcessStringNotification(StringNotification message)
        {
            if (DataStore.NodeMap.ContainsKey(message.SenderId))
            {
                Console.WriteLine($"Data recieved from {message.SenderId}");
                Console.WriteLine($"Message Content: {message.Content}"); ;
                DataStore.NetworkData.Add(message.Id, message);
                Console.WriteLine($"Item {message.Id} stored\n");
            }
        }

        private async Task ProcessConnectionNotification(ConnectionNotification notification)
        {
            var senderId = notification.SenderId;
            Console.WriteLine($"New node added to network \n Name: {notification.NodeName} \n ID: {senderId} \n  EP: {notification.ConnectionInformation.Address}:{notification.ConnectionInformation.Port}");
            if (!DataStore.NodeMap.ContainsKey(senderId) && senderId != LocalClientInfo.ClientId && NetworkId.HasValue)
            {
                DataStore.NodeMap.Add(senderId, notification.ConnectionInformation.ToNodeInfo(senderId, notification.NodeName));
                Console.WriteLine("Node added to store \n");
            }

            if (notification.SendData)
            {
                var modNodeMap = DataStore.NodeMap.Where(x => x.Key != notification.SenderId)
                                          .ToDictionary(pair => pair.Key, pair => pair.Value);

                modNodeMap.Add(LocalClientInfo.ClientId, LocalClientInfo);

                var syncNotification = new InitialDataNotification
                {
                    Id = Guid.NewGuid(),
                    NetworkData = DataStore.NetworkData,
                    NodeMap = modNodeMap,
                    SenderId = LocalClientInfo.ClientId,
                    Timestamp = DateTime.UtcNow,
                    NetworkId = NetworkId.Value
                };

                //Return data to new node
                await SendUDP(notification.SenderId, syncNotification);
                Console.WriteLine("Notified New node of accepted connection and returned network data\n");
            }
        }

        private async Task ProcessInitialDataSync(InitialDataNotification dataNotification)
        {
            DataStore.NodeMap = dataNotification.NodeMap;
            DataStore.NetworkData = dataNotification.NetworkData;
            if (!NetworkId.HasValue)
            {
                NetworkId = dataNotification.NetworkId;
                await DirectoryService.AddNodeToNetwork(NetworkId.Value);
            }
            Console.WriteLine($"Data sync'd from node {dataNotification.SenderId}\n");

            var connectionMessage = new ConnectionNotification
            {
                IP = LocalClientInfo.LocalNodeIP.ToString(),
                Port = LocalClientInfo.Port,
                Id = Guid.NewGuid(),
                SenderId = LocalClientInfo.ClientId,
                SendData = false,
                Timestamp = DateTime.UtcNow,
                NodeName = LocalClientInfo.ClientName
            };

            //Notify other nodes of new connection
            await SendUDPToNodes(DataStore.NodeMap.Where(x => x.Key != dataNotification.SenderId).Select(x => x.Key).ToList(), connectionMessage);
            Console.WriteLine("Notified all other nodes of connection\n");
        }

        private async Task ProcessNetworkNotification(NetworkNotification networkNotification)
        {
            if (DataStore.NodeMap.TryGetValue(networkNotification.SenderId, out var node))
            {
                switch (networkNotification.Type)
                {
                    case NotificationType.Disconnection:
                        Console.WriteLine($"Node {node.ClientName} disconnected from network\n");
                        DataStore.NodeMap.Remove(node.ClientId);                 
                        break;

                    case NotificationType.NetworkShutdown:
                        DataStore.ClearAllData();
                        Console.WriteLine($"Network shutdown initiated by node {node.ClientName}\n");
                        Environment.Exit(0);
                        break;

                    case NotificationType.Sync:
                        var nodeMap = DataStore.NodeMap;

                        var dataSync = new DataSyncNotification
                        {
                            Id = Guid.NewGuid(),
                            SenderId = LocalClientInfo.ClientId,
                            NetworkData = DataStore.NetworkData,
                            NodeMap = nodeMap,
                            Timestamp = DateTime.UtcNow,
                            IsSyncingNode = true,
                        };
                        Console.WriteLine($"Network sync requested by node {node.ClientName}\n");
                        await SendUDP(networkNotification.SenderId, dataSync);
                        break;

                    case NotificationType.Ping:
                        var ack = new NetworkNotification
                        {
                            Id = Guid.NewGuid(),
                            SenderId = LocalClientInfo.ClientId,
                            Timestamp = DateTime.UtcNow,
                            Type = NotificationType.PingAck
                        };

                        Console.WriteLine($"Ping received by {node.ClientName}, sending ACK");
                        await SendUDP(node.ClientId, ack);
                        break;

                    case NotificationType.PingAck:
                        if (PingService == null) return;
                        PingService.PingAckNotifications.Add(networkNotification);
                        break;

                    case NotificationType.InvalidPort:
                        if(DataStore.NodeMap.Count == 0)
                        {
                            RetryDifferentPort();
                        }
                        break;

                    default:
                        Console.WriteLine($"Unknown network notification type {networkNotification.Type} sent by node {DataStore.GetNodeName(networkNotification.SenderId)}");
                        break;
                };
            }
        }

        #endregion
        
        #endregion

        #region Helpers

        public void InitialiseNode()
        {
            LocalClientInfo.Port = GetPortForNewNode();

            var ips = NetworkExtensions.GetLocalIPAddress().ToArray();
            if(ips.Length > 1)
            {
                var ipSelected = false;
                while (!ipSelected)
                {
                    Console.WriteLine("Multiple IP Addresses found, please select the IP address you wish to use:");
                    for (int i = 0; i < ips.Length; i++)
                    {
                        Console.WriteLine($"{i + 1}. {ips.ElementAt(i)}");
                    }
                    var selectedIp = Console.ReadLine();
                    if (int.TryParse(selectedIp, out int ipIndex))
                    {
                        if(ipIndex > ips.Length || ipIndex < 1)
                        {
                            Console.WriteLine("Invalid IP address selected, please try again.");
                            continue;
                        }
                        LocalClientInfo.LocalNodeIP = ips.ElementAt(ipIndex - 1).ToString();
                        ipSelected = true;
                    }
                }
            }
            else
            {
                LocalClientInfo.LocalNodeIP = ips.First().ToString();
            }
            LocalClientInfo.ClientId = Guid.NewGuid();
            UDPListen = true;
            RecieveUDP(LocalClientInfo.Port);
        }
            
        private int GetPortForNewNode()
        {
            Random random = new Random();
            int port;
            bool foundPort = false;

            while(!foundPort)
            {
                port = random.Next(10000, 11998);
                if(!DataStore.NodeMap.Select(x => x.Value.Port).Contains(port)) { 
                    foundPort = true;
                    return port;
                }
            }
            return 0;
        }

        private void RetryDifferentPort()
        {
            //Port likely in use, get new port and retry
            var newPort = GetPortForNewNode();
            Console.WriteLine($"Port {LocalClientInfo.Port} is unavailable, switching to port {newPort}\nAttempt connection again.");
            LocalClientInfo.Port = newPort;
            RecieveUDP(LocalClientInfo.Port);
        }

        #endregion
    }
}
