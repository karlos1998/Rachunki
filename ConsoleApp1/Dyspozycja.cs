using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InsERT.Moria.Wydruki;
using InsERT.Moria.Wydruki.Enums;
using InsERT.Moria.ModelDanych;
using InsERT.Moria.Klienci;
using InsERT.Moria.Bank;
using InsERT.Moria.Kasa;
using InsERT.Moria.Rozrachunki;

using System.Threading;

namespace ConsoleApp1
{
    class Dyspozycja
    {

        public Dyspozycja()
        {

            
        }


        public static Dictionary<string, object> WykonajRachunki()
        {
            var timeNow = DateTime.Now;
            string DzisiejszyDzien = timeNow.ToString();

            IPodmioty podmioty = Program.sfera.PodajObiektTypu<IPodmioty>();
            IDyspozycjeBankowe dyspozycje = Program.sfera.PodajObiektTypu<IDyspozycjeBankowe>();

            /* DataZlozenia - kolejny dzień, ale nie weekend */
            int DzienTygodnia = (int)DateTime.Now.DayOfWeek;
            DateTime DataZlozenia = DateTime.Now.AddDays(DzienTygodnia >= 5 ? (8 - DzienTygodnia) : 1 );

            using (IDyspozycjaBankowa dyspozycja = dyspozycje.Utworz())
            {
                dyspozycja.Dane.Tytulem = "Rachunki - " + DzisiejszyDzien;
                dyspozycja.Dane.Kwota = 0;

                dyspozycja.Dane.DataZlozenia = DataZlozenia;

                /*Odbiorca - PayLand  */
                Podmiot odbiorca = podmioty.Dane.Wszystkie().Where(a => a.NIP == "5851425854").FirstOrDefault();
                Console.WriteLine("Znaleziono odbiorce: " + odbiorca.NazwaSkrocona);
                dyspozycja.UstawOdbiorce(odbiorca);

                //var rachunek = odbiorca.Rachunki.FirstOrDefault();
                var rachunek = odbiorca.Rachunki.Where(a => a.Numer == "09 2490 0005 0000 4600 9106 7744").FirstOrDefault();
                dyspozycja.UstawRachunekOdbiorcy(rachunek);

                IRozrachunki r = Program.sfera.PodajObiektTypu<IRozrachunki>();
                IQueryable<Rozrachunek> rr = r.Dane.Wszystkie().Where(o =>
                    o.Tytul.Contains("do pobrania") &&
                    o.Podmiot != null &&
                    o.Podmiot.NazwaSkrocona == odbiorca.NazwaSkrocona &&

                    o.DataPowstania.Value.Day == timeNow.Day &&
                    o.DataPowstania.Value.Month == timeNow.Month &&
                    o.DataPowstania.Value.Year == timeNow.Year
                );

                foreach (var o in rr)
                {
                    if (o.Rozliczony()) continue;

                    Console.WriteLine(o.DataPowstania.ToString() + " ---> " + o.Tytul);

                    var f = r.Znajdz(o);
                    f.Odblokuj();

                    dyspozycja.Dane.Kwota += o.Kwota;
                    dyspozycja.Rozrachunek.Rozlicz(o, o.Kwota);

                    f.Odblokuj();

                }

                Console.WriteLine("KWOTA: " + dyspozycja.Dane.Kwota);


                if (dyspozycja.Zapisz())
                {
                    /*
                    IWydruki manager = Program.sfera.PodajObiektTypu<IWydruki>();
                    using (IWydruk wydruk = manager.Utworz(TypWzorcaWydruku.DBStandard))
                    {
                        wydruk.ObiektDoWydruku = dyspozycja.Dane;
                        wydruk.Drukuj();
                    }
                    */
                    
                }
                else
                {
                    dyspozycja.WypiszBledy();
                    return new Dictionary<string, object>() { { "ok", false }, { "err", "Nie udało się zapisać dyspozycji" } };
                }
            }



            /* Dodowanie drugiej dyspozycji, na 1zł */


            using (IDyspozycjaBankowa dyspozycja = dyspozycje.Utworz())
            {
                dyspozycja.Dane.Kwota = 1;
                dyspozycja.Dane.Tytulem = "prow.polec.zapł - " + DzisiejszyDzien;
                Podmiot odbiorca = podmioty.Dane.Wszystkie().Where(a => a.NazwaSkrocona == "Poczta Polska").FirstOrDefault();
                dyspozycja.UstawOdbiorce(odbiorca);
                dyspozycja.Dane.DataZlozenia = DataZlozenia;

                var rachunek = odbiorca.Rachunki.FirstOrDefault();
                dyspozycja.UstawRachunekOdbiorcy(rachunek);

                if (dyspozycja.Zapisz())
                {
                    return new Dictionary<string, object>() { { "ok", true } };
                }
                else
                {
                    dyspozycja.WypiszBledy();
                    return new Dictionary<string, object>() { { "ok", false }, { "err", "Nie udało się zapisać dyspozycji (2)" } };
                }

            }

        }


