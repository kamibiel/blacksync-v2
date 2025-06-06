using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BlackSync.Views;
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
}