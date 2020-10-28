using System;

namespace BombermanOnline {
    struct SpriteAnim {
        public int Frame => (int)Math.Floor(_frame);

        double _frame;
        byte _maxFrames;
        bool _shouldLoop;

        public SpriteAnim(byte maxFrames, bool shouldLoop) {
            _maxFrames = maxFrames;
            _frame = 0;
            _shouldLoop = shouldLoop;
        }

        public void Update() {
            if ((_frame += T.DeltaFull) >= _maxFrames)
                if (_shouldLoop)
                    _frame -= _maxFrames;
                else
                    _frame = _maxFrames;
        }
    }
}