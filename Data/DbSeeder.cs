public static class DbSeeder
{
    public static void Seed(ValorantDbContext context)
    {
        if (!context.Teams.Any())
        {
            var team1 = new Team
            {
                Id = 1,
                Name = "Team Alpha",
                Region = "Americas",
                LogoUrl = "alpha.png"
            };

            var player1 = new Player
            {
                Id = 1,
                Name = "Player A",
                RealName = "Alex Smith", // New property
                ProfileImageUrl = "https://example.com/player-a.png", // New property
                TeamId = team1.Id,
                Team = team1
            };

            var player2 = new Player
            {
                Id = 2,
                Name = "Player B",
                RealName = "Jamie Doe",
                ProfileImageUrl = "https://example.com/player-b.png",
                TeamId = team1.Id,
                Team = team1
            };

            context.Teams.Add(team1);
            context.Players.Add(player1);
            context.Players.Add(player2);

            context.SaveChanges();
        }
    }
}
