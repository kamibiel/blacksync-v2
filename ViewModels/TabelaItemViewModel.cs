using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BlackSync.ViewModels
{
    public class TabelaItemViewModel : INotifyPropertyChanged
    {
        private bool _selecionado;

        public string Nome { get; set; } = string.Empty;

        public bool Selecionado
        {
            get => _selecionado;
            set
            {
                if (_selecionado != value)
                {
                    _selecionado = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
