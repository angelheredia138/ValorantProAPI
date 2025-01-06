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
            // Check if players for the specified team already exist in the database
            var existingPlayers = await _context.Players.Where(p => p.TeamId == teamId).ToListAsync();

            if (existingPlayers.Any())
            {
                Console.WriteLine($"Returning cached players for team ID: {teamId}");
                return Ok(existingPlayers); // Return cached players
            }

            // If no players are in the database for the team, proceed with scraping
            var scrapedPlayers = await _scraperService.ScrapePlayersFromTeam(teamId);

            foreach (var player in scrapedPlayers)
            {
                // Save new players to the database
                if (!_context.Players.Any(p => p.Id == player.Id))
                {
                    _context.Players.Add(player);
                }
            }

            // Save changes to the database
            await _context.SaveChangesAsync();

            return Ok(scrapedPlayers); // Return the newly scraped players
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
            // Retrieve all teams from the database
            var teams = await _context.Teams.ToListAsync();

            if (!teams.Any())
            {
                return BadRequest("No teams found in the database. Please scrape teams first.");
            }

            var allScrapedPlayers = new List<Player>();

            foreach (var team in teams)
            {
                try
                {
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
                    Console.WriteLine($"Error scraping players for team {team.Name} (ID: {team.Id}): {ex.Message}");
                }
            }

            // Save all changes to the database at once
            await _context.SaveChangesAsync();

            return Ok(allScrapedPlayers);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error scraping players from all teams: {ex.Message}");
        }
    }

}

