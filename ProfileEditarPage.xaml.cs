// Importação dos namespaces necessários
using System.Windows;
using System.Windows.Controls;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Common;
using System.Text.RegularExpressions;
using System.Linq;
using BCrypt.Net; // Adicione esta referência para criptografia

namespace TCC
{
    public partial class ProfileEditarPage : Page
    {
        // String de conexão com o banco MySQL
        private string connectionString = "Server=localhost;Database=Sistema;Uid=root;Pwd=;";

        // NOVAS VARIÁVEIS PARA CONTROLE DE VISIBILIDADE DA SENHA
        private bool isSenhaVisible = false;
        private bool isConfirmarSenhaVisible = false;
        private string senhaOriginalHash = ""; // Para armazenar o hash original do banco
        private bool senhaFoiAlterada = false; // Para controlar se a senha foi modificada
        private bool senhaEhHash = false; // Para indicar se a senha atual é um hash

        // Construtor da página
        public ProfileEditarPage()
        {
            InitializeComponent();
            CarregarDadosUsuario();
            ConfigurarIniciaisUsuario();
            ConfigurarEventosSincronizacao();
            ConfigurarEventosValidacao();
            ConfigurarEventosFoco(); // NOVO: Configurar eventos de foco
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

        // NOVO MÉTODO PARA CONFIGURAR EVENTOS DE FOCO
        private void ConfigurarEventosFoco()
        {
            // Eventos de foco para os campos de senha
            if (txtSenha != null)
            {
                txtSenha.GotFocus += TxtSenha_GotFocus;
                txtSenha.LostFocus += TxtSenha_LostFocus;
            }
            if (txtSenhaVisivel != null)
            {
                txtSenhaVisivel.GotFocus += TxtSenhaVisivel_GotFocus;
                txtSenhaVisivel.LostFocus += TxtSenhaVisivel_LostFocus;
            }
            if (txtConfirmarSenha != null)
            {
                txtConfirmarSenha.GotFocus += TxtConfirmarSenha_GotFocus;
                txtConfirmarSenha.LostFocus += TxtConfirmarSenha_LostFocus;
            }
            if (txtConfirmarSenhaVisivel != null)
            {
                txtConfirmarSenhaVisivel.GotFocus += TxtConfirmarSenhaVisivel_GotFocus;
                txtConfirmarSenhaVisivel.LostFocus += TxtConfirmarSenhaVisivel_LostFocus;
            }
        }

        // NOVOS EVENTOS DE FOCO PARA MOSTRAR MENSAGEM INFORMATIVA
        private void TxtSenha_GotFocus(object sender, RoutedEventArgs e)
        {
            MostrarMensagemSenhaCriptografada();
        }

        private void TxtSenha_LostFocus(object sender, RoutedEventArgs e)
        {
            OcultarMensagemSenhaCriptografada();
        }

        private void TxtSenhaVisivel_GotFocus(object sender, RoutedEventArgs e)
        {
            MostrarMensagemSenhaCriptografada();
        }

        private void TxtSenhaVisivel_LostFocus(object sender, RoutedEventArgs e)
        {
            OcultarMensagemSenhaCriptografada();
        }

        private void TxtConfirmarSenha_GotFocus(object sender, RoutedEventArgs e)
        {
            MostrarMensagemSenhaCriptografada();
        }

        private void TxtConfirmarSenha_LostFocus(object sender, RoutedEventArgs e)
        {
            OcultarMensagemSenhaCriptografada();
        }

        private void TxtConfirmarSenhaVisivel_GotFocus(object sender, RoutedEventArgs e)
        {
            MostrarMensagemSenhaCriptografada();
        }

        private void TxtConfirmarSenhaVisivel_LostFocus(object sender, RoutedEventArgs e)
        {
            OcultarMensagemSenhaCriptografada();
        }

        // NOVOS MÉTODOS PARA CONTROLAR MENSAGEM INFORMATIVA
        private void MostrarMensagemSenhaCriptografada()
        {
            if (senhaEhHash && txtInfoSenha != null)
            {
                txtInfoSenha.Text = "Para alterar sua senha, digite uma nova senha. Deixe vazio para manter a senha atual.";
                txtInfoSenha.Visibility = Visibility.Visible;
                txtInfoSenha.Foreground = System.Windows.Media.Brushes.Red; // Cor azul para informação
            }
        }

        private void OcultarMensagemSenhaCriptografada()
        {
            if (txtInfoSenha != null)
            {
                txtInfoSenha.Visibility = Visibility.Collapsed;
            }
        }

        // NOVO MÉTODO PARA CONFIGURAR EVENTOS DE VALIDAÇÃO
        private void ConfigurarEventosValidacao()
        {
            // Eventos para validação em tempo real
            txtNome.TextChanged += ValidarNome;
            txtEmailEdit.TextChanged += ValidarEmail;
            txtTelefone.TextChanged += ValidarTelefone;
            txtSenha.PasswordChanged += ValidarSenha;
            txtSenhaVisivel.TextChanged += ValidarSenhaVisivel;
            txtConfirmarSenha.PasswordChanged += ValidarConfirmarSenha;
            txtConfirmarSenhaVisivel.TextChanged += ValidarConfirmarSenhaVisivel;

            // NOVO: Eventos para detectar alteração na senha
            txtSenha.PasswordChanged += DetectarAlteracaoSenha;
            txtSenhaVisivel.TextChanged += DetectarAlteracaoSenhaVisivel;
        }

        // NOVOS MÉTODOS PARA DETECTAR ALTERAÇÃO NA SENHA
        private void DetectarAlteracaoSenha(object sender, RoutedEventArgs e)
        {
            if (!isSenhaVisible)
            {
                senhaFoiAlterada = true;
                OcultarMensagemSenhaCriptografada(); // Oculta a mensagem quando começar a digitar
            }
        }

        private void DetectarAlteracaoSenhaVisivel(object sender, TextChangedEventArgs e)
        {
            if (isSenhaVisible)
            {
                senhaFoiAlterada = true;
                OcultarMensagemSenhaCriptografada(); // Oculta a mensagem quando começar a digitar
            }
        }

        // MÉTODO PARA CONFIGURAR EVENTOS DE SINCRONIZAÇÃO
        private void ConfigurarEventosSincronizacao()
        {
            // Adicionar eventos para sincronizar o texto em tempo real
            if (txtSenha != null && txtSenhaVisivel != null)
            {
                txtSenha.PasswordChanged += TxtSenha_PasswordChanged;
                txtSenhaVisivel.TextChanged += TxtSenhaVisivel_TextChanged;
            }
            if (txtConfirmarSenha != null && txtConfirmarSenhaVisivel != null)
            {
                txtConfirmarSenha.PasswordChanged += TxtConfirmarSenha_PasswordChanged;
                txtConfirmarSenhaVisivel.TextChanged += TxtConfirmarSenhaVisivel_TextChanged;
            }
        }

        // EVENTOS PARA SINCRONIZAÇÃO EM TEMPO REAL - SENHA
        private void TxtSenha_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!isSenhaVisible && txtSenhaVisivel != null)
            {
                txtSenhaVisivel.Text = txtSenha.Password;
            }
        }

