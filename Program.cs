using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace ScriptCSVToXML
{
    // Una pelicula del catalogo
    class Pelicula
    {
        public string Title = "";
        public string Genre = "";
        public string Premiere = "";
        public string Runtime = "";
        public string IMDBScore = "";
        public string Language = "";

        // Saca el año de la fecha de estreno (p. ej. "August 5, 2019" -> 2019)
        public int? Anio()
        {
            Match m = Regex.Match(Premiere, @"\d{4}");
            return m.Success ? int.Parse(m.Value) : (int?)null;
        }
    }

    class Program
    {
        // Cabecera del CSV
        static readonly string[] Cabecera = { "Title", "Genre", "Premiere", "Runtime", "IMDB Score", "Language" };

        // Rutas por defecto
        static string rutaCsvEntrada = "NetflixOriginals.csv";
        static string rutaXmlSalida = "NetflixOriginals.xml";
        static string rutaXmlEntrada = "NetflixOriginals.xml";
        static string rutaCsvSalida = "NetflixOriginals_exportado.csv";

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            bool salir = false;

            while (!salir)
            {
                Console.WriteLine();
                Console.WriteLine("========= FLUJO DE FICHEROS =========");
                Console.WriteLine("De CSV a XML");
                Console.WriteLine("  1. Elegir ruta del CSV de entrada");
                Console.WriteLine("  2. Elegir ruta donde guardar el XML");
                Console.WriteLine("  3. Exportar CSV -> XML");
                Console.WriteLine("De XML a CSV");
                Console.WriteLine("  4. Elegir ruta del XML de entrada");
                Console.WriteLine("  5. Elegir ruta donde guardar el CSV");
                Console.WriteLine("  6. Exportar XML -> CSV");
                Console.WriteLine("0. Salir");
                Console.Write("Opcion: ");

                switch (LeerLinea())
                {
                    case "1": rutaCsvEntrada = PedirRuta("Ruta del CSV", rutaCsvEntrada); break;
                    case "2": rutaXmlSalida = PedirRuta("Ruta del XML de salida", rutaXmlSalida); break;
                    case "3": Exportar(true); break;
                    case "4": rutaXmlEntrada = PedirRuta("Ruta del XML", rutaXmlEntrada); break;
                    case "5": rutaCsvSalida = PedirRuta("Ruta del CSV de salida", rutaCsvSalida); break;
                    case "6": Exportar(false); break;
                    case "0": salir = true; Console.WriteLine("Hasta luego."); break;
                    default: Console.WriteLine("Opcion no valida."); break;
                }
            }
        }

        // ------------------------------------------------------------------
        // Menus
        // ------------------------------------------------------------------
        static string LeerLinea()
        {
            return (Console.ReadLine() ?? "").Trim();
        }

        static string PedirRuta(string descripcion, string actual)
        {
            Console.Write($"{descripcion} [actual: {actual}]: ");
            string nueva = LeerLinea().Trim('"');
            string ruta = nueva == "" ? actual : nueva;
            Console.WriteLine($"Ruta guardada: {ruta}");
            return ruta;
        }

        // csvAXml = true -> CSV a XML ; false -> XML a CSV
        static void Exportar(bool csvAXml)
        {
            Console.WriteLine();
            Console.WriteLine(csvAXml ? "--- CSV a XML ---" : "--- XML a CSV ---");
            Console.WriteLine("1. Exportar todo el documento");
            Console.WriteLine("2. Exportar por genero");
            Console.WriteLine("3. Exportar por año");
            Console.WriteLine("4. Exportar por lenguaje");
            Console.WriteLine("0. Volver");
            Console.Write("Opcion: ");
            string opcion = LeerLinea();
            if (opcion == "0") return;

            string origen = csvAXml ? rutaCsvEntrada : rutaXmlEntrada;
            string destino = csvAXml ? rutaXmlSalida : rutaCsvSalida;

            if (!File.Exists(origen))
            {
                Console.WriteLine($"No existe el fichero: {origen}");
                return;
            }

            List<Pelicula> peliculas;
            try
            {
                peliculas = csvAXml ? LeerCsv(origen) : LeerXml(origen);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error al leer {origen}: {e.Message}");
                return;
            }

            List<Pelicula> seleccion = Filtrar(peliculas, opcion);
            if (seleccion == null) return;

            try
            {
                string carpeta = Path.GetDirectoryName(Path.GetFullPath(destino));
                if (!string.IsNullOrEmpty(carpeta)) Directory.CreateDirectory(carpeta);

                if (csvAXml) EscribirXml(seleccion, destino);
                else EscribirCsv(seleccion, destino);

                Console.WriteLine($"Exportadas {seleccion.Count} peliculas a {destino}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error al escribir {destino}: {e.Message}");
            }
        }

        // ------------------------------------------------------------------
        // Filtros
        // ------------------------------------------------------------------
        static List<Pelicula> Filtrar(List<Pelicula> peliculas, string opcion)
        {
            switch (opcion)
            {
                case "1":
                    return peliculas;

                case "2":
                    {
                        // Primera pasada: lista de generos distintos
                        List<string> generos = ValoresDistintos(peliculas.Select(p => p.Genre));
                        string genero = ElegirDeLista(generos, "Genero");
                        if (genero == null) return null;
                        return peliculas.Where(p => p.Genre == genero).ToList();
                    }

                case "3":
                    {
                        Console.Write("Introduce un año (se exportan ese año y los siguientes): ");
                        if (!int.TryParse(LeerLinea(), out int anio))
                        {
                            Console.WriteLine("Año no valido.");
                            return null;
                        }
                        return peliculas.Where(p => p.Anio() != null && p.Anio() >= anio).ToList();
                    }

                case "4":
                    {
                        // Primera pasada: lista de idiomas distintos
                        List<string> idiomas = ValoresDistintos(peliculas.Select(p => p.Language));
                        string idioma = ElegirDeLista(idiomas, "Idioma");
                        if (idioma == null) return null;
                        return peliculas.Where(p => p.Language == idioma).ToList();
                    }

                default:
                    Console.WriteLine("Opcion no valida.");
                    return null;
            }
        }

        static List<string> ValoresDistintos(IEnumerable<string> valores)
        {
            return valores.Where(v => v != "").Distinct().OrderBy(v => v).ToList();
        }

        static string ElegirDeLista(List<string> opciones, string nombre)
        {
            Console.WriteLine();
            Console.WriteLine($"{nombre}s disponibles:");
            for (int i = 0; i < opciones.Count; i++)
                Console.WriteLine($"  {i + 1,3}. {opciones[i]}");

            Console.Write($"Elige el numero del {nombre.ToLower()}: ");
            if (int.TryParse(LeerLinea(), out int n) && n >= 1 && n <= opciones.Count)
                return opciones[n - 1];

            Console.WriteLine("Opcion no valida.");
            return null;
        }

        // ------------------------------------------------------------------
        // CSV
        // ------------------------------------------------------------------
        static List<Pelicula> LeerCsv(string ruta)
        {
            // El CSV original de Netflix viene en latin-1: se prueba UTF-8 y si falla, latin-1
            string[] lineas;
            try
            {
                lineas = File.ReadAllLines(ruta, new UTF8Encoding(false, true));
            }
            catch (DecoderFallbackException)
            {
                lineas = File.ReadAllLines(ruta, Encoding.Latin1);
            }

            var peliculas = new List<Pelicula>();
            if (lineas.Length == 0) return peliculas;

            // Posicion de cada columna segun la cabecera
            List<string> cabecera = SepararCsv(lineas[0]).Select(c => c.Trim().TrimStart('﻿')).ToList();
            int iTitle = cabecera.IndexOf("Title");
            int iGenre = cabecera.IndexOf("Genre");
            int iPremiere = cabecera.IndexOf("Premiere");
            int iRuntime = cabecera.IndexOf("Runtime");
            int iScore = cabecera.IndexOf("IMDB Score");
            int iLanguage = cabecera.IndexOf("Language");

            for (int i = 1; i < lineas.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lineas[i])) continue;
                List<string> c = SepararCsv(lineas[i]);
                peliculas.Add(new Pelicula
                {
                    Title = Campo(c, iTitle),
                    Genre = Campo(c, iGenre),
                    Premiere = Campo(c, iPremiere),
                    Runtime = Campo(c, iRuntime),
                    IMDBScore = Campo(c, iScore),
                    Language = Campo(c, iLanguage)
                });
            }
            return peliculas;
        }

        static string Campo(List<string> campos, int indice)
        {
            return indice >= 0 && indice < campos.Count ? campos[indice].Trim() : "";
        }

        // Separa una linea CSV respetando las comillas (la fecha "August 5, 2019" lleva coma)
        static List<string> SepararCsv(string linea)
        {
            var campos = new List<string>();
            var actual = new StringBuilder();
            bool entreComillas = false;

            for (int i = 0; i < linea.Length; i++)
            {
                char c = linea[i];
                if (entreComillas)
                {
                    if (c == '"')
                    {
                        if (i + 1 < linea.Length && linea[i + 1] == '"') { actual.Append('"'); i++; }
                        else entreComillas = false;
                    }
                    else actual.Append(c);
                }
                else if (c == '"') entreComillas = true;
                else if (c == ',') { campos.Add(actual.ToString()); actual.Clear(); }
                else actual.Append(c);
            }
            campos.Add(actual.ToString());
            return campos;
        }

        static void EscribirCsv(List<Pelicula> peliculas, string ruta)
        {
            using (var sw = new StreamWriter(ruta, false, new UTF8Encoding(false)))
            {
                sw.WriteLine(string.Join(",", Cabecera));
                foreach (Pelicula p in peliculas)
                {
                    string[] campos = { p.Title, p.Genre, p.Premiere, p.Runtime, p.IMDBScore, p.Language };
                    sw.WriteLine(string.Join(",", campos.Select(EscaparCsv)));
                }
            }
        }

        // Pone comillas a los campos que tienen comas o comillas
        static string EscaparCsv(string valor)
        {
            if (valor.Contains(",") || valor.Contains("\"") || valor.Contains("\n"))
                return "\"" + valor.Replace("\"", "\"\"") + "\"";
            return valor;
        }

        // ------------------------------------------------------------------
        // XML
        // ------------------------------------------------------------------
        static void EscribirXml(List<Pelicula> peliculas, string ruta)
        {
            // Raiz <Peliculas>, cada pelicula en <Pelicula> y cada campo con su etiqueta
            // (IMDB_Score lleva guion bajo porque una etiqueta XML no puede tener espacios)
            var raiz = new XElement("Peliculas",
                peliculas.Select(p => new XElement("Pelicula",
                    new XElement("Title", p.Title),
                    new XElement("Genre", p.Genre),
                    new XElement("Premiere", p.Premiere),
                    new XElement("Runtime", p.Runtime),
                    new XElement("IMDB_Score", p.IMDBScore),
                    new XElement("Language", p.Language))));

            var ajustes = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "    ",
                Encoding = new UTF8Encoding(false)
            };
            using (XmlWriter w = XmlWriter.Create(ruta, ajustes))
            {
                new XDocument(new XDeclaration("1.0", "utf-8", null), raiz).Save(w);
            }
        }

        static List<Pelicula> LeerXml(string ruta)
        {
            XDocument doc = XDocument.Load(ruta);
            return doc.Root.Elements("Pelicula").Select(e => new Pelicula
            {
                Title = Texto(e, "Title"),
                Genre = Texto(e, "Genre"),
                Premiere = Texto(e, "Premiere"),
                Runtime = Texto(e, "Runtime"),
                IMDBScore = Texto(e, "IMDB_Score"),
                Language = Texto(e, "Language")
            }).ToList();
        }

        static string Texto(XElement padre, string etiqueta)
        {
            return ((string)padre.Element(etiqueta) ?? "").Trim();
        }
    }
}
