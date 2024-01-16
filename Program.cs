using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace WebCrawler;

class Program
{
    static async Task Main(string[] args)
    {
        Console.Write("Enter the number of top words to return (default is 10): ");
        string input = Console.ReadLine();
        int numberOfWords = string.IsNullOrWhiteSpace(input) ? 10 : int.Parse(input);

        Console.Write("Enter words to exclude (separated by commas): ");
        input = Console.ReadLine();
        var wordsToExclude = input.Split(',')
            .Select(w => w.Trim().ToLower())
            .ToHashSet();

        using var client = new HttpClient();

        try
        {
            // Load webpage as string.
            var response = await client.GetAsync("https://en.wikipedia.org/wiki/Microsoft");
            var pageContent = await response.Content.ReadAsStringAsync();

            // We only need the History section. Will be using HtmlAgilityPack to help us.
            var historyContent = ExtractHistoryContent(pageContent);
            var wordDict = CountWords(historyContent, wordsToExclude);

            var topWords = wordDict
                .OrderByDescending(pair => pair.Value)
                .Take(numberOfWords);

            // Header
            Console.WriteLine("Word\t\t# of Occurrences\n");
            foreach (var pair in topWords)
            {
                Console.WriteLine($"{pair.Key,-15}\t{pair.Value}");
            }

        }
        catch (HttpRequestException e)
        {
            Console.WriteLine("\nException Caught!");
            Console.WriteLine("Message :{0} ", e.Message);
        }
    }

    static List<string> ExtractHistoryContent(string htmlContent)
    {
        var historyText = new List<string>();
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(htmlContent);

        // In the wiki, it seems that the history section is sandwiched between the h2 headers
        // "History" and "Corporate affairs", and no other identifiable classes can be used.
        // So we use the node structure that HtmlAgilityPack offers to extract the content between these two
        // h2 header nodes.
        var historyHeader = htmlDoc.DocumentNode.SelectSingleNode("//h2[contains(., 'History')]");
        var corporateAffairsHeader = htmlDoc.DocumentNode.SelectSingleNode("//h2[contains(., 'Corporate affairs')]");

        if (historyHeader == null || corporateAffairsHeader == null)
        {
            return historyText;
        }

        var currentNode = historyHeader.NextSibling;

        // Loop through the nodes until we reach the h2 "Corporate affairs" header.
        while (currentNode != null && currentNode != corporateAffairsHeader)
        {
            if (!string.IsNullOrWhiteSpace(currentNode.InnerText))
            {
                historyText.Add(currentNode.InnerText);
            }

            currentNode = currentNode.NextSibling;
        }

        return historyText;
    }

    static Dictionary<string, int> CountWords(List<string> texts, HashSet<string> wordsToExclude)
    {
        // Create dictionary to map all occurences of words.
        var wordDict = new Dictionary<string, int>();

        foreach (var text in texts)
        {
            // Regex to identify all words.
            var matches = Regex.Matches(text, @"\b[a-zA-Z]+\b");

            foreach (Match match in matches)
            {
                var word = match.Value.ToLower();

                if (wordsToExclude.Contains(word))
                {
                    continue;
                }

                if (!wordDict.ContainsKey(word))
                {
                    wordDict[word] = 0;
                }

                wordDict[word]++;
            }
        }

        return wordDict;
    }
}
