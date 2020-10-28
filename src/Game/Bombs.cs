using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace BombermanOnline {
    static class Bombs {
        public static Vector2[] XY { get; private set; }
        public static double[] TimeLeft { get; private set; }
        public static FLAGS[] Flags { get; private set; }

        [Flags]
        public enum FLAGS : byte { HAS_PENETRATION = 1 }

        static readonly LinkedList<int> _freeIDs = new LinkedList<int>();
        static readonly HashSet<int> _takenIDs = new HashSet<int>();

        public static void Init(int capacity) {
            XY = new Vector2[capacity];
            TimeLeft = new double[capacity];
            Flags = new FLAGS[capacity];
            _takenIDs.Clear();
            _freeIDs.Clear();
            for (int i = 0; i < capacity; i++)
                _freeIDs.AddLast(i);
        }

        public static void Add(int x, int y, FLAGS flags) {
            var id = _freeIDs.Last.Value;
            _freeIDs.RemoveLast();
            XY[id] = new Vector2(x >> Tile.BITS_PER_SIZE, y >> Tile.BITS_PER_SIZE);
            TimeLeft[id] = 3.5;
            Flags[id] = flags;
            _takenIDs.Add(id);
        }

        public static void Update() {
            foreach (var i in _takenIDs) {
                TimeLeft[i] -= T.DeltaFull;
            }
        }
    }
}