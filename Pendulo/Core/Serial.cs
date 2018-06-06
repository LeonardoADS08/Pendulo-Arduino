using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Diagnostics;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;

namespace Core
{
    public class Serial
    {

        private SerialPort _conexionSerial;
        private List<int> _datos;
        private Thread _hilo;

        public static int INTERVALO = 5;
        private const string FIN = "[FIN]";
        private const string INICIO = "[INICIO]";

        public bool Abierto { get { return _conexionSerial.IsOpen; } }
        public bool Iniciado = false;

        public event EventHandler DatosProcesados;

        public Serial() { }

        public List<string> Puertos() => SerialPort.GetPortNames().ToList();

        public List<int> Velocidades() => new List<int>() { 9600, 19200, 38400, 57600, 74880, 115200, 230400, 250000 };

        public void Conectar(string Puerto, int Velocidad)
        {
            try
            {
                if (_conexionSerial != null && _conexionSerial.IsOpen) _conexionSerial.Close();
                _conexionSerial = new SerialPort(Puerto, Velocidad);
                _conexionSerial.Open();

                // Por si el hilo se encuentra abierto
                if (_hilo != null && _hilo.IsAlive) _hilo.Abort();

                _hilo = new Thread(() => Escuchar());
                _hilo.Start();

                Iniciado = true;
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString());
                return;
            }
        }

        public async void Escuchar()
        {
            _datos = new List<int>();
            // TODO Quitar el debug
            Debug.WriteLine("Nueva toma de datos: ");
            while (_conexionSerial.IsOpen)
            {
                try
                {
                    if (_conexionSerial.ReadBufferSize > 0)
                    {
                        string mensaje = _conexionSerial.ReadLine();
                        if (mensaje != string.Empty)
                        {
                            if (mensaje.Contains(Serial.FIN))
                            {
                                Guardar();
                                break;
                            }
                            else
                            {
                                int temp;
                                if (int.TryParse(mensaje, out temp))
                                    _datos.Add(temp);
                            }
                            // TODO Quitar el debug
                            Debug.WriteLine(mensaje);
                        }
                    }
                }
                catch (Exception ex)
                {
                    //MessageBox.Show(ex.ToString());
                    return;
                }
                
                await Task.Delay(INTERVALO);
            }
           // Liberar();
        }

        public void Guardar()
        {
            try
            {
                // TODO Eliminar filtro simple
                //FiltroSimple();
                string archivo = string.Empty;
                _datos.ForEach(x => archivo += x.ToString() + "\n");
                System.IO.File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "input.txt"), archivo);

                if (File.Exists(Pendulo.DireccionArchivo)) File.Delete(Pendulo.DireccionArchivo);
                string direccion = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "main.py");
                
                //                 ScriptEngine engine = Python.CreateEngine();
                //                 engine.ExecuteFile(direccion);

                ProcessStartInfo start = new ProcessStartInfo();
                string DireccionArchivo = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pythonDir.txt");
                string direccionPython = System.IO.File.ReadAllText(DireccionArchivo);
                start.FileName = direccionPython; //cmd is full path to python.exe
                //start.FileName = @"C:\Users\User\AppData\Local\Programs\Python\Python36\python.exe";
                //start.FileName = @"C:\Archivos de programa\Python36\python.exe"; //cmd is full path to python.exe
                start.Arguments = "main.py";                        
                start.UseShellExecute = false;
                start.RedirectStandardOutput = true;
                start.CreateNoWindow = false;

                using (Process process = Process.Start(start))
                {
                    process.WaitForExit();
                }

                VerificarDatos();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }   
        }

        public async void VerificarDatos()
        {
            while (!File.Exists(Pendulo.DireccionArchivo))
            {
                await Task.Delay(250);
            }

            DatosProcesados?.Invoke(this, EventArgs.Empty);
        }

        public void FiltroSimple()
        {
            List<int> res = new List<int>();
            int acum = 0, cant = 0, maxVariacion = 8;
            for (int i = 0; i < _datos.Count; ++i)
            {
                if (acum == 0 || Math.Abs(acum/cant - _datos[i]) <= maxVariacion )
                {
                    acum += _datos[i];
                    cant++;
                }
                else
                {
                    res.Add(acum / cant);
                    acum = 0;
                    cant = 0;
                }
            }
            // TODO eliminar el debug
            Debug.WriteLine("Datos filtrados: ");

            int val = 1;
            float paux = 0;
            List<float> periodos = new List<float>();
            for (int i = 0; i < res.Count; i++)
            {
                if (val == 4)
                {
                    periodos.Add(paux / 4f - periodos.LastOrDefault());
                    val = 1;
                    paux = 0;
                }
                else
                {
                    paux += res[i];
                    val++;
                }

            }
            periodos.ForEach(x => Debug.WriteLine(x.ToString()));

            Debug.WriteLine("El periodo aproximado es: " + (periodos.Sum()/periodos.Count));
        }

        public void Liberar()
        {
            _conexionSerial?.Close();
            _hilo?.Abort();
            Iniciado = false;
        }
    }
}