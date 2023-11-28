using P2PProject.Client.Models;
using System.Net;

namespace P2PProject.Data
{
    public static class DataStore
    {
        public static Dictionary<Guid, NodeInfo> NodeMap { get; set; } = new();
        public static Dictionary<Guid, ISendableItem> NetworkData { get; set; } = new();

        public static void ClearData() => NetworkData.Clear();
        public static void ClearNodeList() => NodeMap.Clear();
        public static void ClearAllData()
        {
            ClearData();
            ClearNodeList();
        }
    }
}
