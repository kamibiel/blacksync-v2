using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using BlackSync.Services;
using BlackSync.ViewModels;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.Threading.Tasks;

namespace BlackSync.Views
{
    public partial class ManutencaoView : UserControl
    {
        private string caminhoArquivoAccess = null!;
        private MySQLService _mysqlService;
        private FirebirdService _firebirdService;

        public ManutencaoView()
        {
            InitializeComponent();
            this.AttachedToVisualTree += OnViewLoaded;
        }

        private async void OnViewLoaded(object? sender, VisualTreeAttachmentEventArgs e)
        {
            var window = this.GetVisualRoot() as Window;

            try
            {
                var (servidor, banco, usuario, senha) = ConfigService.CarregarConfiguracaoMySQL();
                var dsn = ConfigService.CarregarConfiguracaoFirebird();

                if (string.IsNullOrWhiteSpace(servidor) ||
                    string.IsNullOrWhiteSpace(banco) ||
                    string.IsNullOrWhiteSpace(usuario) ||
                    string.IsNullOrWhiteSpace(senha) ||
                    string.IsNullOrWhiteSpace(dsn))
                {
                    await MessageService.MostrarDialogPersonalizado(window,
                        "Configuração ausente",
                        $"⚠️ Nenhuma configuração de banco encontrada.{Environment.NewLine}Por favor, configure antes de acessar a tela de Manutenção.");

                    
                    btnReabrirDados.IsEnabled = false;
                    btnFecharDados.IsEnabled = false;
                    btnExcluirDados.IsEnabled = false;
                    btnAtualizarFilial.IsEnabled = false;
                    btnAlterarNumeracao.IsEnabled = false;

                    return;
                }

                _mysqlService = new MySQLService(servidor, banco, usuario, senha);
                _firebirdService = new FirebirdService(dsn);

                this.FindControl<Button>("btnReabrirDados").Click += BtnReabrirDados_Click;
                this.FindControl<Button>("btnFecharDados").Click += BtnFecharDados_Click;
                this.FindControl<Button>("btnExcluirDados").Click += BtnExcluirDados_Click;
                this.FindControl<Button>("btnAtualizarFilial").Click += BtnAtualizarFilial_Click;
                this.FindControl<Button>("btnAlterarNumeracao").Click += BtnAlterarNumeracao_Click;
            }
            catch (Exception ex)
            {
                await MessageService.MostrarDialogPersonalizado(window,
                    "Erro ao iniciar",
                    $"❌ Falha ao carregar a tela de migração:{Environment.NewLine}{ex.Message}");
            }
        }

        private void BtnReabrirDados_Click(object? sender, RoutedEventArgs e) { }
        private void BtnFecharDados_Click(object? sender, RoutedEventArgs e) { }
        private void BtnExcluirDados_Click(object? sender, RoutedEventArgs e) { }
        private void BtnAtualizarFilial_Click(object? sender, RoutedEventArgs e) { }
        private void BtnAlterarNumeracao_Click(object? sender, RoutedEventArgs e) { }

        // ZPL
        //private async void BtnSelecionar_Click(object? sender, RoutedEventArgs e)
        //{
        //    var dialog = new OpenFileDialog
        //    {
        //        Title = "Selecione o arquivo zpl.mdb",
        //        Filters = new List<FileDialogFilter>
        //    {
        //        new() { Name = "Arquivo Access", Extensions = { "mdb" } },
        //        new() { Name = "Todos os arquivos", Extensions = { "*" } }
        //    }
        //    };

        //    var result = await dialog.ShowAsync(GetWindow());
        //    if (result is { Length: > 0 })
        //    {
        //        caminhoArquivoAccess = result[0];
        //        txtCaminhoArquivoAccess.Text = caminhoArquivoAccess;
        //    }
        //}

        //private (DataTable dtEtiqueta, DataTable dtModelos) LerTabelasAccess(string caminhoArquivo)
        //{
        //    string connStr = $"Provider=Microsoft.Jet.OLEDB.4.0;Data Source={caminhoArquivo};";
        //    var dtEtiqueta = new DataTable();
        //    var dtModelos = new DataTable();

        //    using var conn = new OleDbConnection(connStr);
        //    conn.Open();

        //    using var ad1 = new OleDbDataAdapter("SELECT * FROM Etiqueta", conn);
        //    ad1.Fill(dtEtiqueta);

        //    using var ad2 = new OleDbDataAdapter("SELECT * FROM Modelos", conn);
        //    ad2.Fill(dtModelos);

        //    return (dtEtiqueta, dtModelos);
        //}

        //private DataTable FiltrarRegistros(DataTable tabela, HashSet<string>? existentes)
        //{
        //    if (existentes == null)
        //        return tabela;

        //    var filtrada = tabela.Clone();
        //    foreach (DataRow row in tabela.Rows)
        //    {
        //        string contador = row["Contador"].ToString();
        //        if (!existentes.Contains(contador))
        //            filtrada.ImportRow(row);
        //    }

