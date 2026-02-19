// Importações necessárias
using System;
using System.Collections.ObjectModel; // Para listas que atualizam automaticamente a interface
using System.Linq;                     // Para uso de LINQ (filtragem de listas)
using System.Windows;                 // Elementos básicos do WPF
using System.Windows.Controls;        // Controles WPF como Page, ComboBox, etc.
using System.Windows.Input;           // Para eventos de teclado
using MySql.Data.MySqlClient;         // Biblioteca para conexão com banco MySQL

namespace TCC
{
    // Classe que representa a página de notificações
    public partial class NotificationsPage : Page
    {
        // Lista de todas as notificações carregadas do banco
        public ObservableCollection<NotificacaoAlagamento> Notificacoes { get; set; }

        // Lista que é exibida na interface, podendo ser filtrada
        public ObservableCollection<NotificacaoAlagamento> NotificacoesFiltradas { get; set; }

        // Monitor serial que recebe dados do sensor (ex: Arduino)
        private SerialMonitor serialMonitor;

        // String de conexão com banco de dados MySQL
        private string connectionString = "Server=localhost;Database=Sistema;Uid=root;Pwd=;";
        //Verifica para que não sejam geradas multiplas notificações
        private int ultimoValorRecebido = -1;
        // Construtor da página
        public NotificationsPage()
        {
            InitializeComponent(); // Inicializa componentes XAML

            // Obtém a instância do monitor serial e se inscreve no evento de dado recebido
            this.serialMonitor = SerialMonitor.Instancia;
            this.serialMonitor.DadoRecebido += SerialMonitor_DadoRecebido;

            // Inicializa as listas observáveis
            Notificacoes = new ObservableCollection<NotificacaoAlagamento>();
            NotificacoesFiltradas = new ObservableCollection<NotificacaoAlagamento>();

            // Define o DataContext para binding de dados na interface
            DataContext = this;

            // Define a fonte de dados do ItemsControl da interface
            NotificacoesItemsControl.ItemsSource = NotificacoesFiltradas;

            // Carrega dados do banco ao iniciar
            CarregarNotificacoesDoBanco();
            AtualizarListaFiltrada(); // Aplica filtros iniciais
        }

        // Evento chamado quando o dado é recebido da porta serial
        private void SerialMonitor_DadoRecebido(int valor)
        {
            // Usa o Dispatcher para garantir que atualizações na UI sejam feitas na thread correta
            Dispatcher.Invoke(() =>
            {
                ProcessarDado(valor); // Processa o dado recebido
            });
        }

        // Método que busca notificações do banco e adiciona à lista
        private void CarregarNotificacoesDoBanco()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // SQL que junta notificações com seus sensores (endereço)
                    string sql = @"SELECT n.nivel_alarme, n.data_hora, n.sensor, s.endereco 
                                   FROM notificacoes n 
                                   LEFT JOIN sensores s ON n.sensor = s.id 
                                   ORDER BY n.data_hora DESC";

                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // Lê dados da linha atual
                            int nivel = reader.GetInt32("nivel_alarme");
                            DateTime horario = reader.GetDateTime("data_hora");
                            string endereco = reader.IsDBNull(reader.GetOrdinal("endereco"))
                                              ? "Endereço não cadastrado"
                                              : reader.GetString("endereco");

                            // Converte nível numérico para string
                            string nivelTexto = nivel switch
                            {
                                1 => "baixo",
                                2 => "médio",
                                3 => "alto",
                                _ => "desconhecido"
                            };

