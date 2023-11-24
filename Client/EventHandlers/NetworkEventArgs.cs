using P2PProject.Client.Models;

namespace P2PProject.Client.EventHandlers
{
    public class NetworkEventArgs : EventArgs
    {
        public NetworkEventArgs(Guid nodeId, NetworkNotification notification)
        {
            NodeId = nodeId;
            Notification = notification;
        }

        public Guid NodeId { get; set; }
        public NetworkNotification Notification { get; set; }
    }
}
