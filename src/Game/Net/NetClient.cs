using System;
using System.Collections.Generic;
using LiteNetLib;
using LiteNetLib.Utils;

namespace BombermanOnline {
    static class NetClient {
        const float SYNC_PLAYERS_TIME = 1 / 60f;

        public static readonly int PACKET_MAX_ID;

        public static bool IsRunning => _manager.IsRunning;

        public enum Packets { PLAYER, PLACE_BOMB, COLLECT_POWER, CHAT }

        static int _initialDataState;

        static readonly EventBasedNetListener _listener = new EventBasedNetListener();
        static readonly NetManager _manager = new NetManager(_listener) { AutoRecycle = true, UpdateTime = 15 };
        static readonly Dictionary<int, NetWriter> _packets = new Dictionary<int, NetWriter>();
        static readonly int _packetClearStartBits;
        static readonly NetReader _r = new NetReader();

        static double _syncPlayersTimer;

        static NetClient() {
            var packets = Enum.GetValues(typeof(Packets));
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
                    Players.InsertLocal(_r.ReadPlayerID());
                    while (!_r.EndOfData) {
                        int i = _r.ReadPlayerID();
                        Players.Insert(i);
                        Players.Dir[i] = (Players.DIR)_r.ReadInt(0, 3);
                        Players.Flags[i] = (Players.FLAGS)_r.ReadInt(0, Players.FLAGS_COUNT);
                    }
                    _manager.FirstPeer.Send(new byte[0], DeliveryMethod.ReliableSequenced);
                    G.SetScr<GameScr>();
                    _initialDataState++;
                } else {
                    var p = (NetServer.Packets)_r.ReadInt(0, NetServer.PACKET_MAX_ID);
                    if (p == NetServer.Packets.PLAYER) {
                        var i = _r.ReadInt(0, NetServer.PLAYER_SUB_IDS);
                        switch (i) {
                            case 0:
                                var k = NetServer.ReadPlayerID(_r);
                                if (_r.ReadBool())
                                    Players.Insert(k);
                                else
                                    Players.Remove(k);
                                break;
                            case 1:
                                while (!_r.EndOfData) {
                                    var j = _r.ReadPlayerID();
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
                                }
                                break;
                        }
                    } else if (p == NetServer.Packets.PLACE_BOMB) {
                        var j = _r.ReadPlayerID();
                        _r.ReadTileXY(out var x, out var y);
                        var flags = (Bombs.FLAGS)_r.ReadInt(0, Bombs.FLAGS_COUNT);
                        Bombs.Spawn(x, y, flags, j);
                    } else if (p == NetServer.Packets.SYNC_BOMBS) {
                        Bombs.DespawnAll();
                        while (!_r.EndOfData) {
                            _r.ReadTileXY(out var x, out var y);
                            var flags = (Bombs.FLAGS)_r.ReadInt(0, Bombs.FLAGS_COUNT);
                            var j = Bombs.Spawn(x, y, flags, 0);
                            if (flags.HasFlag(Bombs.FLAGS.HAS_EXPLODED)) {
                                Bombs.Power[j] = (byte)_r.ReadInt(1, Stats.MAX_FIRE);
                                Bombs.Explode(j);
                                Bombs.Despawn(j);
                            }
                        }
                    } else if (p == NetServer.Packets.COLLECT_POWER) {
                        var j = _r.ReadPlayerID();
                        _r.ReadTileXY(out var x, out var y);
                        var id = _r.ReadPowerID();
                        if (Powers.HasPower(x, y, out var pi) && Powers.ID[pi] == id)
                            Powers.Despawn(pi);
                        Players.AddPower(id, j);
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

        public static NetWriter CreatePacket(Packets packetId) {
            var p = _packets[(int)packetId];
            p.Clear(_packetClearStartBits);
            return p;
        }
        public static void PollEvents() {
            _manager.PollEvents();
            if (_initialDataState != 0 && (_syncPlayersTimer += T.DeltaFull) >= SYNC_PLAYERS_TIME) {
                _syncPlayersTimer -= SYNC_PLAYERS_TIME;
                var w = CreatePacket(Packets.PLAYER);
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
                Send(w, DeliveryMethod.Sequenced);
            }
        }

        public static void Send(NetWriter writer, DeliveryMethod deliveryMethod) => _manager.FirstPeer.Send(writer.Data, 0, writer.LengthBytes, deliveryMethod);
    }
}