using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using BlackSync.Enums;
using BlackSync.Services;
using BlackSync.ViewModels;
using Google.Protobuf.WellKnownTypes;
using Material.Dialog;
using MsBox.Avalonia.Enums;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace BlackSync.Views
{
    public partial class ManutencaoView : UserControl
    {
        private MySQLService _mysqlService;
        private FirebirdService _firebirdService;

        private Window GetWindow() => (Window)this.VisualRoot;

        public ManutencaoView()
        {
            InitializeComponent();
            dpDe.SelectedDateChanged += DpDe_SelectedDateChanged;

            this.AttachedToVisualTree += OnViewLoaded;
        }

        private void DpDe_SelectedDateChanged(object? sender, DatePickerSelectedValueChangedEventArgs e)
        {
            if (dpDe.SelectedDate.HasValue)
            {
                dpAte.SelectedDate = dpDe.SelectedDate;
            }
        }

        private void SetarStatusBotoes(bool habilitar)
        {
            btnAtualizarFilial.IsEnabled = habilitar;
            btnExcluirDados.IsEnabled = habilitar;
            btnFecharDados.IsEnabled = habilitar;
            btnReabrirDados.IsEnabled = habilitar;
            btnAlterarNumeracao.IsEnabled = habilitar;
        }

        private async void OnViewLoaded(object? sender, VisualTreeAttachmentEventArgs e)
        {
            var window = this.GetVisualRoot() as Window;

            try
            {
                var (servidor, porta, banco, usuario, senha) = ConfigService.CarregarConfiguracaoMySQL();
                var dsn = ConfigService.CarregarConfiguracaoFirebird();

                if (string.IsNullOrWhiteSpace(servidor) ||
                    string.IsNullOrWhiteSpace(porta) ||
                    string.IsNullOrWhiteSpace(banco) ||
                    string.IsNullOrWhiteSpace(usuario) ||
                    string.IsNullOrWhiteSpace(senha) ||
                    string.IsNullOrWhiteSpace(dsn))
                {
                    await MessageService.MostrarDialogPersonalizado(window,
                        "Configuração ausente",
                        $"⚠️ Nenhuma configuração de banco encontrada.{Environment.NewLine}Por favor, configure antes de acessar a tela de Manutenção.");

                    SetarStatusBotoes(false);

                    return;
                }

                _mysqlService = new MySQLService(servidor, porta, banco, usuario, senha);
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

        private List<string> ObterTabelasPorCategoria(List<string> categorias)
        {
            Dictionary<string, List<string>> tabelasPorCategoria = new Dictionary<string, List<string>>()
            {
                { "Estoque", new List<string> {"movestoque", "nfentrada", "itemnfentrada" } },
                { "Financeiro", new List<string> {"baixapagar", "baixareceber", "contacartao", "pagar", "receber"} },
                { "Vendas", new List<string> {"abrecaixa", "caixa", "itensnf", "itenspedidovenda", "notafiscal", "pedidosvenda" } }
            };

            List<string> tabelasSelecionadas = new List<string>();

            foreach (var categoria in categorias)
            {
                if (tabelasPorCategoria.ContainsKey(categoria))
                {
                    tabelasSelecionadas.AddRange(tabelasPorCategoria[categoria]);
                }
            }

            return tabelasSelecionadas.Distinct().ToList();
        }

        private async void BtnAtualizarFilial_Click(object? sender, RoutedEventArgs e)
        {
            var window = this.GetVisualRoot() as Window;

            try
            {
                LogService.RegistrarLog(
                    "INFO",
                    "🔄 Iniciando o processo de atualização da filial."
                );

                // Pega a numeração da filial
                var config = ConfigService.CarregarConfiguracaoEmpresa();
                int xFilial = int.TryParse(config.filial, out var parsedFilial) ? parsedFilial : 0;

                if (xFilial < 0)
                {
                    await MessageService.MostrarDialogPersonalizado(window,
                        "Aviso",
                        $"⚠️ Por favor, selecione um número de filial válido maior ou igual a zero."
                    );

                    LogService.RegistrarLog("INFO", "⚠️ Nenhuma filial válida foi informada.");
                    return;
                }

                LogService.RegistrarLog(
                    "INFO",
                    $"📌 Número da filial selecionada: {xFilial}."
                );

                // Verifica quais tipos de dados foram marcados
                List<string> categoriasSelecionadas = new List<string>();
                if (cbEstoque.IsChecked is true) categoriasSelecionadas.Add("Estoque");
                if (cbFinanceiro.IsChecked is true) categoriasSelecionadas.Add("Financeiro");
                if (cbVendas.IsChecked is true) categoriasSelecionadas.Add("Vendas");

                if (categoriasSelecionadas.Count == 0)
                {
                    Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        await MessageService.MostrarDialogPersonalizado(window,
                            "Aviso",
                            $"⚠️ Selecione ao menos um tipo de dados (Estoque, Financeiro, Vendas)."
                        );
                    });

                    LogService.RegistrarLog(
                        "INFO",
                        $"⚠️ Nenhuma categoria selecionada."
                    );

                    return;
                }

                // Obtém as tabelas especificas para cada categoria
                List<string> tabelasLimpezaBanco = ObterTabelasPorCategoria(categoriasSelecionadas);

                if (tabelasLimpezaBanco.Count == 0)
                {
                    await MessageService.MostrarDialogPersonalizado(window,
                        "Aviso",
                        $"⚠️ Nenhuma tabela foi selecionada para atualização da filial."
                    );

                    LogService.RegistrarLog(
                        "INFO",
                        "⚠️ Nenhuma tabela foi selecionada para atualização da filial."
                    );

                    return;
                }

                LogService.RegistrarLog(
                    "INFO",
                    $"🔄 Tabelas selecionadas para atualização da filial: {string.Join(", ", tabelasLimpezaBanco)}."
                );

                // Verifica qual banco foi selecionado
                string bancoSelecionado = cbBanco.SelectedItem?.ToString() ?? "Nenhum";

                if (bancoSelecionado != "Firebird")
                {
                    await MessageService.MostrarDialogPersonalizado(window,
                        "Erro",
                        $"❌ O banco de dados selecionado não é válido para esta ação. Escolha o Firebird."
                    );

                    LogService.RegistrarLog(
                        "INFO",
                        $"⚠️ Banco selecionado inválido: {bancoSelecionado}."
                    );

                    return;
                }

                // Iniciar barra de progresso
                pbProgresso.IsVisible = true;
                pbProgresso.Minimum = 0;
                pbProgresso.Maximum = tabelasLimpezaBanco.Count;
                pbProgresso.Value = 0;

                SetarStatusBotoes(false);

                // Atualiza a filial nas tabelas do Firebird
                LogService.RegistrarLog(
                    "INFO",
                    $"🔄 Atualizando filial nas tabelas do Firebird."
                );

                var mensagem =
                        $"🔍 Resumo da Operação{Environment.NewLine}" +
                        $"🗄 Banco de Dados: {bancoSelecionado}{Environment.NewLine}" +
                        $"📌 Categorias Selecionadas: {string.Join(", ", categoriasSelecionadas)}{Environment.NewLine}{Environment.NewLine}" +
                        $"⚠️ Esta ação atualizará a filial para a(s) categoria(s) selecionada(s).{Environment.NewLine}" +
                        $"❗ Deseja realmente continuar?";

                var resposta = await MessageService.MostrarDialogConfirmacaoPersonalizado(this, "Confirmação de Reabertura", mensagem);

                if (resposta == OpcaoConfirmacao.Sim)
                {
                    // Executar a atualização das tabelas de forma assíncrona

                    foreach (string tabela in tabelasLimpezaBanco)
                    {
                        await Task.Run(() =>
                        {
                            _firebirdService.AtualizarFilialFirebird(tabela, xFilial);
                        });

                        // Atualiza a UI na thread correta
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            pbProgresso.Value = Math.Min(pbProgresso.Maximum, pbProgresso.Value + 1);
                        });
                    }
                    ;

                    LogService.RegistrarLog(
                        "SUCCESSO",
                        $"✅ Atualização da filial concluída para as tabelas: {string.Join(", ", tabelasLimpezaBanco)}."
                    );
                }
                else
                {
                    LogService.RegistrarLog(
                        "INFO",
                        $"⚠️ Operação cancelada: Atualização da filial não foi realizada para as categorias: {string.Join(", ", categoriasSelecionadas)}."
                    );

                    var mensagem2 =
                        $"🔄 Ação Cancelada{Environment.NewLine}{Environment.NewLine}" +
                        $"As seguintes categorias não tiveram sua filial atualizada:{Environment.NewLine}" +
                        $"📌 {string.Join(", ", categoriasSelecionadas)}{Environment.NewLine}{Environment.NewLine}" +
                        $"✅ Nenhuma alteração foi feita." +
                        $"Operação Cancelada";

                    var resposta2 = await MessageService.MostrarDialogConfirmacaoPersonalizado(this, "Confirmação de Reabertura", mensagem2);
                }

                SetarStatusBotoes(true);
                pbProgresso.IsVisible = false;

                await MessageService.MostrarDialogPersonalizado(window,
                        "Sucesso",
                        $"✅ Atualização da filial concluída com sucesso!"
                    );
            }
            catch (Exception ex)
            {
                LogService.RegistrarLog(
                    "ERRO",
                    $"❌ Erro ao atualizar a filial: {ex.Message}"
                );

                await MessageService.MostrarDialogPersonalizado(window,
                    "Erro",
                    $"❌ Ocorreu um erro ao atualizar a filial: {Environment.NewLine}{ex.Message}"
                );
            }
        }

        private async void BtnReabrirDados_Click(object sender, EventArgs e)
        {
            var window = this.GetVisualRoot() as Window;

            try
            {
                LogService.RegistrarLog(
                    "INFO",
                    "🔄 Iniciando o processo de reabertura"
                );

                // Pega o período selecionado
                DateTime? dataInicio = dpDe.SelectedDate?.DateTime;
                DateTime? dataFim = dpAte.SelectedDate?.DateTime;

                LogService.RegistrarLog(
                    "INFO",
                    $"📤 Foi selecionado o perído De: {dataInicio} até: {dataFim}."
                );

                // Verifica quais tipos de dados foram marcados
                List<string> categoriasSelecionadas = new List<string>();
                if (cbEstoque.IsChecked is true) categoriasSelecionadas.Add("Estoque");
                if (cbFinanceiro.IsChecked is true) categoriasSelecionadas.Add("Financeiro");
                if (cbVendas.IsChecked is true) categoriasSelecionadas.Add("Vendas");

                if (categoriasSelecionadas.Count == 0)
                {
                    await MessageService.MostrarDialogPersonalizado(window,
                        "Aviso",
                        $"⚠️ Selecione ao menos um tipo de dado (Estoque, Financeiro, Vendas)."
                    );

                    LogService.RegistrarLog(
                        "INFO",
                        $"⚠️ Nenhuma categoria selecionada."
                    );

                    return;
                }

                // Obtém as tabelas específicas para cada categoria
                List<string> tabelasParaReabrir = ObterTabelasPorCategoria(categoriasSelecionadas);

                LogService.RegistrarLog(
                    "INFO",
                    $"🔄 Iniciando a reabertura dos dados das tabelas: {tabelasParaReabrir}."
                );

                if (tabelasParaReabrir.Count == 0)
                {
                    await MessageService.MostrarDialogPersonalizado(window,
                        "Aviso",
                        $"⚠️ Nenhuma tabela foi selecionada para reabertura de movimento."
                    );

                    LogService.RegistrarLog(
                        "INFO",
                        "⚠️ Nenhuma tabela foi selecionada para reabertura de movimento."
                    );

                    return;
                }

                // Verifica qual banco foi selecionado
                string bancoSelecionado = cbBanco.SelectedItem?.ToString() ?? "";
                LogService.RegistrarLog(
                    "INFO",
                    $"📤 Foi selecionado o banco de dados: {bancoSelecionado}"
                );

                if (string.IsNullOrWhiteSpace(bancoSelecionado))
                {
                    await MessageService.MostrarDialogPersonalizado(window,
                        "Aviso",
                        $"⚠️ Selecione ao menos um banco de dados."
                    );

                    LogService.RegistrarLog(
                        "INFO",
                        $"⚠️ Nenhum banco de dados selecionado."
                    );

                    return;
                }

                string operadorSelecionado = cbComparacao.SelectedItem?.ToString()?.ToLower();
                
                if (string.IsNullOrWhiteSpace(operadorSelecionado))
                {
                    await MessageService.MostrarDialogPersonalizado(window,
                        "Aviso",
                        $"⚠️ Selecione ao menos uma condição."
                    );

                    LogService.RegistrarLog(
                        "INFO",
                        $"⚠️ Nenhuma condição selecionada."
                    );

                    return;
                }

                if (operadorSelecionado == "between" && dataFim == null)
                {
                    await MessageService.MostrarMensagem(GetWindow(), "Erro", "Para BETWEEN, selecione a data final.");
                    return;
                }

                LogService.RegistrarLog(
                    "INFO",
                    $"📤 Foi selecionado a condição: {operadorSelecionado}"
                );

                if (bancoSelecionado == "Firebird" || bancoSelecionado == "Ambos")
                {
                    // Verifique antes se as datas não são nulas
                    if (dataInicio is null || dataFim is null)
                    {
                        await MessageService.MostrarDialogPersonalizado(window,
                            "Erro",
                            $"❌ Selecione um período válido."
                        );

                        return;
                    }

                    // Iniciar barra de progresso
                    pbProgresso.IsVisible = true;
                    pbProgresso.Minimum = 0;
                    pbProgresso.Maximum = tabelasParaReabrir.Count;
                    pbProgresso.Value = 0;

                    SetarStatusBotoes(false);

                    LogService.RegistrarLog(
                        "INFO",
                        $"🔄 Iniciando a reabertura dos dados das tabelas: {tabelasParaReabrir} para o banco de dados: {bancoSelecionado}."
                    );

                    var mensagem =
                        $"🔍 Resumo da Operação{Environment.NewLine}" +
                        $"📅 Período: {dataInicio:dd/MM/yyyy} a {dataFim:dd/MM/yyyy}{Environment.NewLine}" +
                        $"🗄 Banco de Dados: {bancoSelecionado}{Environment.NewLine}" +
                        $"📌 Categorias Selecionadas: {string.Join(", ", categoriasSelecionadas)}{Environment.NewLine}{Environment.NewLine}" +
                        $"⚠️ Esta ação reabrirá os movimentos para o período selecionado.{Environment.NewLine}" +
                        $"❗ Deseja realmente continuar?";

                    var resposta = await MessageService.MostrarDialogConfirmacaoPersonalizado(this, "Confirmação de Reabertura", mensagem);

                    if (resposta == OpcaoConfirmacao.Sim)
                    {
                        await Task.Run(async () =>
                        {
                            foreach (string tabela in tabelasParaReabrir)
                            {
                                _firebirdService.ReabrirMovimentoFirebird(tabela, operadorSelecionado, dataInicio.Value, dataFim.Value);

                                await Dispatcher.UIThread.InvokeAsync(() =>
                                {
                                    pbProgresso.Value = Math.Min(pbProgresso.Maximum, pbProgresso.Value + 1);
                                });
                            }
                        });

                        LogService.RegistrarLog(
                            "SUCCESS",
                            $"$🚀 Finalizado a rebertura dos dados das tabelas: {tabelasParaReabrir} com sucesso!"
                        );
                    }
                    else
                    {
                        LogService.RegistrarLog(
                            "INFO",
                            $"⚠️ Operação cancelada: Reabertura do movimento não foi realizada para as categorias: {string.Join(", ", categoriasSelecionadas)}."
                        );

                        await MessageService.MostrarMensagem(
                            GetWindow(),
                            "Operação Cancelada",
                            $"🔄 Ação Cancelada{Environment.NewLine}{Environment.NewLine}" +
                            $"As seguintes categorias não tiveram seus movimentos reabertos:{Environment.NewLine}" +
                            $"📌 {string.Join(", ", categoriasSelecionadas)}{Environment.NewLine}{Environment.NewLine}" +
                            $"✅ Nenhuma alteração foi feita.",
                            MsBox.Avalonia.Enums.Icon.Warning
                        );
                    }
                }

                if (bancoSelecionado == "MySQL" || bancoSelecionado == "Ambos")
                {
                    // Verifique antes se as datas não são nulas
                    if (dataInicio is null || dataFim is null)
                    {
                        await MessageService.MostrarMensagem(GetWindow(), "Erro", "Selecione um período válido.");
                        return;
                    }

                    // Iniciar barra de progresso
                    pbProgresso.IsVisible = true;
                    pbProgresso.Minimum = 0;
                    pbProgresso.Maximum = tabelasParaReabrir.Count;
                    pbProgresso.Value = 0;

                    SetarStatusBotoes(false);

                    LogService.RegistrarLog(
                        "INFO",
                        $"🔄 Iniciando a reabertura dos movimentos das tabelas: {tabelasParaReabrir} para o banco de dados: {bancoSelecionado}."
                    );

                    var mensagem =
                        $"🔍 Resumo da Operação{Environment.NewLine}" +
                        $"📅 Período: {dataInicio:dd/MM/yyyy} a {dataFim:dd/MM/yyyy}{Environment.NewLine}" +
                        $"🗄 Banco de Dados: {bancoSelecionado}{Environment.NewLine}" +
                        $"📌 Categorias Selecionadas: {string.Join(", ", categoriasSelecionadas)}{Environment.NewLine}{Environment.NewLine}" +
                        $"⚠️ Esta ação reabrirá os movimentos para o período selecionado.{Environment.NewLine}" +
                        $"❗ Deseja realmente continuar?";

                    var resposta = await MessageService.MostrarDialogConfirmacaoPersonalizado(this, "Confirmação de Reabertura", mensagem);

                    if (resposta == OpcaoConfirmacao.Sim)
                    {
                        await Task.Run(async () =>
                        {
                            foreach (string tabela in tabelasParaReabrir)
                            {
                                _mysqlService.ReabrirMovimentoMySQL(tabela, operadorSelecionado, dataInicio.Value, dataFim.Value);

                                await Dispatcher.UIThread.InvokeAsync(() =>
                                {
                                    pbProgresso.Value = Math.Min(pbProgresso.Maximum, pbProgresso.Value + 1);
                                });
                            }
                        });

                        LogService.RegistrarLog(
                            "SUCCESS",
                            $"$🚀 Finalizado a rebertura dos movimentos das tabelas: {tabelasParaReabrir} com sucesso!"
                        );
                    }
                    else
                    {
                        LogService.RegistrarLog(
                            "INFO",
                            $"⚠️ Operação cancelada: Reabertura do movimento não foi realizada para as categorias: {string.Join(", ", categoriasSelecionadas)}."
                        );

                        await MessageService.MostrarMensagem(
                            GetWindow(), // ou 'this.GetVisualRoot() as Window' se preferir
                            "Operação Cancelada",
                            $"As seguintes categorias não tiveram seus movimentos reabertos:{Environment.NewLine}" +
                            $"📌 {string.Join(", ", categoriasSelecionadas)}{Environment.NewLine}{Environment.NewLine}" +
                            $"✅ Nenhuma alteração foi feita.",
                            MsBox.Avalonia.Enums.Icon.Warning
                        );
                    }
                }

                SetarStatusBotoes(true);
                pbProgresso.IsVisible = false;

                await MessageService.MostrarDialogPersonalizado(window,
                    "Sucesso",
                    $"✅ Movimento reaberto com sucesso!"    
                );
            }
            catch (Exception ex)
            {
                LogService.RegistrarLog(
                    "ERROR",
                    $"❌ Erro ao reabrir movimento: {ex.Message}"
                );

                await MessageService.MostrarDialogPersonalizado(window,
                    "Erro",
                    $"❌ Erro ao reabrir movimento: {ex.Message}"
                );
            }
        }

        private async void BtnFecharDados_Click(object? sender, RoutedEventArgs e) 
        {
            var window = this.GetVisualRoot() as Window;

            try
            {
                LogService.RegistrarLog(
                    "INFO",
                    "🔄 Iniciando o processo de fechamento dos movimentos"
                );

                // Pega o período selecionado
                DateTime? dataInicio = dpDe.SelectedDate?.DateTime;
                DateTime? dataFim = dpAte.SelectedDate?.DateTime;

                LogService.RegistrarLog(
                    "INFO",
                    $"📤 Foi selecionado o perído De: {dataInicio} até: {dataFim}."
                );

                // Verifica quais tipos de dados foram marcados
                List<string> categoriasSelecionadas = new List<string>();
                if (cbEstoque.IsChecked is true) categoriasSelecionadas.Add("Estoque");
                if (cbFinanceiro.IsChecked is true) categoriasSelecionadas.Add("Financeiro");
                if (cbVendas.IsChecked is true) categoriasSelecionadas.Add("Vendas");

                if (categoriasSelecionadas.Count == 0)
                {
                    await MessageService.MostrarDialogPersonalizado(window,
                        "Aviso",
                        $"⚠️ Selecione ao menos um tipo de dado (Estoque, Financeiro, Vendas)."
                    );

                    LogService.RegistrarLog(
                        "INFO",
                        $"⚠️ Nenhuma categoria selecionada."
                    );

                    return;
                }

                // Obtém as tabelas específicas para cada categoria
                List<string> tabelasParaFechar = ObterTabelasPorCategoria(categoriasSelecionadas);

                LogService.RegistrarLog(
                    "INFO",
                    $"🔄 Iniciando o fechamento dos movimentos das tabelas: {tabelasParaFechar}."
                );

                if (tabelasParaFechar.Count == 0)
                {
                    await MessageService.MostrarDialogPersonalizado(window,
                        "Aviso",
                        $"⚠️ Nenhuma tabela foi selecionada para reabertura de movimento."
                    );

                    LogService.RegistrarLog(
                        "INFO",
                        "⚠️ Nenhuma tabela foi selecionada para o fechamento dos movimentos."
                    );

                    return;
                }

                // Verifica qual banco foi selecionado
                string bancoSelecionado = cbBanco.SelectedItem?.ToString() ?? "";

                LogService.RegistrarLog(
                    "INFO",
                    $"📤 Foi selecionado o banco de dados: {bancoSelecionado}"
                );

                if (string.IsNullOrWhiteSpace(bancoSelecionado))
                {
                    await MessageService.MostrarDialogPersonalizado(window,
                        "Aviso",
                        $"⚠️ Selecione ao menos um banco de dados."
                    );

                    LogService.RegistrarLog(
                        "INFO",
                        $"⚠️ Nenhum banco de dados selecionado."
                    );

                    return;
                }

                string operadorSelecionado = cbComparacao.SelectedItem?.ToString()?.ToLower();
                
                if (string.IsNullOrWhiteSpace(operadorSelecionado))
                {
                    await MessageService.MostrarDialogPersonalizado(window,
                        "Aviso",
                        $"⚠️ Selecione ao menos uma condição."
                    );

                    LogService.RegistrarLog(
                        "INFO",
                        $"⚠️ Nenhuma condição selecionada."
                    );

                    return;
                }

                if (operadorSelecionado == "between" && dataFim == null)
                {
                    await MessageService.MostrarMensagem(GetWindow(), "Erro", "Para BETWEEN, selecione a data final.");
                    return;
                }

                LogService.RegistrarLog(
                    "INFO",
                    $"📤 Foi selecionado a condição: {operadorSelecionado}"
                );

                if (bancoSelecionado == "Firebird" || bancoSelecionado == "Ambos")
                {
                    // Verifique antes se as datas não são nulas
                    if (dataInicio is null || dataFim is null)
                    {
                        await MessageService.MostrarDialogPersonalizado(window,
                            "Erro",
                            $"❌ Selecione um período válido."
                        );

                        return;
                    }

                    // Iniciar barra de progresso
                    pbProgresso.IsVisible = true;
                    pbProgresso.Minimum = 0;
                    pbProgresso.Maximum = tabelasParaFechar.Count;
                    pbProgresso.Value = 0;

                    SetarStatusBotoes(false);

                    LogService.RegistrarLog(
                        "INFO",
                        $"🔄 Iniciando o fechamento dos movimentos das tabelas: {tabelasParaFechar} para o banco de dados: {bancoSelecionado}."
                    );

                    var mensagem =
                        $"🔍 Resumo da Operação{Environment.NewLine}" +
                        $"📅 Período: {dataInicio:dd/MM/yyyy} a {dataFim:dd/MM/yyyy}{Environment.NewLine}" +
                        $"🗄 Banco de Dados: {bancoSelecionado}{Environment.NewLine}" +
                        $"📌 Categorias Selecionadas: {string.Join(", ", categoriasSelecionadas)}{Environment.NewLine}{Environment.NewLine}" +
                        $"⚠️ Esta ação fechará os movimentos para o período selecionado.{Environment.NewLine}" +
                        $"❗ Deseja realmente continuar?";

                    var resposta = await MessageService.MostrarDialogConfirmacaoPersonalizado(this, "Confirmação de Reabertura", mensagem);

                    if (resposta == OpcaoConfirmacao.Sim)
                    {
                        // Executar a atualização das tabelas de forma assíncrona

                        foreach (string tabela in tabelasParaFechar)
                        {
                            await Task.Run(() =>
                            {
                                _firebirdService.FecharMovimentoFirebird(tabela, operadorSelecionado, dataInicio.Value, dataFim.Value);
                            });

                            // Atualiza a UI na thread correta
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                pbProgresso.Value = Math.Min(pbProgresso.Maximum, pbProgresso.Value + 1);
                            });
                        }
                    ;

                        LogService.RegistrarLog(
                            "SUCCESSO",
                            $"✅ Atualização da filial concluída para as tabelas: {string.Join(", ", tabelasParaFechar)}."
                        );
                    }
                    else
                    {
                        LogService.RegistrarLog(
                            "INFO",
                            $"⚠️ Operação cancelada: Fechamento do movimento não foi realizada para as categorias: {string.Join(", ", categoriasSelecionadas)}."
                        );

                        await MessageService.MostrarMensagem(
                            GetWindow(),
                            "Operação Cancelada",
                            $"🔄 Ação Cancelada{Environment.NewLine}{Environment.NewLine}" +
                            $"As seguintes categorias não tiveram seus movimentos reabertos:{Environment.NewLine}" +
                            $"📌 {string.Join(", ", categoriasSelecionadas)}{Environment.NewLine}{Environment.NewLine}" +
                            $"✅ Nenhuma alteração foi feita.",
                            MsBox.Avalonia.Enums.Icon.Warning
                        );
                    }
                }

                if (bancoSelecionado == "MySQL" || bancoSelecionado == "Ambos")
                {
                    // Verifique antes se as datas não são nulas
                    if (dataInicio is null || dataFim is null)
                    {
                        await MessageService.MostrarMensagem(GetWindow(), "Erro", "Selecione um período válido.");
                        return;
                    }

                    // Iniciar barra de progresso
                    pbProgresso.IsVisible = true;
                    pbProgresso.Minimum = 0;
                    pbProgresso.Maximum = tabelasParaFechar.Count;
                    pbProgresso.Value = 0;

                    SetarStatusBotoes(false);

                    LogService.RegistrarLog(
                        "INFO",
                        $"🔄 Iniciando o fechamento dos movimentos das tabelas: {tabelasParaFechar} para o banco de dados: {bancoSelecionado}."
                    );

                    var mensagem =
                        $"🔍 Resumo da Operação{Environment.NewLine}" +
                        $"📅 Período: {dataInicio:dd/MM/yyyy} a {dataFim:dd/MM/yyyy}{Environment.NewLine}" +
                        $"🗄 Banco de Dados: {bancoSelecionado}{Environment.NewLine}" +
                        $"📌 Categorias Selecionadas: {string.Join(", ", categoriasSelecionadas)}{Environment.NewLine}{Environment.NewLine}" +
                        $"⚠️ Esta ação fechar os movimentos para o período selecionado.{Environment.NewLine}" +
                        $"❗ Deseja realmente continuar?";

                    var resposta = await MessageService.MostrarDialogConfirmacaoPersonalizado(this, "Confirmação de Reabertura", mensagem);

                    if (resposta == OpcaoConfirmacao.Sim)
                    {
                            foreach (string tabela in tabelasParaFechar)
                            {
                                await Task.Run(() =>
                                {
                                    _mysqlService.FecharMovimentoMySQL(tabela, operadorSelecionado, dataInicio.Value, dataFim.Value);
                                });

                                // Atualiza a UI na thread correta
                                await Dispatcher.UIThread.InvokeAsync(() =>
                                {
                                    pbProgresso.Value = Math.Min(pbProgresso.Maximum, pbProgresso.Value + 1);
                                });
                            };

                        LogService.RegistrarLog(
                            "SUCCESS",
                            $"$🚀 Finalizado o fechamento dos movimentos das tabelas: {tabelasParaFechar} com sucesso!"
                        );
                    }
                    else
                    {
                        LogService.RegistrarLog(
                            "INFO",
                            $"⚠️ Operação cancelada: Fechamento do movimento não foi realizada para as categorias: {string.Join(", ", categoriasSelecionadas)}."
                        );

                        await MessageService.MostrarMensagem(
                            GetWindow(),
                            "Operação Cancelada",
                            $"As seguintes categorias não tiveram seus movimentos fechados:{Environment.NewLine}" +
                            $"📌 {string.Join(", ", categoriasSelecionadas)}{Environment.NewLine}{Environment.NewLine}" +
                            $"✅ Nenhuma alteração foi feita.",
                            MsBox.Avalonia.Enums.Icon.Warning
                        );
                    }

                }

                SetarStatusBotoes(true);

                pbProgresso.IsVisible = false;

                await MessageService.MostrarDialogPersonalizado(window,
                    "Sucesso",
                    $"✅ Movimento fechado com sucesso!"
                );
            }
            catch (Exception ex)
            {
                LogService.RegistrarLog(
                    "ERROR",
                    $"❌ Erro ao fechar movimento: {ex.Message}"
                );

                await MessageService.MostrarDialogPersonalizado(window,
                    "Erro",
                    $"❌ Erro ao fechar movimento: {ex.Message}"
                );
            }
        }

        private async void BtnExcluirDados_Click(object? sender, RoutedEventArgs e) 
        {
            var window = this.GetVisualRoot() as Window;

            try
            {

                LogService.RegistrarLog(
                    "INFO",
                    "🔄 Iniciando o processo de exclusão dos movimentos"
                );

                // Pega o período selecionado
                DateTime? dataInicio = dpDe.SelectedDate?.DateTime;
                DateTime? dataFim = dpAte.SelectedDate?.DateTime;

                LogService.RegistrarLog(
                    "INFO",
                    $"📤 Foi selecionado o perído De: {dataInicio} até: {dataFim}."
                );

                // Verifica quais tipos de dados foram marcados
                List<string> categoriasSelecionadas = new List<string>();
                if (cbEstoque.IsChecked is true) categoriasSelecionadas.Add("Estoque");
                if (cbFinanceiro.IsChecked is true) categoriasSelecionadas.Add("Financeiro");
                if (cbVendas.IsChecked is true) categoriasSelecionadas.Add("Vendas");

                if (categoriasSelecionadas.Count == 0)
                {
                    await MessageService.MostrarDialogPersonalizado(window,
                        "Aviso",
                        $"⚠️ Selecione ao menos um tipo de dado (Estoque, Financeiro, Vendas)."
                    );

                    LogService.RegistrarLog(
                        "INFO",
                        $"⚠️ Nenhuma categoria selecionada."
                    );

                    return;
                }

                // Obtém as tabelas específicas para cada categoria
                List<string> tabelasParaExcluir = ObterTabelasPorCategoria(categoriasSelecionadas);

                LogService.RegistrarLog(
                    "INFO",
                    $"🔄 Iniciando a exclusão dos movimentos na tabelas: {tabelasParaExcluir}"
                );

                if (tabelasParaExcluir.Count == 0)
                {
                    await MessageService.MostrarDialogPersonalizado(window,
                        "Aviso",
                        $"Nenhuma tabela foi selecionada para exclusão dos movimentos.");

                    LogService.RegistrarLog(
                        "INFO",
                        "⚠️ Nenhuma tabela foi selecionada para a exclusão dos movimentos."
                    );

                    return;
                }

                // Verifica qual banco foi selecionado
                string bancoSelecionado = cbBanco.SelectedItem.ToString() ?? "";

                LogService.RegistrarLog(
                    "INFO",
                    $"📤 Foi selecionado o banco de dados: {bancoSelecionado}"
                );

                if (string.IsNullOrWhiteSpace(bancoSelecionado))
                {
                    await MessageService.MostrarDialogPersonalizado(window,
                        "Aviso",
                        $"⚠️ Selecione ao menos um banco de dados."
                    );

                    LogService.RegistrarLog(
                        "INFO",
                        $"⚠️ Nenhum banco de dados selecionado."
                    );

                    return;
                }

                string operadorSelecionado = cbComparacao.SelectedItem?.ToString()?.ToLower();

                if (string.IsNullOrWhiteSpace(operadorSelecionado))
                {
                    await MessageService.MostrarDialogPersonalizado(window,
                        "Aviso",
                        $"⚠️ Selecione ao menos uma condição."
                    );

                    LogService.RegistrarLog(
                        "INFO",
                        $"⚠️ Nenhuma condição selecionada."
                    );

                    return;
                }

                if (operadorSelecionado == "between" && dataFim == null)
                {
                    await MessageService.MostrarMensagem(GetWindow(), "Erro", "Para BETWEEN, selecione a data final.");
                    return;
                }

                LogService.RegistrarLog(
                    "INFO",
                    $"📤 Foi selecionado a condição: {operadorSelecionado}"
                );

                if (bancoSelecionado == "Firebird" || bancoSelecionado == "Ambos")
                {
                    // Verifique antes se as datas não são nulas
                    if (dataInicio is null || dataFim is null)
                    {
                        await MessageService.MostrarDialogPersonalizado(window,
                            "Erro",
                            $"❌ Selecione um período válido."
                        );

                        return;
                    }

                    pbProgresso.IsVisible = true;
                    pbProgresso.Minimum = 0;
                    pbProgresso.Maximum = tabelasParaExcluir.Count;
                    pbProgresso.Value = 0;

                    SetarStatusBotoes(false);

                    LogService.RegistrarLog(
                        "INFO",
                        $"🔄 Iniciando a exclusão dos movimentos das tabelas: {tabelasParaExcluir} para o banco de dados: {bancoSelecionado}."
                    );

                    var mensagem =
                        $"🔍 Resumo da Operação{Environment.NewLine}" +
                        $"📅 Período: {dataInicio:dd/MM/yyyy} a {dataFim:dd/MM/yyyy}{Environment.NewLine}" +
                        $"🗄 Banco de Dados: {bancoSelecionado}{Environment.NewLine}" +
                        $"📌 Categorias Selecionadas: {string.Join(", ", categoriasSelecionadas)}{Environment.NewLine}{Environment.NewLine}" +
                        $"⚠️ Esta ação excluirá os movimentos para o período selecionado.{Environment.NewLine}" +
                        $"❗ Deseja realmente continuar?";

                    var resposta = await MessageService.MostrarDialogConfirmacaoPersonalizado(this, "Confirmação de Reabertura", mensagem);

                    if (resposta == OpcaoConfirmacao.Sim)
                    {
                        // Executar a atualização das tabelas de forma assíncrona
                            // Executa a exclusão no Firebird
                            foreach (string tabela in tabelasParaExcluir)
                            {
                                await Task.Run(() =>
                                {
                                    _firebirdService.ExcluirMovimentoFirebird(tabela, operadorSelecionado, dataInicio.Value, dataFim.Value);
                                });

                                // Atualiza a UI na thread correta
                                await Dispatcher.UIThread.InvokeAsync(() =>
                                {
                                    pbProgresso.Value = Math.Min(pbProgresso.Maximum, pbProgresso.Value + 1);
                                });
                        };

                        LogService.RegistrarLog(
                            "SUCCESSO",
                            $"🚀 Finalizado a exclusão dos movimentos das tabelas: {tabelasParaExcluir} com sucesso!"
                        );
                    }
                    else
                    {
                        LogService.RegistrarLog(
                           "INFO",
                           $"⚠️ Operação cancelada: Exclusão do movimento não foi realizada para as categorias: {string.Join(", ", categoriasSelecionadas)}."
                        );

                        await MessageService.MostrarMensagem(
                            GetWindow(),
                            "Operação Cancelada",
                            $"🔄 Ação Cancelada{Environment.NewLine}{Environment.NewLine}" +
                            $"As seguintes categorias não tiveram seus movimentos excluídos:{Environment.NewLine}" +
                            $"📌 {string.Join(", ", categoriasSelecionadas)}{Environment.NewLine}{Environment.NewLine}" +
                            $"✅ Nenhuma alteração foi feita.",
                            MsBox.Avalonia.Enums.Icon.Warning
                        );
                    }
                }

                if (bancoSelecionado == "MySQL" || bancoSelecionado == "Ambos")
                {
                    // Verifique antes se as datas não são nulas
                    if (dataInicio is null || dataFim is null)
                    {
                        await MessageService.MostrarMensagem(GetWindow(), "Erro", "Selecione um período válido.");
                        return;
                    }

                    // Iniciar barra de progresso
                    pbProgresso.IsVisible = true;
                    pbProgresso.Minimum = 0;
                    pbProgresso.Maximum = tabelasParaExcluir.Count;
                    pbProgresso.Value = 0;

                    SetarStatusBotoes(false);

                    LogService.RegistrarLog(
                        "INFO",
                        $"🔄 Iniciando a exclusão dos movimentos das tabelas: {tabelasParaExcluir} para o banco de dados: {bancoSelecionado}."
                    );

                    var mensagem =
                        $"🔍 Resumo da Operação{Environment.NewLine}" +
                        $"📅 Período: {dataInicio:dd/MM/yyyy} a {dataFim:dd/MM/yyyy}{Environment.NewLine}" +
                        $"🗄 Banco de Dados: {bancoSelecionado}{Environment.NewLine}" +
                        $"📌 Categorias Selecionadas: {string.Join(", ", categoriasSelecionadas)}{Environment.NewLine}{Environment.NewLine}" +
                        $"⚠️ Esta ação excluirá os movimentos para o período selecionado.{Environment.NewLine}" +
                        $"❗ Deseja realmente continuar?";

                    var resposta = await MessageService.MostrarDialogConfirmacaoPersonalizado(this, "Confirmação de Reabertura", mensagem);

                    if (resposta == OpcaoConfirmacao.Sim)
                    {
                        // Executar a atualização das tabelas de forma assíncrona
                        

                            // Executa a exclusão no MySQL
                            foreach (string tabela in tabelasParaExcluir)
                            {
                                await Task.Run(() =>
                                {
                                    _mysqlService.ExcluirMovimentoMySQL(tabela, operadorSelecionado, dataInicio.Value, dataFim.Value);
                                });

                                // Atualiza a UI na thread correta
                                await Dispatcher.UIThread.InvokeAsync(() =>
                                {
                                    pbProgresso.Value = Math.Min(pbProgresso.Maximum, pbProgresso.Value + 1);
                                });
                            };

                        LogService.RegistrarLog(
                            "SUCCESS",
                            $"🚀 Finalizado a exclusão dos movimentos das tabelas: {tabelasParaExcluir} com sucesso!"
                        );
                    }
                    else
                    {
                        LogService.RegistrarLog(
                           "INFO",
                           $"⚠️ Operação cancelada: Exclusão do movimento não foi realizada para as categorias: {string.Join(", ", categoriasSelecionadas)}."
                        );

                        await MessageService.MostrarMensagem(
                            GetWindow(),
                            "Operação Cancelada",
                            $"As seguintes categorias não tiveram seus movimentos excluídos:{Environment.NewLine}" +
                            $"📌 {string.Join(", ", categoriasSelecionadas)}{Environment.NewLine}{Environment.NewLine}" +
                            $"✅ Nenhuma alteração foi feita.",
                            MsBox.Avalonia.Enums.Icon.Warning
                        );
                    }
                }

                SetarStatusBotoes(true);

                pbProgresso.IsVisible = false;

                await MessageService.MostrarDialogPersonalizado(window,
                    "Sucesso",
                    $"✅ Movimento excluído com sucesso!"
                );
            }
            catch (Exception ex)
            {
                LogService.RegistrarLog(
                    "ERRO",
                    $"❌ Erro ao excluir os movimentos: {ex.Message}"
                );

                await MessageService.MostrarDialogPersonalizado(window,
                    "ERRO",
                    $"❌ Erro ao excluir os movimentos: {ex.Message}"
                );
            }
        }

        private async void BtnAlterarNumeracao_Click(object? sender, RoutedEventArgs e) 
        {
            var window = this.GetVisualRoot() as Window;

            try
            {
                LogService.RegistrarLog("INFO", "🔄 Iniciando o processo de alterar a numeração dos documentos");

                // Pega os valores da empresa/documento
                var config = ConfigService.CarregarConfiguracaoEmpresa();
                int xEmpresa = int.TryParse(config.empresaAntiga, out var empresaAntiga) ? empresaAntiga : 0;
                int yEmpresa = int.TryParse(config.empresaNova, out var empresaNova) ? empresaNova : 0;

                if (xEmpresa <= 0 || yEmpresa <= 0)
                {
                    await MessageService.MostrarDialogPersonalizado(window,
                        "Aviso",
                        $"⚠️ Por favor, selecione números válidos para a numeração."
                    );

                    LogService.RegistrarLog("INFO", "⚠️ Numeração inválida informada.");
                    return;
                }

                LogService.RegistrarLog(
                    "INFO",
                    $"📌 Número da empresa selecionada: {xEmpresa}. Novo número: {yEmpresa}."
                );

                // Verifica quais tipos de dados foram marcados
                List<string> categoriasSelecionadas = new List<string>();
                if (cbEstoque.IsChecked is true) categoriasSelecionadas.Add("Estoque");
                if (cbFinanceiro.IsChecked is true) categoriasSelecionadas.Add("Financeiro");
                if (cbVendas.IsChecked is true) categoriasSelecionadas.Add("Vendas");

                if (categoriasSelecionadas.Count == 0)
                {
                    await MessageService.MostrarDialogPersonalizado(window,
                        "Aviso",
                        $"⚠️ Selecione ao menos um tipo de dados (Estoque, Financeiro, Vendas)."
                    );

                    LogService.RegistrarLog(
                        "INFO",
                        $"⚠️ Nenhuma categoria selecionada."
                    );

                    return;
                }

                // Obtém as tabelas específicas para cada categoria
                List<string> tabelasParaAlterar = ObterTabelasPorCategoria(categoriasSelecionadas);

                if (tabelasParaAlterar.Count == 0)
                {
                    await MessageService.MostrarDialogPersonalizado(window,
                        "Aviso",
                        $"⚠️ Nenhuma tabela foi selecionada para alteração dos documentos."
                    );

                    LogService.RegistrarLog("INFO", "⚠️ Nenhuma tabela foi selecionada para a alteração dos documentos.");
                    return;
                }

                // Verifica qual banco foi selecionado
                string bancoSelecionado = cbBanco.SelectedItem.ToString() ?? "";
                LogService.RegistrarLog("INFO", $"📤 Banco de dados selecionado: {bancoSelecionado}");

                if (bancoSelecionado == "Firebird" || bancoSelecionado == "Ambos")
                {
                    // Iniciar barra de progresso
                    pbProgresso.IsVisible = true;
                    pbProgresso.Minimum = 0;
                    pbProgresso.Maximum = tabelasParaAlterar.Count;
                    pbProgresso.Value = 0;

                    SetarStatusBotoes(false);

                    LogService.RegistrarLog(
                        "INFO",
                        $"🔄 Iniciando a alteração da numeração dos documentos das tabelas: {tabelasParaAlterar} para o banco de dados: {bancoSelecionado}."
                    );

                    var mensagem =
                        $"🔍 Resumo da Operação{Environment.NewLine}" +
                        $"🗄 Banco de Dados: {bancoSelecionado}{Environment.NewLine}" +
                        $"📌 Categorias Selecionadas: {string.Join(", ", categoriasSelecionadas)}{Environment.NewLine}{Environment.NewLine}" +
                        $"⚠️ Esta ação altera todos os documentos dos movimentos.{Environment.NewLine}" +
                        $"❗ Deseja realmente continuar?";

                    var resposta = await MessageService.MostrarDialogConfirmacaoPersonalizado(this, "Confirmação de Reabertura", mensagem);

                    if (resposta == OpcaoConfirmacao.Sim)
                    {
                        // Executar a atualização das tabelas de forma assíncrona
                        
                            foreach (string tabela in tabelasParaAlterar)
                            {
                                await Task.Run(() =>
                                {
                                    _firebirdService.AlterarDocumentoFirebird(tabela, xEmpresa, yEmpresa);
                                });

                                // Atualiza a UI na thread correta
                                await Dispatcher.UIThread.InvokeAsync(() =>
                                {
                                    pbProgresso.Value = Math.Min(pbProgresso.Maximum, pbProgresso.Value + 1);
                                });
                            };

                        LogService.RegistrarLog("SUCCESSO", $"🚀 Alteração concluída para as tabelas: {tabelasParaAlterar}.");
                    }
                }

                if (bancoSelecionado == "MySQL" || bancoSelecionado == "Ambos")
                {
                    // Iniciar barra de progresso
                    pbProgresso.IsVisible = true;
                    pbProgresso.Minimum = 0;
                    pbProgresso.Maximum = tabelasParaAlterar.Count;
                    pbProgresso.Value = 0;

                    SetarStatusBotoes(false);

                    LogService.RegistrarLog(
                        "INFO",
                        $"🔄 Iniciando a alteração da numeração dos documentos das tabelas: {tabelasParaAlterar} para o banco de dados: {bancoSelecionado}."
                    );

                    var mensagem =
                        $"🔍 Resumo da Operação{Environment.NewLine}" +
                        $"🗄 Banco de Dados: {bancoSelecionado}{Environment.NewLine}" +
                        $"📌 Categorias Selecionadas: {string.Join(", ", categoriasSelecionadas)}{Environment.NewLine}{Environment.NewLine}" +
                        $"⚠️ Esta ação altera todos os documentos dos movimentos.{Environment.NewLine}" +
                        $"❗ Deseja realmente continuar?";

                    var resposta = await MessageService.MostrarDialogConfirmacaoPersonalizado(this, "Confirmação de Reabertura", mensagem);

                    if (resposta == OpcaoConfirmacao.Sim)
                    {
                        foreach (string tabela in tabelasParaAlterar)
                        {
                            await Task.Run(() =>
                            {
                                _mysqlService.AlterarDocumentoMySQL(tabela, xEmpresa, yEmpresa);
                            });

                            // Atualiza a UI na thread correta
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                pbProgresso.Value = Math.Min(pbProgresso.Maximum, pbProgresso.Value + 1);
                            });
                        }
                        ;

                        LogService.RegistrarLog("SUCCESSO", $"🚀 Alteração concluída para as tabelas: {tabelasParaAlterar}.");
                    }   
                }

                SetarStatusBotoes(true);

                pbProgresso.IsVisible = false;

                await MessageService.MostrarDialogPersonalizado(window,
                    "Sucesso",
                    $"✅ Alteração concluída com sucesso!"
                );
            }
            catch (Exception ex)
            {
                LogService.RegistrarLog("ERROR", $"❌ Erro ao alterar numeração dos documentos: {ex.Message}");

                await MessageService.MostrarDialogPersonalizado(window,
                    "Erro",
                    $"❌ Erro ao alterar numeração dos documentos:{Environment.NewLine}{ex.Message}"
                );
            }
        }

    }
}