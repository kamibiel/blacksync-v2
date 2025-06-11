using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using BlackSync.Services;
using System;
using System.Text;

namespace BlackSync.Views;

public partial class LogsView : UserControl
{
    public LogsView()
    {
        InitializeComponent();
        // Protege de chamadas em tempo de design que causam crash
        if (Design.IsDesignMode)
            return;

        try
        {
            CarregarLogsDoDia();
        }
        catch (Exception ex)
        {
            // Opcional: registrar ou ignorar exceções de preview
            Console.WriteLine($"Erro ao carregar logs: {ex.Message}");
        }
    }

    private void CarregarLogsDoDia()
    {
        txtLogs.Text = "";

        var logs = LogService.ObterLogsDoDia();

        if (logs.Count == 0)
        {
            txtLogs.Text = "Nenhum log registrado hoje.";
            return;
        }

        var sb = new StringBuilder();
        foreach (var log in logs)
        {
            sb.AppendLine($"[{log.DataHora}] {log.Tipo}: {log.Mensagem}");
        }

        txtLogs.Text = sb.ToString();

        txtLogs.CaretIndex = txtLogs.Text?.Length ?? 0;
        scrollViewer.Offset = new Avalonia.Vector(scrollViewer.Offset.X, scrollViewer.Extent.Height);
    }

    private void BtnAtualizar_Click(object? sender, RoutedEventArgs e)
    {
        CarregarLogsDoDia();
    }
}
