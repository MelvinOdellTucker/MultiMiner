﻿using MultiMiner.Xgminer.Api.Parsers;
using MultiMiner.Xgminer.Api.Data;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace MultiMiner.Xgminer.Api
{
    public class ApiContext
    {
        //events
        // delegate declaration 
        public delegate void LogEventHandler(object sender, LogEventArgs ea);

        // event declaration 
        public event LogEventHandler LogEvent;

        private readonly int port;
        private readonly string ipAddress;

        public ApiContext(int port, string ipAddress = "127.0.0.1")
        {
            this.port = port;
            this.ipAddress = ipAddress;
        }

        public List<DeviceInformation> GetDeviceInformation(int logInterval)
        {
            string textResponse = GetResponse(ApiVerb.Devs);
            List<DeviceInformation> result = new List<DeviceInformation>();
            DeviceInformationParser.ParseTextForDeviceInformation(textResponse, result, logInterval);
            return result;
        }

        public List<DeviceDetails> GetDeviceDetails()
        {
            string textResponse = GetResponse(ApiVerb.DevDetails);
            List<DeviceDetails> result = new List<DeviceDetails>();
            DeviceDetailsParser.ParseTextForDeviceDetails(textResponse, result);
            return result;
        }

        public SummaryInformation GetSummaryInformation()
        {
            string textResponse = GetResponse(ApiVerb.Summary);
            SummaryInformation result = new SummaryInformation();
            SummaryInformationParser.ParseTextForSummaryInformation(textResponse, result);
            return result;
        }

        public List<PoolInformation> GetPoolInformation()
        {
            string textResponse = GetResponse(ApiVerb.Pools);
            List<PoolInformation> result = new List<PoolInformation>();
            PoolInformationParser.ParseTextForDeviceDetails(textResponse, result);
            return result;
        }

        public VersionInformation GetVersionInformation()
        {
            string textResponse = GetResponse(ApiVerb.Version);
            VersionInformation result = new VersionInformation();
            VersionInformationParser.ParseTextForVersionInformation(textResponse, result);
            return result;
        }

        public string QuitMining()
        {
            return GetResponse(ApiVerb.Quit);
        }

        public string RestartMining()
        {
            return GetResponse(ApiVerb.Restart);
        }

        public string SwitchPool(int poolIndex)
        {
            return GetResponse(String.Format("{0}|{1}", ApiVerb.SwitchPool, poolIndex));
        }

        public string GetResponse(string apiVerb, int timeoutMs = 500)
        {
            TcpClient tcpClient = new TcpClient(this.ipAddress, port);
            NetworkStream tcpStream = tcpClient.GetStream();

            Byte[] request = Encoding.ASCII.GetBytes(apiVerb);

            long timeoutTicks = timeoutMs * TimeSpan.TicksPerMillisecond;
            long ticks = DateTime.Now.Ticks;

            tcpStream.Write(request, 0, request.Length);

            Byte[] responseBuffer = new Byte[4096];
            string response = string.Empty;
            do
            {
                int bytesRead = tcpStream.Read(responseBuffer, 0, responseBuffer.Length);
                response = response + Encoding.ASCII.GetString(responseBuffer, 0, bytesRead);
            } while (
                //check timeout
                ((DateTime.Now.Ticks - ticks) <= timeoutTicks) &&
                //looking for a terminating NULL character from the RPC API
                (String.IsNullOrEmpty(response) ||
                                     (response[response.Length - 1] != '\0')));
            
            if (LogEvent != null)
            {
                LogEventArgs args = new LogEventArgs();

                args.DateTime = DateTime.Now;
                args.Request = apiVerb;
                args.Response = response;

                LogEvent(this, args);
            }

            tcpClient.Close();

            return response;
        } 
    }
}
