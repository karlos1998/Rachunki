using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InsERT.Moria.ModelDanych;
using InsERT.Moria.Sfera;
using InsERT.Moria.Asortymenty;
using InsERT.Moria.Klienci;
using InsERT.Moria.Waluty;
using InsERT.Mox.BibliotekaDokumentow.ObiektyBiznesowe;
using InsERT.Mox.Product;
using InsERT.Mox.Runtime;


namespace ConsoleApp1
{
    class Paczka
    {
        static IAsortymenty asortymenty;
        static Dictionary<string, ObiektPaczka> Lista;
        public Dictionary<string, object> result;
        public struct ObiektPaczka
        {
            public string Nazwa;
            public string Symbol;
            public ObiektPaczka (Asortyment s)
            {
                Nazwa = s.Nazwa;
                Symbol = s.Symbol;
            }
        }
        public static Dictionary<string, ObiektPaczka> PobierzListePaczek ()
        {
            asortymenty = Program.sfera.PodajObiektTypu<IAsortymenty>();
            var paczki = asortymenty.Dane.Wszystkie().Where(a => a.Nazwa.Contains("Przesyłka Kurierska"));
            Lista = new Dictionary<string, ObiektPaczka>();
            foreach(var p in paczki)
            {
                Lista.Add(p.Symbol, new ObiektPaczka(p));
            }
            return Lista;
        }
        public Paczka(string Symbol, bool DrukujParagon = true)
        {
            result = Przyjmij(Symbol, DrukujParagon);
        }

        Dictionary<string, object> Przyjmij (string Symbol, bool DrukujParagon = true)
        {
            if (!Lista.ContainsKey(Symbol)) return new Dictionary<string, object>() { { "ok", false }, { "err", "Nie znaleziono takiej paczki" } };

            return PrzyjmijAsortyment.Wykonaj(Symbol, DrukujParagon);

        }
    }
}
