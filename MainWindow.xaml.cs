using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MySql.Data.MySqlClient;
using System.Linq;
using BCrypt.Net; // Adicione esta referência

namespace TCC
{
    public partial class MainWindow : Window
    {
        // Conexão com o banco de dados MySQL
        private MySqlConnection Conexao;
        private string connectionString = "Server=localhost;Database=Sistema;Uid=root;Pwd=;";

        // Declaração das variáveis para controlar a visibilidade das senhas
        private bool isSenhaLOGINVisible = false;
        private bool isSenhaVisible = false;
        private bool isConfirmaSenhaVisible = false;

        // Construtor da janela principal
        public MainWindow()
        {
            InitializeComponent();
            txtEmailLOGIN.Focus();
            SetLoginActive();
            ConfigurarEventosValidacao();
            ConfigurarEventosSincronizacao();
        }

        // MÉTODOS DE CRIPTOGRAFIA DE SENHA

        /// <summary>
        /// Cria um hash seguro da senha usando BCrypt
        /// </summary>
        /// <param name="senha">Senha em texto plano</param>
        /// <returns>Hash da senha</returns>
        private string CriptografarSenha(string senha)
        {
            try
            {
                // Gera um salt aleatório e cria o hash da senha
                // O número 12 é o "work factor" - quanto maior, mais seguro mas mais lento
                return BCrypt.Net.BCrypt.HashPassword(senha, 12);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao criptografar senha: " + ex.Message, "Erro",
                               MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        /// <summary>
        /// Verifica se a senha fornecida corresponde ao hash armazenado
        /// </summary>
        /// <param name="senha">Senha em texto plano</param>
        /// <param name="hashArmazenado">Hash armazenado no banco de dados</param>
        /// <returns>True se a senha estiver correta</returns>
        private bool VerificarSenha(string senha, string hashArmazenado)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(senha, hashArmazenado);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao verificar senha: " + ex.Message, "Erro",
                               MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        // NOVO MÉTODO PARA CONFIGURAR EVENTOS DE VALIDAÇÃO
        private void ConfigurarEventosValidacao()
        {
            // Eventos para validação em tempo real - LOGIN
            txtEmailLOGIN.TextChanged += ValidarEmailLogin;
            txtSenhaLOGIN.PasswordChanged += ValidarSenhaLogin;
            txtSenhaLOGINVisible.TextChanged += ValidarSenhaLoginVisivel;

            // Eventos para validação em tempo real - CADASTRO
            txtNome.TextChanged += ValidarNome;
            txtEmail.TextChanged += ValidarEmail;
            txtTelefone.TextChanged += ValidarTelefone;
            txtSenha.PasswordChanged += ValidarSenha;
            txtSenhaVisible.TextChanged += ValidarSenhaVisivel;
            txtConfirmaSenha.PasswordChanged += ValidarConfirmaSenha;
            txtConfirmaSenhaVisible.TextChanged += ValidarConfirmaSenhaVisivel;
        }

        // MÉTODO PARA CONFIGURAR EVENTOS DE SINCRONIZAÇÃO
        private void ConfigurarEventosSincronizacao()
        {
            // Adicionar eventos para sincronizar o texto em tempo real - LOGIN
            txtSenhaLOGIN.PasswordChanged += TxtSenhaLOGIN_PasswordChanged;
            txtSenhaLOGINVisible.TextChanged += TxtSenhaLOGINVisible_TextChanged;

            // Adicionar eventos para sincronizar o texto em tempo real - CADASTRO
            txtSenha.PasswordChanged += TxtSenha_PasswordChanged;
            txtSenhaVisible.TextChanged += TxtSenhaVisible_TextChanged;
            txtConfirmaSenha.PasswordChanged += TxtConfirmaSenha_PasswordChanged;
            txtConfirmaSenhaVisible.TextChanged += TxtConfirmaSenhaVisible_TextChanged;

            // Configurar eventos de tecla para navegação
            txtEmailLOGIN.KeyDown += TextBox_KeyDown;
            txtSenhaLOGIN.KeyDown += PasswordBox_KeyDown;
            txtNome.KeyDown += TextBox_KeyDown;
            txtEmail.KeyDown += TextBox_KeyDown;
            txtTelefone.KeyDown += TextBox_KeyDown;
            txtSenha.KeyDown += PasswordBox_KeyDown;
            txtConfirmaSenha.KeyDown += LastPasswordBox_KeyDown;
        }

        // MÉTODOS DE VALIDAÇÃO - LOGIN
        private void ValidarEmailLogin(object sender, TextChangedEventArgs e)
        {
            string email = txtEmailLOGIN.Text.Trim();
            if (string.IsNullOrEmpty(email))
            {
                txtErroEmailLOGIN.Text = "Email é obrigatório";
                txtErroEmailLOGIN.Visibility = Visibility.Visible;
                txtEmailLOGIN.BorderBrush = Brushes.Red;
            }
            else if (!email.Contains("@"))
            {
                txtErroEmailLOGIN.Text = "Email deve conter @";
                txtErroEmailLOGIN.Visibility = Visibility.Visible;
                txtEmailLOGIN.BorderBrush = Brushes.Red;
            }
            else if (!IsValidEmail(email))
            {
                txtErroEmailLOGIN.Text = "Formato de email inválido";
                txtErroEmailLOGIN.Visibility = Visibility.Visible;
                txtEmailLOGIN.BorderBrush = Brushes.Red;
            }
            else
            {
                txtErroEmailLOGIN.Visibility = Visibility.Collapsed;
                txtEmailLOGIN.ClearValue(Border.BorderBrushProperty);
            }
        }

        private void ValidarSenhaLogin(object sender, RoutedEventArgs e)
        {
            if (!isSenhaLOGINVisible)
            {
                ValidarSenhaLoginComum(txtSenhaLOGIN.Password);
            }
        }

        private void ValidarSenhaLoginVisivel(object sender, TextChangedEventArgs e)
        {
            if (isSenhaLOGINVisible)
            {
                ValidarSenhaLoginComum(txtSenhaLOGINVisible.Text);
            }
        }

        private void ValidarSenhaLoginComum(string senha)
        {
            if (string.IsNullOrEmpty(senha))
            {
                txtErroSenhaLOGIN.Text = "Senha é obrigatória";
                txtErroSenhaLOGIN.Visibility = Visibility.Visible;
                txtSenhaLOGIN.BorderBrush = Brushes.Red;
                txtSenhaLOGINVisible.BorderBrush = Brushes.Red;
            }
            else
            {
                txtErroSenhaLOGIN.Visibility = Visibility.Collapsed;
                txtSenhaLOGIN.ClearValue(Border.BorderBrushProperty);
                txtSenhaLOGINVisible.ClearValue(Border.BorderBrushProperty);
            }
        }

        // MÉTODOS DE VALIDAÇÃO - CADASTRO
        private void ValidarNome(object sender, TextChangedEventArgs e)
        {
            string nome = txtNome.Text.Trim();
            if (string.IsNullOrEmpty(nome))
            {
                txtErroNome.Text = "Nome é obrigatório";
                txtErroNome.Visibility = Visibility.Visible;
                txtNome.BorderBrush = Brushes.Red;
            }
            else if (nome.Length < 6)
            {
                txtErroNome.Text = "Nome deve ter pelo menos 6 caracteres";
                txtErroNome.Visibility = Visibility.Visible;
                txtNome.BorderBrush = Brushes.Red;
            }
            else if (nome.Length > 25)
            {
                txtErroNome.Text = "Nome deve ter no máximo 25 caracteres";
                txtErroNome.Visibility = Visibility.Visible;
                txtNome.BorderBrush = Brushes.Red;
            }
            else if (!Regex.IsMatch(nome, @"^[A-Za-zÀ-ÿ0-9\s]+$"))
            {
                txtErroNome.Text = "Nome deve conter apenas letras e números";
                txtErroNome.Visibility = Visibility.Visible;
                txtNome.BorderBrush = Brushes.Red;
            }
            else if (NomeUsuarioJaExiste(nome))
            {
                txtErroNome.Text = "Este nome de usuário já está em uso";
                txtErroNome.Visibility = Visibility.Visible;
                txtNome.BorderBrush = Brushes.Red;
            }
            else
            {
                txtErroNome.Visibility = Visibility.Collapsed;
                txtNome.ClearValue(Border.BorderBrushProperty);
            }
        }

        private void ValidarEmail(object sender, TextChangedEventArgs e)
        {
            string email = txtEmail.Text.Trim();
            if (string.IsNullOrEmpty(email))
            {
                txtErroEmail.Text = "Email é obrigatório";
                txtErroEmail.Visibility = Visibility.Visible;
                txtEmail.BorderBrush = Brushes.Red;
            }
            else if (!email.Contains("@"))
            {
                txtErroEmail.Text = "Email deve conter @";
                txtErroEmail.Visibility = Visibility.Visible;
                txtEmail.BorderBrush = Brushes.Red;
            }
            else if (!IsValidEmail(email))
            {
                txtErroEmail.Text = "Formato de email inválido. OBS: Apenas letras e números";
                txtErroEmail.Visibility = Visibility.Visible;
                txtEmail.BorderBrush = Brushes.Red;
            }
            else if (EmailJaExiste(email))
            {
                txtErroEmail.Text = "Este email já está cadastrado";
                txtErroEmail.Visibility = Visibility.Visible;
                txtEmail.BorderBrush = Brushes.Red;
            }
            else
            {
                txtErroEmail.Visibility = Visibility.Collapsed;
                txtEmail.ClearValue(Border.BorderBrushProperty);
            }
        }

        private void ValidarTelefone(object sender, TextChangedEventArgs e)
        {
            string telefone = txtTelefone.Text;
            string apenasNumeros = Regex.Replace(telefone, @"[^\d]", "");

            if (apenasNumeros.Length <= 11)
            {
                string telefoneFormatado = FormatarTelefone(apenasNumeros);
                if (txtTelefone.Text != telefoneFormatado)
                {
                    int cursorPosition = txtTelefone.CaretIndex;
                    string textoAntesCursor = telefone.Substring(0, Math.Min(cursorPosition, telefone.Length));
                    int digitosAntesCursor = Regex.Replace(textoAntesCursor, @"[^\d]", "").Length;

                    txtTelefone.Text = telefoneFormatado;
                    int novaPosicao = CalcularPosicaoCursor(telefoneFormatado, digitosAntesCursor);
                    txtTelefone.CaretIndex = Math.Min(novaPosicao, telefoneFormatado.Length);
                }
            }

            if (string.IsNullOrEmpty(apenasNumeros))
            {
                txtErroTelefone.Text = "Telefone é obrigatório";
                txtErroTelefone.Visibility = Visibility.Visible;
                txtTelefone.BorderBrush = Brushes.Red;
            }
            else if (apenasNumeros.Length != 11)
            {
                txtErroTelefone.Text = "Telefone deve ter exatamente 11 dígitos";
                txtErroTelefone.Visibility = Visibility.Visible;
                txtTelefone.BorderBrush = Brushes.Red;
            }
            else if (TelefoneJaExiste(telefone))
            {
                txtErroTelefone.Text = "Este telefone já está cadastrado";
                txtErroTelefone.Visibility = Visibility.Visible;
                txtTelefone.BorderBrush = Brushes.Red;
            }
            else
            {
                txtErroTelefone.Visibility = Visibility.Collapsed;
                txtTelefone.ClearValue(Border.BorderBrushProperty);
            }
        }

        private int CalcularPosicaoCursor(string textoFormatado, int quantidadeDigitos)
        {
            int posicao = 0;
            int digitosContados = 0;
            for (int i = 0; i < textoFormatado.Length && digitosContados < quantidadeDigitos; i++)
            {
                if (char.IsDigit(textoFormatado[i]))
                {
                    digitosContados++;
                }
                posicao = i + 1;
            }
            return posicao;
        }

        private void ValidarSenha(object sender, RoutedEventArgs e)
        {
            if (!isSenhaVisible)
            {
                ValidarSenhaComum(txtSenha.Password);
            }
        }

        private void ValidarSenhaVisivel(object sender, TextChangedEventArgs e)
        {
            if (isSenhaVisible)
            {
                ValidarSenhaComum(txtSenhaVisible.Text);
            }
        }

        private void ValidarSenhaComum(string senha)
        {
            if (string.IsNullOrEmpty(senha))
            {
                txtErroSenha.Text = "Senha é obrigatória";
                txtErroSenha.Visibility = Visibility.Visible;
                txtSenha.BorderBrush = Brushes.Red;
                txtSenhaVisible.BorderBrush = Brushes.Red;
            }
            else if (senha.Length < 8)
            {
                txtErroSenha.Text = "Senha deve ter pelo menos 8 caracteres";
                txtErroSenha.Visibility = Visibility.Visible;
                txtSenha.BorderBrush = Brushes.Red;
                txtSenhaVisible.BorderBrush = Brushes.Red;
            }
            else if (senha.Length > 20)
            {
                txtErroSenha.Text = "Senha deve ter no máximo 20 caracteres";
                txtErroSenha.Visibility = Visibility.Visible;
                txtSenha.BorderBrush = Brushes.Red;
                txtSenhaVisible.BorderBrush = Brushes.Red;
            }
            else if (!senha.Any(char.IsLetter))
            {
                txtErroSenha.Text = "Senha deve conter pelo menos uma letra";
                txtErroSenha.Visibility = Visibility.Visible;
                txtSenha.BorderBrush = Brushes.Red;
                txtSenhaVisible.BorderBrush = Brushes.Red;
            }
            else if (!senha.Any(char.IsDigit))
            {
                txtErroSenha.Text = "Senha deve conter pelo menos um número";
                txtErroSenha.Visibility = Visibility.Visible;
                txtSenha.BorderBrush = Brushes.Red;
                txtSenhaVisible.BorderBrush = Brushes.Red;
            }
            else
            {
                txtErroSenha.Visibility = Visibility.Collapsed;
                txtSenha.ClearValue(Border.BorderBrushProperty);
                txtSenhaVisible.ClearValue(Border.BorderBrushProperty);
            }

            ValidarConfirmacaoSenha();
        }

        private void ValidarConfirmaSenha(object sender, RoutedEventArgs e)
        {
            if (!isConfirmaSenhaVisible)
            {
                ValidarConfirmacaoSenha();
            }
        }

        private void ValidarConfirmaSenhaVisivel(object sender, TextChangedEventArgs e)
        {
            if (isConfirmaSenhaVisible)
            {
                ValidarConfirmacaoSenha();
            }
        }

        private void ValidarConfirmacaoSenha()
        {
            string senha = ObterSenhaCadastro();
            string confirmaSenha = ObterConfirmaSenha();

            if (string.IsNullOrEmpty(confirmaSenha))
            {
                txtErroConfirmaSenha.Text = "Confirmação de senha é obrigatória";
                txtErroConfirmaSenha.Visibility = Visibility.Visible;
                txtConfirmaSenha.BorderBrush = Brushes.Red;
                txtConfirmaSenhaVisible.BorderBrush = Brushes.Red;
            }
            else if (confirmaSenha.Length < 8)
            {
                txtErroConfirmaSenha.Text = "Confirmação deve ter pelo menos 8 caracteres";
                txtErroConfirmaSenha.Visibility = Visibility.Visible;
                txtConfirmaSenha.BorderBrush = Brushes.Red;
                txtConfirmaSenhaVisible.BorderBrush = Brushes.Red;
            }
            else if (confirmaSenha.Length > 20)
            {
                txtErroConfirmaSenha.Text = "Confirmação deve ter no máximo 20 caracteres";
                txtErroConfirmaSenha.Visibility = Visibility.Visible;
                txtConfirmaSenha.BorderBrush = Brushes.Red;
                txtConfirmaSenhaVisible.BorderBrush = Brushes.Red;
            }
            else if (!confirmaSenha.Any(char.IsLetter))
            {
                txtErroConfirmaSenha.Text = "Confirmação deve conter pelo menos uma letra";
                txtErroConfirmaSenha.Visibility = Visibility.Visible;
                txtConfirmaSenha.BorderBrush = Brushes.Red;
                txtConfirmaSenhaVisible.BorderBrush = Brushes.Red;
            }
            else if (!confirmaSenha.Any(char.IsDigit))
            {
                txtErroConfirmaSenha.Text = "Confirmação deve conter pelo menos um número";
                txtErroConfirmaSenha.Visibility = Visibility.Visible;
                txtConfirmaSenha.BorderBrush = Brushes.Red;
                txtConfirmaSenhaVisible.BorderBrush = Brushes.Red;
            }
            else if (senha != confirmaSenha)
            {
                txtErroConfirmaSenha.Text = "Senhas não coincidem";
                txtErroConfirmaSenha.Visibility = Visibility.Visible;
                txtConfirmaSenha.BorderBrush = Brushes.Red;
                txtConfirmaSenhaVisible.BorderBrush = Brushes.Red;
            }
            else
            {
                txtErroConfirmaSenha.Visibility = Visibility.Collapsed;
                txtConfirmaSenha.ClearValue(Border.BorderBrushProperty);
                txtConfirmaSenhaVisible.ClearValue(Border.BorderBrushProperty);
            }
        }

        // MÉTODOS AUXILIARES
        private string FormatarTelefone(string numeros)
        {
            if (numeros.Length == 0) return "";
            if (numeros.Length <= 2) return $"({numeros}";
            if (numeros.Length <= 3) return $"({numeros.Substring(0, 2)}) {numeros.Substring(2)}";
            if (numeros.Length <= 7) return $"({numeros.Substring(0, 2)}) {numeros.Substring(2, 1)} {numeros.Substring(3)}";
            if (numeros.Length <= 11) return $"({numeros.Substring(0, 2)}) {numeros.Substring(2, 1)} {numeros.Substring(3, 4)}-{numeros.Substring(7)}";
            return $"({numeros.Substring(0, 2)}) {numeros.Substring(2, 1)} {numeros.Substring(3, 4)}-{numeros.Substring(7, 4)}";
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var regex = new Regex(@"^[a-zA-Z0-9._]+@[a-zA-Z0-9]+\.[a-zA-Z]{2,}$");
                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        // MÉTODOS DE VERIFICAÇÃO NO BANCO DE DADOS
        private bool EmailJaExiste(string email, int? excluirCod = null)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT COUNT(*) FROM usuariosCadastrados WHERE email = @email";
                    if (excluirCod.HasValue)
                    {
                        query += " AND cod != @cod";
                    }
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@email", email);
                    if (excluirCod.HasValue)
                    {
                        command.Parameters.AddWithValue("@cod", excluirCod.Value);
                    }
                    int count = Convert.ToInt32(command.ExecuteScalar());
                    return count > 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao verificar email: " + ex.Message, "Erro",
                               MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private bool TelefoneJaExiste(string telefone, int? excluirCod = null)
        {
            try
            {
                string telefoneSomenteNumeros = Regex.Replace(telefone, @"[^\d]", "");
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT COUNT(*) FROM usuariosCadastrados WHERE telefone = @telefone";
                    if (excluirCod.HasValue)
                    {
                        query += " AND cod != @cod";
                    }
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@telefone", telefoneSomenteNumeros);
                    if (excluirCod.HasValue)
                    {
                        command.Parameters.AddWithValue("@cod", excluirCod.Value);
                    }
                    int count = Convert.ToInt32(command.ExecuteScalar());
                    return count > 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao verificar telefone: " + ex.Message, "Erro",
                               MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private bool NomeUsuarioJaExiste(string nome, int? excluirCod = null)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT COUNT(*) FROM usuariosCadastrados WHERE LOWER(usuario) = LOWER(@nome)";
                    if (excluirCod.HasValue)
                    {
                        query += " AND cod != @cod";
                    }
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@nome", nome.Trim());
                    if (excluirCod.HasValue)
                    {
                        command.Parameters.AddWithValue("@cod", excluirCod.Value);
                    }
                    int count = Convert.ToInt32(command.ExecuteScalar());
                    return count > 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao verificar nome de usuário: " + ex.Message, "Erro",
                               MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private bool TodosCamposCadastroValidos()
        {
            return txtErroNome.Visibility == Visibility.Collapsed &&
                   txtErroEmail.Visibility == Visibility.Collapsed &&
                   txtErroTelefone.Visibility == Visibility.Collapsed &&
                   txtErroSenha.Visibility == Visibility.Collapsed &&
                   txtErroConfirmaSenha.Visibility == Visibility.Collapsed;
        }

        private bool TodosCamposLoginValidos()
        {
            return txtErroEmailLOGIN.Visibility == Visibility.Collapsed &&
                   txtErroSenhaLOGIN.Visibility == Visibility.Collapsed;
        }

        // EVENTOS PARA SINCRONIZAÇÃO EM TEMPO REAL - LOGIN
        private void TxtSenhaLOGIN_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!isSenhaLOGINVisible && txtSenhaLOGINVisible != null)
            {
                txtSenhaLOGINVisible.Text = txtSenhaLOGIN.Password;
            }
        }

        private void TxtSenhaLOGINVisible_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isSenhaLOGINVisible && txtSenhaLOGIN != null)
            {
                txtSenhaLOGIN.Password = txtSenhaLOGINVisible.Text;
            }
        }

        // Eventos para sincronizar o conteúdo em tempo real - CADASTRO SENHA
        private void TxtSenha_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!isSenhaVisible && txtSenhaVisible != null)
            {
                txtSenhaVisible.Text = txtSenha.Password;
            }
        }

