﻿using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using BlackSync.Enums;
using BlackSync.Services;
using BlackSync.ViewModels;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace BlackSync.Views;

public partial class MigracaoView : UserControl
{
    private readonly MySQLService _mysqlService;
    private readonly FirebirdService _firebirdService;
    private string _ultimaLetraDigitada = "";
    private DateTime _ultimaTeclaPressionada = DateTime.MinValue;
    private List<string> _tabelasMySQL = new List<string>();
    private List<string> _tabelasParaCriar = new List<string>();
    private List<string> _tabelasComErro = new List<string>();

    public MigracaoView()
    {
        InitializeComponent();

        var lb = this.FindControl<ListBox>("lbTabelasFirebird");
        lb.AddHandler(KeyDownEvent, ListBox_KeyDown, RoutingStrategies.Tunnel);

        var window = this.GetVisualRoot() as Window;

        try
        {
            var (servidor, banco, usuario, senha) = ConfigService.CarregarConfiguracaoMySQL();
            var dsn = ConfigService.CarregarConfiguracaoFirebird();

            // Verifica se algo está vazio
            if (string.IsNullOrWhiteSpace(servidor) ||
                string.IsNullOrWhiteSpace(banco) ||
                string.IsNullOrWhiteSpace(usuario) ||
                string.IsNullOrWhiteSpace(senha) ||
                string.IsNullOrWhiteSpace(dsn))
            {
                Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await MessageService.MostrarDialogPersonalizado(window,
                        "Configuração ausente",
                        $"⚠️ Nenhuma configuração de banco encontrada.{Environment.NewLine}Por favor, configure antes de acessar a tela de Migração."
                    );
                });
                return;
            }

            _mysqlService = new MySQLService(servidor, banco, usuario, senha);
            _firebirdService = new FirebirdService(dsn);

            this.FindControl<Button>("btnMigrar").Click += BtnMigrar_Click;
            this.FindControl<Button>("btnVerificarTabelas").Click += BtnVerificarTabelas_Click;
            this.FindControl<Button>("btnVerificarEstrutura").Click += BtnVerificarEstrutura_Click;
            this.FindControl<Button>("btnGerarScripts").Click += BtnGerarScripts_Click;
            this.FindControl<Button>("btnGerarFeedback").Click += BtnGerarFeedback_Click;
            this.FindControl<CheckBox>("cbMarcarTodas").Checked += CbMarcarTodas_Changed;

