using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PProject.Client.Models
{
    public interface ISendableItem
    {
        Guid Id { get; set; }
        Guid SenderId { get; set; }
        DateTime? Timestamp { get; set; }
    }
}
