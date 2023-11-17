using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace P2PProject.Client.Models
{
    public static class DataStore
    {
        public static Dictionary<Guid, IPEndPoint> NodeMap { get; set; } = new();
        public static Dictionary<Guid, ISendableItem> NetworkData { get; set; } = new();
    }
}
