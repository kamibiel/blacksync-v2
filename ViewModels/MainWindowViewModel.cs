using System.Collections.ObjectModel;

namespace BlackSync.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        public ObservableCollection<MenuItemViewModel> MenuItems { get; } = new();

        public MainWindowViewModel()
        {
            MenuItems.Add(new MenuItemViewModel
            {
                Title = "Migração",
            });

            MenuItems.Add(new MenuItemViewModel { Title = "Manutenção" });

            MenuItems.Add(new MenuItemViewModel { 
                Title = "Configuração",
                SubItems = new ObservableCollection<string>
                {
                    "Conexão",
                    "Empresa"
                }
            });

            MenuItems.Add(new MenuItemViewModel { Title = "Logs" });
            MenuItems.Add(new MenuItemViewModel { Title = "Sobre" });
        }
    }
}
