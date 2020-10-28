namespace BombermanOnline {
    struct Tile {
        public const int SIZE = 16;
        public const int HALF_SIZE = SIZE / 2;
        public const int BITS_PER_SIZE = 4;

        public IDS ID;

        public enum IDS { grass, wall, bound0, bound1 }
    }
}