using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace BlackSync.Views.Dialogs
{
    public partial class CustomMessageBox : Window
    {
        public string Titulo { get; set; }
        public string Mensagem { get; set; }

        public CustomMessageBox(string titulo, string mensagem)
        {
            InitializeComponent();

            this.Title = titulo;
            this.Titulo = titulo;
            this.Mensagem = mensagem;

            DataContext = this;
        }

        private void Ok_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
