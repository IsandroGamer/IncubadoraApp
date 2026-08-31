using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using IncubadoraApp.Models;

namespace IncubadoraApp.Views;

/// View principal do sistema. Os dados vêm da base de dados MySQL
/// `incubadora_db` através de Database.GetConnection() (Models/Database.cs).
public partial class MainView : UserControl
{
    // ==========================================
    // PROPRIEDADES E ESTADOS DA INTERFACE
    // ==========================================

    public ModelUtilizador? UsuarioAtual { get; set; }
    public ModelFase? FaseAtual { get; set; }
    public ModelDiagnostico? UltimoDiagnostico { get; set; }
    public ModelLeitura? UltimaLeitura { get; set; }

    public ObservableCollection<ModelDispositivo> Dispositivos { get; set; } = new();
    public ObservableCollection<ModelAlerta> Alertas { get; set; } = new();

    private string _nomeUsuarioLogado = string.Empty;
    private bool _carregandoDados;

    // Atualização automática periódica dos dados (além do botão manual)
    private readonly TimeSpan _intervaloAutoRefresh = TimeSpan.FromSeconds(10);
    private DispatcherTimer? _timerAtualizacao;

    // ==========================================
    // CONSTRUTORES
    // ==========================================

    public MainView()
    {
        InitializeComponent();
        ConfigurarBotoes();
        _ = CarregarDadosAsync(string.Empty);
        IniciarAutoRefresh();
    }

    public MainView(string usuario)
    {
        InitializeComponent();
        _nomeUsuarioLogado = usuario;
        ConfigurarBotoes();
        _ = CarregarDadosAsync(usuario);
        IniciarAutoRefresh();
    }

    /// Liga o temporizador que recarrega os dados periodicamente.
    /// Fica ativo enquanto a MainView estiver visível no ecrã.
    private void IniciarAutoRefresh()
    {
        _timerAtualizacao = new DispatcherTimer
        {
            Interval = _intervaloAutoRefresh
        };
        _timerAtualizacao.Tick += async (_, _) =>
        {
            // Se já houver um carregamento em curso (ex: o utilizador acabou
            // de premir "Atualizar"), este tick é simplesmente ignorado —
            // CarregarDadosAsync já se protege com _carregandoDados.
            await CarregarDadosAsync(_nomeUsuarioLogado);
        };
        _timerAtualizacao.Start();
    }