            // Chama o método assíncrono sem bloquear a UI
            _ = CarregarTabelasAsync();
        }
        catch (Exception ex)
        {
            _ = MessageService.MostrarMensagem(
                window!,
                "Erro ao iniciar",
                $"❌ Falha ao carregar a tela de migração:{Environment.NewLine}{ex.Message}",
                MsBox.Avalonia.Enums.Icon.Error
            );
        }
    }

    private void ListBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (_tabelas.Count == 0 || string.IsNullOrWhiteSpace(e.Key.ToString()))
            return;

        // Tempo máximo para considerar múltiplas teclas como sequência (ex: "t", "a" → "ta")
        var agora = DateTime.Now;
        if ((agora - _ultimaTeclaPressionada).TotalMilliseconds > 800)
            _ultimaLetraDigitada = "";

        _ultimaLetraDigitada += e.Key.ToString().ToLowerInvariant();
        _ultimaTeclaPressionada = agora;

        var lb = this.FindControl<ListBox>("lbTabelasFirebird");

        var item = _tabelas.FirstOrDefault(t => t.Nome.ToLowerInvariant().StartsWith(_ultimaLetraDigitada));
        if (item != null)
        {
            lb.SelectedItem = item;
            lb.ScrollIntoView(item);
        }
    }

    private async Task CarregarTabelasAsync()
    {
        if (_firebirdService == null)
            return;

        var window = this.GetVisualRoot() as Window;
        var nomes = await _firebirdService.GetTabelasFirebird(window);

        _tabelas = new ObservableCollection<TabelaItemViewModel>(
            nomes.Select(nome => new TabelaItemViewModel
            {
                Nome = nome,
                Selecionado = false
            })
        );

        var lb = this.FindControl<ListBox>("lbTabelasFirebird");
        lb.ItemsSource = _tabelas;

        foreach (var item in _tabelas)
        {
            item.PropertyChanged += Item_PropertyChanged;
        }

        AtualizarTextoCheckbox();
    }

    private void CarregarTabelasMySQL()
    {
        try
        {
            // Armazena as tabelas do MySQL
            _tabelasMySQL.Clear();

            (string servidor, string banco, string usuario, string senha) = ConfigService.CarregarConfiguracaoMySQL();
            string connectionString = $"Server={servidor};Database={banco};User Id={usuario};Password={senha};";

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = "SHOW TABLES";

                using (var cmd = new MySqlCommand(query, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        _tabelasMySQL.Add(reader.GetString(0).Trim().ToUpper());
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogService.RegistrarLog("ERRO", $"❌ Erro ao carregar tabelas do MySQL: {ex.Message}");
            txtLog.Text += $"❌ Erro ao carregar tabelas do MySQL: {ex.Message}";
        }
    }

    private void CompararTabelas()
    {
        List<string> tabelasFirebird = lbTabelasFirebird.Items
            .Cast<TabelaItemViewModel>()
            .Select(t => t.Nome.ToUpper())
            .ToList();

        List<string> apenasNoFirebird = tabelasFirebird.Except(_tabelasMySQL).ToList();

        txtLog.Clear();
        _tabelasComErro.Clear();
        _tabelasParaCriar.Clear();

        LogService.RegistrarLog("INFO", $"🔄 Iniciando a verificação das tabelas.");

        if (apenasNoFirebird.Count > 0)
        {
            _tabelasParaCriar.AddRange(apenasNoFirebird);
            txtLog.Text += $"⚠️ Tabelas que estão no Firebird e não no MySQL:{Environment.NewLine}";
            LogService.RegistrarLog("INFO", $"⚠️ Tabelas que estão no Firebird e não no MySQL:");

            foreach (var tabela in apenasNoFirebird)
            {
                txtLog.Text += $"- {tabela}{Environment.NewLine}";
            }
        }
        else
        {
            txtLog.Text += $"✅ Todas as tabelas já existem no MySQL.{Environment.NewLine}";
        }

        LogService.RegistrarLog("INFO", $"🎉 Comparação concluída.");
    }

    private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TabelaItemViewModel.Selecionado))
            AtualizarTextoCheckbox();
    }

    private ObservableCollection<TabelaItemViewModel> _tabelas = new();

    private bool _atualizandoCheckInternamente = false;

    private void AtualizarTextoCheckbox()
    {
        _atualizandoCheckInternamente = true;

        var cb = this.FindControl<CheckBox>("cbMarcarTodas");
        bool todosSelecionados = _tabelas.All(t => t.Selecionado);

        cb.IsChecked = todosSelecionados;
        cb.Content = todosSelecionados ? "Desmarcar todas as tabelas" : "Selecionar todas as tabelas";

        _atualizandoCheckInternamente = false;
    }

    private void CbMarcarTodas_Changed(object? sender, RoutedEventArgs e)
    {
        if (_atualizandoCheckInternamente) return;

        var cb = this.FindControl<CheckBox>("cbMarcarTodas");

        bool selecionar = cb.IsChecked == true;

        foreach (var item in _tabelas)
            item.Selecionado = selecionar;

        AtualizarTextoCheckbox();
    }

    private List<string> ObterTabelasSelecionadas()
    {
        return _tabelas
            .Where(item => item.Selecionado)
            .Select(item => item.Nome)
            .ToList();
    }

    private async void BtnMigrar_Click(object? sender, RoutedEventArgs e)
    {
        var txtLog = this.FindControl<TextBox>("txtLog");
        var pbMigracao = this.FindControl<ProgressBar>("pbMigracao");
        var btnMigrar = this.FindControl<Button>("btnMigrar");
        var btnVerificarTabelas = this.FindControl<Button>("btnVerificarTabelas");
        var btnVerificarEstrutura = this.FindControl<Button>("btnVerificarEstrutura");
        var btnGerarScripts = this.FindControl<Button>("btnGerarScripts");
        var btnGerarFeedback = this.FindControl<Button>("btnGerarFeedback");
        var window = this.GetVisualRoot() as Window;

        LogService.RegistrarLog("INFO", "🔄 Limpando o Log Verificação");
        txtLog.Text = string.Empty;
        pbMigracao.Value = 0;

        var lb = this.FindControl<ListBox>("lbTabelasFirebird");
        var tabelasSelecionadas = ObterTabelasSelecionadas();

        if (tabelasSelecionadas.Count == 0)
        {
            await MessageService.MostrarDialogPersonalizado(window,
                "Aviso",
                "Por favor, selecione ao menos uma tabela para migrar."
            );
            return;
        }

        LogService.RegistrarLog("INFO", $"🔄 Iniciando migração de {tabelasSelecionadas.Count} tabelas.");
        txtLog.Text += $"🔄 Iniciando migração de {tabelasSelecionadas.Count} tabelas...{Environment.NewLine}";

        pbMigracao.IsVisible = true;
        btnMigrar.IsEnabled = false;
        btnVerificarTabelas.IsEnabled = false;
        btnVerificarEstrutura.IsEnabled = false;
        btnGerarScripts.IsEnabled = false;
        btnGerarFeedback.IsEnabled = false;

        int totalRegistros = await _firebirdService.ObterTotalRegistros(tabelasSelecionadas);
        pbMigracao.Maximum = Math.Max(1, totalRegistros);

        await Task.Run(async () =>
        {
            foreach (var tabela in tabelasSelecionadas)
            {
                try
                {
                    LogService.RegistrarLog("INFO", $"📥 Migrando tabela: {tabela}");
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        txtLog.Text += $"📥 Migrando tabela: {tabela}...{Environment.NewLine}";
                    });

                    bool truncarTabela = false;
                    bool apenasNovosRegistros = false;

                    if (await _mysqlService.TabelaTemDados(window, tabela))
                    {
                        var resposta = await Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            return await MessageService.MostrarDialogConfirmacaoPersonalizado(this,
                                "⚠️ Dados existentes detectados!",
                                $"A tabela {tabela} já contém dados no MySQL.{Environment.NewLine}{Environment.NewLine}" +
                                $"✔ [SIM] - Apaga todos os dados do MySQL antes da inserção{Environment.NewLine}" +
                                $"✔ [NÃO] - Insere apenas registros novos{Environment.NewLine}" +
                                $"✔ [CANCELAR] - Ignora essa tabela e segue para a próxima");
                        });

                        if (resposta == OpcaoConfirmacao.Sim)
                        {

                            truncarTabela = true;
                            await Task.Run(() => _mysqlService.TruncateTabela(window, tabela));
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                LogService.RegistrarLog("SUCCESS", $"🚀 Tabela {tabela} truncada!");
                                txtLog.Text += $"🚀 Tabela {tabela} truncada!{Environment.NewLine}";
                            });
                        }
                        else if (resposta == OpcaoConfirmacao.Nao)
                        {
                            apenasNovosRegistros = true;
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                LogService.RegistrarLog("SUCCESS", $"✅ Novos registros inseridos com sucesso na tabela {tabela}.{Environment.NewLine}");
                                txtLog.Text += $"✅ Novos registros inseridos com sucesso na tabela {tabela}.{Environment.NewLine}";
                            });
                        }
                        else
                        {
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                LogService.RegistrarLog("INFO", $"⚠️ Migração cancelada para {tabela}.{Environment.NewLine}");
                                txtLog.Text += $"⚠️ Migração cancelada para {tabela}.{Environment.NewLine}";
                            });
                            continue;
                        }
                    }

                    if (await _firebirdService.FirebirdTemMaisColunasQueMySQL(tabela, _mysqlService))
                    {
                        await MessageService.MostrarDialogPersonalizado(window,
                            "Erro",
                            $"Erro: A tabela {tabela} no Firebird tem mais colunas que no MySQL. Ajuste a estrutura do MySQL para continuar."
                        );
                        continue;
                    }

                    var lotes = await _firebirdService.ObterDadosTabelaEmLotes(tabela);
                    foreach (var lote in lotes)
                    {
                        if (lote.Rows.Count == 0)
                        {
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                txtLog.Text += $"⚠️ Nenhum dado encontrado na tabela {tabela}.{Environment.NewLine}";
                            });
                            continue;
                        }

                        await _mysqlService.InserirDadosTabela(window, tabela, lote);
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            pbMigracao.Value = Math.Min(pbMigracao.Maximum, pbMigracao.Value + lote.Rows.Count);
                        });
                    }

                    if (await _firebirdService.ColunaExiste(window, tabela, "Enviado"))
                    {
                        await _firebirdService.AtualizarEnviado(window, tabela);
                    }

                    if (await _mysqlService.ColunaExiste(window, tabela, "Enviado"))
                    {
                        await _mysqlService.AtualizarEnviado(window, tabela);
                    }

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        txtLog.Text += $"✅ Tabela {tabela} migrada com sucesso!{Environment.NewLine}";
                    });
                }
                catch (Exception ex)
                {
                    LogService.RegistrarLog("ERROR", $"❌ Erro ao migrar {tabela}: {ex.Message}");
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        txtLog.Text += $"❌ Erro ao migrar {tabela}: {ex.Message}{Environment.NewLine}";
                    });
                }
            }
        });

        pbMigracao.IsVisible = false;
        btnMigrar.IsEnabled = true;
        txtLog.Text += $"🎉 Migração concluída!{Environment.NewLine}";
        pbMigracao.IsVisible = false;

        btnMigrar.IsEnabled = true;
        btnVerificarTabelas.IsEnabled = true;
        btnVerificarEstrutura.IsEnabled = true;
        btnGerarScripts.IsEnabled = true;
        btnGerarScripts.IsEnabled = true;
        btnGerarFeedback.IsEnabled = true;
    }

    private void BtnVerificarTabelas_Click(object? sender, RoutedEventArgs e)
    {
        CarregarTabelasMySQL();
        CompararTabelas();
    }

    private async Task VerificarEstruturaTabelas()
    {
        StringBuilder resultado = new StringBuilder();

        var tabelasSelecionadas = ObterTabelasSelecionadas();
        var txtLog = this.FindControl<TextBox>("txtLog");
        var pbMigracao = this.FindControl<ProgressBar>("pbMigracao");
        var btnMigrar = this.FindControl<Button>("btnMigrar");
        var btnVerificarTabelas = this.FindControl<Button>("btnVerificarTabelas");
        var btnVerificarEstrutura = this.FindControl<Button>("btnVerificarEstrutura");
        var btnGerarScripts = this.FindControl<Button>("btnGerarScripts");
        var btnGerarFeedback = this.FindControl<Button>("btnGerarFeedback");
        var window = this.GetVisualRoot() as Window;

        if (tabelasSelecionadas.Count == 0)
        {
            await MessageService.MostrarDialogPersonalizado(window,
                "Erro",
                $"Por favor, selecione ao menos uma tabela para verificar a estrutura."
            );
            return;
        }

        pbMigracao.IsVisible = true;
        pbMigracao.Value = 0;
        pbMigracao.Maximum = tabelasSelecionadas.Count;

        pbMigracao.IsVisible = true;
        btnMigrar.IsEnabled = false;
        btnVerificarTabelas.IsEnabled = false;
        btnVerificarEstrutura.IsEnabled = false;
        btnGerarScripts.IsEnabled = false;
        btnGerarFeedback.IsEnabled = false;

        LogService.RegistrarLog("INFO", $"📥 Limpando as tabelas com erro.");
        txtLog.Clear();
        _tabelasComErro.Clear();
        _tabelasParaCriar.Clear();

        await Task.Run(async () =>
        {
            foreach (var tabela in tabelasSelecionadas)
            {
                var estruturaMySQL = await _mysqlService.ObterEstruturaTabela(tabela);

                if (estruturaMySQL == null || estruturaMySQL.Count == 0) // 🔹 Se a tabela NÃO EXISTE no MySQL
                {
                    _tabelasParaCriar.Add(tabela);
                    resultado.AppendLine($"🚨 Tabela {tabela} **NÃO EXISTE** no MySQL e precisa ser criada.");
                }
                else
                {
                    var colunasFaltantes = await _firebirdService.CompararEstrutura(tabela, _mysqlService);

                    if (colunasFaltantes.Any()) // 🔹 Se existem colunas faltando, precisa de ajustes
                    {
                        _tabelasComErro.Add(tabela);
                        resultado.AppendLine($"❌ Tabela {tabela} precisa de ajustes! {colunasFaltantes.Count} colunas faltando.");
                    }
                }

                await Dispatcher.UIThread.InvokeAsync(() => pbMigracao.Value++);
            }
        });

        txtLog.Text = resultado.Length > 0
        ? resultado.ToString()
        : $"✅ Estrutura do MySQL compatível.{Environment.NewLine}";

        pbMigracao.IsVisible = false;
        btnMigrar.IsEnabled = true;
        btnVerificarTabelas.IsEnabled = true;
        btnVerificarEstrutura.IsEnabled = true;
        btnGerarScripts.IsEnabled = true;
        btnGerarFeedback.IsEnabled = true;
    }

    private async void BtnVerificarEstrutura_Click(object? sender, RoutedEventArgs e)
    {
        await VerificarEstruturaTabelas();
    }

    private async void SalvarScript(StringBuilder script)
    {
        var window = this.GetVisualRoot() as Window;

        var dialog = new SaveFileDialog
        {
            Title = "Salvar Script MySQL",
            Filters = new List<FileDialogFilter>
            {
                new FileDialogFilter { Name = "Arquivo SQL", Extensions = { "sql" } }
            },
            InitialFileName = "script-mysql-ajustes.sql"
        };

        var result = await dialog.ShowAsync(window);
        if (string.IsNullOrWhiteSpace(result))
            return;

        System.IO.File.WriteAllText(result, script.ToString(), Encoding.UTF8);

        await MessageService.MostrarDialogPersonalizado(window,
            "Sucesso",
            $"Script salvo com sucesso!"
        );
    }

    private async void BtnGerarScripts_Click(object sender, EventArgs e)
    {
        var tabelasSelecionadas = ObterTabelasSelecionadas();
        var txtLog = this.FindControl<TextBox>("txtLog");
        var pbMigracao = this.FindControl<ProgressBar>("pbMigracao");
        var btnMigrar = this.FindControl<Button>("btnMigrar");
        var btnVerificarTabelas = this.FindControl<Button>("btnVerificarTabelas");
        var btnVerificarEstrutura = this.FindControl<Button>("btnVerificarEstrutura");
        var btnGerarScripts = this.FindControl<Button>("btnGerarScripts");
        var btnGerarFeedback = this.FindControl<Button>("btnGerarFeedback");
        var window = this.GetVisualRoot() as Window;

        if (_tabelasParaCriar.Count == 0 && _tabelasComErro.Count == 0)
        {
            await MessageService.MostrarDialogPersonalizado(window,
                "Informação",
                $"Nenhuma tabela precisa ser corrigida ou criada!"
            );
            return;
        }

        StringBuilder scriptFinal = new StringBuilder();
        string firebirdDSN = ConfigService.CarregarConfiguracaoFirebird();

        pbMigracao.IsVisible = true;
        pbMigracao.Value = 0;
        pbMigracao.Maximum = _tabelasParaCriar.Count + _tabelasComErro.Count;

        await Task.Run(async () =>
        {
            foreach (var tabela in _tabelasParaCriar)
            {
                scriptFinal.AppendLine($"-- Criar tabela {tabela} no MySQL");
                scriptFinal.AppendLine(ScriptGeneratorService.GerarScriptFirebirdParaMySQL(tabela, firebirdDSN));

                await Dispatcher.UIThread.InvokeAsync(() => pbMigracao.Value++);
            }

            foreach (var tabela in _tabelasComErro)
            {
                var colunasFaltantes = await _firebirdService.CompararEstrutura(tabela, _mysqlService);
                string alterScript = ScriptGeneratorService.GerarScriptAlteracao(tabela, colunasFaltantes);
                scriptFinal.AppendLine(alterScript);

                await Dispatcher.UIThread.InvokeAsync(() => pbMigracao.Value++);
            }
        });

        SalvarScript(scriptFinal);
        pbMigracao.IsVisible = false;
        btnMigrar.IsEnabled = true;
        btnVerificarTabelas.IsEnabled = true;
        btnVerificarEstrutura.IsEnabled = true;
        btnGerarScripts.IsEnabled = true;
        btnGerarFeedback.IsEnabled = true;
    }

    private async void BtnGerarFeedback_Click(object? sender, RoutedEventArgs e)
    {
        var window = this.GetVisualRoot() as Window;

        // 1. Pergunta o nome do cliente
        string nomeCliente = await MessageService.PerguntarNomeCliente(window);
        if (string.IsNullOrWhiteSpace(nomeCliente))
            return;

        // 2. Dados
        var tabelas = ObterTabelasSelecionadas();
        var (_, banco, _, _) = ConfigService.CarregarConfiguracaoMySQL();

        // 3. Monta o texto
        StringBuilder feedback = new StringBuilder();
        feedback.AppendLine($"*Cliente:* _{nomeCliente}_");
        feedback.AppendLine($"*Tipo:* _Migração dados_");
        feedback.AppendLine($"*Software:* _Total on_");
        feedback.AppendLine($"*Descrição:*");
        feedback.AppendLine($"_Realizado a migração das seguintes tabelas para o banco de dados:_");
        feedback.AppendLine();
        feedback.AppendLine("*###Tabelas migradas*");

        string tabelasMigradas = string.Join(", ",
            tabelas.Select(t => t.ToLowerInvariant())
        );

        feedback.AppendLine(tabelasMigradas);
        feedback.AppendLine();
        feedback.AppendLine("_Realizado o banco de dados na web com os dados:_");
        feedback.AppendLine($"`{banco}`");
        feedback.AppendLine();
        feedback.AppendLine("*Instruções:*");
        feedback.AppendLine();
        feedback.AppendLine("_Banco de dados Web_");
        feedback.AppendLine("Tem que realizar o acerto dos contadores.");

        // 4. Salvar
        SaveFileDialog save = new SaveFileDialog
        {
            Title = "Salvar Feedback",
            InitialFileName = "feedback-migracao.txt",
            Filters = new List<FileDialogFilter> {
            new FileDialogFilter { Name = "Arquivo de Texto", Extensions = { "txt" } }
        }
        };

        var caminho = await save.ShowAsync(window);
        if (!string.IsNullOrWhiteSpace(caminho))
        {
            File.WriteAllText(caminho, feedback.ToString(), Encoding.UTF8);
            await MessageService.MostrarDialogPersonalizado(window, "Sucesso", "Feedback gerado com sucesso!");
        }
    }
}

