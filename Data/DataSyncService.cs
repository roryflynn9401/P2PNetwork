using P2PProject.Client;
using P2PProject.Client.Models;
using System.Timers;

namespace P2PProject.Data
{
    public class DataSyncService : SyncService
    {
        public List<DataSyncNotification> SyncNotifications = new();
        private bool _isLocalSync;
        public DataSyncService(Node node, bool isLocalSync = false) : base(node)
        {
            _isLocalSync = isLocalSync;
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
            Console.WriteLine("Sync Initalised, this may take up to 10s...\n");
        }

        protected override async void OnSyncEvent(object source, ElapsedEventArgs e)
        {
            Timer?.Stop();
            if (_localNode == null) return;
            if (_isLocalSync)
            {
                Console.WriteLine("Clearing local stores and rebuilding data");
                DataStore.ClearData();
            }

            if (SyncNotifications.Count == DataStore.NodeMap.Count)
            {
                foreach(var notification in SyncNotifications)
                {
                    //Sync all local data first
                    var missingLocalData = notification.NetworkData.Keys.Except(DataStore.NetworkData.Keys).ToList();
                    
                    if (missingLocalData.Any())
                    {
                        foreach(var key in missingLocalData)
                        {
                            DataStore.NetworkData.TryAdd(key, notification.NetworkData[key]);
                        }
                        Console.WriteLine($"Added {missingLocalData.Count} missing data items to local data store from node {DataStore.GetNodeName(notification.SenderId)}");
                    }            
                }

                foreach(var notification in SyncNotifications)
                {
                    //After local store is up to date, sync all other nodes
                    var missingNetworkData = DataStore.NetworkData.Keys.Except(notification.NetworkData.Keys);
                    if (missingNetworkData.Any())
                    {
                        var nodeMap = new Dictionary<Guid, NodeInfo>(DataStore.NodeMap)
                        {
                            { _localNode.LocalClientInfo.ClientId, _localNode.LocalClientInfo }
                        };
                        nodeMap.Remove(notification.SenderId);

                        var syncNotification = new DataSyncNotification
                        {
                            Id = Guid.NewGuid(),
                            SenderId = _localNode.LocalClientInfo.ClientId,
                            Timestamp = DateTime.UtcNow,
                            NetworkData = DataStore.NetworkData,
                            NodeMap = nodeMap,
                            IsSyncingNode = false,
                        };

                        await _localNode.SendUDP(notification.SenderId, syncNotification);
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
