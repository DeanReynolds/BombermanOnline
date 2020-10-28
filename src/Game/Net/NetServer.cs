using System;
using System.Collections.Generic;
using LiteNetLib;

namespace BombermanOnline {
    static class NetServer {
        public const int PORT = 8989;

        internal const int PLAYER_SUB_IDS = 1;

        public static readonly int PACKET_MAX_ID;

        public static bool IsRunning => _manager.IsRunning;

        public enum PacketId { PLAYER }

        static readonly EventBasedNetListener _listener = new EventBasedNetListener();
        static readonly NetManager _manager = new NetManager(_listener) { AutoRecycle = true, UpdateTime = 15 };
        static readonly Dictionary<NetPeer, int> _players = new Dictionary<NetPeer, int>();
        static readonly Dictionary<int, NetPeer> _peers = new Dictionary<int, NetPeer>();
        static readonly Dictionary<int, NetWriter> _packets = new Dictionary<int, NetWriter>();
        static readonly int _initialDataStart = 8,
            _packetClearStart;
        static readonly NetWriter _initialData = new NetWriter();
        static readonly NetReader _r = new NetReader();

        static NetServer() {
            var packets = Enum.GetValues(typeof(PacketId));
            PACKET_MAX_ID = packets.Length - 1;
            foreach (var p in packets) {
                var w = new NetWriter();
                _packetClearStart = w.Put(0, PACKET_MAX_ID, (int)p);
                _packets.Add((int)p, w);
            }
            _listener.NetworkReceiveEvent += (peer, readerOutdated, delieryMethod) => {
                _r.ReadFrom(readerOutdated);
                var packetId = (NetClient.PacketId)_r.ReadInt(0, NetClient.PACKET_MAX_ID);
                var p = _players[peer];
            };
            _listener.PeerConnectedEvent += peer => {
                var p = _players[peer];
                _peers.Add(p, peer);
                _initialData.Clear(_initialDataStart); {
                    PutPlayerId(_initialData, p);
                    for (int j = 0; j < Players.MaxPlayers; j++) {
                        if (j == p)
                            continue;
                        _initialData.PutPlayerId(j);
                    }
                    Send(_initialData, peer, DeliveryMethod.ReliableOrdered);
                }
                var w = CreatePacket(PacketId.PLAYER); {
                    w.Put(0, PLAYER_SUB_IDS, 0);
                    w.Put(true);
                    w.PutPlayerId(Players.LocalID);
                    SendToAll(w, DeliveryMethod.ReliableOrdered, peer);
                }
            };
            _listener.PeerDisconnectedEvent += (peer, disconnectInfo) => {
                var p = _players[peer];
                var w = CreatePacket(PacketId.PLAYER); {
                    w.Put(0, PLAYER_SUB_IDS, 0);
                    w.Put(false);
                    w.PutPlayerId(p);
                    SendToAll(w, DeliveryMethod.ReliableOrdered, peer);
                }
                Players.Remove(p);
            };
            _listener.ConnectionRequestEvent += request => {
                if (_manager.ConnectedPeersCount + 1 >= Players.MaxPlayers) {
                    request.Reject();
                    return;
                }
                _players.Add(request.Accept(), Players.PopFreeID());
            };
        }

        public static void Host(int maxPlayers) {
            _manager.Start(PORT);
            _initialData.Clear(0);
            Players.Init(maxPlayers);
            _initialData.Put((byte)(Players.MaxPlayers - 1));
        }
        public static void Stop() => _manager.Stop(true);

        public static NetWriter CreatePacket(PacketId packetId) {
            var p = _packets[(int)packetId];
            p.Clear(_packetClearStart);
            return p;
        }
        public static void PutPlayerId(this NetWriter writer, int playerId) => writer.Put(0, Players.MaxPlayers - 1, playerId);
        public static int ReadPlayerId(this NetReader reader) => reader.ReadInt(0, Players.MaxPlayers - 1);

        public static void PollEvents() => _manager.PollEvents();

        public static void SendToAll(NetWriter writer, DeliveryMethod deliveryMethod) => _manager.SendToAll(writer.Data, 0, writer.LengthBytes, deliveryMethod);
        public static void SendToAll(NetWriter writer, DeliveryMethod deliveryMethod, NetPeer excludePeer) => _manager.SendToAll(writer.Data, 0, writer.LengthBytes, deliveryMethod, excludePeer);
        public static void Send(NetWriter writer, NetPeer peer, DeliveryMethod deliveryMethod) => peer.Send(writer.Data, 0, writer.LengthBytes, deliveryMethod);
    }
}