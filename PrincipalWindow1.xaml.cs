// Importa os namespaces necessários para funcionamento do WPF e manipulação de elementos da interface
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows; // Elementos básicos do WPF como Window, RoutedEventArgs, etc.
using System.Windows.Controls; // Controles como Button, Frame, etc.
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input; // Eventos de entrada como cliques e teclado
using System.Windows.Media; // Manipulação de cores e gráficos
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TCC // Define o namespace do projeto, geralmente o nome da solução
{
    /// <summary>
    /// Código-behind da janela PrincipalWindow1.xaml
    /// </summary>
    public partial class PrincipalWindow1 : Window
    {
        // Construtor da janela principal
        public PrincipalWindow1()
        {
            InitializeComponent(); // Inicializa os componentes da interface definidos no XAML
            MainFrame.Navigate(new HomePage()); // Define a página inicial exibida no Frame como HomePage

            // Define o botão "Home" como ativo ao iniciar
            UpdateActiveButton(0);
        }

        // Evento acionado ao clicar no botão "Info"
        private void Info_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Informacao()); // Navega para a página de informações
            UpdateActiveButton(3); // Define o botão "Info" como ativo
        }

        // Evento acionado ao clicar no botão "Home"
        private void Home_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new HomePage()); // Navega para a página inicial
            UpdateActiveButton(0); // Define o botão "Home" como ativo
        }

        // Evento acionado ao clicar no botão "Notificações"
        private void Notifications_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new NotificationsPage()); // Navega para a página de notificações
            UpdateActiveButton(1); // Define o botão "Notificações" como ativo
        }

        // Evento acionado ao clicar no botão "Perfil"
        private void Profile_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ProfilePage()); // Navega para a página de perfil do usuário
            UpdateActiveButton(2); // Define o botão "Perfil" como ativo
        }

        /// <summary>
        /// Atualiza o indicador visual e o estilo do botão ativo no menu lateral
        /// </summary>
        /// <param name="activeIndex">
        /// Índice que representa o botão ativo:
        /// 0 = Home, 1 = Notifications, 2 = Profile, 3 = Info
        /// </param>
        private void UpdateActiveButton(int activeIndex)
        {
            // Move o indicador visual (por exemplo, uma linha ou barra) para o botão selecionado
            Grid.SetColumn(ActiveIndicator, activeIndex);

            // Redefine todos os botões para o estilo padrão
            HomeButton.Style = (Style)FindResource("MenuButtonStyle");
            NotificationsButton.Style = (Style)FindResource("MenuButtonStyle");
            ProfileButton.Style = (Style)FindResource("MenuButtonStyle");
            InfoButton.Style = (Style)FindResource("MenuButtonStyle");
            SairButton.Style = (Style)FindResource("MenuButtonStyle");

            // Aplica o estilo de botão ativo apenas ao botão correspondente
            switch (activeIndex)
            {
                case 0:
                    HomeButton.Style = (Style)FindResource("ActiveMenuButtonStyle");
                    break;
                case 1:
                    NotificationsButton.Style = (Style)FindResource("ActiveMenuButtonStyle");
                    break;
                case 2:
                    ProfileButton.Style = (Style)FindResource("ActiveMenuButtonStyle");
                    break;
                case 3:
                    InfoButton.Style = (Style)FindResource("ActiveMenuButtonStyle");
                    break;
                case 4:
                    SairButton.Style = (Style)FindResource("ActiveMenuButtonStyle");
                    break;
            }
        }

        private void Sair_Click(object sender, RoutedEventArgs e)
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
