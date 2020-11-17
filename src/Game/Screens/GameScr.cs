using System;
using Dcrew.Camera;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BombermanOnline {
    sealed class GameScr : Scr {
        public static Camera Camera;
        public static RenderTarget2D GameTexture;

        public override void Open() {
            int w = G.Tiles.GetLength(0) << Tile.BITS_PER_SIZE,
                h = G.Tiles.GetLength(1) << Tile.BITS_PER_SIZE;
            GameTexture = new RenderTarget2D(G.SB.GraphicsDevice, w, h);
            Camera = new Camera(new Vector2(w >> 1, h >> 1), (w, h));
        }
        public override void Close() {
            Camera.Dispose();
            Camera = null;
        }

        public override void Update() {
            Players.Update();
            Powers.Update();
            Anims.Update();
            Bombs.Update();
        }
        public override void Draw() {
            G.SB.Begin(samplerState: SamplerState.PointClamp, transformMatrix: Camera.View(0));
            for (var y = 0; y < G.Tiles.GetLength(1); y++)
                for (var x = 0; x < G.Tiles.GetLength(0); x++) {
                    var sprite = G.Sprites[G.Tiles[x, y].ID.ToString()];
                    G.SB.Draw(G.Sprites.Texture, new Rectangle(x << Tile.BITS_PER_SIZE, y << Tile.BITS_PER_SIZE, Tile.SIZE, Tile.SIZE), sprite.Source, Color.White, 0, Vector2.Zero, 0, 0);
                }
            Powers.Draw();
            Bombs.Draw();
            Anims.Draw();
            Players.Draw();
            G.SB.End();

            // G.SB.GraphicsDevice.SetRenderTarget(GameTexture);
            // G.SB.Begin(samplerState: SamplerState.PointClamp);
            // for (var y = 0; y < G.Tiles.GetLength(1); y++)
            //     for (var x = 0; x < G.Tiles.GetLength(0); x++) {
            //         var sprite = G.Sprites[G.Tiles[x, y].ID.ToString()];
            //         G.SB.Draw(G.Sprites.Texture, new Rectangle(x << Tile.BITS_PER_SIZE, y << Tile.BITS_PER_SIZE, Tile.SIZE, Tile.SIZE), sprite.Source, Color.White, 0, Vector2.Zero, 0, 0);
            //     }
            // G.SB.End();
            // G.SB.Begin(samplerState: SamplerState.PointClamp);
            // Powers.Draw();
            // Bombs.Draw();
            // Anims.Draw();
            // Players.Draw();
            // G.SB.End();
            // G.SB.GraphicsDevice.SetRenderTarget(null);
            // G.SB.Begin(samplerState: SamplerState.PointClamp);
            // G.SB.Draw(GameTexture, G.RenderRect, Color.White);
            // G.SB.End();
        }
    }
}