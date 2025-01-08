public class PlayerStats
{
    public int PlayerId { get; set; }
    public string? PlayerImage { get; set; } // URL for the player's image
    public string? PlayerRealName { get; set; } // Player's real name
    public string? Country { get; set; } // Player's country
    public string? CurrentTeam { get; set; }
    public string? CurrentTeamImage { get; set; } // Current team's image
    public List<string> PastTeams { get; set; } = new();
    public List<string> PastTeamImages { get; set; } = new(); // Past teams' images
    public string TotalWinnings { get; set; } = "0";
    public List<string> SocialMediaHandles { get; set; } = new();
    public List<MatchResult> LastMatches { get; set; } = new();
    public List<AgentStats> TopAgents { get; set; } = new();
}
