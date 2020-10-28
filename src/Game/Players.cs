using System;
using System.Collections.Generic;
using Apos.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BombermanOnline {
    static class Players {
        public const int HITBOX_SIZE = 12;
        public const float HALF_HITBOX_SIZE = HITBOX_SIZE / 2f;

        public static int MaxPlayers { get; private set; }
        public static int LocalID { get; private set; } = -1;
        public static Vector2[] XY { get; private set; }
        public static DIR[] Dir { get; private set; }
        public static SpriteAnim[] Anim { get; private set; }
        public static FLAGS[] Flags { get; private set; }

        public enum DIR { NORTH, EAST, SOUTH, WEST }

        [Flags]
        public enum FLAGS : byte { CAN_KICK_BOMBS = 1, BOMBS_CAN_PENETRATE = 2, HAS_LOUIE = 4 }

        static readonly LinkedList<int> _freeIDs = new LinkedList<int>();
        static readonly HashSet<int> _takenIDs = new HashSet<int>();

        internal static void Init(int maxPlayers) {
            MaxPlayers = maxPlayers;
            XY = new Vector2[maxPlayers];
            Dir = new DIR[maxPlayers];
            Anim = new SpriteAnim[maxPlayers];
            Flags = new FLAGS[maxPlayers];
            _takenIDs.Clear();
            _freeIDs.Clear();
            for (int i = 0; i < maxPlayers; i++)
                _freeIDs.AddLast(i);
        }

        internal static void Insert(int i) {
            _freeIDs.Remove(i);
            _takenIDs.Add(i);
        }
        internal static void InsertLocal(int i) {
            Insert(i);
            LocalID = i;
        }
        internal static void Remove(int i) {
            if (!_takenIDs.Remove(i))
                return;
            XY[i] = new Vector2(float.MinValue);
            _freeIDs.AddLast(i);
        }
        internal static void Clear() {
            foreach (int i in _takenIDs)
                Remove(i);
            LocalID = -1;
        }
        internal static int PopFreeID() {
            int i = _freeIDs.Last.Value;
            _freeIDs.RemoveLast();
            return i;
        }

        public static void Update() {
            var moveSpd = 50 * T.Delta;
            var ptx = ((int)XY[LocalID].X) >> Tile.BITS_PER_SIZE;
            var pty = ((int)XY[LocalID].Y) >> Tile.BITS_PER_SIZE;
            var dir = 0;
            if (KeyboardCondition.Held(Keys.A)) {
                XY[LocalID].X -= moveSpd;
                dir = -1;
            }
            if (KeyboardCondition.Held(Keys.D)) {
                XY[LocalID].X += moveSpd;
                dir = 1;
            }
            var di = false;
            var hb = new Rectangle((int)(XY[LocalID].X - HALF_HITBOX_SIZE), (int)(XY[LocalID].Y - HALF_HITBOX_SIZE), HITBOX_SIZE, HITBOX_SIZE);
            var pdt = (((int)XY[LocalID].X) >> Tile.BITS_PER_SIZE) + (dir * 2);
            var nt = 100;
            if (dir != 0) {
                for (var y = -1; y <= 1; y++) {
                    var ry = pty + y;
                    if (ry < 0 || ry > G.Tiles.GetLength(1))
                        continue;
                    for (var x = ptx + dir; x != pdt; x += dir) {
                        if (x < 0 || x >= G.Tiles.GetLength(0))
                            break;
                        if (G.IsTileSolid(x, ry) && hb.Intersects(new Rectangle(x << Tile.BITS_PER_SIZE, ry << Tile.BITS_PER_SIZE, Tile.SIZE, Tile.SIZE))) {
                            if (MathF.Abs(ptx - x) < MathF.Abs(ptx - nt))
                                nt = x;
                            di = true;
                            break;
                        }
                    }
                }
                if (di)
                    XY[LocalID].X = (nt << Tile.BITS_PER_SIZE) + (dir < 0 ? Tile.SIZE : 0) - (dir * (hb.Width / 2f));
            }
            dir = 0;
            if (KeyboardCondition.Held(Keys.W)) {
                XY[LocalID].Y -= moveSpd;
                dir = -1;
            }
            if (KeyboardCondition.Held(Keys.S)) {
                XY[LocalID].Y += moveSpd;
                dir = 1;
            }
            if (dir != 0) {
                hb = new Rectangle((int)(XY[LocalID].X - HALF_HITBOX_SIZE), (int)(XY[LocalID].Y - HALF_HITBOX_SIZE), HITBOX_SIZE, HITBOX_SIZE);
                pdt = (((int)XY[LocalID].Y) >> Tile.BITS_PER_SIZE) + (dir * 2);
                nt = 100;
                di = false;
                for (var x = -1; x <= 1; x++) {
                    var rx = ptx + x;
                    if (rx < 0 || rx > G.Tiles.GetLength(0))
                        continue;
                    for (var y = pty + dir; y != pdt; y += dir) {
                        if (y < 0 || y >= G.Tiles.GetLength(1))
                            break;
                        if (G.IsTileSolid(rx, y) && hb.Intersects(new Rectangle(rx << Tile.BITS_PER_SIZE, y << Tile.BITS_PER_SIZE, Tile.SIZE, Tile.SIZE))) {
                            if (MathF.Abs(pty - y) < MathF.Abs(pty - nt))
                                nt = y;
                            di = true;
                            break;
                        }
                    }
                }
                if (di)
                    XY[LocalID].Y = (nt << Tile.BITS_PER_SIZE) + (dir < 0 ? Tile.SIZE : 0) - (dir * (hb.Height / 2f));
            }
            foreach (var i in _takenIDs) {

            }
        }
        public static void Draw() {
            foreach (var i in _takenIDs) {
                var hb = new Rectangle((int)(XY[i].X - HALF_HITBOX_SIZE), (int)(XY[i].Y - HALF_HITBOX_SIZE), HITBOX_SIZE, HITBOX_SIZE);
                G.SB.FillRectangle(hb, Color.Blue);
            }
        }
    }
}