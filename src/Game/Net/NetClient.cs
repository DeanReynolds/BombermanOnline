using System;
using System.Collections.Generic;
using LiteNetLib;
using LiteNetLib.Utils;

namespace BombermanOnline {
    static class NetClient {
        const float SYNC_PLAYERS_TIME = 1 / 60f;

        public static readonly int PACKET_MAX_ID;

        public static bool IsRunning => _manager.IsRunning;

        public enum PacketId { PLAYER, PLACE_BOMB, SET_POWER, CHAT }

        static int _initialDataState;

        static readonly EventBasedNetListener _listener = new EventBasedNetListener();
        static readonly NetManager _manager = new NetManager(_listener) { AutoRecycle = true, UpdateTime = 15 };
        static readonly Dictionary<int, NetWriter> _packets = new Dictionary<int, NetWriter>();
        static readonly int _packetClearStartBits;
        static readonly NetReader _r = new NetReader();

        static double _syncPlayersTimer;

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
                        Players.Dir[i] = (Players.DIR)_r.ReadInt(0, 3);
                    }
                    _manager.FirstPeer.Send(new byte[0], DeliveryMethod.ReliableSequenced);
                    G.SetScr<GameScr>();
                    _initialDataState++;
                } else {
                    var packetId = (NetServer.PacketId)_r.ReadInt(0, NetServer.PACKET_MAX_ID);
                    switch (packetId) {
                        case NetServer.PacketId.PLAYER:
                            var i = _r.ReadInt(0, NetServer.PLAYER_SUB_IDS);
                            switch (i) {
                                case 0:
                                    var p = NetServer.ReadPlayerId(_r);
                                    if (_r.ReadBool())
                                        Players.Insert(p);
                                    else
                                        Players.Remove(p);
                                    break;
                                case 1:
                                    while (!_r.EndOfData) {
                                        var j = _r.ReadPlayerId();
                                        Players.XY[j] = _r.ReadVector2();
                                        Players.Input[j] = 0;
                                        if (_r.ReadBool())
                                            if (_r.ReadInt(0, 1) == 0)
                                                Players.Input[j] |= Players.INPUT.MOV_UP;
                                            else
                                                Players.Input[j] |= Players.INPUT.MOV_DOWN;
                                        if (_r.ReadBool())
                                            if (_r.ReadInt(0, 1) == 0)
                                                Players.Input[j] |= Players.INPUT.MOV_LEFT;
                                            else
                                                Players.Input[j] |= Players.INPUT.MOV_RIGHT;
                                        Players.Dir[j] = (Players.DIR)_r.ReadInt(0, 3);
                                    }
                                    break;
                            }
                            break;
                    }
                }
            };
            _listener.PeerDisconnectedEvent += (peer, disconnectInfo) => {
                // SERVER/HOST DIED
                Players.Clear();
                _initialDataState = 0;
                G.SetScr<MainScr>();
            };
        }

        public static void Join(string ip) {
            _manager.Start();
            _initialDataState = 0;
            _manager.Connect(ip, NetServer.PORT, new NetDataWriter());
        }
        public static void Stop() => _manager.Stop(true);

        public static NetWriter CreatePacket(PacketId packetId) {
            var p = _packets[(int)packetId];
            p.Clear(_packetClearStartBits);
            return p;
        }

        public static void PollEvents() {
            _manager.PollEvents();
            if (_initialDataState != 0 && (_syncPlayersTimer += T.DeltaFull) >= SYNC_PLAYERS_TIME) {
                _syncPlayersTimer -= SYNC_PLAYERS_TIME;
                var w = CreatePacket(PacketId.PLAYER);
                var j = Players.LocalID;
                w.Put(Players.XY[j]);
                if (Players.Input[j].HasFlag(Players.INPUT.MOV_UP)) {
                    w.Put(true);
                    w.Put(0, 1, 0);
                } else if (Players.Input[j].HasFlag(Players.INPUT.MOV_DOWN)) {
                    w.Put(true);
                    w.Put(0, 1, 1);
                } else
                    w.Put(false);
                if (Players.Input[j].HasFlag(Players.INPUT.MOV_LEFT)) {
                    w.Put(true);
                    w.Put(0, 1, 0);
                } else if (Players.Input[j].HasFlag(Players.INPUT.MOV_RIGHT)) {
                    w.Put(true);
                    w.Put(0, 1, 1);
                } else
                    w.Put(false);
                w.Put(0, 3, (int)Players.Dir[j]);
                Send(w, DeliveryMethod.Sequenced);
            }
        }

        public static void Send(NetWriter writer, DeliveryMethod deliveryMethod) => _manager.FirstPeer.Send(writer.Data, 0, writer.LengthBytes, deliveryMethod);
    }
}