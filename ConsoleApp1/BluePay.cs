
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Threading;

namespace ConsoleApp1
{
    class BluePay
    {
        CookieContainer Cookies = new CookieContainer();
        Uri target = new Uri("https://is2.bluepay.pl/");

        string idSessji = "";

        string[] Lista_Rachunkow;

        string OstatniTypRachunku = "";

        JObject DaneRachunku;

        int iloscZatwierdzonychRachunkow = 0;

        string GetVal(string arg)
        {
            var r = (string) DaneRachunku.GetValue(arg);
            if (r == null) return "";
            return r;
        }


        public struct Status
        {
            public int typ;
            public List<string> bledy;

            public Status(int t, List<string> b)
            {
                typ = t;
                bledy = b;
            }

            public Status(int t)
            {
                typ = t;
                bledy = new List<string>();
            }

        }


        public static Dictionary<string, Status> StatusyRachunkow = new Dictionary<string, Status>();

        List<string> bledyAktualnegoRachunku;

        public bool bladLogowania = true;

        int timeout;

        public BluePay(string[] lr, int tout = 0)
        {

            timeout = tout > 0 ? tout : (1000 * 15);

            Lista_Rachunkow = lr;

            Console.WriteLine("Łączenie z BluePay...");

            /*
            using (StreamReader sr = new StreamReader(Program.ProjectPath + "sesja.txt"))
            {
                idSessji = sr.ReadLine();
            }

            if (idSessji.Length > 1)
            {
                Console.WriteLine("Przypisywanie id wcześniejszej sesji: " + idSessji);
                Cookies.Add(new Cookie("JSESSIONID", idSessji) { Domain = target.Host });
            }
            */

            string a = file_get_contents("https://is2.bluepay.pl/pulpit", "");
            a = file_get_contents("https://is2.bluepay.pl/j_security_check", string.Format("j_username={0}&j_password={1}", Program.config.GetValue("BluePay", "login"), Program.config.GetValue("BluePay", "haslo")));


            /*
            bool odnowionoLogowanie;
            if (odnowionoLogowanie = a.Contains("Wyloguj ["))
            {
                Console.WriteLine("Udało się zalogować po wcześniejszym ID");
            }
            else
            {
                Console.WriteLine("Trwa ponowne logownie");
                Cookies = new CookieContainer();
                a = file_get_contents("https://is2.bluepay.pl/j_security_check", string.Format("j_username={0}&j_password={1}", Program.config.GetValue("BluePay", "login"), Program.config.GetValue("BluePay", "haslo")));
            }
            */


            if (bladLogowania = !a.Contains("Wyloguj [")) throw new Exception("WRONG_PASS");

            /* Wchodzi w zakładkę "Rachunki" */
            RezultatOstatniegoZapytania = file_get_contents("https://is2.bluepay.pl/rachunki/oplac_rachunki", "");
            /* Zapisuje do zmiennej ukryty klucz potrzebny do dalszych requestów (zapisywany w kazdym zapytaniu) */
            ZdefiniujViewState();

            PrzetworzRachunki();

            using (StreamWriter sw = File.CreateText(Program.ProjectPath + "test.txt"))
            {
                sw.Write(RezultatOstatniegoZapytania);
            }
            using (StreamWriter sw = File.CreateText(Program.ProjectPath + "sesja.txt"))
            {
                sw.Write(idSessji);
            }

        }
        public string file_get_contents(string url, string postData, int tOut = 0)
        {

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            request.Timeout = tOut > 0 ? (tOut * 1000) : timeout;

            //var data = Encoding.ASCII.GetBytes(postData);
            //var data = Encoding.Default.GetBytes(postData);
            var data = Encoding.UTF8.GetBytes(postData);

            request.Method = "POST";
            //request.ContentType = "application/x-www-form-urlencoded";
            request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            request.ContentLength = data.Length;
            request.CookieContainer = Cookies;
            //request.Headers.Add("Content-Type: application/octet-stream; charset=UTF-8");
            //request.CookieContainer = new CookieContainer("");

            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            //HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                //Console.WriteLine("Znalezione Nagłowki: " + response.Headers.Count);
                foreach (var header in response.Headers.AllKeys)
                {
                    //Console.WriteLine(response.Headers[header]);
                    if (header.ToString() == "Set-Cookie")
                    {
                        var cookie_string = response.Headers[header];
                        var c = cookie_string.Split('=');
                        string name = c[0];
                        string val = c[1].Split(';')[0];
                        //Console.WriteLine("Dodano cookie '{0}' -> '{1}'", name, val);
                        if (name == "JSESSIONID") idSessji = val;
                        Cookies.Add(new Cookie(name, val) { Domain = target.Host });
                    }
                }

                string responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

                return responseString;
            }
            catch (WebException ex)
            {
                HttpWebResponse errorResponse = ex.Response as HttpWebResponse;

                if (errorResponse == null) throw new Exception("TIMEOUT");

                string responseContent = "";

                using (StreamReader r = new StreamReader(errorResponse.GetResponseStream()))
                {
                    responseContent = r.ReadToEnd();
                }

                Console.WriteLine("The server at {0} returned {1}", errorResponse.ResponseUri, errorResponse.StatusCode);

                

                //Console.WriteLine(responseContent);
            }
            return "ERROR";
        }

