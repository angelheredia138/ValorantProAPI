public static class DbSeeder
{
    public static void Seed(ValorantDbContext context)
    {
        if (!context.Teams.Any())
        {
            // Create the team
            var team1 = new Team 
            { 
                Id = 1, 
                Name = "Team Alpha", 
                Region = "Americas", 
                LogoUrl = "alpha.png" 
            };

            // Create the player, explicitly setting TeamId
            var player1 = new Player
            {
                Id = 1,
                Name = "Player A",
                Role = "Duelist",
                Age = 25,
                TeamId = team1.Id, // Explicitly set TeamId
                Team = team1,      // Also set the navigation property
                Stats = new PlayerStats
                {
                    Kills = 100,
                    Deaths = 50,
                    Assists = 30,
                    MatchesPlayed = 20
                }
            };

            // Add the team and player to the context
            context.Teams.Add(team1);
            context.Players.Add(player1);

            // Save changes
            context.SaveChanges();
        }
    }
}
