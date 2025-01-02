using HtmlAgilityPack;
using System.Net.Http;
using Microsoft.EntityFrameworkCore;

public class VlrScraperService
{
    private readonly HttpClient _httpClient;
    private readonly ValorantDbContext _context;

    public VlrScraperService(HttpClient httpClient, ValorantDbContext context)
    {
        _httpClient = httpClient;
        _context = context;
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
                var href = nameNode.GetAttributeValue("href", "").Trim();
                var teamIdMatch = System.Text.RegularExpressions.Regex.Match(href, @"/team/(\d+)/");
                int teamId = teamIdMatch.Success ? int.Parse(teamIdMatch.Groups[1].Value) : 0;

                var team = new Team
                {
                    Id = teamId, // Save the parsed team ID
                    Name = nameNode.InnerText.Trim(),
                    LogoUrl = logoNode.GetAttributeValue("src", "").Trim(),
                    Region = region
                };

                if (!seenTeamNames.Contains(team.Name))
                {
                    seenTeamNames.Add(team.Name);
                    teams.Add(team);
                }
            }
        }
        return teams;
    }
    public async Task<List<Player>> ScrapePlayersFromTeam(int teamId)
    {
        // Fetch the team from the database to ensure it's tracked
        var team = await _context.Teams.FirstOrDefaultAsync(t => t.Id == teamId);
        if (team == null)
        {
            throw new Exception($"Team with ID {teamId} not found in the database.");
        }

        // Construct the team page URL
        var url = $"https://www.vlr.gg/team/{teamId}";

        // Fetch the HTML content from the team page
        var html = await _httpClient.GetStringAsync(url);

        // Parse the HTML content using HtmlAgilityPack
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(html);

        // Find the section of the HTML containing player information
        var playerNodes = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'team-roster-item')]");
        if (playerNodes == null)
        {
            return new List<Player>(); // Return an empty list if no players are found
        }

        // Extract player information and convert to Player objects
        var players = new List<Player>();
        var existingPlayerIds = new HashSet<int>(); // Track player IDs to avoid duplicates

        foreach (var playerNode in playerNodes)
        {
            var linkNode = playerNode.SelectSingleNode(".//a");
            var aliasNode = playerNode.SelectSingleNode(".//div[contains(@class, 'team-roster-item-name-alias')]");
            var realNameNode = playerNode.SelectSingleNode(".//div[contains(@class, 'team-roster-item-name-real')]");
            var imgNode = playerNode.SelectSingleNode(".//img");
            var flagNode = aliasNode?.SelectSingleNode(".//i[contains(@class, 'flag')]");
            var captainNode = aliasNode?.SelectSingleNode(".//i[contains(@class, 'fa-star')]");
            var roleNode = playerNode.SelectSingleNode(".//div[contains(@class, 'wf-tag mod-light team-roster-item-name-role')]");

            if (linkNode != null && aliasNode != null)
            {
                // Extract player ID from the href
                var href = linkNode.GetAttributeValue("href", "").Trim();
                var playerIdMatch = System.Text.RegularExpressions.Regex.Match(href, @"/player/(\d+)/");
                if (!playerIdMatch.Success) continue;

                int playerId = int.Parse(playerIdMatch.Groups[1].Value);
                if (existingPlayerIds.Contains(playerId)) continue; // Skip duplicates
                existingPlayerIds.Add(playerId);

                // Extract image URL
                var imageUrl = imgNode?.GetAttributeValue("src", "").Trim();

                // Determine if this is a staff member
                var isStaff = roleNode != null;

                // Create a new Player object with additional fields
                var player = new Player
                {
                    Id = playerId,
                    Name = aliasNode.InnerText.Trim(),
                    RealName = realNameNode?.InnerText.Trim() ?? "Unknown",
                    ProfileImageUrl = string.IsNullOrEmpty(imageUrl) ? null : imageUrl.StartsWith("//") ? "https:" + imageUrl : imageUrl,
                    IsCaptain = captainNode != null,
                    Country = flagNode?.GetAttributeValue("class", "").Split(' ').LastOrDefault()?.Replace("mod-", "") ?? "Unknown",
                    IsStaff = isStaff,
                    RoleDescription = isStaff ? roleNode.InnerText.Trim() : null,
                    TeamId = teamId
                };

                // Check if the player already exists in the database
                if (!_context.Players.Any(p => p.Id == player.Id))
                {
                    players.Add(player);
                }
            }
        }

        // Add new players to the database
        _context.Players.AddRange(players);
        await _context.SaveChangesAsync();

        return players;
    }

}
