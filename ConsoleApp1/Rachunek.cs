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

using InsERT.Moria.Sfera;
using InsERT.Moria.Asortymenty;
using InsERT.Moria.ModelDanych;
using InsERT.Moria.Klienci;
using InsERT.Moria.Waluty;
using InsERT.Mox.BibliotekaDokumentow.ObiektyBiznesowe;
using InsERT.Mox.Product;
using InsERT.Mox.Runtime;

using InsERT.Mox.DatabaseAccess;
using System.Data.Common;

namespace ConsoleApp1
{
    class Rachunek : Prowizje
    {

        string getVal(string key)
        {
            if (query.ContainsKey(key))
                return query[key];
            return "";
        }

        Uchwyt sfera = Program.sfera;

        Dictionary<string, string> query;

        public Dictionary<string, object> result = new Dictionary<string, object>();

        Double ProwizjaAgenta, ProwizjaKlienta, prowizjaBP;

        //public bool WymaganaAktualizacjaPodmiotow = false;

        public Rachunek (Dictionary<string, string> q)
        {
            query = q;

            PrzyjmijRachunek();

            if (getVal("usunRachunek").Length > 1) File.Delete(getVal("usunRachunek"));

        }

        private void PrzyjmijRachunek()
        {
            if(getVal("kreskowy") != "1") SprwadzNadawce();

            RodzajRachunku = "ZWYKLY";
            if (query.ContainsKey("przelewUS")) RodzajRachunku = "US";
            else if (getVal("przelewZUS") == "ON") RodzajRachunku = "ZUS";

            //Console.WriteLine("Kwota Rachunku (string): " + getVal("kwota"));

            KwotaRachunku = ConvertToDouble(getVal("kwota"));

            Console.WriteLine("Kwota rachunku: " + KwotaRachunku);

            express = getVal("EXPRESS") == "ON";

            ProwizjaAgenta = ObliczProwizjeAgenta();

            ProwizjaKlienta = ObliczProwizjeKlienta();

            Console.WriteLine("Rodzaj Rachunku: " + RodzajRachunku );

            Console.WriteLine("Ekspresowy: " + (getVal("EXPRESS") == "ON" ? "Tak" : "Nie"));

            Console.WriteLine("Prowizja Agenta: " + ProwizjaAgenta);
            Console.WriteLine("Prowizja Klienta: " + ProwizjaKlienta);

            //prowizjaBP = Math.Floor((ProwizjaKlienta - ProwizjaAgenta) * 100) / 100;
            //prowizjaBP = Math.Floor( (ProwizjaKlienta * 100) - (ProwizjaAgenta * 100) ) / 100;
            prowizjaBP = Math.Ceiling(Math.Round(ProwizjaKlienta * 100 - ProwizjaAgenta * 100, 2)) / 100;
            Console.WriteLine("Prowizja BluePay: " + prowizjaBP);

            var ListaOperacji = new List<WplataDoKasy.Operacja>() {
                new WplataDoKasy.Operacja(ProwizjaAgenta, "Prowizja officeSMART za rachunek"),
                new WplataDoKasy.Operacja(KwotaRachunku, "Kwota rachunku - do pobrania"),
                new WplataDoKasy.Operacja(prowizjaBP, "Prowizja BluePay - do pobrania")
            };
            result.Add("ListaOperacji", ListaOperacji);
            
            /* Dodawanie do KP - zakomentowane, bo będzie jednak dodawane potem, po podsumowaniu */
            //new WplataDoKasy(ListaOperacji);

            ZapiszDoPliku(ListaOperacji);

        }

