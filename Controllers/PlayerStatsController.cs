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
    private async Task<bool> IsScrapeRecent(string operation, TimeSpan duration)
    {
        var log = await _context.ScrapeLogs.FirstOrDefaultAsync(l => l.Operation == operation);
        return log != null && DateTime.UtcNow - log.LastRun < duration;
    }

    private async Task UpdateScrapeLog(string operation)
    {
        var log = await _context.ScrapeLogs.FirstOrDefaultAsync(l => l.Operation == operation);
        if (log == null)
        {
            _context.ScrapeLogs.Add(new ScrapeLog { Operation = operation, LastRun = DateTime.UtcNow });
        }
        else
        {
            log.LastRun = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync();
    }
    // GET: api/PlayerStats/scrape/{playerId}
    [HttpGet("scrape/{playerId}")]
    public async Task<ActionResult<PlayerStats>> ScrapePlayerStats(int playerId)
    {
        try
        {
            // Check if the scrape for this player's stats was recently run
            if (await IsScrapeRecent($"scrape_player_stats_{playerId}", TimeSpan.FromHours(1)))
            {
                Console.WriteLine($"Returning cached stats for player ID: {playerId}");
                // Retrieve and return cached player stats if available
                var cachedPlayerStats = await _context.PlayerStats.FirstOrDefaultAsync(ps => ps.PlayerId == playerId);
                if (cachedPlayerStats != null)
                {
                    return Ok(cachedPlayerStats);
                }

                // If cached stats are not found, proceed to scrape (fallback scenario)
                Console.WriteLine($"No cached stats found for player ID: {playerId}, proceeding to scrape.");
            }

            // Ensure the player exists in the database
            var playerExists = await _context.Players.AnyAsync(p => p.Id == playerId);
            if (!playerExists)
            {
                return NotFound($"Player with ID {playerId} not found in the database.");
            }

            // Scrape the player stats
            var playerStats = await _scraperService.ScrapePlayerStats(playerId);

            // Save the player stats to the database
            var existingStats = await _context.PlayerStats.FirstOrDefaultAsync(ps => ps.PlayerId == playerId);
            if (existingStats != null)
            {
                _context.PlayerStats.Remove(existingStats); // Remove old stats
            }
            _context.PlayerStats.Add(playerStats); // Add new stats

            await _context.SaveChangesAsync();
            await UpdateScrapeLog($"scrape_player_stats_{playerId}");

            return Ok(playerStats);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error scraping stats for player {playerId}: {ex.Message}");
        }
    }

}
