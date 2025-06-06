using Avalonia.Controls;
using FirebirdSql.Data.FirebirdClient;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using MsBox.Avalonia.Enums;
using BlackSync.Services;

namespace BlackSync.Services
{
    public class FirebirdService
    {
        private string connectionString;

        public FirebirdService(string dsn)
        {
            this.connectionString = $"DSN={dsn};";
        }

        public async Task<bool> TestarConexao(Window janela)
        {
            try
            {
                using (var conn = new OdbcConnection(connectionString))
                {
                    conn.Open();

                    // Executa uma consulta simples para validar a conexão
                    using (var cmd = new OdbcCommand("SELECT 1 FROM RDB$DATABASE", conn))
                    {
                        cmd.ExecuteScalar();
                    }

                    //await MessageService.MostrarMensagem(janela, "Conexão com Firebird via ODBC bem-sucedida!", "Sucesso", Icon.Success);
                    return true;
                }
            }
            catch (OdbcException ex)
            {
                //await MessageService.MostrarMensagem(janela, $"Erro ao conectar ao Firebird via ODBC: {Environment.NewLine}{ex.Message}", "Erro", Icon.Error);
                return false;
            }
            catch (Exception ex)
            {
                //await MessageService.MostrarMensagem(janela, $"Erro inesperado:{Environment.NewLine}{ex.Message}", "Erro", Icon.Error);
                return false;
            }
        }

