namespace csharp_web_scraper
{
    using HtmlAgilityPack;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Reflection;

    class Program
    {
        static void Main(string[] args)
        {
            string path="";
            if (args.Length == 0) {
                Console.WriteLine("provide an absolute path to save the scraped data, otherwise the default path will be used\n\n");
            }
            else {
                Console.WriteLine($"data will be saved in {args[0]}");
                path = args[0];
            }

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
                Console.WriteLine("initiating a series of POST request for every currency...");

                foreach (var currency in currencies)
                {
                    string scrapedData = HttpPost(fromDate, toDate, currency, url, "1");



                    Regex regex = new Regex("var m_nRecordCount = [0-9]+;");
                    Match match = regex.Match(scrapedData);
                    if (match.Success)
                    {
                        bool isHeaderInitialized = false;
                        int page = 1;
                        int currentEntry = 20;
                        int entriesCount = Int32.Parse(Regex.Match(match.ToString(), @"\d+").Value);
                        Console.WriteLine($"There is {entriesCount} on the topic of {currency}");

                        scrapedData = FilterData(scrapedData, isHeaderInitialized);
                        isHeaderInitialized = true;

                        while (currentEntry < entriesCount)
                        {
                            currentEntry += 20;
                            page += 1;                             
                            scrapedData += FilterData(HttpPost(fromDate, toDate, currency, url, page.ToString()), isHeaderInitialized);
                        }
                    }
                    else
                    {
                        Console.WriteLine("no data for this currency currently (probably a server issue), switching to the next currency...");
                    }
                    if (args.Length == 0)
                        SaveToFile($"{currency}.csv", scrapedData);
                    else
                        SaveToFile(path + $"/{currency}.csv", scrapedData);

                }
            }
        }

        static string HttpGet(string url)
        {
            Console.WriteLine("initiating the GET request...");
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            HttpWebResponse response = null;

            try { response = (HttpWebResponse)request.GetResponse(); }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.WriteLine("Press any key to quit:");
                Console.ReadKey();
                Environment.Exit(0);
            }
            Console.WriteLine($"response status code : {response.StatusCode}");
            if ((response.StatusCode == HttpStatusCode.OK))
            {
                return ReadHTML(response);
            }
            else
                return $"Error, response finished with status code {response.StatusCode}";
        }

        static string HttpPost(DateTime from, DateTime to, string currency, string url, string page)
        {
            Console.WriteLine($"Requesting {currency} data from {from.ToString("yyyy-MM-dd")} to {to.ToString("yyyy-MM-dd")} page {page}");

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            string postData = $"erectDate={from.ToString("yyyy-MM-dd")}&nothing={to.ToString("yyyy-MM-dd")}&pjname={currency}&page={page}";
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);
            request.ContentLength = byteArray.Length;

            Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();
            HttpWebResponse response = null ;

            try { response = (HttpWebResponse)request.GetResponse(); }
            catch(Exception e)
            {
                Console.WriteLine(e);
                Console.WriteLine("Press Y if you want to try and continue scraping (next page), or anything else if you want to quit:");
                if(Console.ReadKey().Key == ConsoleKey.Y)
                    return "";
                else if (Console.ReadKey(true).Key != ConsoleKey.Y)
                    Environment.Exit(0);
            }
            Console.WriteLine($"response status code : {response.StatusCode}");

            return ReadHTML(response);
        }

        static string ReadHTML(HttpWebResponse response)
        {

            Stream htmlStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(htmlStream);
            Console.WriteLine("waiting to parse page contents...");
            string responseHTML = reader.ReadToEnd();

            response.Close();
           
            return responseHTML;
        }

        static List<string> LoadCurrencies(string html)
        {
            List<string> currencies = new List<string>();

            //can use regex for this but it is way more tiresome
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

        static string FilterData(string html, bool isHeaderInitialized)
        {

            string filteredData = "";
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);
            if (document.DocumentNode.SelectSingleNode("//table[2]/tr[2]") ==null)
            {
                Console.WriteLine("THE PAGE FOR THIS CURRENCY WAS EMPTY, DUE TO A SERVER ERROR!");
                return filteredData;
            }
            Console.WriteLine("Filtering useful content from current page...");

            if (!isHeaderInitialized)
            {
                int colCounter = 0;
                HtmlNode node = document.DocumentNode.SelectSingleNode("//table[2]/tr[1]");
                node = node.FirstChild;
                //skip text node
                node = node.NextSibling;
                while (node != null)
                {
                    if (colCounter < 6)
                    {
                        filteredData += node.InnerText + ',';
                        Console.Write(node.InnerText + ',');
                        colCounter++;
                    }
                    else
                    {
                        filteredData += node.InnerText + '\n';
                        Console.Write(node.InnerText + '\n');
                        colCounter = 0;
                    }

                    node = node.NextSibling;
                    node = node.NextSibling;
                }
                colCounter = 0;
                node = document.DocumentNode.SelectSingleNode("//table[2]/tr[2]");
                while (node != null)
                {
                    HtmlNode child = node.FirstChild;
                    //skip text nodes
                    child = child.NextSibling;
                    while (child != null)
                    {
                        if (colCounter < 6)
                        {
                            filteredData += child.InnerText + ',';
                            Console.Write(child.InnerText + ',');
                            colCounter++;
                        }
                        else
                        {
                            filteredData += child.InnerText + '\n';
                            Console.Write(child.InnerText + '\n');
                            colCounter = 0;
                        }
                        child = child.NextSibling;
                        child = child.NextSibling;
                    }

                    node = node.NextSibling;
                    //skip text nodes
                    node = node.NextSibling;
                }

            }
            else
            {

                HtmlNode node = document.DocumentNode.SelectSingleNode("//table[2]/tr[2]");
                int colCounter = 0;
                while (node != null)
                {
                    HtmlNode child = node.FirstChild;
                    //skip text nodes
                    child = child.NextSibling;
                    while (child != null)
                    {
                        if (colCounter < 6)
                        {
                            filteredData += child.InnerText + ',';
                            Console.Write(child.InnerText + ',');
                            colCounter++;
                        }
                        else
                        {
                            filteredData += child.InnerText + '\n';
                            Console.Write(child.InnerText + '\n');
                            colCounter = 0;
                        }
                        child = child.NextSibling;
                        child = child.NextSibling;
                    }

                    node = node.NextSibling;
                    //skip text nodes
                    node = node.NextSibling;
                }
            }
            return filteredData;
        }

        static void SaveToFile(string path,string data)
        {
            try
            {
                FileStream fs = File.Create(path);
                {
                    byte[] info = new UTF8Encoding(true).GetBytes(data);
                    // Add some information to the file.
                    fs.Write(info, 0, info.Length);
                }

                fs.Close();
            }

            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
