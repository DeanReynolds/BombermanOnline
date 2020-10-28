using System;

namespace Microsoft.Xna.Framework {
    public static class RectangleExtensions {
        public static Rectangle Rotate(float width, float height, float angle, Vector2 origin) {
            float cos = MathF.Cos(angle),
                sin = MathF.Sin(angle),
                x = -origin.X,
                y = -origin.Y,
                w = width + x,
                h = height + y,
                xcos = x * cos,
                ycos = y * cos,
                xsin = x * sin,
                ysin = y * sin,
                wcos = w * cos,
                wsin = w * sin,
                hcos = h * cos,
                hsin = h * sin,
                tlx = xcos - ysin,
                tly = xsin + ycos,
                trx = wcos - ysin,
                tr_y = wsin + ycos,
                brx = wcos - hsin,
                bry = wsin + hcos,
                blx = xcos - hsin,
                bly = xsin + hcos,
                minx = tlx,
                miny = tly,
                maxx = minx,
                maxy = miny;
            if (trx < minx)
                minx = trx;
            if (brx < minx)
                minx = brx;
            if (blx < minx)
                minx = blx;
            if (tr_y < miny)
                miny = tr_y;
            if (bry < miny)
                miny = bry;
            if (bly < miny)
                miny = bly;
            if (trx > maxx)
                maxx = trx;
            if (brx > maxx)
                maxx = brx;
            if (blx > maxx)
                maxx = blx;
            if (tr_y > maxy)
                maxy = tr_y;
            if (bry > maxy)
                maxy = bry;
            if (bly > maxy)
                maxy = bly;
            return new Rectangle((int)minx, (int)miny, (int)MathF.Ceiling(maxx - minx), (int)MathF.Ceiling(maxy - miny));
        }
        public static Rectangle Rotate(this Rectangle area, float angle, Vector2 origin) {
            var r = Rotate(area.Width, area.Height, angle, origin);
            r.Offset(area.X + origin.X, area.Y + origin.Y);
            return r;
        }
        public static void Rotate(this Rectangle area, float angle, Vector2 origin, out Vector2 topLeft, out Vector2 topRight, out Vector2 bottomRight, out Vector2 bottomLeft) {
            float cos = MathF.Cos(angle),
                sin = MathF.Sin(angle),
                x = -origin.X,
                y = -origin.Y,
                w = area.Width + x,
                h = area.Height + y,
                xcos = x * cos,
                ycos = y * cos,
                xsin = x * sin,
                ysin = y * sin,
                wcos = w * cos,
                wsin = w * sin,
                hcos = h * cos,
                hsin = h * sin;
            topLeft = new Vector2(xcos - ysin, xsin + ycos);
            topRight = new Vector2(wcos - ysin, wsin + ycos);
            bottomRight = new Vector2(wcos - hsin, wsin + hcos);
            bottomLeft = new Vector2(xcos - hsin, xsin + hcos);
        }
    }
}