using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PProject.Client.Models
{
    public interface ISendableItem
    {
         long Id { get; set; }
         long SenderId { get; set; }
    }
}
