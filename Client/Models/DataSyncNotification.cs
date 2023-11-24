using P2PProject.Client.Extensions;

namespace P2PProject.Client.Models
{
    public class DataSyncNotification : ISendableItem
    {
        public Guid Id { get; set; }
        public Guid SenderId { get; set; }
       
        public Dictionary<Guid, NodeInfo> NodeMap { get; set; } = new();
        public Dictionary<Guid, ISendableItem> NetworkData { get; set; } = new();
        public DateTime? Timestamp { get; set; }
    }
}
