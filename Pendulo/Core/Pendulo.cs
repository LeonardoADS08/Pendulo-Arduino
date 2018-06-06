using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Core
{
    public class Pendulo
    {
        public static string DireccionArchivo = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "output.txt");
        
        public struct Datos
        {
            public float Periodo;
            public float Gravedad;
            public float VelocidadAngular;
        }

        public static Datos LeerDatos()
        {
            string text = System.IO.File.ReadAllText(DireccionArchivo);
            List<string> datos = Regex.Split(text, "\n").ToList();
            Datos resultado = new Datos();
            resultado.Periodo = Convert.ToSingle(datos[0]);
            resultado.Gravedad = Convert.ToSingle(datos[1]);
            //resultado.VelocidadAngular = Convert.ToSingle(datos[2]);
            return resultado;
        }

    }
}