        void Kreskowy (bool przekieruj)
        {
            if (przekieruj) WyslijZapytanie(new Dictionary<string, string>() {
                {"javax.faces.partial.ajax", "true"},
                {"javax.faces.source", "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView"},
                {"javax.faces.partial.execute", "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView"},
                {"javax.faces.partial.render", "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:barCodeEditWarnMessage+newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:barCodePaymentButtons+newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:manualEditWarnMessage+newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:manualPaymentButtons+newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:customEditWarnMessage+newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:customPaymentButtons+newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:zusEditWarnMessage+newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:zusPaymentButtons+newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usEditWarnMessage+newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPaymentButtons"},
                {"javax.faces.behavior.event", "tabChange"},
                {"javax.faces.partial.event", "tabChange"},
                {"newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView_newTab", "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:barCodePaymentTab"},
                {"newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView_tabindex", "0"},
                {"newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:barCodePayment_barCode", ""},
                {"newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:customPayment_title", ""},
                {"newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:customPayment_amount", ""},
                {"newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:custom_parties:beneficjentRachunek", ""},
                {"newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:custom_parties:beneficjentNazwa_input", ""},
                {"newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:custom_parties:beneficjentNazwa_hinput", ""},
                {"newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:custom_parties:beneficjentMiejscowosc_input", ""},
                {"newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:custom_parties:beneficjentUlica_input", ""},
                {"newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:custom_parties:beneficjentKodPocztowy", ""},
                {"newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:custom_parties:podmiotNazwa_input", ""},
                {"newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:custom_parties:podmiotNazwa_hinput", ""},
                {"newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:custom_parties:podmiotMiejscowosc_input", ""},
                {"newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:custom_parties:podmiotUlica_input", ""},
                {"newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:custom_parties:podmiotKodPocztowy", ""},
                {"newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:custom_parties:podmiotKraj", "PL"},
                {"newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_beneficjentNazwa", ""},
                {"newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_beneficjentRachunek", ""},
                {"newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_amount", ""},
                {"newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_podmiotNazwa", ""},
                {"newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_identifierType", ""},
                {"newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_formSymbol", ""},
                {"newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_obligation", ""},
                {"newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_periodType", ""},
                {"newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView_activeIndex", "0"}
            });

            WyslijZapytanie(new Dictionary<string, string>() {
                {"javax.faces.partial.ajax", "true"},
                {"javax.faces.source", "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:onBarCodePaymentButton"},
                {"javax.faces.partial.execute", "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:onBarCodePaymentButton newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:barCodePaymentPanel"},
                {"javax.faces.partial.render", "form"},
                {"newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:onBarCodePaymentButton", "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:onBarCodePaymentButton"},
                {"newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:barCodePayment_barCode", GetVal("kod_EAN")}
            });

            if (RezultatOstatniegoZapytania.Contains("wprowadź kwotę")) WyslijZapytanie(new Dictionary<string, string>() {
                {"javax.faces.partial.ajax", "true"},
                {"javax.faces.source", "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:onBarCodePaymentButton"},
                {"javax.faces.partial.execute", "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:onBarCodePaymentButton newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:barCodePaymentPanel"},
                {"javax.faces.partial.render", "form"},
                {"newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:onBarCodePaymentButton", "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:onBarCodePaymentButton"},
                {"newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:barCodePayment_amount", GetVal("kwota")}
            });
            else ZnajdzBledy(true);
        }

