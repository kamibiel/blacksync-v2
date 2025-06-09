using System.Globalization;
using Avalonia;
using System;

namespace BlackSync
{
    internal class Program
    {
        // Ponto de entrada
        [STAThread]
        public static void Main(string[] args)
        {
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("pt-BR");
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("pt-BR");

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }

        // Configurações da aplicação Avalonia
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                         .UsePlatformDetect()
                         .LogToTrace();
    }
}
