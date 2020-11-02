namespace BombermanOnline {
    struct PlayerPowers {
        public const int MAX_FIRE = 10,
            MAX_BOMBS = 8,
            MAX_SPEED = 3;
        public const sbyte MIN_SPEED = -3;

        public byte Fire;
        public byte Bombs;
        public sbyte Speed;
    }
}