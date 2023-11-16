using P2PProject.Client.EventHandlers;
using P2PProject.Client.Extensions;
using P2PProject.Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace P2PProject.Client
{
    public class Client
    {
        private ClientInfo _clientInfo { get; set; } = new();
        private Thread UDPListenerThread;

        public List<ClientInfo> Clients { get; set; } = new();
        public Dictionary<long, IPEndPoint> NodeMap { get; set; } = new();
        public Dictionary<long, object> NetworkData { get; set; } = new();
        
        public event EventHandler<MessageEventArgs> OnMessageReceived;

        public bool UDPListen { get; set; }



        public Client(int _localPort, bool _udpListen = false) 
        {
            UDPListen = _udpListen;
            //_udpClient.Client.SetIPProtectionLevel(IPProtectionLevel.Unrestricted);
            //_udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            _clientInfo.LocalCLientEndpoint = new IPEndPoint(NetworkExtensions.GetLocalIPAddress(), _localPort);
            _clientInfo.ClientId = _localPort;
        }

        public void RecieveUDP(int receivingPort)
        {
            //Start Listener thread for incoming UDP packets
            UDPListenerThread = new Thread(new ThreadStart(delegate
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
                            //Safe cast sendable items to decode what is sent
                            if (returnData is Message message)
                            {
                                Console.WriteLine($"Data recieved from {message.SenderId}");
                                Console.WriteLine($"Message Content: {message.Content}");
                                Console.WriteLine("This message was sent from " +
                                                    RemoteIpEndPoint.Address.ToString() +
                                                    " on their port number " +
                                                    RemoteIpEndPoint.Port.ToString());

                                //OnMessageReceived(this, new MessageEventArgs(senderInfo, message, RemoteIpEndPoint));
                            }
                        }
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }));
            UDPListenerThread.Start();
        }

        public void SendUDP(long recipientId, ISendableItem item)
        {
            item.SenderId = _clientInfo.ClientId;
            var sendData = ByteExtensions.GetByteArray(item);
            try
            {    
                if (sendData != null && NodeMap.TryGetValue(recipientId, out var send))
                {
                    //Only local addresses for now
                    Console.WriteLine($"Sending data to {send.Address} at port {send.Port}");

                    using var client = new UdpClient();
                    client.Send(sendData, sendData.Length, send);
                }
                    
            }
            catch (Exception e)
            {
                
            }
        }

    }
}
