using System;
using System.Collections.Generic;
using Apos.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BombermanOnline {
    static class Players {
        public const int HITBOX_WIDTH = 16,
            HITBOX_HEIGHT = 10;

        public static int MaxPlayers { get; private set; }
        public static int LocalID { get; private set; } = -1;
        public static Vector2[] XY { get; private set; }
        public static INPUT[] Input { get; private set; }
        public static DIR[] Dir { get; private set; }
        public static SpriteAnim[] Anim { get; private set; }
        public static FLAGS[] Flags { get; private set; }

        public static readonly HashSet<int> AlivePlayers = new HashSet<int>();

        public enum DIR { NORTH = 0, EAST = 1, SOUTH = 2, WEST = 3 }

        [Flags]
        public enum INPUT : byte { MOV_UP = 1, MOV_DOWN = 2, MOV_RIGHT = 4, MOV_LEFT = 8 }

        [Flags]
        public enum FLAGS : byte { IS_DEAD = 1, CAN_KICK_BOMBS = 2, BOMBS_CAN_PIERCE = 4, HAS_LOUIE = 8 }

        static readonly LinkedList<int> _freeIDs = new LinkedList<int>();
        static readonly HashSet<int> _takenIDs = new HashSet<int>();

        internal static void Init(int capacity) {
            MaxPlayers = capacity;
            XY = new Vector2[capacity];
            Input = new INPUT[capacity];
            Dir = new DIR[capacity];
            Anim = new SpriteAnim[capacity];
            Flags = new FLAGS[capacity];
            _takenIDs.Clear();
            _freeIDs.Clear();
            for (int i = 0; i < capacity; i++)
                _freeIDs.AddLast(i);
        }

        internal static void Insert(int i) {
            _freeIDs.Remove(i);
            _takenIDs.Add(i);
            // Flags[i] = FLAGS.IS_DEAD;
            XY[i] = new Vector2(24, 24);
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
            if (!Flags[LocalID].HasFlag(FLAGS.IS_DEAD)) {
                Input[LocalID] = 0;
                if (KeyboardCondition.Held(Keys.W)) {
                    if (!KeyboardCondition.Held(Keys.S))
                        Input[LocalID] |= INPUT.MOV_UP;
                } else if (KeyboardCondition.Held(Keys.S))
                    Input[LocalID] |= INPUT.MOV_DOWN;
                if (KeyboardCondition.Held(Keys.A)) {
                    if (!KeyboardCondition.Held(Keys.D))
                        Input[LocalID] |= INPUT.MOV_LEFT;
                } else if (KeyboardCondition.Held(Keys.D))
                    Input[LocalID] |= INPUT.MOV_RIGHT;
                if (KeyboardCondition.Held(Keys.Space)) {
                    int x = (int)XY[LocalID].X >> Tile.BITS_PER_SIZE,
                        y = (int)XY[LocalID].Y >> Tile.BITS_PER_SIZE;
                    if (!Bombs.HasBomb(x, y, out _)) {
                        Bombs.Spawn(x, y, 0);
                    }
                }
            }
            float moveSpd = 50 * T.Delta, oldXY;
            int ptx, pty, dir;
            foreach (var i in _takenIDs) {
                ptx = ((int)XY[i].X) >> Tile.BITS_PER_SIZE;
                pty = ((int)XY[i].Y) >> Tile.BITS_PER_SIZE;
                dir = 0;
                oldXY = XY[i].X;
                if (Input[i].HasFlag(INPUT.MOV_LEFT)) {
                    XY[i].X -= moveSpd;
                    dir = -1;
                } else if (Input[i].HasFlag(INPUT.MOV_RIGHT)) {
                    XY[i].X += moveSpd;
                    dir = 1;
                }
                var di = false;
                var hb = new Rectangle((int)(XY[i].X - (HITBOX_WIDTH >> 1)), (int)(XY[i].Y - (HITBOX_HEIGHT >> 1)), HITBOX_WIDTH, HITBOX_HEIGHT);
                var pdt = (((int)XY[i].X) >> Tile.BITS_PER_SIZE) + (dir * 2);
                var nt = 100;
                if (dir != 0) {
                    for (var y = -1; y <= 1; y++) {
                        var ry = pty + y;
                        if (ry < 0 || ry > G.Tiles.GetLength(1))
                            continue;
                        for (var x = ptx + dir; x != pdt; x += dir) {
                            if (x < 0 || x >= G.Tiles.GetLength(0))
                                break;
                            if ((G.IsTileSolid(x, ry) || Bombs.HasBomb(x, ry, out _)) && hb.Intersects(new Rectangle(x << Tile.BITS_PER_SIZE, ry << Tile.BITS_PER_SIZE, Tile.SIZE, Tile.SIZE))) {
                                if (MathF.Abs(ptx - x) < MathF.Abs(ptx - nt))
                                    nt = x;
                                di = true;
                                break;
                            }
                        }
                    }
                    if (di)
                        XY[i].X = (nt << Tile.BITS_PER_SIZE) + (dir < 0 ? Tile.SIZE : 0) - (dir * (hb.Width / 2f));
                    if (XY[i].X != oldXY)
                        Dir[i] = dir == 1 ? DIR.EAST : DIR.WEST;
                }
                dir = 0;
                oldXY = XY[i].Y;
                if (Input[i].HasFlag(INPUT.MOV_UP)) {
                    XY[i].Y -= moveSpd;
                    dir = -1;
                } else if (Input[i].HasFlag(INPUT.MOV_DOWN)) {
                    XY[i].Y += moveSpd;
                    dir = 1;
                }
                if (dir != 0) {
                    hb = new Rectangle((int)(XY[i].X - (HITBOX_WIDTH >> 1)), (int)(XY[i].Y - (HITBOX_HEIGHT >> 1)), HITBOX_WIDTH, HITBOX_HEIGHT);
                    pdt = (((int)XY[i].Y) >> Tile.BITS_PER_SIZE) + (dir * 2);
                    nt = 100;
                    di = false;
                    for (var x = -1; x <= 1; x++) {
                        var rx = ptx + x;
                        if (rx < 0 || rx > G.Tiles.GetLength(0))
                            continue;
                        for (var y = pty + dir; y != pdt; y += dir) {
                            if (y < 0 || y >= G.Tiles.GetLength(1))
                                break;
                            if ((G.IsTileSolid(rx, y) || Bombs.HasBomb(rx, y, out _)) && hb.Intersects(new Rectangle(rx << Tile.BITS_PER_SIZE, y << Tile.BITS_PER_SIZE, Tile.SIZE, Tile.SIZE))) {
                                if (MathF.Abs(pty - y) < MathF.Abs(pty - nt))
                                    nt = y;
                                di = true;
                                break;
                            }
                        }
                    }
                    if (di)
                        XY[i].Y = (nt << Tile.BITS_PER_SIZE) + (dir < 0 ? Tile.SIZE : 0) - (dir * (hb.Height / 2f));
                    if (XY[i].Y != oldXY)
                        Dir[i] = dir == 1 ? DIR.SOUTH : DIR.NORTH;
                }
            }
        }
        public static void Draw() {
            foreach (var i in _takenIDs) {
                var hb = new Rectangle((int)(XY[i].X - (HITBOX_WIDTH >> 1)), (int)(XY[i].Y - (HITBOX_HEIGHT >> 1)), HITBOX_WIDTH, HITBOX_HEIGHT);
                G.SB.FillRectangle(hb, Color.Blue);
                var dir = (int)Dir[i];
                SpriteEffects effect = 0;
                var xy = XY[i].ToPoint().ToVector2();
                if (dir == 3) {
                    dir = 1;
                    effect = SpriteEffects.FlipHorizontally;
                    xy.X += 2;
                }
                var s = G.Sprites[$"p{dir}0"];
                G.SB.Draw(s.Texture, xy, s.Source, Color.White, 0, s.Origin, 1, effect, 0);
            }
        }
    }
}