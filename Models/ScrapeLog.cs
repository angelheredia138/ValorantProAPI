public class ScrapeLog
{
    public int Id { get; set; }
    public string Operation { get; set; } = null!; // e.g., "scrape_all_teams" or "scrape_all_players"
    public DateTime LastRun { get; set; }
}