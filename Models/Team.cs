public class Team
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Region { get; set; }
    public required string LogoUrl { get; set; }
    public List<Player> Players { get; set; } = new();
    public List<string> Accomplishments { get; set; } = new();
}
