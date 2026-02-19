using System;
using System.Windows;
using System.Windows.Controls;
using MySql.Data.MySqlClient;
using static TCC.MainWindow;

namespace TCC
{
    public partial class ProfilePage : Page
    {
        // String de conexão para acessar o banco de dados MySQL local
        private string connectionString = "Server=localhost;Database=Sistema;Uid=root;Pwd=;";

        // Armazena a senha real do usuário (carregada do banco)
        private string senhaArmazenada = "";

        // Controle de visibilidade da senha
        private bool isSenhaVisible = false;

        // Flag para indicar se a senha é um hash BCrypt
        private bool senhaEhHash = false;

        public ProfilePage()
        {
            InitializeComponent();
            CarregarDadosPerfil();
            ConfigurarIniciaisUsuario();
        }

        /// <summary>
        /// Verifica se uma string é um hash BCrypt válido
        /// </summary>
        /// <param name="texto">Texto a ser verificado</param>
        /// <returns>True se for um hash BCrypt</returns>
        private bool EhHashBCrypt(string texto)
        {
            // Hash BCrypt sempre começa com $2a$, $2b$, $2x$ ou $2y$ e tem 60 caracteres
            return !string.IsNullOrEmpty(texto) &&
                   texto.Length == 60 &&
                   (texto.StartsWith("$2a$") || texto.StartsWith("$2b$") ||
                    texto.StartsWith("$2x$") || texto.StartsWith("$2y$"));
        }

        // Método para configurar as iniciais do nome do usuário
        private void ConfigurarIniciaisUsuario()
        {
            if (!string.IsNullOrEmpty(txtNome.Text))
            {
                string[] partesNome = txtNome.Text.Split(' ');
                string iniciais = "";
                if (partesNome.Length > 0)
                {
                    iniciais += partesNome[0][0];
                    if (partesNome.Length > 1)
                    {
                        iniciais += partesNome[partesNome.Length - 1][0];
                    }
                }
                txtInitials.Text = iniciais.ToUpper();
            }
        }

        // MÉTODO MODIFICADO para carregar os dados do perfil
        private void CarregarDadosPerfil()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT usuario, telefone, email, senha FROM usuariosCadastrados WHERE cod = @UsuarioId";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@UsuarioId", SessaoUsuario.Cod);

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // Preenche os campos da interface com os valores do banco
                            txtNome.Text = reader["usuario"].ToString();
                            txtTelefone.Text = reader["telefone"].ToString();
                            txtEmail.Text = reader["email"].ToString();

                            // MODIFICADO: Armazena a senha e verifica se é hash
                            senhaArmazenada = reader["senha"].ToString();
                            senhaEhHash = EhHashBCrypt(senhaArmazenada);

                            
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar dados do perfil: " + ex.Message);
            }
        }

       

        // Evento para navegar à página de edição do perfil
        private void btnEditar_Click(object sender, RoutedEventArgs e)
        {
           

            ProfileEditarPage profileEditarPage = new ProfileEditarPage();
            NavigationService.Navigate(profileEditarPage);
        }

        // Evento para sair da conta
        private void btnSairDaConta_Click(object sender, RoutedEventArgs e)
        {
            MainWindow novaJanela = new MainWindow();
            novaJanela.Show();
            Window janelaAtual = Window.GetWindow(this);
            if (janelaAtual != null)
            {
                janelaAtual.Close();
            }
        }
    }
}
