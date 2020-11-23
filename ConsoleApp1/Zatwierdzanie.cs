using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
using System.Text.RegularExpressions;

using InsERT.Moria.Sfera;
using InsERT.Moria.Asortymenty;
using InsERT.Moria.ModelDanych;
using InsERT.Moria.Klienci;
using InsERT.Moria.Waluty;
using InsERT.Mox.BibliotekaDokumentow.ObiektyBiznesowe;
using InsERT.Mox.Product;
using InsERT.Mox.Runtime;
using InsERT.Moria.Bank;
using InsERT.Moria.Kasa;

using System.Threading;

using System.Data.Common;


using System.Security.Principal;

namespace ConsoleApp1
{
    class Zatwierdzanie : Program
    {

        public static string ZatwierdzOdlozone ()
        {
            

            bool zatwierdzonoWszystkie = false;

            /* wszystkie pliki z folderu oczekujacych */
            var wszystkieOdlozone = Directory.EnumerateFiles(SciezkaDoOdlozonychRachunkow, "*.json");

            /* oczekujace wykluczajac te, ktore udalo sie juz dodac do listy wlasnia BP */
            var odlozone = wszystkieOdlozone.Where(a => !File.Exists(a.Replace(SciezkaDoOdlozonychRachunkow, SciezkaDoZatwierdzonychRachunkow))).ToList<string>();

            try
            {

                /* sprwadza czy sesja kasowa jest otwarta */
                if (!WplataDoKasy.PrzygotujKase()) return JsonConvert.SerializeObject(new Dictionary<string, object>() { { "ok", false }, { "err", "Brak otwartej sesji kasowej" } });

                /* Datan rachunku (Zawarta w nazwie) */
                WplataDoKasy.ZdefiniujDate();

                /* ilość zatwierdzonych przelewów z listy */
                int UdanePrzelewy = 0;

                /* jesli jest jakis na liscie odlozonych */
                if (odlozone.Count > 0)
                {

                    /* nawiazanie połączenia z BP - logowanie i wyslanie rachunkow */
                    var BPConnect = new BluePay(odlozone.ToArray<string>(), 60 * 1000);

                    /* Blad logowania - przerwij dodawanie rachunkow */
                    if (BPConnect.bladLogowania) return JsonConvert.SerializeObject(new Dictionary<string, object>() { { "ok", false }, { "err", "Nie udało się zalogować do BluePay" } });

                    /* currentFile - aktualny rachunek, wybierany po kolei z listy */
                    foreach (string currentFile in odlozone)
                    {
                        /* Pomijaj rachunek, jeśli wykazal bledy */
                        if (!BluePay.StatusyRachunkow.ContainsKey(currentFile) || BluePay.StatusyRachunkow[currentFile].typ != 2) continue;

                        UdanePrzelewy++;

                        /* kopiuj plik do zatwierdzonych */
                        string fileName = currentFile.Substring(SciezkaDoOczekujacychRachunkow.Length);
                        File.Copy(currentFile, Path.Combine(SciezkaDoZatwierdzonychRachunkow, fileName));
                    }
                }

                if (
                    (odlozone.Count == 0 && wszystkieOdlozone.Count() > 0) ||
                    UdanePrzelewy == odlozone.Count ||
                    /* jesli zaden rachunek nie wykazal bledu */
                    BluePay.StatusyRachunkow.Select(a => a.Value.typ == 0).Count() == 0
                )
                {
                    /* Pousuwaj pliki z foldru oczekujacych, skoro wszystkie sie zatwierdzily */
                    zatwierdzonoWszystkie = WyczyscListeOdlozonych();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());

                if (e.Message == "TIMEOUT")
                {
                    //
                }

                return JsonConvert.SerializeObject(new Dictionary<string, object>() { { "ok", false }, { "err", "Bład Krytyczny" }, { "errInfo", e.Message } });
            }

            return JsonConvert.SerializeObject(new Dictionary<string, object>() { { "ok", true }, { "all", zatwierdzonoWszystkie } });
        }

