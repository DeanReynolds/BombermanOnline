using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BombermanOnline {
    struct SpriteSheet {
        public static SpriteSheet Load(Texture2D texture, string dataFilePath) {
            var sprites = new Dictionary<string, Sprite>();
            string line;
            using var reader = new StreamReader($@"{Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\{G.Content.RootDirectory}\{dataFilePath}");
            while ((line = reader.ReadLine()) != null) {
                line = line.Trim();
                if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line))
                    continue;
                var s = line.Split(';');
                var name = s[0];
                // var isRotated = int.Parse(s[1]) == 1;
                var source = new Rectangle(int.Parse(s[2]), int.Parse(s[3]), int.Parse(s[4]), int.Parse(s[5]));
                var origin = new Vector2(float.Parse(s[8]) * source.Width, float.Parse(s[9]) * source.Height);
                sprites.Add(name,
                    new Sprite {
                        Source = source,
                            // IsRotated = isRotated,
                            Origin = origin
                    });
            }
            return new SpriteSheet(texture, sprites);
        }

        public readonly Texture2D Texture;

        readonly Dictionary<string, Sprite> _sprites;

        public SpriteSheet(Texture2D texture, Dictionary<string, Sprite> sprites) {
            Texture = texture;
            _sprites = sprites;
        }

        public Sprite this[string name] => _sprites[name];
    }
}