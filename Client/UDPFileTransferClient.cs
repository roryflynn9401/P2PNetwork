using P2PProject.Client.Extensions;
using P2PProject.Client.Models;
using P2PProject.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace P2PProject.Client
{
    public class UDPFileTransferClient
    {
        private List<IPacket> _packets = new();
        private const int _maxChunkSize = 1024;
        private Node _node;
        private byte[] _fileData = new byte[0];
        private InfoPacket? _fileInfo;
        private string _fileName = string.Empty;
        private bool _transferInitiated;
        private string? _saveLocation;

        public ManualResetEvent StopThread = new ManualResetEvent(false);

        public UDPFileTransferClient(Node node, string? saveLocation = null)
        {
            _node = node;
            _node.TransferringFile = true;
            _saveLocation = saveLocation;
        }

        public async Task ProcessData(IPacket packet)
        {
            _packets.Add(packet);

            if (packet is RequestPacket requestPacket)
            {
                _fileName = requestPacket.FileName;
                var validTransfer = File.Exists(requestPacket.FileName) && DataStore.NodeMap.ContainsKey(requestPacket.SenderId);
                var ack = new AckPacket
                {
                    Id = Guid.NewGuid(),
                    SenderId = _node.LocalClientInfo.ClientId,
                    AckType = validTransfer ? AckType.FileExists : AckType.FileNotFound
                };
                await SendUDP(requestPacket.SenderId, ack);

            }
            else if(packet is AckPacket ackPacket)
            {
                switch (ackPacket.AckType)
                {
                    case AckType.FileInfoRecieved:
                        Console.WriteLine($"File info recieved by node {DataStore.GetNodeName(ackPacket.SenderId)}, Preparing for transfer\n");
                        break;

                    case AckType.FileExists:
                        var sendFileInfoAck = new AckPacket
                        {
                            Id = Guid.NewGuid(),
                            SenderId = _node.LocalClientInfo.ClientId,
                            AckType = _transferInitiated ? AckType.TransferInProgress: AckType.SendFileInfo
                        };
                        await SendUDP(ackPacket.SenderId, sendFileInfoAck);                        
                        if(!_transferInitiated) Console.WriteLine($"File found on node {DataStore.GetNodeName(ackPacket.SenderId)}, Initiating transfer\n");
                        _transferInitiated = true;
                        break;

                    case AckType.FileNotFound:
                        Console.WriteLine($"File not found on node {DataStore.GetNodeName(ackPacket.SenderId)}\n");
                        break;

                    case AckType.SendFileInfo:
                        using (var fileStream = File.OpenRead(_fileName))
                        {
                            _fileData = new byte[fileStream.Length];
                            await fileStream.ReadAsync(_fileData, 0, _fileData.Length);
                        }
                        var fileInfo = new InfoPacket
                        {
                            Id = Guid.NewGuid(),
                            SenderId = _node.LocalClientInfo.ClientId,
                            FileSize = _fileData.Length,
                            FileChecksum = _fileData.GetChecksumString(),
                            ChunkCount = (int)Math.Ceiling((double)_fileData.Length / _maxChunkSize),
                            MaxChunkSize = _maxChunkSize
                        };
                        await SendUDP(ackPacket.SenderId, fileInfo);
                        break;

                    default:
                        break;
                }
            }
            else if(packet is InfoPacket infoPacket)
            {
                var ack = new AckPacket
                {
                    Id = Guid.NewGuid(),
                    SenderId = _node.LocalClientInfo.ClientId,
                    AckType = AckType.FileInfoRecieved
                };
                _fileInfo = infoPacket;
                await SendUDP(infoPacket.SenderId, ack);

                var requestPayload = new RequestPayloadPacket
                {
                    Id = Guid.NewGuid(),
                    SenderId = _node.LocalClientInfo.ClientId,
                    ChunkIndex = 1
                };
                await SendUDP(infoPacket.SenderId, requestPayload);
            }
            else if(packet is RequestPayloadPacket requestPayloadPacket)
            {
                var sendBytes = _fileData.Skip((requestPayloadPacket.ChunkIndex - 1) * _maxChunkSize).Take(_maxChunkSize).ToArray();
                var payloadPacket = new PayloadPacket
                {
                    Id = Guid.NewGuid(),
                    SenderId = _node.LocalClientInfo.ClientId,
                    ChunkIndex = requestPayloadPacket.ChunkIndex,
                    Data = sendBytes
                };
                await SendUDP(requestPayloadPacket.SenderId, payloadPacket);
            }
            else if(packet is PayloadPacket payloadPacket)
            {
                var maxNoChunks = _fileInfo?.ChunkCount;
                if(maxNoChunks != null)
                {
                    var progress = (double)payloadPacket.ChunkIndex / maxNoChunks * 100;
                    Console.WriteLine($"File transfer progress: {progress}%");
                }

                if(payloadPacket.ChunkIndex != maxNoChunks)
                {
                    var requestPayload = new RequestPayloadPacket
                    {
                        Id = Guid.NewGuid(),
                        SenderId = _node.LocalClientInfo.ClientId,
                        ChunkIndex = payloadPacket.ChunkIndex + 1
                    };
                    await SendUDP(payloadPacket.SenderId, requestPayload);
                }
                else
                {
                    var transferComplete = new ByePacket
                    {
                        Id = Guid.NewGuid(),
                        SenderId = _node.LocalClientInfo.ClientId
                    };
                    await SendUDP(payloadPacket.SenderId, transferComplete);

                    Console.WriteLine("File transfer complete\n Reconstructing file");
                    var fileData = _packets.Where(x => x is PayloadPacket)
                                            .Cast<PayloadPacket>()
                                            .OrderBy(x => x.ChunkIndex)
                                            .SelectMany(x => x.Data)
                                            .ToArray();
                    if(fileData.GetChecksumString() == _fileInfo?.FileChecksum && _saveLocation != null)
                    {
                        using (StreamWriter file = new StreamWriter(_saveLocation))
                        {
                            Console.WriteLine($"File reconstructed successfully @ {_saveLocation}\n");
                            file.Write(Encoding.UTF8.GetString(fileData));
                        }
                    }
                    else
                    {
                        Console.WriteLine("File checksums do not match, file may be corrupted");
                    }
                }
            }
            else if(packet is ByePacket byePacket)
            {
                _node.TransferringFile = false;
                if(!_packets.Any(x => x is ByePacket))
                {
                    var byeAck = new ByePacket
                    {
                        Id = Guid.NewGuid(),
                        SenderId = _node.LocalClientInfo.ClientId
                    };
                    await SendUDP(byePacket.SenderId, byeAck);
                }
            }
        }

        public async Task SendUDP(Guid recipientId, IPacket item)
        {
            try
            {
                if (DataStore.NodeMap.TryGetValue(recipientId, out NodeInfo? send))
                {
                    Console.WriteLine($"Sending file data to {send.ClientName} at {send.LocalNodeIP}:{send.Port}\n");
                    var sendData = ByteExtensions.GetByteArray(item);

                    if (sendData != null)
                    {
                        using var client = new UdpClient();
                        await client.SendAsync(sendData, sendData.Length, send.LocalIPEndPoint);
                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public async Task SendUDPToNodes(List<Guid> ids, IPacket item)
        {
            var tasks = new List<Task>();
            ids.ForEach(x => tasks.Add(SendUDP(x, item)));
            await Task.WhenAll(tasks);
        }
    }

    public interface IPacket
    {
        public Guid Id { get; set; }
        public Guid SenderId { get; set; }
    }
    public abstract class Packet : IPacket
    {
        public Guid Id { get; set; }
        public Guid SenderId { get; set; }
    }

    public class RequestPacket : Packet
    {
        public string FileName { get; set; }
    }
    public class AckPacket : Packet
    {
        public AckType AckType { get; set; }
    }
    public class InfoPacket : Packet
    {
        public string FileChecksum { get; set; }
        public int FileSize { get; set; }
        public int MaxChunkSize { get; set; }
        public int ChunkCount { get; set; }
    }
    public class RequestPayloadPacket : Packet
    {
        public int ChunkIndex { get; set; }
    }
    public class PayloadPacket : Packet
    {
        public int ChunkIndex { get; set; }
        public byte[] Data { get; set; }
    }
    public class ByePacket : Packet
    {
    }
    public enum AckType
    {
        FileInfoRecieved,
        FileExists,
        FileNotFound,
        TransferInProgress,
        SendFileInfo,
    }
}
