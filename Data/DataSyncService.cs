using P2PProject.Client;
using P2PProject.Client.Models;
using System.Timers;

namespace P2PProject.Data
{
    public class DataSyncService : SyncService
    {
        public List<DataSyncNotification> SyncNotifications = new();

        public DataSyncService(Node node) : base(node)
        {
        }

        public override async Task InitaliseSync()
        {
            await base.InitaliseSync();

            Console.WriteLine($"Initalising data sync for {DataStore.NodeMap.Count} nodes");
            var networkNotification = new NetworkNotification
            {
                Id = Guid.NewGuid(),
                SenderId = _localNode.LocalClientInfo.ClientId,
                Timestamp = DateTime.UtcNow,
                Type = NotificationType.Sync,
            };

            await _localNode.SendUDPToNodes(DataStore.NodeMap.Select(x => x.Key).ToList(), networkNotification);
        }

        protected override async void OnSyncEvent(object source, ElapsedEventArgs e)
        {
            Timer?.Stop();
            if (_localNode == null) return;
            if (SyncNotifications.Count == DataStore.NodeMap.Count)
            {
                var networkItemIds = SyncNotifications.SelectMany(x => x.NetworkData.Keys).Distinct();
                var localIds = DataStore.NetworkData.Select(x => x.Key);


                var missingLocalData = networkItemIds.Except(localIds);
                var missingNetworkData = localIds.Except(networkItemIds);

                if (missingLocalData.Any())
                {
                    var dataToAdd = SyncNotifications.SelectMany(x => x.NetworkData)
                        .Where(x => missingLocalData.Contains(x.Key))
                        .Distinct()
                        .ToList();
                    
                    foreach(var item in dataToAdd)
                    {
                        DataStore.NetworkData.TryAdd(item.Key, item.Value);
                    }
                    Console.WriteLine($"Added {dataToAdd.Count} items from network sync\n");
                }
                if(missingNetworkData.Any())
                {
                    var nodeIds = SyncNotifications.Where(x => x.NetworkData.Any(x => missingNetworkData.Contains(x.Key)))
                                                   .Select(x => x.SenderId)
                                                   .ToList();

                    Console.WriteLine($"{nodeIds.Count} unsynchronised nodes detected. Returning correct data list\n");
                    foreach(var id in nodeIds)
                    {
                        var nodeMap = new Dictionary<Guid, NodeInfo>(DataStore.NodeMap)
                        {
                            { _localNode.LocalClientInfo.ClientId, _localNode.LocalClientInfo }
                        };
                        nodeMap.Remove(id);

                        var syncNotification = new DataSyncNotification
                        {
                            Id = Guid.NewGuid(),
                            SenderId = _localNode.LocalClientInfo.ClientId,
                            Timestamp = DateTime.UtcNow,
                            NetworkData = DataStore.NetworkData,
                            NodeMap = nodeMap,
                            IsSyncingNode = false,
                        };

                         await _localNode.SendUDP(id, syncNotification);
                    }                    
                }
            }
            else
            {
                _localNode.PingService = new PingService(_localNode);
                _localNode.PingService.FromDataSync = true;
                await _localNode.PingService.InitaliseSync();
            }
            SyncNotifications.Clear();
        }
      
    }
}
