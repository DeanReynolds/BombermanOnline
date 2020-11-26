using System;
using Microsoft.Xna.Framework;

namespace BombermanOnline {
    static class Bombs {
        public static readonly int FLAGS_COUNT = (int)MathF.Pow(Enum.GetValues(typeof(FLAGS)).Length, 2);

        public static int Count { get; private set; }
        public static Vector2[] XY { get; private set; }
        public static double[] TimeLeft { get; private set; }
        public static FLAGS[] Flags { get; private set; }
        public static float[] SpawnTime { get; private set; }
        public static byte[] Power { get; private set; }
        public static byte[] Owner { get; private set; }

        [Flags]
        public enum FLAGS : byte { HAS_EXPLODED = 1, HAS_PIERCE = 2 }

        enum EXPLOSION_DIR : byte { INTERSECTION = 0, NORTH = 1, EAST = 2, VERT = 3, HORIZ = 4, WEST = 5, SOUTH = 6 }

        static SpriteAnim ExplosionIntersection, ExplosionNorth, ExplosionEast, ExplosionVert, ExplosionHoriz, ExplosionWest, ExplosionSouth, WallExplosion;
        static(int X, int Y, Powers.IDS Power)[] _powersSpawned;
        static int _powersSpawnedCount;

        public static void Init(int capacity) {
            XY = new Vector2[capacity];
            TimeLeft = new double[capacity];
            Flags = new FLAGS[capacity];
            SpawnTime = new float[capacity];
            Power = new byte[capacity];
            Owner = new byte[capacity];
            var s = new [] {
                G.Sprites["ex00"], G.Sprites["ex01"], G.Sprites["ex02"], G.Sprites["ex03"], G.Sprites["ex04"],
                G.Sprites["ex10"], G.Sprites["ex11"], G.Sprites["ex12"], G.Sprites["ex13"], G.Sprites["ex14"],
                G.Sprites["ex20"], G.Sprites["ex21"], G.Sprites["ex22"], G.Sprites["ex23"], G.Sprites["ex24"],
                G.Sprites["wallblown0"], G.Sprites["wallblown1"], G.Sprites["wallblown2"], G.Sprites["wallblown3"], G.Sprites["wallblown4"], G.Sprites["wallblown5"]
            };
            const float HALF_PI = MathF.PI / 2,
                SPLOSION_SPEED = 1;
            ExplosionIntersection = new SpriteAnim(false, SPLOSION_SPEED, 0, 1, 0, s[2], s[3], s[4], s[3], s[4], s[3], s[4], s[3], s[2], s[1], s[0]);
            ExplosionHoriz = new SpriteAnim(false, SPLOSION_SPEED, 0, ExplosionIntersection.Scale, 0, s[7], s[8], s[9], s[8], s[9], s[8], s[9], s[8], s[7], s[6], s[5]);
            ExplosionVert = ExplosionHoriz;
            ExplosionVert.Rotation = HALF_PI;
            ExplosionEast = new SpriteAnim(false, SPLOSION_SPEED, 0, ExplosionIntersection.Scale, 0, s[12], s[13], s[14], s[13], s[14], s[13], s[14], s[13], s[12], s[11], s[10]);
            ExplosionWest = ExplosionEast;
            ExplosionWest.Rotation = MathF.PI;
            ExplosionSouth = ExplosionEast;
            ExplosionSouth.Rotation = HALF_PI;
            ExplosionNorth = ExplosionEast;
            ExplosionNorth.Rotation = -HALF_PI;
            WallExplosion = new SpriteAnim(false, SPLOSION_SPEED, 0, 1, 0, s[15], s[16], s[17], s[18], s[19], s[20]);
            _powersSpawned = new(int, int, Powers.IDS)[100];
        }

        public static int Spawn(int x, int y, FLAGS flags, int owner) {
            var i = Count++;
            XY[i] = new Vector2(x << Tile.BITS_PER_SIZE, y << Tile.BITS_PER_SIZE);
            TimeLeft[i] = 3.5;
            Flags[i] = flags;
            SpawnTime[i] = T.Total;
            Power[i] = Players.Stats[owner].Fire;
            Owner[i] = (byte)owner;
            Players.Stats[owner].BombsInPlay++;
            return i;
        }
        public static bool Despawn(int i) {
#if DEBUG
            if (Count <= i)
                return false;
#endif
            --Count;
            XY[i] = XY[Count];
            TimeLeft[i] = TimeLeft[Count];
            Flags[i] = Flags[Count];
            SpawnTime[i] = SpawnTime[Count];
            Power[i] = Power[Count];
            Players.Stats[Owner[i]].BombsInPlay--;
            Owner[i] = Owner[Count];
            return true;
        }
        public static void DespawnAll() {
            Count = 0;
        }
        public static bool HasBomb(int x, int y, out int i) {
            for (var j = 0; j < Count; j++)
                if ((int)XY[j].X >> Tile.BITS_PER_SIZE == x && (int)XY[j].Y >> Tile.BITS_PER_SIZE == y) {
                    i = j;
                    return true;
                }
            i = -1;
            return false;
        }

