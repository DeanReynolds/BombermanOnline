﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Apos.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BombermanOnline {
    sealed class G : Game {
        public static new ContentManager Content { get; private set; }
        public static SpriteBatch SB { get; private set; }
        public static Scr Scr { get; private set; }
        public static SpriteSheet Sprites { get; private set; }
        public static Tile[, ] Tiles { get; private set; }

        static GraphicsDeviceManager _gfx;

        static readonly AnyCondition _quit =
            new AnyCondition(
                new KeyboardCondition(Keys.Escape),
                new GamePadCondition(GamePadButton.Back, 0)
            );
        static readonly Dictionary<Type, Scr> _scr = new Dictionary<Type, Scr>();

        public static void SetScr<T>()where T : Scr {
            Scr.Close();
            Scr = _scr[typeof(T)];
            Scr.Open();
        }

        public static void MakeMap(int width, int height) {
            Tiles = new Tile[width, height];
            for (var y = 1; y < height - 1; y++)
                for (var x = 1; x < width - 1; x++)
                    Tiles[x, y].ID = Tile.IDS.wall;
            for (var y = 1; y < 3; y++)
                for (var x = 1; x < 3; x++)
                    Tiles[x, y].ID = Tile.IDS.grass;
            for (var y = 1; y < 3; y++)
                for (var x = 1; x < 3; x++)
                    Tiles[width - 1 - x, y].ID = Tile.IDS.grass;
            for (var y = 1; y < 3; y++)
                for (var x = 1; x < 3; x++)
                    Tiles[x, height - 1 - y].ID = Tile.IDS.grass;
            for (var y = 1; y < 3; y++)
                for (var x = 1; x < 3; x++)
                    Tiles[width - 1 - x, height - 1 - y].ID = Tile.IDS.grass;
            for (var y = 0; y < height; y++) {
                Tiles[0, y].ID = Tile.IDS.bound0;
                Tiles[width - 1, y].ID = Tile.IDS.bound0;
            }
            for (var x = 1; x < width - 1; x++) {
                Tiles[x, 0].ID = Tile.IDS.bound0;
                Tiles[x, height - 1].ID = Tile.IDS.bound0;
            }
            for (var y = 2; y < height - 2; y += 2)
                for (var x = 2; x < width - 2; x += 2)
                    Tiles[x, y].ID = Tile.IDS.bound0;
        }

        public static bool IsTileSolid(int x, int y) {
            switch (Tiles[x, y].ID) {
                case Tile.IDS.wall:
                case Tile.IDS.bound0:
                case Tile.IDS.bound1:
                    return true;
                default:
                    return false;
            }
        }

        public G() {
            _gfx = new GraphicsDeviceManager(this) {
                PreferredBackBufferWidth = 1280,
                PreferredBackBufferHeight = 720,
                IsFullScreen = false,
                HardwareModeSwitch = false
            };
            Content = base.Content;
            Content.RootDirectory = "Content";
        }

        protected override void Initialize() {
            Window.Title = "Bomberman Online";
            Window.AllowUserResizing = true;
            IsMouseVisible = true;
            InputHelper.Setup(this);
            foreach (var s in Assembly.GetExecutingAssembly().GetTypes().Where(x => x.IsClass && !x.IsAbstract && x.IsSubclassOf(typeof(Scr))))
                _scr.Add(s, (Scr)Activator.CreateInstance(s));
            Scr = _scr[typeof(GameScr)];
            base.Initialize();
        }

        protected override void LoadContent() {
            SB = new SpriteBatch(GraphicsDevice);
            SpriteBatchExtensions.Init();
            Sprites = SpriteSheet.Load(Content.Load<Texture2D>("sprites"), "sprites.dat");
            Bombs.Init(50);
            Scr.Open();
        }

        protected override void Update(GameTime gameTime) {
            T.Total = (float)(T.TotalFull = gameTime.TotalGameTime.TotalSeconds);
            T.Delta = (float)(T.DeltaFull = gameTime.ElapsedGameTime.TotalSeconds);
            InputHelper.UpdateSetup();
            if (_quit.Pressed())
                Exit();
            base.Update(gameTime);
            Scr.Update();
            InputHelper.UpdateCleanup();
        }

        protected override void Draw(GameTime gameTime) {
            GraphicsDevice.Clear(Color.Black);
            Scr.Draw();
            base.Draw(gameTime);
        }
    }
}