using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Markup.Xaml;
using System.Collections.ObjectModel;
using IncubadoraApp.Models; // Importação dos modelos de dados do projeto (ModelUtilizador, ModelDispositivo, etc.)

namespace IncubadoraApp.Views;

/// View principal do sistema. Gerencia a exibição dos dados da incubadora,
/// alertas ativos, estado das fases e informações do utilizador logado.
public partial class MainView : UserControl
{
    // ==========================================
    // PROPRIEDADES E ESTADOS DA INTERFACE
    // ==========================================

    // Armazena as informações do utilizador atualmente autenticado no sistema
    public ModelUtilizador? UsuarioAtual { get; set; }

    // Armazena a fase de incubação/eclosão em execução no momento
    public ModelFase FaseAtual { get; set; } = null!;

    // Guarda o último diagnóstico gerado pela inteligência/algoritmo da incubadora
    public ModelDiagnostico UltimoDiagnostico { get; set; } = null!;

    // Coleções observáveis que atualizam a interface automaticamente quando itens são adicionados/removidos
    public ObservableCollection<ModelDispositivo> Dispositivos { get; set; } = null!;
    public ObservableCollection<ModelAlerta> Alertas { get; set; } = null!;

    // ==========================================
    // CONSTRUTORES DE INICIALIZAÇÃO
    // ==========================================

    
    /// Construtor padrão (exigido pelo designer de XAML do Avalonia e para testes simples)
    
    public MainView()
    {
        InitializeComponent();
        InicializarDados(string.Empty); // Inicializa com nome de utilizador vazio
    }

 
    /// Construtor principal chamado após o login bem-sucedido
   
    /// <param name="usuario">Nome do utilizador recebido da tela de Login</param>
    public MainView(string usuario)
    {
        InitializeComponent();
        InicializarDados(usuario); // Passa o nome do utilizador logado para preparar os dados
    }

    // ==========================================
    // MÉTODOS DE CONFIGURAÇÃO E DADOS
    // ==========================================


    /// Carrega e estrutura os dados iniciais do sistema (MockData para simulação)
    /// e vincula os eventos de clique dos botões da tela.
    /// 
       private void InicializarDados(string usuario)
    {
        // 1. Instancia o utilizador atual com dados padrão de teste
        UsuarioAtual = new ModelUtilizador
        {
            Id = 1,
            Nome = usuario,
            Email = $"{usuario.ToLower()}@incubadora.com",
            Contacto = "+244923000000",
            Papel = "avicultor" // Papel padrão conforme regras de permissão
        };

        // 2. Define a fase de operação atual do processo de incubação
        FaseAtual = new ModelFase
        {
            Id = 1,
            DispositivoId = 101,
            Tipo = "incubacao",
            Nome = "Viragem de Ovos",
            Estado = "ativa"
        };

        // 3. Define o estado do último relatório de diagnóstico do sistema
        UltimoDiagnostico = new ModelDiagnostico
        {
            Id = 1,
            DispositivoId = 101,
            Resultado = "Estável",
            Confianca = 95.5m,
            Recomendacao = "Manter Humidade"
        };

        // 4. Lista de hardware/dispositivos monitorizados (ex: Placa ESP32 da Incubadora)
        Dispositivos = new ObservableCollection<ModelDispositivo>
        {
            new ModelDispositivo
            {
                Id = 101,
                Nome = "Incubadora Galpão 1",
                Tipo = "incubadora",
                NumeroSerie = "ESP32-AA-BB-CC",
                Localizacao = "Setor A",
                Estado = "ligado"
            }
        };

        // 5. Lista de alertas pendentes do sistema
        Alertas = new ObservableCollection<ModelAlerta>
        {
            new ModelAlerta
            {
                Id = 501,
                DispositivoId = 101,
                Tipo = "temperatura_alta",
                Gravidade = "media", // Mapeado conforme ENUM do banco de dados (sem acento)
                Mensagem = "Temperatura subiu 0.5°C acima do limite ideal.",
                Estado = "ativo"
            }
        };

        // 6. Mapeamento dos botões declarados na view XAML usando os seus IDs (Name)
        var btnAlterarFase = this.FindControl<Button>("BtnAlterarFase");
        var btnExecutarDiagnostico = this.FindControl<Button>("BtnExecutarDiagnostico");
        var btnAlternarModo = this.FindControl<Button>("BtnAlternarModo");
        var btnSair = this.FindControl<Button>("BtnSair");

        // 7. Associação dos eventos de clique aos respetivos métodos tratadores (handlers)
        if (btnAlterarFase != null) btnAlterarFase.Click += OnAlterarFaseClicked;
        if (btnExecutarDiagnostico != null) btnExecutarDiagnostico.Click += OnExecutarDiagnosticoClicked;
        if (btnAlternarModo != null) btnAlternarModo.Click += OnAlternarModoClicked;
        if (btnSair != null) btnSair.Click += OnSairClicked;

        // 8. Desenha e preenche a interface com os dados inicializados
        AtualizarInterface();
    }

    // ==========================================
    // RENDERIZAÇÃO DA INTERFACE (UI)
    // ==========================================

    
    /// Lê as propriedades da classe e renderiza visualmente os componentes no layout Avalonia.
   