        public static string ZatwierdzOczekujace (Dictionary<string, string> query)
        {
            /* Zatwierdza aktualne rachunki klienta - dodaje KP w subiekcie  i wysyla do bluepay */

            bool zaplaconoKarta = query.ContainsKey("zaplaconoKarta") && bool.Parse(query["zaplaconoKarta"]);
            bool oplataZaPotwierdzenie = query.ContainsKey("oplataZaPotwierdzenie") && bool.Parse(query["oplataZaPotwierdzenie"]);

            bool zatwierdzonoWszystkie = false;

            /* wszystkie pliki z folderu oczekujacych */
            var wszystkieOczekujace = Directory.EnumerateFiles(SciezkaDoOczekujacychRachunkow, "*.json");

            /* oczekujace wykluczajac te, ktore udalo sie juz dodac do listy wlasnia BP */
            var oczekujace = wszystkieOczekujace.Where(a => !File.Exists(a.Replace(SciezkaDoOczekujacychRachunkow, SciezkaDoZatwierdzonychRachunkow))).ToList<string>();

            try
            {
                /* numer rachunku z listy */
                int NumerRachunku = 1;

                /* sprwadza czy sesja kasowa jest otwarta */
                if (!WplataDoKasy.PrzygotujKase()) return JsonConvert.SerializeObject(new Dictionary<string, object>() { { "ok", false }, { "err", "Brak otwartej sesji kasowej" } });

                /* Datan rachunku (Zawarta w nazwie) */
                WplataDoKasy.ZdefiniujDate();

                /* ilość zatwierdzonych przelewów z listy */
                int UdanePrzelewy = 0;

                /* jesli jest jakis na liscie oczekujacych */
                if (oczekujace.Count > 0)
                {

                    /* nawiazanie połączenia z BP - logowanie i wyslanie rachunkow */
                    var BPConnect = new BluePay(oczekujace.ToArray<string>(), query.ContainsKey("timeOut") ? Int32.Parse(query["timeOut"]) : 0);

                    /* Blad logowania - przerwij dodawanie rachunkow */
                    if (BPConnect.bladLogowania) return JsonConvert.SerializeObject(new Dictionary<string, object>() { { "ok", false }, { "err", "Nie udało się zalogować do BluePay" } });

                    /* currentFile - aktualny rachunek, wybierany po kolei z listy */
                    foreach (string currentFile in oczekujace)
                    {
                        /* Pomijaj rachunek, jeśli wykazal bledy */
                        if (!BluePay.StatusyRachunkow.ContainsKey(currentFile) || BluePay.StatusyRachunkow[currentFile].typ != 2) continue;

                        UdanePrzelewy++;

                        /* odczytaj zawartosc pliku tego rachunku */
                        string fileName = currentFile.Substring(SciezkaDoOczekujacychRachunkow.Length);
                        using (StreamReader sr = new StreamReader(currentFile))
                        {
                            /* Dodaj operacje kasowe */
                            var ob = JsonConvert.DeserializeObject<Dictionary<string, object>>(sr.ReadToEnd());
                            var operacje = (JArray)ob["operacje"];
                            var ListaOperacji = new List<WplataDoKasy.Operacja>();
                            foreach (var operacja in operacje) ListaOperacji.Add(new WplataDoKasy.Operacja((Double)operacja["Kwota"], (string)operacja["Tytul"]));
                            var wplata = new WplataDoKasy(ListaOperacji, NumerRachunku++, zaplaconoKarta);
                        }

                        /* kopiuj plik do zatwierdzonych */
                        File.Copy(currentFile, Path.Combine(SciezkaDoZatwierdzonychRachunkow, fileName));
                    }
                }

                if (
                    (oczekujace.Count == 0 && wszystkieOczekujace.Count() > 0) ||
                    UdanePrzelewy == oczekujace.Count ||
                    /* jesli zaden rachunek nie wykazal bledu */
                    BluePay.StatusyRachunkow.Select(a => a.Value.typ == 0).Count() == 0
                )
                {
                    /* Pousuwaj pliki z foldru oczekujacych, skoro wszystkie sie zatwierdzily */
                    zatwierdzonoWszystkie = WyczyscListeOczekujacych();

                    /* NOWOSC
                     * jesli jest zaznaczona oplata za wydruk potwierdzenia...
                     * to po zatwierdzeniu wszystkich rachunkow poprawnie dodawana jest jeszcze jedna operacja kasowa
                     */
                    if (oplataZaPotwierdzenie)
                    {
                        new WplataDoKasy(new List<WplataDoKasy.Operacja>() {
                            new WplataDoKasy.Operacja(0.5, "Potwierdzenie transakcji")
                        }, -1, zaplaconoKarta);
                    }
                }

            }
            catch (Exception e)
            {

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Wystąpił problem podczas dodawania operacji kasowych.");
                Console.ResetColor();

                Console.WriteLine(e.ToString());

                if (e.Message == "TIMEOUT")
                {
                    /*foreach (string currentFile in wszystkieOczekujace)
                    {
                        string fileName = currentFile.Substring(SciezkaDoOczekujacychRachunkow.Length);
                        Directory.Move(currentFile, Path.Combine(SciezkaDoOdlozonychRachunkow, fileName));
                    }*/
                    //WyczyscListeZatwierdzonych();
                    OdlozWykonanie(zaplaconoKarta, oplataZaPotwierdzenie);
                }

                return JsonConvert.SerializeObject(new Dictionary<string, object>() { { "ok", false }, { "err", "Bład Krytyczny" }, { "errInfo", e.Message } });
            }

            return JsonConvert.SerializeObject(new Dictionary<string, object>() { { "ok", true }, { "all", zatwierdzonoWszystkie } });
        }

