using P2PProject.Client;
using P2PProject.Client.Models;
using System.Timers;

namespace P2PProject.Data
{
    public class PingService : SyncService
    {
        public List<NetworkNotification> PingAckNotifications = new();
        public bool FromDataSync = false;

        public PingService(Node node) : base(node)
        {
        }

        public override async Task InitaliseSync()
        {
            await base.InitaliseSync();
            Console.WriteLine($"Pinging {DataStore.NodeMap.Count} nodes");

            var pingNotification = new NetworkNotification
            {
                Id = Guid.NewGuid(),
                SenderId = _localNode.LocalClientInfo.ClientId,
                Timestamp = DateTime.UtcNow,
                Type = NotificationType.Ping,
            };

            await _localNode.SendUDPToNodes(DataStore.NodeMap.Select(x => x.Key).ToList(), pingNotification);
        }

        protected override async void OnSyncEvent(object source, ElapsedEventArgs e)
        {
            Timer?.Stop();
            if (_localNode == null) return;
            
            var noResponseIds = DataStore.NodeMap.Select(x => x.Key)
                                                     .Except(PingAckNotifications.Select(x => x.SenderId))
                                                     .ToList();
            if(noResponseIds.Count == 0)
            {
                Console.WriteLine("All nodes responded to ping");
            }
            else
            {
                Console.WriteLine($"{noResponseIds.Count} nodes did not respond to ping. \nNotifying network of inactive nodes... \nRemoving nodes from data store");
                
                var responses = PingAckNotifications.Select(x =>x.SenderId).ToList();
                noResponseIds.ForEach(x => DataStore.NodeMap.Remove(x));
                var nodeMap = new Dictionary<Guid, NodeInfo>(DataStore.NodeMap)
                {
                    { _localNode.LocalClientInfo.ClientId, _localNode.LocalClientInfo }
                };

                var dataSync = new DataSyncNotification
                {
                    Id = Guid.NewGuid(),
                    SenderId = _localNode.LocalClientInfo.ClientId,
                    IsSyncingNode = false,
                    NetworkData = DataStore.NetworkData,
                    NodeMap = nodeMap,
                    Timestamp = DateTime.UtcNow
                };

                await _localNode.SendUDPToNodes(responses, dataSync);
            }

            if (FromDataSync && _localNode.SyncService != null)
            {
                Console.WriteLine("Inactive nodes removed from network, retrying data sync");
                await _localNode.SyncService.InitaliseSync();
            }
            FromDataSync = false;
            PingAckNotifications.Clear();
        }

    }
}
