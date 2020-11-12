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

        public float SpeedPerFrame;
        public float Rotation;
        public float Scale;
        public SpriteEffects Effects;

        public readonly Sprite[] Frames;

        double _frame;

        readonly bool _shouldLoop;

        public SpriteAnim(bool shouldLoop, float speed, float rotation, float scale, SpriteEffects effects, params Sprite[] sprites) {
            _shouldLoop = shouldLoop;
            SpeedPerFrame = speed / sprites.Length;
            Rotation = rotation;
            Scale = scale;
            Effects = effects;
            Frames = sprites;
            _frame = 0;
            for (var i = 0; i < Frames.Length; i++) {
                if (Effects.HasFlag(SpriteEffects.FlipHorizontally))
                    Frames[i].Origin.X = Frames[i].Source.Width - Frames[i].Origin.X;
                if (Effects.HasFlag(SpriteEffects.FlipVertically))
                    Frames[i].Origin.Y = Frames[i].Source.Width - Frames[i].Origin.Y;
            }
        }

        public void Update() {
            if ((_frame += T.DeltaFull / SpeedPerFrame) >= MaxFrames)
                if (_shouldLoop)
                    _frame -= MaxFrames;
                else
                    _frame = MaxFrames;
        }

        public void Restart() => _frame = 0;
    }
}