using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BombermanOnline {
    static class Bombs {
        public static readonly int FLAGS_COUNT = Enum.GetValues(typeof(FLAGS)).Length;

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

        public static void Init(int capacity) {
            XY = new Vector2[capacity];
            TimeLeft = new double[capacity];
            Flags = new FLAGS[capacity];
            SpawnTime = new float[capacity];
            Power = new byte[capacity];
            Owner = new byte[capacity];
            const float explosionSpeed = 1;
            var s = new [] {
                G.Sprites["explode00"], G.Sprites["explode01"], G.Sprites["explode02"], G.Sprites["explode03"],
                G.Sprites["explode10"], G.Sprites["explode11"], G.Sprites["explode12"], G.Sprites["explode13"],
                G.Sprites["explode20"], G.Sprites["explode21"], G.Sprites["explode22"], G.Sprites["explode23"],
                G.Sprites["explode30"], G.Sprites["explode31"], G.Sprites["explode32"], G.Sprites["explode33"],
                G.Sprites["explode40"], G.Sprites["explode41"], G.Sprites["explode42"], G.Sprites["explode43"],
                G.Sprites["wallblown0"], G.Sprites["wallblown1"], G.Sprites["wallblown2"], G.Sprites["wallblown3"], G.Sprites["wallblown4"], G.Sprites["wallblown5"]
            };
            ExplosionIntersection = new SpriteAnim(false, explosionSpeed, 0, s[0], s[1], s[2], s[1], s[2], s[1], s[2], s[1], s[2], s[3]);
            ExplosionNorth = new SpriteAnim(false, explosionSpeed, 0, s[4], s[5], s[6], s[5], s[6], s[5], s[6], s[5], s[6], s[7]);
            ExplosionEast = new SpriteAnim(false, explosionSpeed, 0, s[8], s[9], s[10], s[9], s[10], s[9], s[10], s[9], s[10], s[11]);
            ExplosionVert = new SpriteAnim(false, explosionSpeed, 0, s[12], s[13], s[14], s[13], s[14], s[13], s[14], s[13], s[14], s[15]);
            ExplosionHoriz = new SpriteAnim(false, explosionSpeed, 0, s[16], s[17], s[18], s[17], s[18], s[17], s[18], s[17], s[18], s[19]);
            ExplosionWest = new SpriteAnim(false, explosionSpeed, SpriteEffects.FlipHorizontally, s[8], s[9], s[10], s[9], s[10], s[9], s[10], s[9], s[10], s[11]);
            ExplosionSouth = new SpriteAnim(false, explosionSpeed, SpriteEffects.FlipVertically, s[4], s[5], s[6], s[5], s[6], s[5], s[6], s[5], s[6], s[7]);
            WallExplosion = new SpriteAnim(false, .75f, 0, s[20], s[21], s[22], s[23], s[24], s[25]);
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
            for (var i = 0; i < Count; i++)
                Despawn(i);
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
                for (var i = 0; i < Count; i++)
                    if ((TimeLeft[i] -= T.DeltaFull) <= 0 && !Flags[i].HasFlag(FLAGS.HAS_EXPLODED)) {
                        Explode(i);
                        hasABombExploded = true;
                    }
                if (hasABombExploded) {
                    var w = NetServer.CreatePacket(NetServer.Packets.SYNC_BOMBS);
                    for (var i = 0; i < Count; i++) {
                        int x = (int)XY[i].X >> Tile.BITS_PER_SIZE,
                            y = (int)XY[i].Y >> Tile.BITS_PER_SIZE;
                        w.PutTileXY(x, y);
                        w.Put(0, FLAGS_COUNT, (int)Flags[i]);
                        if (Flags[i].HasFlag(FLAGS.HAS_EXPLODED)) {
                            w.Put(1, Stats.MAX_FIRE, Power[i]);
                            Despawn(i--);
                        }
                        NetServer.SendToAll(w, LiteNetLib.DeliveryMethod.ReliableOrdered);
                    }
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
                var xy = new Vector2((x << Tile.BITS_PER_SIZE) + Tile.HALF_SIZE, (y << Tile.BITS_PER_SIZE) + Tile.HALF_SIZE);
                SpriteAnim anim = ExplosionIntersection;
                switch (dir) {
                    case EXPLOSION_DIR.NORTH:
                        anim = ExplosionNorth;
                        xy.Y += Tile.HALF_SIZE;
                        break;
                    case EXPLOSION_DIR.EAST:
                        anim = ExplosionEast;
                        xy.X -= Tile.HALF_SIZE;
                        break;
                    case EXPLOSION_DIR.SOUTH:
                        anim = ExplosionSouth;
                        xy.Y += Tile.HALF_SIZE - 3;
                        break;
                    case EXPLOSION_DIR.WEST:
                        anim = ExplosionWest;
                        xy.X -= Tile.HALF_SIZE - 3;
                        break;
                    case EXPLOSION_DIR.HORIZ:
                        anim = ExplosionHoriz;
                        break;
                    case EXPLOSION_DIR.VERT:
                        anim = ExplosionVert;
                        break;
                }
                Animations.Spawn(xy, anim);
            }
            static void SpawnPower(int x, int y) {
                var xy = new Vector2((x << Tile.BITS_PER_SIZE) + Tile.HALF_SIZE, (y << Tile.BITS_PER_SIZE) + Tile.HALF_SIZE);
                Powers.Spawn(xy, Powers.IDS.FIRE_UP);
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
                            Animations.Spawn(new Vector2((x << Tile.BITS_PER_SIZE) + Tile.HALF_SIZE, (ry << Tile.BITS_PER_SIZE) + Tile.HALF_SIZE), WallExplosion);
                            SpawnPower(x, ry);
                            continueUp = false;
                        } else if (G.IsTileSolid(x, ry))
                            continueUp = false;
                        else
                            SpawnExplosion(x, ry, j != Power[i] ? EXPLOSION_DIR.VERT : EXPLOSION_DIR.NORTH);
                    }
                    if (continueRight) {
                        var rx = x + j;
                        if (HasBomb(rx, y, out var k)) {
                            continueRight = false;
                            if (!Flags[k].HasFlag(FLAGS.HAS_EXPLODED))
                                Explode(k);
                        } else if (G.Tiles[rx, y].ID == Tile.IDS.wall) {
                            G.Tiles[rx, y].ID = Tile.IDS.grass;
                            Animations.Spawn(new Vector2((rx << Tile.BITS_PER_SIZE) + Tile.HALF_SIZE, (y << Tile.BITS_PER_SIZE) + Tile.HALF_SIZE), WallExplosion);
                            SpawnPower(rx, y);
                            continueRight = false;
                        } else if (G.IsTileSolid(rx, y))
                            continueRight = false;
                        else
                            SpawnExplosion(rx, y, j != Power[i] ? EXPLOSION_DIR.HORIZ : EXPLOSION_DIR.EAST);
                    }
                    if (continueDown) {
                        var ry = y + j;
                        if (HasBomb(x, ry, out var k)) {
                            continueDown = false;
                            if (!Flags[k].HasFlag(FLAGS.HAS_EXPLODED))
                                Explode(k);
                        } else if (G.Tiles[x, ry].ID == Tile.IDS.wall) {
                            G.Tiles[x, ry].ID = Tile.IDS.grass;
                            Animations.Spawn(new Vector2((x << Tile.BITS_PER_SIZE) + Tile.HALF_SIZE, (ry << Tile.BITS_PER_SIZE) + Tile.HALF_SIZE), WallExplosion);
                            SpawnPower(x, ry);
                            continueDown = false;
                        } else if (G.IsTileSolid(x, ry))
                            continueDown = false;
                        else
                            SpawnExplosion(x, ry, j != Power[i] ? EXPLOSION_DIR.VERT : EXPLOSION_DIR.SOUTH);
                    }
                    if (continueLeft) {
                        var rx = x - j;
                        if (HasBomb(rx, y, out var k)) {
                            continueLeft = false;
                            if (!Flags[k].HasFlag(FLAGS.HAS_EXPLODED))
                                Explode(k);
                        } else if (G.Tiles[rx, y].ID == Tile.IDS.wall) {
                            G.Tiles[rx, y].ID = Tile.IDS.grass;
                            Animations.Spawn(new Vector2((rx << Tile.BITS_PER_SIZE) + Tile.HALF_SIZE, (y << Tile.BITS_PER_SIZE) + Tile.HALF_SIZE), WallExplosion);
                            SpawnPower(rx, y);
                            continueLeft = false;
                        } else if (G.IsTileSolid(rx, y))
                            continueLeft = false;
                        else
                            SpawnExplosion(rx, y, j != Power[i] ? EXPLOSION_DIR.HORIZ : EXPLOSION_DIR.WEST);
                    }
                }
            SpawnExplosion(x, y, EXPLOSION_DIR.INTERSECTION);
            TimeLeft[i] = 0;
        }
    }
}