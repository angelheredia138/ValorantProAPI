using Microsoft.EntityFrameworkCore;

public class ValorantDbContext : DbContext
{
    public DbSet<Team> Teams { get; set; } = null!;
    public DbSet<Player> Players { get; set; } = null!;
    public DbSet<PlayerStats> PlayerStats { get; set; } = null!;

    public ValorantDbContext(DbContextOptions<ValorantDbContext> options)
        : base(options)
    {
    }
}

