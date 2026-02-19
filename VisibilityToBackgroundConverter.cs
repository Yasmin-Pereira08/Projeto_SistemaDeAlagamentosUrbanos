using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace TCC
{
    // Classe que implementa a interface IValueConverter para converter
    // um valor do tipo Visibility em uma cor de fundo (Brush).
    public class VisibilityToBackgroundConverter : IValueConverter
    {
        // Método chamado para converter o valor de origem para o valor do destino.
        // Neste caso, converte Visibility em uma SolidColorBrush, conforme o parâmetro.
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Verifica se o valor recebido é do tipo Visibility e o parâmetro é uma string
            // que representa uma cor (ex: "#FF0000" ou "Red").
            if (value is Visibility visibility && parameter is string colorString)
            {
                // Se o valor Visibility for Visible, converte a string da cor para um objeto Color
                // e retorna uma SolidColorBrush com essa cor.
                if (visibility == Visibility.Visible)
                {
                    // ColorConverter converte a string da cor para o tipo Color.
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorString));
                }
            }
            // Se o valor não for Visibility.Visible ou os parâmetros forem inválidos,
            // retorna uma SolidColorBrush transparente.
            return new SolidColorBrush(Colors.Transparent);
        }

        // Método para converter no sentido contrário, não implementado porque
        // normalmente não é necessário para esse tipo de conversão de UI.
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Lança exceção para indicar que o método não foi implementado.
            throw new NotImplementedException();
        }
    }
}