        /*
        Podmiot ZnajdzNadawce (IEnumerable<Podmiot> WszystkiePodmioty)
        {
            foreach (var p in WszystkiePodmioty)
            {
                var ident = getVal("identyfikator");
                if(ident.Length > 1) switch (getVal("typ_identyfikatora"))
                {
                    case "P":
                        if (p.Osoba != null && p.Osoba.PESEL == ident) return p;
                        break;
                    case "N":
                        if (p.NIP == ident) return p;
                        break;
                    case "R":
                        if (p.Firma != null && p.Firma.REGON == ident) return p;
                        break;
                }

                if (!(p.AdresPodstawowy != null &&
                    p.AdresPodstawowy.Szczegoly != null &&
                    (p.AdresPodstawowy.Szczegoly.Miejscowosc == "" || p.AdresPodstawowy.Szczegoly.Miejscowosc == getVal("miejscowosc_nadawcy")) &&
                    (p.AdresPodstawowy.Szczegoly.Ulica == "" || p.AdresPodstawowy.Szczegoly.Ulica == getVal("ulica_nadawcy")) &&
                    (p.AdresPodstawowy.Szczegoly.NrDomu == "" || p.AdresPodstawowy.Szczegoly.NrDomu == getVal("numer_domu_nadawcy"))
                )) continue;

                

                if (getVal("telefon_nadawcy") != "" && p.Telefon == getVal("telefon_nadawcy")) return p;

                if (getVal("podmiotTyp") == "OF" && p.NazwaSkrocona == (getVal("imie_nadawcy") + " " + getVal("nazwisko_nadawcy"))) return p;
                if (getVal("podmiotTyp") == "F" && p.NazwaSkrocona == getVal("nazwa_nadawcy")) return p;

            }
            return null;
        }
        */

        Program.UproszczonyPodmiot? ZnajdzNadawce()
        {
            var WszystkiePodmioty = Program.PobierzListePodmiotow();
            foreach (var ob in WszystkiePodmioty)
            {
                var p = ob.Value;

                var ident = getVal("identyfikator");
                if (ident.Length > 1) switch (getVal("typ_identyfikatora"))
                {
                    case "P":
                        if (p.PESEL == ident) return p;
                        break;
                    case "N":
                        if (p.NIP == ident) return p;
                        break;
                    case "R":
                        if (p.Firma && p.REGON == ident) return p;
                        break;
                }

                if (!(
                    (p.adres.Miejscowosc == "" || p.adres.Miejscowosc.Trim() == getVal("miejscowosc_nadawcy")) &&
                    (p.adres.Ulica == "" || p.adres.Ulica.Trim() == getVal("ulica_nadawcy")) &&
                    (p.adres.NrDomu == "" || p.adres.NrDomu.Trim() == getVal("numer_domu_nadawcy"))
                )) continue;



                if (getVal("telefon_nadawcy") != "" && p.Telefon == getVal("telefon_nadawcy")) return p;

                if (getVal("podmiotTyp") == "OF" && p.NazwaSkrocona == (getVal("imie_nadawcy") + " " + getVal("nazwisko_nadawcy"))) return p;
                if (getVal("podmiotTyp") == "F" && p.NazwaSkrocona == getVal("nazwa_nadawcy")) return p;
                
            }
            return null;
        }

