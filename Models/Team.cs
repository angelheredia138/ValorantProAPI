using System.Text.Json.Serialization;

public class Team
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Region { get; set; }
    public required string LogoUrl { get; set; }

    [JsonIgnore] // Avoid cycles when serializing individual players
    public List<Player> Players { get; set; } = new();
}