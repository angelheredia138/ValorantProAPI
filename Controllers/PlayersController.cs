using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class PlayersController : ControllerBase
{
    private readonly ValorantDbContext _context;
    private readonly VlrScraperService _scraperService;

    public PlayersController(ValorantDbContext context, VlrScraperService scraperService)
    {
        _context = context;
        _scraperService = scraperService;
    }

    // GET: api/Players/scrape/{teamId}
    [HttpGet("scrape/{teamId}")]
    public async Task<ActionResult<IEnumerable<Player>>> ScrapePlayers(int teamId)
    {
        try
        {
            var players = await _scraperService.ScrapePlayersFromTeam(teamId);

            foreach (var player in players)
            {
                if (!_context.Players.Any(p => p.Name == player.Name && p.TeamId == player.TeamId))
                {
                    _context.Players.Add(player);
                }
            }

            await _context.SaveChangesAsync();

            return Ok(players);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error scraping players for team {teamId}: {ex.Message}");
        }
    }
}

