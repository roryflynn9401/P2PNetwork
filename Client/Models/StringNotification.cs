using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PProject.Client.Models
{
    public class StringNotification : ISendableItem
    {
        public Guid Id { get; set; }
        public string Content { get; set; }
        public Guid SenderId { get; set; }
        public DateTime? Timestamp { get; set; }
    }
}
