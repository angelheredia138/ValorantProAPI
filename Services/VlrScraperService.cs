using HtmlAgilityPack;
using System.Net.Http;

public class VlrScraperService
{
    private readonly HttpClient _httpClient;

    public VlrScraperService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<Team>> ScrapeTeamsFromVlr(string region)
    {
        // Define URLs for each region
        var regionUrls = new Dictionary<string, string>
        {
            { "emea", "https://www.vlr.gg/event/2276/champions-tour-2025-emea-kickoff" },
            { "pacific", "https://www.vlr.gg/event/2277/champions-tour-2025-pacific-kickoff" },
            { "americas", "https://www.vlr.gg/event/2274/champions-tour-2025-americas-kickoff" },
            { "china", "https://www.vlr.gg/event/2275/champions-tour-2025-china-kickoff" }
        };

        // Ensure the region is valid
        if (!regionUrls.ContainsKey(region.ToLower()))
        {
            throw new ArgumentException("Invalid region specified");
        }

        // Fetch the URL for the specified region
        var url = regionUrls[region.ToLower()];

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
        var seenTeamNames = new HashSet<string>(); // Keep track of team names to avoid duplicates

        foreach (var teamNode in teamNodes)
        {
            var nameNode = teamNode.SelectSingleNode(".//a[contains(@class, 'event-team-name')]");
            var logoNode = teamNode.SelectSingleNode(".//img[contains(@class, 'event-team-players-mask-team')]");

            if (nameNode != null && logoNode != null)
            {
                var teamName = nameNode.InnerText.Trim();
                if (!seenTeamNames.Contains(teamName))
                {
                    seenTeamNames.Add(teamName); // Add the team name to the set

                    var team = new Team
                    {
                        Name = teamName,
                        LogoUrl = logoNode.GetAttributeValue("src", "").Trim(),
                        Region = region // Assign the region dynamically
                    };

                    teams.Add(team);
                }
            }
        }

        return teams;
    }
}
