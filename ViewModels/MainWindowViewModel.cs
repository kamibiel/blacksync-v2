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
            //    SubItems = new ObservableCollection<string>
            //{
            //    "Dados",
            //    "Estrutura",
            //    "Migração ZPL"
            //}
            });

            MenuItems.Add(new MenuItemViewModel { Title = "Manutenção" });
            MenuItems.Add(new MenuItemViewModel { Title = "Configuração" });
            MenuItems.Add(new MenuItemViewModel { Title = "Logs" });
            MenuItems.Add(new MenuItemViewModel { Title = "Sobre" });
        }
    }
}
