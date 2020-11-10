using System;
using System.Collections.Generic;
using Apos.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BombermanOnline {
    struct PlayerStats {
        public const int MAX_FIRE = 10,
            MAX_BOMBS = 8,
            MAX_SPEED = 3;
        public const sbyte MIN_SPEED = -3;

        public byte Fire;
        public byte BombsInPlay;
        public byte MaxBombs;
        public sbyte Speed;
    }
    static class Players {
        public const int HITBOX_WIDTH = 14,
            HITBOX_HEIGHT = 11;
        public static readonly int FLAGS_COUNT = (int)MathF.Pow(Enum.GetValues(typeof(FLAGS)).Length, 2);
        public static readonly int TEAMS_COUNT = Enum.GetValues(typeof(TEAMS)).Length - 1;

        public static int MaxPlayers { get; private set; }
        public static int LocalID { get; private set; } = -1;
        public static Vector2[] XY { get; private set; }
        public static INPUT[] Input { get; private set; }
        public static DIR[] Dir { get; private set; }
        public static SpriteAnim[] Anim { get; private set; }
        public static FLAGS[] Flags { get; private set; }
        public static PlayerStats[] Stats { get; private set; }
        public static TEAMS[] Team { get; private set; }

        public static readonly HashSet<int> TakenIDs = new HashSet<int>();

        public enum DIR : byte { NORTH = 0, EAST = 1, SOUTH = 2, WEST = 3 }

        [Flags]
        public enum INPUT : byte { MOV_UP = 1, MOV_DOWN = 2, MOV_RIGHT = 4, MOV_LEFT = 8 }

        [Flags]
        public enum FLAGS : byte { IS_DEAD = 1, CAN_KICK_BOMBS = 2, BOMBS_CAN_PIERCE = 4, HAS_LOUIE = 8 }

        public enum TEAMS : byte { FFA = 0 }

        static SpriteAnim Death;

        static readonly LinkedList<int> _freeIDs = new LinkedList<int>();
        static readonly Dictionary<TEAMS, byte> _playersAlive = new Dictionary<TEAMS, byte>();

        static Players() {
            for (var i = 0; i < TEAMS_COUNT + 1; i++)
                _playersAlive.Add((TEAMS)i, 0);
        }

        internal static void Init(int capacity) {
            MaxPlayers = capacity;
            XY = new Vector2[capacity];
            Input = new INPUT[capacity];
            Dir = new DIR[capacity];
            Anim = new SpriteAnim[capacity];
            Flags = new FLAGS[capacity];
            Stats = new PlayerStats[capacity];
            Team = new TEAMS[capacity];
            TakenIDs.Clear();
            _freeIDs.Clear();
            for (int i = 0; i < capacity; i++)
                _freeIDs.AddLast(i);
            var s = new [] {
                G.Sprites["p20"], G.Sprites["pd0"], G.Sprites["pd1"], G.Sprites["pd2"],
            };
            Death = new SpriteAnim(false, .5f, 0, s[0], s[1], s[2], s[3]);
        }

        internal static void Spawn(int i) {
            _freeIDs.Remove(i);
            TakenIDs.Add(i);
            Flags[i] = FLAGS.IS_DEAD;
        }
        internal static void SpawnLocal(int i) {
            Spawn(i);
            LocalID = i;
        }
        internal static void Despawn(int i) {
            if (!TakenIDs.Remove(i))
                return;
            _freeIDs.AddLast(i);
        }
        internal static void DespawnAll() {
            foreach (int i in TakenIDs)
                _freeIDs.AddLast(i);
            TakenIDs.Clear();
            LocalID = -1;
        }
        internal static int PopFreeID() {
            int i = _freeIDs.Last.Value;
            _freeIDs.RemoveLast();
            return i;
        }
        public static void Kill(int i) {
            Flags[i] |= FLAGS.IS_DEAD;
            Anims.Spawn(XY[i], Death);
            _playersAlive[Team[i]]--;
        }
        public static bool TryKillAt(int x, int y) {
            if (!Flags[LocalID].HasFlag(FLAGS.IS_DEAD)) {
                int ptx = ((int)XY[LocalID].X) >> Tile.BITS_PER_SIZE,
                    pty = ((int)XY[LocalID].Y) >> Tile.BITS_PER_SIZE;
                if (ptx == x && pty == y) {
                    Kill(LocalID);
                    if (NetServer.IsRunning) {
                        var w = NetServer.CreatePacket(NetServer.Packets.PLAYER_DIED);
                        w.PutPlayerID(LocalID);
                        w.Put(XY[LocalID]);
                        NetServer.SendToAll(w, LiteNetLib.DeliveryMethod.ReliableOrdered);
                        if (NetServer.RestartGameInTime <= 0 && ShouldRestartGame())
                            NetServer.RestartGameInTime = 3;
                    } else if (NetClient.IsRunning) {
                        var w = NetClient.CreatePacket(NetClient.Packets.PLAYER_DIED);
                        w.Put(XY[LocalID]);
                        NetClient.Send(w, LiteNetLib.DeliveryMethod.ReliableOrdered);
                    }
                    return true;
                }
            }
            return false;
        }
        public static bool ShouldRestartGame() {
            var teamsAlive = 0;
            for (var i = 0; i < TEAMS_COUNT + 1; i++)
                if (_playersAlive[(TEAMS)i] > 0)
                    teamsAlive++;
            var multipleFFAAlive = _playersAlive[TEAMS.FFA] > 1;
            return teamsAlive <= 1 && !multipleFFAAlive;
        }

        public static void Update() {
            if (!Flags[LocalID].HasFlag(FLAGS.IS_DEAD)) {
                Input[LocalID] = 0;
                if (KeyboardCondition.Held(Keys.W)) {
                    if (!KeyboardCondition.Held(Keys.S))
                        Input[LocalID] |= INPUT.MOV_UP;
                } else if (KeyboardCondition.Held(Keys.S))
                    Input[LocalID] |= INPUT.MOV_DOWN;
                if (KeyboardCondition.Held(Keys.A)) {
                    if (!KeyboardCondition.Held(Keys.D))
                        Input[LocalID] |= INPUT.MOV_LEFT;
                } else if (KeyboardCondition.Held(Keys.D))
                    Input[LocalID] |= INPUT.MOV_RIGHT;
                if ((KeyboardCondition.Held(Keys.Space) || KeyboardCondition.Held(Keys.Enter)) && Stats[LocalID].BombsInPlay < Stats[LocalID].MaxBombs) {
                    int x = (int)XY[LocalID].X >> Tile.BITS_PER_SIZE,
                        y = (int)XY[LocalID].Y >> Tile.BITS_PER_SIZE;
                    if (!Bombs.HasBomb(x, y, out _)) {
                        var flags = (Bombs.FLAGS)0;
                        if (NetServer.IsRunning) {
                            var w = NetServer.CreatePacket(NetServer.Packets.PLACE_BOMB);
                            w.PutPlayerID(LocalID);
                            w.PutTileXY(x, y);
                            w.Put(0, Bombs.FLAGS_COUNT, (int)flags);
                            NetServer.SendToAll(w, LiteNetLib.DeliveryMethod.ReliableOrdered);
                        } else if (NetClient.IsRunning) {
                            var w = NetClient.CreatePacket(NetClient.Packets.PLACE_BOMB);
                            w.PutTileXY(x, y);
                            w.Put(0, Bombs.FLAGS_COUNT, (int)flags);
                            NetClient.Send(w, LiteNetLib.DeliveryMethod.ReliableOrdered);
                        }
                        Bombs.Spawn(x, y, flags, LocalID);
                    }
                }
            }
            static void CollectPower(int player, int power, int x, int y) {
                var id = Powers.ID[power];
                if (player == LocalID) {
                    // int x = (int)XY[power].X >> Tile.BITS_PER_SIZE,
                    //     y = (int)XY[power].Y >> Tile.BITS_PER_SIZE;
                    if (NetServer.IsRunning) {
                        var w = NetServer.CreatePacket(NetServer.Packets.COLLECT_POWER);
                        w.PutPlayerID(LocalID);
                        w.PutTileXY(x, y);
                        w.PutPowerID(Powers.ID[power]);
                        NetServer.SendToAll(w, LiteNetLib.DeliveryMethod.ReliableOrdered);
                    } else if (NetClient.IsRunning) {
                        var w = NetClient.CreatePacket(NetClient.Packets.COLLECT_POWER);
                        w.PutTileXY(x, y);
                        w.PutPowerID(Powers.ID[power]);
                        NetClient.Send(w, LiteNetLib.DeliveryMethod.ReliableOrdered);
                    }
                    AddPower(id, player);
                }
                Powers.Despawn(power);
            }
            int ptx, pty, dir;
            foreach (var i in TakenIDs) {
                float moveSpd = (50 + (8 * Stats[i].Speed)) * T.Delta, oldXY;
                ptx = ((int)XY[i].X) >> Tile.BITS_PER_SIZE;
                pty = ((int)XY[i].Y) >> Tile.BITS_PER_SIZE;
                dir = 0;
                oldXY = (int)XY[i].X;
                if (Input[i].HasFlag(INPUT.MOV_LEFT)) {
                    XY[i].X -= moveSpd;
                    dir = -1;
                } else if (Input[i].HasFlag(INPUT.MOV_RIGHT)) {
                    XY[i].X += moveSpd;
                    dir = 1;
                }
                var di = false;
                var hb = new Rectangle((int)(XY[i].X - (HITBOX_WIDTH >> 1)), (int)(XY[i].Y - (HITBOX_HEIGHT >> 1)), HITBOX_WIDTH, HITBOX_HEIGHT);
                var pdt = (((int)XY[i].X) >> Tile.BITS_PER_SIZE) + (dir * 2);
                var nt = 100;
                if (dir != 0) {
                    for (var y = -1; y <= 1; y++) {
                        var ry = pty + y;
                        if (ry < 0 || ry > G.Tiles.GetLength(1))
                            continue;
                        for (var x = ptx + dir; x != pdt; x += dir) {
                            if (x < 0 || x >= G.Tiles.GetLength(0))
                                break;
                            if ((G.IsTileSolid(x, ry) || Bombs.HasBomb(x, ry, out _)) &&
                                hb.Intersects(new Rectangle(x << Tile.BITS_PER_SIZE, ry << Tile.BITS_PER_SIZE, Tile.SIZE, Tile.SIZE))) {
                                if (MathF.Abs(ptx - x) < MathF.Abs(ptx - nt))
                                    nt = x;
                                di = true;
                                break;
                            }
                        }
                    }
                    if (di)
                        XY[i].X = (nt << Tile.BITS_PER_SIZE) + (dir < 0 ? Tile.SIZE : 0) - (dir * (hb.Width / 2f));
                    if ((int)XY[i].X != oldXY) {
                        Dir[i] = dir == 1 ? DIR.EAST : DIR.WEST;
                        int nptx = ((int)XY[i].X) >> Tile.BITS_PER_SIZE;
                        if (nptx != ptx) {
                            ptx = nptx;
                            if (Powers.HasPower(ptx, pty, out var pi)) {
                                // var phb = new Rectangle((int)(Powers.XY[pi].X - (Powers.HITBOX_WIDTH >> 1)), (int)(Powers.XY[pi].Y - (Powers.HITBOX_HEIGHT >> 1)), Powers.HITBOX_WIDTH, Powers.HITBOX_HEIGHT);
                                // if (hb.Intersects(phb))
                                CollectPower(i, pi, ptx, pty);
                            }
                        }
                    }
                }
                dir = 0;
                oldXY = (int)XY[i].Y;
                if (Input[i].HasFlag(INPUT.MOV_UP)) {
                    XY[i].Y -= moveSpd;
                    dir = -1;
                } else if (Input[i].HasFlag(INPUT.MOV_DOWN)) {
                    XY[i].Y += moveSpd;
                    dir = 1;
                }
                if (dir != 0) {
                    hb = new Rectangle((int)(XY[i].X - (HITBOX_WIDTH >> 1)), (int)(XY[i].Y - (HITBOX_HEIGHT >> 1)), HITBOX_WIDTH, HITBOX_HEIGHT);
                    pdt = (((int)XY[i].Y) >> Tile.BITS_PER_SIZE) + (dir * 2);
                    nt = 100;
                    di = false;
                    for (var x = -1; x <= 1; x++) {
                        var rx = ptx + x;
                        if (rx < 0 || rx > G.Tiles.GetLength(0))
                            continue;
                        for (var y = pty + dir; y != pdt; y += dir) {
                            if (y < 0 || y >= G.Tiles.GetLength(1))
                                break;
                            if ((G.IsTileSolid(rx, y) || Bombs.HasBomb(rx, y, out _)) &&
                                hb.Intersects(new Rectangle(rx << Tile.BITS_PER_SIZE, y << Tile.BITS_PER_SIZE, Tile.SIZE, Tile.SIZE))) {
                                if (MathF.Abs(pty - y) < MathF.Abs(pty - nt))
                                    nt = y;
                                di = true;
                                break;
                            }
                        }
                    }
                    if (di)
                        XY[i].Y = (nt << Tile.BITS_PER_SIZE) + (dir < 0 ? Tile.SIZE : 0) - (dir * (hb.Height / 2f));
                    if ((int)XY[i].Y != oldXY) {
                        Dir[i] = dir == 1 ? DIR.SOUTH : DIR.NORTH;
                        int npty = ((int)XY[i].Y) >> Tile.BITS_PER_SIZE;
                        if (npty != pty) {
                            pty = npty;
                            if (Powers.HasPower(ptx, pty, out var pi)) {
                                // var phb = new Rectangle((int)(Powers.XY[pi].X - (Powers.HITBOX_WIDTH >> 1)), (int)(Powers.XY[pi].Y - (Powers.HITBOX_HEIGHT >> 1)), Powers.HITBOX_WIDTH, Powers.HITBOX_HEIGHT);
                                // if (hb.Intersects(phb))
                                CollectPower(i, pi, ptx, pty);
                            }
                        }
                    }
                }
            }
        }
        public static void Draw() {
            foreach (var i in TakenIDs)
                if (!Flags[i].HasFlag(FLAGS.IS_DEAD)) {
                    // var hb = new Rectangle((int)(XY[i].X - (HITBOX_WIDTH >> 1)), (int)(XY[i].Y - (HITBOX_HEIGHT >> 1)), HITBOX_WIDTH, HITBOX_HEIGHT);
                    // G.SB.FillRectangle(hb, Color.Blue);
                    var xy = XY[i].ToPoint().ToVector2();
                    byte dir;
                    SpriteEffects effect = 0;
                    switch (Dir[i]) {
                        case DIR.EAST:
                            xy.X -= 1;
                            dir = 1;
                            break;
                        case DIR.WEST:
                            xy.X += 3;
                            dir = 1;
                            effect = SpriteEffects.FlipHorizontally;
                            break;
                        default:
                            dir = (byte)Dir[i];
                            break;
                    }
                    var s = G.Sprites[$"p{dir}0"];
                    G.SB.Draw(G.Sprites.Texture, xy, s.Source, Color.White, 0, s.Origin, 1, effect, 0);
                }
        }

        public static void Reset(int i) {
            Flags[i] = 0;
            Stats[i] = new PlayerStats {
                Fire = 1,
                MaxBombs = 1,
                Speed = 0
            };
            _playersAlive[Team[i]]++;
        }
        public static void ResetAll() {
            foreach (var i in TakenIDs)
                Reset(i);
        }
        public static void AddPower(Powers.IDS id, int player) {
            switch (id) {
                case Powers.IDS.FIRE_UP:
                    Stats[player].Fire = (byte)Math.Min(Stats[player].Fire + 1, PlayerStats.MAX_FIRE);
                    break;
                case Powers.IDS.FIRE_DOWN:
                    Stats[player].Fire = (byte)Math.Max(Stats[player].Fire - 1, 1);
                    break;
                case Powers.IDS.FULL_FIRE:
                    Stats[player].Fire = PlayerStats.MAX_FIRE;
                    break;
                case Powers.IDS.BOMB_UP:
                    Stats[player].MaxBombs = (byte)Math.Min(Stats[player].MaxBombs + 1, PlayerStats.MAX_BOMBS);
                    break;
                case Powers.IDS.BOMB_DOWN:
                    Stats[player].MaxBombs = (byte)Math.Max(Stats[player].MaxBombs - 1, 1);
                    break;
                case Powers.IDS.POWER_BOMB:
                    Stats[player].MaxBombs = PlayerStats.MAX_BOMBS;
                    break;
                case Powers.IDS.SKATE:
                    Stats[player].Speed = (sbyte)Math.Min(Stats[player].Speed + 1, PlayerStats.MAX_SPEED);
                    break;
                case Powers.IDS.GETA:
                    Stats[player].Speed = (sbyte)Math.Max(Stats[player].Speed - 1, PlayerStats.MIN_SPEED);
                    break;
            }
        }
    }
}