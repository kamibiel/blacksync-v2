using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

public static class LogService
{
    private static readonly string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");

    /// <summary>
    /// Gera o caminho do arquivo de log com base na data do dia.
    /// </summary>
    private static string ObterCaminhoArquivoLog()
    {
        string nomeArquivo = $"{DateTime.Now:yyyy-MM-dd}.json";
        return Path.Combine(logDirectory, nomeArquivo);
    }

    /// <summary>
    /// Verifica se a pasta de logs existe e cria se necessário.
    /// </summary>
    private static void VerificarCriarPastaLogs()
    {
        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }
    }

    /// <summary>
    /// Adiciona uma nova entrada de log ao arquivo do dia.
    /// </summary>
    public static void RegistrarLog(string tipo, string mensagem)
    {
        VerificarCriarPastaLogs();
        string caminhoArquivo = ObterCaminhoArquivoLog();

        List<LogEntry> logs = new List<LogEntry>();

        if (File.Exists(caminhoArquivo))
        {
            string jsonExistente = File.ReadAllText(caminhoArquivo);
            logs = JsonConvert.DeserializeObject<List<LogEntry>>(jsonExistente) ?? new List<LogEntry>();
        }

        logs.Add(new LogEntry
        {
            DataHora = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            Tipo = tipo,
            Mensagem = mensagem
        });

        string novoJson = JsonConvert.SerializeObject(logs, Formatting.Indented);
        File.WriteAllText(caminhoArquivo, novoJson);
    }

    /// <summary>
    /// Lê os logs do dia atual e retorna uma lista formatada.
    /// </summary>
    public static List<LogEntry> ObterLogsDoDia()
    {
        string caminhoArquivo = ObterCaminhoArquivoLog();

        if (!File.Exists(caminhoArquivo))
        {
            return new List<LogEntry>();
        }

        string json = File.ReadAllText(caminhoArquivo);
        return JsonConvert.DeserializeObject<List<LogEntry>>(json) ?? new List<LogEntry>();
    }
}

/// <summary>
/// Estrutura de um log.
/// </summary>
public class LogEntry
{
    public string DataHora { get; set; }
    public string Tipo { get; set; }
    public string Mensagem { get; set; }
}