        //    return filtrada;
        //}

        //private async void btnImportarZPL_Click(object? sender, RoutedEventArgs e)
        //{
        //    var janela = GetWindow();

        //    if (string.IsNullOrEmpty(caminhoArquivoAccess))
        //    {
        //        await MessageService.MostrarMensagem(janela, "Erro", "Selecione o arquivo Access primeiro.", Icon.Warning);
        //        return;
        //    }

        //    pbProgresso.Value = 0;
        //    pbProgresso.IsVisible = true;
        //    btnReabrirDados.IsEnabled = false;
        //    btnFecharDados.IsEnabled = false;
        //    btnExcluirDados.IsEnabled = false;
        //    btnAtualizarFilial.IsEnabled = false;
        //    btnAlterarNumeracao.IsEnabled = false;
            

        //    try
        //    {
        //        LogService.RegistrarLog("INFO", "🔄 Iniciando importação do arquivo Access.");

        //        var (dtEtiqueta, dtModelos) = await Task.Run(() => LerTabelasAccess(caminhoArquivoAccess));

        //        LogService.RegistrarLog("INFO", $"📊 Etiqueta: {dtEtiqueta.Rows.Count} registros carregados.");
        //        LogService.RegistrarLog("INFO", $"📊 Modelos: {dtModelos.Rows.Count} registros carregados.");

        //        bool truncar = false, apenasNovos = false;

        //        if (await _mysqlService.TabelaTemDados(janela, "etiqueta_zpl"))
        //        {
        //            var box = await MessageService.ConfirmarCompleto(janela,
        //                "Dados existentes detectados!",
        //                $"A tabela etiqueta_zpl já contém dados no MySQL.{Environment.NewLine}" +
        //                $"SIM - Apaga todos os dados antes da inserção{Environment.NewLine}" +
        //                $"NÃO - Insere apenas novos registros{Environment.NewLine}" +
        //                "CANCELAR - Cancela a importação");

        //            if (box == ButtonResult.Yes)
        //            {
        //                truncar = true;
        //                _mysqlService.TruncateTabela(janela, "etiqueta_zpl");
        //                LogService.RegistrarLog("SUCCESS", "🚀 Tabela etiqueta_zpl truncada!");
        //            }
        //            else if (box == ButtonResult.No)
        //            {
        //                apenasNovos = true;
        //            }
        //            else
        //            {
                        
        //                pbProgresso.IsVisible = false;
        //                return;
        //            }
        //        }

        //        var dtFinal = FiltrarRegistros(dtEtiqueta, apenasNovos ? await _mysqlService.ObterCodigosExistentes(janela, "etiqueta_zpl", "contador") : null);

        //        int total = dtFinal.Rows.Count + dtModelos.Rows.Count;
        //        pbProgresso.Maximum = Math.Max(1, total);
        //        pbProgresso.Value = 0;

        //        if (dtFinal.Rows.Count > 0)
        //        {
        //            await Task.Run(() =>
        //            {
        //                _mysqlService.InserirDadosTabela(janela, "etiqueta_zpl", dtFinal);
        //                Dispatcher.UIThread.InvokeAsync(() => pbProgresso.Value += dtFinal.Rows.Count);
        //            });

        //            LogService.RegistrarLog("SUCCESS", $"✅ {dtFinal.Rows.Count} registros inseridos em etiqueta_zpl.");
        //        }

        //        if (dtModelos.Rows.Count > 0)
        //        {
        //            await Task.Run(() =>
        //            {
        //                _mysqlService.InserirDadosTabela(janela, "modelos_zpl", dtModelos);
        //                Dispatcher.UIThread.InvokeAsync(() => pbProgresso.Value += dtModelos.Rows.Count);
        //            });

        //            LogService.RegistrarLog("SUCCESS", $"✅ {dtModelos.Rows.Count} registros inseridos em modelos_zpl.");
        //        }

        //        await MessageService.MostrarMensagem(janela, "Sucesso", "Importação concluída com sucesso!", Icon.Info);
        //    }
        //    catch (Exception ex)
        //    {
        //        LogService.RegistrarLog("ERROR", $"❌ Erro na importação: {ex.Message}");
        //        await MessageService.MostrarMensagem(janela, "Erro", $"Erro ao importar dados: {ex.Message}", Icon.Warning);                
        //    }
        //    finally
        //    {
                
                
        //        btnReabrirDados.IsEnabled = true;
        //        btnFecharDados.IsEnabled = true;
        //        btnExcluirDados.IsEnabled = true;
        //        btnAtualizarFilial.IsEnabled = true;
        //        btnAlterarNumeracao.IsEnabled = true;
        //        btnSelecionar.IsEnabled = true;
        //        btnLimpar.IsEnabled = true;
        //        pbProgresso.IsVisible = false;
        //        pbProgresso.Value = 0;
        //    }
        //}

        private Window GetWindow() => (Window)this.VisualRoot;
    }
}