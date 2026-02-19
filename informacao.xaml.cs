using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TCC
{
    /// <summary>
    /// Interação lógica para Informacao.xaml
    /// </summary>
    public partial class Informacao : Page
    {
        public Informacao()
        {
            InitializeComponent();
        }

        private void AbrirPDF_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string caminhoPdf = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "manual.pdf");
                Process.Start(new ProcessStartInfo(caminhoPdf) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao abrir o PDF: " + ex.Message);
            }
        }


    }
}