    /// Para o temporizador quando a view sai do ecrã (ex: logout), para não
    /// continuar a consultar a base de dados em segundo plano sem necessidade.
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _timerAtualizacao?.Stop();
        _timerAtualizacao = null;
        base.OnDetachedFromVisualTree(e);
    }

    // ==========================================
    // CONFIGURAÇÃO DE BOTÕES
    // ==========================================

    private void ConfigurarBotoes()
    {
        var btnAlterarFase = this.FindControl<Button>("BtnAlterarFase");
        var btnExecutarDiagnostico = this.FindControl<Button>("BtnExecutarDiagnostico");
        var btnAlternarModo = this.FindControl<Button>("BtnAlternarModo");
        var btnAtualizar = this.FindControl<Button>("BtnAtualizar");
        var btnSair = this.FindControl<Button>("BtnSair");

        if (btnAlterarFase != null) btnAlterarFase.Click += OnAlterarFaseClicked;
        if (btnExecutarDiagnostico != null) btnExecutarDiagnostico.Click += OnExecutarDiagnosticoClicked;
        if (btnAlternarModo != null) btnAlternarModo.Click += OnAlternarModoClicked;
        if (btnAtualizar != null) btnAtualizar.Click += OnAtualizarClicked;
        if (btnSair != null) btnSair.Click += OnSairClicked;
    }

    // ==========================================
    // HELPERS DE LEITURA (colunas nulas / tipos unsigned do MySQL)
    // ==========================================

    private static string? GetNullableString(MySqlDataReader r, string coluna)
        => r.IsDBNull(r.GetOrdinal(coluna)) ? null : r.GetString(coluna);

    private static decimal? GetNullableDecimal(MySqlDataReader r, string coluna)
        => r.IsDBNull(r.GetOrdinal(coluna)) ? (decimal?)null : r.GetDecimal(coluna);

    private static DateTime? GetNullableDateTime(MySqlDataReader r, string coluna)
        => r.IsDBNull(r.GetOrdinal(coluna)) ? (DateTime?)null : r.GetDateTime(coluna);

    private static uint GetUInt32(MySqlDataReader r, string coluna)
        => r.GetFieldValue<uint>(r.GetOrdinal(coluna));

    private static uint? GetNullableUInt32(MySqlDataReader r, string coluna)
        => r.IsDBNull(r.GetOrdinal(coluna)) ? (uint?)null : r.GetFieldValue<uint>(r.GetOrdinal(coluna));

    private static ulong GetUInt64(MySqlDataReader r, string coluna)
        => r.GetFieldValue<ulong>(r.GetOrdinal(coluna));

    private static ulong? GetNullableUInt64(MySqlDataReader r, string coluna)
        => r.IsDBNull(r.GetOrdinal(coluna)) ? (ulong?)null : r.GetFieldValue<ulong>(r.GetOrdinal(coluna));

    // ==========================================
    // CARREGAMENTO DE DADOS (MySQL)
    // ==========================================

    /// Vai buscar todos os dados à base de dados e atualiza a UI.
    /// Chamado no arranque e sempre que o botão "Atualizar" é premido.
    private async Task CarregarDadosAsync(string usuario)
    {
        if (_carregandoDados) return;
        _carregandoDados = true;

        DefinirEstadoBotaoAtualizar(ativo: false, texto: "A atualizar...");
        DefinirStatus("A carregar dados da base de dados...", erro: false);

        try
        {
            UsuarioAtual = await ObterUtilizadorPorNomeAsync(usuario);
            bool isAdmin = UsuarioAtual?.Papel == "administrador";

            // Administrador vê todos os dispositivos; avicultor só os seus;
            // se o utilizador não for encontrado, mostra todos como fallback.
            Dispositivos = (UsuarioAtual == null || isAdmin)
                ? await ObterDispositivosAsync()
                : await ObterDispositivosDoUtilizadorAsync(UsuarioAtual.Id);

            if (Dispositivos.Count > 0)
            {
                var dispositivoId = Dispositivos[0].Id;
                FaseAtual = await ObterFaseAtivaAsync(dispositivoId);
                UltimoDiagnostico = await ObterUltimoDiagnosticoAsync(dispositivoId);
                UltimaLeitura = await ObterUltimaLeituraAsync(dispositivoId);
                Alertas = await ObterAlertasAtivosAsync(dispositivoId);
            }
            else
            {
                FaseAtual = null;
                UltimoDiagnostico = null;
                UltimaLeitura = null;
                Alertas = await ObterTodosAlertasAtivosAsync();
            }

            AtualizarInterface();
            DefinirStatus($"Dados atualizados às {DateTime.Now:HH:mm:ss}", erro: false);
        }
        catch (Exception ex)
        {
            DefinirStatus($"Erro ao ligar à base de dados: {ex.Message}", erro: true);
        }
        finally
        {
            DefinirEstadoBotaoAtualizar(ativo: true, texto: "Atualizar");
            _carregandoDados = false;
        }
    }

    // ------------------------------------------------------------------
    // UTILIZADOR
    // ------------------------------------------------------------------
    private static async Task<ModelUtilizador?> ObterUtilizadorPorNomeAsync(string nome)
    {
        const string sql = @"SELECT id, nome, email, contacto, papel, ativo, criado_em, atualizado_em
                              FROM utilizadores
                              WHERE nome = @nome AND ativo = 1
                              LIMIT 1;";

        await using var conn = Database.GetConnection();
        await conn.OpenAsync();

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@nome", nome);

        await using var reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new ModelUtilizador
            {
                Id = GetUInt32(reader, "id"),
                Nome = reader.GetString("nome"),
                Email = reader.GetString("email"),
                Contacto = GetNullableString(reader, "contacto"),
                Papel = reader.GetString("papel"), // "avicultor" ou "administrador"
                Ativo = reader.GetBoolean("ativo"),
                CriadoEm = reader.GetDateTime("criado_em"),
                AtualizadoEm = reader.GetDateTime("atualizado_em")
            };
        }

        return null;
    }

    // ------------------------------------------------------------------
    // DISPOSITIVOS
    // ------------------------------------------------------------------
    private static async Task<ObservableCollection<ModelDispositivo>> ObterDispositivosAsync()
    {
        const string sql = @"SELECT id, nome, tipo, numero_serie, localizacao, estado, ultima_leitura_em
                              FROM dispositivos
                              ORDER BY id;";

        return await ExecutarListaDispositivosAsync(sql, cmd => { });
    }

    private static async Task<ObservableCollection<ModelDispositivo>> ObterDispositivosDoUtilizadorAsync(uint utilizadorId)
    {
        const string sql = @"SELECT d.id, d.nome, d.tipo, d.numero_serie, d.localizacao, d.estado, d.ultima_leitura_em
                              FROM dispositivos d
                              INNER JOIN utilizadores_dispositivos ud ON ud.dispositivo_id = d.id
                              WHERE ud.utilizador_id = @utilizadorId
                              ORDER BY d.id;";

        return await ExecutarListaDispositivosAsync(sql, cmd =>
            cmd.Parameters.AddWithValue("@utilizadorId", utilizadorId));
    }

    private static async Task<ObservableCollection<ModelDispositivo>> ExecutarListaDispositivosAsync(
        string sql, Action<MySqlCommand> configurarParametros)
    {
        var lista = new ObservableCollection<ModelDispositivo>();

        await using var conn = Database.GetConnection();
        await conn.OpenAsync();

        await using var cmd = new MySqlCommand(sql, conn);
        configurarParametros(cmd);

        await using var reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(new ModelDispositivo
            {
                Id = GetUInt32(reader, "id"),
                Nome = reader.GetString("nome"),
                Tipo = reader.GetString("tipo"),
                NumeroSerie = reader.GetString("numero_serie"),
                Localizacao = GetNullableString(reader, "localizacao"),
                Estado = reader.GetString("estado"), // ligado / desligado / erro / ciclo_concluido
                UltimaLeituraEm = GetNullableDateTime(reader, "ultima_leitura_em")
            });
        }

        return lista;
    }

    // ------------------------------------------------------------------
    // FASE ATIVA
    // ------------------------------------------------------------------
    private static async Task<ModelFase?> ObterFaseAtivaAsync(uint dispositivoId)
    {
        const string sql = @"SELECT id, dispositivo_id, tipo, nome, data_inicio, data_fim, estado
                              FROM fases
                              WHERE dispositivo_id = @dispositivoId AND estado = 'ativa'
                              ORDER BY id DESC
                              LIMIT 1;";

        await using var conn = Database.GetConnection();
        await conn.OpenAsync();

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@dispositivoId", dispositivoId);

        await using var reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new ModelFase
            {
                Id = GetUInt32(reader, "id"),
                DispositivoId = GetUInt32(reader, "dispositivo_id"),
                Tipo = reader.GetString("tipo"), // incubacao / eclosao / pos_eclosao
                Nome = GetNullableString(reader, "nome"),
                DataInicio = reader.GetDateTime("data_inicio"),
                DataFim = GetNullableDateTime(reader, "data_fim"),
                Estado = reader.GetString("estado")
            };
        }

        return null;
    }

    // ------------------------------------------------------------------
    // ÚLTIMA LEITURA (temperatura/humidade/rotação)
    // ------------------------------------------------------------------
    private static async Task<ModelLeitura?> ObterUltimaLeituraAsync(uint dispositivoId)
    {
        const string sql = @"SELECT id, dispositivo_id, fase_id, dia, fase_nome, temperatura, humidade,
                                     estado_temperatura, estado_humidade, rotacao_ativa, alerta, data_hora
                              FROM vw_ultima_leitura_por_dispositivo
                              WHERE dispositivo_id = @dispositivoId
                              LIMIT 1;";

        await using var conn = Database.GetConnection();
        await conn.OpenAsync();

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@dispositivoId", dispositivoId);

        await using var reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new ModelLeitura
            {
                Id = GetUInt64(reader, "id"),
                DispositivoId = GetUInt32(reader, "dispositivo_id"),
                FaseId = GetNullableUInt32(reader, "fase_id"),
                Dia = reader.GetByte("dia"),
                FaseNome = reader.GetString("fase_nome"),
                Temperatura = GetNullableDecimal(reader, "temperatura"),
                Humidade = GetNullableDecimal(reader, "humidade"),
                EstadoTemperatura = reader.GetString("estado_temperatura"),
                EstadoHumidade = reader.GetString("estado_humidade"),
                RotacaoAtiva = reader.GetBoolean("rotacao_ativa"),
                Alerta = reader.GetString("alerta"),
                DataHora = reader.GetDateTime("data_hora")
            };
        }

        return null;
    }

    // ------------------------------------------------------------------
    // ÚLTIMO DIAGNÓSTICO
    // ------------------------------------------------------------------
    private static async Task<ModelDiagnostico?> ObterUltimoDiagnosticoAsync(uint dispositivoId)
    {
        const string sql = @"SELECT id, dispositivo_id, fase_id, resultado, confianca, recomendacao, data_hora, gerado_por
                              FROM diagnosticos
                              WHERE dispositivo_id = @dispositivoId
                              ORDER BY data_hora DESC, id DESC
                              LIMIT 1;";

        await using var conn = Database.GetConnection();
        await conn.OpenAsync();

        await using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@dispositivoId", dispositivoId);

        await using var reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new ModelDiagnostico
            {
                Id = GetUInt32(reader, "id"),
                DispositivoId = GetUInt32(reader, "dispositivo_id"),
                FaseId = GetNullableUInt32(reader, "fase_id"),
                Resultado = reader.GetString("resultado"),
                Confianca = GetNullableDecimal(reader, "confianca"),
                Recomendacao = GetNullableString(reader, "recomendacao"),
                DataHora = reader.GetDateTime("data_hora"),
                GeradoPor = GetNullableUInt32(reader, "gerado_por")
            };
        }

        return null;
    }

    // ------------------------------------------------------------------
    // ALERTAS ATIVOS
    // ------------------------------------------------------------------
    private static async Task<ObservableCollection<ModelAlerta>> ObterAlertasAtivosAsync(uint dispositivoId)
    {
        const string sql = @"SELECT id, dispositivo_id, leitura_id, tipo, gravidade, mensagem, estado,
                                     data_hora, resolvido_por, resolvido_em
                              FROM alertas
                              WHERE dispositivo_id = @dispositivoId AND estado = 'ativo'
                              ORDER BY data_hora DESC;";

        return await ExecutarListaAlertasAsync(sql, cmd =>
            cmd.Parameters.AddWithValue("@dispositivoId", dispositivoId));
    }

    private static async Task<ObservableCollection<ModelAlerta>> ObterTodosAlertasAtivosAsync()
    {
        const string sql = @"SELECT id, dispositivo_id, leitura_id, tipo, gravidade, mensagem, estado,
                                     data_hora, resolvido_por, resolvido_em
                              FROM alertas
                              WHERE estado = 'ativo'
                              ORDER BY data_hora DESC;";

        return await ExecutarListaAlertasAsync(sql, cmd => { });
    }

    private static async Task<ObservableCollection<ModelAlerta>> ExecutarListaAlertasAsync(
        string sql, Action<MySqlCommand> configurarParametros)
    {
        var lista = new ObservableCollection<ModelAlerta>();

        await using var conn = Database.GetConnection();
        await conn.OpenAsync();

        await using var cmd = new MySqlCommand(sql, conn);
        configurarParametros(cmd);

        await using var reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(new ModelAlerta
            {
                Id = GetUInt32(reader, "id"),
                DispositivoId = GetUInt32(reader, "dispositivo_id"),
                LeituraId = GetNullableUInt64(reader, "leitura_id"),
                Tipo = reader.GetString("tipo"),
                Gravidade = reader.GetString("gravidade"), // baixa/media/alta/critica
                Mensagem = reader.GetString("mensagem"),
                Estado = reader.GetString("estado"),
                DataHora = reader.GetDateTime("data_hora"),
                ResolvidoPor = GetNullableUInt32(reader, "resolvido_por"),
                ResolvidoEm = GetNullableDateTime(reader, "resolvido_em")
            });
        }

        return lista;
    }

    // ==========================================
    // ESTADO / FEEDBACK NA UI
    // ==========================================

    private void DefinirEstadoBotaoAtualizar(bool ativo, string texto)
    {
        var btnAtualizar = this.FindControl<Button>("BtnAtualizar");
        if (btnAtualizar != null)
        {
            btnAtualizar.IsEnabled = ativo;
            btnAtualizar.Content = texto;
        }
    }

    private void DefinirStatus(string mensagem, bool erro)
    {
        var lblStatus = this.FindControl<TextBlock>("LblStatus");
        if (lblStatus != null)
        {
            lblStatus.Text = mensagem;
            lblStatus.Foreground = erro
                ? new SolidColorBrush(Color.Parse("#DC2626"))
                : new SolidColorBrush(Color.Parse("#64748B"));
        }
    }

    // ==========================================
    // RENDERIZAÇÃO DA INTERFACE (UI)
    // ==========================================

    private void AtualizarInterface()
    {
        var lblNomeUsuario = this.FindControl<TextBlock>("LblNomeUsuario");
        var lblFaseNome = this.FindControl<TextBlock>("LblFaseNome");
        var lblFaseEstado = this.FindControl<TextBlock>("LblFaseEstado");
        var lblDiagnosticoResultado = this.FindControl<TextBlock>("LblDiagnosticoResultado");
        var lblDiagnosticoConfianca = this.FindControl<TextBlock>("LblDiagnosticoConfianca");
        var lblTemperatura = this.FindControl<TextBlock>("LblTemperatura");
        var lblHumidade = this.FindControl<TextBlock>("LblHumidade");
        var lblRotacao = this.FindControl<TextBlock>("LblRotacao");

        if (lblNomeUsuario != null)
            lblNomeUsuario.Text = UsuarioAtual != null
                ? $"Utilizador: {UsuarioAtual.Nome}"
                : "Utilizador não encontrado";

        if (lblFaseNome != null) lblFaseNome.Text = FaseAtual?.Nome ?? "Sem fase ativa";
        if (lblFaseEstado != null) lblFaseEstado.Text = $"Estado: {FaseAtual?.Estado ?? "-"}";
        if (lblDiagnosticoResultado != null) lblDiagnosticoResultado.Text = UltimoDiagnostico?.Resultado ?? "Sem diagnóstico";

        if (lblDiagnosticoConfianca != null)
            lblDiagnosticoConfianca.Text = UltimoDiagnostico?.Confianca is decimal conf
                ? $"Confiança: {conf:0.0}%"
                : "Confiança: --%";

        // Telemetria (tabela `leituras`)
        if (lblTemperatura != null)
            lblTemperatura.Text = UltimaLeitura?.Temperatura is decimal t ? $"{t:0.0} °C" : "--.- °C";

        if (lblHumidade != null)
            lblHumidade.Text = UltimaLeitura?.Humidade is decimal h ? $"{h:0.0} %" : "--.- %";

        if (lblRotacao != null)
        {
            var ativa = UltimaLeitura?.RotacaoAtiva ?? false;
            lblRotacao.Text = ativa ? "Ativa" : "Inativa";
            lblRotacao.Foreground = new SolidColorBrush(Color.Parse(ativa ? "#16A34A" : "#D97706"));
        }

        // Lista de dispositivos
        var listaDispositivos = this.FindControl<ListBox>("ListaDispositivos");
        if (listaDispositivos != null)
        {
            listaDispositivos.Items.Clear();

            foreach (var dispositivo in Dispositivos)
            {
                var border = new Border
                {
                    Background = Brushes.White,
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(10),
                    BorderBrush = new SolidColorBrush(Color.Parse("#E2E8F0")),
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(0, 2)
                };

                var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
                var stack = new StackPanel { Spacing = 2 };

                var nome = new TextBlock { Text = dispositivo.Nome, FontSize = 14, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.Parse("#1E293B")) };
                var localizacao = new TextBlock { Text = $"Local: {dispositivo.Localizacao ?? "-"}", FontSize = 12, Foreground = new SolidColorBrush(Color.Parse("#64748B")) };

                stack.Children.Add(nome);
                stack.Children.Add(localizacao);
                Grid.SetColumn(stack, 0);

                var estado = new TextBlock
                {
                    Text = dispositivo.Estado,
                    FontSize = 12,
                    FontWeight = FontWeight.Bold,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    Foreground = new SolidColorBrush(Color.Parse(
                        dispositivo.Estado.Equals("ligado", StringComparison.OrdinalIgnoreCase) ? "#16A34A" : "#DC2626"))
                };
                Grid.SetColumn(estado, 1);

                grid.Children.Add(stack);
                grid.Children.Add(estado);
                border.Child = grid;

                listaDispositivos.Items.Add(border);
            }
        }

        // Lista de alertas
        var listaAlertas = this.FindControl<ListBox>("ListaAlertas");
        if (listaAlertas != null)
        {
            listaAlertas.Items.Clear();

            foreach (var alerta in Alertas)
            {
                var border = new Border
                {
                    Background = new SolidColorBrush(Color.Parse("#FEF2F2")),
                    BorderBrush = new SolidColorBrush(Color.Parse("#FECACA")),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(10),
                    Margin = new Thickness(0, 2)
                };

                var stack = new StackPanel { Spacing = 2 };
                var titulo = new TextBlock { Text = $"{alerta.Tipo} - {alerta.Gravidade}", FontSize = 13, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.Parse("#DC2626")) };
                var mensagem = new TextBlock { Text = alerta.Mensagem, FontSize = 12, Foreground = new SolidColorBrush(Color.Parse("#7F1D1D")) };

                stack.Children.Add(titulo);
                stack.Children.Add(mensagem);
                border.Child = stack;

                listaAlertas.Items.Add(border);
            }

            if (Alertas.Count == 0)
            {
                listaAlertas.Items.Add(new TextBlock
                {
                    Text = "Sem alertas ativos.",
                    Foreground = new SolidColorBrush(Color.Parse("#64748B")),
                    Margin = new Thickness(10)
                });
            }
        }
    }

    // ==========================================
    // TRATADORES DE EVENTOS DE BOTÃO
    // ==========================================

    private void OnAlterarFaseClicked(object? sender, RoutedEventArgs e)
    {
        // NOTA: ainda só altera em memória. Para persistir na BD, adiciona
        // aqui um UPDATE/INSERT à tabela `fases` usando Database.GetConnection().
        if (FaseAtual == null) return;

        FaseAtual.Tipo = "eclosao";
        FaseAtual.Nome = "Eclodimento Final";
        FaseAtual.Estado = "ativa";

        AtualizarInterface();
    }

    private void OnExecutarDiagnosticoClicked(object? sender, RoutedEventArgs e)
    {
        // NOTA: idem — chamar o teu algoritmo real e gravar em `diagnosticos`.
        if (UltimoDiagnostico == null) return;

        UltimoDiagnostico.Resultado = "Excelente";
        UltimoDiagnostico.Confianca = 99.0m;

        AtualizarInterface();
    }

    private void OnAlternarModoClicked(object? sender, RoutedEventArgs e)
    {
        // A implementar conforme a lógica dos atuadores do ESP32
    }

    private async void OnAtualizarClicked(object? sender, RoutedEventArgs e)
    {
        await CarregarDadosAsync(_nomeUsuarioLogado);
    }

    private void OnSairClicked(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is Window window)
        {
            window.Content = new LoginView();
        }
    }
}