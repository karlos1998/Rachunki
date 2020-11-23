using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConsoleApp1
{
    class Prowizje
    {
        public Double KwotaRachunku;

        public  Double ConvertToDouble(string s)    => Double.Parse(s.Replace(".", ","));
        private Double PobierzCene(string Key)      => ConvertToDouble(Program.config.GetValue("Prowizje", Key));

        public double RoundDown(double number)      => Math.Floor(number * 100) / 100;

        public bool express;
        public string RodzajRachunku;


        public Double ObliczProwizjeAgenta ()
        {

            if (express) return PobierzCene("prowizjaAgentaExpress");

            if (KwotaRachunku <= PobierzCene("kwotaKNF"))
            {
                if (RodzajRachunku == "ZWYKLY")                         return PobierzCene("prowizjaAgentaZwyklyPonizejKwotyKNF");
                if (RodzajRachunku == "ZUS" || RodzajRachunku == "US")  return PobierzCene("prowizjaAgentaZUsUSPonizejKwotyKNF");
            }
            else
            {
                if (RodzajRachunku == "ZWYKLY")                         return PobierzCene("prowizjaAgentaZwyklyPowyzejKwotyKNF");
                if (RodzajRachunku == "ZUS" || RodzajRachunku == "US")  return PobierzCene("prowizjaAgentaZUsUSPowyzejKwotyKNF");
            }

            return 0;
            
        }

        public Double OplataKnf;
        public Double ObliczOplateKnf ()
        {
            if (KwotaRachunku <= PobierzCene("kwotaKNF")) return OplataKnf = 0;

            return OplataKnf = RoundDown(KwotaRachunku * PobierzCene("procentProwizjiKNF"));
        }

        public Double ObliczProwizjeKlienta() => Math.Round(ObliczProwizjeKlienta (true) * 100) / 100;
        public Double ObliczProwizjeKlienta (bool c)
        {

            if (express) return Math.Floor( ( (KwotaRachunku * PobierzCene("procentProwizjiExpress")) + PobierzCene("prowizjaKlientaExpress") ) * 100 )  / 100;

            ObliczOplateKnf();

            switch (RodzajRachunku)
            {
                case "ZWYKLY" : return OplataKnf + PobierzCene("prowizjaKlientaZwykly");
                case "ZUS"    :
                case "US"     : return OplataKnf + PobierzCene("prowizjaKlientaZusUs");
            }

            return 0;

        }
    }
}
