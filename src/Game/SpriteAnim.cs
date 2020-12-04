using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BombermanOnline {
    struct SpriteAnim {
        public int Frame {
            get {
                int v = (int)Math.Floor(_frame);
                return v >= Frames.Length ? MaxFrames - 1 : v;
            }
        }
        public bool Finished {
            get {
                switch (FinishMode) {
                    case FINISH_MODE.LAST_FRAME:
                        return (int)Math.Floor(_frame) >= MaxFrames;
                    case FINISH_MODE.NO_SCALE:
                        return Scale <= 0;
                }
                return false;
            }
        }

        public int MaxFrames => Frames.Length;

        public float SpeedPerFrame;
        public float Rotation;
        public float Scale;
        public SpriteEffects Effects;
        public float ScaleGain;
        public float ScaleGainSub;
        public float ScaleGainSubGain;
        public FINISH_MODE FinishMode;
        public Color Tint;
        public float Layer;

        public readonly Sprite[] Frames;

        public enum FINISH_MODE { LAST_FRAME, NO_SCALE }

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
            ScaleGain = 0;
            ScaleGainSub = 0;
            ScaleGainSubGain = 0;
            FinishMode = FINISH_MODE.LAST_FRAME;
            Tint = Color.White;
            Layer = 0;
        }

        public void Update() {
            if ((_frame += T.DeltaFull / SpeedPerFrame) >= MaxFrames)
                if (_shouldLoop)
                    _frame -= MaxFrames;
                else
                    _frame = MaxFrames;
            if (ScaleGainSub != 0) {
                Scale = MathF.Max(0, Scale + (ScaleGain * T.Delta));
                ScaleGain += ScaleGainSub * T.Delta;
                ScaleGainSub += ScaleGainSubGain * T.Delta;
            }
        }

        public void Restart() => _frame = 0;
    }
}