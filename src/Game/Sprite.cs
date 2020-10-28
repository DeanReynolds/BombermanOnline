using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BombermanOnline {
    struct Sprite {
        public Texture2D Texture;
        public Rectangle Source;
        public bool IsRotated;
        public Vector2 Origin;
    }
}