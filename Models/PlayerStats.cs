public class PlayerStats
{
    public int PlayerId { get; set; }
    public string? CurrentTeam { get; set; }
    public List<string> PastTeams { get; set; } = new();
    public string TotalWinnings { get; set; } = "0";
    public List<string> SocialMediaHandles { get; set; } = new();
    public List<MatchResult> LastMatches { get; set; } = new();
    public List<AgentStats> TopAgents { get; set; } = new(); // New property for agent stats
}