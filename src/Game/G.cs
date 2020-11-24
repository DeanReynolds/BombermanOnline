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
        public static Viewport Viewport { get; private set; }
        public static Rectangle RenderRect { get; private set; }
        public static Scr Scr { get; private set; }
        public static SpriteSheet Sprites { get; private set; }
        public static SpriteSheet PlayerSprites { get; private set; }
        public static Tile[, ] Tiles { get; private set; }
        public static FastRng Rng { get; private set; }

        static GraphicsDeviceManager _gfx;

        static readonly AnyCondition _quit =
            new AnyCondition(
                new KeyboardCondition(Keys.Escape),
                new GamePadCondition(GamePadButton.Back, 0)
            );
        static readonly Dictionary<Type, Scr> _scr = new Dictionary<Type, Scr>();
        static readonly Dictionary<PlayerColors, Texture2D> _players = new Dictionary<PlayerColors, Texture2D>();

        static Color[] _playerSpritesTexture;

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
        public static Texture2D GetPlayer(PlayerColors colors) {
            if (_players.TryGetValue(colors, out var texture))
                return texture;
            var darkBody = Color.Lerp(colors.Body, Color.Black, .5f);
            var newPx = new Color[_playerSpritesTexture.Length];
            Array.Copy(_playerSpritesTexture, newPx, _playerSpritesTexture.Length);
            for (var i = 0; i < _playerSpritesTexture.Length; i++) {
                if (_playerSpritesTexture[i].A == 0)
                    continue;
                if (_playerSpritesTexture[i].PackedValue == 4294967295)
                    newPx[i] = colors.Body;
                else if (_playerSpritesTexture[i].PackedValue == 4287926932)
                    newPx[i] = darkBody;
                else if (_playerSpritesTexture[i].PackedValue == 4279800575)
                    newPx[i] = colors.Skin;
                else if (_playerSpritesTexture[i].PackedValue == 4290576639)
                    newPx[i] = colors.Accessories;
                else if (_playerSpritesTexture[i].PackedValue == 4293356800)
                    newPx[i] = colors.Clothes;
            }
            var newTexture = new Texture2D(_gfx.GraphicsDevice, PlayerSprites.Texture.Width, PlayerSprites.Texture.Height);
            newTexture.SetData(newPx);
            _players.Add(colors, newTexture);
            return newTexture;
        }

        public G() {
            _gfx = new GraphicsDeviceManager(this) {
                PreferredBackBufferWidth = 1280,
                PreferredBackBufferHeight = 720,
                IsFullScreen = false,
                HardwareModeSwitch = false,
                SynchronizeWithVerticalRetrace = false,
                GraphicsProfile = GraphicsProfile.HiDef
            };
            Content = base.Content;
            Content.RootDirectory = "Content";
        }

        private void OnScreenSizeChange(object sender, EventArgs e) {
            float outputAspectRatio = Window.ClientBounds.Width / (float)Window.ClientBounds.Height,
                preferredAspectRatio = 1;
            if (preferredAspectRatio > 0f) {
                if (outputAspectRatio <= preferredAspectRatio) {
                    int presentHeight = (int)((Window.ClientBounds.Width / preferredAspectRatio) + .5f);
                    int barHeight = (Window.ClientBounds.Height - presentHeight) / 2;
                    RenderRect = new Rectangle(0, barHeight, Window.ClientBounds.Width, presentHeight);
                } else {
                    int presentWidth = (int)((Window.ClientBounds.Height * preferredAspectRatio) + .5f);
                    int barWidth = (Window.ClientBounds.Width - presentWidth) / 2;
                    RenderRect = new Rectangle(barWidth, 0, presentWidth, Window.ClientBounds.Height);
                }
            }
        }

        protected override void Initialize() {
            Window.Title = "Bomberman Online";
            Window.AllowUserResizing = true;
            IsMouseVisible = true;
            IsFixedTimeStep = false;
            InputHelper.Setup(this);
            foreach (var s in Assembly.GetExecutingAssembly().GetTypes().Where(x => x.IsClass && !x.IsAbstract && x.IsSubclassOf(typeof(Scr))))
                _scr.Add(s, (Scr)Activator.CreateInstance(s));
            Scr = _scr[typeof(MainScr)];
            Rng = new FastRng();
            base.Initialize();
            Window.ClientSizeChanged += OnScreenSizeChange;
            OnScreenSizeChange(null, null);
            Bombs.Init(100);
            Anims.Init(375);
            Powers.Init(200);
        }

        protected override void LoadContent() {
            SB = new SpriteBatch(GraphicsDevice);
            Viewport = GraphicsDevice.Viewport;
            FMOD.Init(Content.RootDirectory);
            SpriteBatchExtensions.Init();
            Sprites = SpriteSheet.Load(Content.Load<Texture2D>("sprites"), "sprites.dat");
            PlayerSprites = SpriteSheet.Load(Content.Load<Texture2D>("playersprites"), "playersprites.dat");
            _playerSpritesTexture = new Color[PlayerSprites.Texture.Width * PlayerSprites.Texture.Height];
            PlayerSprites.Texture.GetData(_playerSpritesTexture);
            Scr.Open();
        }

        protected override void Update(GameTime gameTime) {
            T.Total = (float)(T.TotalFull = gameTime.TotalGameTime.TotalSeconds);
            T.Delta = (float)(T.DeltaFull = gameTime.ElapsedGameTime.TotalSeconds);
            InputHelper.UpdateSetup();
            if (_quit.Pressed())
                Exit();
            Scr.Update();
            if (NetServer.IsRunning)
                NetServer.PollEvents();
            else if (NetClient.IsRunning)
                NetClient.PollEvents();
            base.Update(gameTime);
            InputHelper.UpdateCleanup();
        }

        protected override void Draw(GameTime gameTime) {
            GraphicsDevice.Clear(Color.Black);
            Scr.Draw();
            base.Draw(gameTime);
        }

        protected override void OnExiting(object sender, EventArgs args) {
            if (NetServer.IsRunning)
                NetServer.Stop();
            else if (NetClient.IsRunning)
                NetClient.Stop();
            base.OnExiting(sender, args);
        }
    }
}