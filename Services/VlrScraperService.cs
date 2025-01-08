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
    private string NormalizeImageUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
            return null;

        // Add "https:" if the URL starts with "//"
        if (url.StartsWith("//"))
            return "https:" + url;

        return url; // Return the original URL if no changes are needed
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
                    LogoUrl = NormalizeImageUrl(logoNode.GetAttributeValue("src", "").Trim()),
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
                var normalizedImageUrl = NormalizeImageUrl(imageUrl);

                // Determine if this is a staff member
                var isStaff = roleNode != null;

                // Create a new Player object with additional fields
                var player = new Player
                {
                    Id = playerId,
                    Name = aliasNode.InnerText.Trim(),
                    RealName = realNameNode?.InnerText.Trim() ?? "Unknown",
                    ProfileImageUrl = normalizedImageUrl, // Use normalized image URL
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
    public async Task<PlayerStats> ScrapePlayerStats(int playerId)
    {
        var url = $"https://www.vlr.gg/player/{playerId}/?timespan=90d";

        var html = await _httpClient.GetStringAsync(url);
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(html);

        var playerStats = new PlayerStats { PlayerId = playerId };
        // Extract player image
        var imageNode = htmlDoc.DocumentNode.SelectSingleNode("//div[contains(@class, 'wf-avatar mod-player')]//img");
        if (imageNode != null)
        {
            var imageUrl = imageNode.GetAttributeValue("src", "").Trim();
            if (!string.IsNullOrEmpty(imageUrl))
            {
                playerStats.PlayerImage = imageUrl.StartsWith("//") ? $"https:{imageUrl}" : imageUrl;
            }
        }

        // Extract player real name
        var realNameNode = htmlDoc.DocumentNode.SelectSingleNode("//h2[contains(@class, 'player-real-name')]");
        if (realNameNode != null)
        {
            playerStats.PlayerRealName = realNameNode.InnerText.Trim();
        }
        // Extract player's username
        var usernameNode = htmlDoc.DocumentNode.SelectSingleNode("//h1[contains(@class, 'wf-title')]");
        if (usernameNode != null)
        {
            playerStats.Username = usernameNode.InnerText.Trim();
        }

        // Extract country
        var countryNode = htmlDoc.DocumentNode.SelectSingleNode("//div[contains(@class, 'ge-text-light') and i[contains(@class, 'flag')]]/i");
        if (countryNode != null)
        {
            // Extract the class attribute and look for the "mod-XX" part
            var flagClass = countryNode.GetAttributeValue("class", "").Split(' ').FirstOrDefault(c => c.StartsWith("mod-"));
            if (!string.IsNullOrEmpty(flagClass))
            {
                playerStats.Country = flagClass.Replace("mod-", "").ToLower(); // Extract "us" and convert to lowercase
            }
        }

        // Extract social media handles
        var socialMediaNodes = htmlDoc.DocumentNode.SelectNodes("//a[contains(@href, 'x.com') or contains(@href, 'twitch.tv')]");
        if (socialMediaNodes != null)
        {
            foreach (var node in socialMediaNodes)
            {
                var handle = node.GetAttributeValue("href", "").Trim();
                if (!string.IsNullOrEmpty(handle))
                {
                    playerStats.SocialMediaHandles.Add(handle);
                }
            }
        }

        // Extract current team and image
        var currentTeamNode = htmlDoc.DocumentNode.SelectSingleNode("//h2[contains(text(),'Current Teams')]/following-sibling::div/a[contains(@class, 'wf-module-item')]");
        if (currentTeamNode != null)
        {
            var teamName = currentTeamNode.SelectSingleNode(".//div[contains(@style, 'font-weight: 500')]")?.InnerText.Trim();
            var teamImageNode = currentTeamNode.SelectSingleNode(".//img");
            var teamImage = teamImageNode?.GetAttributeValue("src", "").Trim();

            if (!string.IsNullOrEmpty(teamImage))
            {
                teamImage = teamImage.StartsWith("//") ? $"https:{teamImage}" : teamImage;
            }

            playerStats.CurrentTeam = teamName;
            playerStats.CurrentTeamImage = teamImage;
        }


        // Extract past teams
        var pastTeamNodes = htmlDoc.DocumentNode.SelectNodes("//h2[contains(text(),'Past Teams')]/following-sibling::div/a[contains(@class, 'wf-module-item')]");
        if (pastTeamNodes != null)
        {
            foreach (var node in pastTeamNodes)
            {
                // Extract team name
                var teamName = node.SelectSingleNode(".//div[contains(@style, 'font-weight: 500')]")?.InnerText.Trim();

                // Handle missing team name
                if (string.IsNullOrEmpty(teamName))
                {
                    teamName = "Unknown Team"; // Default placeholder
                }

                // Extract duration (with multiple edge case handling)
                var durationNode = node.SelectNodes(".//div[contains(@class, 'ge-text-light')]");
                string duration = null;

                if (durationNode != null)
                {
                    foreach (var subNode in durationNode)
                    {
                        var text = subNode.InnerText.Trim();
                        if (!string.IsNullOrEmpty(text))
                        {
                            if (text.Contains("–")) // Typical duration format: "Jan 2022 – Dec 2022"
                            {
                                duration = text;
                                break;
                            }
                            else if (text.StartsWith("left in", StringComparison.OrdinalIgnoreCase)) // Edge case: "left in 2023"
                            {
                                duration = text;
                                break;
                            }
                            else if (text.StartsWith("joined", StringComparison.OrdinalIgnoreCase)) // Edge case: "joined Dec 2021"
                            {
                                duration = text;
                                break;
                            }
                        }
                    }
                }

                // Fallback for missing duration
                if (string.IsNullOrEmpty(duration))
                {
                    duration = "Unknown Duration";
                }

                // Extract team image (if available)
                var teamImageNode = node.SelectSingleNode(".//img");
                var teamImage = teamImageNode?.GetAttributeValue("src", "").Trim();
                if (!string.IsNullOrEmpty(teamImage))
                {
                    teamImage = teamImage.StartsWith("//") ? $"https:{teamImage}" : teamImage;
                }

                // Add team info to player stats
                playerStats.PastTeams.Add($"{teamName} ({duration})");
                playerStats.PastTeamImages.Add(teamImage ?? "No Image Available");
            }
        }

        // Extract total winnings
        var winningsNode = htmlDoc.DocumentNode.SelectSingleNode("//div[contains(text(),'Total Winnings')]/following-sibling::span");
        if (winningsNode != null)
        {
            playerStats.TotalWinnings = winningsNode.InnerText.Trim();
        }

        // Extract last matches
        var matchNodes = htmlDoc.DocumentNode.SelectNodes("//h2[contains(text(),'Recent Results')]/following-sibling::div//a[contains(@class, 'm-item')]");
        if (matchNodes != null)
        {
            foreach (var matchNode in matchNodes.Take(3)) // Last three matches
            {
                // Extract event name
                var eventName = matchNode.SelectSingleNode(".//div[contains(@class, 'm-item-event')]//div[contains(@style, 'font-weight: 700')]")
                    ?.InnerText.Trim() ?? "Unknown Event";

                // Extract stage
                var stage = matchNode.SelectSingleNode(".//div[contains(@class, 'm-item-event')]")?.InnerText.Trim()
                    .Replace("\n", "").Replace("\t", "").Replace("&sdot;", "·").Split('·').LastOrDefault()?.Trim() ?? "Unknown Stage";

                // Extract match image
                var matchImageNode = matchNode.SelectSingleNode(".//div[contains(@class, 'm-item-thumb')]//img");
                var matchImage = matchImageNode?.GetAttributeValue("src", "").Trim();
                if (!string.IsNullOrEmpty(matchImage))
                {
                    matchImage = matchImage.StartsWith("//") ? $"https:{matchImage}" : matchImage;
                }

                // Extract team name
                var teamName = matchNode.SelectSingleNode(".//div[contains(@class, 'm-item-team') and not(contains(@class, 'mod-right'))]/span[@class='m-item-team-name']")
                    ?.InnerText.Trim() ?? "Unknown Team";

                // Extract team image (fixing the issue)
                var teamImageNode = matchNode.SelectSingleNode(".//div[contains(@class, 'm-item-team') and not(contains(@class, 'mod-right'))]/following-sibling::div[contains(@class, 'm-item-logo')]//img");
                var teamImage = teamImageNode?.GetAttributeValue("src", "").Trim();
                if (!string.IsNullOrEmpty(teamImage))
                {
                    teamImage = teamImage.StartsWith("//") ? $"https:{teamImage}" : teamImage;
                }

                // Extract opponent name
                var opponentNode = matchNode.SelectSingleNode(".//div[contains(@class, 'm-item-team') and contains(@class, 'mod-right')]/span[contains(@class, 'm-item-team-name')]");
                var opponentName = opponentNode?.InnerText.Trim() ?? "Unknown Opponent";

                // Extract opponent image
                var opponentImageNode = matchNode.SelectSingleNode(".//div[contains(@class, 'm-item-logo mod-right')]//img");
                var opponentImage = opponentImageNode?.GetAttributeValue("src", "").Trim();
                if (!string.IsNullOrEmpty(opponentImage))
                {
                    opponentImage = opponentImage.StartsWith("//") ? $"https:{opponentImage}" : opponentImage;
                }

                // Extract result
                var result = matchNode.SelectSingleNode(".//div[contains(@class, 'm-item-result')]")
                    ?.InnerText.Trim().Replace("\n", "").Replace("\t", "").Replace(" ", "") ?? "Unknown Result";

                // Extract match date and time
                var date = matchNode.SelectSingleNode(".//div[contains(@class, 'm-item-date')]/div")
                    ?.InnerText.Trim() ?? "Unknown Date";
                var time = matchNode.SelectSingleNode(".//div[contains(@class, 'm-item-date')]/following-sibling::text()")
                    ?.InnerText.Trim() ?? "Unknown Time";

                // Add match data to player stats
                playerStats.LastMatches.Add(new MatchResult
                {
                    EventName = eventName,
                    Stage = stage,
                    MatchImage = matchImage,
                    TeamName = teamName,
                    TeamImage = teamImage,
                    Opponent = opponentName,
                    OpponentImage = opponentImage,
                    Result = result,
                    Date = $"{date} {time}"
                });
            }
        }

        // Extract agent stats and images
        var agentRows = htmlDoc.DocumentNode.SelectNodes("//table[@class='wf-table']//tbody/tr");
        if (agentRows != null)
        {
            foreach (var row in agentRows.Take(3)) // Only take the top 3 agents
            {
                // Extract agent name
                var agentName = row.SelectSingleNode(".//img")?.GetAttributeValue("alt", "Unknown") ?? "Unknown";

                // Extract agent image
                var agentImageNode = row.SelectSingleNode(".//img");
                var agentImage = agentImageNode?.GetAttributeValue("src", "").Trim();
                if (!string.IsNullOrEmpty(agentImage))
                {
                    agentImage = agentImage.StartsWith("/img/") ? $"https://www.vlr.gg{agentImage}" : agentImage;
                }

                // Extract usage percentage
                var usage = row.SelectSingleNode(".//td[contains(@class, 'mod-right')][1]")?.InnerText.Trim() ?? "0%";

                // Extract rounds played
                var roundsPlayedText = row.SelectSingleNode(".//td[contains(@class, 'mod-right')][2]")?.InnerText.Trim() ?? "0";
                var roundsPlayed = int.TryParse(roundsPlayedText, out var rp) ? rp : 0;

                // Extract rating
                var ratingText = row.SelectSingleNode(".//td[contains(@class, 'mod-center')]")?.InnerText.Trim() ?? "0";
                var rating = double.TryParse(ratingText, out var rt) ? rt : 0;

                // Extract ACS
                var acsText = row.SelectSingleNode(".//td[contains(@class, 'mod-right')][3]")?.InnerText.Trim() ?? "0";
                var acs = double.TryParse(acsText, out var ac) ? ac : 0;

                // Extract KD
                var kdText = row.SelectSingleNode(".//td[contains(@class, 'mod-right')][4]")?.InnerText.Trim() ?? "0";
                var kd = double.TryParse(kdText, out var kdVal) ? kdVal : 0;

                // Extract ADR
                var adrText = row.SelectSingleNode(".//td[contains(@class, 'mod-right')][5]")?.InnerText.Trim() ?? "0";
                var adr = double.TryParse(adrText, out var adrVal) ? adrVal : 0;

                // Extract KAST percentage
                var kast = row.SelectSingleNode(".//td[contains(@class, 'mod-right')][6]")?.InnerText.Trim() ?? "0%";

                // Extract KPR (Kills Per Round)
                var kprText = row.SelectSingleNode(".//td[contains(@class, 'mod-right')][7]")?.InnerText.Trim() ?? "0";
                var kpr = double.TryParse(kprText, out var kprVal) ? kprVal : 0;

                // Extract APR (Assists Per Round)
                var aprText = row.SelectSingleNode(".//td[contains(@class, 'mod-right')][8]")?.InnerText.Trim() ?? "0";
                var apr = double.TryParse(aprText, out var aprVal) ? aprVal : 0;

                // Extract FKPR (First Kills Per Round)
                var fkprText = row.SelectSingleNode(".//td[contains(@class, 'mod-right')][9]")?.InnerText.Trim() ?? "0";
                var fkpr = double.TryParse(fkprText, out var fkprVal) ? fkprVal : 0;

                // Extract FDPR (First Deaths Per Round)
                var fdprText = row.SelectSingleNode(".//td[contains(@class, 'mod-right')][10]")?.InnerText.Trim() ?? "0";
                var fdpr = double.TryParse(fdprText, out var fdprVal) ? fdprVal : 0;

                // Add the extracted data to the list of top agents
                var agentStats = new AgentStats
                {
                    AgentName = agentName,
                    AgentImage = agentImage,
                    Usage = usage,
                    RoundsPlayed = roundsPlayed,
                    Rating = rating,
                    ACS = acs,
                    KD = kd,
                    ADR = adr,
                    KAST = kast,
                    KPR = kpr,
                    APR = apr,
                    FKPR = fkpr,
                    FDPR = fdpr
                };

                playerStats.TopAgents.Add(agentStats);
            }
        }

        return playerStats;
    }

}
