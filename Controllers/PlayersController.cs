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

    // GET: api/Players/scrape/all
    [HttpGet("scrape/all")]
    public async Task<ActionResult<IEnumerable<Player>>> ScrapeAllPlayers()
    {
        try
        {
            // Retrieve all team IDs from the database
            var teams = await _context.Teams.ToListAsync();

            if (!teams.Any())
            {
                return BadRequest("No teams found in the database. Please add teams first.");
            }

            var allScrapedPlayers = new List<Player>();

            foreach (var team in teams)
            {
                try
                {
                    // Log the team being processed
                    Console.WriteLine($"Scraping players for team: {team.Name} (ID: {team.Id})");

                    // Scrape players for the current team
                    var players = await _scraperService.ScrapePlayersFromTeam(team.Id);

                    foreach (var player in players)
                    {
                        // Add to response collection
                        allScrapedPlayers.Add(player);

                        // Save new players to the database
                        if (!_context.Players.Any(p => p.Name == player.Name && p.TeamId == player.TeamId))
                        {
                            _context.Players.Add(player);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log errors for individual teams and continue with others
                    Console.WriteLine($"Error scraping players for team {team.Name} (ID: {team.Id}): {ex.Message}");
                }
            }

            // Save all changes to the database at once
            await _context.SaveChangesAsync();

            // Return all scraped players (including duplicates)
            return Ok(allScrapedPlayers);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error scraping players from all teams: {ex.Message}");
        }
    }

}

