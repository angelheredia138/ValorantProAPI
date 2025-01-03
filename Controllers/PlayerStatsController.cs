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

            // Optionally save player stats if you want to persist them
            // Add a PlayerStats table in the database for persistence
            // _context.PlayerStats.Add(playerStats);
            // await _context.SaveChangesAsync();

            return Ok(playerStats);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error scraping stats for player {playerId}: {ex.Message}");
        }
    }
}
