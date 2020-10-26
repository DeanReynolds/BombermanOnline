using Dcrew.Spatial;
using Microsoft.Xna.Framework;

namespace BombermanOnline {
    class SpatialItem : IBounds {
        public int ObjectID;

        public Vector2 Position {
            get => _bounds.XY;
            set => _bounds.XY = value;
        }

        public Vector2 Size {
            get => _bounds.Size;
            set => _bounds.Size = value;
        }

        public Vector2 Origin {
            get => _bounds.Origin;
            set => _bounds.Origin = value;
        }

        public RotRect Bounds => _bounds;

        private RotRect _bounds = new RotRect { Size = new Vector2(32, 32) };
    }
}