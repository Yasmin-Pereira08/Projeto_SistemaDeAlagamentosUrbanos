using System.Windows;          // Namespace para classes relacionadas a janelas, visibilidade, etc.
using System.Windows.Controls; // Namespace para controles visuais como TextBox, Image, ToolTip
using System.Windows.Media;    // Namespace para elementos gráficos, como cores e pincéis (Brushes)

namespace SeuProjeto.Helpers    // Define o namespace do projeto, para organizar e agrupar classes auxiliares
{
    // Classe estática para validação visual de campos no WPF
    public static class ValidadorVisual
    {
        // Método estático para validar um campo TextBox, exibindo ou escondendo um ícone de erro e alterando o estilo visual
        // Parâmetros:
        //  - textBox: o controle TextBox que será validado
        //  - iconeErro: uma imagem que serve como ícone de erro, exibida quando a validação falha
        //  - mensagemErro: texto que será exibido no ToolTip do ícone de erro
        //  - condicaoValida: função que recebe o texto do TextBox e retorna true se o valor for válido, false caso contrário
        public static void ValidarCampo(TextBox textBox, Image iconeErro, string mensagemErro, Func<string, bool> condicaoValida)
        {
            // Verifica se o texto do TextBox satisfaz a condição de validação
            if (condicaoValida(textBox.Text))
            {
                // Se válido:
                // Oculta o ícone de erro (visibilidade Collapsed = não ocupa espaço na UI)
                iconeErro.Visibility = Visibility.Collapsed;
                // Restaura a cor padrão da borda do TextBox (cinza)
                textBox.BorderBrush = Brushes.Gray; // Ou outra cor padrão que queira usar
            }
            else
            {
                // Se inválido:
                // Define a mensagem do ToolTip do ícone de erro com a mensagem passada
                iconeErro.ToolTip = new ToolTip { Content = mensagemErro };
                // Exibe o ícone de erro (visibilidade Visível)
                iconeErro.Visibility = Visibility.Visible;
                // Muda a borda do TextBox para vermelho para indicar erro
                textBox.BorderBrush = Brushes.Red;
            }
        }
    }
}
