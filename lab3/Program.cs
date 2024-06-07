using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Threading;
using lab3;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


public static class TweetExtensions
{
    public static void SaveToXml(this List<Tweet> tweets, string filePath)
    {
        var xml = new XElement("Tweets",
            tweets.Select(t => new XElement("Tweet",
                new XElement("Username", t.Username),
                new XElement("Text", t.Text),
                new XElement("CreatedAt", t.CreatedAt))));

        xml.Save(filePath);
    }

    public static List<Tweet> LoadFromXml(string filePath)
    {
        var xml = XElement.Load(filePath);
        return xml.Elements("Tweet").Select(x => new Tweet
        {
            Username = x.Element("Username").Value,
            Text = x.Element("Text").Value,
            CreatedAt = x.Element("CreatedAt").Value, // DateTime.Parse(x.Element("CreatedAt").Value)
        }).ToList();
    }

    public static List<Tweet> SortTweetsByUsername(this List<Tweet> tweets)
    {
        return tweets.OrderBy(t => t.Username).ToList();
    }

    public static List<Tweet> SortTweetsByCreatedAt(this List<Tweet> tweets)
    {
        return tweets.OrderBy(t => t.CreatedAt).ToList();
    }

    public static Tweet GetNewestTweet(this List<Tweet> tweets)
    {
        return tweets.OrderByDescending(t => t.CreatedAt).FirstOrDefault();
    }

    public static Tweet GetOldestTweet(this List<Tweet> tweets)
    {
        return tweets.OrderBy(t => t.CreatedAt).FirstOrDefault();
    }

    public static Dictionary<string, List<Tweet>> CreateUsernameDictionary(this List<Tweet> tweets)
    {
        return tweets.GroupBy(t => t.Username)
                     .ToDictionary(g => g.Key, g => g.ToList());
    }

    public static Dictionary<string, int> CalculateWordFrequency(this List<Tweet> tweets)
    {
        var wordFrequency = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var tweet in tweets)
        {
            var words = tweet.Text.Split(new[] { ' ', '.', ',', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var word in words)
            {
                if (wordFrequency.ContainsKey(word))
                {
                    wordFrequency[word]++;
                }
                else
                {
                    wordFrequency[word] = 1;
                }
            }
        }
        return wordFrequency;
    }

    public static List<string> GetTop10LongestWords(this Dictionary<string, int> wordFrequency)
    {
        return wordFrequency.Where(kvp => kvp.Key.Length >= 5)
                            .OrderByDescending(kvp => kvp.Value)
                            .Take(10)
                            .Select(kvp => kvp.Key)
                            .ToList();
    }

    public static Dictionary<string, double> CalculateIDF(this List<Tweet> tweets)
    {
        var wordDocumentCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        int totalDocuments = tweets.Count;

        foreach (var tweet in tweets)
        {
            var wordsInTweet = new HashSet<string>(tweet.Text.Split(new[] { ' ', '.', ',', '!', '?' }, StringSplitOptions.RemoveEmptyEntries), StringComparer.OrdinalIgnoreCase);
            foreach (var word in wordsInTweet)
            {
                if (wordDocumentCount.ContainsKey(word))
                {
                    wordDocumentCount[word]++;
                }
                else
                {
                    wordDocumentCount[word] = 1;
                }
            }
        }

        return wordDocumentCount.ToDictionary(kvp => kvp.Key, kvp => Math.Log((double)totalDocuments / (1 + kvp.Value)));
    }

    public static List<string> GetTop10IDFWords(this Dictionary<string, double> idf)
    {
        return idf.OrderByDescending(kvp => kvp.Value)
                  .Take(10)
                  .Select(kvp => kvp.Key)
                  .ToList();
    }
}

public class Program
{
    public static List<Tweet> ReadTweets(string filePath)
    {
        var tweets = new List<Tweet>();
        string json = File.ReadAllText(filePath);
        var jsonData = JObject.Parse(json)["data"];
        foreach (var token in jsonData.Children())
        {
            var tweet = JsonConvert.DeserializeObject<Tweet>(token.ToString());
            // tweet.CreatedAt = ParseTweetDateTime(token["CreatedAt"].ToString());
            tweets.Add(tweet);
        }
        return tweets;
    }
    
    private static DateTime ParseTweetDateTime(string dateString)
    {
        string format = "MMMM dd, yyyy 'at' hh:mmtt";
        return DateTime.ParseExact(dateString, format, System.Globalization.CultureInfo.InvariantCulture);
    }

    public static void Main(string[] args)
    {
        // string filePath = "/home/mswiatek/RiderProjects/lab3/lab3/data.json";
        // List<Tweet> tweets = ReadTweets(filePath);
        string xmlPath = "/home/mswiatek/RiderProjects/lab3/lab3/tweets.xml";
        // tweets.SaveToXml(xmlPath);
        
        // var tweetsFromXml = TweetExtensions.LoadFromXml(xmlPath);
        var tweets = TweetExtensions.LoadFromXml(xmlPath);

        // Sortowanie tweetów
        var sortedByUsername = tweets.SortTweetsByUsername();
        var sortedByDate = tweets.SortTweetsByCreatedAt();

        // Najnowszy i najstarszy tweet
        var newestTweet = tweets.GetNewestTweet();
        var oldestTweet = tweets.GetOldestTweet();
        Console.WriteLine($"Newest Tweet: {newestTweet.Text}");
        Console.WriteLine($"Oldest Tweet: {oldestTweet.Text}");

        // Słownik tweetów użytkowników
        var userTweets = tweets.CreateUsernameDictionary();

        // Częstość występowania słów
        var wordFrequency = tweets.CalculateWordFrequency();

        // 10 najczęściej występujących wyrazów o długości co najmniej 5 liter
        var top10LongestWords = wordFrequency.GetTop10LongestWords();
        Console.WriteLine("Top 10 longest words:");
        top10LongestWords.ForEach(Console.WriteLine);

        // Obliczanie IDF
        var idf = tweets.CalculateIDF();
        var top10IDFWords = idf.GetTop10IDFWords();
        Console.WriteLine("Top 10 IDF words:");
        top10IDFWords.ForEach(Console.WriteLine);
    }
}
