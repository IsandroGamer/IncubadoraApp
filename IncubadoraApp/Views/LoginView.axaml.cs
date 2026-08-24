using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace IncubadoraApp.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        AvaloniaXamlLoader.Load(this);

        var btnEntrar = this.FindControl<Button>("BtnEntrar");

        if (btnEntrar != null)
        {
            btnEntrar.Click += OnBtnEntrarClicked;
        }
    }

    private void OnBtnEntrarClicked(object? sender, RoutedEventArgs e)
    {
        var txtUsuario = this.FindControl<TextBox>("TxtUsuario");
        var txtSenha = this.FindControl<TextBox>("TxtSenha");
        var lblErro = this.FindControl<TextBlock>("LblErro");

        string usuario = txtUsuario?.Text?.Trim() ?? string.Empty;
        string senha = txtSenha?.Text ?? string.Empty;

        // Validação dos campos
        if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(senha))
        {
            ExibirErro(lblErro, "Preencha todos os campos para continuar.");
            return;
        }

        // Autenticação
        if (usuario == "Isandro" && senha == "1234")
        {
            if (lblErro != null)
                lblErro.IsVisible = false;

            if (TopLevel.GetTopLevel(this) is Window window)
            {
                // Passa o nome do utilizador para a MainView
                window.Content = new MainView(usuario);
            }
        }
        else
        {
            ExibirErro(lblErro, "Utilizador ou palavra-passe incorretos.");
        }
    }

    private void ExibirErro(TextBlock? lblErro, string mensagem)
    {
        if (lblErro != null)
        {
            lblErro.Text = mensagem;
            lblErro.IsVisible = true;
        }
    }
}