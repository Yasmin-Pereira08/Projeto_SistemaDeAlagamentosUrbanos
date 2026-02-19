using System;
using System.IO.Ports;  // Namespace necessário para trabalhar com portas seriais

namespace TCC
{
    // Classe responsável por monitorar dados recebidos de uma porta serial
    public class SerialMonitor
    {
        // Instância estática única da classe (Singleton) para garantir que só exista um monitor serial
        private static SerialMonitor _instancia = new SerialMonitor();

        // Propriedade pública que expõe a instância única da classe
        public static SerialMonitor Instancia => _instancia;

        private SerialPort portaSerial; // Objeto que representa a porta serial física
        public event Action<int> DadoRecebido; // Evento para notificar quando um dado inteiro for recebido da porta

        // Construtor privado para evitar criação externa de instâncias e configurar a porta serial
        private SerialMonitor()
        {
            portaSerial = new SerialPort
            {
                BaudRate = 9600,      // Velocidade da comunicação (bits por segundo)
                DataBits = 8,         // Quantidade de bits de dados por pacote
                Parity = Parity.None, // Nenhum bit de paridade
                StopBits = StopBits.One // Um bit de parada
            };

            // Associa o método que será chamado quando dados chegarem pela porta serial
            portaSerial.DataReceived += PortaSerial_DataReceived;
        }

        // Método para iniciar a comunicação na porta especificada
        public void Iniciar(string nomePorta)
        {
            // Verifica se a porta já não está aberta para evitar exceções
            if (!portaSerial.IsOpen)
            {
                portaSerial.PortName = nomePorta; // Define a porta física (ex: "COM3")
                portaSerial.Open ();               // Abre a porta para comunicação
            }
        }

        // Método disparado automaticamente quando dados são recebidos na porta serial
        private void PortaSerial_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                // Lê uma linha completa de texto da porta e remove espaços em branco no início/fim
                string linha = portaSerial.ReadLine().Trim();

                // Tenta converter a linha para um número inteiro
                if (int.TryParse(linha, out int valor))
                {
                    // Se conseguir, dispara o evento DadoRecebido, notificando assinantes com o valor recebido
                    DadoRecebido?.Invoke(valor);
                }
            }
            catch
            {
                // Caso ocorra algum erro na leitura, ele é ignorado (pode-se adicionar log aqui)
            }
        }
    }
}
