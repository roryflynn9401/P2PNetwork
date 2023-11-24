using P2PProject.Client.EventHandlers;
using P2PProject.Client.Extensions;
using P2PProject.Client.Models;
using P2PProject.Data;
using System.Net;
using System.Net.Sockets;

namespace P2PProject.Client
{
    public class Node
    {
        private Thread UDPListenerThread;
        public NodeInfo LocalClientInfo { get; set; } = new();
        public bool UDPListen { get; set; }
        
        public event EventHandler<MessageEventArgs> MessageReceived;
        public event EventHandler<ConnectionEventArgs> NodeConnected;
        public event EventHandler<ConnectionEventArgs> NetworkConnect;
        public event EventHandler<NetworkEventArgs> NetworkDisconnect;
        public event EventHandler<NetworkEventArgs> NetworkShutdown;

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
                    }
                }
                catch(SocketException se)
                {
                    //Port likely in use, retry print error for now
                    Console.WriteLine(se.ToString());
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
                    Console.WriteLine($"Sending data to node {send.ClientName} {send.LocalNodeIP}:{send.Port}");
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

            DataStore.ClearForDisconnect();
            await SendUDPToNodes(DataStore.NodeMap.Select(x => x.Key).ToList(), disconnectionNotification);                           
        }



        #endregion

        #region Processing

        private async Task ProcessItem(ISendableItem item, IPEndPoint senderEP)
        {
            //Safe cast sendable items to decode what is sent
            if (item is StringNotification message)
            {               
                OnMessageReceived(new MessageEventArgs(message.SenderId, message));
                ProcessStringNotification(message);
            }
            else if(item is ConnectionNotification notification)
            {
                OnNodeConnected(new ConnectionEventArgs(notification.SenderId, notification));
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
        }

        #region Process methods for readability

        private void ProcessStringNotification(StringNotification message)
        {
            if (DataStore.NodeMap.ContainsKey(message.SenderId))
            {
                Console.WriteLine($"Data recieved from {message.SenderId}");
                Console.WriteLine($"Message Content: {message.Content}"); ;
                DataStore.NetworkData.Add(message.Id, message);
                Console.WriteLine($"Item {message.Id} stored");
            }
        }

        private async Task ProcessConnectionNotification(ConnectionNotification notification)
        {
            var senderId = notification.SenderId;
            Console.WriteLine($"New node added to network Name: {notification.NodeName} \n ID: {senderId} \n  EP: {notification.ConnectionInformation.Address}:{notification.ConnectionInformation.Port}");
            if (!DataStore.NodeMap.ContainsKey(senderId))
            {
                DataStore.NodeMap.Add(senderId, notification.ConnectionInformation.ToNodeInfo(senderId));
                Console.WriteLine("Node added to store \n returning network information and data");
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
                };

                //Return data to new node
                await SendUDP(notification.SenderId, syncNotification);
                Console.WriteLine("Notified New node of accepted connection and returned network data");
            }
        }

        private async Task ProcessInitialDataSync(InitialDataNotification dataNotification)
        {
            foreach (var node in dataNotification.NodeMap)
            {
                if (!node.Key.Equals(LocalClientInfo.ClientId))
                {
                    DataStore.NodeMap.TryAdd(node.Key, node.Value);
                }
            }
            foreach (var data in dataNotification.NetworkData)
            {
                DataStore.NetworkData.TryAdd(data.Key, data.Value);
            }
            Console.WriteLine($"Data sync'd from node {dataNotification.SenderId}");

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

            Program.Connected = true;
            //Notify other nodes of new connection
            await SendUDPToNodes(DataStore.NodeMap.Where(x => x.Key != dataNotification.SenderId).Select(x => x.Key).ToList(), connectionMessage);
            Console.WriteLine("Notified all other nodes of connection");
        }

        private async Task ProcessNetworkNotification(NetworkNotification networkNotification)
        {
            switch (networkNotification.Type)
            {
                case NotificationType.Disconnection:
                    OnNodeDisconnect(new NetworkEventArgs(LocalClientInfo.ClientId, networkNotification));
                    if (DataStore.NodeMap.TryGetValue(networkNotification.SenderId, out var node))
                    {
                        Console.WriteLine($"Node {node.ClientName} disconnected from network");
                        DataStore.NodeMap.Remove(node.ClientId);
                    }
                    break;
                case NotificationType.NetworkShutdown:
                    OnNetworkShutdown(new NetworkEventArgs(LocalClientInfo.ClientId, networkNotification));
                    break;
                case NotificationType.Sync:
                    break;
                default:
                    break;
            };
        }

        #endregion
        #endregion

        #region Helpers

        public void InitialiseNode()
        {
            LocalClientInfo.Port = GetPortForNewNode();
            LocalClientInfo.LocalNodeIP = NetworkExtensions.GetLocalIPAddress().ToString();
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

        protected virtual void OnMessageReceived(MessageEventArgs e) => OnEventOccured(e, MessageReceived);
        protected virtual void OnNodeConnected(ConnectionEventArgs e) => OnEventOccured(e, NodeConnected);
        protected virtual void OnNodeDisconnect(NetworkEventArgs e) => OnEventOccured(e, NetworkDisconnect);
        protected virtual void OnNetworkShutdown(NetworkEventArgs e) => OnEventOccured(e, NetworkShutdown);
        protected void OnEventOccured<TEventArgs>(TEventArgs e, EventHandler<TEventArgs> h)
        {
            EventHandler<TEventArgs> handler = h;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        #endregion
    }
}
