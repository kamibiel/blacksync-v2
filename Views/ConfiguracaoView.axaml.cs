using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using BlackSync.Services;
using System;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia;

namespace BlackSync.Views
{

    public partial class ConfiguracaoView : UserControl
    {

        public ConfiguracaoView()
        {
            InitializeComponent();
            CarregarConfiguracoesSalvas();
        }

        public void CarregarConfiguracoesSalvas()
        {
            var mysql = ConfigService.CarregarConfiguracaoMySQL();
            var firebird = ConfigService.CarregarConfiguracaoFirebird();

            txtServidor.Text = mysql.servidor;
            txtPorta.Text = mysql.porta;
            txtBanco.Text = mysql.banco;
            txtUsuario.Text = mysql.usuario;
            txtSenha.Text = mysql.senha;
            txtDSN.Text = firebird;
        }

        private async void BtnTestarConexao_Click(object? sender, RoutedEventArgs e)
        {
            var janela = (Window)this.VisualRoot;

            // Validação básica dos campos
            if (string.IsNullOrWhiteSpace(txtServidor.Text) ||
                string.IsNullOrWhiteSpace(txtPorta.Text) ||
                string.IsNullOrWhiteSpace(txtBanco.Text) ||
                string.IsNullOrWhiteSpace(txtUsuario.Text) ||
                string.IsNullOrWhiteSpace(txtSenha.Text) ||
                string.IsNullOrWhiteSpace(txtDSN.Text))
            {
                await MessageService.MostrarMensagem(janela,
                    "Campos obrigatórios",
                    "Por favor, preencha todos os campos antes de testar a conexão.",
                    Icon.Warning);

                return;
            }

            bool mysqlOk = false;
            bool firebirdOk = false;

            try
            {
                var mysqlService = new MySQLService(
                    txtServidor.Text,
                    txtPorta.Text,
                    txtBanco.Text,
                    txtUsuario.Text,
                    txtSenha.Text
                );

                mysqlOk = await mysqlService.TestarConexao(janela);
            }
            catch
            {
                /* Silencia erro individual */
            }

            try
            {
                var firebirdService = new FirebirdService(txtDSN.Text);
                firebirdOk = await firebirdService.TestarConexao(janela);
            }
            catch
            {
                /* Silencia erro individual */
            }

            if (mysqlOk && firebirdOk)
            {
                await MessageService.MostrarMensagem(janela, "Conexão", "✅ Conexão com ambos os bancos realizada com sucesso!", Icon.Info);
                LogService.RegistrarLog("INFO", "✅ Conexão com ambos os bancos realizada com sucesso!");
            }
            else
            {
                await MessageService.MostrarMensagem(janela, "Erro de Conexão", "❌ Não foi possível conectar aos bancos. Verifique as credenciais e tente novamente.", Icon.Error);
                LogService.RegistrarLog("ERROR", "❌ Não foi possível conectar aos bancos. Verifique as credenciais e tente novamente.");
            }
        }

        private async void BtnSalvar_Click(object? sender, RoutedEventArgs e)
        {
            var janela = (Window)this.VisualRoot;

            // Validação dos campos obrigatórios
            if (string.IsNullOrWhiteSpace(txtServidor.Text) ||
                string.IsNullOrWhiteSpace(txtPorta.Text) ||
                string.IsNullOrWhiteSpace(txtBanco.Text) ||
                string.IsNullOrWhiteSpace(txtUsuario.Text) ||
                string.IsNullOrWhiteSpace(txtSenha.Text) ||
                string.IsNullOrWhiteSpace(txtDSN.Text))
            {
                await MessageService.MostrarMensagem(janela,
                    "Campos obrigatórios",
                    "Por favor, preencha todos os campos antes de salvar a configuração.",
                    Icon.Warning);

                return;
            }

            try
            {
                await ConfigService.SalvarConfiguracaoMySQL(
                janela,
                txtServidor.Text,
                txtPorta.Text,
                txtBanco.Text,
                txtUsuario.Text,
                txtSenha.Text);

                await ConfigService.SalvarConfiguracaoFirebird(
                    janela,
                    txtDSN.Text);

                // Exemplo de alerta ao usuário
                await MessageService.MostrarMensagem(janela, "Sucesso", "Configurações salvas com sucesso!", Icon.Info);
                LogService.RegistrarLog("INFO", "✅ Configurações salvas com sucesso!");
            }

            catch (Exception ex)
            {
                await MessageService.MostrarMensagem(janela, "Erro", $"Erro ao salvar configurações: {ex.Message}", Icon.Error);
                LogService.RegistrarLog("ERROR", $"❌ Erro ao salvar configurações: {ex.Message}.");
            }

        }

        private async void BtnLimpar_Click(object? sender, RoutedEventArgs e)
        {
            txtServidor.Text = string.Empty;
            txtPorta.Text = string.Empty;
            txtBanco.Text = string.Empty;
            txtUsuario.Text = string.Empty;
            txtSenha.Text = string.Empty;
            txtDSN.Text = string.Empty;
        }

    }

}