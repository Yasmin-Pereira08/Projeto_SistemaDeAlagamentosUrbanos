// Importa o namespace System, que contém classes fundamentais como DateTime
using System;

namespace TCC // Define um namespace chamado TCC, útil para organizar o código e evitar conflitos de nomes
{
    // Declara a classe pública NotificacaoAlagamento, que representa uma notificação de alagamento
    public class NotificacaoAlagamento
    {
        // Propriedade pública que representa o nível do alagamento (baixo, médio, alto, etc.)
        public string Nivel { get; set; }

        // Propriedade pública que representa o local onde ocorreu o alagamento
        public string Local { get; set; }

        // Propriedade pública que representa a data e hora do alagamento
        public DateTime Horario { get; set; }

        // Propriedade somente leitura que gera uma mensagem com base no nível do alagamento
        // Usa expressão switch para retornar a mensagem apropriada
        public string Mensagem => Nivel switch
        {
            "baixo" => " Pequeno alagamento, passagem possível",        // Para nível "baixo"
            "médio" => " Alagamento moderado, trânsito lento",         // Para nível "médio"
            "alto" => " Alagamento severo, evite a área",             // Para nível "alto"
            _ => "Alerta de alagamento"                          // Para qualquer outro valor
        };

        // Propriedade somente leitura que monta uma string com detalhes formatados da notificação
        // Inclui local e horário formatado como "dd/MM/yyyy HH:mm:ss"
        public string Detalhes => $"📍 {Local}   🕒 {Horario:dd/MM/yyyy HH:mm:ss}";
    }
}
