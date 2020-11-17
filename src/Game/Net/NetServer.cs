using System;
using System.Collections.Generic;
using LiteNetLib;
using Microsoft.Xna.Framework;

namespace BombermanOnline {
    static class NetServer {
        public const int PORT = 6121;

        const float SYNC_PLAYERS_TIME = 1 / 60f;

        internal const int PLAYER_SUB_IDS = 1;

        public static readonly int PACKET_MAX_ID;

        public static bool IsRunning => _manager.IsRunning;

        public static double RestartGameInTime;

        public enum Packets { PLAYER, PLACE_BOMB, SYNC_BOMBS, SPAWN_POWER, COLLECT_POWER, PLAYER_HIT, RESTART_GAME, CHAT }

        static double _syncPlayersTimer;

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
            var packets = Enum.GetValues(typeof(Packets));
            PACKET_MAX_ID = packets.Length - 1;
            foreach (var p in packets) {
                var w = new NetWriter();
                _packetClearStart = w.Put(0, PACKET_MAX_ID, (int)p);
                _packets.Add((int)p, w);
            }
            _listener.NetworkReceiveEvent += (peer, readerOutdated, delieryMethod) => {
                var j = _players[peer];
                if (readerOutdated.EndOfData) {
                    _peers.Add(j, peer);
                    var w = CreatePacket(Packets.PLAYER); {
                        w.Put(0, PLAYER_SUB_IDS, 0);
                        w.PutPlayerID(j);
                        w.Put(true);
                        SendToAll(w, DeliveryMethod.ReliableOrdered, peer);
                    }
                    if (Players.ShouldRestartGame())
                        RestartGameInTime = 1;
                    return;
                }
                _r.ReadFrom(readerOutdated);
                var p = (NetClient.Packets)_r.ReadInt(0, NetClient.PACKET_MAX_ID);
                if (p == NetClient.Packets.PLAYER) {
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
                } else if (p == NetClient.Packets.PLACE_BOMB) {
                    if (Players.Stats[j].BombsInPlay >= Players.Stats[j].MaxBombs)
                        return;
                    _r.ReadTileXY(out var x, out var y);
                    var flags = (Bombs.FLAGS)_r.ReadInt(0, Bombs.FLAGS_COUNT);
                    var w = CreatePacket(Packets.PLACE_BOMB);
                    w.PutPlayerID(j);
                    w.PutTileXY(x, y);
                    w.Put(0, Bombs.FLAGS_COUNT, (int)flags);
                    SendToAll(w, DeliveryMethod.ReliableOrdered, peer);
                    Bombs.Spawn(x, y, flags, j);
                } else if (p == NetClient.Packets.COLLECT_POWER) {
                    _r.ReadTileXY(out var x, out var y);
                    var id = _r.ReadPowerID();
                    if (Powers.HasPower(x, y, out var pi) && Powers.ID[pi] == id)
                        Powers.Despawn(pi);
                    var w = CreatePacket(Packets.COLLECT_POWER);
                    w.PutPlayerID(j);
                    w.PutTileXY(x, y);
                    w.PutPowerID(id);
                    SendToAll(w, DeliveryMethod.ReliableOrdered, peer);
                    Players.AddPower(id, j);
                } else if (p == NetClient.Packets.PLAYER_HIT) {
                    var xy = _r.ReadVector2();
                    var w = CreatePacket(Packets.PLAYER_HIT);
                    w.PutPlayerID(j);
                    w.Put(xy);
                    SendToAll(w, DeliveryMethod.ReliableOrdered, peer);
                    Players.XY[j] = xy;
                    Players.Kill(j);
                }
            };
            _listener.PeerConnectedEvent += peer => {
                var p = _players[peer];
                Players.Spawn(p);
                _initialData.Clear(_initialDataStart); {
                    _initialData.PutPlayerID(p);
                    _initialData.Put((byte)(G.Tiles.GetLength(0) - 1));
                    _initialData.Put((byte)(G.Tiles.GetLength(1) - 1));
                    for (var x = 0; x < G.Tiles.GetLength(0); x++)
                        for (var y = 0; y < G.Tiles.GetLength(1); y++)
                            _initialData.Put(0, Tile.MAX_ID, (int)G.Tiles[x, y].ID);
                    static void PutPlayer(int j) {
                        _initialData.PutPlayerID(j);
                        _initialData.Put(0, Players.FLAGS_COUNT, (int)Players.Flags[j]);
                        _initialData.Put(0, Players.TEAMS_COUNT, (int)Players.Team[j]);
                        if (!Players.Flags[j].HasFlag(Players.FLAGS.IS_DEAD))
                            _initialData.Put(0, 3, (int)Players.Dir[j]);
                    }
                    PutPlayer(Players.LocalID);
                    foreach (var j in _peers.Keys)
                        PutPlayer(j);
                    Send(_initialData, peer, DeliveryMethod.ReliableOrdered);
                }
            };
            _listener.PeerDisconnectedEvent += (peer, disconnectInfo) => {
                var p = _players[peer];
                var w = CreatePacket(Packets.PLAYER); {
                    w.Put(0, PLAYER_SUB_IDS, 0);
                    w.Put(false);
                    w.PutPlayerID(p);
                    SendToAll(w, DeliveryMethod.ReliableOrdered, peer);
                }
                Players.Despawn(p);
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
            Players.Init(maxPlayers);
            _initialData.Clear();
            _initialData.Put((byte)(maxPlayers - 1));
        }
        public static void Stop() => _manager.Stop(true);