        public async Task<List<string>> GetTabelasFirebird(Window janela)
        {
            List<string> tabelas = new List<string>();

            try
            {
                using (var conn = new OdbcConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT UPPER(RDB$RELATION_NAME) FROM RDB$RELATIONS WHERE RDB$VIEW_BLR IS NULL ORDER BY RDB$RELATION_NAME";

                    using (var cmd = new OdbcCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tabelas.Add(reader.GetString(0).Trim());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await MessageService.MostrarMensagem(janela, $"Erro ao obter tabelas do Firebird: {ex.Message}", "Erro", Icon.Error);
            }

            return tabelas;
        }

        /// <summary>
        /// Converte os códigos dos tipos do Firebird para nomes reais
        /// </summary>
        private string ConverterTipoFirebird(int tipo, int tamanho)
        {
            Dictionary<int, string> tiposFirebird = new Dictionary<int, string>
            {
                { 7, "SMALLINT" },
                { 8, "INT" },
                { 16, "BIGINT" }, // 🔹 Firebird usa 16 para BIGINT, mas MySQL pode tratar como DECIMAL
                { 10, "FLOAT" },
                { 27, "DOUBLE" },
                { 37, $"VARCHAR({tamanho})" },
                { 14, $"CHAR({tamanho})" },
                { 12, "DATE" },
                { 13, "TIME" },
                { 35, "DATETIME" },
                { 261, "LONGTEXT" }
            };

            // 🔹 Se for um tipo desconhecido, assume DECIMAL(15,2) por segurança
            return tiposFirebird.ContainsKey(tipo) ? tiposFirebird[tipo] : "DECIMAL(15,2)";
        }

        /// <summary>
        /// Obtém a estrutura da tabela no Firebird (nomes e tipos das colunas).
        /// Agora retorna uma lista de objetos (Nome, Tipo) para ser compatível com CompararEstrutura().
        /// </summary>
        public async Task<List<(string Nome, string Tipo)>> ObterEstruturaTabela(string tabela)
        {
            List<(string Nome, string Tipo)> estrutura = new List<(string Nome, string Tipo)>();

            try
            {
                using (var conn = new OdbcConnection(connectionString))
                {
                    conn.Open();
                    string query = $@"
                    SELECT 
                        UPPER(rf.RDB$FIELD_NAME) AS COLUNA, 
                        f.RDB$FIELD_TYPE AS TIPO,
                        COALESCE(f.RDB$FIELD_LENGTH, 0) AS TAMANHO
                    FROM RDB$RELATION_FIELDS rf
                    JOIN RDB$FIELDS f ON rf.RDB$FIELD_SOURCE = f.RDB$FIELD_NAME
                    WHERE rf.RDB$RELATION_NAME = UPPER('{tabela}')";

                    using (var cmd = new OdbcCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string coluna = reader["COLUNA"].ToString().Trim();
                            int tipo = Convert.ToInt32(reader["TIPO"]);
                            int tamanho = Convert.ToInt32(reader["TAMANHO"]);

                            string tipoConvertido = ConverterTipoFirebird(tipo, tamanho);

                            estrutura.Add((coluna, tipoConvertido));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await MessageService.MostrarDialogPersonalizado(
                    "Erro",
                    $"Erro ao obter estrutura da tabela {tabela} no Firebird: {ex.Message}"
                );
            }

            return estrutura;
        }

        /// <summary>
        /// Método que compara a estrutura das tabelas do Firebird e MySQL
        /// </summary>
        public async Task<List<(string Nome, string Tipo)>> CompararEstrutura(string tabela, MySQLService mySQLService)
        {
            var estruturaFirebird = await ObterEstruturaTabela(tabela.ToUpper());
            var estruturaMySQL = await mySQLService.ObterEstruturaTabela(tabela.ToLower()); 

            var colunasMySQL = estruturaMySQL.Select(e => e.Nome.ToUpper()).ToList();

            // Filtrar colunas que existem no Firebird, mas não no MySQL
            var colunasFaltantes = estruturaFirebird.Where(e => !colunasMySQL.Contains(e.Nome.ToUpper())).ToList();

            return colunasFaltantes;
        }

        public async Task<List<DataTable>> ObterDadosTabelaEmLotes(string tabela, int tamanhoLote = 5000)
        {
            List<DataTable> lotes = new List<DataTable>();

            try
            {
                using (var conn = new OdbcConnection(connectionString))
                {
                    conn.Open();
                    int offset = 0;
                    bool hasMoreData = true;

                    while (hasMoreData)
                    {
                        // 🔹 Corrigindo a query SQL
                        string query = $@"SELECT FIRST {tamanhoLote} SKIP {offset} * FROM {tabela}";

                        using (var cmd = new OdbcCommand(query, conn))
                        using (var adapter = new OdbcDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);

                            if (dt.Rows.Count > 0)
                            {
                                lotes.Add(dt); // Adiciona o lote na lista
                                offset += tamanhoLote;
                            }
                            else
                            {
                                hasMoreData = false;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await MessageService.MostrarDialogPersonalizado(
                    "Erro",
                    $"Erro ao obter dados da tabela {tabela} no Firebird: {ex.Message}"
                );
            }

            return lotes; // Retorna todos os lotes como uma lista
        }


        public async Task<bool> ColunaExiste(Window janela, string tabela, string coluna)
        {
            bool existe = false;

            try
            {
                using (var conn = new OdbcConnection(connectionString))
                {
                    conn.Open();
                    string query = $@"
                        SELECT COUNT(*) 
                        FROM RDB$RELATION_FIELDS 
                        WHERE RDB$RELATION_NAME = '{tabela.ToUpper()}' 
                        AND RDB$FIELD_NAME = '{coluna.ToUpper()}'";

                    using (var cmd = new OdbcCommand(query, conn))
                    {
                        int count = Convert.ToInt32(cmd.ExecuteScalar());
                        existe = count > 0;
                    }
                }

                Console.WriteLine($"🔎 Verificação de coluna: A tabela '{tabela}' {(existe ? "tem" : "NÃO TEM")} a coluna '{coluna}'.");
            }
            catch (Exception ex)
            {
                MessageService.MostrarMensagem(janela, $"Erro ao verificar coluna '{coluna.ToLower()}' na tabela {tabela.ToLower()} no Firebird: {ex.Message}", "Erro", Icon.Error);
            }

            return existe;
        }

        public async Task<int> ObterTotalRegistros(List<string> tabelas)
        {
            int total = 0;
            try
            {
                using (var conn = new OdbcConnection(connectionString))
                {
                    conn.Open();

                    foreach (var tabela in tabelas)
                    {
                        string query = $"SELECT COUNT(*) FROM {tabela}";
                        using (var cmd = new OdbcCommand(query, conn))
                        {
                            total += Convert.ToInt32(cmd.ExecuteScalar());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await MessageService.MostrarDialogPersonalizado(
                    "Erro", 
                    $"Erro ao obter contagem de registros: {ex.Message}"
                );
            }
            return total;
        }

        public async Task<bool> FirebirdTemMaisColunasQueMySQL(string tabela, MySQLService mySQLService)
        {
            var colunasFirebird = (await ObterEstruturaTabela( tabela)).Select(c => c.Nome.ToLower()).ToList();
            var colunasMySQL = (await mySQLService.ObterEstruturaTabela(tabela)).Select(c => c.Nome.ToLower()).ToList();

            return colunasFirebird.Except(colunasMySQL).Any(); // Retorna true se Firebird tiver colunas extras
        }

        public async Task AtualizarEnviado(Window janela, string tabela)
        {
            try
            {
                if (!await ColunaExiste(janela, tabela, "Enviado"))
                {
                    Console.WriteLine($"⚠️ A coluna 'Enviado' não existe na tabela {tabela}. Nenhuma atualização foi feita.");
                    return;
                }

                using (var conn = new OdbcConnection(connectionString))
                {
                    conn.Open();
                    string query = $"UPDATE {tabela} SET enviado = 1 WHERE enviado = 0";

                    using (var cmd = new OdbcCommand(query, conn))
                    {
                        int registrosAtualizados = cmd.ExecuteNonQuery();
                        Console.WriteLine($"✅ {registrosAtualizados} registros foram marcados como 'Enviado' na tabela {tabela} do Firebird.");

                        if (registrosAtualizados == 0)
                        {
                            Console.WriteLine($"⚠️ Nenhum registro foi atualizado na tabela {tabela}. Todos os registros já estavam marcados como '1'.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageService.MostrarMensagem(janela, $"Erro ao atualizar coluna 'Enviado' na tabela {tabela} no Firebird: {ex.Message}",
                    "Erro", Icon.Error);
            }
        }

        public void ReabrirMovimentoFirebird(string tabela, DateTime dataInicio, DateTime dataFim)
        {
            try
            {
                using (var conn = new OdbcConnection(connectionString))
                {
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
                        query = $@"
                        UPDATE {tabela}
                        SET enviado = '0'
                        WHERE nf IN (SELECT nf FROM notafiscal WHERE dtemissao BETWEEN ? AND ?)";
                    }
                    else if (tabela == "itenspedidovenda")
                    {
                        query = $@"
                        UPDATE {tabela}
                        SET enviado = '0'
                        WHERE documento IN (SELECT documento FROM pedidosvenda WHERE data BETWEEN ? AND ?)";
                    }
                    else if (tabela == "itemnfentrada")
                    {
                        query = $@"
                        UPDATE {tabela}
                        SET enviado = '0'
                        WHERE documento IN (SELECT documento FROM nfentrada WHERE dtlanca BETWEEN ? AND ?)";
                    }
                    else if (colunaData != null)
                    {
                        query = $@"
                        UPDATE {tabela}
                        SET enviado = '0'
                        WHERE {colunaData} BETWEEN ? AND ?";
                    }
                    else
                    {
                        LogService.RegistrarLog(
                            "ERROR", 
                            $"⚠️ Tabela {tabela} não possui uma coluna de data válida definida."
                        );

                        return;
                    }

                    using (var cmd = new OdbcCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("?", dataInicio.Date);
                        cmd.Parameters.AddWithValue("?", dataFim.Date);

                        int linhasAfetadas = cmd.ExecuteNonQuery();
                        LogService.RegistrarLog(
                            "INFO", 
                            $"🔄 {linhasAfetadas} registros reabertos na tabela {tabela} (Firebird)."
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.RegistrarLog("cccc", $"❌ Erro ao reabrir movimento na tabela {tabela} (Firebird): {ex.Message}");
            }
        }

        public void FecharMovimentoFirebird(string tabela, DateTime dataInicio, DateTime dataFim)
        {
            try
            {
                using (var conn = new OdbcConnection(connectionString))
                {
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
                        query = $@"
                UPDATE {tabela}
                SET enviado = '1'
                WHERE nf IN (SELECT nf FROM notafiscal WHERE dtemissao BETWEEN ? AND ?)";
                    }
                    else if (tabela == "itenspedidovenda")
                    {
                        query = $@"
                UPDATE {tabela}
                SET enviado = '1'
                WHERE documento IN (SELECT documento FROM pedidosvenda WHERE data BETWEEN ? AND ?)";
                    }
                    else if (tabela == "itemnfentrada")
                    {
                        query = $@"
                UPDATE {tabela}
                SET enviado = '1'
                WHERE documento IN (SELECT documento FROM nfentrada WHERE dtlanca BETWEEN ? AND ?)";
                    }
                    else if (colunaData != null)
                    {
                        query = $@"
                UPDATE {tabela}
                SET enviado = '1'
                WHERE {colunaData} BETWEEN ? AND ?";
                    }
                    else
                    {
                        LogService.RegistrarLog(
                            "ERROR",
                            $"⚠️ Tabela {tabela} não possui uma coluna de data válida definida."
                        );

                        return;
                    }

                    using (var cmd = new OdbcCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("?", dataInicio.Date);
                        cmd.Parameters.AddWithValue("?", dataFim.Date);

                        int linhasAfetadas = cmd.ExecuteNonQuery();
                        LogService.RegistrarLog(
                            "INFO",
                            $"🔄 {linhasAfetadas} registros reabertos na tabela {tabela} (Firebird)."
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.RegistrarLog("ERROR", $"❌ Erro ao reabrir movimento na tabela {tabela} (Firebird): {ex.Message}");
            }
        }

        public void ExcluirMovimentoFirebird(string tabela, DateTime dataInicio, DateTime dataFim)
        {
            try
            {
                using (var conn = new OdbcConnection(connectionString))
                {
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
                        query = $@"
                        delete from {tabela}
                        WHERE nf IN (SELECT nf FROM notafiscal WHERE dtemissao BETWEEN ? AND ?)";
                    }
                    else if (tabela == "itenspedidovenda")
                    {
                        query = $@"
                        delete from {tabela}
                        WHERE documento IN (SELECT documento FROM pedidosvenda WHERE data BETWEEN ? AND ?)";
                    }
                    else if (tabela == "itemnfentrada")
                    {
                        query = $@"
                        delete from {tabela}
                        WHERE documento IN (SELECT documento FROM nfentrada WHERE dtlanca BETWEEN ? AND ?)";
                    }
                    else if (colunaData != null)
                    {
                        query = $@"
                        delete from {tabela}
                        WHERE {colunaData} BETWEEN ? AND ?";
                    }
                    else
                    {
                        LogService.RegistrarLog(
                            "ERROR",
                            $"⚠️ Tabela {tabela} não possui uma coluna de data válida definida."
                        );

                        return;
                    }

                    using (var cmd = new OdbcCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("?", dataInicio.Date);
                        cmd.Parameters.AddWithValue("?", dataFim.Date);

                        int linhasAfetadas = cmd.ExecuteNonQuery();
                        LogService.RegistrarLog(
                            "INFO",
                            $"🔄 {linhasAfetadas} registros excluídos na tabela {tabela} (Firebird)."
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.RegistrarLog("ERROR", $"❌ Erro ao excluir os movimentos na tabela {tabela} (Firebird): {ex.Message}");
            }
        }

        public void AtualizarFilialFirebird(string tabela, int xFilial)
        {
            try
            {
                using (var conn = new OdbcConnection(connectionString))
                {
                    conn.Open();

                    string query = $@"
                    UPDATE {tabela}
                    SET filial = ?
                    WHERE filial IS NULL OR filial != ?";

                    using (var cmd = new OdbcCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@filial", xFilial);
                        cmd.Parameters.AddWithValue("@filialAtual", xFilial); // Para evitar atualização desnecessária.

                        int linhasAfetadas = cmd.ExecuteNonQuery();

                        LogService.RegistrarLog(
                            "INFO",
                            $"🔄 {linhasAfetadas} registros atualizados na tabela {tabela} (Firebird)."
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.RegistrarLog("ERROR", $"❌ Erro ao atualizar a filial na tabela {tabela} (Firebird): {ex.Message}");
            }
        }

        public void AlterarDocumentoFirebird(string tabela, int xEmpresa, int yEmpresa)
        {
            try
            {
                using (var conn = new OdbcConnection(connectionString))
                {
                    conn.Open();

                    // Mapeamento das colunas
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

                    // Formata a query diretamente para evitar problemas com o REPLACE()
                    string query = $@"
                    UPDATE {tabela}
                    SET {colunaDocumento} = REPLACE(REPLACE({colunaDocumento}, 'P{xEmpresa}', 'P{yEmpresa}'), 'F{xEmpresa}', 'F{yEmpresa}')
                    WHERE {colunaDocumento} LIKE 'P{xEmpresa}%' OR {colunaDocumento} LIKE 'F{xEmpresa}%'";

                    using (var cmd = new OdbcCommand(query, conn))
                    {
                        int linhasAfetadas = cmd.ExecuteNonQuery();
                        LogService.RegistrarLog("INFO", $"✅ {linhasAfetadas} registros alterados na tabela {tabela} (Firebird).");
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.RegistrarLog("ERROR", $"❌ Erro ao alterar a numeração do documento na tabela {tabela} (Firebird): {ex.Message}");
            }
        }
    }
}
