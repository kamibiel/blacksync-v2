using Avalonia.Controls;
using Avalonia.VisualTree;
using Avalonia.Controls.ApplicationLifetimes;
using BlackSync.Views.Dialogs;
using Avalonia;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using System.Threading.Tasks;
using BlackSync.Enums;

namespace BlackSync.Services
{
    public static class MessageService
    {
        // -------------------------
        // MÉTODOS BASEADOS EM MessageBox.Avalonia
        // -------------------------
        public static async Task MostrarMensagem(Window janela, string titulo, string mensagem, Icon icone = Icon.Info)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
            {
                ContentTitle = titulo,
                ContentMessage = mensagem,
                ButtonDefinitions = ButtonEnum.Ok,
                Icon = icone,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            });

            await box.ShowAsync();
        }

        public static async Task<bool> Confirmar(Window janela, string titulo, string mensagem)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
            {
                ContentTitle = titulo,
                ContentMessage = mensagem,
                ButtonDefinitions = ButtonEnum.YesNo,
                Icon = Icon.Question,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            });

            var resultado = await box.ShowAsync();
            return resultado == ButtonResult.Yes;
        }

        public static async Task<ButtonResult> ConfirmarCompleto(Window janela, string titulo, string mensagem)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
            {
                ContentTitle = titulo,
                ContentMessage = mensagem,
                ButtonDefinitions = ButtonEnum.YesNoCancel,
                Icon = Icon.Question,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            });

            return await box.ShowAsync();
        }

        // -------------------------
        // NOVOS MÉTODOS PERSONALIZADOS (Custom Dialogs)
        // -------------------------

        // Para controle (View/UserControl)
        public static async Task MostrarDialogPersonalizado(Control control, string titulo, string mensagem)
        {
            var window = TopLevel.GetTopLevel(control) as Window;
            if (window == null) return;

            var dialog = new CustomMessageBox(titulo, mensagem);
            await dialog.ShowDialog(window);
        }

        public static async Task<OpcaoConfirmacao> MostrarDialogConfirmacaoPersonalizado(Control control, string titulo, string mensagem)
        {
            var window = TopLevel.GetTopLevel(control) as Window;
            if (window == null) return OpcaoConfirmacao.Cancelar;

            var dialog = new CustomConfirmDialog(titulo, mensagem);
            await dialog.ShowDialog(window);
            return dialog.Resultado;
        }

        // Sobrecarga para chamadas onde você não tem um Control
        public static async Task MostrarDialogPersonalizado(string titulo, string mensagem)
        {
            var window = ObterMainWindow();
            if (window == null) return;

            var dialog = new CustomMessageBox(titulo, mensagem);
            await dialog.ShowDialog(window);
        }

        public static async Task<OpcaoConfirmacao> MostrarDialogConfirmacaoPersonalizado(string titulo, string mensagem)
        {
            var window = ObterMainWindow();
            if (window == null) return OpcaoConfirmacao.Cancelar;

            var dialog = new CustomConfirmDialog(titulo, mensagem);
            await dialog.ShowDialog(window);
            return dialog.Resultado;
        }

        public static async Task<string> PerguntarNomeCliente(Window window)
        {
            var inputDialog = new CustomInputDialog("Nome do cliente", "Informe o nome do cliente:");
            await inputDialog.ShowDialog(window);
            return inputDialog.Resultado?.Trim() ?? string.Empty;
        }

        // -------------------------
        // MÉTODO DE SUPORTE
        // -------------------------

        private static Window? ObterMainWindow()
        {
            return (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        }
    }
}
