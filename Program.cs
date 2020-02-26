﻿using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TicketInfoSpider
{
    internal class Program
    {
        private static readonly CookieContainer CookieContainer = new CookieContainer();

        private static readonly HttpClient MainClient = new HttpClient(new SocketsHttpHandler
            {UseCookies = true, CookieContainer = CookieContainer});

        private static void Main()
        {
            Logger("Trying to open csv file...");
            var dataTable = CsvLoader.Csv2DataTable("test.csv");
            Logger("csv has been loaded into memory.");
            var successfulRequest = dataTable.Rows.Count;
            var completeNumber = 0;
            Console.WriteLine($"Rows:{successfulRequest}");
            Logger("Trying to get valid code, please wait...");
            MainClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.116 Safari/537.36");
            MainClient.DefaultRequestHeaders.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8,ja;q=0.7");
            MainClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");

            var validCodeResponse = MainClient.GetAsync(TicketInfoApi.ValidateCode).Result;
            if (SaveValidCodePng(validCodeResponse.Content.ReadAsByteArrayAsync().Result))
                Logger("Valid code has been save as ValidCode.png");
            Logger("Open ValidCode.png and enter the valid code below");
            var validCode = GetValidCode();
            var start = DateTime.UtcNow; // Start time
            foreach (DataRow row in dataTable.Rows)
            {
                completeNumber++;
                var ticketDataCollection =
                    TicketDataRequestAsync(row[0].ToString(), validCode, row[1].ToString()).Result;
                if (ticketDataCollection == null || ticketDataCollection.rtnCode == "-1")
                {
                    Logger($"FAILED : TicketId={row[0]} [{completeNumber}/{dataTable.Rows.Count}]");
                    successfulRequest -= 1;
                    continue;
                }

                foreach (var ticket in ticketDataCollection.rtnData)
                    PdfDownLoadAsync(ticket.pdfurl, $"{ticket.fpdm}_{ticket.fphm}");
                Logger($"SUCCESS: TicketId={row[0]} [{completeNumber}/{dataTable.Rows.Count}]");
            }

            var finish = DateTime.UtcNow;
            Logger("Task finished");
            Console.WriteLine(
                $"Total Rows:{dataTable.Rows.Count}\tSuccessful Request:{successfulRequest}\tSuccessful rate:{(double) successfulRequest / (double) dataTable.Rows.Count:P}\tTotal time:{finish - start}");
        }

        public static bool SaveValidCodePng(byte[] bytes)
        {
            if (bytes.Length == 0) return false;
            var fileStream = new FileStream("VaildCode.png", FileMode.Create);
            fileStream.Write(bytes);
            fileStream.Close();
            return true;
        }

        public static string GetValidCode()
        {
            var validCode = Console.ReadLine();
            while (validCode == null || validCode.Length != 5 || !validCode.All(char.IsLetterOrDigit))
            {
                Logger("You entered an illegal valid code, try again");
                validCode = Console.ReadLine();
            }

            return validCode;
        }

        public static void Logger(string s)
        {
            Console.WriteLine($"{DateTime.UtcNow}: {s}");
        }

        public static async void PdfDownLoadAsync(string url, string fileName)
        {
            try
            {
                var data = await MainClient.GetByteArrayAsync(url);
                var fileStream = new FileStream($".\\pdf\\{fileName}.pdf", FileMode.Create);
                fileStream.Write(data);
                fileStream.Close();
            }
            catch (Exception)
            {
                Logger("DownLoad Failed.");
            }
        }

        public static async Task<TicketDataCollection> TicketDataRequestAsync(string ticketId, string validCode,
            string price)
        {
            try
            {
                var responseMessage = await MainClient.SendAsync(new TicketDataRequestMessage(ticketId, validCode,
                    price));
                var jsonData = responseMessage.Content.ReadAsStringAsync().Result;
                var ticketDataCollection =
                    JsonConvert.DeserializeObject<TicketDataCollection>(jsonData);
                return ticketDataCollection;

            }
            catch (Exception)
            {
                Logger("Request Failed");
                return null;
            }
        }
    }
}