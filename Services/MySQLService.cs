using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using MySql.Data.MySqlClient;
using Avalonia.Controls;
using MsBox.Avalonia.Enums;

namespace BlackSync.Services
{
    public class MySQLService
    {
        private string connectionString;
        private string _banco;

        public MySQLService(string servidor, string banco, string usuario, string senha)
        {
            _banco = banco;
            connectionString = $"Server={servidor};Database={banco};User={usuario};Password={senha};";
        }

        public async Task<bool> TestarConexao(Window janela)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    return true;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool VerificarSeTabelaExiste(string tabela)
        {
            tabela = tabela.ToLower(); // 🔹 Converte para minúsculas para evitar erros

            string query = $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{_banco}' AND LOWER(TABLE_NAME) = '{tabela}'";

            using (var connection = new MySqlConnection(connectionString))
            using (var cmd = new MySqlCommand(query, connection))
            {
                connection.Open();
                int count = Convert.ToInt32(cmd.ExecuteScalar());
                return count > 0; // Se houver retorno, a tabela existe
            }
        }

        public async Task<List<string>> GetTabelasMySQL(Window janela)
        {
            List<string> tabelas = new List<string>();

            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SHOW TABLES";

                    using (var cmd = new MySqlCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tabelas.Add(reader.GetString(0).Trim().ToLower());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageService.MostrarMensagem(
                        janela,
                    $"Erro ao obter tabelas do MySQL: {ex.Message}",
                    "Erro",
                    Icon.Error
                );
            }

            return tabelas;
        }

        /// <summary>
        /// Obtém a estrutura da tabela no MySQL (nomes e tipos das colunas).
        /// </summary>
        public async Task<List<(string Nome, string Tipo)>> ObterEstruturaTabela(string tabela)
        {
            List<(string Nome, string Tipo)> estrutura = new List<(string Nome, string Tipo)>();

            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // 🔹 Força a verificação do banco de dados correto
                    string query = $@"
                        SELECT COLUMN_NAME, DATA_TYPE 
                        FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_SCHEMA = '{_banco}' 
                        AND TABLE_NAME COLLATE utf8_general_ci = '{tabela.ToLower()}'";

                    using (var cmd = new MySqlCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string nome = reader["COLUMN_NAME"].ToString().Trim().ToLower();
                            string tipo = reader["DATA_TYPE"].ToString().Trim();

                            estrutura.Add((nome, tipo));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await MessageService.MostrarDialogPersonalizado(
                    "Erro",
                    $"Erro ao obter estrutura da tabela {tabela} no MySQL: {ex.Message}"
                );
            }

            return estrutura;
        }

        public async void ExecutarScript(Window janela, string script)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    using (var cmd = new MySqlCommand(script, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageService.MostrarMensagem(
                        janela,
                    $"Erro ao executar script no MySQL: {ex.Message}",
                    "Erro",
                    Icon.Error
                );
            }
        }

        public async Task<bool> TabelaTemDados(Window janela, string tabela)
        {
            bool temDados = false;

            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = $"SELECT COUNT(*) FROM `{tabela.ToLower()}`";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        temDados = count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageService.MostrarMensagem(
                        janela,
                    $"Erro ao verificar dados na tabela {tabela}: {ex.Message}",
                    "Erro",
                    Icon.Error
                );
                return false; // Se der erro, assume que a tabela está vazia para evitar problemas.
            }

            return temDados;
        }

