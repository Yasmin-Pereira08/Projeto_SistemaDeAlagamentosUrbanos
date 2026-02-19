// Referências necessárias para uso de WebView2, banco de dados MySQL, interface WPF, e manipulação de tempo/rede.
using Microsoft.Web.WebView2.Wpf;
using MySql.Data.MySqlClient;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace TCC
{
    // Página principal da aplicação, derivada de Page (usada com NavigationService)
    public partial class HomePage : Page
    {
        // Variável que armazena o valor atual do ícone no mapa
        private int resdesen = 0;

        // String de conexão com o banco de dados MySQL
        private string connectionString = "Server=localhost;Database=Sistema;Uid=root;";

        // Construtor da página Home
        public HomePage()
        {
            InitializeComponent(); // Inicializa os componentes visuais (XAML)

            InicializarMapaAsync(); // Carrega o mapa HTML no WebView2
            AtualizarClima(); // Busca informações climáticas da API
            AtualizarMapa(2); // Define um valor inicial para o ícone do mapa
            CarregarNomeUsuario(); // Tenta buscar e exibir o nome do usuário logado

            // Inscreve-se no evento do monitor serial (recebe valores do Arduino)
            SerialMonitor.Instancia.DadoRecebido += valor =>
            {
                Dispatcher.Invoke(() =>
                {
                    int novoValor = (valor >= 1 && valor <= 3) ? valor : 0; // 0 para remover ícone se valor == 4

                    // Só atualiza o mapa se o valor mudou
                    if (resdesen != novoValor)
                    {
                        AtualizarMapa(novoValor);
                    }
                });
            };


            try
            {
                SerialMonitor.Instancia.Iniciar("COM3");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Aviso: Porta COM3 não encontrada ou indisponível.\nO sistema continuará funcionando sem leitura do sensor.\n\nDetalhes: " + ex.Message);
            }
        }

        // Método alternativo (não utilizado no código atual) para tratar dado recebido
        private void SerialMonitor_DadoRecebido(int valor)
        {
            Dispatcher.Invoke(() =>
            {
                if (valor >= 1 && valor <= 3)
                {
                    AtualizarMapa(valor);
                }
                else if (valor == 4)
                {
                    AtualizarMapa(0);
                }
            });
        }

        // Exibe o nome do usuário logado no componente txtNomeUsuario
        private void CarregarNomeUsuario()
        {
            try
            {
                string usuarioLogado = ObterUsuarioLogado();

                txtNomeUsuario.Text = string.IsNullOrEmpty(usuarioLogado) ? "Visitante" : usuarioLogado;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar nome do usuário: " + ex.Message);
                txtNomeUsuario.Text = "Usuário";
            }
        }

        // Consulta o banco de dados para obter o nome do primeiro usuário cadastrado
        private string ObterUsuarioLogado()
        {
            string nomeUsuario = string.Empty;

            try
            {

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string query = "SELECT usuario FROM usuariosCadastrados WHERE cod = @cod";

                    using (MySqlCommand cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@cod", MainWindow.SessaoUsuario.Cod);

                        object result = cmd.ExecuteScalar();
                        if (result != null)
                            nomeUsuario = result.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao conectar ao banco de dados: " + ex.Message);
            }

            return nomeUsuario;
        }

        // Carrega o arquivo mapa.html no WebView2
        private async void InicializarMapaAsync()
        {
            string pathHTML = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "mapa.html");

            await webViewMapa.EnsureCoreWebView2Async(); // Garante que o WebView esteja pronto

            if (File.Exists(pathHTML))
            {
                // Constrói uma URI local para o arquivo HTML
                string uriLocal = $"file:///{pathHTML.Replace("\\", "/")}";
                webViewMapa.CoreWebView2.Navigate(uriLocal);
            }
            else
            {
                MessageBox.Show("Arquivo mapa.html não encontrado.");
            }
        }

        // Evento quando o campo de busca ganha foco
        private void TxtBusca_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtBusca.Text == "Para onde deseja ir?")
            {
                txtBusca.Text = "";
                txtBusca.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
            }
        }

        // Evento quando o campo de busca perde o foco
        private void TxtBusca_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtBusca.Text))
            {
                txtBusca.Text = "Para onde deseja ir?";
                txtBusca.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
            }
        }

        // Atualiza o ícone do mapa via JavaScript executado dentro do WebView2
        public async void AtualizarMapa(int novoValor)
        {
            resdesen = novoValor;

            if (webViewMapa.CoreWebView2 != null)
            {
                try
                {
                    // Executa a função JavaScript no HTML carregado
                    await webViewMapa.ExecuteScriptAsync($"atualizarIcone({resdesen});");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erro ao atualizar ícone no mapa: " + ex.Message);
                }
            }
        }

        // Obtém dados de previsão do tempo da API OpenWeather e atualiza a interface
        private async void AtualizarClima()
        {
            try
            {
                string apiKey = "548b073f98cffe2684c58d9c00c4714c";
                string url = $"https://api.openweathermap.org/data/2.5/forecast?q=Curitiba,br&appid={apiKey}&units=metric&lang=pt_br";

                using HttpClient client = new HttpClient();
                var response = await client.GetStringAsync(url);
                var json = JsonDocument.Parse(response);

                var previsao = json.RootElement.GetProperty("list")[0]; // Pega a primeira previsão
                double temperatura = previsao.GetProperty("main").GetProperty("temp").GetDouble();
                string descricao = previsao.GetProperty("weather")[0].GetProperty("description").GetString();

                // Atualiza os campos na interface
                txtTemperatura.Text = $"Temp. prevista: {temperatura:0.#} °C";
                txtDescricao.Text = $"Descrição: {descricao}";

                // Se houver propriedade de chance de chuva (pop), exibe
                if (previsao.TryGetProperty("pop", out JsonElement popElement))
                {
                    double chanceChuva = popElement.GetDouble() * 100;
                    txtChuva.Text = $"Chance de chuva: {chanceChuva:0.#}%";
                }
                else
                {
                    txtChuva.Text = "Sem previsão de chuva.";
                }
            }
            catch
            {
                // Caso ocorra erro, exibe mensagens padrão
                txtTemperatura.Text = "Erro ao obter clima.";
                txtDescricao.Text = "";
                txtChuva.Text = "Erro ao obter previsão.";
            }

            // Cria um temporizador para atualizar o clima a cada 10 minutos
            DispatcherTimer timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(10)
            };
            timer.Tick += (s, e) => AtualizarClima();
            timer.Start();
        }

        // Evento disparado quando o texto do campo de busca muda (não utilizado aqui)
        private void txtBusca_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        // Evento quando o usuário pressiona tecla no campo de busca
        private void txtBusca_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                string origem = "Rua XV de Novembro, Curitiba";
                string destino = txtBusca.Text.Trim();

                // Verifica se o destino é válido
                if (!string.IsNullOrEmpty(destino) && destino != "Buscar por localização...")
                {
                    // Abre a página de rotas passando origem e destino
                    RotasNavigationWindow rotasPage = new RotasNavigationWindow(origem, destino);
                    NavigationService?.Navigate(rotasPage);
                }
                else
                {
                    MessageBox.Show("Por favor, digite um destino válido.");
                }
            }
        }
    }
}