        private void TxtSenhaVisible_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isSenhaVisible && txtSenha != null)
            {
                txtSenha.Password = txtSenhaVisible.Text;
            }
        }

        // Eventos para sincronizar o conteúdo em tempo real - CONFIRMAR SENHA
        private void TxtConfirmaSenha_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!isConfirmaSenhaVisible && txtConfirmaSenhaVisible != null)
            {
                txtConfirmaSenhaVisible.Text = txtConfirmaSenha.Password;
            }
        }

        private void TxtConfirmaSenhaVisible_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isConfirmaSenhaVisible && txtConfirmaSenha != null)
            {
                txtConfirmaSenha.Password = txtConfirmaSenhaVisible.Text;
            }
        }

        // MÉTODOS AUXILIARES PARA OBTER SENHAS
        private string ObterSenhaLogin()
        {
            return isSenhaLOGINVisible ? txtSenhaLOGINVisible.Text : txtSenhaLOGIN.Password;
        }

        private string ObterSenhaCadastro()
        {
            return isSenhaVisible ? txtSenhaVisible.Text : txtSenha.Password;
        }

        private string ObterConfirmaSenha()
        {
            return isConfirmaSenhaVisible ? txtConfirmaSenhaVisible.Text : txtConfirmaSenha.Password;
        }

        // Classe estática que armazena os dados do usuário logado
        public static class SessaoUsuario
        {
            public static int Cod { get; set; }
            public static string Nome { get; set; }
            public static string Email { get; set; }
            public static string Telefone { get; set; }
            public static string Regiao { get; set; }
        }

        // Alterna para a aba de Login
        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            LoginPanel.Visibility = Visibility.Visible;
            CadastroPanel.Visibility = Visibility.Collapsed;
            SetLoginActive();
            LimparErrosLogin();
        }

        // Alterna para a aba de Cadastro
        private void BtnCadastro_Click(object sender, RoutedEventArgs e)
        {
            LoginPanel.Visibility = Visibility.Collapsed;
            CadastroPanel.Visibility = Visibility.Visible;
            SetCadastroActive();
            LimparErrosCadastro();
        }

        private void LimparErrosLogin()
        {
            txtErroEmailLOGIN.Visibility = Visibility.Collapsed;
            txtErroSenhaLOGIN.Visibility = Visibility.Collapsed;
            txtSucessoLogin.Visibility = Visibility.Collapsed;
            txtEmailLOGIN.ClearValue(Border.BorderBrushProperty);
            txtSenhaLOGIN.ClearValue(Border.BorderBrushProperty);
            txtSenhaLOGINVisible.ClearValue(Border.BorderBrushProperty);
        }

        private void LimparErrosCadastro()
        {
            txtErroNome.Visibility = Visibility.Collapsed;
            txtErroEmail.Visibility = Visibility.Collapsed;
            txtErroTelefone.Visibility = Visibility.Collapsed;
            txtErroSenha.Visibility = Visibility.Collapsed;
            txtErroConfirmaSenha.Visibility = Visibility.Collapsed;
            txtNome.ClearValue(Border.BorderBrushProperty);
            txtEmail.ClearValue(Border.BorderBrushProperty);
            txtTelefone.ClearValue(Border.BorderBrushProperty);
            txtSenha.ClearValue(Border.BorderBrushProperty);
            txtSenhaVisible.ClearValue(Border.BorderBrushProperty);
            txtConfirmaSenha.ClearValue(Border.BorderBrushProperty);
            txtConfirmaSenhaVisible.ClearValue(Border.BorderBrushProperty);
        }

        // MÉTODO DE LOGIN MODIFICADO PARA USAR HASH
        private void BtnEntrar_Click(object sender, RoutedEventArgs e)
        {
            // Forçar validação dos campos
            ValidarEmailLogin(null, null);
            ValidarSenhaLoginComum(ObterSenhaLogin());

            // Verificar se todos os campos são válidos
            if (!TodosCamposLoginValidos())
            {
                MessageBox.Show("Por favor, corrija os erros antes de entrar.", "Erro de Validação",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string email = txtEmailLOGIN.Text.Trim();
            string senhaDigitada = ObterSenhaLogin();

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // MODIFICADO: Buscar o hash da senha armazenado no banco
                    string query = "SELECT cod, usuario, telefone, senha FROM usuariosCadastrados WHERE email = @Email";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Email", email);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string hashArmazenado = reader.GetString("senha");

                            // MODIFICADO: Verificar se a senha digitada corresponde ao hash
                            if (VerificarSenha(senhaDigitada, hashArmazenado))
                            {
                                // Preenche os dados do usuário na sessão
                                SessaoUsuario.Cod = reader.GetInt32("cod");
                                SessaoUsuario.Nome = reader.GetString("usuario");
                                SessaoUsuario.Email = email;
                                SessaoUsuario.Telefone = reader.GetString("telefone");

                                // Mostra ícone de sucesso e abre a janela principal
                                txtSucessoLogin.Visibility = Visibility.Visible;
                                txtSucessoLogin.ToolTip = "Login bem-sucedido!";

                                PrincipalWindow1 novaJanela = new PrincipalWindow1();
                                novaJanela.Show();
                                this.Hide();
                            }
                            else
                            {
                                // Senha incorreta
                                txtErroEmailLOGIN.Text = "Email ou senha incorretos";
                                txtErroEmailLOGIN.Visibility = Visibility.Visible;
                                txtErroSenhaLOGIN.Text = "Email ou senha incorretos";
                                txtErroSenhaLOGIN.Visibility = Visibility.Visible;
                                txtEmailLOGIN.BorderBrush = Brushes.Red;
                                txtSenhaLOGIN.BorderBrush = Brushes.Red;
                                txtSenhaLOGINVisible.BorderBrush = Brushes.Red;
                            }
                        }
                        else
                        {
                            // Email não encontrado
                            txtErroEmailLOGIN.Text = "Email ou senha incorretos";
                            txtErroEmailLOGIN.Visibility = Visibility.Visible;
                            txtErroSenhaLOGIN.Text = "Email ou senha incorretos";
                            txtErroSenhaLOGIN.Visibility = Visibility.Visible;
                            txtEmailLOGIN.BorderBrush = Brushes.Red;
                            txtSenhaLOGIN.BorderBrush = Brushes.Red;
                            txtSenhaLOGINVisible.BorderBrush = Brushes.Red;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao conectar ao banco de dados: " + ex.Message, "Erro",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // MÉTODO DE CADASTRO MODIFICADO PARA USAR HASH
        private void BtnCadastrar_Click(object sender, RoutedEventArgs e)
        {
            // Forçar validação de todos os campos
            ValidarNome(null, null);
            ValidarEmail(null, null);
            ValidarTelefone(null, null);
            ValidarSenhaComum(ObterSenhaCadastro());
            ValidarConfirmacaoSenha();

            // Verificar se todos os campos são válidos
            if (!TodosCamposCadastroValidos())
            {
                MessageBox.Show("Por favor, corrija os erros antes de cadastrar.", "Erro de Validação",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Captura os dados preenchidos
            string nome = txtNome.Text.Trim();
            string email = txtEmail.Text.Trim();
            string senhaTextoPlano = ObterSenhaCadastro().Trim();
            string telefone = txtTelefone.Text.Trim();

            // MODIFICADO: Criptografar a senha antes de salvar
            string senhaHash = CriptografarSenha(senhaTextoPlano);

            if (senhaHash == null)
            {
                MessageBox.Show("Erro ao processar a senha. Tente novamente.", "Erro",
                               MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Extrair apenas os números do telefone para salvar no banco
            string telefoneSomenteNumeros = Regex.Replace(telefone, @"[^\d]", "");

            try
            {
                using (Conexao = new MySqlConnection(connectionString))
                {
                    Conexao.Open();

                    // MODIFICADO: Inserir o hash da senha no banco
                    string sql = "INSERT INTO usuariosCadastrados (usuario, email, senha, telefone) " +
                                 "VALUES (@nome, @email, @senhaHash, @telefone)";
                    MySqlCommand comando = new MySqlCommand(sql, Conexao);
                    comando.Parameters.AddWithValue("@nome", nome);
                    comando.Parameters.AddWithValue("@email", email);
                    comando.Parameters.AddWithValue("@senhaHash", senhaHash); // MODIFICADO: usar hash
                    comando.Parameters.AddWithValue("@telefone", telefoneSomenteNumeros);

                    comando.ExecuteNonQuery();

                    MessageBox.Show("Cadastro realizado com sucesso!\n\nSua senha foi criptografada com segurança.", "Sucesso",
                                   MessageBoxButton.OK, MessageBoxImage.Information);

                    // Limpa os campos do formulário
                    txtNome.Clear();
                    txtEmail.Clear();
                    txtSenha.Clear();
                    txtSenhaVisible.Clear();
                    txtConfirmaSenha.Clear();
                    txtConfirmaSenhaVisible.Clear();
                    txtTelefone.Clear();

                    // Muda para a tela de login
                    BtnLogin_Click(sender, e);
                }
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1062)
                {
                    MessageBox.Show("Dados já existem no sistema. Verifique email, telefone ou nome de usuário.", "Dados Duplicados",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show("Erro ao cadastrar: " + ex.Message, "Erro",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao cadastrar: " + ex.Message, "Erro",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // MÉTODOS DE TOGGLE PARA SENHAS
        private void BtnToggleSenhaLOGIN_Click(object sender, RoutedEventArgs e)
        {
            if (isSenhaLOGINVisible)
            {
                txtSenhaLOGIN.Password = txtSenhaLOGINVisible.Text;
                txtSenhaLOGIN.Visibility = Visibility.Visible;
                txtSenhaLOGINVisible.Visibility = Visibility.Collapsed;
                btnToggleSenhaLOGIN.Content = "🚫";
                btnToggleSenhaLOGIN.ToolTip = "Mostrar senha";
                isSenhaLOGINVisible = false;
                txtSenhaLOGIN.Focus();
            }
            else
            {
                txtSenhaLOGINVisible.Text = txtSenhaLOGIN.Password;
                txtSenhaLOGIN.Visibility = Visibility.Collapsed;
                txtSenhaLOGINVisible.Visibility = Visibility.Visible;
                btnToggleSenhaLOGIN.Content = "👁️";
                btnToggleSenhaLOGIN.ToolTip = "Ocultar senha";
                isSenhaLOGINVisible = true;
                txtSenhaLOGINVisible.Focus();
                txtSenhaLOGINVisible.CaretIndex = txtSenhaLOGINVisible.Text.Length;
            }
        }

        private void BtnToggleSenha_Click(object sender, RoutedEventArgs e)
        {
            if (isSenhaVisible)
            {
                txtSenha.Password = txtSenhaVisible.Text;
                txtSenha.Visibility = Visibility.Visible;
                txtSenhaVisible.Visibility = Visibility.Collapsed;
                btnToggleSenha.Content = "🚫";
                btnToggleSenha.ToolTip = "Mostrar senha";
                isSenhaVisible = false;
                txtSenha.Focus();
            }
            else
            {
                txtSenhaVisible.Text = txtSenha.Password;
                txtSenha.Visibility = Visibility.Collapsed;
                txtSenhaVisible.Visibility = Visibility.Visible;
                btnToggleSenha.Content = "👁️";
                btnToggleSenha.ToolTip = "Ocultar senha";
                isSenhaVisible = true;
                txtSenhaVisible.Focus();
                txtSenhaVisible.CaretIndex = txtSenhaVisible.Text.Length;
            }
        }

        private void BtnToggleConfirmaSenha_Click(object sender, RoutedEventArgs e)
        {
            if (isConfirmaSenhaVisible)
            {
                txtConfirmaSenha.Password = txtConfirmaSenhaVisible.Text;
                txtConfirmaSenha.Visibility = Visibility.Visible;
                txtConfirmaSenhaVisible.Visibility = Visibility.Collapsed;
                btnToggleConfirmaSenha.Content = "🚫";
                btnToggleConfirmaSenha.ToolTip = "Mostrar senha";
                isConfirmaSenhaVisible = false;
                txtConfirmaSenha.Focus();
            }
            else
            {
                txtConfirmaSenhaVisible.Text = txtConfirmaSenha.Password;
                txtConfirmaSenha.Visibility = Visibility.Collapsed;
                txtConfirmaSenhaVisible.Visibility = Visibility.Visible;
                btnToggleConfirmaSenha.Content = "👁️";
                btnToggleConfirmaSenha.ToolTip = "Ocultar senha";
                isConfirmaSenhaVisible = true;
                txtConfirmaSenhaVisible.Focus();
                txtConfirmaSenhaVisible.CaretIndex = txtConfirmaSenhaVisible.Text.Length;
            }
        }

        private void BtnirpgPrincipal_Click(object sender, RoutedEventArgs e)
        {
            PrincipalWindow1 novaJanela = new PrincipalWindow1();
            novaJanela.Show();
            this.Hide();
        }

        private void BtnEsqueci_Click(object sender, RoutedEventArgs e)
        {
            EsqueciSenha novaJanela = new EsqueciSenha();
            novaJanela.Show();
            this.Hide();
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                MoveFocusToNextElement();
            }
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                MoveFocusToNextElement();
            }
        }

        private void LastPasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                BtnCadastrar_Click(sender, e);
            }
        }

        private void MoveFocusToNextElement()
        {
            var request = new TraversalRequest(FocusNavigationDirection.Next);
            if (Keyboard.FocusedElement is UIElement currentElement)
            {
                currentElement.MoveFocus(request);
            }
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            if (e.Key == Key.Enter && (Keyboard.FocusedElement == txtSenhaLOGIN || Keyboard.FocusedElement == txtSenhaLOGINVisible))
            {
                e.Handled = true;
                BtnEntrar_Click(this, new RoutedEventArgs());
            }
        }

        private void SetLoginActive()
        {
            btnLogin.Style = (Style)FindResource("AzulHoverButtonStyle");
            btnCadastro.Style = (Style)FindResource("AmareloHoverButtonStyle");
        }

        private void SetCadastroActive()
        {
            btnLogin.Style = (Style)FindResource("AmareloHoverButtonStyle");
            btnCadastro.Style = (Style)FindResource("AzulHoverButtonStyle");
        }
    }
}