                            // Adiciona notificação à lista
                            Notificacoes.Add(new NotificacaoAlagamento
                            {
                                Nivel = nivelTexto,
                                Horario = horario,
                                Local = endereco
                            });
                        }
                    }
                }

                AtualizarListaFiltrada(); // Atualiza exibição com base nos novos dados
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar notificações do banco: " + ex.Message);
            }
        }

        // Processa um dado vindo do sensor e adiciona notificação
        private void ProcessarDado(int valor)
        {
            // Evita notificações duplicadas
            if (valor == ultimoValorRecebido)
                return;

            ultimoValorRecebido = valor;

            // Converte valor numérico em string correspondente
            string nivel = valor switch
            {
                1 => "baixo",
                2 => "médio",
                3 => "alto",
                _ => null
            };

            if (nivel != null)
            {
                AdicionarAlertaDireto(nivel, 1); // Ex: sensorId 1
            }
        }


        // Busca o endereço de um sensor específico no banco
        private string ObterEnderecoSensor(int sensorId)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    string sql = "SELECT endereco FROM sensores WHERE id = @sensorId";
                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@sensorId", sensorId);
                        object result = cmd.ExecuteScalar();

                        return result != null ? result.ToString() : "Endereço não cadastrado";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao buscar endereço do sensor: " + ex.Message);
                return "Erro ao buscar endereço";
            }
        }

        // Adiciona uma nova notificação manualmente ou via sensor
        private void AdicionarAlertaDireto(string nivel, int sensorId)
        {
            DateTime horario = DateTime.Now;
            string endereco = ObterEnderecoSensor(sensorId);

            // Adiciona na interface (posição 0 = topo da lista)
            Notificacoes.Insert(0, new NotificacaoAlagamento
            {
                Nivel = nivel,
                Horario = horario,
                Local = endereco
            });

            AtualizarListaFiltrada();

            try
            {
                // Converte string para valor inteiro
                int nivelInt = nivel switch
                {
                    "baixo" => 1,
                    "médio" => 2,
                    "alto" => 3,
                    _ => 0
                };

                // Salva notificação no banco de dados
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    string sql = @"INSERT INTO notificacoes (nivel_alarme, sensor, data_hora)
                                   VALUES (@nivel, @sensor, @data)";
                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@nivel", nivelInt);
                        cmd.Parameters.AddWithValue("@sensor", sensorId);
                        cmd.Parameters.AddWithValue("@data", horario);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao salvar notificação no banco: " + ex.Message);
            }
        }

        // Botões de simulação de alerta
        private void SimularBaixo_Click(object sender, RoutedEventArgs e)
        {
            AdicionarAlertaDireto("baixo", 1);
        }

        private void SimularMedio_Click(object sender, RoutedEventArgs e)
        {
            AdicionarAlertaDireto("médio", 3);
        }

        private void SimularAlto_Click(object sender, RoutedEventArgs e)
        {
            AdicionarAlertaDireto("alto", 1);
        }

        // Evento de digitação no campo de busca
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Intencionalmente vazio para evitar atualizações constantes
        }

        // Evento ao pressionar tecla no campo de busca
        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AtualizarListaFiltrada(); // Filtra ao pressionar Enter
            }
        }

        // Clique no botão de busca
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            AtualizarListaFiltrada();
        }

        // Evento de troca de item no ComboBox de filtro
        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AtualizarListaFiltrada();
        }

        // Atualiza lista exibida com base na busca e filtro
        private void AtualizarListaFiltrada()
        {
            try
            {
                // Se lista ainda não estiver ligada à interface, conecta
                if (NotificacoesFiltradas == null)
                {
                    NotificacoesFiltradas = new ObservableCollection<NotificacaoAlagamento>();
                    if (NotificacoesItemsControl != null)
                    {
                        NotificacoesItemsControl.ItemsSource = NotificacoesFiltradas;
                    }
                }

                NotificacoesFiltradas.Clear(); // Limpa a lista anterior

                // Obtém texto da busca e nível de filtro
                string termoPesquisa = SearchBox?.Text?.ToLower() ?? "";
                string filtroNivel = "";
                if (FilterComboBox?.SelectedItem != null)
                {
                    ComboBoxItem item = FilterComboBox.SelectedItem as ComboBoxItem;
                    string conteudo = item?.Content?.ToString() ?? "";

                    if (conteudo.Contains("baixo")) filtroNivel = "baixo";
                    else if (conteudo.Contains("médio")) filtroNivel = "médio";
                    else if (conteudo.Contains("alto")) filtroNivel = "alto";
                }

                // Aplica filtros com LINQ
                var notificacoesFiltradas = Notificacoes.Where(n =>
                    (string.IsNullOrEmpty(termoPesquisa) ||
                     (n.Local != null && n.Local.ToLower().Contains(termoPesquisa))) &&
                    (string.IsNullOrEmpty(filtroNivel) || n.Nivel == filtroNivel)
                ).ToList();

                // Adiciona resultados à lista vinculada
                foreach (var notificacao in notificacoesFiltradas)
                {
                    NotificacoesFiltradas.Add(notificacao);
                }

                // Exibe ou oculta a mensagem de "nenhuma notificação"
                if (NoNotificationsMessage != null)
                {
                    NoNotificationsMessage.Visibility = (NotificacoesFiltradas.Count > 0)
                                                        ? Visibility.Collapsed
                                                        : Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao filtrar notificações: " + ex.Message);
            }
        }
    }
}
