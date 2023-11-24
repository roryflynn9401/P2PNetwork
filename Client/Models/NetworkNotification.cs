using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PProject.Client.Models
{
    public enum NotificationType
    {
        NetworkShutdown = 1,
        Disconnection = 2,
        Sync = 3,
    }
    public class NetworkNotification : ISendableItem
    {
        public Guid Id { get; set; }
        public Guid SenderId { get; set; }
        public DateTime? Timestamp { get; set; }
        public NotificationType Type { get; set; }
    }
}
