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


    [HttpGet("scrape/{teamId}")]
    public async Task<ActionResult<IEnumerable<Player>>> ScrapePlayers(int teamId)
    {
        try
        {
            // Check if the scrape for this team ID was recently run
            if (await IsScrapeRecent($"scrape_team_{teamId}", TimeSpan.FromHours(1)))
            {
                Console.WriteLine($"Returning cached players for team ID: {teamId}");
                return Ok(await _context.Players.Where(p => p.TeamId == teamId).ToListAsync());
            }

            // Scrape the players for the specified team
            var players = await _scraperService.ScrapePlayersFromTeam(teamId);

            foreach (var player in players)
            {
                if (!_context.Players.Any(p => p.Name == player.Name && p.TeamId == player.TeamId))
                {
                    _context.Players.Add(player);
                }
            }

            await _context.SaveChangesAsync();
            await UpdateScrapeLog($"scrape_team_{teamId}");

            return Ok(players);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error scraping players for team ID {teamId}: {ex.Message}");
        }
    }


    [HttpGet("scrape/all")]
    public async Task<ActionResult<IEnumerable<Player>>> ScrapeAllPlayers()
    {
        try
        {
            // Check if the scrape-all operation was recently run
            if (await IsScrapeRecent("scrape_all_players", TimeSpan.FromHours(6)))
            {
                Console.WriteLine("Returning cached players.");
                return Ok(await _context.Players.ToListAsync());
            }

            var teams = await _context.Teams.ToListAsync();
            if (!teams.Any())
            {
                return BadRequest("No teams found in the database. Please scrape teams first.");
            }

            var allScrapedPlayers = new List<Player>();

            foreach (var team in teams)
            {
                var players = await _scraperService.ScrapePlayersFromTeam(team.Id);

                foreach (var player in players)
                {
                    if (!_context.Players.Any(p => p.Name == player.Name && p.TeamId == player.TeamId))
                    {
                        _context.Players.Add(player);
                    }

                    if (!allScrapedPlayers.Any(p => p.Name == player.Name && p.TeamId == player.TeamId))
                    {
                        allScrapedPlayers.Add(player);
                    }
                }
            }

            await _context.SaveChangesAsync();
            await UpdateScrapeLog("scrape_all_players");

            return Ok(allScrapedPlayers);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error scraping players from all teams: {ex.Message}");
        }
    }
}