        public static void Update() {
            if (NetServer.IsRunning) {
                var hasABombExploded = false;
                _powersSpawnedCount = 0;
                for (var i = 0; i < Count; i++)
                    if ((TimeLeft[i] -= T.DeltaFull) <= 0 && !Flags[i].HasFlag(FLAGS.HAS_EXPLODED)) {
                        Explode(i);
                        hasABombExploded = true;
                    }
                if (hasABombExploded) {
                    var w = NetServer.CreatePacket(NetServer.Packets.SYNC_BOMBS);
                    w.Put(1, XY.Length, Count);
                    for (var i = 0; i < Count; i++) {
                        int x = (int)XY[i].X >> Tile.BITS_PER_SIZE,
                            y = (int)XY[i].Y >> Tile.BITS_PER_SIZE;
                        w.PutTileXY(x, y);
                        w.Put(0, FLAGS_COUNT, (int)Flags[i]);
                        w.PutPlayerID(Owner[i]);
                        if (Flags[i].HasFlag(FLAGS.HAS_EXPLODED)) {
                            w.Put(1, PlayerStats.MAX_FIRE, Power[i]);
                            Despawn(i--);
                        }
                    }
                    for (var i = 0; i < _powersSpawnedCount; i++) {
                        var p = _powersSpawned[i];
                        w.PutTileXY(p.X, p.Y);
                        w.PutPowerID(p.Power);
                    }
                    NetServer.SendToAll(w, LiteNetLib.DeliveryMethod.ReliableOrdered);
                }
            }
        }
        public static void Draw() {
            for (var i = 0; i < Count; i++) {
                var xy = new Vector2((int)XY[i].X + Tile.HALF_SIZE, (int)XY[i].Y + Tile.HALF_SIZE);
                var s = G.Sprites[$"bomb"];
                G.SB.Draw(G.Sprites.Texture, xy, s.Source, Color.White, 0, s.Origin, .8f + (MathF.Sin((T.Total - SpawnTime[i] + 1) * 5) * .15f), 0, 0);
            }
        }

