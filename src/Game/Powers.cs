using System;
using Microsoft.Xna.Framework;

namespace BombermanOnline {
    static class Powers {
        public const int HITBOX_WIDTH = 12,
            HITBOX_HEIGHT = 12;
        public static readonly int MAX_IDS = Enum.GetValues(typeof(IDS)).Length;

        public static int Count { get; private set; }
        public static Vector2[] XY { get; private set; }
        public static IDS[] ID { get; private set; }
        public static float[] SpawnTime { get; private set; }

        public enum IDS : byte { FIRE_UP, FIRE_DOWN, FULL_FIRE, BOMB_UP, BOMB_DOWN, POWER_BOMB, SKATE, GETA, BOMB_KICK, BOMB_PIERCE }

        public static void Init(int capacity) {
            XY = new Vector2[capacity];
            ID = new IDS[capacity];
            SpawnTime = new float[capacity];
        }

        public static void Spawn(Vector2 xy, IDS id) {
            var i = Count++;
            XY[i] = xy;
            ID[i] = id;
            SpawnTime[i] = T.Total;
        }
        public static bool Despawn(int i) {
#if DEBUG
            if (Count <= i)
                return false;
#endif
            --Count;
            XY[i] = XY[Count];
            ID[i] = ID[Count];
            SpawnTime[i] = SpawnTime[Count];
            return true;
        }
        public static bool HasPower(int x, int y, out int i) {
            for (var j = 0; j < Count; j++)
                if ((int)XY[j].X >> Tile.BITS_PER_SIZE == x && (int)XY[j].Y >> Tile.BITS_PER_SIZE == y) {
                    i = j;
                    return true;
                }
            i = -1;
            return false;
        }

        public static void Update() {
            for (var i = 0; i < Count; i++) {
                var timeAlive = T.Total - SpawnTime[i];
                if (timeAlive >= 20)
                    Despawn(i--);
            }
        }
        public static void Draw() {
            for (var i = 0; i < Count; i++) {
                var s = G.Sprites[$"+{ID[i].ToString().ToLower()}"];
                var timeAlive = T.Total - SpawnTime[i];
                G.SB.Draw(G.Sprites.Texture, XY[i], s.Source, Color.White * (timeAlive <= 10 ? 1 : (timeAlive * 10 % MathF.Abs(25 - timeAlive) < 5)?.1f : 1), 0, s.Origin, .5f + (MathF.Sin((T.Total - SpawnTime[i] + 1) * 5) * .1f), 0, 0);
            }
        }

        public static void PutPowerID(this NetWriter w, IDS id) => w.Put(0, MAX_IDS - 1, (int)id);
        public static IDS ReadPowerID(this NetReader r) => (IDS)r.ReadInt(0, MAX_IDS - 1);
    }
}