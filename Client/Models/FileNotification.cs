namespace P2PProject.Client.Models
{
    public class FileNotification : ISendableItem
    {
        public Guid Id { get; set; }
        public Guid SenderId { get; set; }
        public DateTime? Timestamp { get; set; }
        public string FileName { get; set; }
    }
}
