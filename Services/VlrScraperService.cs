using HtmlAgilityPack; // Assuming HtmlAgilityPack is used for web scraping
using System.Net.Http;

public class VlrScraperService
{
    private readonly HttpClient _httpClient;

    public VlrScraperService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<Team>> ScrapeTeamsFromVlr()
    {
        // The event URL you want to scrape
        var url = "https://www.vlr.gg/event/2277/champions-tour-2025-pacific-kickoff";

        // Fetch the HTML content from the event page
        var html = await _httpClient.GetStringAsync(url);

        // Parse the HTML content using HtmlAgilityPack
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(html);

        // Find the section of the HTML containing team information
        var teamNodes = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'event-team')]");

        if (teamNodes == null)
        {
            return new List<Team>(); // Return an empty list if no teams are found
        }

        // Extract team information and convert to Team objects
        var teams = new List<Team>();
        foreach (var teamNode in teamNodes)
        {
            var nameNode = teamNode.SelectSingleNode(".//a[contains(@class, 'event-team-name')]");
            var logoNode = teamNode.SelectSingleNode(".//img[contains(@class, 'event-team-players-mask-team')]");

            if (nameNode != null && logoNode != null)
            {
                var team = new Team
                {
                    Name = nameNode.InnerText.Trim(),
                    LogoUrl = logoNode.GetAttributeValue("src", "").Trim(),
                    Region = "Pacific" // Replace with logic if the region can be derived
                };

                teams.Add(team);
            }
        }

        return teams;
    }
}
