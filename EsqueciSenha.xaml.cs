using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MySql.Data.MySqlClient;
using System.Linq;
using BCrypt.Net; // Adicione esta referência para criptografia

namespace TCC
{
    public partial class EsqueciSenha : Window
    {
        // String de conexão com o banco de dados MySQL
        private string connectionString = "Server=localhost;Database=Sistema;Uid=root;Pwd=;";

        // NOVAS VARIÁVEIS PARA CONTROLE DE VISIBILIDADE DAS SENHAS
        private bool isSenhaVisible = false;
        private bool isConfirmaSenhaVisible = false;

        // Construtor da janela "EsqueciSenha"
        public EsqueciSenha()
        {
            InitializeComponent();
            // Define o foco inicial no campo de email
            txtEmailRecuperacao.Focus();
            // Configurar eventos de validação
            ConfigurarEventosValidacao();
            // Configurar eventos de sincronização
            ConfigurarEventosSincronizacao();
            // Configurar eventos de tecla para navegação
            ConfigurarEventosTecla();
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

        // NOVO MÉTODO PARA CONFIGURAR EVENTOS DE VALIDAÇÃO
        private void ConfigurarEventosValidacao()
        {
            // Eventos para validação em tempo real
            txtEmailRecuperacao.TextChanged += ValidarEmail;
            txtNovaSenha.PasswordChanged += ValidarSenha;
            txtSenhaVisible.TextChanged += ValidarSenhaVisivel;
            txtConfirmaNovaSenha.PasswordChanged += ValidarConfirmaSenha;
            txtConfirmaSenhaVisible.TextChanged += ValidarConfirmaSenhaVisivel;
        }

        // MÉTODO PARA CONFIGURAR EVENTOS DE SINCRONIZAÇÃO
        private void ConfigurarEventosSincronizacao()
        {
            // Adicionar eventos para sincronizar o texto em tempo real
            if (txtNovaSenha != null && txtSenhaVisible != null)
            {
                txtNovaSenha.PasswordChanged += TxtNovaSenha_PasswordChanged;
                txtSenhaVisible.TextChanged += TxtSenhaVisible_TextChanged;
            }
            if (txtConfirmaNovaSenha != null && txtConfirmaSenhaVisible != null)
            {
                txtConfirmaNovaSenha.PasswordChanged += TxtConfirmaNovaSenha_PasswordChanged;
                txtConfirmaSenhaVisible.TextChanged += TxtConfirmaSenhaVisible_TextChanged;
            }
        }

        // MÉTODO PARA CONFIGURAR EVENTOS DE TECLA
        private void ConfigurarEventosTecla()
        {
            // Define o que acontece quando pressionar Enter nos campos
            txtEmailRecuperacao.KeyDown += TextBox_KeyDown;
            txtNovaSenha.KeyDown += PasswordBox_KeyDown;
            txtConfirmaNovaSenha.KeyDown += LastPasswordBox_KeyDown;
            txtSenhaVisible.KeyDown += TextBox_KeyDown;
            txtConfirmaSenhaVisible.KeyDown += LastPasswordBox_KeyDown;
        }

        // MÉTODOS DE VALIDAÇÃO
        private void ValidarEmail(object sender, TextChangedEventArgs e)
        {
            string email = txtEmailRecuperacao.Text.Trim();
            if (string.IsNullOrEmpty(email))
            {
                txtErroEmail.Text = "Email é obrigatório";
                txtErroEmail.Visibility = Visibility.Visible;
                txtEmailRecuperacao.BorderBrush = Brushes.Red;
            }
            else if (!email.Contains("@"))
            {
                txtErroEmail.Text = "Email deve conter @";
                txtErroEmail.Visibility = Visibility.Visible;
                txtEmailRecuperacao.BorderBrush = Brushes.Red;
            }
            else if (!IsValidEmail(email))
            {
                txtErroEmail.Text = "Formato de email inválido";
                txtErroEmail.Visibility = Visibility.Visible;
                txtEmailRecuperacao.BorderBrush = Brushes.Red;
            }
            else if (!EmailExisteNoBanco(email))
            {
                txtErroEmail.Text = "Email não cadastrado no sistema";
                txtErroEmail.Visibility = Visibility.Visible;
                txtEmailRecuperacao.BorderBrush = Brushes.Red;
            }
            else
            {
                txtErroEmail.Visibility = Visibility.Collapsed;
                txtEmailRecuperacao.ClearValue(Border.BorderBrushProperty);
            }
        }

        private void ValidarSenha(object sender, RoutedEventArgs e)
        {
            if (!isSenhaVisible)
            {
                ValidarSenhaComum(txtNovaSenha.Password);
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
                txtNovaSenha.BorderBrush = Brushes.Red;
                txtSenhaVisible.BorderBrush = Brushes.Red;
            }
            else if (senha.Length < 8)
            {
                txtErroSenha.Text = "Senha deve ter pelo menos 8 caracteres";
                txtErroSenha.Visibility = Visibility.Visible;
                txtNovaSenha.BorderBrush = Brushes.Red;
                txtSenhaVisible.BorderBrush = Brushes.Red;
            }
            else if (senha.Length > 20)
            {
                txtErroSenha.Text = "Senha deve ter no máximo 20 caracteres";
                txtErroSenha.Visibility = Visibility.Visible;
                txtNovaSenha.BorderBrush = Brushes.Red;
                txtSenhaVisible.BorderBrush = Brushes.Red;
            }
            else if (!senha.Any(char.IsLetter))
            {
                txtErroSenha.Text = "Senha deve conter pelo menos uma letra";
                txtErroSenha.Visibility = Visibility.Visible;
                txtNovaSenha.BorderBrush = Brushes.Red;
                txtSenhaVisible.BorderBrush = Brushes.Red;
            }
            else if (!senha.Any(char.IsDigit))
            {
                txtErroSenha.Text = "Senha deve conter pelo menos um número";
                txtErroSenha.Visibility = Visibility.Visible;
                txtNovaSenha.BorderBrush = Brushes.Red;
                txtSenhaVisible.BorderBrush = Brushes.Red;
            }
            else
            {
                txtErroSenha.Visibility = Visibility.Collapsed;
                txtNovaSenha.ClearValue(Border.BorderBrushProperty);
                txtSenhaVisible.ClearValue(Border.BorderBrushProperty);
            }

            // Revalidar confirmação de senha quando a senha principal muda
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
            string senha = ObterNovaSenha();
            string confirmaSenha = ObterConfirmaSenha();

            if (string.IsNullOrEmpty(confirmaSenha))
            {
                txtErroConfirmaSenha.Text = "Confirmação de senha é obrigatória";
                txtErroConfirmaSenha.Visibility = Visibility.Visible;
                txtConfirmaNovaSenha.BorderBrush = Brushes.Red;
                txtConfirmaSenhaVisible.BorderBrush = Brushes.Red;
            }
            else if (confirmaSenha.Length < 8)
            {
                txtErroConfirmaSenha.Text = "Confirmação deve ter pelo menos 8 caracteres";
                txtErroConfirmaSenha.Visibility = Visibility.Visible;
                txtConfirmaNovaSenha.BorderBrush = Brushes.Red;
                txtConfirmaSenhaVisible.BorderBrush = Brushes.Red;
            }
            else if (confirmaSenha.Length > 20)
            {
                txtErroConfirmaSenha.Text = "Confirmação deve ter no máximo 20 caracteres";
                txtErroConfirmaSenha.Visibility = Visibility.Visible;
                txtConfirmaNovaSenha.BorderBrush = Brushes.Red;
                txtConfirmaSenhaVisible.BorderBrush = Brushes.Red;
            }
            else if (!confirmaSenha.Any(char.IsLetter))
            {
                txtErroConfirmaSenha.Text = "Confirmação deve conter pelo menos uma letra";
                txtErroConfirmaSenha.Visibility = Visibility.Visible;
                txtConfirmaNovaSenha.BorderBrush = Brushes.Red;
                txtConfirmaSenhaVisible.BorderBrush = Brushes.Red;
            }
            else if (!confirmaSenha.Any(char.IsDigit))
            {
                txtErroConfirmaSenha.Text = "Confirmação deve conter pelo menos um número";
                txtErroConfirmaSenha.Visibility = Visibility.Visible;
                txtConfirmaNovaSenha.BorderBrush = Brushes.Red;
                txtConfirmaSenhaVisible.BorderBrush = Brushes.Red;
            }
            else if (senha != confirmaSenha)
            {
                txtErroConfirmaSenha.Text = "Senhas não coincidem";
                txtErroConfirmaSenha.Visibility = Visibility.Visible;
                txtConfirmaNovaSenha.BorderBrush = Brushes.Red;
                txtConfirmaSenhaVisible.BorderBrush = Brushes.Red;
            }
            else
            {
                txtErroConfirmaSenha.Visibility = Visibility.Collapsed;
                txtConfirmaNovaSenha.ClearValue(Border.BorderBrushProperty);
                txtConfirmaSenhaVisible.ClearValue(Border.BorderBrushProperty);
            }
        }

        // MÉTODOS AUXILIARES
        // Verificar se o email existe no banco de dados
        private bool EmailExisteNoBanco(string email)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT COUNT(*) FROM usuariosCadastrados WHERE email = @email";
                    MySqlCommand command = new MySqlCommand(query, connection);
                    command.Parameters.AddWithValue("@email", email);
                    int count = Convert.ToInt32(command.ExecuteScalar());
                    return count > 0;
                }
            }
            catch (Exception)
            {
                // Em caso de erro de conexão, não validamos o email
                return true;
            }
        }

        // Validar formato de email mais rigoroso
        private bool IsValidEmail(string email)
        {
            try
            {
                var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        // Verificar se todos os campos são válidos
        private bool TodosCamposValidos()
        {
            return txtErroEmail.Visibility == Visibility.Collapsed &&
                   txtErroSenha.Visibility == Visibility.Collapsed &&
                   txtErroConfirmaSenha.Visibility == Visibility.Collapsed;
        }

        // EVENTOS PARA SINCRONIZAÇÃO EM TEMPO REAL - NOVA SENHA
        private void TxtNovaSenha_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!isSenhaVisible && txtSenhaVisible != null && txtNovaSenha.IsEnabled)
            {
                txtSenhaVisible.Text = txtNovaSenha.Password;
            }
        }

        private void TxtSenhaVisible_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isSenhaVisible && txtNovaSenha != null)
            {
                txtNovaSenha.Password = txtSenhaVisible.Text;
            }
        }

        // EVENTOS PARA SINCRONIZAÇÃO EM TEMPO REAL - CONFIRMAR SENHA
        private void TxtConfirmaNovaSenha_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!isConfirmaSenhaVisible && txtConfirmaSenhaVisible != null && txtConfirmaNovaSenha.IsEnabled)
            {
                txtConfirmaSenhaVisible.Text = txtConfirmaNovaSenha.Password;
            }
        }

        private void TxtConfirmaSenhaVisible_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isConfirmaSenhaVisible && txtConfirmaNovaSenha != null)
            {
                txtConfirmaNovaSenha.Password = txtConfirmaSenhaVisible.Text;
            }
        }

        // MÉTODOS AUXILIARES PARA OBTER SENHAS
        private string ObterNovaSenha()
        {
            if (isSenhaVisible && txtSenhaVisible != null)
            {
                return txtSenhaVisible.Text;
            }
            else if (txtNovaSenha != null)
            {
                return txtNovaSenha.Password;
            }
            return "";
        }

        private string ObterConfirmaSenha()
        {
            if (isConfirmaSenhaVisible && txtConfirmaSenhaVisible != null)
            {
                return txtConfirmaSenhaVisible.Text;
            }
            else if (txtConfirmaNovaSenha != null)
            {
                return txtConfirmaNovaSenha.Password;
            }
            return "";
        }

        // MÉTODOS PARA ALTERNAR VISIBILIDADE DAS SENHAS
        private void BtnToggleSenha_Click(object sender, RoutedEventArgs e)
        {
            if (isSenhaVisible)
            {
                // Ocultar senha - mostrar PasswordBox
                txtNovaSenha.Password = txtSenhaVisible.Text;
                txtNovaSenha.Visibility = Visibility.Visible;
                txtSenhaVisible.Visibility = Visibility.Collapsed;
                btnToggleSenha.Content = "🚫";
                btnToggleSenha.ToolTip = "Mostrar senha";
                isSenhaVisible = false;
                txtNovaSenha.Focus();
            }
            else
            {
                // Mostrar senha - mostrar TextBox
                txtSenhaVisible.Text = txtNovaSenha.Password;
                txtNovaSenha.Visibility = Visibility.Collapsed;
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
                // Ocultar senha - mostrar PasswordBox
                txtConfirmaNovaSenha.Password = txtConfirmaSenhaVisible.Text;
                txtConfirmaNovaSenha.Visibility = Visibility.Visible;
                txtConfirmaSenhaVisible.Visibility = Visibility.Collapsed;
                btnToggleConfirmaSenha.Content = "🚫";
                btnToggleConfirmaSenha.ToolTip = "Mostrar senha";
                isConfirmaSenhaVisible = false;
                txtConfirmaNovaSenha.Focus();
            }
            else
            {
                // Mostrar senha - mostrar TextBox
                txtConfirmaSenhaVisible.Text = txtConfirmaNovaSenha.Password;
                txtConfirmaNovaSenha.Visibility = Visibility.Collapsed;
                txtConfirmaSenhaVisible.Visibility = Visibility.Visible;
                btnToggleConfirmaSenha.Content = "👁️";
                btnToggleConfirmaSenha.ToolTip = "Ocultar senha";
                isConfirmaSenhaVisible = true;
                txtConfirmaSenhaVisible.Focus();
                txtConfirmaSenhaVisible.CaretIndex = txtConfirmaSenhaVisible.Text.Length;
            }
        }

        // Botão que volta para a tela principal (MainWindow)
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show(); // Abre a janela principal
            this.Close();      // Fecha a janela atual
        }

        // MÉTODO MODIFICADO PARA ALTERAR SENHA COM CRIPTOGRAFIA
        private void BtnAlterarSenha_Click(object sender, RoutedEventArgs e)
        {
            // Forçar validação de todos os campos
            ValidarEmail(null, null);
            ValidarSenhaComum(ObterNovaSenha());
            ValidarConfirmacaoSenha();

            // Verificar se todos os campos são válidos
            if (!TodosCamposValidos())
            {
                MessageBox.Show("Por favor, corrija os erros antes de alterar a senha.", "Erro de Validação",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Captura os dados inseridos pelo usuário
            string email = txtEmailRecuperacao.Text.Trim();
            string novaSenhaTextoPlano = ObterNovaSenha().Trim();

            // MODIFICADO: Criptografar a nova senha antes de salvar
            string novaSenhaHash = CriptografarSenha(novaSenhaTextoPlano);

            if (novaSenhaHash == null)
            {
                MessageBox.Show("Erro ao processar a nova senha. Tente novamente.", "Erro",
                               MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Bloco de tentativa para conectar ao banco de dados e alterar a senha
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // MODIFICADO: Atualiza o hash da senha no banco
                    string atualizarSenhaQuery = "UPDATE usuariosCadastrados SET senha = @NovaSenhaHash WHERE email = @Email";
                    MySqlCommand atualizarCmd = new MySqlCommand(atualizarSenhaQuery, connection);
                    atualizarCmd.Parameters.AddWithValue("@NovaSenhaHash", novaSenhaHash); // MODIFICADO: usar hash
                    atualizarCmd.Parameters.AddWithValue("@Email", email);

                    int rowsAffected = atualizarCmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Senha alterada com sucesso!\n\nSua nova senha foi criptografada com segurança.", "Sucesso",
                                       MessageBoxButton.OK, MessageBoxImage.Information);

                        // Após a alteração, retorna para a tela principal
                        MainWindow mainWindow = new MainWindow();
                        mainWindow.Show();
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("Não foi possível alterar a senha. Verifique se o email está correto.",
                                       "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                // Exibe mensagem de erro caso algo dê errado
                MessageBox.Show("Erro ao alterar senha: " + ex.Message, "Erro",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Quando o usuário pressiona Enter no campo de texto, move para o próximo campo
        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                MoveFocusToNextElement();
            }
        }

        // Quando o usuário pressiona Enter no campo de senha, move para o próximo campo
        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                MoveFocusToNextElement();
            }
        }

        // Quando o usuário pressiona Enter no campo de confirmação, executa a função de alterar senha
        private void LastPasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                BtnAlterarSenha_Click(sender, e);
            }
        }

        // Move o foco para o próximo elemento na tela (campo ou botão)
        private void MoveFocusToNextElement()
        {
            var request = new TraversalRequest(FocusNavigationDirection.Next);
            if (Keyboard.FocusedElement is UIElement currentElement)
            {
                currentElement.MoveFocus(request);
            }
        }
    }
}
