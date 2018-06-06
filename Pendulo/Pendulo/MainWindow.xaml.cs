using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
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

namespace Pendulo
{
    public partial class MainWindow : Window
    {
        public Func<double, string> YFormatter { get; set; }
        public SeriesCollection PuntosX { get; set; }

        // TODO Suponiendo que los datos recibidos son floats
        public string[] PuntosY { get; set; }

        Core.Serial serial = new Core.Serial();

        double viscosidadAire = 1.8 * Math.Pow(10, -3);
        double Radio;
        double Masa;
        double LongitudHilo;
        double Amplitud;

        double precision = 0.05f;
        double Maximo = 50;

        Core.Pendulo.Datos Resultado;
        bool DatosObtenidos = false;

        public MainWindow()
        {
            InitializeComponent();

            // Estado inicial es desconectado
            TextoDesconectado();
            OcultarTextos();

            YFormatter = value => Math.Round(value, 2).ToString();
            LimitesGrafica(-1, 1);

            TB_Radio.Text = "1.5";
            TB_Amplitud.Text = "18";
            TB_Masa.Text = "8.85";
            TB_Longiutd.Text = "118.5";

            ActualizarValores();
        }

        public void LimitesGrafica(double Maximo, double Minimo)
        {
            try
            {
                Serie.AxisY.Clear();
                Serie.AxisY.Add(
                new Axis
                {
                    MaxValue = Maximo,
                    MinValue = Minimo,
                    LabelFormatter = YFormatter
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error limitando los valores de la gráfica." + Environment.NewLine + Environment.NewLine + ex.Message, "Error de ejecución", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
            }
        }

        public void InicializarGrafica()
        {
            try
            {
                //Serie = new CartesianChart();

                // La 'distancia' entre punto y punto depende del valor de la variable 'Precision', y la cantidad de puntos depende de la variable 'Maximo'
                List<double> PuntosTiempo = new List<double>();
                for (double i = 0; i < Maximo; i += precision)
                    PuntosTiempo.Add(i);

                List<string> res = new List<string>();
                PuntosTiempo.ForEach(x => res.Add(Math.Round(x, 2).ToString()));
                PuntosY = res.ToArray();

                // Con los puntos anteriormente conseguidos, se pasan los valores por la formula
                List<double> PuntosAmplitud = new List<double>();
                PuntosTiempo.ForEach(x => PuntosAmplitud.Add(ValorGradiente(x, Resultado.Gravedad, Resultado.Periodo)));

                LimitesGrafica(Amplitud + Amplitud/2, -(Amplitud + Amplitud/2));

                var valores = new ChartValues<Point>();
                for (int i = 0; i < PuntosAmplitud.Count; i++)
                {
                    valores.Add(new Point(PuntosTiempo[i], PuntosAmplitud[i]));
                }


                PuntosX = new SeriesCollection
                {
                new LineSeries
                {
                    Title = "Momento de la oscilacion:",
                    Values = new ChartValues<double>(PuntosAmplitud),
                    LineSmoothness = 1,
                    PointGeometry = null,
                    Fill = Brushes.Transparent,
                    Stroke = Brushes.OrangeRed
                }
            };

                DataContext = this;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error graficando con los datos obtenidos." + Environment.NewLine + Environment.NewLine + ex.Message, "Error de ejecución", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
            }


        }
        public double ValorGradiente(double Tiempo, double Gravedad, double Periodo)
        {
            double K = (3 * Math.PI * viscosidadAire * Radio) / Masa;
            double MomentoInercia = (Masa / 5) * (5 * Math.Pow(LongitudHilo, 2) + 2 * Math.Pow(Radio, 2));
            double VelAngularInicial = Math.Sqrt((Masa * LongitudHilo * Gravedad) / MomentoInercia);
            double VelAngular = Math.Sqrt(Math.Pow(VelAngularInicial, 2) - Math.Pow(K, 2));
            double E = Math.Pow(Math.E, -(K * Tiempo));
            return Amplitud * E * Math.Cos(VelAngular * Tiempo);
        }

        public double Valor(double T, double A) => A * Math.Cos(((2*Math.PI)/Resultado.Periodo) * T);


        private void B_Conectar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (serial.Puertos().Count == 0)
                {
                    MessageBox.Show("No se ha encontrado ningún arduino conectado.", "Error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                    return;
                }
                else
                {
                    if (serial?.Iniciado == true) serial.Liberar();
                    serial.Conectar(serial.Puertos().First(), 9600);
                    serial.DatosProcesados += new EventHandler(ResultadosObtenidos);

                    TextoConectado();
                    OcultarTextos();
                    VerificarConexion();
                    DatosObtenidos = false;
                    ActualizarInterfaz();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error intentando conectar mediante serial." + Environment.NewLine + Environment.NewLine + ex.Message, "Error de ejecución", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
            }
           
        }

        private async void VerificarConexion()
        {
            try
            {
                while (serial.Abierto)
                    await Task.Delay(250);

                TextoDesconectado();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error mientras se escuchaba el puerto serial." + Environment.NewLine + Environment.NewLine + ex.Message, "Error de ejecución", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
            }
        }

        private void ResultadosObtenidos(object sender, EventArgs e)
        {
            Resultado = Core.Pendulo.LeerDatos();
            DatosObtenidos = true;
        }

        private async void ActualizarInterfaz()
        {
            while (!DatosObtenidos)
            {
                await Task.Delay(250);
            }

            MostrarTextos();
            L_Periodo.Content = "Periodo: " + Math.Round(Resultado.Periodo, 5);
            L_Gravedad.Content = "Gravedad: " + Math.Round(Resultado.Gravedad, 5);

            InicializarGrafica();
        }

        private void TextoDesconectado()
        {
            L_Estado.Content = "Desconectado";
            L_Estado.Foreground = new SolidColorBrush(Color.FromRgb(192, 57, 43));
        }

        private void TextoConectado()
        {
            L_Estado.Content = "Conectado";
            L_Estado.Foreground = new SolidColorBrush(Color.FromRgb(39, 174, 96));
        }

        private void OcultarTextos()
        {
            L_Gravedad.Visibility = Visibility.Hidden;
            L_Periodo.Visibility = Visibility.Hidden;
        }

        private void MostrarTextos()
        {
            L_Gravedad.Visibility = Visibility.Visible;
            L_Periodo.Visibility = Visibility.Visible;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
           // serial?.Liberar();
        }

        private void TB_Masa_TextChanged(object sender, TextChangedEventArgs e)
        {
            double temp;
            if (Double.TryParse(TB_Masa.Text, out temp))
            {
                Masa = temp;
            }
        }

        private void TB_Amplitud_TextChanged(object sender, TextChangedEventArgs e)
        {
            double temp;
            if (Double.TryParse(TB_Amplitud.Text, out temp))
            {
                Amplitud = temp;
            }
        }

        private void TB_Longiutd_TextChanged(object sender, TextChangedEventArgs e)
        {
            double temp;
            if (Double.TryParse(TB_Longiutd.Text, out temp))
            {
                LongitudHilo = temp;
            }
        }

        private void TB_Radio_TextChanged(object sender, TextChangedEventArgs e)
        {
            double temp;
            if (Double.TryParse(TB_Radio.Text, out temp))
            {
                Radio = temp;
            }
        }


        private void ActualizarValores()
        {
            double temp;
            if (Double.TryParse(TB_Masa.Text, out temp))
            {
                Masa = temp/1000;
            }

            if (Double.TryParse(TB_Amplitud.Text, out temp))
            {
                Amplitud = temp/100;
            }

            if (Double.TryParse(TB_Longiutd.Text, out temp))
            {
                LongitudHilo = temp/100;
            }

            if (Double.TryParse(TB_Radio.Text, out temp))
            {
                Radio = temp/100;
            }
        }

    }
}