        public static NetWriter CreatePacket(Packets packetId) {
            var p = _packets[(int)packetId];
            p.Clear(_packetClearStart);
            return p;
        }
        public static void PollEvents() {
            _manager.PollEvents();
            if ((_syncPlayersTimer += T.DeltaFull) >= SYNC_PLAYERS_TIME) {
                _syncPlayersTimer -= SYNC_PLAYERS_TIME;
                foreach (var p in _peers.Keys) {
                    var w = CreatePacket(Packets.PLAYER);
                    w.Put(0, PLAYER_SUB_IDS, 1);
                    for (var j = 0; j < Players.MaxPlayers; j++)
                        if (j != p && !Players.Flags[j].HasFlag(Players.FLAGS.IS_DEAD)) {
                            PutPlayerID(w, j);
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
                        }
                    Send(w, _peers[p], DeliveryMethod.Sequenced);
                }
            }
            if (RestartGameInTime > 0) {
                if ((RestartGameInTime -= T.DeltaFull) <= 0) {
                    var w = NetServer.CreatePacket(NetServer.Packets.RESTART_GAME);
                    var spawns = new List<Point> {
                        new Point(1, 1),
                        new Point(G.Tiles.GetLength(0) - 2, 1),
                        new Point(1, G.Tiles.GetLength(1) - 2),
                        new Point(G.Tiles.GetLength(0) - 2, G.Tiles.GetLength(1) - 2),
                    };
                    var players = new Dictionary<Players.TEAMS, List<int>>();
                    foreach (var i in Players.TakenIDs) {
                        if (!players.ContainsKey(Players.Team[i]))
                            players.Add(Players.Team[i], new List<int> { i });
                        else
                            players[Players.Team[i]].Add(i);
                    }
                    G.MakeMap(G.Tiles.GetLength(0), G.Tiles.GetLength(1));
                    w.Put((byte)(G.Tiles.GetLength(0) - 1));
                    w.Put((byte)(G.Tiles.GetLength(1) - 1));
                    w.Put((byte)Players.TakenIDs.Count);
                    foreach (var t in players.Keys) {
                        getFreshSpawn : if (spawns.Count == 0) {
                            spawns.Add(new Point(1, G.Tiles.GetLength(1) / 2));
                            spawns.Add(new Point(G.Tiles.GetLength(0) - 2, G.Tiles.GetLength(1) / 2));
                            for (var i = -1; i <= 1; i++) {
                                G.Tiles[1, G.Tiles.GetLength(1) / 2 + i].ID = Tile.IDS.grass;
                                G.Tiles[G.Tiles.GetLength(0) - 2, G.Tiles.GetLength(1) / 2 + i].ID = Tile.IDS.grass;
                            }
                            spawns.Add(new Point(G.Tiles.GetLength(0) / 2, 1));
                            spawns.Add(new Point(G.Tiles.GetLength(0) / 2, G.Tiles.GetLength(1) - 2));
                            for (var i = -1; i <= 1; i++) {
                                G.Tiles[G.Tiles.GetLength(0) / 2 + i, 1].ID = Tile.IDS.grass;
                                G.Tiles[G.Tiles.GetLength(0) / 2 + i, G.Tiles.GetLength(1) - 2].ID = Tile.IDS.grass;
                            }
                        }
                        var j = G.Rng.Next(spawns.Count);
                        var p = spawns[j];
                        spawns.RemoveAt(j);
                        for (var n = 0; n < players[t].Count; n++) {
                            var i = players[t][n];
                            // Console.WriteLine($"{i}--{p.X},{p.Y}");
                            w.PutPlayerID(i);
                            w.PutTileXY(p.X, p.Y);
                            Players.XY[i] = new Vector2((p.X << Tile.BITS_PER_SIZE) + Tile.HALF_SIZE, (p.Y << Tile.BITS_PER_SIZE) + Tile.HALF_SIZE);
                            players[t].RemoveAt(n--);
                            if (t == Players.TEAMS.FFA)
                                goto getFreshSpawn;
                        }
                    }
                    for (var x = 0; x < G.Tiles.GetLength(0); x++)
                        for (var y = 0; y < G.Tiles.GetLength(1); y++)
                            w.Put(0, Tile.MAX_ID, (int)G.Tiles[x, y].ID);
                    NetServer.SendToAll(w, LiteNetLib.DeliveryMethod.ReliableOrdered);
                    Bombs.DespawnAll();
                    Powers.DespawnAll();
                    Anims.DespawnAll();
                    Players.ResetAll();
                }
            }
        }

        public static void SendToAll(NetWriter writer, DeliveryMethod deliveryMethod) => _manager.SendToAll(writer.Data, 0, writer.LengthBytes, deliveryMethod);
        public static void SendToAll(NetWriter writer, DeliveryMethod deliveryMethod, NetPeer excludePeer) => _manager.SendToAll(writer.Data, 0, writer.LengthBytes, deliveryMethod, excludePeer);
        public static void Send(NetWriter writer, NetPeer peer, DeliveryMethod deliveryMethod) => peer.Send(writer.Data, 0, writer.LengthBytes, deliveryMethod);

        public static void PutPlayerID(this NetWriter writer, int playerId) => writer.Put(0, Players.MaxPlayers - 1, playerId);
        public static int ReadPlayerID(this NetReader reader) => reader.ReadInt(0, Players.MaxPlayers - 1);
        public static void PutTileXY(this NetWriter w, int x, int y) {
            w.Put(1, G.Tiles.GetLength(0) - 2, x);
            w.Put(1, G.Tiles.GetLength(1) - 2, y);
        }
        public static void ReadTileXY(this NetReader r, out int x, out int y) {
            x = r.ReadInt(1, G.Tiles.GetLength(0) - 2);
            y = r.ReadInt(1, G.Tiles.GetLength(1) - 2);
        }
    }
}