        public static string OdlozWykonanie (bool zaplaconoKarta, bool oplataZaPotwierdzenie)
        {
            int NumerRachunku = 1;
            string unikalnyKlucz = Guid.NewGuid().ToString();//  + "_" + (new DateTime()).Millisecond;

            /* sprwadza czy sesja kasowa jest otwarta */
            if (!WplataDoKasy.PrzygotujKase()) return JsonConvert.SerializeObject(new Dictionary<string, object>() { { "ok", false }, { "err", "Brak otwartej sesji kasowej" } });

            /* Datan rachunku (Zawarta w nazwie) */
            WplataDoKasy.ZdefiniujDate();

            //bool zaplaconoKarta = query.ContainsKey("zaplaconoKarta") && bool.Parse(query["zaplaconoKarta"]);
            //bool oplataZaPotwierdzenie = query.ContainsKey("oplataZaPotwierdzenie") && bool.Parse(query["oplataZaPotwierdzenie"]);

            var wszystkieOczekujace = Directory.EnumerateFiles(SciezkaDoOczekujacychRachunkow, "*.json");
            foreach (string currentFile in wszystkieOczekujace)
            {

                /* Dopisz w danych rachunku informacje o typie płatnosci itp */
                string text = File.ReadAllText(currentFile);
                var fileJson = JsonConvert.DeserializeObject<Dictionary<string, object>>(text);

                /* Dodaj operacje kasowe */
                var operacje = (JArray)fileJson["operacje"];
                var ListaOperacji = new List<WplataDoKasy.Operacja>();
                foreach (var operacja in operacje) ListaOperacji.Add(new WplataDoKasy.Operacja((Double)operacja["Kwota"], string.Format("{0} - odłożony", operacja["Tytul"])));
                var wplata = new WplataDoKasy(ListaOperacji, NumerRachunku++, zaplaconoKarta);

                var dodatkoweDane = new Dictionary<string, object>();
                dodatkoweDane.Add("zaplaconoKarta", zaplaconoKarta);
                dodatkoweDane.Add("oplataZaPotwierdzenie", oplataZaPotwierdzenie);
                dodatkoweDane.Add("unikalnyKluczGrupy", unikalnyKlucz);
                if (!fileJson.ContainsKey("dodatkoweDane")) fileJson.Add("dodatkoweDane", dodatkoweDane);
                else fileJson["dodatkoweDane"] = dodatkoweDane;

                string new_text = JsonConvert.SerializeObject(fileJson);
                File.WriteAllText(currentFile, new_text);

                /* Przenieś do katalogu odłożonych */
                string fileName = currentFile.Substring(SciezkaDoOczekujacychRachunkow.Length);
                fileName = fileName.Substring(0, fileName.Length - ".json".Length);
                fileName = string.Format("{0}_${1}.json", fileName, unikalnyKlucz);
                fileName = Path.Combine(SciezkaDoOdlozonychRachunkow, fileName);
                Directory.Move(currentFile, fileName);
            }

            if (oplataZaPotwierdzenie) new WplataDoKasy(new List<WplataDoKasy.Operacja>() {
                        new WplataDoKasy.Operacja(0.5, "Potwierdzenie transakcji")
                    }, -1, zaplaconoKarta);

            return JsonConvert.SerializeObject(new Dictionary<string, object>() { { "ok", true } });
        }

    }
}
