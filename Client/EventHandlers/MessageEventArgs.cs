using P2PProject.Client.Models;
using System.Net;

namespace P2PProject.Client.EventHandlers
{
    public class MessageEventArgs : EventArgs
    {
        public StringNotification Message { get; set; }
        public Guid NodeId { get; set; }

        public MessageEventArgs(Guid nodeId, StringNotification message)
        {
             NodeId = nodeId;
            Message = message;
        }
    }
}
