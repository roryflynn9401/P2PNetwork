using P2PProject.Client.EventHandlers;
using P2PProject.Client.Extensions;
using P2PProject.Client.Models;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace P2PProject.Client
{
    public class Node
    {
        private Thread UDPListenerThread;
        public ClientInfo LocalClientInfo { get; set; } = new();
        public event EventHandler<MessageEventArgs> OnMessageReceived;
        public bool UDPListen { get; set; }

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
                        if (returnData != null)
                        {
                            await ProcessItem(returnData, RemoteIpEndPoint);
                        }
                    }

                }
                catch(SocketException se)
                {
                    //Port likely in use, retry
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
            item.SenderId = LocalClientInfo.ClientId;
            var sendData = ByteExtensions.GetByteArray(item);
            try
            {    
                if (sendData != null && DataStore.NodeMap.TryGetValue(recipientId, out IPEndPoint send))
                {
                    //Only local addresses for now
                    Console.WriteLine($"Sending data to {send.Address} at port {send.Port}");

                    using var client = new UdpClient();
                    await client.SendAsync(sendData, sendData.Length, send);
                }
                    
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public async Task SendUDPToAllNodes(List<Guid> ids, ISendableItem item)
        {
            var tasks = new List<Task>();
            ids.ForEach(x => tasks.Add(SendUDP(x, item)));
            await Task.WhenAll(tasks);
        }

        #endregion

        public async Task IPInitialConnection(IPEndPoint ep, ConnectionNotification connectionInfo)
        {
            var tempId = Guid.NewGuid();
            DataStore.NodeMap.Add(tempId, ep);
            await SendUDP(tempId, connectionInfo);
            UDPListen = true;
        }

        #region Processing

        public async Task ProcessItem(ISendableItem item, IPEndPoint senderEP)
        {
            //Safe cast sendable items to decode what is sent
            if (item is StringNotification message)
            {
                Console.WriteLine($"Data recieved from {message.SenderId}");
                Console.WriteLine($"Message Content: {message.Content}");
                Console.WriteLine("This message was sent from " +
                                    senderEP.Address.ToString() +
                                    " on their port number " +
                                    senderEP.Port.ToString());
                DataStore.NetworkData.Add(item.Id, item);
                Console.WriteLine($"Item {message.Id} stored");
                //OnMessageReceived(this, new MessageEventArgs(senderInfo, message, RemoteIpEndPoint));
            }
            else if(item is ConnectionNotification notification)
            {
                Console.WriteLine($"New node added to network \n ID: {notification.SenderId} \n  EP: {notification.ConnectionInformation.Address}:{notification.ConnectionInformation.Port}");
                DataStore.NodeMap.Add(notification.SenderId, senderEP);
                Console.WriteLine("Node added to store \n returning network information and data");

                var syncNotification = new InitialDataNotification
                {
                    Id = Guid.NewGuid(),
                    NetworkData = DataStore.NetworkData,
                    NodeMap = DataStore.NodeMap.Where(x => x.Key != notification.SenderId)
                                               .ToDictionary(pair => pair.Key, pair => pair.Value),
                };
                //Return data and port to new node
                await SendUDP(notification.SenderId, syncNotification);
                Console.WriteLine("Notified New node of accepted connection and returned available port");

            }
            else if(item is InitialDataNotification dataNotification)
            {
                foreach(var node in dataNotification.NodeMap)
                {
                    if(!node.Key.Equals(LocalClientInfo.ClientId))
                    { 
                        DataStore.NodeMap.TryAdd(node.Key, node.Value);
                    }
                }
                foreach (var data in dataNotification.NetworkData)
                {
                    DataStore.NetworkData.TryAdd(data.Key, data.Value);
                }
                Console.WriteLine($"Data sync'd from node {dataNotification.SenderId}");

                //Get available port for communications, freeing up connection port
                var freePortForNode = GetPortForNewNode();
                LocalClientInfo.Port = freePortForNode;

                var connectionMessage = new ConnectionNotification
                {
                    IP = LocalClientInfo.LocalClientIP.ToString(),
                    Port = freePortForNode,
                    Id = Guid.NewGuid(),
                    SenderId = LocalClientInfo.ClientId
                };

                //Notify other nodes of new connection
                await SendUDPToAllNodes(DataStore.NodeMap.Select(x => x.Key).ToList(), connectionMessage);
                Console.WriteLine("Notified all other nodes of new connection");
            }
        }

        #endregion

        #region Helpers

        public void InitialiseNode()
        {
            LocalClientInfo.Port = GetPortForNewNode();
            LocalClientInfo.LocalClientIP = NetworkExtensions.GetLocalIPAddress();
            LocalClientInfo.ClientId = Guid.NewGuid();
            UDPListen = true;
            RecieveUDP(LocalClientInfo.Port);
        }

        public int GetPortForNewNode()
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

        #endregion
    }
}
