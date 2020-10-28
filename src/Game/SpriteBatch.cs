using System;
using System.Reflection;

namespace Microsoft.Xna.Framework.Graphics {
    public enum RectStyle { Inline = 0, Centered = 1, Outline = 2 }

    public static class SpriteBatchExtensions {
        public static Texture2D Pixel { get; private set; }

        static readonly Game _game;
        static readonly Vector2 PixelOrigin = new Vector2(.5f);
        static readonly Vector2 _lineOrigin = new Vector2(0, .5f);

        static SpriteBatchExtensions() {
            foreach (var p in typeof(Game).GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static))
                if (p.GetValue(_game)is Game g)
                    _game = g;
        }

        public static void Init() {
            Pixel = new Texture2D(_game.GraphicsDevice, 1, 1);
            Pixel.SetData(new Color[] { Color.White });
        }

        public static void DrawPixel(this SpriteBatch s, Vector2 pos, Color color, float rotation = 0, float scale = 1, float layer = 0) => s.Draw(Pixel, pos, null, color, rotation, PixelOrigin, scale, 0, layer);

        public static void DrawRectangle(this SpriteBatch s, Rectangle rect, Color color, float rotation, Vector2 origin, RectStyle style, float thickness = 1, float layerDepth = 0) {
            var o = new Vector2(0, (int)style / 2f);
            float t = thickness * (1 - (int)style),
                offX = rect.X + origin.X,
                offY = rect.Y + origin.Y;
            rect.Rotate(rotation, origin, out var tl, out var tr, out var br, out var bl);
            s.Draw(Pixel, new Vector2(tl.X + offX, tl.Y + offY), null, color, MathF.Atan2(tr.Y - tl.Y, tr.X - tl.X), o, new Vector2(Vector2.Distance(tl, tr) - t, thickness), 0, layerDepth);
            s.Draw(Pixel, new Vector2(tr.X + offX, tr.Y + offY), null, color, MathF.Atan2(br.Y - tr.Y, br.X - tr.X), o, new Vector2(Vector2.Distance(tr, br) - t, thickness), 0, layerDepth);
            s.Draw(Pixel, new Vector2(br.X + offX, br.Y + offY), null, color, MathF.Atan2(bl.Y - br.Y, bl.X - br.X), o, new Vector2(Vector2.Distance(br, bl) - t, thickness), 0, layerDepth);
            s.Draw(Pixel, new Vector2(bl.X + offX, bl.Y + offY), null, color, MathF.Atan2(tl.Y - bl.Y, tl.X - bl.X), o, new Vector2(Vector2.Distance(bl, tl) - t, thickness), 0, layerDepth);
        }
        public static void FillRectangle(this SpriteBatch s, Rectangle rect, Color color, float rotation = 0, float layer = 0) => s.Draw(Pixel, rect, null, color, rotation, Vector2.Zero, 0, layer);
        public static void FillRectangle(this SpriteBatch s, Vector2 pos, Vector2 scale, Color color, float rotation = 0, float layer = 0) => s.Draw(Pixel, pos, null, color, rotation, PixelOrigin, scale, 0, layer);
        public static void FillRectangle(this SpriteBatch s, Vector2 pos, float scale, Color color, float rotation = 0, float layer = 0) => s.Draw(Pixel, pos, null, color, rotation, PixelOrigin, scale, 0, layer);
        public static void FillRectangle(this SpriteBatch s, Rectangle rect, Color color, float rotation, Vector2 origin, float layer = 0) => s.Draw(Pixel, rect, null, color, rotation, (rect.Size.ToVector2() / origin), 0, layer);
        /// <summary></summary>
        /// <param name="origin">This is scaled. 0.5 = center</param>
        public static void FillRectangle(this SpriteBatch s, Vector2 pos, Vector2 scale, Color color, float rotation, Vector2 origin, float layer = 0) => s.Draw(Pixel, pos, null, color, rotation, origin, scale, 0, layer);
        /// <summary></summary>
        /// <param name="origin">This is scaled. 0.5 = center</param>
        public static void FillRectangle(this SpriteBatch s, Vector2 pos, float scale, Color color, float rotation, Vector2 origin, float layer = 0) => s.Draw(Pixel, pos, null, color, rotation, origin, scale, 0, layer);

        public static void DrawLine(this SpriteBatch s, (Vector2 A, Vector2 B)pos, Color color, float thickness = 1, float layer = 0) => s.Draw(Pixel, pos.A, null, color, MathF.Atan2(pos.B.Y - pos.A.Y, pos.B.X - pos.A.X), _lineOrigin, new Vector2(Vector2.Distance(pos.A, pos.B), thickness), 0, layer);
    }
}