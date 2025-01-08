public class MatchResult
{
    public string EventName { get; set; } = string.Empty;
    public string Stage { get; set; } = string.Empty;
    public string TeamName { get; set; } = string.Empty;
    public string Opponent { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string? MatchImage { get; set; } // Match-related image
    public string? TeamImage { get; internal set; }
    public string? OpponentImage { get; internal set; }
}
