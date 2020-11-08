using Microsoft.Xna.Framework;

namespace BombermanOnline {
    static class Anims {
        public static Vector2[] XY { get; private set; }
        public static SpriteAnim[] Anim { get; private set; }

        public static int Count { get; private set; }

        public static void Init(int capacity) {
            XY = new Vector2[capacity];
            Anim = new SpriteAnim[capacity];
        }

        public static void Spawn(Vector2 xy, SpriteAnim anim) {
            var i = Count++;
            XY[i] = xy;
            Anim[i] = anim;
        }
        public static bool Despawn(int i) {
#if DEBUG
            if (Count <= i)
                return false;
#endif
            --Count;
            XY[i] = XY[Count];
            Anim[i] = Anim[Count];
            return true;
        }
        public static void DespawnAll() {
            Count = 0;
        }

        public static void Update() {
            for (var i = 0; i < Count; i++) {
                var finished = Anim[i].Finished;
                Anim[i].Update();
                if (finished)
                    Despawn(i--);
            }
        }
        public static void Draw() {
            for (var i = 0; i < Count; i++) {
                var s = Anim[i].Frames[Anim[i].Frame];
                G.SB.Draw(G.Sprites.Texture, XY[i], s.Source, Color.White, 0, s.Origin, 1, Anim[i].Effects, 0);
            }
        }
    }
}