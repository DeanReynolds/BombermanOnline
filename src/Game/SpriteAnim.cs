using System;
using Microsoft.Xna.Framework.Graphics;

namespace BombermanOnline {
    struct SpriteAnim {
        public int Frame {
            get {
                int v = (int)Math.Floor(_frame);
                return v >= Frames.Length ? MaxFrames - 1 : v;
            }
        }
        public bool Finished => (int)Math.Floor(_frame) >= MaxFrames;
        public int MaxFrames => Frames.Length;

        public SpriteEffects Effects;

        public readonly Sprite[] Frames;

        double _frame;

        readonly bool _shouldLoop;
        readonly float _speed;

        public SpriteAnim(bool shouldLoop, float speed, SpriteEffects effects, params Sprite[] sprites) {
            _shouldLoop = shouldLoop;
            _speed = speed / sprites.Length;
            Effects = effects;
            Frames = sprites;
            _frame = 0;
        }

        public void Update() {
            if ((_frame += T.DeltaFull / _speed) >= MaxFrames)
                if (_shouldLoop)
                    _frame -= MaxFrames;
                else
                    _frame = MaxFrames;
        }
    }
}