        private void SprwadzNadawce()
        {

            IPodmiot PodmiotNadawca = null;
            IPodmioty podmioty = sfera.PodajObiektTypu<IPodmioty>();
            var wszystkiePodmioty = podmioty.Dane.Wszystkie();
            /*
            var wszystkiePodmioty = podmioty.Dane.Wszystkie();
            var znaleziony = ZnajdzNadawce(wszystkiePodmioty);

            if (znaleziony != null)
                PodmiotNadawca = podmioty.Znajdz(znaleziony);
            
            */

            var znaleziony = ZnajdzNadawce();
            if (znaleziony != null && znaleziony.Value.Id > 0)
            {
                Console.WriteLine("Znaleziono uproszczonego nadawce: [{0}] {1}", znaleziony.Value.Id, znaleziony.Value.NazwaSkrocona);

                int znalezioneID = znaleziony.Value.Id;
                Podmiot znalezionyPodmiot;

                if(getVal("podmiotTyp") == "OF") znalezionyPodmiot = podmioty.Dane.WszystkieOsoby().Where(p => p.Id == znalezioneID).FirstOrDefault();
                else znalezionyPodmiot = podmioty.Dane.WszystkieFirmy().Where(p => p.Id == znalezioneID).FirstOrDefault();

                if (znalezionyPodmiot != null) PodmiotNadawca = podmioty.Znajdz(znalezionyPodmiot);

            }


            if (PodmiotNadawca == null)
            {


                Console.WriteLine("Tworzenie nowego nadawcy");

                IPodmiot nadawca;

                if (getVal("podmiotTyp") == "OF")
                {

                    nadawca = podmioty.UtworzOsobe();

                    /*
                    var nazwa = getVal("nazwa_nadawcy").Split(' ');
                    nadawca.Dane.Osoba.Imie = nazwa[0];
                    nadawca.Dane.Osoba.Nazwisko = nazwa[1];
                    */

                    nadawca.Dane.Osoba.Imie = getVal("imie_nadawcy");
                    nadawca.Dane.Osoba.Nazwisko = getVal("nazwisko_nadawcy");

                }
                else  /* if(getVal("podmiotTyp") == "F") */
                {
                    nadawca = podmioty.UtworzFirme();

                    //int id = podmioty.Dane.Wszystkie().Select(a => a.Id).Max() + 1;
                    nadawca.Dane.Firma.Nazwa = getVal("nazwa_nadawcy");
                    nadawca.Dane.NazwaSkrocona = getVal("nazwa_nadawcy");
                    //nadawca.Dane.Firma.N

                }


                nadawca.Dane.AdresPodstawowy = UtworzAdres(nadawca);

                if(getVal("telefon_nadawcy") != null && getVal("telefon_nadawcy") != "" && getVal("telefon_nadawcy").Length > 6) DodajKontakt(nadawca);

                //nadawca.Dane.Cechy.Add();

                switch (getVal("typ_identyfikatora"))
                {
                    case "P":
                        if (getVal("podmiotTyp") == "OF") nadawca.Dane.Osoba.PESEL = getVal("identyfikator");
                        break;
                    case "N":
                        if (getVal("podmiotTyp") == "F") nadawca.Dane.NIP = getVal("identyfikator");
                        break;
                }


                if (!query.ContainsKey("przelewUS")) nadawca.Dane.Notatki.Add(DodajOdbiorce());

                nadawca.Dane.Cechy.Add(DodajCecheRachunkowa());

                if (nadawca.Zapisz())
                {
                    Console.WriteLine("Zapisano nadawce");
                    Program.AktualizujPodmiotWLiscie(nadawca.Dane, true);
                }
                else
                {
                    nadawca.WypiszBledy();
                    Console.WriteLine("Błąd zapisu nowego nadawcy");
                }
            }
            else
            {

                /* * *
                 * Aktualizownie istniejącego nadawcy (podmiotu)
                 */

                Console.WriteLine("Aktualizowanie nadawcy: ({0}) {1}", PodmiotNadawca.Dane.Id, PodmiotNadawca.Dane.NazwaSkrocona);

                PodmiotNadawca.Odblokuj();

                if (PodmiotNadawca.Dane.Osoba != null)
                {
                    PodmiotNadawca.Dane.Osoba.Imie = getVal("imie_nadawcy");
                    PodmiotNadawca.Dane.Osoba.Nazwisko = getVal("nazwisko_nadawcy");
                }
                else if(PodmiotNadawca.Dane.Firma != null)
                {
                    PodmiotNadawca.Dane.Firma.Nazwa = getVal("nazwa_nadawcy");
                }
                else PodmiotNadawca.Dane.NazwaSkrocona = getVal("nazwa_nadawcy");

                PodmiotNadawca.Dane.AdresPodstawowy = UtworzAdres(PodmiotNadawca);

                //Console.WriteLine("TELEFON " + PodmiotNadawca.Dane.Telefon);
                if (PodmiotNadawca.Dane.Telefon == "" && PodmiotNadawca.Dane.Telefon != getVal("telefon_nadawcy")) DodajKontakt(PodmiotNadawca);

                if (!query.ContainsKey("przelewUS")) //Jeśli nie jest to przelew US, czyli jest zwykły
                {

                    Notatka notka = DodajOdbiorce();

                    /* Jeśli nie ma jeszcze notatki z taką treścia (takiego odbiorcy) to dodaj*/
                    if (PodmiotNadawca.Dane.Notatki.Filter(a => a.Tresc == notka.Tresc).ToList().Count == 0)
                    {
                        PodmiotNadawca.Dane.Notatki.Add(notka);
                    }

                }

                PodmiotNadawca.Dane.Cechy.Add(DodajCecheRachunkowa());
                if(PodmiotNadawca.Zapisz())
                {
                    Program.AktualizujPodmiotWLiscie(PodmiotNadawca.Dane);
                }
                else
                {
                    PodmiotNadawca.Odblokuj();
                    Console.WriteLine("Nie udało się zaktualizować nadawcy");
                    PodmiotNadawca.WypiszBledy();
                }
            }

            if (getVal("podmiotTyp") == "OF")
                query["nazwa_nadawcy"] = getVal("imie_nadawcy") + " " + getVal("nazwisko_nadawcy");

            //result.Add("sql", sql_result);
            //result.Add("sql_string", select);

        }

