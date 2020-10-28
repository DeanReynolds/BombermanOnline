using System;
using Dcrew.Camera;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BombermanOnline {
    sealed class GameScr : Scr {
        public static Camera Camera;

        public override void Open() {
            G.MakeMap(25, 25);
            int w = G.Tiles.GetLength(0) * 16,
                h = G.Tiles.GetLength(1) * 16;
            Camera = new Camera(new Vector2(w / 2f, h / 2f), (w, h));
            Players.Init(8);
            Players.InsertLocal(Players.PopFreeID());
            Players.XY[Players.LocalID] = new Vector2(24, 24);
        }
        public override void Close() {
            Camera.Dispose();
            Camera = null;
        }

        public override void Update() {
            Players.Update();
        }
        public override void Draw() {
            const float piOver2 = MathF.PI;
            G.SB.Begin(samplerState: SamplerState.PointClamp, transformMatrix: Camera.View(0));
            for (var y = 0; y < G.Tiles.GetLength(1); y++)
                for (var x = 0; x < G.Tiles.GetLength(0); x++) {
                    var sprite = G.Sprites[G.Tiles[x, y].ID.ToString()];
                    G.SB.Draw(sprite.Texture, new Rectangle(x << Tile.BITS_PER_SIZE, y << Tile.BITS_PER_SIZE, Tile.SIZE, Tile.SIZE), sprite.Source, Color.White, sprite.IsRotated?piOver2 : 0, Vector2.Zero, 0, 0);
                }
            Players.Draw();
            G.SB.End();
        }
    }
}