        public void US (bool przekieruj)
        {
            
            if(przekieruj) WyslijZapytanie(new Dictionary<string, string>() {
                { "javax.faces.partial.ajax", "true" },
                { "javax.faces.source", "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView" },
                { "javax.faces.partial.execute", "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView" },
                { "javax.faces.partial.render", "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:barCodeEditWarnMessage newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:barCodePaymentButtons newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:manualEditWarnMessage newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:manualPaymentButtons newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:customEditWarnMessage newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:customPaymentButtons newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:zusEditWarnMessage newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:zusPaymentButtons newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usEditWarnMessage newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPaymentButtons" },
                { "javax.faces.behavior.event", "tabChange" },
                { "javax.faces.partial.event", "tabChange" },
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView_newTab", "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPaymentTab" },
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView_tabindex", "4" },
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:barCodePayment_barCode", ""},
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:manualPayment_title", ""},
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:manualPayment_amount", ""},
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:manual_parties:beneficjentRachunek", ""},
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:customPayment_title", ""},
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:customPayment_amount", ""},
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:custom_parties:beneficjentRachunek", ""},
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:custom_parties:beneficjentNazwa_input", ""},
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:custom_parties:beneficjentNazwa_hinput", ""},
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:custom_parties:beneficjentMiejscowosc_input", ""},
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:custom_parties:beneficjentUlica_input", ""},
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:custom_parties:beneficjentKodPocztowy", ""},
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:custom_parties:podmiotNazwa_input", ""},
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:custom_parties:podmiotNazwa_hinput", ""},
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:custom_parties:podmiotMiejscowosc_input", ""},
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:custom_parties:podmiotUlica_input", ""},
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:custom_parties:podmiotKodPocztowy", ""},
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:custom_parties:podmiotKraj", "PL" },
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_beneficjentNazwa", ""},
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_beneficjentRachunek", ""},
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_amount", ""},
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_podmiotNazwa", ""},
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_identifierType", ""},
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_formSymbol", ""},
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_obligation", ""},
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_periodType", ""},
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView_activeIndex", "4" }
            });

            //DaneRachunku["typ_identyfikatora"] = "N";
            //DaneRachunku["identyfikator"] = "522-262-64-70";

            /* Wyślij rachunek US 
             * (2x w pętli, bo za pierwszym razem dobiera inputy pod okres rozliczeniowy itp 
             */

            var test = new Dictionary<string, string>() {
                { "javax.faces.partial.ajax", "true" },
                { "javax.faces.source", "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:onUsPaymentButton" },
                { "javax.faces.partial.execute", "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:onUsPaymentButton newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPaymentPanel" },
                { "javax.faces.partial.render", "form" },
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:onUsPaymentButton", "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:onUsPaymentButton" },
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_beneficjentNazwa", GetVal("nazwa_us") },
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_beneficjentRachunek", GetVal("nr_rachunku_odbiorcy") },
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_amount", GetVal("kwota") },
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_podmiotNazwa", GetVal("nazwa_nadawcy") },

                /* 1 linia to typ identyfikatora, a kolejne to podstawianie pod kazdy typ identyfikatora zawrtosc, choć i tak tylko 1 jest uzywany*/
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_identifierType", GetVal("typ_identyfikatora") },
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_identifier_N", GetVal("identyfikator") }, //"522-262-64-70"
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_identifier_P", GetVal("identyfikator") }, //"522-262-64-70"
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_identifier_R", GetVal("identyfikator") }, //"522-262-64-70"
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_identifier_1", GetVal("identyfikator") }, //"522-262-64-70"
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_identifier_2", GetVal("identyfikator") }, //"522-262-64-70"
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_identifier_3", GetVal("identyfikator") }, //"522-262-64-70"

                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_formSymbol", GetVal("symbol") },
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_obligation", GetVal("identyfikacja_zobowiazania") },

                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_periodType", GetVal("typ_okresu") },
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_year", GetVal("rok_okresu").Length >= 4 ? GetVal("rok_okresu").Substring(2, 2) : "" },
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_halfYear", GetVal("polrocze_okresu") },
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_month", GetVal("miesiac_okresu") },
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_day", GetVal("dzien_okresu") },
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_tenDays", GetVal("dekada_okresu") },
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_quarter", GetVal("kwartal_okresu") }
            };



            for (int i = 0; i < 2; i++)
            {

                /* Poniższe dla typu 'dzień' - nie działą i tak... */
                /*
                Console.WriteLine("Typ OKresu: " + GetVal("typ_okresu"));
                
                if (i == 1 && GetVal("typ_okresu") == "J")
                {
                    Console.WriteLine("Wywoływanie przejścia");
                    WyslijZapytanie(new Dictionary<string, string>() {
                        { "javax.faces.partial.ajax", "true" },
                        { "javax.faces.source", "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_periodType" },
                        { "javax.faces.partial.execute", "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_periodType newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_year newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_halfYear newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_quarter newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_month newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_tenDays newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_day" },
                        { "javax.faces.partial.render", "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_periodType_message newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_year_tbody newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_halfYear_tbody newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_quarter_tbody newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_month_tbody newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_tenDays_tbody newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_day_tbody" },
                        { "javax.faces.behavior.event", "change" },
                        { "javax.faces.partial.event", "change" },
                        { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPayment_periodType", "J" },
                    });
                };
                
                */

                WyslijZapytanie(new Dictionary<string, string>(test));
            }

            //Console.WriteLine(JsonConvert.SerializeObject(test));

        }

