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

        public Dictionary<string, object> result;

        public Dyspozycja()
        {

            result = Wykonaj();
            
        }


        Dictionary<string, object> Wykonaj()
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

    }
}
