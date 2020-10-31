using Apos.Input;
using Microsoft.Xna.Framework.Input;

namespace BombermanOnline {
    sealed class MainScr : Scr {
        public override void Open() {}
        public override void Close() {}

        public override void Update() {
            if (KeyboardCondition.Pressed(Keys.D1)) {
                NetServer.Host(8);
                Players.InsertLocal(0);
                G.SetScr<GameScr>();
            } else if (KeyboardCondition.Pressed(Keys.D2)) {
                NetClient.Join("127.0.0.1");
            }
        }
        public override void Draw() {}
    }
}