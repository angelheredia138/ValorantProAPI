public static class DbSeeder
{
    public static void Seed(ValorantDbContext context)
    {
        // Leave the database empty initially
        if (!context.Teams.Any() && !context.Players.Any())
        {
            Console.WriteLine("Database initialized but left empty for scraping.");
        }
    }
}