        public static Dictionary<string, object> WykonajPoczta (decimal Kwota)
        {
            IPodmioty podmioty = Program.sfera.PodajObiektTypu<IPodmioty>();
            IDyspozycjeBankowe dyspozycje = Program.sfera.PodajObiektTypu<IDyspozycjeBankowe>();


            using (IDyspozycjaBankowa dyspozycja = dyspozycje.Utworz())
            {
                dyspozycja.Dane.Kwota = Kwota;
                dyspozycja.Dane.Tytulem = "Wpłata - Utarg";

                Podmiot odbiorca = podmioty.Dane.WszystkieFirmy().Where(f => f.Firma is MojaFirma).First();

                //Podmiot odbiorca = podmioty.Dane.Wszystkie().Where(a => a.Firma is MojaFirma && a.NazwaSkrocona == NazwaKonta).FirstOrDefault();
                Console.WriteLine("Nazwa odbiorcy dyspozycji: '{0}'", odbiorca.NazwaSkrocona);
                
                dyspozycja.UstawOdbiorce(odbiorca);
                dyspozycja.Dane.DataZlozenia = DateTime.Now;

                string NazwaKonta = Program.config.GetValue("Dyspozycja", "nazwaKontaRor");

                //var rachunek = odbiorca.Rachunki.Where(a => a.Wlasciciel.JestMojaFirma() && a.Nazwa == "Pocztowy Firmowe").FirstOrDefault();
                var rachunek = odbiorca.Rachunki.Where(a => a.Wlasciciel.JestMojaFirma() && a.Nazwa == NazwaKonta).FirstOrDefault();

                dyspozycja.UstawRachunekOdbiorcy(rachunek);

                dyspozycja.Dane.Rodzaj = (byte) 1;

                //dyspozycja.Dane.Rozrachunek =

                dyspozycja.Rozrachunkowa = false;


                /*
                 * TODO : zmiana statusu na wykonana
                 */


                if (dyspozycja.Zapisz())
                {
                    IWydruki manager = Program.sfera.PodajObiektTypu<IWydruki>();
                    using (IWydruk wydruk = manager.Utworz(TypWzorcaWydruku.DBStandard))
                    {
                        // wskazanie obiektu do wydruku
                        wydruk.ObiektDoWydruku = dyspozycja.Dane;
                        // wykonanie wydruku
                        wydruk.Drukuj();
                    }

                    Console.WriteLine("Zapisano, a teraz....");

                    /* KW - Wypłata z kasy */
                    IOperacjeKasowe mgr = Program.sfera.PodajObiektTypu<IOperacjeKasowe>();
                    using (var bo = mgr.Utworz())
                    {

                        /* Rezerwacja numeru */
                        bo.ZarezerwujNumer();

                        /* Tytuł */
                        bo.Dane.Tytul = "Przykładowy tytuł";

                        /* Stanowisko */
                        
                        var SymbolKasy = Program.config.GetValue("Dyspozycja", "symbolKasyPrzejsciowej");
                        IStanowiskaKasowe stanowiskaKasowe = Program.sfera.PodajObiektTypu<IStanowiskaKasowe>();

                        var ZnajdzStanowisko = stanowiskaKasowe.Dane.Wszystkie().Where(s => s.Symbol == SymbolKasy);
                        if (ZnajdzStanowisko.Count() == 0)
                        {
                            Console.WriteLine("Nie znaleziono kasy z symbolem: " + SymbolKasy);
                            return new Dictionary<string, object>() { { "ok", false }, { "err", "Nie znaleziono stanowiska kasowego" } };
                        }
                        StanowiskoKasowe stanowiskoKasowe = ZnajdzStanowisko.FirstOrDefault();

                        bo.Dane.Stanowisko = stanowiskoKasowe;

                        /* Kwota */
                        bo.Dane.Kwota = Kwota;

                        /* Rodzaje operacji kasowych */
                        IRodzajeOperacjiKasowych rodzaje = Program.sfera.PodajObiektTypu<IRodzajeOperacjiKasowych>();
                        bo.Dane.Rodzaj = rodzaje.Dane.Wszystkie().Where(s => s.Nazwa == "Transfer").FirstOrDefault();

                        /* Cel Transferu */
                        bo.Dane.TypCGFTransferu = (byte)TypCGFTransferu.Rachunek;

                        /* Rachunek */
                        //IRachunkiBankowe m = Program.sfera.PodajObiektTypu<IRachunkiBankowe>();
                        //var konto = m.Dane.Wszystkie().Where(a => a.Wlasciciel.JestMojaFirma() && a.Nazwa == "Pocztowy Firmowe").FirstOrDefault();
                        //bo.Dane.ElementTransferu.Centrum = konto;


                        string NazwaKontaFirmowe = Program.config.GetValue("Dyspozycja", "nazwaKontaFirmowe");
                        var rachunekFirmowy = odbiorca.Rachunki.Where(a => a.Wlasciciel.JestMojaFirma() && a.Nazwa == NazwaKontaFirmowe).FirstOrDefault();

                        bo.Dane.ElementTransferu.Centrum = rachunek;

                        


                        if (bo.Zapisz())
                        {
                            Console.WriteLine("Krok 3");
                            IOperacjeBankowe mgrr = Program.sfera.PodajObiektTypu<IOperacjeBankowe>();
                            using (var boo = mgrr.Utworz())
                            {
                                IRodzajeOperacjiBankowych rodzajeOB = Program.sfera.PodajObiektTypu<IRodzajeOperacjiBankowych>();
                                boo.Dane.RodzajOperacji = rodzajeOB.Dane.Wszystkie().Where(s => s.Nazwa == "Transfer").FirstOrDefault();

                                boo.Dane.Kwota = Kwota;

                                boo.Dane.Rachunek = rachunek;

                                boo.Dane.TypCGFTransferu = (byte)TypCGFTransferu.Kasa;

                                //boo.PolaczZ(dyspozycja.Dane);

                                boo.Dane.Wplyw = true;

                                boo.Dane.ElementTransferu.Centrum = stanowiskoKasowe;


                                if (!boo.Zapisz())
                                {
                                    boo.WypiszBledy();
                                    return new Dictionary<string, object>() { { "ok", false }, { "err", "Nie udało się zapisać dyspozycji (3)" } };
                                }

                            }

                        }
                        else
                        {
                            bo.WypiszBledy();
                        }
                    }





                    return new Dictionary<string, object>() { { "ok", true } };
                }
                else
                {
                    dyspozycja.WypiszBledy();
                    return new Dictionary<string, object>() { { "ok", false }, { "err", "Nie udało się zapisać dyspozycji (2)" } };
                }

                

            }
        }


    }
}
