using Avalonia.Controls;
using Avalonia.Interactivity;
using MySql.Data.MySqlClient;
using System;

namespace IncubadoraApp.Views
{
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
        }

        private void OnBtnEntrarClicked(object? sender, RoutedEventArgs e)
        {
            var txtUsuario = this.FindControl<TextBox>("TxtUsuario");
            var txtSenha = this.FindControl<TextBox>("TxtSenha");
            var lblErro = this.FindControl<TextBlock>("LblErro");

            string usuario = txtUsuario?.Text?.Trim() ?? string.Empty;
            string senha = txtSenha?.Text ?? string.Empty;

            // Validação dos campos vazios
            if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(senha))
            {
                ExibirErro(lblErro, "Preencha todos os campos para continuar.");
                return;
            }

            // String de ligação com o MySQL
            string conexaoString = "Server=localhost;Database=incubadora_db;Uid=root;Pwd=IsaKellY1971;";

            try
            {
                using (var conexao = new MySqlConnection(conexaoString))
                {
                    conexao.Open();

                    // Consulta que valida utilizador/email, senha e estado ativo
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

                                if (TopLevel.GetTopLevel(this) is Window window)
                                {
                                    window.Content = new MainView(nomeEncontrado);
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

        // Método auxiliar para exibir a mensagem de erro no TextBlock LblErro
        private void ExibirErro(TextBlock? lblErro, string mensagem)
        {
            if (lblErro != null)
            {
                lblErro.Text = mensagem;
                lblErro.IsVisible = true;
            }
        }
    }
}