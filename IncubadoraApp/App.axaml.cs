using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Diagnostics; // <-- Necessário para o DevTools
using Avalonia.Markup.Xaml;
using IncubadoraApp.ViewModels;
using IncubadoraApp.Views;

namespace IncubadoraApp;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
#if DEBUG
        this.AttachDevTools(); // <-- Atualizado para a sintaxe do Avalonia 11
#endif
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Instancia a Janela Principal e define a LoginView como o conteúdo inicial
            var mainWindow = new MainWindow
            {
                DataContext = new MainViewModel(),
                Content = new LoginView() // <-- Carrega o Login primeiro
            };

            desktop.MainWindow = mainWindow;
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            // Para plataformas móveis/browser
            singleViewPlatform.MainView = new LoginView();
        }

        base.OnFrameworkInitializationCompleted();
    }
}