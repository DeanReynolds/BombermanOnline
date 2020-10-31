using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace BombermanOnline {
    static class Animations {
        public static Vector2[] XY { get; private set; }
        public static SpriteAnim[] Anim { get; private set; }

        static readonly LinkedList<int> _freeIDs = new LinkedList<int>();
        static readonly SafeHashSet<int> _takenIDs = new SafeHashSet<int>();

        public static void Init(int capacity) {
            XY = new Vector2[capacity];
            Anim = new SpriteAnim[capacity];
            _takenIDs.Clear();
            _freeIDs.Clear();
            for (int i = 0; i < capacity; i++)
                _freeIDs.AddLast(i);
        }

        public static void Spawn(Vector2 xy, SpriteAnim anim) {
            var j = _freeIDs.Last.Value;
            _freeIDs.RemoveLast();
            XY[j] = xy;
            Anim[j] = anim;
            _takenIDs.Add(j);
        }
        public static void Despawn(int i) {
            if (!_takenIDs.Remove(i))
                return;
            _freeIDs.AddLast(i);
        }

        public static void Update() {
            foreach (var i in _takenIDs) {
                if (Anim[i].Finished)
                    Despawn(i);
                Anim[i].Update();
            }
        }
        public static void Draw() {
            foreach (var i in _takenIDs) {
                var s = Anim[i].Frames[Anim[i].Frame];
                G.SB.Draw(G.Sprites.Texture, XY[i], s.Source, Color.White, 0, s.Origin, 1, Anim[i].Effects, 0);
            }
        }
    }
}