        public void Reczny(bool przekieruj)
        {
            

            /* Przechodzi do zakładki przelewów ręcznych */
            if(przekieruj) WyslijZapytanie(new Dictionary<string, string>()
            {
                { "javax.faces.partial.ajax", "true" },
                { "javax.faces.source", "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView" },
                { "javax.faces.partial.execute", "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView" },
                { "javax.faces.partial.render", "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:barCodeEditWarnMessage newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:barCodePaymentButtons newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:manualEditWarnMessage newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:manualPaymentButtons newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:customEditWarnMessage newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:customPaymentButtons newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:zusEditWarnMessage newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:zusPaymentButtons newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usEditWarnMessage newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:usPaymentButtons" },
                { "javax.faces.behavior.event", "tabChange" },
                { "javax.faces.partial.event", "tabChange" },
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView_newTab", "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:customPaymentTab" },
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView_tabindex", "2" },
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView_activeIndex", "2" },
            });

            /* Wysyła przelew */

            string UlicaNadawcy = GetVal("ulica_nadawcy");
            if (GetVal("numer_domu_nadawcy") != "" && GetVal("numer_lokalu_nadawcy") != "")
                UlicaNadawcy += " " + GetVal("numer_domu_nadawcy") + "/" + GetVal("numer_lokalu_nadawcy");
            else UlicaNadawcy += " " + (GetVal("numer_domu_nadawcy") != "" ? GetVal("numer_domu_nadawcy") : GetVal("numer_lokalu_nadawcy"));

            var przelew = new Dictionary<string, string>()
            {
                { "javax.faces.partial.ajax", "true" },
                { "javax.faces.source", "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:onCustomPaymentButton" },
                { "javax.faces.partial.execute", "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:onCustomPaymentButton newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:customPaymentPanel" },
                { "javax.faces.partial.render", "form" },
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:onCustomPaymentButton", "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:onCustomPaymentButton" },
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:customPayment_title", GetVal("tytul") },
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:customPayment_amount", GetVal("kwota") },
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:custom_parties:beneficjentRachunek", GetVal("nr_rachunku") },
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:custom_parties:beneficjentNazwa_input", GetVal("nazwa_odbiorcy") },
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:custom_parties:beneficjentNazwa_hinput", "" },
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:custom_parties:beneficjentMiejscowosc_input", GetVal("miejscowosc_odbiorcy") },
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:custom_parties:beneficjentUlica_input", GetVal("ulica_odbiorcy") },
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:custom_parties:beneficjentKodPocztowy", GetVal("kod_pocztowy_odbiorcy") },
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:custom_parties:podmiotNazwa_input", GetVal("nazwa_nadawcy") },
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:custom_parties:podmiotNazwa_hinput", "" },
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:custom_parties:podmiotMiejscowosc_input", GetVal("miejscowosc_nadawcy") },
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:custom_parties:podmiotUlica_input", UlicaNadawcy },
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:custom_parties:podmiotKodPocztowy", GetVal("kod_pocztowy_nadawcy") },
                { "newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:custom_parties:podmiotKraj", "PL" }
            };

            if (GetVal("EXPRESS") == "ON")
            {
                przelew.Add("newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:customPayment_express_input", "on");

                WyslijZapytanie(przelew);

                przelew.Add("newPaymentCC:newPaymentWizardStep1TabView:newPaymentModeTabView:custom_parties:podmiotTelefon", GetVal("telefon_nadawcy"));
            }

            WyslijZapytanie(przelew);

            

        }

