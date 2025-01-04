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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure PlayerStats with a key
        modelBuilder.Entity<PlayerStats>().HasKey(ps => ps.PlayerId);

        // Configure relationships for PlayerStats
        modelBuilder.Entity<PlayerStats>()
            .HasOne<Player>()
            .WithMany()
            .HasForeignKey(ps => ps.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure MatchResult and AgentStats as owned types
        modelBuilder.Entity<PlayerStats>().OwnsMany(ps => ps.LastMatches);
        modelBuilder.Entity<PlayerStats>().OwnsMany(ps => ps.TopAgents);
    }


}
