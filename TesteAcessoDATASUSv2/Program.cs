using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AcessoDATASUSv2;
using System.Data;
using System.Net;

namespace TesteAcessoDATASUSv2
{
    class Program
    {
        static void Main(string[] args)
        {

            TesteConsultaFederal();
            Console.WriteLine("Pressione qualquer tecla para continuar...");
            Console.ReadKey();
        }

        static void TesteConsultaFederal()
        {
            string msgRetorno = String.Empty;

            //Nome: SERGIO ARAUJO CORREIA LIMA
            //CPF: 66105234368
            //CNS: 703404696479515
            DataTable dtRet = ConsultaCADSUS.Instancia.ConsultaFederal("", "", "", DateTime.MinValue, "", "66105234368", out msgRetorno);
            if (dtRet != null)
            {
                if (dtRet.Rows.Count > 0)
                {
                    Console.WriteLine("OK");
                    Console.WriteLine("-----------------------------");
                    foreach (DataRow r in dtRet.Rows)
                    {
                        foreach (DataColumn c in dtRet.Columns)
                        {
                            Console.WriteLine(String.Format("{0}: {1}", c.ColumnName, r[c].ToString()));
                        }
                        Console.WriteLine("-----------------------------");
                    }
                }
                else
                {
                    Console.Write("[NADA ENCONTRADO]");
                }
            }
            else
            {
                Console.Write("[ERRO] " + msgRetorno);
            }
        }
    }
}
