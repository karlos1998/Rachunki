using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace ConsoleApp1
{
    class Doladowanie
    {

        static Dictionary<string, Dictionary<int, string>> Operatorzy;

        public Dictionary<string, object> result;

        static public string MatchKey(string input, string regexstr) => Program.MatchKey(input, regexstr);

        public static void ZdefiniujObiektOperatorow ()
        {

            

            Console.Write("Wczytuję operatorów z plików");
            Operatorzy = new Dictionary<string, Dictionary<int, string>>();
            var path = Program.SciezkaDoListyDoladowan;
            var plikiOperatorow = Directory.EnumerateFiles(path);
            foreach(var plikOperatora in plikiOperatorow)
            {
                var Operator = new Dictionary<int, string>();
                using (StreamReader sr = new StreamReader(plikOperatora))
                {
                    string line;
                    while((line = sr.ReadLine()) != null)
                    {
                        var l = line.Split('=');
                        if (l.Count() != 2) break;
                        //Console.WriteLine(plikOperatora + ": " + line);
                        Operator.Add( Int32.Parse( l[0] ), l[1] );
                    }
                }
                Operatorzy.Add(MatchKey(plikOperatora.Substring(path.Length + 1), "(.*).txt"), Operator);
            }
            Console.WriteLine(" - GOTOWE");
        }

        public static Dictionary<string, Dictionary<int, string>> PobierzOperatorow() => Operatorzy;

        public Doladowanie (string siec, int kwota, bool WystawiajParagon = true)
        {
            result = Doladuj(siec, kwota, WystawiajParagon);
        }

        /// <summary>
        /// Ta klasa cośtam robi.
        /// </summary>
        public Dictionary<string, Object> Doladuj(string siec, int kwota, bool WystawiajParagon)
        {
            Console.WriteLine("Przyjmowanie doładowania...");

            /*
             * Sprawdzenie poprawnosci danych wejsciowych
             */

            if (!Operatorzy.ContainsKey(siec))
            {
                return new Dictionary<string, object>() { { "ok", false }, { "err", "Nie znaleziono operatora '" + siec + "'" } };
            }

            var WybranyOperator = Operatorzy[siec];

            if(!WybranyOperator.ContainsKey(kwota))
            {
                return new Dictionary<string, object>() { { "ok", false }, { "err", "Nie znaleziono kwoty '" + kwota + "' operatora '" + siec + "'" } };
            }

            var aSymbol = WybranyOperator[kwota];

            return PrzyjmijAsortyment.Wykonaj(aSymbol, WystawiajParagon);

        }
    }
}
