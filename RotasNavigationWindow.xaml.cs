using System;
using System.IO.Ports;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;
using System.Windows;
using System.Windows.Threading;
using Google.Protobuf;

namespace TCC
{
    public partial class RotasNavigationWindow : Page
    {
        private readonly string origem;
        private readonly string destino;

        private bool rotasSaoIguais = false;
        private string rotaDiretaPolyline = null;

        // Guarda o último valor do sensor para evitar atualizações repetidas
        private int ultimoValorSensor = -1;

        public RotasNavigationWindow(string origem, string destino)
        {
            InitializeComponent();

            this.origem = origem;
            this.destino = destino;

            Loaded += RotasNavigationWindow_Loaded;
            Unloaded += RotasNavigationWindow_Unloaded;

            OrigemTextBlock.Text = $"De: {origem}";
            DestinoTextBlock.Text = $"Para: {destino}";
        }

        private async void RotasNavigationWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await webViewDireta.EnsureCoreWebView2Async();
                await webViewDesvio.EnsureCoreWebView2Async();

                webViewDireta.CoreWebView2.WebMessageReceived += WebMessageReceived;
                webViewDesvio.CoreWebView2.WebMessageReceived += WebMessageReceived;

                string htmlDireta = ObterHtmlRotaDireta(origem, destino);
                string htmlDesvio = ObterHtmlRotaDesvio(origem, destino);

                webViewDireta.NavigateToString(htmlDireta);
                webViewDesvio.NavigateToString(htmlDesvio);

                SerialMonitor.Instancia.DadoRecebido += SerialMonitor_DadoRecebido;
                SerialMonitor.Instancia.Iniciar("COM3");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar o mapa ou configurar sensores: " + ex.Message);
            }
        }

        private void RotasNavigationWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            SerialMonitor.Instancia.DadoRecebido -= SerialMonitor_DadoRecebido;
        }

        private async void SerialMonitor_DadoRecebido(int valor)
        {
            if (valor >= 1 && valor <= 4 && valor != ultimoValorSensor)
            {
                ultimoValorSensor = valor;

                await Dispatcher.InvokeAsync(async () =>
                {
                    if (webViewDireta.CoreWebView2 != null)
                    {
                        await webViewDireta.CoreWebView2.ExecuteScriptAsync($"valordosensor({valor});");
                    }
                });

                // A rota de desvio só será atualizada se a polyline da direta mudar.
            }
        }


        private void WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            var mensagem = e.TryGetWebMessageAsString();

            // DISTÂNCIA das rotas
            if (mensagem.StartsWith("distancia:"))
            {
                string valorDistancia = mensagem.Replace("distancia:", "").Trim();

                Dispatcher.Invoke(() =>
                {
                    if (sender == webViewDireta.CoreWebView2)
                    {
                        DistanciaTextBlock.Text = $"📏: {valorDistancia}";
                    }
                    else if (sender == webViewDesvio.CoreWebView2)
                    {
                        DistanciaDesvioTextBlock.Text = $"📏: {valorDistancia}";
                    }
                });
            }

            // TEMPO das rotas
            if (mensagem.StartsWith("tempo:"))
            {
                string valorTempo = mensagem.Replace("tempo:", "").Trim();

                Dispatcher.Invoke(() =>
                {
                    if (sender == webViewDireta.CoreWebView2)
                    {
                        TempoTextBlock.Text = $"⏱: {valorTempo}";
                    }
                    else if (sender == webViewDesvio.CoreWebView2)
                    {
                        TempoDesvioTextBlock.Text = $"⏱: {valorTempo}";
                    }
                });
            }

            // 2. POLYLINE da rota direta
            if (mensagem.StartsWith("rotaDireta_polyline:"))
            {
                var novaPolyline = mensagem.Replace("rotaDireta_polyline:", "");

                if (rotaDiretaPolyline != novaPolyline)
                {
                    rotaDiretaPolyline = novaPolyline;

                    if (webViewDesvio.CoreWebView2 != null)
                    {
                        webViewDesvio.CoreWebView2.PostWebMessageAsString("rotaDireta_polyline:" + novaPolyline);
                    }
                }
                return;
            }

            // 3. ROTAS IGUAIS = não passa por alagamento
            if (mensagem == "rotas_iguais")
            {
                rotasSaoIguais = true;

                Dispatcher.Invoke(() =>
                {
                    bordarota.Visibility = Visibility.Collapsed;
                    PainelDesvio.Visibility = Visibility.Collapsed;
                    AlertaRotaSeguraPanel.Visibility = Visibility.Collapsed;

                    AlertaTextBlock.Text = "✅ Rota Segura";
                    AlertaTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(11, 30, 63));
                    AlertaPanel.Background = new SolidColorBrush(Color.FromRgb(135, 206, 235));

                    AlertaPanel.Visibility = Visibility.Visible;
                    AlertaRotaSeguraPanel.Visibility = Visibility.Collapsed;
                });

                return;
            }

            // 4. ROTAS DIFERENTES = rota passa por alagamento
            if (mensagem == "rotas_diferentes")
            {
                rotasSaoIguais = false;

                Dispatcher.Invoke(() =>
                {
                    bordarota.Visibility = Visibility.Visible;
                    PainelDesvio.Visibility = Visibility.Visible;
                    AlertaPanel.Visibility = Visibility.Visible;
                });

                return;
            }

            // 5. ALERTAS DE RISCO (somente se não for segura)
            if (!rotasSaoIguais)
            {
                Dispatcher.Invoke(() =>
                {
                    switch (mensagem)
                    {
                        case "risco_medio":
                            AlertaTextBlock.Text = "⚠️ Nível Médio de Risco";
                            AlertaTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(11, 30, 63));
                            AlertaPanel.Background = new SolidColorBrush(Color.FromRgb(255, 215, 0));
                            bordarota.Visibility = Visibility.Visible;
                            PainelDesvio.Visibility = Visibility.Visible;
                            AlertaPanel.Visibility = Visibility.Visible;
                            AlertaRotaSeguraPanel.Visibility = Visibility.Visible;
                            break;

                        case "risco_alto":
                            AlertaTextBlock.Text = "🚨 Risco Alto de Alagamento";
                            AlertaTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(11, 30, 63));
                            AlertaPanel.Background = new SolidColorBrush(Color.FromArgb(100, 255, 0, 0));
                            bordarota.Visibility = Visibility.Visible;
                            PainelDesvio.Visibility = Visibility.Visible;
                            AlertaPanel.Visibility = Visibility.Visible;
                            AlertaRotaSeguraPanel.Visibility = Visibility.Visible;
                            break;

                        case "risco_baixo":
                            AlertaTextBlock.Text = "✅ Nível Baixo - Rota Segura";
                            AlertaTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(11, 30, 63));
                            AlertaPanel.Background = new SolidColorBrush(Color.FromRgb(135, 206, 235));
                            bordarota.Visibility = Visibility.Visible;
                            PainelDesvio.Visibility = Visibility.Visible;
                            AlertaPanel.Visibility = Visibility.Visible;
                            AlertaRotaSeguraPanel.Visibility = Visibility.Visible;
                            break;

                        case "seguro":
                            AlertaTextBlock.Text = "✅ Rota Segura";
                            AlertaTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(11, 30, 63));
                            AlertaPanel.Background = new SolidColorBrush(Color.FromRgb(135, 206, 235));
                            bordarota.Visibility = Visibility.Collapsed;
                            PainelDesvio.Visibility = Visibility.Collapsed;
                            AlertaRotaSeguraPanel.Visibility = Visibility.Collapsed;
                            break;
                    }
                });
            }
        }




        private string ObterHtmlRotaDireta(string origem, string destino)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Rota Direta</title>
    <style>html, body, #map {{ height: 100%; margin: 0; padding: 0; }}</style>
