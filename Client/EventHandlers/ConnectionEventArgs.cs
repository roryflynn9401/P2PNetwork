using P2PProject.Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PProject.Client.EventHandlers
{
    public class ConnectionEventArgs : EventArgs
    {
        public ConnectionEventArgs(Guid nodeId, ConnectionNotification notification)
        {
            NodeId = nodeId;
            Notification = notification;
        }

        public Guid NodeId { get; set; }
        public ConnectionNotification Notification { get; set; }
    }
}
