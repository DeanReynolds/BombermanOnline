using System;
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
            base.Initialize();
            InputHelper.Setup(this);
            foreach (var s in Assembly.GetExecutingAssembly().GetTypes().Where(x => x.IsClass && !x.IsAbstract && x.IsSubclassOf(typeof(Scr))))
                _scr.Add(s, (Scr)Activator.CreateInstance(s));
            Scr = _scr[typeof(MainScr)];
        }

        protected override void LoadContent() {
            SB = new SpriteBatch(GraphicsDevice);
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