</head>
<body onload=""initMap()"">
    <div id='map'></div>
    <!-- Google Maps API com callback -->
    <script src='https://maps.googleapis.com/maps/api/js?key=AIzaSyAJjmRAlbQP3ADQnlcFior3_n48KhLS17Q&callback=initMap&libraries=geometry'></script>
    <script>
        let map;
        let directionsRenderer;
        let directionsService;
        let overviewPolyline = null;

        function initMap() {{
            const centro = {{ lat: -25.4284, lng: -49.2733 }};
            map = new google.maps.Map(document.getElementById('map'), {{
                zoom: 13,
                center: centro
            }});
            directionsService = new google.maps.DirectionsService();
            directionsRenderer = new google.maps.DirectionsRenderer({{
                map: map,
                suppressMarkers: false,
                polylineOptions: {{ strokeColor: '#FF4500', strokeWeight: 4 }}
            }});
            calcularRota('#FF4500');
        }}

        function calcularRota(corLinha) {{
            directionsRenderer.setOptions({{
                polylineOptions: {{
                    strokeColor: corLinha,
                    strokeWeight: 4
                }}
            }});

            directionsService.route({{
                origin: '{origem}',
                destination: '{destino}',
                travelMode: 'DRIVING'
            }}, function(result, status) {{
                if (status === 'OK') {{
                    directionsRenderer.setDirections(result);
                    overviewPolyline = result.routes[0].overview_polyline;

                    const distanciaTotal = result.routes[0].legs[0].distance.text;
                    const duracaoTotal = result.routes[0].legs[0].duration.text;

                    window.chrome.webview.postMessage('distancia:' + distanciaTotal);
                    window.chrome.webview.postMessage('tempo:' + duracaoTotal);


                    // Envia também a polyline para comparar no desvio
                    window.chrome.webview.postMessage('rotaDireta_polyline:' + overviewPolyline);
                }}
            }});
        }}

        function valordosensor(tipo) {{
            const centro = {{ lat: -25.4372, lng: -49.2328537 }};
            const icone = `icone${{tipo}}.png`;

            new google.maps.Marker({{
                position: centro,
                map: map,
                icon: icone
            }});

            if (tipo === 1) {{
                window.chrome.webview.postMessage('risco_baixo');
                calcularRota('#1E90FF');
            }} else if (tipo === 2) {{
                window.chrome.webview.postMessage('risco_medio');
                calcularRota('#FFD700');
            }} else if (tipo === 3) {{
                window.chrome.webview.postMessage('risco_alto');
                calcularRota('#FF0000');
            }} else if (tipo === 4) {{
                window.chrome.webview.postMessage('seguro');
                calcularRota('#1E90FF');
            }}
        }}
    </script>