        string RezultatOstatniegoZapytania = "";
        string ViewState = "";
        string WyslijZapytanie (Dictionary<string, string> data_in, int tOut = 0)
        {

            var data = new Dictionary<string, string>(data_in);

            Thread.Sleep(900);

            if(ViewState.Length > 0) data.Add("javax.faces.ViewState", ViewState);

            string request = "";
            foreach (var arg in data)
            {
                //request += "&" + HttpUtility.UrlEncode(arg.Key) + "=" + Program.KonwertujDoUTF8( arg.Value);
                request += "&" + arg.Key + "=" + arg.Value;
            }

            RezultatOstatniegoZapytania = file_get_contents("https://is2.bluepay.pl/billpayments/billpayments-newpayment.jsf", request, tOut);

            ZdefiniujViewState();
            Zdefiniuj_j_idt();

            return RezultatOstatniegoZapytania;
        }

        void ZdefiniujViewState()
        {
            ViewState = MatchKey(RezultatOstatniegoZapytania, "([-]{0,1}[0-9]+[:][-]{0,1}[0-9]+)");
        }

        string j_idt = "844";
        void Zdefiniuj_j_idt()
        {
            string s = MatchKey(RezultatOstatniegoZapytania, "j_idt([0-9]+)_input");

            if (!string.IsNullOrEmpty(s))
            {
                j_idt = s;
                //Console.WriteLine("j_idt: " + s);
            }
        }

        void ZatwierdzPrzelewy ()
        {
            /* Klika zakładkę "Wprowadzone przelewy" */

            var obj = new Dictionary<string, string>()
            {
                { "javax.faces.partial.ajax", "true" },
                { "javax.faces.source", "newPaymentCC:newPaymentWizardStep1TabView" },
                { "javax.faces.partial.execute", "newPaymentCC:newPaymentWizardStep1TabView" },
                { "javax.faces.partial.render", "newPaymentCC:newPaymentWizardStep1TabView:newPaymentGroupExtendedView:newPaymentGroupTable" },
                { "javax.faces.behavior.event", "tabChange" },
                { "javax.faces.partial.event", "tabChange" },
                { "newPaymentCC:newPaymentWizardStep1TabView_newTab", "newPaymentCC:newPaymentWizardStep1TabView:newPaymentWizardStep1TabViewTab2" },
                { "newPaymentCC:newPaymentWizardStep1TabView_tabindex", "1" },
                //{ "newPaymentCC:newPaymentWizardStep1TabView:newPaymentGroupExtendedView:newPaymentGroupTable:0:j_idt965_input", "on" }
            };

            /*
            if(iloscZatwierdzonychRachunkow > 1)
            {
                for(int i = 1; i < iloscZatwierdzonychRachunkow; i++)
                {
                    obj.Add("newPaymentCC:newPaymentWizardStep1TabView:newPaymentGroupExtendedView:newPaymentGroupTable:" + i + ":j_idt965_input", "on");
                }
            }
            */
            int i = 0;
            foreach(var status in StatusyRachunkow)
            {
                if (status.Value.typ == 2) obj.Add(string.Format("newPaymentCC:newPaymentWizardStep1TabView:newPaymentGroupExtendedView:newPaymentGroupTable:{0}:j_idt{1}_input", i++, j_idt), "on");
            }


            var v0 = WyslijZapytanie(obj, 45);

            using (StreamWriter sw = File.CreateText(Program.ProjectPath + "v0.txt"))
            {
                sw.Write(v0);
            }

            /* Klika "Przejdź do zatwierdzania" */
            var v1 = WyslijZapytanie(new Dictionary<string, string>()
            {
                { "javax.faces.partial.ajax", "true" },
                { "javax.faces.source", "newPaymentCC:goToConfirmationButton" },
                { "javax.faces.partial.execute", "newPaymentCC:goToConfirmationButton" },
                { "javax.faces.partial.render", "form" },
                { "newPaymentCC:goToConfirmationButton", "newPaymentCC:goToConfirmationButton" },
            }, 45);

            using (StreamWriter sw = File.CreateText(Program.ProjectPath + "v1.txt"))
            {
                sw.Write(v1);
            }

            /* Zatwierdza listę przelewów */
            var v2 = WyslijZapytanie(new Dictionary<string, string>()
            {
                { "javax.faces.partial.ajax", "true" },
                { "javax.faces.source", "newPaymentCC:commitButton" },
                { "javax.faces.partial.execute", "newPaymentCC:commitButton" },
                { "javax.faces.partial.render", "form" },
                { "newPaymentCC:commitButton", "newPaymentCC:commitButton" }
            }, 60);

            using (StreamWriter sw = File.CreateText(Program.ProjectPath + "v2.txt"))
            {
                sw.Write(v2);
            }

        }

