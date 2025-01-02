public class Player
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Role { get; set; }
    public int Age { get; set; }
    public required int TeamId { get; set; }
    public required Team Team { get; set; }
    public required PlayerStats Stats { get; set; }
}
