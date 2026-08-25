using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MySql.Data.MySqlClient;

namespace IncubadoraApp.Views;

public partial class LoginView : UserControl
{
    private const string ConexaoString = "Server=192.168.1.213;Database=incubadora_db;Uid=root;Pwd=IsaKellY1971;SslMode=Disabled;AllowPublicKeyRetrieval=True;";

    public LoginView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
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

        if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(senha))
        {
            ExibirErro(lblErro, "Preencha todos os campos para continuar.");
            return;
        }

        try
        {
            using (var conexao = new MySqlConnection(ConexaoString))
            {
                conexao.Open();

                string query = @"SELECT nome, papel 
                                 FROM utilizadores 
                                 WHERE (nome = @u OR email = @u) 
                                   AND credenciais_hash = @s 
                                   AND ativo = 1 
                                 LIMIT 1";

                using (var comando = new MySqlCommand(query, conexao))
                {
                    comando.Parameters.AddWithValue("@u", usuario);
                    comando.Parameters.AddWithValue("@s", senha);

                    using (var reader = comando.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string nomeEncontrado = reader.GetString("nome");

                            if (lblErro != null)
                                lblErro.IsVisible = false;

                            // Suporte para Desktop (Window) e Mobile (ContentControl)
                            var topLevel = TopLevel.GetTopLevel(this);
                            if (topLevel is Window window)
                            {
                                window.Content = new MainView(nomeEncontrado);
                            }
                            else if (this.Parent is ContentControl parentControl)
                            {
                                parentControl.Content = new MainView(nomeEncontrado);
                            }
                        }
                        else
                        {
                            ExibirErro(lblErro, "Utilizador ou palavra-passe incorretos.");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ExibirErro(lblErro, $"Erro ao ligar à base de dados: {ex.Message}");
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