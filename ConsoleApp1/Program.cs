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
    class Program
    {


        public static Uchwyt sfera;

        public static IniFile config;

        public static string ProjectPath;
        public static string SciezkaDoOczekujacychRachunkow;
        public static string SciezkaDoZatwierdzonychRachunkow;
        public static string SciezkaDoAnulowaniaRachunkow;
        public static string SciezkaDoOdlozonychRachunkow;
        public static string SciezkaDoListyDoladowan;

        static Dictionary<int, UproszczonyPodmiot> ListaPodmiotow;


        static bool IsUserAdministrator()
        {
            bool isAdmin;
            try
            {
                WindowsIdentity user = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(user);
                isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (UnauthorizedAccessException ex)
            {
                isAdmin = false;
            }
            catch (Exception ex)
            {
                isAdmin = false;
            }
            return isAdmin;
        }


        public static Dictionary<int, UproszczonyPodmiot> PobierzListePodmiotow ()
        {
            return ListaPodmiotow;
        }

        static List<Dictionary<string, object>> RachunkiBankowe;

        public static Uchwyt UruchomSfere()
        {

            

            Console.Write("[UPDATE 03.11.2021 9:00 (37.1.0)] Uruchamianie Sfery ({0})", config.GetValue("Sfera", "desktop"));
            DanePolaczenia danePolaczenia;

            if (config.GetValue("Sfera", "autentykacjaWindowsLogin") != null && config.GetValue("Sfera", "autentykacjaWindowsLogin") != "" && config.GetValue("Sfera", "autentykacjaWindowsHaslo") != null && config.GetValue("Sfera", "autentykacjaWindowsLogin") != "")
                 danePolaczenia = DanePolaczenia.Jawne(config.GetValue("Sfera", "desktop"), config.GetValue("Sfera", "baza"), false, config.GetValue("Sfera", "autentykacjaWindowsLogin"), config.GetValue("Sfera", "autentykacjaWindowsHaslo"));
            else
                 danePolaczenia = DanePolaczenia.Jawne(config.GetValue("Sfera", "desktop"), config.GetValue("Sfera", "baza"), true);

            MenedzerPolaczen mp = new MenedzerPolaczen();
            Uchwyt sfera = mp.Polacz(danePolaczenia, ProductId.Subiekt);
            sfera.ZalogujOperatora(config.GetValue("Operator", "nazwa"), config.GetValue("Operator", "haslo"));

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(" - Uruchomiona");
            Console.ResetColor();

            Console.WriteLine("Wybrano bazę ({0})", config.GetValue("Sfera", "baza"));

            return sfera;
        }

        static public string MatchKey(string input, string regexstr)
        {
            Regex _regex = new Regex(regexstr);
            Match match = _regex.Match(input);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            else
            {
                return "";
            }
        }

        static void Main(string[] args)
        {
            Console.CursorVisible = false;
            if (Environment.OSVersion.ToString().Contains("Windows")) DisableConsoleQuickEdit.Go();

            Console.WriteLine("Uprawnienia administratora: {0}", IsUserAdministrator() ? "Tak" : "Brak");

            try
            {
                Console.WriteLine("Rozpoczynam pracę programu");

                ProjectPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/Rachunki/";

                SciezkaDoOczekujacychRachunkow   = ProjectPath + "DANE/rachunki/oczekujace/";
                SciezkaDoZatwierdzonychRachunkow = ProjectPath + "DANE/rachunki/zatwierdzone/";
                SciezkaDoAnulowaniaRachunkow     = ProjectPath + "DANE/rachunki/wycofane/";
                SciezkaDoOdlozonychRachunkow     = ProjectPath + "DANE/rachunki/odlozone/";
                SciezkaDoListyDoladowan          = ProjectPath + "CONFIG/doladowania";

                string configFile = ProjectPath + "/CONFIG/config.ini";
                if (!File.Exists(configFile))
                {
                    Console.WriteLine("Praca przerwana - brak pliku konfiguracyjnego:");
                    Console.WriteLine(configFile);
                    while (true) ;
                }

                config = new IniFile(configFile);

                Doladowanie.ZdefiniujObiektOperatorow();
                
                try
                {
                    sfera = UruchomSfere();
                } catch (Exception e)
                {
                    Console.WriteLine(" - Nie udało się zalogować");

                    Console.WriteLine("");
                    Console.WriteLine(e.Message);

                    while (true) ;
                }



                /*InsERT.Moria.Rozrachunki.IRozrachunki mgrrr = sfera.PodajObiektTypu<InsERT.Moria.Rozrachunki.IRozrachunki>();
                var encje = mgrrr.Dane.Wszystkie();

                foreach(var encja in encje)
                {
                    Console.WriteLine(encja.Tytul);
                };

                while (true) ;*/

                //InsERT.Moria.Bank.IRachunkiBankowe m = sfera.PodajObiektTypu<InsERT.Moria.Bank.IRachunkiBankowe>();
                //var encje = m.Dane.Wszystkie();
                //foreach(var encja in encje) if(encja.Wlasciciel.JestMojaFirma()) Console.WriteLine("{0} ----> {1}", encja.Nazwa, encja.Numer);
                //while (true) ;


                /*
                IRodzajeOperacjiKasowych rodzaje = Program.sfera.PodajObiektTypu<IRodzajeOperacjiKasowych>();
                var encje = rodzaje.Dane.Wszystkie();
                foreach(var a in encje)
                {
                    Console.Write("Rodzaj opracji kasowej: ");
                    Console.WriteLine(a.Nazwa);
                }
                while (true) ;
                */


                Console.WriteLine("Sprwadzam formy płatności: ");
                IFormyPlatnosci mgr = sfera.PodajObiektTypu<IFormyPlatnosci>();
                foreach(var a in mgr.Dane.Wszystkie())
                {
                    Console.WriteLine("--> ({0}) {1}  -  {2}", a.TypPlatnosci.Id, a.TypPlatnosci.Nazwa, a.Nazwa);
                }
                //return;


                Console.WriteLine("Rodzaje operacji kasowych: ");
                IRodzajeOperacjiKasowych mgrr = sfera.PodajObiektTypu<InsERT.Moria.Kasa.IRodzajeOperacjiKasowych>();
                foreach(var rodzajOperacji in mgrr.Dane.Wszystkie())
                {
                    Console.WriteLine("-> {0} - {1}", rodzajOperacji.Nazwa, rodzajOperacji.Id);
                }




                PrzyjmijAsortyment.ZdefiniujIDKasyFiskalnej();

                Console.WriteLine("Wiadomość powitalna: {0}", config.GetValue("Server", "powitanie"));


                

                SQL.Connect();

                Console.WriteLine("Spisuję rachunki bankowe (US) ...");
                RachunkiBankowe = SQL.prepare("SELECT ModelDanychContainer.RachunkiBankoweUS.TypPodatku, ModelDanychContainer.RachunkiBankoweUS.Numer, ModelDanychContainer.RachunkiBankoweUS.Bank, ModelDanychContainer.UrzedySkarbowe.*, ModelDanychContainer.SymboleDeklaracjiDyspozycjiPodatkowej.SymbolFormularza FROM ModelDanychContainer.RachunkiBankoweUS INNER JOIN ModelDanychContainer.UrzedySkarbowe ON ModelDanychContainer.RachunkiBankoweUS.UrzadSkarbowy_Id = ModelDanychContainer.UrzedySkarbowe.Id INNER JOIN ModelDanychContainer.SymboleDeklaracjiDyspozycjiPodatkowej ON ModelDanychContainer.RachunkiBankoweUS.TypPodatku = ModelDanychContainer.SymboleDeklaracjiDyspozycjiPodatkowej.TypPodatku");

                Console.WriteLine("Zaczekaj jeszcze chwilę. Pobieram listę podmiotów...");
                AktualizujListePodmiotow();
                Console.WriteLine("Lista podmiotów pobrana");

                WebServer ws = new WebServer(SendResponse, config.GetValue("Server", "link"));
                //WebServer ws = new WebServer(SendResponse, "http://+:80/");
                //WebServer ws = new WebServer(SendResponse, new string[] { "http://localhost:9696/rachuneczki/" /*, "http://25.30.156.248:9696/r/"*/ });
                ws.Run();
                Console.WriteLine("Server wystartował na: {0}", config.GetValue("Server", "link"));




                Console.WriteLine("-----------------------------------------------------------------------------------------------------------------------");
                Console.WriteLine("                          gp                                        ,,                                    ");
                Console.WriteLine("`7MMF'                mm  \\/              .g8\"\"\"bgd               `7MM              `7MMF' mm           OO");
                Console.WriteLine("  MM                  MM  `'            .dP'     `M                 MM                MM   MM           88");
                Console.WriteLine("  MM         .gP\"Ya mmMMmm, pP\"Ybd     dM'       ` ,pW\"Wq.    , M\"\"bMM.gP\"Ya        MM mmMMmm           ||");
                Console.WriteLine("  MM        ,M'   Yb  MM    8I   `\"     MM         6W'   `Wb ,AP    MM ,M'   Yb       MM   MM           || ");
                Console.WriteLine("  MM      , 8M\"\"\"\"\"\"  MM    `YMMMa.     MM.        8M     M8 8MI    MM 8M\"\"\"\"\"\"       MM   MM           `'");
                Console.WriteLine("  MM     ,M YM.    ,  MM    L.   I8     `Mb.     ,'YA.   ,A9 `Mb    MM YM.    ,       MM   MM           ,,");
                Console.WriteLine(".JMMmmmmMMM  `Mbmmd'  `Mbmo M9mmmP'       `\"bmmmd'  `Ybmd9'   `Wbmd\"MML.`Mbmmd'     .JMML. `Mbmo        db");

                Console.WriteLine("-----------------------------------------------------------------------------------------------------------------------");


                var wszystkieOczekujace = Directory.EnumerateFiles(SciezkaDoOczekujacychRachunkow, "*.json");
                foreach (var rachunek in wszystkieOczekujace)
                    if (!BluePay.StatusyRachunkow.ContainsKey(rachunek))
                        BluePay.StatusyRachunkow.Add(rachunek, new BluePay.Status(File.Exists(rachunek.Replace(SciezkaDoOczekujacychRachunkow, SciezkaDoZatwierdzonychRachunkow)) ? 2 : 0));

                

            }
            catch (Exception e)
            {
                Console.WriteLine("BŁAD!");
                Console.WriteLine(e.Message);
                Console.WriteLine("Szczegóły:");
                Console.WriteLine(e.ToString());
            }
            finally
            {
                while (true) Thread.Sleep(1000);
            }

     
        }

        public struct UproszczonyPodmiot
        {
            public int Id;
            public string NazwaSkrocona;
            public string NIP;
            public string Telefon;

            public struct Adres
            {
                public string Ulica;
                public string Miejscowosc;
                public string NrDomu;
                public string NrLokalu;
                public string KodPocztowy;
                
                public Adres (AdresPodmiotu a)
                {
                    if (a == null)
                    {
                        Ulica = Miejscowosc = NrDomu = NrLokalu = KodPocztowy = "";
                        return;
                    }

                    var dane    = a.Szczegoly;
                    Ulica       = dane.Ulica;
                    Miejscowosc = dane.Miejscowosc;
                    NrDomu      = dane.NrDomu;
                    NrLokalu    = dane.NrLokalu;
                    KodPocztowy = dane.KodPocztowy;
                }
                
            }

            public Adres adres;

            public string REGON;

            public string PESEL;
            public string Imie;
            public string Nazwisko;

            public bool Firma;

            public List<string> notatki;

            public bool CechaRachunki;

            public UproszczonyPodmiot (Podmiot p)
            {
                Id = p.Id;
                NazwaSkrocona = p.NazwaSkrocona;
                NIP = p.NIP;
                Telefon = p.Telefon;

                adres = new Adres(p.AdresPodstawowy);
                
                REGON = p.Firma != null ? p.Firma.REGON : null;

                if (p.Osoba != null)
                {
                    PESEL = p.Osoba.PESEL;
                    Imie = p.Osoba.Imie;
                    Nazwisko = p.Osoba.Nazwisko;
                }
                else PESEL = Imie = Nazwisko = "";

                Firma = p.Firma != null;

                var Odbiorcy = new List<string>();
                foreach (var notka in p.Notatki)
                {
                    Odbiorcy.Add(notka.Tresc);
                }
                notatki = Odbiorcy;

                CechaRachunki = p.Cechy.Where(a => a.Nazwa == Rachunek.NazwaCechy).FirstOrDefault() != null;

            }
        }
        

        public static void AktualizujPodmiotWLiscie (Podmiot p, bool nowy = false)
        {

            //var ob = KreatorPodmiotu(p);

            //var podm = KreatorPodmiotu(p);

            var podm = new UproszczonyPodmiot(p);

            Console.WriteLine("Edycja podmiotu: " + (nowy ? "nowy":"stary"));
            if (ListaPodmiotow.ContainsKey(p.Id))
            {
                Console.WriteLine("Aktualizowanie podmiotu ({0}) '{1}'", p.Id, p.NazwaSkrocona);

                ListaPodmiotow[p.Id] = podm;
            }
            else
            {
                //ListaPodmiotow.Add(ob);
                Console.WriteLine("Dodawwanie podmiotu ({0}) '{1}'", p.Id, p.NazwaSkrocona);
                ListaPodmiotow.Add(p.Id, podm);
            }

        }
        public static void AktualizujListePodmiotow ()
        {
            IPodmioty podmioty = sfera.PodajObiektTypu<IPodmioty>();
            IEnumerable<Podmiot> WszystkiePodmioty = podmioty.Dane.Wszystkie();

            ListaPodmiotow = new Dictionary<int, UproszczonyPodmiot>();

            var all_count = WszystkiePodmioty.Count();

            foreach (var p in WszystkiePodmioty)
            {
                var ob = new UproszczonyPodmiot(p);
                ListaPodmiotow.Add(p.Id, ob);
                Console.Write("[{0}/{1}] - {2}%", ListaPodmiotow.Count, all_count, Math.Round((decimal)(((decimal)ListaPodmiotow.Count / all_count) * 100)));
                Console.SetCursorPosition(0, Console.CursorTop);
            }
            Console.WriteLine();
        }

        public static string KonwertujDoUTF8 (string val)
        {
            byte[] bytes = Encoding.Default.GetBytes(val);
            return Encoding.UTF8.GetString(bytes);
        }

        static string PokazListeOczekujacych ()
        {
            string[] fileArray = Directory.GetFiles(SciezkaDoOczekujacychRachunkow, "*.json");

            //Console.WriteLine("Ścieżka oczekujących '{0}' zawiera '{1}' rachunków", SciezkaDoOczekujacychRachunkow, fileArray.Length);

            var res = new List<Dictionary<string, object>>();
            
            foreach (string file in fileArray)
            {
                using (StreamReader sr = new StreamReader(file))
                {
                    String line = sr.ReadToEnd();
                    //Console.WriteLine(line);
                    var ob = JsonConvert.DeserializeObject<Dictionary<string, Object>>(line);
                    ob.Add("file", file);

                    res.Add(ob);
                }
            }

            return JsonConvert.SerializeObject(res);
        }

        public struct DanyPrzedzialInfo
        {
            public int ilosc;
            public decimal kwota;
        }


        static string PokazListeZatwierdzonych()
        {
            string[] fileArray = Directory.GetFiles(SciezkaDoZatwierdzonychRachunkow, "*.json");

            Array.Sort(fileArray);
            Array.Reverse(fileArray);

            var day = DateTime.Now.ToString("dd.MM.yyyy");
            Console.WriteLine("[" + fileArray.Length + "] Wyświetlanie rachunków z " + day);

            var res = new List<Dictionary<string, object>>();
            foreach (string file in fileArray)
            {
                using (StreamReader sr = new StreamReader(file))
                {
                    String line = sr.ReadToEnd();
                    var ob = JsonConvert.DeserializeObject<Dictionary<string, object>>(line);

                    var query = (JObject)ob["query"];

                    var date = ob["plikUtworzono"].ToString();
                    string date_day = date.Split(' ')[0];

                    var k = (string)query["kwota"];
                    Decimal kwota = 0;

                    Decimal.TryParse(k, out kwota);

                    Console.Write(file + " --> " + date_day + " - ");

                    if (date_day == day)
                    {
                        Console.WriteLine("OK");
                        res.Add(ob);
                    }
                    else
                    {
                        Console.WriteLine("NOPE");
                        break;
                    }

                }
            }

            return JsonConvert.SerializeObject(res);
        }

        /*
        static string PokazListeZatwierdzonych()
        {
            string[] fileArray = Directory.GetFiles(SciezkaDoZatwierdzonychRachunkow, "*.json");

            var res = new Dictionary<string, DanyPrzedzialInfo>();
            foreach (string file in fileArray)
            {
                using (StreamReader sr = new StreamReader(file))
                {
                    String line = sr.ReadToEnd();
                    var ob = JsonConvert.DeserializeObject<Dictionary<string, object>>(line);

                    var query = (JObject)ob["query"];

                    var date = ob["plikUtworzono"].ToString();
                    string date_index = date.Split(' ')[0];

                    var k = (string) query["kwota"];
                    Decimal kwota = 0;

                    Decimal.TryParse(k, out kwota);


                    if (res.ContainsKey(date_index))
                    {
                        var i = res[date_index];
                        i.ilosc += 1;
                        i.kwota += kwota;

                        res[date_index] = i;
                    }
                    else
                    {
                        res.Add(date_index, new DanyPrzedzialInfo { ilosc = 1, kwota = kwota});
                    }

                }
            }

            return JsonConvert.SerializeObject(res);
        }*/

        static string PokazListeOdlozonych()
        {
            string[] fileArray = Directory.GetFiles(SciezkaDoOdlozonychRachunkow, "*.json");

            var res = new List<Dictionary<string, object>>();
            foreach (string file in fileArray)
            {
                using (StreamReader sr = new StreamReader(file))
                {
                    String line = sr.ReadToEnd();
                    var ob = JsonConvert.DeserializeObject<Dictionary<string, Object>>(line);
                    ob.Add("file", file);
                    res.Add(ob);
                }
            }

            return JsonConvert.SerializeObject(res);
        }

        public static bool WyczyscListeOczekujacych ()
        {
            Console.WriteLine("Czyszczenie folderu oczekujących");
            DirectoryInfo di = new DirectoryInfo(SciezkaDoOczekujacychRachunkow);
            foreach (FileInfo file in di.EnumerateFiles())
            {
                file.Delete();
            }
            return true;
        }
        public static bool WyczyscListeOdlozonych()
        {
            Console.WriteLine("Czyszczenie folderu odlozonych");
            DirectoryInfo di = new DirectoryInfo(SciezkaDoOdlozonychRachunkow);
            foreach (FileInfo file in di.EnumerateFiles())
            {
                file.Delete();
            }
            return true;
        }


        public static bool WyczyscListeZatwierdzonych()
        {
            Console.WriteLine("Czyszczenie folderu zatwierdzonych");
            DirectoryInfo di = new DirectoryInfo(SciezkaDoZatwierdzonychRachunkow);
            foreach (FileInfo file in di.EnumerateFiles())
            {
                file.Delete();
            }
            return true;
        }

        static string SzukajRachunku(string prefix)
        {
            Console.WriteLine("Szukana fraza: '{0}'", prefix);

            string[] fileArray = Directory.GetFiles(SciezkaDoZatwierdzonychRachunkow, "*.json");

            string[] keys = new string[] { "nazwa_nadawcy", "tytul", "ulica_odbiorcy", "telefon_nadawcy" };

            Array.Sort(fileArray);
            Array.Reverse(fileArray);

            var res = new List<Dictionary<string, object>>();
            foreach (string file in fileArray)
            {
                using (StreamReader sr = new StreamReader(file))
                {
                    String line = sr.ReadToEnd();
                    var ob = JsonConvert.DeserializeObject<Dictionary<string, object>>(line);

                    var query = (JObject)ob["query"];

                    //Console.Write(file + " ...");

                    bool Filter() {
                        foreach (var key in keys) 
                        {
                            try
                            {
                                if (((string)query[key]).ToLower().Contains(prefix.ToLower()))
                                {
                                    Console.WriteLine("{0} -> {1} ?= {2} ::: {3}", key, query[key], prefix, ((string)query[key]).Contains(prefix));
                                    return true;
                                }
                            } catch (Exception e)
                            {
                                //Console.WriteLine(e);
                                return false;
                            }
                        }
                        return false; 
                    };

                    if ( Filter() )
                    {
                        Console.Write("OK");
                        res.Add(ob);
                    }

                    //Console.WriteLine();
                }
            }

            return JsonConvert.SerializeObject(res);
        }

        public static string SendResponse(HttpListenerRequest request)
        {
            
            try
            {
                Dictionary<string, string> query = request.QueryString.AllKeys.ToDictionary(k => k, k => request.QueryString[k]);

                Dictionary<string, string> query_tmp = new Dictionary<string, string>(query);

                foreach (KeyValuePair<string, string> q in query_tmp)
                {
                    query[q.Key] = KonwertujDoUTF8(q.Value).Trim();

                    //Console.WriteLine("{0}: {1} --> {2}", q.Key, q.Value, val);

                }

                if (query.ContainsKey("saveUS"))
                {
                    /* Przyjmuje rachunek z formularza - zapisuje rachunek w pliku, a dane odbiorcy i nadawcy w subiekcie */

                    Console.WriteLine("-----------------------------------------------------------");

                    Console.WriteLine("Przyjmowanie rachunku...");

                    var nowyRachunek = new Rachunek(query);

                    Console.WriteLine("-----------------------------------------------------------");

                    return JsonConvert.SerializeObject(new Dictionary<string, object>() { { "query", query }, { "result", nowyRachunek.result } });
                }
                else if (query.ContainsKey("ProwizjeTest"))
                {
                    var prowizje = new Prowizje();
                    prowizje.express = query.ContainsKey("express");
                    prowizje.KwotaRachunku = Convert.ToDouble(query["ProwizjeTest"]);
                    prowizje.RodzajRachunku = query["rodzaj"];
                    var agenta = prowizje.ObliczProwizjeAgenta();
                    var klienta = prowizje.ObliczProwizjeKlienta();
                    Console.WriteLine(agenta);
                    Console.WriteLine(klienta);
                    return JsonConvert.SerializeObject(new Dictionary<string, object>() {
                        { "kwota", prowizje.KwotaRachunku },
                        { "rodzaj", prowizje.RodzajRachunku },
                        { "agent", agenta },
                        { "klienta", klienta },
                        { "BP_round", Math.Floor( ( klienta * 100 - agenta * 100 ) ) / 100 },
                        { "BP_round_2", Math.Floor( ( klienta * 100 - agenta * 100 ) ) },
                        { "BP_round_3", Math.Round( ( klienta * 100 - agenta * 100 ) ) / 100 },
                        { "BP_round_4", Math.Ceiling( ( klienta * 100 - agenta * 100 ) ) / 100 },
                        { "BP_round_5", Math.Ceiling( Math.Round( klienta * 100 - agenta * 100, 2 ) ) / 100 },
                        { "BP", ( klienta - agenta ) }
                        //{ "BP", ( klienta - agenta ) }
                    });
                }
                else if (query.ContainsKey("pokazOczekujace"))
                {
                    /* Wyświetla listę rachunków aktualnego klienta */
                    return PokazListeOczekujacych();
                }
                else if (query.ContainsKey("pokazOdlozone"))
                {
                    /* Wyświetla listę odłożonych rachunków */
                    /** Do listy dodwane są rachunki, które nie zostały wysłane do BP przez problemy techniczne lub na naszą prośbę */
                    return PokazListeOdlozonych();
                }
                else if (query.ContainsKey("pokazZatwierdzone"))
                {
                    return PokazListeZatwierdzonych();
                }
                else if (query.ContainsKey("pokazStatusyBP"))
                {
                    /* Pokazuje statusy wysylanych rachunkow (ajax w js sprawdza je co sekundę )  */
                    return JsonConvert.SerializeObject(BluePay.StatusyRachunkow);
                }
                else if (query.ContainsKey("anulujRachunek"))
                {

                    /* Anuluje rachunek o podanej sciezce ( przerzuca go z oczekujacych do wycofanych ) */

                    string currentFile = query["anulujRachunek"];
                    Console.WriteLine("Anulowanie rachunku: " + currentFile);
                    string fileName = currentFile.Substring(SciezkaDoOczekujacychRachunkow.Length);
                    try
                    {
                        Directory.Move(currentFile, Path.Combine(SciezkaDoAnulowaniaRachunkow, fileName));
                        return JsonConvert.SerializeObject(new Dictionary<string, object>() { { "ok", true } });
                    }
                    catch (Exception e)
                    {
                        return JsonConvert.SerializeObject(new Dictionary<string, object>() { { "ok", false }, { "err", "Bład" }, { "alert", e.Message } });
                    }

                }
                else if (query.ContainsKey("zatwierdzPrzelewy"))
                {
                    return Zatwierdzanie.ZatwierdzOczekujace(query);
                }
                else if(query.ContainsKey("zatwierdzOdlozonePrzelewy"))
                {
                    return Zatwierdzanie.ZatwierdzOdlozone();
                }
                else if(query.ContainsKey("PrzywrocOdlozone"))
                {
                    /*
                    var wszystkieOdlozone = Directory.EnumerateFiles(SciezkaDoOdlozonychRachunkow, "*.json");
                    foreach (string currentFile in wszystkieOdlozone)
                    {
                        string fileName = currentFile.Substring(SciezkaDoOdlozonychRachunkow.Length);
                        Directory.Move(currentFile, Path.Combine(SciezkaDoOczekujacychRachunkow, fileName));
                    }
                    */
                    return "funkcja niekatywna";
                }
                else if (query.ContainsKey("OdlozWykonanie"))
                {
                    bool zaplaconoKarta = query.ContainsKey("zaplaconoKarta") && bool.Parse(query["zaplaconoKarta"]);
                    bool oplataZaPotwierdzenie = query.ContainsKey("oplataZaPotwierdzenie") && bool.Parse(query["oplataZaPotwierdzenie"]);
                    return Zatwierdzanie.OdlozWykonanie(zaplaconoKarta, oplataZaPotwierdzenie);
                }
                else if (query.ContainsKey("AktualizujListePodmiotow"))
                {
                    AktualizujListePodmiotow();
                }
                else if (query.ContainsKey("PobierzListeOperatorow"))
                {
                    return JsonConvert.SerializeObject(Doladowanie.PobierzOperatorow());
                }
                else if (query.ContainsKey("PrzyjmijDoladowanie"))
                {
                    if (!query.ContainsKey("operator") || !query.ContainsKey("kwota")) return JsonConvert.SerializeObject(new Dictionary<string, object>() { { "ok", false }, { "err", "Wypełnij wszystkie pola" } });
                    var doladowanie = new Doladowanie(query["operator"], Int32.Parse(query["kwota"]), (query.ContainsKey("drukujParagon") && query["drukujParagon"] == "ON"));
                    return JsonConvert.SerializeObject(doladowanie.result);
                }
                else if (query.ContainsKey("utworzDyspozycjeBankowa"))
                {
                    //var dysp = new Dyspozycja();
                    return JsonConvert.SerializeObject(Dyspozycja.WykonajRachunki());
                }
                else if (query.ContainsKey("PrzyjmijPaczke"))
                {
                    var p = new Paczka(query["symbol"], (query.ContainsKey("drukujParagon") && query["drukujParagon"] == "ON"));
                    return JsonConvert.SerializeObject(p.result);
                }
                else if (query.ContainsKey("PobierzListePaczek"))
                {
                    return JsonConvert.SerializeObject(new Dictionary<string, object>() { { "paczki", Paczka.PobierzListePaczek() } });
                }
                else if (query.ContainsKey("PobierzTylkoPodmioty"))
                {
                    return JsonConvert.SerializeObject(new Dictionary<string, object>() { { "podmioty", ListaPodmiotow.Values.Where(a => a.CechaRachunki) } });
                }
                else if(query.ContainsKey("UsunNadawceLubOdbiorce"))
                {
                    int id_nadawcy = Int32.Parse(query["id_nadawcy"]);

                    IPodmioty podmioty = sfera.PodajObiektTypu<IPodmioty>();

                    var znalezionyNadawca = podmioty.Dane.Wszystkie().Where(p => p.Id == id_nadawcy).FirstOrDefault();

                    if (znalezionyNadawca == null) return JsonConvert.SerializeObject(new Dictionary<string, object>() { { "ok", false }, { "err", "Brak takiego nadawcy" } });

                    var PodmiotNadawca = podmioty.Znajdz(znalezionyNadawca);

                    if (query["type"] == "nadawca")
                    {
                        if(!PodmiotNadawca.Usun())
                        {
                            PodmiotNadawca.WypiszBledy();
                            return JsonConvert.SerializeObject(new Dictionary<string, object>() { { "ok", false }, { "err", "Nie udało się usunąć nadawcy" } });
                        }
                        ListaPodmiotow.Remove(id_nadawcy);
                    }
                    else if(query["type"] == "odbiorca")
                    {
                        var odbiorca = PodmiotNadawca.Dane.Notatki.Where(a => a.Tresc == query["dane_odbiorcy"]).FirstOrDefault();
                        if(odbiorca == null) return JsonConvert.SerializeObject(new Dictionary<string, object>() { { "ok", false }, { "err", "Nie znaleziono takiego odbiorcy u tego nadawcy" } });

                        PodmiotNadawca.Dane.Notatki.Remove(odbiorca);

                        if (!PodmiotNadawca.Zapisz())
                        {
                            PodmiotNadawca.WypiszBledy();
                            return JsonConvert.SerializeObject(new Dictionary<string, object>() { { "ok", false }, { "err", "Nie udało się usunąć odbiorcy" } });
                        }

                        AktualizujPodmiotWLiscie(PodmiotNadawca.Dane);
                    }
                    else return JsonConvert.SerializeObject(new Dictionary<string, object>() { { "ok", false }, { "err", "Niepoprawny typ" } });

                    return JsonConvert.SerializeObject(new Dictionary<string, object>() { { "ok", true } });
                }
                else if (query.ContainsKey("FiltrujRachunki"))
                {
                    Console.WriteLine(query["FiltrujRachunki"]);
                    return SzukajRachunku(Encoding.UTF8.GetString(Convert.FromBase64String(query["FiltrujRachunki"])));
                }
                else if (query.ContainsKey("utworzDyspozycjePoczta"))
                {
                    Console.WriteLine("Tworzenie dyspozycji pocztowej");
                    return JsonConvert.SerializeObject(Dyspozycja.WykonajPoczta( Decimal.Parse(query["utworzDyspozycjePoczta"]) ));
                }
                else if (query.ContainsKey("kwotaDyspozycjiPocztowej"))
                {
                    IStanowiskaKasowe stanowiskaKasowe = Program.sfera.PodajObiektTypu<IStanowiskaKasowe>();
                    var ZnajdzStanowisko = stanowiskaKasowe.Dane.Wszystkie().Where(s => s.Symbol == "KP2").FirstOrDefault();

                    //var operacje = sfera.PodajObiektTypu<IOperacjeKasowe>().Dane.Wszystkie().Where(a => a.Stanowisko.Symbol == ZnajdzStanowisko.Symbol && a.Data == DateTime.Today && a.Wplyw);

                    var operacje = sfera.PodajObiektTypu<IOperacjeKasowe>().Dane.Wszystkie().Where(a => a.Stanowisko.Symbol == ZnajdzStanowisko.Symbol);
                    //decimal sum = 0;
                    //foreach(var op in operacje) sum += op.Kwota;
                    decimal sum = operacje.Select(a => (a.Wplyw ? 1 : -1) * a.Kwota).ToArray().Sum();
                    return JsonConvert.SerializeObject(new Dictionary<string, object>() { { "kwota", sum } });
                }
                else
                {

                    //Console.WriteLine("Pobieram listę nadawcow i odbiorcow (dla podpowiedzi w wypelnianiu formularza");
                    var json = new Dictionary<string, object>(){
                        //{ "podmioty", SQL.prepare(SQLSelectPodmiotyString) },
                        //{ "podmiotyNotatki", SQL.prepare(SQLSelectNotatki) },
                        { "rachunkiBankowe", RachunkiBankowe },
                        { "query", query },
                        { "podmioty",  ListaPodmiotow }
                    };

                    Console.WriteLine("Kończę pobieranie");

                    return JsonConvert.SerializeObject(json);
                }

                return JsonConvert.SerializeObject(new Dictionary<string, object>() { { "defaultResult", true } });

                //sfera.PodajObiektTypu<IPodmioty>().UtworzOsobe().Dane.Firma.Nazwa

                //return users.Count.ToString();
            }
            catch (Exception Ex)
            {
                Console.WriteLine("Error: {0}", Ex);
                return JsonConvert.SerializeObject(new Dictionary<string, object>() { { "ok", false }, { "err", "Wykryto niezdefiniowany błąd" }, { "errInfo", Ex } });
            }
        }

        

    }
}
