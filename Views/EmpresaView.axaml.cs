﻿using Avalonia.Controls;
using Avalonia.Interactivity;
using BlackSync.Services;
using MsBox.Avalonia.Enums;
using System;

namespace BlackSync.Views
{
    public partial class EmpresaView : UserControl
    {
        public EmpresaView()
        {
            InitializeComponent();
            this.FindControl<Button>("btnLimpar").Click += BtnLimpar_Click;
            this.FindControl<Button>("btnSalvar").Click += BtnSalvar_Click;
        }

        public void CarregarConfiguracoesSalvas()
        {
            var config = ConfigService.CarregarConfiguracaoEmpresa();
            
            txtEmpresaAntiga.Text = config.empresaAntiga;
            txtEmpresaNova.Text = config.empresaNova;
            txtFilial.Text = config.filial;

        }

        private async void BtnLimpar_Click(object? sender, RoutedEventArgs e)
        {
            txtEmpresaAntiga.Text = string.Empty;
            txtEmpresaNova.Text = string.Empty;
            txtFilial.Text = string.Empty;
        }

        private async void BtnSalvar_Click(object? sender, RoutedEventArgs e)
        {
            var janela = (Window)this.VisualRoot;

            // Validação dos campos obrigatórios
            if (string.IsNullOrWhiteSpace(txtEmpresaAntiga.Text) ||
                string.IsNullOrWhiteSpace(txtEmpresaNova.Text) ||
                string.IsNullOrWhiteSpace(txtFilial.Text))
            {
                await MessageService.MostrarMensagem(janela,
                    "Campos obrigatórios",
                    "Por favor, preencha todos os campos antes de salvar a configuração.",
                    Icon.Warning);

                return;
            }

            try
            {
                await ConfigService.SalvarConfiguracaoEmpresa(
                    janela,
                    txtEmpresaAntiga.Text,
                    txtEmpresaNova.Text,
                    txtFilial.Text);

                await MessageService.MostrarMensagem(janela, "Sucesso", "Configurações salvas com sucesso!", Icon.Info);
                LogService.RegistrarLog("INFO", "✅ Configurações salvas com sucesso!");
            }
            catch (Exception ex)
            {
                await MessageService.MostrarMensagem(janela, "Erro", $"Erro ao salvar configurações: {ex.Message}", Icon.Error);
                LogService.RegistrarLog("ERROR", $"❌ Erro ao salvar configurações: {ex.Message}.");
            }
        }
    }
}

