/*
╔╗
║║ACF Dynamic Kiosk Script
  Author Cesar Lara
║║Version 1.0 - 18/06/19 - OK
╚╝
*/

using System;
using QFlow.Library;
using System.Collections.Generic;

namespace QFlow.Scripting
{
    internal class ReceptionPointProfileScript
    {
        private const byte _universalTimeout = 30;
        private const byte _errorTimeout = 10;
        private const byte _thanksTimeout = 7;
        private const string _thanksPage = "CelPag5";
        private const string _errorPage = "Error";
        private const string _resetCommand = "cmdResetSession";
        private const string _enqueueCommand = "enqueue";

        //Metodo para encolar casos
        public static ScriptResults Command(ref ReceptionPointState rpState, string commandName, string commandArgument)
        {
            try
            {
                if (commandName.StartsWith(_enqueueCommand, StringComparison.CurrentCultureIgnoreCase))
                {
                    Enqueue(ref rpState, Convert.ToInt16(commandArgument), _thanksPage, _thanksTimeout);
                    return new ScriptResults();
                }
                if (commandName.StartsWith(_resetCommand, StringComparison.CurrentCultureIgnoreCase))
                {
                    rpState.ResetSession();
                    return new ScriptResults();
                }
            
                
                rpState.GotoPage(commandName, _universalTimeout);
            }
            catch (Exception ex)
            {
                EventLogEntry log = new EventLogEntry(EventLogEntryType.Error, "Reception Point " + rpState.ReceptionPointId,
                    "Kiosko1Cel error: " + ex.Message + Environment.NewLine + ex.Source + Environment.NewLine + ex.StackTrace, 0, 0);
                EventLog.Write(ref log);
                rpState.SessionParameters.Set("Error", ex.Message);
                rpState.SessionParameters.Set("Source", ex.Source);
                rpState.GotoPage(_errorPage, _errorTimeout);
            }
            return new ScriptResults();
        }

        //Input se utiliza en las páginas de keypad para ingreso de numeros

        public static ScriptResults Input(ref ReceptionPointState rpState, string data, string source)
        {
            ScriptResults rs = new ScriptResults();
            Customer oCustomer = null;

            if (rpState.CurrentPageKey == "CelPag3")
            {
                try
                {
                    oCustomer = Customer.Get(data, 0);
                    rpState.SessionParameters.Set("IDCliente", oCustomer.Id.ToString());
                    rpState.SessionParameters.Set("FirstName", oCustomer.FirstName.ToString());
                    rpState.SessionParameters.Set("LastName", oCustomer.LastName.ToString());
                    rpState.SessionParameters.Set("Texto", "SI"); //variable para servicio condicional - muestra boton
                    rpState.GotoPage("CelPag4", _universalTimeout);
                }
                catch (Exception ex)
                {
                    rpState.SessionParameters.Set("Texto","NO");
                    SaveLog("Kiosko1Cel", "Error en busqueda de cliente: " + ex.Message + " datavalue: " + data, EventLogEntryType.Error);
                }
               
               
            }
            else if (rpState.CurrentPageKey == "CelPag8")
            {
                rpState.SessionParameters.Set("Telefono", data);
                rpState.SessionParameters.Set("Saldo", DateTime.Now.ToString());
                rpState.GotoPage("CelPag9", _universalTimeout);
            }
            
            return rs;
        }
        
        //metodo que imprime los valores del ticket
        private static void Enqueue(ref ReceptionPointState rpState, short serviceId, string goToPage, short timeout)
        {
            ServiceEnqueueResults res = null;          
            List<int> cid = null;
            int customerId = 0;

            int.TryParse(rpState.SessionParameters.Get("IDCliente"), out customerId);

            res = Service.Enqueue(
                serviceId, //ServiceId
                rpState.ReceptionPointId, //ReceptionPointID
                0, //userId
                customerId, //customerId
                rpState.CurrentLanguageCode, //languageCode
                String.Empty, //subject
                String.Empty, //extRef
                String.Empty, //notes
                ref cid, //classificationIds
                true, //printTicket
                0, 0, 0, 0);

            rpState.SessionParameters.Set("QCode", res.QCode);
            rpState.SessionParameters.Set("QNumber", res.QNumber.ToString());
            rpState.GotoPage(goToPage, timeout);
        }


        static bool tracemode = true;
        /// <summary>
        /// Write a Qflow event log, use the tracemode variable to enable or disable the verbose mode
        /// </summary>
        /// <param name="source">Contains the source of the event log</param>
        /// <param name="message">Contains the message that will be stored in the vent log, this must be descriptive enough</param>
        /// <param name="type">Contains the event type, this could be: Informative, Warning o Error</param>
        private static void SaveLog(string source, string message, EventLogEntryType type)
        {
            EventLogEntry eventLogEntry;
            if (tracemode || type == EventLogEntryType.Error)
            {
                eventLogEntry = new EventLogEntry(type, source, message, 0, 0);
                QFlow.Library.EventLog.Write(ref eventLogEntry);
            }
        }
    }
}