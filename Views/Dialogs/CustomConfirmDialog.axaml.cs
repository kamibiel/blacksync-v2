using Avalonia.Controls;
using System.Threading.Tasks;
using BlackSync.Enums;

namespace BlackSync.Views.Dialogs
{
    public partial class CustomConfirmDialog : Window
    {
        public OpcaoConfirmacao Resultado { get; private set; } = OpcaoConfirmacao.Cancelar;

        public CustomConfirmDialog(string titulo, string mensagem)
        {
            InitializeComponent();
            this.Title = titulo;
            this.FindControl<TextBlock>("MensagemTextBlock").Text = mensagem;

            this.FindControl<Button>("SimButton").Click += (_, _) =>
            {
                Resultado = OpcaoConfirmacao.Sim;
                Close();
            };

            this.FindControl<Button>("NaoButton").Click += (_, _) =>
            {
                Resultado = OpcaoConfirmacao.Nao;
                Close();
            };

            this.FindControl<Button>("CancelarButton").Click += (_, _) =>
            {
                Resultado = OpcaoConfirmacao.Cancelar;
                Close();
            };
        }
    }
}