</body>
</html>";
        }

        private string ObterHtmlRotaDesvio(string origem, string destino)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Rota com Desvio</title>
    <style>html, body, #map {{ height: 100%; margin: 0; padding: 0; }}</style>
</head>
<body onload='initMap()'>
    <div id='map'></div>
    <script src='https://maps.googleapis.com/maps/api/js?key=AIzaSyAJjmRAlbQP3ADQnlcFior3_n48KhLS17Q&callback=initMap&libraries=geometry'></script>
    <script>
        let map;
        let directionsService;
        let directionsRenderer;
        let overviewPolyline = null;
        let rotaDiretaPolyline = null;

        window.chrome.webview.addEventListener('message', event => {{
            const msg = event.data;
            if (msg.startsWith('rotaDireta_polyline:')) {{
                rotaDiretaPolyline = msg.replace('rotaDireta_polyline:', '');
                if (map && directionsService) {{
                    calcularRotaDesvio();
                }}
            }}
        }});

        function initMap() {{
            const pontoEvitar = {{ lat: -25.445778776393947, lng: -49.245404019567964 }};
            const pontoDesvio = {{ lat: pontoEvitar.lat + 0.01, lng: pontoEvitar.lng }};

            map = new google.maps.Map(document.getElementById('map'), {{
                zoom: 13,
                center: pontoEvitar
            }});

            directionsService = new google.maps.DirectionsService();
            directionsRenderer = new google.maps.DirectionsRenderer({{
                map: map,
                polylineOptions: {{ strokeColor: '#00BFFF', strokeWeight: 4 }}
            }});
        }}

        function calcularRotaDesvio() {{
            const geocoder = new google.maps.Geocoder();
            geocoder.geocode({{ address: '{destino}' }}, function(results, status) {{
                if (status === 'OK' && results[0]) {{
                    const destinoLatLng = results[0].geometry.location;
                    const pontoEvitar = new google.maps.LatLng(-25.445778776393947, -49.245404019567964);
                    const pontoDesvio = new google.maps.LatLng(pontoEvitar.lat() + 0.01, pontoEvitar.lng());

                    const distancia = google.maps.geometry.spherical.computeDistanceBetween(pontoEvitar, destinoLatLng);

                    let request;
                    if (distancia < 1000) {{
                        request = {{
                            origin: '{origem}',
                            destination: '{destino}',
                            travelMode: 'DRIVING',
                            waypoints: [{{ location: pontoDesvio, stopover: false }}]
                        }};
                    }} else {{
                        request = {{
                            origin: '{origem}',
                            destination: '{destino}',
                            travelMode: 'DRIVING'
                        }};
                    }}

                    directionsService.route(request, function(result, status) {{
                        if (status === 'OK') {{
                            directionsRenderer.setDirections(result);
                            overviewPolyline = result.routes[0].overview_polyline;

                            const distanciaTotalDesvio = result.routes[0].legs[0].distance.text;
                            const duracaoTotalDesvio = result.routes[0].legs[0].duration.text;

                            window.chrome.webview.postMessage('distancia:' + distanciaTotalDesvio);
                            window.chrome.webview.postMessage('tempo:' + duracaoTotalDesvio);


                            if (rotaDiretaPolyline === overviewPolyline) {{
                                window.chrome.webview.postMessage('rotas_iguais');
                            }} else {{
                                window.chrome.webview.postMessage('rotas_diferentes');
                            }}
                        }}
                    }});
                }}
            }});
        }}
    </script>
</body>
</html>";
        }
    }
}




