namespace csharp_web_scraper
{
    using HtmlAgilityPack;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Text;

    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
                Console.WriteLine("provide a path to save the scraped data, otherwise the default path will be used");

            string url = "https://srh.bankofchina.com/search/whpj/searchen.jsp";
            DateTime toDate = DateTime.Now;
            DateTime fromDate = toDate.AddDays(-2);
            string html = HttpGet(url);

            if (html.Contains("Error, response finished with status code"))
            {
                Console.WriteLine(html + ". Restart the program");
            }
            else
            {
                var currencies = LoadCurrencies(html);
                string result = HttpPost(fromDate, toDate, "EUR", url);
            }
        }

         static string HttpGet(string url)
        {
            Console.WriteLine("initiating the request...");
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            var response = (HttpWebResponse)request.GetResponse();
            Console.WriteLine($"response status code : {response.StatusCode}");
            if ((response.StatusCode == HttpStatusCode.OK))
            {
                return ReadHTML(response,true);
            }
            else
                return $"Error, response finished with status code {response.StatusCode}";
        }

        static string HttpPost(DateTime from, DateTime to, string currency, string url)
        {
            Console.WriteLine($"Requesting {currency} data from {from} to {to}");
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";

            string postData = $"erectDate={from.ToString("yyyy-MM-dd")}&nothing={to.ToString("yyyy-MM-dd")}&pjname={currency}";
            Console.WriteLine(postData);
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);

            request.ContentLength = byteArray.Length;

            Stream dataStream = request.GetRequestStream();
            // Write the data to the request stream.
            dataStream.Write(byteArray, 0, byteArray.Length);
            // Close the Stream object.
            dataStream.Close();
            // Get the response.
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            // Display the status.
            Console.WriteLine(((HttpWebResponse)response).StatusDescription);

            return ReadHTML(response,false);
        }

        static string ReadHTML(HttpWebResponse response, bool isGet)
        {
            Stream htmlStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(htmlStream);
            Console.WriteLine("waiting for content...");
            string responseHTML = reader.ReadToEnd();

            response.Close();
            return responseHTML;
        }

        static List<string> LoadCurrencies(string html)
        {
            List<string> currencies = new List<string>();

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);
            Console.WriteLine("getting all currently available currencies....");
            IEnumerable<HtmlNode> nodes = document.DocumentNode.SelectNodes("//select/option");

            foreach (var node in nodes)
            {
                var currency = node.GetAttributeValue("value", "0");
                if (!currency.Equals("0"))
                {
                    Console.WriteLine(currency);
                    currencies.Add(currency);
                }

            }
            return currencies;
        }
    }
}
