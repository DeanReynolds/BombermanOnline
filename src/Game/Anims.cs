using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BombermanOnline {
    static class Anims {
        public static Vector2[] XY { get; private set; }
        public static SpriteAnim[] Anim { get; private set; }
        public static Texture2D[] Sprite { get; private set; }

        public static int Count { get; private set; }

        public static void Init(int capacity) {
            XY = new Vector2[capacity];
            Anim = new SpriteAnim[capacity];
            Sprite = new Texture2D[capacity];
        }

        public static void Spawn(Vector2 xy, SpriteAnim anim, Texture2D sprite) {
            var i = Count++;
            XY[i] = xy;
            Anim[i] = anim;
            Sprite[i] = sprite;
        }
        public static bool Despawn(int i) {
#if DEBUG
            if (Count <= i)
                return false;
#endif
            --Count;
            XY[i] = XY[Count];
            Anim[i] = Anim[Count];
            Sprite[i] = Sprite[Count];
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
                G.SB.Draw(Sprite[i], XY[i], s.Source, Anim[i].Tint, Anim[i].Rotation, s.Origin, Anim[i].Scale, Anim[i].Effects, Anim[i].Layer);
            }
        }
    }
}