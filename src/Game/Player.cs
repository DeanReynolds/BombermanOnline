using Microsoft.Xna.Framework;

namespace BombermanOnline {
    static class Player {
        public static Vector2[] XY { get; private set; }
        public static DIR[] Dir { get; private set; }

        public enum DIR { NORTH, EAST, SOUTH, WEST }
    }
}