        private AdresPodmiotu UtworzAdres (IPodmiot nadawca)
        {
            IPanstwa panstwa = sfera.PodajObiektTypu<IPanstwa>();

            //if (nadawca.Dane.Adresy.Count > 0) return nadawca.Dane.AdresPodstawowy;

            AdresPodmiotu adres = nadawca.DodajAdres();
            //AdresPodmiotu adres = new AdresPodmiotu();
            adres.Szczegoly.Ulica = getVal("ulica_nadawcy");
            adres.Szczegoly.NrDomu = getVal("numer_domu_nadawcy");
            adres.Szczegoly.NrLokalu = getVal("numer_lokalu_nadawcy");
            adres.Szczegoly.KodPocztowy = getVal("kod_pocztowy_nadawcy");
            adres.Szczegoly.Miejscowosc = getVal("miejscowosc_nadawcy");
            adres.Panstwo = panstwa.Dane.Wszystkie().Where(p => p.Nazwa.CompareTo("Polska") == 0).FirstOrDefault();

            return adres;
        }

        void DodajKontakt (IPodmiot podmiot)
        {

            IRodzajeKontaktuDaneDomyslne rodzajeKontaktuDD = sfera.PodajObiektTypu<IRodzajeKontaktu>().DaneDomyslne;
            var kontakt = new Kontakt();
            podmiot.Dane.Kontakty.Add(kontakt);
            kontakt.Rodzaj = rodzajeKontaktuDD.Telefon;
            kontakt.Wartosc = getVal("telefon_nadawcy");
            kontakt.Podstawowy = true;
        }

        private Notatka DodajOdbiorce()
        {


            Console.WriteLine("Dodawanie notatki do nadawcy");
            //odbiorca||321654897||Adam Przykładowy||Miejscowość||Uliczka 11||01-132
            Notatka notka = new Notatka();
            notka.Tresc = string.Join("||", new List<string>() { "odbiorca", getVal("nr_rachunku"), getVal("nazwa_odbiorcy"), getVal("miejscowosc_odbiorcy"), getVal("ulica_odbiorcy"), getVal("kod_pocztowy_odbiorcy") });
            notka.Temat = "Rachunki";

            return notka;

        }

        public static string NazwaCechy = "Rachunki";

        private Cecha DodajCecheRachunkowa()
        {



            ICechy cechy = sfera.PodajObiektTypu<ICechy>();
            ICecha cechaBO = cechy.Znajdz(NazwaCechy);

            if (cechaBO == null)
            {
                Console.WriteLine("Nie ma takiej cechy w słowniku: " + NazwaCechy);
                cechaBO = cechy.Utworz();
                cechaBO.Dane.Nazwa = NazwaCechy;
                cechaBO.Zapisz();
            }

            Cecha c = cechaBO.Dane;

            return c;
        }

        static Int64 MicroTime() => (Int64)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;

        void ZapiszDoPliku (List<WplataDoKasy.Operacja> operacje)
        {

            using (StreamWriter sw = File.CreateText(Program.SciezkaDoOczekujacychRachunkow + MicroTime() + ".json"))
            {
                sw.Write(JsonConvert.SerializeObject(
                    new Dictionary<string, object>() {
                        { "query", query },
                        { "operacje", operacje },
                        { "plikUtworzono",  DateTime.Now.ToString() }
                    }
                ));
            }
        }

    }
}
