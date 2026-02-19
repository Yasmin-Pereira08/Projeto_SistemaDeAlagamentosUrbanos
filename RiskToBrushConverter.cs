using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace TCC
{
    // Classe que implementa a interface IValueConverter para converter um valor (string) de nível de risco em uma cor (Brush)
    public class RiskToBrushConverter : IValueConverter
    {
        // Método chamado para converter o valor de origem (string do nível de risco) para o tipo alvo (Brush)
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Converte o valor para string, espera receber "baixo", "médio", "alto" ou outros
            string nivel = value as string;

            // Retorna uma cor diferente dependendo do valor do nível de risco usando expressão switch
            return nivel switch
            {
                "baixo" => Brushes.SkyBlue,   // risco baixo -> cor azul claro
                "médio" => Brushes.Gold,      // risco médio -> cor dourada
                "alto" => Brushes.Red,        // risco alto -> cor vermelha
                _ => Brushes.Gray             // qualquer outro valor -> cor cinza padrão
            };
        }

        // Método que faria a conversão inversa (de Brush para string), não implementado aqui pois não é necessário no contexto
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Lança exceção para indicar que esta funcionalidade não foi implementada
            throw new NotImplementedException();
        }
    }
}
