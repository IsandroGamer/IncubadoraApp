using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using IncubadoraApp.ViewModels;
using IncubadoraApp.Views;

namespace IncubadoraApp;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // O DevTools fica restrito ao Desktop
#if DEBUG
            this.AttachDevTools();
#endif

            // Execução no Desktop
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel(),
                Content = new LoginView()
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            // Execução no Mobile (Android)
            // Instancia a LoginView diretamente
            singleViewPlatform.MainView = new LoginView();
        }

        base.OnFrameworkInitializationCompleted();
    }
}