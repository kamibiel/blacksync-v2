using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;

namespace BlackSync.Views
{
    public partial class HomeView : UserControl
    {
        public HomeView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void BtnLogs_Click(object? sender, RoutedEventArgs e)
        {
            // 1. Encontra a janela principal que está "segurando" esta HomeView
            var mainWindow = (MainWindow)this.VisualRoot;

            // 2. Se a janela existir, trocamos o conteúdo dela para a LogsView
            if (mainWindow != null)
            {
                // Substitui a HomeView pela LogsView
                mainWindow.MainContent.Content = new LogsView();
            }
        }

        private void BtnMigracao_Click(object? sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)this.VisualRoot;
            if (mainWindow != null)
            {
                // Certifique-se de que MigracaoView é o nome correto do seu UserControl
                mainWindow.MainContent.Content = new MigracaoView();
            }
        }

        // Método do botão Manutenção
        private void BtnManutencao_Click(object? sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)this.VisualRoot;
            if (mainWindow != null)
            {
                mainWindow.MainContent.Content = new ManutencaoView();
            }
        }

        // Método do botão Configurações
        private void BtnConfiguracao_Click(object? sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)this.VisualRoot;
            if (mainWindow != null)
            {
                // Como Configuração tem um submenu (Conexão e Empresa), 
                // redirecionamos para a tela padrão (ex: ConexaoView)
                //mainWindow.MainContent.Content = new ConfiguracaoView();
                mainWindow.NavegarParaConfiguracao();
            }
        }
    }
}