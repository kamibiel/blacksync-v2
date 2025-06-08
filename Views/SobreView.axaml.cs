using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using BlackSync.Views;
using System.Diagnostics;
using System.Reflection;

namespace BlackSync.Views;

public partial class SobreView : UserControl
{
    public string VersaoSistema { get; }

    public SobreView()
    {
        VersaoSistema = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
        DataContext = this;
        InitializeComponent();
    }

    private void AbrirLinkedIn_Click(object? sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://www.linkedin.com/in/gabriel-bonif%C3%A1cio-oliveira-403298138/",
            UseShellExecute = true
        });
    }

    private void AbrirGitHub_Click(object? sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/kamibiel",
            UseShellExecute = true
        });
    }
}