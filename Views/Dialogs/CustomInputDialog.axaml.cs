using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace BlackSync.Views.Dialogs
{
    public partial class CustomInputDialog : Window
    {
        public string Titulo { get; set; }
        public string Mensagem { get; set; }
        public string? Resultado { get; private set; }

        public CustomInputDialog(string titulo, string mensagem)
        {
            InitializeComponent();

            Titulo = titulo;
            Mensagem = mensagem;

            DataContext = this;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void Ok_Click(object? sender, RoutedEventArgs e)
        {
            var inputBox = this.FindControl<TextBox>("InputBox");
            Resultado = inputBox.Text?.Trim();
            Close();
        }
    }
}