        public static void Explode(int i) {
            static void SpawnExplosion(int x, int y, EXPLOSION_DIR dir) {
                if (Powers.HasPower(x, y, out var i))
                    Powers.Despawn(i);
                var xy = new Vector2((x << Tile.BITS_PER_SIZE) + Tile.HALF_SIZE, (y << Tile.BITS_PER_SIZE) + Tile.HALF_SIZE);
                SpriteAnim anim = ExplosionIntersection;
                switch (dir) {
                    case EXPLOSION_DIR.NORTH:
                        anim = ExplosionNorth;
                        break;
                    case EXPLOSION_DIR.EAST:
                        anim = ExplosionEast;
                        break;
                    case EXPLOSION_DIR.SOUTH:
                        anim = ExplosionSouth;
                        break;
                    case EXPLOSION_DIR.WEST:
                        anim = ExplosionWest;
                        break;
                    case EXPLOSION_DIR.HORIZ:
                        anim = ExplosionHoriz;
                        break;
                    case EXPLOSION_DIR.VERT:
                        anim = ExplosionVert;
                        break;
                }
                Anims.Spawn(xy, anim, G.Sprites.Texture);
            }
            static Powers.IDS SpawnPower(int x, int y) {
                if (NetClient.IsRunning || G.Rng.NextFloat() >.7f)
                    return 0;
                var pick = G.Rng.NextFloat(Powers.TOTAL_SPAWN_WEIGHT);
                var i = 0;
                var weight = Powers.SPAWN[0].Weight;
                while (pick > weight && i < Powers.SPAWN.Length - 1)
                    weight += Powers.SPAWN[++i].Weight;
                var power = Powers.SPAWN[i].Power;
                _powersSpawned[_powersSpawnedCount++] = (x, y, power);
                Powers.Spawn(x, y, power);
                return power;
            }
            Flags[i] |= FLAGS.HAS_EXPLODED;
            int x = (int)XY[i].X >> Tile.BITS_PER_SIZE,
                y = (int)XY[i].Y >> Tile.BITS_PER_SIZE;
            bool continueUp = true,
                continueRight = true,
                continueDown = true,
                continueLeft = true;
            if (!Flags[i].HasFlag(FLAGS.HAS_PIERCE))
                for (var j = 1; j <= Power[i]; j++) {
                    if (continueUp) {
                        var ry = y - j;
                        if (HasBomb(x, ry, out var k)) {
                            continueUp = false;
                            if (!Flags[k].HasFlag(FLAGS.HAS_EXPLODED))
                                Explode(k);
                        } else if (G.Tiles[x, ry].ID == Tile.IDS.wall) {
                            G.Tiles[x, ry].ID = Tile.IDS.grass;
                            Anims.Spawn(new Vector2((x << Tile.BITS_PER_SIZE) + Tile.HALF_SIZE, (ry << Tile.BITS_PER_SIZE) + Tile.HALF_SIZE), WallExplosion, G.Sprites.Texture);
                            SpawnPower(x, ry);
                            continueUp = false;
                        } else if (G.IsTileSolid(x, ry))
                            continueUp = false;
                        else {
                            SpawnExplosion(x, ry, j != Power[i] ? EXPLOSION_DIR.VERT : EXPLOSION_DIR.NORTH);
                            Players.TryKillAt(x, ry);
                        }
                    }
                    if (continueRight) {
                        var rx = x + j;
                        if (HasBomb(rx, y, out var k)) {
                            continueRight = false;
                            if (!Flags[k].HasFlag(FLAGS.HAS_EXPLODED))
                                Explode(k);
                        } else if (G.Tiles[rx, y].ID == Tile.IDS.wall) {
                            G.Tiles[rx, y].ID = Tile.IDS.grass;
                            Anims.Spawn(new Vector2((rx << Tile.BITS_PER_SIZE) + Tile.HALF_SIZE, (y << Tile.BITS_PER_SIZE) + Tile.HALF_SIZE), WallExplosion, G.Sprites.Texture);
                            SpawnPower(rx, y);
                            continueRight = false;
                        } else if (G.IsTileSolid(rx, y))
                            continueRight = false;
                        else {
                            SpawnExplosion(rx, y, j != Power[i] ? EXPLOSION_DIR.HORIZ : EXPLOSION_DIR.EAST);
                            Players.TryKillAt(rx, y);
                        }
                    }
                    if (continueDown) {
                        var ry = y + j;
                        if (HasBomb(x, ry, out var k)) {
                            continueDown = false;
                            if (!Flags[k].HasFlag(FLAGS.HAS_EXPLODED))
                                Explode(k);
                        } else if (G.Tiles[x, ry].ID == Tile.IDS.wall) {
                            G.Tiles[x, ry].ID = Tile.IDS.grass;
                            Anims.Spawn(new Vector2((x << Tile.BITS_PER_SIZE) + Tile.HALF_SIZE, (ry << Tile.BITS_PER_SIZE) + Tile.HALF_SIZE), WallExplosion, G.Sprites.Texture);
                            SpawnPower(x, ry);
                            continueDown = false;
                        } else if (G.IsTileSolid(x, ry))
                            continueDown = false;
                        else {
                            SpawnExplosion(x, ry, j != Power[i] ? EXPLOSION_DIR.VERT : EXPLOSION_DIR.SOUTH);
                            Players.TryKillAt(x, ry);
                        }
                    }
                    if (continueLeft) {
                        var rx = x - j;
                        if (HasBomb(rx, y, out var k)) {
                            continueLeft = false;
                            if (!Flags[k].HasFlag(FLAGS.HAS_EXPLODED))
                                Explode(k);
                        } else if (G.Tiles[rx, y].ID == Tile.IDS.wall) {
                            G.Tiles[rx, y].ID = Tile.IDS.grass;
                            Anims.Spawn(new Vector2((rx << Tile.BITS_PER_SIZE) + Tile.HALF_SIZE, (y << Tile.BITS_PER_SIZE) + Tile.HALF_SIZE), WallExplosion, G.Sprites.Texture);
                            SpawnPower(rx, y);
                            continueLeft = false;
                        } else if (G.IsTileSolid(rx, y))
                            continueLeft = false;
                        else {
                            SpawnExplosion(rx, y, j != Power[i] ? EXPLOSION_DIR.HORIZ : EXPLOSION_DIR.WEST);
                            Players.TryKillAt(rx, y);
                        }
                    }
                }
            SpawnExplosion(x, y, EXPLOSION_DIR.INTERSECTION);
            Players.TryKillAt(x, y);
            GameScr.Explode.Play();
            TimeLeft[i] = 0;
        }
    }
}