namespace P2PProject.Client.Models
{
    public class SendableItem : ISendableItem
    {
        public Guid Id { get; set; }
        public Guid SenderId { get; set; }
        public DateTime? Timestamp { get; set; }
        public object? Item { get; set; }
        public bool ExternalItem { get; set; }
    }
}
