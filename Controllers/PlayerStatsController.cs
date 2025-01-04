using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class PlayerStatsController : ControllerBase
{
    private readonly ValorantDbContext _context;
    private readonly VlrScraperService _scraperService;

    public PlayerStatsController(ValorantDbContext context, VlrScraperService scraperService)
    {
        _context = context;
        _scraperService = scraperService;
    }

    // GET: api/PlayerStats/scrape/{playerId}
    [HttpGet("scrape/{playerId}")]
    public async Task<ActionResult<PlayerStats>> ScrapePlayerStats(int playerId)
    {
        try
        {
            var playerExists = await _context.Players.AnyAsync(p => p.Id == playerId);
            if (!playerExists)
            {
                return NotFound($"Player with ID {playerId} not found in the database.");
            }

            var playerStats = await _scraperService.ScrapePlayerStats(playerId);


            return Ok(playerStats);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error scraping stats for player {playerId}: {ex.Message}");
        }
    }

    // GET: api/PlayerStats/scrape/all
    [HttpGet("scrape/all")]
    public async Task<ActionResult<IEnumerable<PlayerStats>>> ScrapeAllPlayerStats()
    {
        try
        {
            // Retrieve all player IDs from the database
            var players = await _context.Players.ToListAsync();

            if (!players.Any())
            {
                return BadRequest("No players found in the database. Please add players first.");
            }

            var allScrapedPlayerStats = new List<PlayerStats>();

            foreach (var player in players)
            {
                try
                {
                    // Log the player being processed
                    Console.WriteLine($"Scraping stats for player: {player.Name} (ID: {player.Id})");

                    // Scrape stats for the current player
                    var playerStats = await _scraperService.ScrapePlayerStats(player.Id);

                    if (playerStats != null)
                    {
                        // Add to response collection
                        allScrapedPlayerStats.Add(playerStats);

                        // Save new stats to the database
                        if (!_context.PlayerStats.Any(ps => ps.PlayerId == player.Id))
                        {
                            _context.PlayerStats.Add(playerStats);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log errors for individual players and continue with others
                    Console.WriteLine($"Error scraping stats for player {player.Name} (ID: {player.Id}): {ex.Message}");
                }
            }

            // Save all changes to the database at once
            await _context.SaveChangesAsync();

            // Return all scraped player stats
            return Ok(allScrapedPlayerStats);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error scraping stats for all players: {ex.Message}");
        }
    }

}
