using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

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
    class PrzyjmijAsortyment
    {

        public static Dictionary<string, object> Wykonaj (string aSymbol, bool WystawiajParagon)
        {

            StanowiskoKasowe stanowiskoKasowe;
            IStanowiskaKasowe stanowiskaKasowe = Program.sfera.PodajObiektTypu<IStanowiskaKasowe>();

            var SymbolKasy = Program.config.GetValue("Kasa", "symbol");

            var ZnajdzStanowisko = stanowiskaKasowe.Dane.Wszystkie().Where(s => s.Symbol == SymbolKasy);
            if (ZnajdzStanowisko.Count() == 0)
            {
                Console.WriteLine("Nie znaleziono kasy z symbolem: " + SymbolKasy);
                return new Dictionary<string, object>() { { "ok", false }, { "dokument", true }, { "drukuj", false }, { "err", "Nie znaleziono kasy z symbolem '" + SymbolKasy + "'" } };
            }
            stanowiskoKasowe = ZnajdzStanowisko.FirstOrDefault();
            if (stanowiskoKasowe.BiezacaSesja == null)
            {
                Console.WriteLine("Brak otwartej sesji kasowej");
                return new Dictionary<string, object>() { { "ok", false }, { "dokument", true }, { "drukuj", false }, { "err", "Brak otwartej sesji kasowej" } };
            }

            /* 
             * Zdefiniuj wszystkie obiekty dla operacji w NEXO
             */
            var magSymbol = Program.config.GetValue("Doladowania", "magazyn");
            Magazyn mag = Program.sfera.PodajObiektTypu<IMagazyny>().Dane.Wszystkie().Where(m => m.Symbol == magSymbol).FirstOrDefault();
            IAsortymenty asortyment = Program.sfera.PodajObiektTypu<IAsortymenty>();
            IJednostkiMiar jednostkiMiary = Program.sfera.PodajObiektTypu<IJednostkiMiar>();
            IPodmioty podmioty = Program.sfera.PodajObiektTypu<IPodmioty>();
            IStatusyDokumentowDaneDomyslne statusyDD = Program.sfera.PodajObiektTypu<IStatusyDokumentow>().DaneDomyslne;

            Konfiguracja konfPar = Program.sfera.PodajObiektTypu<IKonfiguracje>().DaneDomyslne.Paragon;
            Konfiguracja konfPz = Program.sfera.PodajObiektTypu<IKonfiguracje>().DaneDomyslne.PrzyjecieZewnetrzne;

            IDokumentySprzedazy dokumentySprzedazy = Program.sfera.PodajObiektTypu<IDokumentySprzedazy>();
            IPrzyjeciaZewnetrzne dokumentyPrzyjecia = Program.sfera.PodajObiektTypu<IPrzyjeciaZewnetrzne>();






            /*
             * PZ
             * Zapis do bazy przyjecia zewnetrznego
             * Przyjęciem jest zakup doładowania telefonicznego
             * Sprzedawca: (firma sprzedajaca usluge
             * Nabywca: niezdefiniowano
             */

            using (IPrzyjecieZewnetrzne pz = dokumentyPrzyjecia.Utworz(konfPz))
            {
                pz.Dane.Magazyn = mag;

                var NIPSprzedawcy = Program.config.GetValue("Doladowania", "NIPSprzedawcy");
                var klient = podmioty.Dane.Wszystkie().Where(p => p.NIP == NIPSprzedawcy).FirstOrDefault();
                pz.Dane.Podmiot = klient;

                Asortyment a = asortyment.Dane.Wszystkie().Where(t => t.Symbol == aSymbol).First();
                var poz = pz.Pozycje.Dodaj(a, 1m, a.JednostkaSprzedazy);
                poz.Cena.NettoPrzedRabatem = poz.CenaEwidencyjna;

                // pz.Dane.WystawilaOsoba = podmioty.Dane.Wszystkie().Where(p => p.Osoba != null && p.NazwaSkrocona == "Przykładowy Jan").FirstOrDefault().Osoba;

                //Console.Write("Zapis ");
                if (pz.Zapisz())
                {
                    // MessageBox.Show("Czy mozna zapisac PZ: " + pz.MoznaZapisac);
                    Console.WriteLine("Zaksięgowałem PZ na " + a.Nazwa + " Nr:" + pz.Dane.NumerWewnetrzny.PelnaSygnatura);

                    //Console.WriteLine(pz.Dane.NumerWewnetrzny.PelnaSygnatura);
                }
                else
                {

                    Console.WriteLine("Nie udało się zaksięgować PZ");
                    //Console.WriteLine("Błędy:");
                    //   pz.WypiszBledy();
                }
            }



            if (!WystawiajParagon) return new Dictionary<string, object>() { { "ok", true }, { "dokument", false }, { "drukuj", false } };


            /* * * * * * * * * * * * * * * * * * * * * * * * */


            /* Zapisz paragon */

            string parDoFiskNumerWewnetrzny = "";



            //biezacaSesja = stanowiskoKasowe.BiezacaSesja;


            using (IDokumentSprzedazy fs = dokumentySprzedazy.Utworz(konfPar))
            {
                fs.Dane.Magazyn = mag;

                /* var NIPSprzedawcy = Program.config.GetValue("Doladowania", "NIPSprzedawcy");
                var klient = podmioty.Dane.Wszystkie().Where(p => p.NIP == NIPSprzedawcy).FirstOrDefault();
                fs.Dane.Podmiot = klient; */

                fs.Dane.StatusDokumentu = statusyDD.Sprzedaz_WydanoWykonano;

                fs.Dokument.StanowiskoKasowe = stanowiskoKasowe;
                Console.WriteLine("nazwa stanowiska: " + fs.Dokument.StanowiskoKasowe.Nazwa);

                Asortyment a = asortyment.Dane.Wszystkie().Where(t => t.Symbol == aSymbol).First();
                var poz = fs.Pozycje.Dodaj(a, 1m, a.JednostkaSprzedazy);

                fs.Platnosci.DodajDomyslnaPlatnoscNatychmiastowaNaKwoteDokumentu();

                var kasjer = Program.config.GetValue("Kasa", "kasjer");
                Console.WriteLine("Kasjer z configu: '{0}'", kasjer);
                fs.Dane.WystawilaOsoba = podmioty.Dane.Wszystkie().Where(p => p.Osoba != null && p.Osoba.Pracownik != null && p.NazwaSkrocona == kasjer).FirstOrDefault().Osoba;
                Console.WriteLine("Kasjer: '{0}'", fs.Dane.WystawilaOsoba.Podmiot.NazwaSkrocona);


                Console.Write("Zapis ");
                if (fs.Zapisz())
                {
                    Console.WriteLine(fs.Dane.NumerWewnetrzny.PelnaSygnatura);
                    parDoFiskNumerWewnetrzny = fs.Dane.NumerWewnetrzny.PelnaSygnatura;
                }
                else
                {
                    Console.WriteLine("Błędy:");
                    fs.WypiszBledy();

                    return new Dictionary<string, object>() { { "ok", true }, { "dokument", true }, { "drukuj", false }, { "err", "Nie udało się zapisać dokumentu" } };
                }
            }





            if (parDoFiskNumerWewnetrzny != "")
            {
                /*
                UrzadzenieZewnetrzne drukFisk = Program.sfera.PodajObiektTypu<UrzadzenieZewnetrzne>();

                Console.WriteLine("Nr id drukarki fiskalnej " + drukFisk.Id);
                Console.WriteLine("nazwa drukarki fiskalnej " + drukFisk.Nazwa);
                */

                IFiskalizacjaDokumentu fiskDok = Program.sfera.PodajObiektTypu<IFiskalizacjaDokumentu>();

                IDokumentySprzedazy dokumenty = Program.sfera.PodajObiektTypu<IDokumentySprzedazy>();
                Dokument dokument = dokumenty.Dane.Wszystkie().Where(t => t.NumerWewnetrzny.PelnaSygnatura == parDoFiskNumerWewnetrzny).FirstOrDefault();
                if (dokument == null)
                {
                    Console.WriteLine("Brak dokumentu w bazie");
                    return new Dictionary<string, object>() { { "ok", true }, { "dokument", true }, { "drukuj", false }, { "err", "Brak dokumentu w bazie" } };
                }

                Console.WriteLine("Nr id dokumentu" + dokument.Id);
                
                fiskDok.Fiskalizuj(dokument.Id, IdKasyFiskalnej);
            }
            else Console.WriteLine("Pominięto drukowanie");




            return new Dictionary<string, object>() { { "ok", true }, { "dokument", true }, { "drukuj", true } };
        }

        static int IdKasyFiskalnej;

        public static void ZdefiniujIDKasyFiskalnej ()
        {
            InsERT.Moria.Urzadzenia.Core.IUrzadzeniaZewnetrzne mgr = Program.sfera.PodajObiektTypu<InsERT.Moria.Urzadzenia.Core.IUrzadzeniaZewnetrzne>();
            var encje = mgr.Dane.Wszystkie();

            /*foreach (var encja in encje)
            {
                Console.WriteLine(encja.Id + " - " + encja.Nazwa + " - " + encja.StanowiskoKasowe.Symbol);
            }*/

            var kasaSymbol = Program.config.GetValue("Kasa", "symbol");

            Console.Write("Ustalanie id kasy fiskalnej dla stanowiska kasowego '{0}' - ", kasaSymbol);

            var urzadzenie = mgr.Dane.Wszystkie().Where(a => a.StanowiskoKasowe.Symbol == kasaSymbol).FirstOrDefault();

            if(urzadzenie.Id > 0)
            {
                IdKasyFiskalnej = urzadzenie.Id;
                Console.WriteLine(IdKasyFiskalnej + " -> OK");
            }
            else
            {
                Console.WriteLine("Brak");
            }

        }

    }
}