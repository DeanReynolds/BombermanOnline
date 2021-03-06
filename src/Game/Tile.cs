using System;
namespace BombermanOnline {
    struct Tile {
        public const int SIZE = 16;
        public const int HALF_SIZE = SIZE / 2;
        public const int BITS_PER_SIZE = 4;
        public static readonly int MAX_ID = Enum.GetValues(typeof(IDS)).Length - 1;

        public IDS ID;
        public SpriteAnim Anim;

        public enum IDS { floor, wall, bound }
    }
}