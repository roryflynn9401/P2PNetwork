using P2PProject.Client;
using P2PProject.Client.Models;
using System.Linq;
using System.Timers;

namespace P2PProject.Data
{
    public static class SyncService
    {
        private static Node? _localNode;
        private static System.Timers.Timer? _timer;
        public static List<DataSyncNotification> syncNotifications = new();


        public static void InitialiseSync(Node node)
        {
            _localNode = node;
            _timer = new System.Timers.Timer(15000);
            _timer.Elapsed += OnDataSyncEvent;
            _timer.Enabled = true;
            _timer.Start();
        }

        private static void OnDataSyncEvent(object source, ElapsedEventArgs e)
        {
            if (syncNotifications.Count == DataStore.NodeMap.Count)
            {
                var networkItemIds = syncNotifications.SelectMany(x => x.NetworkData.Keys).Distinct();
                var localIds = DataStore.NetworkData.Select(x => x.Key);

                var missingLocalData = networkItemIds.Except(localIds);
                var missingNetworkData = localIds.Except(networkItemIds);

                if(missingLocalData.Any())
                {
                    var dataToAdd = syncNotifications.SelectMany(x => x.NetworkData)
                        .TakeWhile(x => missingLocalData.Contains(x.Key))
                        .Distinct()
                        .ToList();

                    foreach(var item in dataToAdd)
                    {
                        DataStore.NetworkData.TryAdd(item.Key, item.Value);
                    }
                    Console.WriteLine($"Added {dataToAdd.Count} items from network sync");
                }
                if(missingNetworkData.Any())
                {

                }
            }
            else
            {

            }
        }
    }
}
