using P2PProject.Client.Models;
using System.Net;

namespace P2PProject.Client.EventHandlers
{
    public class MessageEventArgs : EventArgs
    {
        public StringNotification Message { get; set; }
        public ClientInfo ClientInfo { get; set; }
        public IPEndPoint RecievedEndpoint { get; set; }

        public MessageEventArgs(ClientInfo _clientInfo, StringNotification _message, IPEndPoint _recievedEndpoint)
        {
            ClientInfo = _clientInfo;
            Message = _message;
            RecievedEndpoint = _recievedEndpoint;
        }
    }
}
