using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using BlackSync.Services;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Threading.Tasks;

namespace BlackSync.Views;

public partial class ZplMigracaoView : UserControl
{
    private string caminhoArquivoAccess = null!;
    private readonly MySQLService _mySQLService;

    public ZplMigracaoView()
    {
        InitializeComponent();

        var config = ConfigService.CarregarConfiguracaoMySQL();
        _mySQLService = new MySQLService(config.servidor, config.banco, config.usuario, config.senha);

        btnSelecionar.Click += BtnSelecionar_Click;
        btnConverter.Click += BtnConverter_Click;
    }

    private async void BtnSelecionar_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Selecione o arquivo zpl.mdb",
            Filters = new List<FileDialogFilter>
            {
                new() { Name = "Arquivo Access", Extensions = { "mdb" } },
                new() { Name = "Todos os arquivos", Extensions = { "*" } }
            }
        };

        var result = await dialog.ShowAsync(GetWindow());
        if (result is { Length: > 0 })
        {
            caminhoArquivoAccess = result[0];
            txtCaminhoArquivoAccess.Text = caminhoArquivoAccess;
        }
    }

    private async void BtnConverter_Click(object? sender, RoutedEventArgs e)
    {
        var janela = GetWindow();

        if (string.IsNullOrEmpty(caminhoArquivoAccess))
        {
            await MessageService.MostrarMensagem(janela, "Erro", "Selecione o arquivo Access primeiro.", Icon.Warning);
            return;
        }

        pbProgresso.Value = 0;
        pbProgresso.IsVisible = true;
        btnConverter.IsEnabled = false;

        try
        {
            LogService.RegistrarLog("INFO", "🔄 Iniciando importação do arquivo Access.");

            var (dtEtiqueta, dtModelos) = await Task.Run(() => LerTabelasAccess(caminhoArquivoAccess));

            LogService.RegistrarLog("INFO", $"📊 Etiqueta: {dtEtiqueta.Rows.Count} registros carregados.");
            LogService.RegistrarLog("INFO", $"📊 Modelos: {dtModelos.Rows.Count} registros carregados.");

            bool truncar = false, apenasNovos = false;

            if (await _mySQLService.TabelaTemDados(janela, "etiqueta_zpl"))
            {
                var box = await MessageService.ConfirmarCompleto(janela,
                    "Dados existentes detectados!",
                    $"A tabela etiqueta_zpl já contém dados no MySQL.{Environment.NewLine}" +
                    $"SIM - Apaga todos os dados antes da inserção{Environment.NewLine}" +
                    $"NÃO - Insere apenas novos registros{Environment.NewLine}" +
                    "CANCELAR - Cancela a importação");

                if (box == ButtonResult.Yes)
                {
                    truncar = true;
                    _mySQLService.TruncateTabela(janela, "etiqueta_zpl");
                    LogService.RegistrarLog("SUCCESS", "🚀 Tabela etiqueta_zpl truncada!");
                }
                else if (box == ButtonResult.No)
                {
                    apenasNovos = true;
                }
                else
                {
                    btnConverter.IsEnabled = true;
                    pbProgresso.IsVisible = false;
                    return;
                }
            }

            var dtFinal = FiltrarRegistros(dtEtiqueta, apenasNovos ? await _mySQLService.ObterCodigosExistentes(janela, "etiqueta_zpl", "contador") : null);

            int total = dtFinal.Rows.Count + dtModelos.Rows.Count;
            pbProgresso.Maximum = Math.Max(1, total);
            pbProgresso.Value = 0;

            if (dtFinal.Rows.Count > 0)
            {
                await Task.Run(() =>
                {
                    _mySQLService.InserirDadosTabela(janela, "etiqueta_zpl", dtFinal);
                    Dispatcher.UIThread.InvokeAsync(() => pbProgresso.Value += dtFinal.Rows.Count);
                });

                LogService.RegistrarLog("SUCCESS", $"✅ {dtFinal.Rows.Count} registros inseridos em etiqueta_zpl.");
            }

            if (dtModelos.Rows.Count > 0)
            {
                await Task.Run(() =>
                {
                    _mySQLService.InserirDadosTabela(janela, "modelos_zpl", dtModelos);
                    Dispatcher.UIThread.InvokeAsync(() => pbProgresso.Value += dtModelos.Rows.Count);
                });

                LogService.RegistrarLog("SUCCESS", $"✅ {dtModelos.Rows.Count} registros inseridos em modelos_zpl.");
            }

            await MessageService.MostrarMensagem(janela, "Sucesso", "Importação concluída com sucesso!", Icon.Info);
        }
        catch (Exception ex)
        {
            LogService.RegistrarLog("ERROR", $"❌ Erro na importação: {ex.Message}");
            await MessageService.MostrarMensagem(janela, "Erro", $"Erro ao importar dados: {ex.Message}", Icon.Error);
        }
        finally
        {
            btnConverter.IsEnabled = true;
            pbProgresso.IsVisible = false;
            pbProgresso.Value = 0;
        }
    }

    private (DataTable dtEtiqueta, DataTable dtModelos) LerTabelasAccess(string caminhoArquivo)
    {
        string connStr = $"Provider=Microsoft.Jet.OLEDB.4.0;Data Source={caminhoArquivo};";
        var dtEtiqueta = new DataTable();
        var dtModelos = new DataTable();

        using var conn = new OleDbConnection(connStr);
        conn.Open();

        using var ad1 = new OleDbDataAdapter("SELECT * FROM Etiqueta", conn);
        ad1.Fill(dtEtiqueta);

        using var ad2 = new OleDbDataAdapter("SELECT * FROM Modelos", conn);
        ad2.Fill(dtModelos);

        return (dtEtiqueta, dtModelos);
    }

    private DataTable FiltrarRegistros(DataTable tabela, HashSet<string>? existentes)
    {
        if (existentes == null)
            return tabela;

        var filtrada = tabela.Clone();
        foreach (DataRow row in tabela.Rows)
        {
            string contador = row["Contador"].ToString();
            if (!existentes.Contains(contador))
                filtrada.ImportRow(row);
        }

        return filtrada;
    }

    private Window GetWindow() => (Window)this.VisualRoot;

    private async void BtnLimpar_Click(object? sender, RoutedEventArgs e)
    {
        txtCaminhoArquivoAccess.Text = string.Empty;
    }

    private void AdicionarLog(string mensagem)
    {
        txtLog.Text += $"{mensagem}{Environment.NewLine}";
        txtLog.CaretIndex = txtLog.Text.Length;
    }
}
