using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PProject.Client.Models
{
    [Serializable]
    public class Message : ISendableItem
    {
        public long Id { get; set; }
        public string Content { get; set; }
        public long SenderId { get; set; }
    }
}