    private void AtualizarInterface()
    {
        // Busca os componentes de texto estáticos declarados no XAML
        var lblNomeUsuario = this.FindControl<TextBlock>("LblNomeUsuario");
        var lblFaseNome = this.FindControl<TextBlock>("LblFaseNome");
        var lblFaseEstado = this.FindControl<TextBlock>("LblFaseEstado");
        var lblDiagnosticoResultado = this.FindControl<TextBlock>("LblDiagnosticoResultado");
        var lblDiagnosticoConfianca = this.FindControl<TextBlock>("LblDiagnosticoConfianca");

        // Atualiza o texto dos labels com as informações atuais dos objetos
        if (lblNomeUsuario != null) lblNomeUsuario.Text = $"Utilizador: {UsuarioAtual?.Nome}";
        if (lblFaseNome != null) lblFaseNome.Text = FaseAtual.Nome;
        if (lblFaseEstado != null) lblFaseEstado.Text = $"Estado: {FaseAtual.Estado}";
        if (lblDiagnosticoResultado != null) lblDiagnosticoResultado.Text = UltimoDiagnostico.Resultado;
        if (lblDiagnosticoConfianca != null) lblDiagnosticoConfianca.Text = $"Confiança: {UltimoDiagnostico.Confianca}%";

        // ----------------------------------------------------
        // CONSTRUÇÃO DINÂMICA DA LISTA DE DISPOSITIVOS
        // ----------------------------------------------------
        var listaDispositivos = this.FindControl<ListBox>("ListaDispositivos");
        if (listaDispositivos != null)
        {
            listaDispositivos.Items.Clear(); // Limpa itens antigos para re-renderizar

            foreach (var dispositivo in Dispositivos)
            {
                // Card externo do dispositivo (Container visual com bordas arredondadas)
                var border = new Border
                {
                    Background = Brushes.White,
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(10),
                    BorderBrush = new SolidColorBrush(Color.Parse("#E2E8F0")),
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(0, 2)
                };

                // Grid para alinhar informações à esquerda e estado à direita
                var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
                var stack = new StackPanel { Spacing = 2 };

                // Textos do dispositivo
                var nome = new TextBlock { Text = dispositivo.Nome, FontSize = 14, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.Parse("#1E293B")) };
                var localizacao = new TextBlock { Text = $"Local: {dispositivo.Localizacao}", FontSize = 12, Foreground = new SolidColorBrush(Color.Parse("#64748B")) };

                stack.Children.Add(nome);
                stack.Children.Add(localizacao);
                Grid.SetColumn(stack, 0); // Coluna 0 (Esquerda)

                // Indicador visual de estado (ex: ligado/desligado)
                var estado = new TextBlock
                {
                    Text = dispositivo.Estado,
                    FontSize = 12,
                    FontWeight = FontWeight.Bold,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    Foreground = new SolidColorBrush(Color.Parse("#16A34A")) // Cor verde para estado ligado
                };
                Grid.SetColumn(estado, 1); // Coluna 1 (Direita)

                // Une todos os elementos no card e adiciona ao ListBox
                grid.Children.Add(stack);
                grid.Children.Add(estado);
                border.Child = grid;

                listaDispositivos.Items.Add(border);
            }
        }

        // ----------------------------------------------------
        // CONSTRUÇÃO DINÂMICA DA LISTA DE ALERTAS
        // ----------------------------------------------------
        var listaAlertas = this.FindControl<ListBox>("ListaAlertas");
        if (listaAlertas != null)
        {
            listaAlertas.Items.Clear(); // Limpa a lista antes de reconstruir

            foreach (var alerta in Alertas)
            {
                // Card visual estilizado para destacar o alerta em tom vermelho/alerta
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
        }
    }

    // ==========================================
    // TRATADORES DE EVENTOS DE BOTÃO (ACTIONS)
    // ==========================================

    
    /// Trata a ação de alteração de fase de incubação no sistema
    
    private void OnAlterarFaseClicked(object? sender, RoutedEventArgs e)
    {
        // Simula a transição de fase atual para a fase de eclosão
        FaseAtual.Tipo = "eclosao";
        FaseAtual.Nome = "Eclodimento Final";
        FaseAtual.Estado = "ativa";

        // Recarrega os componentes visuais para aplicar as mudanças de estado
        AtualizarInterface();
    }

    
    /// Executa o diagnóstico sob demanda do sistema
    
    private void OnExecutarDiagnosticoClicked(object? sender, RoutedEventArgs e)
    {
        // Simula atualização no modelo de diagnóstico
        UltimoDiagnostico.Resultado = "Excelente";
        UltimoDiagnostico.Confianca = 99.0m;

        // Recarrega a interface com os novos valores
        AtualizarInterface();
    }

    
    /// Reservado para alternar entre modos de operação da incubadora (ex: Manual / Automático)
    
    private void OnAlternarModoClicked(object? sender, RoutedEventArgs e)
    {
        // A implementar conforme a lógica dos atuadores do ESP32
    }

    
    /// Desconecta o utilizador atual e retorna à tela de Login
   
    private void OnSairClicked(object? sender, RoutedEventArgs e)
    {
        // Obtém a janela principal que está a conter a MainView atual
        if (TopLevel.GetTopLevel(this) is Window window)
        {
            // Substitui o conteúdo da janela atual pela view de Login
            window.Content = new LoginView();
        }
    }
}