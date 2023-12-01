using Newtonsoft.Json;
using P2PProject.Client.Models;
using P2PProject.Data;
using System.Net.Http.Json;

namespace P2PProject.Client
{
    public class DirectoryService
    {
        private Node _node;
        private NodeInfo _nodeInfo => _node.LocalClientInfo;

        private const string _directoryServiceUrl = "https://localhost:7234";

        public DirectoryService(Node node)
        {
            _node = node;
        }

        public async Task<Guid?> InitialiseNetwork()
        {

            using var client = new HttpClient();
            try
            {
                var response = await client.PostAsJsonAsync($"{_directoryServiceUrl}/CreateNetwork", ToNodeDTO(_nodeInfo));
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var networkResponse = JsonConvert.DeserializeObject<Guid>(responseContent);
                    Console.WriteLine($"Network created with id: {networkResponse}");
                   return networkResponse;
                }
            }
            catch
            {
                Console.WriteLine("Error connecting to directory service.");
            }
            return default;
        }

        public async Task<List<NetworkInfo>?> GetNetworks()
        {
            using var client = new HttpClient();
            try
            {
                var response = await client.GetAsync($"{_directoryServiceUrl}/GetNetworks");
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var networks = JsonConvert.DeserializeObject<List<NetworkInfo>>(responseContent);
                    return networks;
                }
            }
            catch
            {
                Console.WriteLine("Error connecting to directory service.");
            }

            return default;
        }

        public async Task<Guid?> ConnectViaDirectory(Guid networkId)
        {
            using var client = new HttpClient();            
            try
            {

                var response = await client.GetAsync($"{_directoryServiceUrl}/GetNodes/{networkId}");
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var nodesData = JsonConvert.DeserializeObject<List<NodeDTO>>(responseContent);

                    var nodes = nodesData?.ToDictionary(x => x.Id, x => new NodeInfo(x.IP, x.Port, x.Id, x.NodeName));
                    if (nodes == null)
                    {
                        Console.WriteLine("Error getting nodes or no nodes on network.");
                        return default;
                    }

                    DataStore.NodeMap = nodes;
                    var dataRequestId = nodes.First().Value.ClientId;
                    var connectionNotification = new ConnectionNotification
                    {
                        Id = Guid.NewGuid(),
                        SenderId = _nodeInfo.ClientId,
                        IP = _nodeInfo.LocalNodeIP,
                        Port = _nodeInfo.Port,
                        NodeName = _nodeInfo.ClientName,
                        SendData = true,
                        Timestamp = DateTime.UtcNow
                    };

                    Console.Write("Connecting to network and receiving network data... \n");
                    //Get Data from one node
                    await _node.SendUDP(dataRequestId, connectionNotification);
                    //Notify all other nodes of connection
                    connectionNotification.SendData = false;
                    await AddNodeToNetwork(networkId);
                    return networkId;
                }
            }
            catch
            {
                Console.WriteLine("Error connecting to directory service.");
            }
            return default;
        }

        public async Task<bool> AddNodeToNetwork(Guid networkId)
        {
            using var client = new HttpClient();
            try
            {
                _node.NetworkId = networkId;
                return (await client.PostAsJsonAsync($"{_directoryServiceUrl}/AddNode/{networkId}", ToNodeDTO(_nodeInfo))).IsSuccessStatusCode;
            }
            catch
            {
                Console.WriteLine("Error connecting to directory service.");
            }

            return false;
        }

        public async Task<bool> RemoveNodeFromNetwork(Guid networkId, Guid? nodeId = null)
        {
            try 
            {
                using var client = new HttpClient();
                return (await client.PostAsJsonAsync($"{_directoryServiceUrl}/RemoveNode/{networkId}", ToNodeDTO(nodeId.HasValue ? DataStore.NodeMap[nodeId.Value] : _nodeInfo))).IsSuccessStatusCode;
            }
            catch
            {
                Console.WriteLine("Error connecting to directory service.");
            }
            return false;
        }

        public async Task<bool> NetworkShutdown(Guid networkId)
        {
            try
            {
                using var client = new HttpClient();
                return (await client.PostAsync($"{_directoryServiceUrl}/DeleteNetwork/{networkId}", null)).IsSuccessStatusCode;
            }
            catch
            {
                Console.WriteLine("Error connecting to directory service.");
            }
            return false;
        }

        private NodeDTO ToNodeDTO(NodeInfo nodeInfo)
        {
            return new NodeDTO
            {
                Id = nodeInfo.ClientId,
                IP = nodeInfo.LocalNodeIP,
                Port = nodeInfo.Port,
                NodeName = nodeInfo.ClientName,
                NetworkId = _node.NetworkId ?? Guid.Empty
            };
        }
    }

    #region Record DTOs

    public record NetworkInfo
    {
        public Guid Id { get; init; }
        public int NodeCount { get; init; }
    }

    internal record Network
    {
        public Guid Id { get; init; }
        public List<NodeInfo> Nodes { get; init; }
    }

    internal record NodeDTO
    {
        public Guid Id { get; init; }
        public string IP { get; init; }
        public int Port { get; init; }
        public string NodeName { get; init; }
        public Guid NetworkId { get; init; }
    }

    #endregion
}
