using Avalonia;
using System;

namespace BlackSync
{
    internal class Program
    {
        // Ponto de entrada
        [STAThread]
        public static void Main(string[] args) =>
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);

        // Configurações da aplicação Avalonia
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                         .UsePlatformDetect()
                         .LogToTrace();
    }
}
