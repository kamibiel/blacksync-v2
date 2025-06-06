using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Linq;
using BlackSync.Views;
using BlackSync.Services;
using System.Threading.Tasks;
using System.IO;

namespace BlackSync.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this.Icon = new WindowIcon("Assets/icone.ico");
        }

        private void MenuItem_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is MenuItemViewModel item)
            {
                if (item.SubItems.Any())
                {
                    // Só expande se tiver submenus
                    item.IsExpanded = !item.IsExpanded;
                }
                else
                {
                    switch (item.Title)
                    {
                        case "Migração":
                            MainContent.Content = new MigracaoView();
                            LogService.RegistrarLog("INFO", "Acesso à tela de Configurações.");
                            break;

                        case "Configuração":
                            MainContent.Content = new ConfiguracaoView();
                            LogService.RegistrarLog("INFO", "Acesso à tela de Configurações.");
                            break;

                        case "Logs":
                            MainContent.Content = new LogsView();
                            LogService.RegistrarLog("INFO", "Acesso à tela de Logs.");
                            break;

                        case "Sobre":
                            MainContent.Content = new SobreView();
                            LogService.RegistrarLog("INFO", "Acesso à tela de Sobre.");
                            break;

                        default:
                            MainContent.Content = new TextBlock
                            {
                                Text = $"Você selecionou: {item.Title}",
                                FontSize = 24,
                                Margin = new Thickness(10)
                            };
                            break;
                    }
                }
            }
        }

        private void SubItem_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                string subItemName = btn.Content?.ToString()?.Trim() ?? "";

                switch (subItemName)
                {
                    case "Dados":
                        MainContent.Content = new MigracaoView();
                        LogService.RegistrarLog("INFO", "Acesso à tela de Migrar tabelas.");
                        break;

                    case "Migração ZPL":
                        MainContent.Content = new ZplMigracaoView();
                        LogService.RegistrarLog("INFO", "Acesso à tela de Migração ZPL.");
                        break;

                    case "Outro Submenu":
                        MainContent.Content = new TextBlock
                        {
                            Text = "Tela para outro submenu...",
                            FontSize = 18,
                            Margin = new Thickness(10)
                        };
                        LogService.RegistrarLog("INFO", "Acesso à tela de Outro Submenu.");
                        break;

                    default:
                        MainContent.Content = new TextBlock
                        {
                            Text = $"Você selecionou: {subItemName}",
                            FontSize = 18,
                            Margin = new Thickness(10)
                        };
                        LogService.RegistrarLog("INFO", $"Subitem não tratado: {subItemName}");
                        break;
                }
            }
        }

        private void BtnSair_Click(object? sender, RoutedEventArgs e)
        {
            Environment.Exit(0); // Fecha a aplicação
        }
    }
}