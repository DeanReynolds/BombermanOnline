using System;

namespace BombermanOnline {
    static class Program {
        [STAThread]
        static void Main() {
            using var game = new G();
            game.Run();
        }
    }
}