        public async void TruncateTabela(Window janela, string tabela)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = $"TRUNCATE TABLE `{tabela.ToLower()}`";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageService.MostrarMensagem(
                        janela,
                    $"Erro ao truncar a tabela {tabela}: {ex.Message}",
                    "Erro",
                    Icon.Error
                );
            }
        }

        public async Task<DataTable> ObterDadosTabela(Window janela, string tabela)
        {
            DataTable dt = new DataTable();

            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = $"SELECT * FROM \"{tabela}\"";

                    LogService.RegistrarLog(
                        "INFO",
                        $"🔍 Executando consulta: {query}"
                    );

                    using (var cmd = new MySqlCommand(query, conn))
                    using (var adapter = new MySqlDataAdapter(cmd))
                    {
                        adapter.Fill(dt);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageService.MostrarMensagem(
                        janela,
                    $"Erro ao obter dados da tabela {tabela} no Firebird: {ex.Message}",
                    "Erro",
                    Icon.Error
                );
            }

            return dt;
        }

        public async Task<List<string>> GetColunasTabelaFirebird(Window janela, string tabela)
        {
            List<string> colunas = new List<string>();

            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = $@"
                        SELECT RDB$FIELD_NAME FROM RDB$RELATION_FIELDS 
                        WHERE RDB$RELATION_NAME = '{tabela.ToUpper()}'";

                    using (var cmd = new MySqlCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            colunas.Add(reader.GetString(0).Trim());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageService.MostrarMensagem(
                        janela,
                    $"Erro ao obter colunas da tabela {tabela}: {ex.Message}",
                    "Erro",
                    Icon.Error
                );
            }

            return colunas;
        }

        public async Task<HashSet<string>> ObterCodigosExistentes(Window janela, string tabela, string colunaPK)
        {
            HashSet<string> codigosExistentes = new HashSet<string>();

            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = $"SELECT `{colunaPK}` FROM `{tabela.ToLower()}`";

                    using (var cmd = new MySqlCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            codigosExistentes.Add(reader[0].ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageService.MostrarMensagem(
                        janela,
                    $"Erro ao obter códigos existentes da tabela {tabela}: {ex.Message}",
                    "Erro",
                    Icon.Error
                );
            }

            return codigosExistentes;
        }

        public List<string> ObterColunasAutoIncrementPK(string tabela)
        {
            List<string> colunasIgnorar = new List<string>();

            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    string query = $@"
                        SELECT COLUMN_NAME 
                        FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_SCHEMA = '{_banco}' 
                          AND TABLE_NAME = '{tabela}' 
                          AND COLUMN_KEY = 'PRI' 
                          AND EXTRA LIKE '%auto_increment%'";

                    using (var cmd = new MySqlCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            colunasIgnorar.Add(reader.GetString(0).Trim().ToLower());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.RegistrarLog("ERROR", $"❌ Erro ao buscar colunas AUTO_INCREMENT na tabela {tabela}: {ex.Message}");
            }

            return colunasIgnorar;
        }

        public async Task InserirDadosTabela(Window janela, string tabela, DataTable dados)
        {
            try
            {
                if (dados.Rows.Count == 0)
                {
                    LogService.RegistrarLog(
                        "INFO",
                        $"⚠️ Nenhum dado para inserir na tabela {tabela}."
                    );

                    return;
                }

                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    LogService.RegistrarLog(
                        "INFO",
                        $"✅ Iniciando inserção de {dados.Rows.Count} registros na tabela {tabela} no MySQL."
                    );

                    string tabelaMySQL = tabela.ToLower();
                    var estruturaTabela = await ObterEstruturaTabela(tabelaMySQL);

                    var colunasIgnorar = ObterColunasAutoIncrementPK(tabelaMySQL);
                    LogService.RegistrarLog("INFO", $"🔎 Ignorando colunas AUTO_INCREMENT/PK na tabela {tabela}: {string.Join(", ", colunasIgnorar)}");

                    var colunasMySQL = estruturaTabela.Select(c => c.Nome.ToLower()).ToList();
                    var colunasFirebird = dados.Columns.Cast<DataColumn>().Select(c => c.ColumnName.ToLower()).ToList();

                    /*var colunasValidas = colunasFirebird.Intersect(colunasMySQL).ToList()*/
                    ;
                    var colunasValidas = colunasFirebird
                        .Intersect(colunasMySQL)
                        .Except(colunasIgnorar)
                        .ToList();
                    var colunasExtrasMySQL = colunasMySQL.Except(colunasFirebird).ToList();

                    if (colunasValidas.Count == 0)
                    {
                        LogService.RegistrarLog("ERROR", $"❌ Nenhuma coluna compatível entre Firebird e MySQL na tabela {tabela}.");
                        return;
                    }

                    List<string> batchInserts = new List<string>();
                    int batchSize = 1000;
                    int totalInseridos = 0;
                    int linhaAtual = 0;

                    foreach (DataRow row in dados.Rows)
                    {
                        List<string> values = new List<string>();
                        linhaAtual++;

                        foreach (var coluna in colunasValidas)
                        {
                            string valor = row[coluna]?.ToString().Trim() ?? "NULL";

                            // 🔹 Verifica se a coluna é DECIMAL(15,4) ou DECIMAL(15,2)

                            var colunaEncontrada = estruturaTabela.FirstOrDefault(c => c.Nome.ToLower() == coluna);
                            var tipoColuna = colunaEncontrada != default ? colunaEncontrada.Tipo : null;

                            bool isDecimal = tipoColuna != null && (tipoColuna.Contains("decimal") || tipoColuna.Contains("float") || tipoColuna.Contains("numeric"));

                            if (isDecimal)
                            {
                                if (string.IsNullOrEmpty(valor) || valor.ToLower() == "null")
                                {
                                    valor = "0";
                                }
                                else
                                {
                                    // 🔹 Substituir vírgulas por pontos para evitar erro de conversão
                                    valor = valor.Replace(",", ".");

                                    if (decimal.TryParse(valor, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal numero))
                                    {
                                        // 🔹 Determina a precisão baseada no DECIMAL(15,4) ou DECIMAL(15,2)
                                        int casasDecimais = tipoColuna.Contains("15,4") ? 4 : 2;
                                        decimal maxValor = casasDecimais == 4 ? 999999999999.9999m : 999999999999.99m;

                                        if (numero > maxValor || numero < -maxValor)
                                        {
                                            LogService.RegistrarLog("ERROR", $"🔴 ERRO na linha {linhaAtual}: Valor '{numero}' excede o limite para a coluna '{coluna}'. Ajustado para {maxValor}.");
                                            numero = maxValor;
                                        }

                                        valor = Math.Round(numero, casasDecimais).ToString(CultureInfo.InvariantCulture);
                                    }
                                    else
                                    {
                                        LogService.RegistrarLog("ERROR", $"⚠️ ERRO na linha {linhaAtual}: Não foi possível converter '{valor}' na coluna '{coluna}'. Substituído por 0.");
                                        valor = "0";
                                    }
                                }
                            }
                            else if (tipoColuna != null && (tipoColuna.Contains("date") || tipoColuna.Contains("datetime")))
                            {
                                valor = DateTime.TryParse(valor, out DateTime dataConvertida) ? $"'{dataConvertida:yyyy-MM-dd}'" : "NULL";
                            }
                            else
                            {
                                valor = string.IsNullOrEmpty(valor) || valor.ToLower() == "null" ? "NULL" : $"'{valor.Replace("'", "''")}'";
                            }

                            values.Add(valor);
                        }

                        foreach (var colunaExtra in colunasExtrasMySQL)
                        {
                            values.Add("NULL");
                        }

                        batchInserts.Add($"({string.Join(", ", values)})");
                        totalInseridos++;

                        if (batchInserts.Count >= batchSize)
                        {
                            InserirLoteNoBanco(tabelaMySQL, colunasValidas, colunasExtrasMySQL, batchInserts, conn);
                            batchInserts.Clear();
                        }
                    }

                    if (batchInserts.Count > 0)
                    {
                        InserirLoteNoBanco(tabelaMySQL, colunasValidas, colunasExtrasMySQL, batchInserts, conn);
                    }

                    LogService.RegistrarLog("SUCCESS", $"🎉 Total de {totalInseridos} registros inseridos na tabela {tabela}.");
                }
            }
            catch (Exception ex)
            {
                LogService.RegistrarLog("ERROR", $"❌ Erro ao inserir dados na tabela {tabela}: {ex.Message}");
            }
        }

        private void InserirLoteNoBanco(string tabela, List<string> colunasValidas, List<string> colunasExtrasMySQL, List<string> batchInserts, MySqlConnection conn)
        {
            try
            {
                // 🔹 Primeiro, tenta inserir todo o lote de uma vez
                string query = $"INSERT INTO `{tabela}` ({string.Join(", ", colunasValidas.Concat(colunasExtrasMySQL))}) VALUES {string.Join(", ", batchInserts)};";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                LogService.RegistrarLog("ERROR", $"❌ Erro ao inserir lote na tabela {tabela}: {ex.Message}");
                LogService.RegistrarLog("ERROR", $"📌 Tentativa de lote com erro: {batchInserts.Count} registros.");

                // 🔹 Se falhar, insere cada linha individualmente para capturar o erro exato
                foreach (string values in batchInserts)
                {
                    string query = $"INSERT INTO `{tabela}` ({string.Join(", ", colunasValidas.Concat(colunasExtrasMySQL))}) VALUES {values};";

                    try
                    {
                        using (var cmd = new MySqlCommand(query, conn))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch (Exception exLinha)
                    {
                        LogService.RegistrarLog("ERROR", $"❌ Erro ao inserir na tabela {tabela}: {exLinha.Message}");
                        LogService.RegistrarLog("ERROR", $"📌 Query com erro: {query}");
                    }
                }
            }
        }

        public async Task<bool> ColunaExiste(Window janela, string tabela, string coluna)
        {
            bool existe = false;

            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = $@"
                        SELECT COUNT(*) 
                        FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_SCHEMA = '{_banco}' 
                        AND TABLE_NAME = '{tabela}' 
                        AND COLUMN_NAME = '{coluna}'";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        existe = count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageService.MostrarMensagem(
                        janela, $"Erro ao verificar coluna '{coluna}' na tabela {tabela} no MySQL: {ex.Message}", "Erro", Icon.Error);
            }

            return existe;
        }

        public async Task AtualizarEnviado(Window janela, string tabela)
        {
            try
            {
                if (!await ColunaExiste(janela, tabela, "Enviado"))
                {
                    LogService.RegistrarLog(
                        "INFO",
                        $"⚠️ A coluna 'Enviado' não existe na tabela {tabela}. Nenhuma atualização foi feita."
                    );

                    return;
                }

                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = $"UPDATE `{tabela}` SET enviado = 1 WHERE enviado = 0";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        int registrosAtualizados = cmd.ExecuteNonQuery();

                        LogService.RegistrarLog(
                            "SUCCESS",
                            $"✅ {registrosAtualizados} registros foram marcados como 'Enviado' na tabela {tabela} do MySQL."
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                MessageService.MostrarMensagem(
                        janela,
                    $"Erro ao atualizar coluna 'Enviado' na tabela {tabela} no MySQL: {ex.Message}",
                    "Erro",
                    Icon.Error
                );
            }
        }

        public void ReabrirMovimentoMySQL(string tabela, string operador, DateTime dataInicio, DateTime? dataFim = null)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    var config = ConfigService.CarregarConfiguracaoEmpresa();
                    string filial = config.filial;

                    conn.Open();

                    string colunaData = tabela switch
                    {
                        "baixapagar" => "dtlanca",
                        "baixareceber" => "dtlanca",
                        "contacartao" => "datavenda",
                        "pagar" => "dtlanca",
                        "receber" => "dtlanca",
                        "abrecaixa" => "dataabre",
                        "caixa" => "data",
                        "notafiscal" => "dtemissao",
                        "pedidosvenda" => "data",
                        "movestoque" => "data",
                        "nfentrada" => "dtlanca",
                        _ => null
                    };

                    string query;

                    // Tratamento para tabelas que usam IN (SELECT ...)
                    if (tabela == "itensnf")
                    {
                        query = operador == "between"
                            ? $@"UPDATE {tabela} SET enviado = '0' WHERE nf IN (
                              SELECT nf FROM notafiscal WHERE dtemissao BETWEEN @DataInicio AND @DataFim and filial = ?)"
                                            : $@"UPDATE {tabela} SET enviado = '0' WHERE nf IN (
                              SELECT nf FROM notafiscal WHERE dtemissao {operador} @DataInicio and filial = ?)";
                    }
                    else if (tabela == "itenspedidovenda")
                    {
                        query = operador == "between"
                            ? $@"UPDATE {tabela} SET enviado = '0' WHERE documento IN (
                              SELECT documento FROM pedidosvenda WHERE data BETWEEN ? AND ? and filial = ?)"
                                            : $@"UPDATE {tabela} SET enviado = '0' WHERE documento IN (
                              SELECT documento FROM pedidosvenda WHERE data {operador} @DataInicio and filial = ?)";
                    }
                    else if (tabela == "itemnfentrada")
                    {
                        query = operador == "between"
                            ? $@"UPDATE {tabela} SET enviado = '0' WHERE documento IN (
                              SELECT documento FROM nfentrada WHERE dtlanca BETWEEN @DataInicio AND @DataFim and filial = ?)"
                                            : $@"UPDATE {tabela} SET enviado = '0' WHERE documento IN (
                              SELECT documento FROM nfentrada WHERE dtlanca {operador} @DataInicio and filial = ?)";
                    }
                    else if (colunaData != null)
                    {
                        query = operador == "between"
                            ? $@"UPDATE {tabela} SET enviado = '0' WHERE {colunaData} BETWEEN ? AND ? and filial = ?"
                            : $@"UPDATE {tabela} SET enviado = '0' WHERE {colunaData} {operador} @DataInicio and filial = ?";
                    }
                    else
                    {
                        LogService.RegistrarLog("ERROR", $"⚠️ Tabela {tabela} não possui uma coluna de data válida definida.");

                        return;
                    }

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@DataInicio", dataInicio.Date);
                        if (operador == "between" && dataFim.HasValue)
                            cmd.Parameters.AddWithValue("@DataFim", dataFim.Value.Date);
                        cmd.Parameters.AddWithValue("?", filial);
                        int linhasAfetadas = cmd.ExecuteNonQuery();

                        LogService.RegistrarLog(
                            "INFO",
                            $"🔄 {linhasAfetadas} registros reabertos na tabela {tabela} (MySQL)."
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.RegistrarLog(
                    "ERROR",
                    $"❌ Erro ao reabrir movimento na tabela {tabela} (MySQL): {ex.Message}"
                );
            }
        }

        public void FecharMovimentoMySQL(string tabela, string operador, DateTime dataInicio, DateTime? dataFim = null)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    var config = ConfigService.CarregarConfiguracaoEmpresa();
                    string filial = config.filial;

                    conn.Open();

                    string colunaData = tabela switch
                    {
                        "baixapagar" => "dtlanca",
                        "baixareceber" => "dtlanca",
                        "contacartao" => "datavenda",
                        "pagar" => "dtlanca",
                        "receber" => "dtlanca",
                        "abrecaixa" => "dataabre",
                        "caixa" => "data",
                        "notafiscal" => "dtemissao",
                        "pedidosvenda" => "data",
                        "movestoque" => "data",
                        "nfentrada" => "dtlanca",
                        _ => null
                    };

                    string query;

                    // Tratamento para tabelas que usam IN (SELECT ...)
                    if (tabela == "itensnf")
                    {
                        query = operador == "between"
                            ? $@"UPDATE {tabela} SET enviado = '1' WHERE nf IN (
                              SELECT nf FROM notafiscal WHERE dtemissao BETWEEN @DataInicio AND @DataFim and filial = ?)"
                                                                : $@"UPDATE {tabela} SET enviado = '1' WHERE nf IN (
                              SELECT nf FROM notafiscal WHERE dtemissao {operador} @DataInicio and filial = ?)";
                    }
                    else if (tabela == "itenspedidovenda")
                    {
                        query = operador == "between"
                            ? $@"UPDATE {tabela} SET enviado = '1' WHERE documento IN (
                              SELECT documento FROM pedidosvenda WHERE data BETWEEN ? AND ? and filial = ?)"
                                                                : $@"UPDATE {tabela} SET enviado = '1' WHERE documento IN (
                              SELECT documento FROM pedidosvenda WHERE data {operador} @DataInicio and filial = ?)";
                    }
                    else if (tabela == "itemnfentrada")
                    {
                        query = operador == "between"
                            ? $@"UPDATE {tabela} SET enviado = '1' WHERE documento IN (
                              SELECT documento FROM nfentrada WHERE dtlanca BETWEEN @DataInicio AND @DataFim and filial = ?)"
                                                                : $@"UPDATE {tabela} SET enviado = '1' WHERE documento IN (
                              SELECT documento FROM nfentrada WHERE dtlanca {operador} @DataInicio and filial = ?)";
                    }
                    else if (colunaData != null)
                    {
                        query = operador == "between"
                            ? $@"UPDATE {tabela} SET enviado = '1' WHERE {colunaData} BETWEEN ? AND ? and filial = ?"
                            : $@"UPDATE {tabela} SET enviado = '1' WHERE {colunaData} {operador} @DataInicio and filial = ?";
                    }
                    else
                    {
                        LogService.RegistrarLog("ERROR", $"⚠️ Tabela {tabela} não possui uma coluna de data válida definida.");

                        return;
                    }

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@DataInicio", dataInicio.Date);
                        if (operador == "between" && dataFim.HasValue)
                            cmd.Parameters.AddWithValue("@DataFim", dataFim.Value.Date);
                        cmd.Parameters.AddWithValue("?", filial);
                        int linhasAfetadas = cmd.ExecuteNonQuery();

                        LogService.RegistrarLog(
                            "INFO",
                            $"🔄 {linhasAfetadas} registros reabertos na tabela {tabela} (MySQL)."
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.RegistrarLog(
                    "ERROR",
                    $"❌ Erro ao reabrir movimento na tabela {tabela} (MySQL): {ex.Message}"
                );
            }
        }

        public void ExcluirMovimentoMySQL(string tabela, string operador, DateTime dataInicio, DateTime? dataFim = null)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    var config = ConfigService.CarregarConfiguracaoEmpresa();
                    string filial = config.filial;

                    conn.Open();

                    string colunaData = tabela switch
                    {
                        "baixapagar" => "dtlanca",
                        "baixareceber" => "dtlanca",
                        "contacartao" => "datavenda",
                        "pagar" => "dtlanca",
                        "receber" => "dtlanca",
                        "abrecaixa" => "dataabre",
                        "caixa" => "data",
                        "notafiscal" => "dtemissao",
                        "pedidosvenda" => "data",
                        "movestoque" => "data",
                        "nfentrada" => "dtlanca",
                        _ => null
                    };

                    string query;

                    // Tratamento para tabelas que usam IN (SELECT ...)
                    if (tabela == "itensnf")
                    {
                        query = operador == "between"
                            ? $@"DELETE from {tabela}
                                WHERE nf IN (SELECT nf FROM notafiscal WHERE dtemissao BETWEEN @DataInicio AND @DataFim and filial = ?)"
                                                : $@"DELETE from {tabela}
                                WHERE nf IN (SELECT nf FROM notafiscal WHERE dtemissao {operador} ? and filial = ?)";
                    }
                    else if (tabela == "itenspedidovenda")
                    {
                        query = operador == "between"
                            ? $@"DELETE from {tabela}
                                WHERE documento IN (SELECT documento FROM pedidosvenda WHERE data BETWEEN @DataInicio AND @DataFim and filial = ?)"
                                                : $@"DELETE from {tabela}
                                WHERE documento IN (SELECT documento FROM pedidosvenda WHERE data {operador} ? and filial = ?)";
                    }
                    else if (tabela == "itemnfentrada")
                    {
                        query = operador == "between"
                            ? $@"DELETE from {tabela}
                                WHERE documento IN (SELECT documento FROM nfentrada WHERE dtlanca BETWEEN @DataInicio AND @DataFim and filial = ?)"
                                                : $@"DELETE from {tabela}
                                WHERE documento IN (SELECT documento FROM nfentrada WHERE dtlanca {operador} ? and filial = ?)";
                    }
                    else if (colunaData != null)
                    {
                        query = operador == "between"
                            ? $@"DELETE from {tabela} WHERE {colunaData} BETWEEN @DataInicio AND @DataFim and filial = ?"
                            : $@"DELETE from {tabela} WHERE {colunaData} {operador} ? and filial = ?";
                    }
                    else
                    {
                        LogService.RegistrarLog(
                            "ERROR",
                            $"⚠️ Tabela {tabela} não possui uma coluna de data válida definida."
                        );

                        return;
                    }

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@DataInicio", dataInicio.Date);
                        if (operador == "between" && dataFim.HasValue)
                            cmd.Parameters.AddWithValue("@DataFim", dataFim.Value.Date);
                        cmd.Parameters.AddWithValue("?", filial);
                        int linhasAfetadas = cmd.ExecuteNonQuery();

                        LogService.RegistrarLog(
                            "INFO",
                            $"🔄 {linhasAfetadas} registros excluídos na tabela {tabela} (MySQL)."
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.RegistrarLog(
                    "ERROR",
                    $"❌ Erro ao excluir os movimento na tabela {tabela} (MySQL): {ex.Message}"
                );
            }
        }

        public void AlterarDocumentoMySQL(string tabela, int xEmpresa, int yEmpresa)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    Dictionary<string, string> colunasDocumento = new Dictionary<string, string>
                    {
                        { "baixapagar", "documento" },
                        { "baixareceber", "documento" },
                        { "contacartao", "documento" },
                        { "pagar", "documento" },
                        { "receber", "documento" },
                        { "caixa", "pedido" },
                        { "notafiscal", "pedido" },
                        { "pedidosvenda", "documento" },
                        { "itenspedidovenda", "documento" },
                        { "movestoque", "documento" },
                        { "nfentrada", "documento" },
                        { "itemnfentrada", "documento" }
                    };

                    if (!colunasDocumento.ContainsKey(tabela))
                    {
                        LogService.RegistrarLog("ERROR", $"⚠️ Tabela {tabela} não possui uma coluna de documento definida.");
                        return;
                    }

                    string colunaDocumento = colunasDocumento[tabela];

                    string query = $@"
                        UPDATE {tabela}
                        SET {colunaDocumento} = REPLACE(REPLACE({colunaDocumento}, 'P{xEmpresa}', 'P{yEmpresa}'), 'F{xEmpresa}', 'F{yEmpresa}')
                        WHERE {colunaDocumento} LIKE 'P{xEmpresa}%' OR {colunaDocumento} LIKE 'F{xEmpresa}%'";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        int linhasAfetadas = cmd.ExecuteNonQuery();
                        LogService.RegistrarLog(
                            "INFO",
                            $"✅ {linhasAfetadas} registros alterados na tabela {tabela} (MySQL)."
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.RegistrarLog("ERROR", $"❌ Erro ao alterar a numeração do documento na tabela {tabela} (MySQL): {ex.Message}");
            }
        }

    }
}
