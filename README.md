# csharp-web-scraper

## The way it works
Using the HttpWebRequest class and GET method, we get the list of current available currencies on the webpage.
Using HtmlAgilityPack we navigate the dom to extract those currencies. Then for each available currency we run a POST method, check if there's a result (a table) and if there is, we check if there's more available pages, if there are, we scrape those aswell (as we're doing that we filter the response for the content we need). Once we're done with a single currency, we write the data either into a default directory, or a directory of our choosing provided with an absolute path as a command line argument.

## Things to pay attention to
The website that is being scraped works extremely poorly, very often it returns status 502, so most probably you won't be able to scrape every page of every currency in one go.