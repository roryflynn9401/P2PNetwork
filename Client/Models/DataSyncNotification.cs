using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace P2PProject.Client.Models
{
    public class DataSyncNotification : ISendableItem
    {
        public Guid Id { get; set; }
        public Guid SenderId { get; set; }
        public Dictionary<Guid, IPEndPoint> NodeMap { get; set; } = new();
        public Dictionary<Guid, ISendableItem> NetworkData { get; set; } = new();
        public DateTime? Timestamp { get; set; }
    }
}