        private void TxtSenhaVisivel_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isSenhaVisible && txtSenha != null)
            {
                txtSenha.Password = txtSenhaVisivel.Text;
            }
        }

        // EVENTOS PARA SINCRONIZAÇÃO EM TEMPO REAL - CONFIRMAR SENHA
        private void TxtConfirmarSenha_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!isConfirmarSenhaVisible && txtConfirmarSenhaVisivel != null)
            {
                txtConfirmarSenhaVisivel.Text = txtConfirmarSenha.Password;
            }
        }

        private void TxtConfirmarSenhaVisivel_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isConfirmarSenhaVisible && txtConfirmarSenha != null)
            {
                txtConfirmarSenha.Password = txtConfirmarSenhaVisivel.Text;
            }
        }

        // MÉTODOS DE VALIDAÇÃO
        // Validação do Nome
        private void ValidarNome(object sender, TextChangedEventArgs e)
        {
            string nome = txtNome.Text.Trim();
            if (string.IsNullOrEmpty(nome))
            {
                txtErroNome.Text = "Nome é obrigatório";
                txtErroNome.Visibility = Visibility.Visible;
            }
            else if (nome.Length < 6)
            {
                txtErroNome.Text = "Nome deve ter pelo menos 6 caracteres";
                txtErroNome.Visibility = Visibility.Visible;
            }
            else if (nome.Length > 25)
            {
                txtErroNome.Text = "Nome deve ter no máximo 25 caracteres";
                txtErroNome.Visibility = Visibility.Visible;
            }
            else if (NomeUsuarioJaExiste(nome, MainWindow.SessaoUsuario.Cod))
            {
                txtErroNome.Text = "Este nome de usuário já está em uso";
                txtErroNome.Visibility = Visibility.Visible;
            }
            else
            {
                txtErroNome.Visibility = Visibility.Collapsed;
                ConfigurarIniciaisUsuario(); // Atualiza as iniciais quando o nome é válido
            }
        }

        // Validação do Email
        private void ValidarEmail(object sender, TextChangedEventArgs e)
        {
            string email = txtEmailEdit.Text.Trim();
            if (string.IsNullOrEmpty(email))
            {
                txtErroEmail.Text = "Email é obrigatório";
                txtErroEmail.Visibility = Visibility.Visible;
            }
            else if (!email.Contains("@"))
            {
                txtErroEmail.Text = "Email deve conter @";
                txtErroEmail.Visibility = Visibility.Visible;
            }
            else if (!IsValidEmail(email))
            {
                txtErroEmail.Text = "Formato de email inválido";
                txtErroEmail.Visibility = Visibility.Visible;
            }
            else if (EmailJaExiste(email, MainWindow.SessaoUsuario.Cod))
            {
                txtErroEmail.Text = "Este email já está cadastrado";
                txtErroEmail.Visibility = Visibility.Visible;
            }
            else
            {
                txtErroEmail.Visibility = Visibility.Collapsed;
            }
        }

        // Validação e formatação do Telefone               
        private void ValidarTelefone(object sender, TextChangedEventArgs e)
        {
            string telefone = txtTelefone.Text;
            // Remove todos os caracteres não numéricos para validação
            string apenasNumeros = Regex.Replace(telefone, @"[^\d]", "");

            // Formatar automaticamente
            if (apenasNumeros.Length <= 11)
            {
                string telefoneFormatado = FormatarTelefone(apenasNumeros);
                // Evita loop infinito ao definir o texto
                if (txtTelefone.Text != telefoneFormatado)
                {
                    // Salva a posição atual do cursor
                    int cursorPosition = txtTelefone.CaretIndex;
                    // Conta quantos dígitos existem antes da posição do cursor
                    string textoAntesCursor = telefone.Substring(0, Math.Min(cursorPosition, telefone.Length));
                    int digitosAntesCursor = Regex.Replace(textoAntesCursor, @"[^\d]", "").Length;

                    // Define o novo texto formatado
                    txtTelefone.Text = telefoneFormatado;
                    // Calcula a nova posição do cursor baseada nos dígitos
                    int novaPosicao = CalcularPosicaoCursor(telefoneFormatado, digitosAntesCursor);
                    txtTelefone.CaretIndex = Math.Min(novaPosicao, telefoneFormatado.Length);
                }
            }

            // Validação
            if (string.IsNullOrEmpty(apenasNumeros))
            {
                txtErroTelefone.Text = "Telefone é obrigatório";
                txtErroTelefone.Visibility = Visibility.Visible;
            }
            else if (apenasNumeros.Length != 11)
            {
                txtErroTelefone.Text = "Telefone deve ter exatamente 11 dígitos";
                txtErroTelefone.Visibility = Visibility.Visible;
            }
            else if (TelefoneJaExiste(telefone, MainWindow.SessaoUsuario.Cod))
            {
                txtErroTelefone.Text = "Este telefone já está cadastrado";
                txtErroTelefone.Visibility = Visibility.Visible;
            }
            else
            {
                txtErroTelefone.Visibility = Visibility.Collapsed;
            }
        }

        // Método auxiliar para calcular a posição correta do cursor
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

        // Validação da Senha
        private void ValidarSenha(object sender, RoutedEventArgs e)
        {
            string senha = txtSenha.Password;
            ValidarSenhaComum(senha);
        }

        private void ValidarSenhaVisivel(object sender, TextChangedEventArgs e)
        {
            if (isSenhaVisible)
            {
                string senha = txtSenhaVisivel.Text;
                ValidarSenhaComum(senha);
            }
        }

        private void ValidarSenhaComum(string senha)
        {
            if (string.IsNullOrEmpty(senha))
            {
                txtErroSenha.Text = "Senha é obrigatória";
                txtErroSenha.Visibility = Visibility.Visible;
            }
            else if (senha.Length < 8)
            {
                txtErroSenha.Text = "Senha deve ter pelo menos 8 caracteres";
                txtErroSenha.Visibility = Visibility.Visible;
            }
            else if (senha.Length > 20)
            {
                txtErroSenha.Text = "Senha deve ter no máximo 20 caracteres";
                txtErroSenha.Visibility = Visibility.Visible;
            }
            else if (!senha.Any(char.IsLetter))
            {
                txtErroSenha.Text = "Senha deve conter pelo menos uma letra";
                txtErroSenha.Visibility = Visibility.Visible;
            }
            else if (!senha.Any(char.IsDigit))
            {
                txtErroSenha.Text = "Senha deve conter pelo menos um número";
                txtErroSenha.Visibility = Visibility.Visible;
            }
            else
            {
                txtErroSenha.Visibility = Visibility.Collapsed;
            }

            // Revalidar confirmação de senha quando a senha principal muda
            ValidarConfirmacaoSenha();
        }

        // Validação da Confirmação de Senha
        private void ValidarConfirmarSenha(object sender, RoutedEventArgs e)
        {
            ValidarConfirmacaoSenha();
        }

        private void ValidarConfirmarSenhaVisivel(object sender, TextChangedEventArgs e)
        {
            if (isConfirmarSenhaVisible)
            {
                ValidarConfirmacaoSenha();
            }
        }

        private void ValidarConfirmacaoSenha()
        {
            string senha = ObterSenhaAtual();
            string confirmarSenha = ObterConfirmarSenhaAtual();

            if (string.IsNullOrEmpty(confirmarSenha))
            {
                txtErroConfirmarSenha.Text = "Confirmação de senha é obrigatória";
                txtErroConfirmarSenha.Visibility = Visibility.Visible;
            }
            else if (confirmarSenha.Length < 8)
            {
                txtErroConfirmarSenha.Text = "Confirmação deve ter pelo menos 8 caracteres";
                txtErroConfirmarSenha.Visibility = Visibility.Visible;
            }
            else if (confirmarSenha.Length > 20)
            {
                txtErroConfirmarSenha.Text = "Confirmação deve ter no máximo 20 caracteres";
                txtErroConfirmarSenha.Visibility = Visibility.Visible;
            }
            else if (!confirmarSenha.Any(char.IsLetter))
            {
                txtErroConfirmarSenha.Text = "Confirmação deve conter pelo menos uma letra";
                txtErroConfirmarSenha.Visibility = Visibility.Visible;
            }
            else if (!confirmarSenha.Any(char.IsDigit))
            {
                txtErroConfirmarSenha.Text = "Confirmação deve conter pelo menos um número";
                txtErroConfirmarSenha.Visibility = Visibility.Visible;
            }
            else if (senha != confirmarSenha)
            {
                txtErroConfirmarSenha.Text = "Senhas não coincidem";
                txtErroConfirmarSenha.Visibility = Visibility.Visible;
            }
            else
            {
                txtErroConfirmarSenha.Visibility = Visibility.Collapsed;
            }
        }

        // MÉTODOS AUXILIARES
        // Formatar telefone automaticamente
        private string FormatarTelefone(string numeros)
        {
            if (numeros.Length == 0) return "";
            if (numeros.Length <= 2) return $"({numeros}";
            if (numeros.Length <= 3) return $"({numeros.Substring(0, 2)}) {numeros.Substring(2)}";
            if (numeros.Length <= 7) return $"({numeros.Substring(0, 2)}) {numeros.Substring(2, 1)} {numeros.Substring(3)}";
            if (numeros.Length <= 11) return $"({numeros.Substring(0, 2)}) {numeros.Substring(2, 1)} {numeros.Substring(3, 4)}-{numeros.Substring(7)}";
            return $"({numeros.Substring(0, 2)}) {numeros.Substring(2, 1)} {numeros.Substring(3, 4)}-{numeros.Substring(7, 4)}";
        }

        // Validar formato de email mais rigoroso
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

        // Verificar se todos os campos são válidos
        private bool TodosCamposValidos()
        {
            return txtErroNome.Visibility == Visibility.Collapsed &&
                   txtErroEmail.Visibility == Visibility.Collapsed &&
                   txtErroTelefone.Visibility == Visibility.Collapsed &&
                   txtErroSenha.Visibility == Visibility.Collapsed &&
                   txtErroConfirmarSenha.Visibility == Visibility.Collapsed;
        }

        // MÉTODO AUXILIAR PARA OBTER SENHA ATUAL
        private string ObterSenhaAtual()
        {
            if (isSenhaVisible && txtSenhaVisivel != null)
            {
                return txtSenhaVisivel.Text;
            }
            else if (txtSenha != null)
            {
                return txtSenha.Password;
            }
            return "";
        }

        // MÉTODO AUXILIAR PARA OBTER CONFIRMAÇÃO DE SENHA ATUAL
        private string ObterConfirmarSenhaAtual()
        {
            if (isConfirmarSenhaVisible && txtConfirmarSenhaVisivel != null)
            {
                return txtConfirmarSenhaVisivel.Text;
            }
            else if (txtConfirmarSenha != null)
            {
                return txtConfirmarSenha.Password;
            }
            return "";
        }

        // MÉTODOS DE VERIFICAÇÃO NO BANCO DE DADOS
        // Verificar se email já existe no banco (excluindo o usuário atual)
        private bool EmailJaExiste(string email, int? excluirCod = null)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT COUNT(*) FROM usuariosCadastrados WHERE email = @email";
                    // Se estiver editando, excluir o próprio usuário da verificação
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

        // Verificar se telefone já existe no banco (excluindo o usuário atual)
        private bool TelefoneJaExiste(string telefone, int? excluirCod = null)
        {
            try
            {
                // Extrair apenas números do telefone
                string telefoneSomenteNumeros = Regex.Replace(telefone, @"[^\d]", "");
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT COUNT(*) FROM usuariosCadastrados WHERE telefone = @telefone";
                    // Se estiver editando, excluir o próprio usuário da verificação
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

        // Verificar se nome de usuário já existe no banco (excluindo o usuário atual)
        private bool NomeUsuarioJaExiste(string nome, int? excluirCod = null)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT COUNT(*) FROM usuariosCadastrados WHERE LOWER(usuario) = LOWER(@nome)";
                    // Se estiver editando, excluir o próprio usuário da verificação
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

        // Método para configurar as iniciais do usuário (ex: João da Silva -> JS)
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

        // MÉTODO MODIFICADO PARA CARREGAR DADOS COM SUPORTE A HASH
        private void CarregarDadosUsuario()
        {
            txtNome.Text = MainWindow.SessaoUsuario.Nome;
            txtEmailEdit.Text = MainWindow.SessaoUsuario.Email;
            txtTelefone.Text = MainWindow.SessaoUsuario.Telefone;

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query = "SELECT senha FROM usuariosCadastrados WHERE cod = @cod";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@cod", MainWindow.SessaoUsuario.Cod);
                    object result = cmd.ExecuteScalar();

                    if (result != null)
                    {
                        string senhaArmazenada = result.ToString();
                        senhaOriginalHash = senhaArmazenada;
                        senhaEhHash = EhHashBCrypt(senhaArmazenada); // MODIFICADO: Detecta se é hash

                        // MODIFICADO: Verificar se é hash ou texto plano
                        if (senhaEhHash)
                        {
                            // É um hash BCrypt - deixar campos vazios
                            if (txtSenha != null)
                            {
                                txtSenha.Password = "";
                            }
                            if (txtSenhaVisivel != null)
                            {
                                txtSenhaVisivel.Text = "";
                            }
                            if (txtConfirmarSenha != null)
                            {
                                txtConfirmarSenha.Password = "";
                            }
                            if (txtConfirmarSenhaVisivel != null)
                            {
                                txtConfirmarSenhaVisivel.Text = "";
                            }

                            // REMOVIDO: MessageBox.Show - agora a mensagem aparece apenas no foco
                        }
                        else
                        {
                            // É texto plano (dados antigos) - mostrar a senha atual
                            if (txtSenha != null)
                            {
                                txtSenha.Password = senhaArmazenada;
                            }
                            if (txtSenhaVisivel != null)
                            {
                                txtSenhaVisivel.Text = senhaArmazenada;
                            }
                            if (txtConfirmarSenha != null)
                            {
                                txtConfirmarSenha.Password = senhaArmazenada;
                            }
                            if (txtConfirmarSenhaVisivel != null)
                            {
                                txtConfirmarSenhaVisivel.Text = senhaArmazenada;
                            }
                        }

                        senhaFoiAlterada = false; // Reset do flag de alteração
                    }
                }
                catch (MySqlException ex)
                {
                    MessageBox.Show("Erro ao carregar dados: " + ex.Message);
                }
            }
        }

        // MÉTODO MODIFICADO PARA SALVAR COM CRIPTOGRAFIA
        private void btnSalvar_Click(object sender, RoutedEventArgs e)
        {
            // Forçar validação de todos os campos
            ValidarNome(null, null);
            ValidarEmail(null, null);
            ValidarTelefone(null, null);

            string senhaAtual = ObterSenhaAtual();

            // MODIFICADO: Validar senha apenas se foi alterada ou se está vazia (para hashes existentes)
            if (senhaFoiAlterada || string.IsNullOrEmpty(senhaAtual))
            {
                if (string.IsNullOrEmpty(senhaAtual))
                {
                    // Se a senha está vazia e temos um hash, não é necessário alterar a senha
                    if (EhHashBCrypt(senhaOriginalHash))
                    {
                        // Senha não será alterada - usar hash existente
                    }
                    else
                    {
                        // Senha obrigatória se não há hash válido
                        txtErroSenha.Text = "Senha é obrigatória";
                        txtErroSenha.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    ValidarSenhaComum(senhaAtual);
                    ValidarConfirmacaoSenha();
                }
            }

            // Verificar se todos os campos são válidos
            if (!TodosCamposValidos())
            {
                MessageBox.Show("Por favor, corrija os erros antes de salvar.", "Erro de Validação",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string nome = txtNome.Text.Trim();
            string email = txtEmailEdit.Text.Trim();
            string telefone = txtTelefone.Text.Trim();

            // MODIFICADO: Determinar qual senha usar
            string senhaParaSalvar;

            if (senhaFoiAlterada && !string.IsNullOrEmpty(senhaAtual))
            {
                // Senha foi alterada - criptografar a nova senha
                senhaParaSalvar = CriptografarSenha(senhaAtual);

                if (senhaParaSalvar == null)
                {
                    MessageBox.Show("Erro ao processar a nova senha. Tente novamente.", "Erro",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            else
            {
                // Senha não foi alterada - usar hash existente
                senhaParaSalvar = senhaOriginalHash;
            }

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query = @"UPDATE usuariosCadastrados 
                                    SET usuario = @nome, email = @email, telefone = @telefone, senha = @senha 
                                    WHERE cod = @cod";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@nome", nome);
                    cmd.Parameters.AddWithValue("@email", email);
                    cmd.Parameters.AddWithValue("@telefone", telefone);
                    cmd.Parameters.AddWithValue("@senha", senhaParaSalvar); // MODIFICADO: usar senha criptografada
                    cmd.Parameters.AddWithValue("@cod", MainWindow.SessaoUsuario.Cod);

                    int result = cmd.ExecuteNonQuery();
                    if (result > 0)
                    {
                        MainWindow.SessaoUsuario.Nome = nome;
                        MainWindow.SessaoUsuario.Email = email;
                        MainWindow.SessaoUsuario.Telefone = telefone;

                        string mensagem = "Dados atualizados com sucesso!";
                        if (senhaFoiAlterada && !string.IsNullOrEmpty(senhaAtual))
                        {
                            mensagem += "\n\nSua senha foi criptografada com segurança.";
                        }

                        MessageBox.Show(mensagem, "Sucesso",
                                       MessageBoxButton.OK, MessageBoxImage.Information);
                        NavigationService.Navigate(new ProfilePage());
                    }
                    else
                    {
                        MessageBox.Show("Nenhum dado foi atualizado.", "Aviso",
                                       MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (MySqlException ex)
                {
                    MessageBox.Show("Erro ao atualizar: " + ex.Message, "Erro",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // MÉTODO para alternar entre mostrar e ocultar a senha
        private void btnToggleSenha_Click(object sender, RoutedEventArgs e)
        {
            if (isSenhaVisible)
            {
                txtSenha.Password = txtSenhaVisivel.Text;
                txtSenha.Visibility = Visibility.Visible;
                txtSenhaVisivel.Visibility = Visibility.Collapsed;
                btnToggleSenha.Content = "🚫";
                btnToggleSenha.ToolTip = "Mostrar senha";
                isSenhaVisible = false;
                txtSenha.Focus();
            }
            else
            {
                txtSenhaVisivel.Text = txtSenha.Password;
                txtSenha.Visibility = Visibility.Collapsed;
                txtSenhaVisivel.Visibility = Visibility.Visible;
                btnToggleSenha.Content = "👁";
                btnToggleSenha.ToolTip = "Ocultar senha";
                isSenhaVisible = true;
                txtSenhaVisivel.Focus();
                txtSenhaVisivel.CaretIndex = txtSenhaVisivel.Text.Length;
            }
        }

        // MÉTODO para alternar entre mostrar e ocultar a confirmação de senha
        private void btnToggleConfirmarSenha_Click(object sender, RoutedEventArgs e)
        {
            if (isConfirmarSenhaVisible)
            {
                txtConfirmarSenha.Password = txtConfirmarSenhaVisivel.Text;
                txtConfirmarSenha.Visibility = Visibility.Visible;
                txtConfirmarSenhaVisivel.Visibility = Visibility.Collapsed;
                btnToggleConfirmarSenha.Content = "🚫";
                btnToggleConfirmarSenha.ToolTip = "Mostrar senha";
                isConfirmarSenhaVisible = false;
                txtConfirmarSenha.Focus();
            }
            else
            {
                txtConfirmarSenhaVisivel.Text = txtConfirmarSenha.Password;
                txtConfirmarSenha.Visibility = Visibility.Collapsed;
                txtConfirmarSenhaVisivel.Visibility = Visibility.Visible;
                btnToggleConfirmarSenha.Content = "👁";
                btnToggleConfirmarSenha.ToolTip = "Ocultar senha";
                isConfirmarSenhaVisible = true;
                txtConfirmarSenhaVisivel.Focus();
                txtConfirmarSenhaVisivel.CaretIndex = txtConfirmarSenhaVisivel.Text.Length;
            }
        }

        // Botão de voltar
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new ProfilePage());
        }
    }
}