        static public string MatchKey(string input, string regexstr) => Program.MatchKey(input, regexstr);        

        //string SciezkaDoPlikowRachunkow = "";

        void PrzetworzRachunki ()
        {
            //SciezkaDoPlikowRachunkow = Program.ProjectPath + "DoBluePay/";

            foreach (var SciezkaPlikuRachunku in Lista_Rachunkow)
            {
                var NowyStatus = new Status(1);

                if (StatusyRachunkow.ContainsKey(SciezkaPlikuRachunku)) StatusyRachunkow[SciezkaPlikuRachunku] = NowyStatus;
                else StatusyRachunkow.Add(SciezkaPlikuRachunku, NowyStatus);


                if (!File.Exists(SciezkaPlikuRachunku))
                {
                    Console.WriteLine("Nie znaleziono ścieżki rachunku: " + SciezkaPlikuRachunku);
                    continue;
                }

                try
                {
                    using (StreamReader sr = File.OpenText(SciezkaPlikuRachunku))
                    {
                        string s = sr.ReadToEnd();
                        var RachunekJson = JsonConvert.DeserializeObject<Dictionary<string, object>>(s);

                        DaneRachunku = (JObject)RachunekJson["query"];

                        //Console.WriteLine(s);

                        WybierzOdpowiedniRachunek(); //Ewentualny catch może wywołać to - celowy exception w przypadku błednych rachunków

                        if (bledyAktualnegoRachunku.Count == 0) iloscZatwierdzonychRachunkow++;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Rachunek '{0}' zwrócił następujący błąd: ", SciezkaPlikuRachunku);
                    Console.WriteLine(e);
                    throw new Exception(e.Message);
                }
                finally
                {
                    StatusyRachunkow[SciezkaPlikuRachunku] = new Status(bledyAktualnegoRachunku.Count > 0 ? 0 : 2, bledyAktualnegoRachunku);
                }

            }

            /*
             * Zapisanie rachunków
             */

            ZatwierdzPrzelewy();

            /* (*) (*) (*) (*) (*) */


        }

        void WybierzOdpowiedniRachunek ()
        {
            bledyAktualnegoRachunku = new List<string>();
            if(GetVal("kreskowy") == "1")
            {
                Kreskowy(OstatniTypRachunku != "kreskowy");
                OstatniTypRachunku = "kreskowy";
            }
            else if (GetVal("przelewUS") == "US")
            {
                US(OstatniTypRachunku != "US");
                OstatniTypRachunku = "US";
            }
            else
            {
                Reczny(OstatniTypRachunku != "reczny");
                OstatniTypRachunku = "reczny";
            }
            ZnajdzBledy();
        }

        //ui-message-error-detail\">([^<]+)

        void ZnajdzBledy (bool BlednyiOstrzezenia = false)
        {
            string html = RezultatOstatniegoZapytania;
            string result;
            int i = 10;

            html = html.Replace("ui-messages-error-summary", "");


            //html = html.Replace("Jeśli nie chcesz zatwierdzać tego przelewu odznacz go w zakładce 'Wprowadzone przelewy'", "");
            //html = html.Replace("W ciągu ostatnich 24 godzin wykonano już płatność na podany rachunek z taką samą kwotą", "");

            do
            {
                result = BlednyiOstrzezenia ? MatchKey(html, "ui-message[s]{0,1}-[error|warn]+-[detail|summary]+\">([^<]{3,})") : MatchKey(html, "ui-message[s]{0,1}-error-detail\">([^<]{3,})");


                //result = MatchKey(html, "ui-message[s]{0,1}-[error-detail|warn-summary]+\">([^<]+)");

                if (result.Length == 0) break;

                Console.WriteLine("BŁĄD -> : {0}", result);

                if (result == "TUTAJ TRZEBA DODAC TRESC BLEDU DEBETU")
                    throw new Exception("DEBET");

                html = html.Replace(result, "");
                bledyAktualnegoRachunku.Add(result);
            } while (result.Length > 0 && i-- > 0);

            if (bledyAktualnegoRachunku.Count > 0)
            {
                using (StreamWriter sw = File.CreateText(Program.ProjectPath + "bledy.txt"))
                {
                    sw.Write(RezultatOstatniegoZapytania);
                }

                throw new Exception("errors_found");
            }

            

        }

    }
}