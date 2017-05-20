using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Text;
using System.ServiceModel;
using System.Configuration;
using AcessoDATASUSv2.SvcAcessoDATASUS;
using System.Net;

namespace AcessoDATASUSv2
{
    public class ConsultaCADSUS
    {
        #region Singleton
        private static ConsultaCADSUS _instancia;

        public static ConsultaCADSUS Instancia
        {
            get
            {
                if (_instancia == null)
                    _instancia = new ConsultaCADSUS();
                return _instancia;
            }
        }
        #endregion

        #region Método Teste
        public string TesteAcesso()
        {
            try
            {
                using (CadsusServicePortTypeClient consultaCNS = new CadsusServicePortTypeClient("CadsusServicePort"))
                {
                    //Atribui usuário e senha de acesso ao WebService
                    consultaCNS.ClientCredentials.UserName.UserName = "CADSUS.CNS.PDQ.PUBLICO";
                    consultaCNS.ClientCredentials.UserName.Password = "kUXNmiiii#RDdlOELdoe00966";

                    //Parâmetros de Pesquisa padrão
                    requestPesquisar paramsPesquisa = new requestPesquisar();

                    FiltroPesquisa filtroPesquisa = new FiltroPesquisa();
                    filtroPesquisa.nomeCompleto = new NomeCompletoType() { Nome = "SERGIO ARAUJO CORREIA LIMA" };
                    filtroPesquisa.tipoPesquisa = TipoPesquisaType.IDENTICA;

                    paramsPesquisa.FiltroPesquisa = filtroPesquisa;
                    paramsPesquisa.CNESUsuario = new CNESUsuarioType() { CNES = "6963447", Usuario = "LEONARDO" };
                    paramsPesquisa.higienizar = false;

                    ResultadoPesquisa[] result = consultaCNS.pesquisar(paramsPesquisa);
                }
            }
            catch (System.ServiceModel.FaultException<AcessoDATASUSv2.SvcAcessoDATASUS.MSFalha> errWS)
            {
                List<string> arMsgsErro = new List<string>();
                foreach (MensagemType msgErrosWS in errWS.Detail.Mensagem)
                {
                    arMsgsErro.Add(msgErrosWS.descricao);
                }

                return "A consulta retornou a(s) seguinte(s) mensagem(ns): " + String.Join(" / ", arMsgsErro.ToArray());
            }
            catch (Exception ex)
            {
                return "ERRO -> " + ex.Message;
            }
            return "OK";
        }
        #endregion

