using System;
using System.Collections.Generic;
using LiteNetLib;
using LiteNetLib.Utils;

namespace BombermanOnline {
    static class NetClient {
        public static readonly int PACKET_MAX_ID;

        public static bool IsRunning => _manager.IsRunning;

        public enum PacketId {}

        static int _initialDataState;

        static readonly EventBasedNetListener _listener = new EventBasedNetListener();
        static readonly NetManager _manager = new NetManager(_listener) { AutoRecycle = true, UpdateTime = 15 };
        static readonly Dictionary<int, NetWriter> _packets = new Dictionary<int, NetWriter>();
        static readonly int _packetClearStartBits;
        static readonly NetReader _r = new NetReader();

        static NetClient() {
            var packets = Enum.GetValues(typeof(PacketId));
            PACKET_MAX_ID = packets.Length - 1;
            foreach (var p in packets) {
                var w = new NetWriter();
                _packetClearStartBits = w.Put(0, PACKET_MAX_ID, (int)p);
                _packets.Add((int)p, w);
            }
            _listener.NetworkReceiveEvent += (peer, reader, delieryMethod) => {
                _r.ReadFrom(reader);
                if (_initialDataState == 0) {
                    Players.Init(_r.ReadByte() + 1);
                    Players.InsertLocal(_r.ReadPlayerId());
                    while (!_r.EndOfData) {
                        int i = _r.ReadPlayerId();
                        Players.Insert(i);
                    }
                    // TODO: SET SCREEN TO GAMEPLAY
                    _initialDataState++;
                } else {
                    var packetId = (NetServer.PacketId)_r.ReadInt(0, NetServer.PACKET_MAX_ID);
                    if (packetId == NetServer.PacketId.PLAYER) {
                        var i = _r.ReadInt(0, NetServer.PLAYER_SUB_IDS);
                        if (i == 0) {
                            var p = NetServer.ReadPlayerId(_r);
                            if (_r.ReadBool())
                                Players.Insert(p);
                            else
                                Players.Remove(p);
                        }
                    }
                }
            };
            _listener.PeerDisconnectedEvent += (peer, disconnectInfo) => {
                // SERVER/HOST DIED
                Players.Clear();
                // TODO: GO BACK TO MENU (?)
            };
        }

        public static void Join(string ip) {
            _manager.Start();
            _initialDataState = 0;
            _manager.Connect(ip, NetServer.PORT, new NetDataWriter());
        }

        public static NetWriter CreatePacket(PacketId packetId) {
            var p = _packets[(int)packetId];
            p.Clear(_packetClearStartBits);
            return p;
        }

        public static void PollEvents() => _manager.PollEvents();

        public static void Send(NetWriter writer, DeliveryMethod deliveryMethod) => _manager.FirstPeer.Send(writer.Data, 0, writer.LengthBytes, deliveryMethod);
    }
}