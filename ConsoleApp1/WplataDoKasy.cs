using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using InsERT.Moria;
using InsERT.Moria.Sfera;
using System.Diagnostics;
using InsERT.Mox.DatabaseAccess;
using System.Data.Common;
using InsERT.Moria.Asortymenty;
using InsERT.Moria.ModelDanych;
using InsERT.Moria.Klienci;
using InsERT.Moria.Kasa;
using InsERT.Moria.Dokumenty.Logistyka;
using InsERT.Moria.ModelOrganizacyjny;
using InsERT.Moria.Wydruki;
using InsERT.Moria.Wydruki.Enums;
using InsERT.Mox.Validation;
using InsERT.Moria.Urzadzenia;
using InsERT.Moria.Rozszerzanie;

namespace ConsoleApp1
{
    class WplataDoKasy
    {

        static Uchwyt sfera = Program.sfera;

        static StanowiskoKasowe stanowiskoKasowe;
        static IOperacjeKasowe operacjeKasowe;

        static Podmiot klient;

        static SesjaKasowa biezacaSesja;




        int NumerRachunku;
        FormaPlatnosci formaPlatnosci;

        bool BladPodczasOperacjiKasowej = false;

        public struct Operacja
        {
            public Decimal Kwota;
            public string Tytul;

            public Operacja(Double k, string t)
            {
                Kwota = Convert.ToDecimal(k);
                Tytul = t;
            }
        }

        static DateTime DateNow = DateTime.Now;

        public static void ZdefiniujDate()
        {
            DateNow = DateTime.Now;
        }

        public static bool PrzygotujKase()
        {
            IStanowiskaKasowe stanowiskaKasowe = sfera.PodajObiektTypu<IStanowiskaKasowe>();

            var SymbolKasy = Program.config.GetValue("Kasa", "symbol");

            var ZnajdzStanowisko = stanowiskaKasowe.Dane.Wszystkie().Where(s => s.Symbol == SymbolKasy);
            if (ZnajdzStanowisko.Count() == 0)
            {
                Console.WriteLine("Nie znaleziono kasy z symbolem: " + SymbolKasy);
                return false;
            }
            stanowiskoKasowe = ZnajdzStanowisko.FirstOrDefault();
            if (stanowiskoKasowe.BiezacaSesja == null)
            {
                Console.WriteLine("Brak otwartej sesji kasowej");
                return false;
            }

            biezacaSesja = stanowiskoKasowe.BiezacaSesja;


            operacjeKasowe = sfera.PodajObiektTypu<IOperacjeKasowe>();

            IPodmioty podmioty = sfera.PodajObiektTypu<IPodmioty>();

            string NIPKlienta = Program.config.GetValue("Klient", "NIP");
            //Console.WriteLine("NIP klienta: " + NIPKlienta);
            klient = podmioty.Dane.Wszystkie().Where(p => p.NIP == NIPKlienta).First();

            return true;

        }

        static List<string> FormyPlatnosci = new List<string>() {"PayLand" , "Gotówka" };

        public WplataDoKasy(List<Operacja> ListaOperacji, int NR = 1, bool zaplaconoKarta = false)
        {
            NumerRachunku = NR;

            string NazwaFormyPlatnosci = FormyPlatnosci[(zaplaconoKarta ? 0 : 1)];
            Console.WriteLine("Nazwa formy płatności: {0}", NazwaFormyPlatnosci);
            IFormyPlatnosci mgr = sfera.PodajObiektTypu<IFormyPlatnosci>();
            formaPlatnosci = mgr.Dane.Wszystkie().Where(o => o.Nazwa ==  NazwaFormyPlatnosci).FirstOrDefault();

            Console.Write("Płatność: "); Console.Write(formaPlatnosci.Id); Console.Write(" - ");
            Console.WriteLine(formaPlatnosci.Nazwa);

            BladPodczasOperacjiKasowej = false;

            ListaOperacji.ForEach(WykonajOperacjeKasowa);

            if(!BladPodczasOperacjiKasowej)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Pomyślnie dodano operacje kasowe.");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Wystąpił problem podczas dodawania operacji kasowych.");
            }

            Console.ResetColor();

        }

        private void WykonajOperacjeKasowa (Operacja operacja)
        {
            
            using (IOperacjaKasowa kp = operacjeKasowe.Utworz())
            {

                kp.ZarezerwujNumer();

                kp.Dane.Stanowisko = stanowiskoKasowe;
                kp.Dane.Kwota = operacja.Kwota;

                kp.Dane.Wplyw = true;
                kp.Dane.Sesja = biezacaSesja;

                kp.UstawPodmiot(klient);
                kp.Dane.Tytul = string.Format("[{0}] [{1}] -> {2}", NumerRachunku, DateNow.ToString() , operacja.Tytul);

                kp.Dane.FormaPlatnosci = formaPlatnosci;

                /* W przypadku płatności kartą */
                if (FormyPlatnosci.IndexOf(formaPlatnosci.Nazwa) == 0)
                {
                    //Console.WriteLine("Zmiana numeru płatności dla karty");

                    kp.Dane.Numer = "PayLand/Card/";
                    kp.Dane.Gotowkowa = false;
                }

                if (kp.Zapisz())
                {
                    //Console.WriteLine("Wykonano operację kasową");
                }
                else
                {
                    BladPodczasOperacjiKasowej = true;

                    Console.WriteLine("!! Błąd wykonywania operacji kasowej !!");
                    Console.WriteLine(kp.Bledy);
                    //MessageBox.Show("błędy");
                    //MessageBox.Show(kp.Bledy.ToString());
                    Console.WriteLine(kp.Bledy.ToString());
                }
            }
        }

    }
}