        #region Métodos para Produção
        public DataTable ConsultaFederal(string pNome, string pNomeMae, string pNomePai, DateTime pDataNascimento, string pNumeroCNS, string pCPF, out string msgRetorno)
        {
            msgRetorno = String.Empty;
            
            /*DataTable de retorno*/
            DataTable dtRet = new DataTable();
            dtRet.Columns.Add(new DataColumn("NOME", typeof(System.String)));
            dtRet.Columns.Add(new DataColumn("NOME_MAE", typeof(System.String)));
            dtRet.Columns.Add(new DataColumn("NOME_PAI", typeof(System.String)));
            dtRet.Columns.Add(new DataColumn("DATA_NASCIMENTO", typeof(System.DateTime)));
            dtRet.Columns.Add(new DataColumn("TIPO_CARTAO", typeof(System.String)));
            dtRet.Columns.Add(new DataColumn("NUMERO_CARTAO", typeof(System.String)));
            dtRet.Columns.Add(new DataColumn("MUNICIPIO_RESIDENCIA", typeof(System.String)));
            dtRet.Columns.Add((new DataColumn("ORIGEM_DADO", typeof(System.String))));

            //Chamada ao WebService do DATASUS
            try
            {
                using (CadsusServicePortTypeClient consultaCNS = new CadsusServicePortTypeClient(ConfigurationManager.AppSettings["EndpointConsultaCNSConfig"].ToString()))
                {
                    //Atribui usuário e senha de acesso ao WebService e dados do Estabelecimento de Saúde
                    consultaCNS.ClientCredentials.UserName.UserName = ConfigurationManager.AppSettings["UserNameWebServiceDATASUS"].ToString();
                    consultaCNS.ClientCredentials.UserName.Password = ConfigurationManager.AppSettings["PasswordWebServiceDATASUS"].ToString();

                    CNESUsuarioType dadosCNES = new CNESUsuarioType() { CNES = ConfigurationManager.AppSettings["CNESWebServiceDATASUS"].ToString(), Usuario = ConfigurationManager.AppSettings["UsuarioCNESWebServiceDATASUS"].ToString() };

                    //Caso o nº do CNS seja informado, é outro tipo de consulta, específica (outro método do WS a ser chamado). A abaixo é a consulta por outros dados
                    if (String.IsNullOrEmpty(pNumeroCNS))
                    {
                        //Inicializa filtro de pesquisa
                        FiltroPesquisa filtroPesquisa = new FiltroPesquisa();
                        filtroPesquisa.tipoPesquisa = TipoPesquisaType.APROXIMADA;

                        if (!String.IsNullOrEmpty(pNome))
                            filtroPesquisa.nomeCompleto = new NomeCompletoType() { Nome = pNome };
                        if (!String.IsNullOrEmpty(pNomeMae))
                            filtroPesquisa.Mae = new NomeCompletoType() { Nome = pNomeMae };
                        if (!String.IsNullOrEmpty(pNomePai))
                            filtroPesquisa.Pai = new NomeCompletoType() { Nome = pNomePai };
                        if (!pDataNascimento.Equals(DateTime.MinValue))
                            filtroPesquisa.dataNascimento = pDataNascimento;
                        if (!String.IsNullOrEmpty(pCPF))
                            filtroPesquisa.CPF = new CPFType() { numeroCPF = pCPF };

                        //Prepara objeto para fazer a requisição ao WebService
                        requestPesquisar paramsPesquisa = new requestPesquisar();
                        paramsPesquisa.FiltroPesquisa = filtroPesquisa;
                        paramsPesquisa.CNESUsuario = dadosCNES;
                        paramsPesquisa.higienizar = false;

                        //Chamada ao WebService
                        ResultadoPesquisa[] resultWS = consultaCNS.pesquisar(paramsPesquisa);

                        //Transporta resultados para o DataTable de retorno. Se a pesquisa não retornou registros, é disparada uma exceção que é tratada no primeiro "catch".
                        foreach (ResultadoPesquisa result in resultWS)
                        {
                            string munResidencia = "NÃO DISPONÍVEL";
                            if (result.MunicipioResidencia != null)
                                munResidencia = String.Format("{0} - {1}", result.MunicipioResidencia.codigoMunicipio, result.MunicipioResidencia.nomeMunicipio);
                            
                            dtRet.Rows.Add(
                                result.NomeCompleto.Nome,
                                result.Mae.Nome,
                                result.Pai.Nome,
                                result.dataNascimento,
                                (result.CNS.tipoCartao.Equals(TipoCNSType.D) ? "DEFINITIVO" : "PROVISORIO"),
                                result.CNS.numeroCNS,
                                munResidencia,
                                "BASE FEDERAL");
                        }

                    }
                    else //Consulta específica por número de Cartão SUS.
                    {
                        //Filtro de Pesquisa
                        CNSType filtroConsulta = new CNSType();
                        filtroConsulta.numeroCNS = pNumeroCNS;

                        //Prepara objeto para requisição ao WebService
                        requestConsultar paramsConsulta = new requestConsultar();
                        paramsConsulta.CNESUsuario = dadosCNES;
                        paramsConsulta.CNS = filtroConsulta;

                        responseConsultar resultWS = consultaCNS.consultar(paramsConsulta);

                        foreach (CNSType cartao in resultWS.UsuarioSUS.Cartoes)
                        {
                            //Coloca registro no DataTable de retorno: para cada nº do cartão do SUS encontrado.
                            dtRet.Rows.Add(
                                resultWS.UsuarioSUS.NomeCompleto.Nome,
                                (resultWS.UsuarioSUS.Mae != null) ? resultWS.UsuarioSUS.Mae.Nome : "NÃO DISPONÍVEL",
                                (resultWS.UsuarioSUS.Pai != null) ? resultWS.UsuarioSUS.Pai.Nome : "NÃO DISPONIVEL",
                                resultWS.UsuarioSUS.dataNascimento,
                                cartao.tipoCartao.Equals(TipoCNSType.D) ? "DEFINITIVO" : "PROVISÓRIO",
                                cartao.numeroCNS,
                                (resultWS.UsuarioSUS.Enderecos.Endereco.Municipio != null) ? String.Format("{0} - {1}", resultWS.UsuarioSUS.Enderecos.Endereco.Municipio.codigoMunicipio, resultWS.UsuarioSUS.Enderecos.Endereco.Municipio.nomeMunicipio) : "NÃO DISPONÍVEL",
                                "BASE FEDERAL"
                                );
                        }
                    }
                }
            }
            catch (System.ServiceModel.FaultException<AcessoDATASUSv2.SvcAcessoDATASUS.MSFalha> errWS) //Erros enviados pelo WebService
            {
                List<string> arMsgsErro = new List<string>();
                foreach (MensagemType msgErrosWS in errWS.Detail.Mensagem)
                {
                    arMsgsErro.Add(msgErrosWS.descricao);
                }

                msgRetorno = "A consulta retornou a(s) seguinte(s) mensagem(ns): " + String.Join(" / ", arMsgsErro.ToArray());
                dtRet = null;
            }
            catch (Exception ex) //Erro geral da aplicação
            {
                msgRetorno = "[ERRO] " +  ex.Message;
                dtRet = null;
            }

            return dtRet;
        }
        #endregion
    }
}
