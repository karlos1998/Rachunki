using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using InsERT.Moria.Sfera;
using InsERT.Mox.DatabaseAccess;
using System.Data.Common;

namespace ConsoleApp1
{
    class SQL
    {

        public static DbConnection connection;

        public static void Connect ()
        {
            var dbcFactory = Program.sfera.PodajObiektTypu<IDbConnectionFactory>();
            connection = dbcFactory.CreateConnection(DbConnectionFlags.NoPooling | DbConnectionFlags.NoEnlist);

            connection.Open();
        }

        public static List<Dictionary<string, object>> prepare (string queryString)
        {

            var tmp  = new List<Dictionary<string, object>>();

            try
            {
                using (DbCommand command = connection.CreateCommand())
                {
                    command.CommandText = queryString;

                    tmp = SerializeMysqlData(command.ExecuteReader());

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Wystąpił problem z SQL:\n{0}", ex.Message);
            }

            return tmp;

        }

        public static List<Dictionary<string, object>> SerializeMysqlData(DbDataReader reader)
        {
            var results = new List<Dictionary<string, object>>();
            var cols = new List<string>();
            for (var i = 0; i < reader.FieldCount; i++)
            {
                //Console.WriteLine("INDEX: " + reader.GetName(i));
                cols.Add(reader.GetName(i));
            }

            while (reader.Read())
            {
                /*
                var r = reader;
                Console.WriteLine("READ: " + r["NazwaSkrocona"]);
                var result = new Dictionary<string, object>();

                var cols_tmp = new List<string>(cols);

                for (int i = 0; i < cols_tmp.Count; ++i)
                {
                    Console.WriteLine("INDEX: " + cols_tmp[i]);
                    result.Add(cols_tmp[i], r[cols_tmp[i]]);
                }

                results.Add(result);*/

                results.Add(SerializeRow(cols, reader));

            }

            reader.Close();

            return results;
        }
        
        public static Dictionary<string, object> SerializeRow(List<string> cols, DbDataReader reader)
        {
            var result = new Dictionary<string, object>();
            for (int i = 0; i < cols.Count; ++i)
                result.Add(cols[i], reader[cols[i]]);
            return result;
        }
        


    }
}
