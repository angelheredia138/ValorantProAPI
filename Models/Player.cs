using System.Text.Json.Serialization;

public class Player
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? RealName { get; set; }
    public string? ProfileImageUrl { get; set; }
    public int TeamId { get; set; }
    public bool IsCaptain { get; set; }
    public string? Country { get; set; }
    public bool IsStaff { get; set; }
    public string? RoleDescription { get; set; } // For staff roles (e.g., head coach)
    [JsonIgnore] // Prevent cyclic reference during serialization
    public Team Team { get; set; } = null!;
}
