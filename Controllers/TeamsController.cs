using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class TeamsController : ControllerBase
{
    private readonly ValorantDbContext _context;
    private readonly VlrScraperService _scraperService;

    public TeamsController(ValorantDbContext context, VlrScraperService scraperService)
    {
        _context = context;
        _scraperService = scraperService;
    }

    // GET: api/Teams/scrape/{region}
    [HttpGet("scrape/{region}")]
    public async Task<ActionResult<IEnumerable<Team>>> ScrapeTeams(string region)
    {
        // Validate the region parameter
        var validRegions = new List<string> { "emea", "pacific", "americas", "china" };
        if (!validRegions.Contains(region.ToLower()))
        {
            return BadRequest($"Invalid region. Valid regions are: {string.Join(", ", validRegions)}");
        }

        try
        {
            // Use the scraper service to get teams for the specified region
            var scrapedTeams = await _scraperService.ScrapeTeamsFromVlr(region);

            // Optionally save the scraped teams to the database
            foreach (var team in scrapedTeams)
            {
                if (!_context.Teams.Any(t => t.Name == team.Name))
                {
                    _context.Teams.Add(team);
                }
            }

            await _context.SaveChangesAsync();

            return Ok(scrapedTeams); // Return the scraped teams
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error scraping teams for {region}: {ex.Message}");
        }
    }
    // GET: api/Teams/scrape/all
    [HttpGet("scrape/all")]
    public async Task<ActionResult<IEnumerable<Team>>> ScrapeAllRegions()
    {
        try
        {
            // Check if there are already teams in the database
            var existingTeams = await _context.Teams.ToListAsync();
            if (existingTeams.Any())
            {
                Console.WriteLine("Returning teams from database cache.");
                return Ok(existingTeams); // Return cached teams
            }

            // If no teams are in the database, proceed with scraping
            var regions = new List<string> { "emea", "pacific", "americas", "china" };
            var allScrapedTeams = new List<Team>();

            foreach (var region in regions)
            {
                // Scrape teams for the current region
                var scrapedTeams = await _scraperService.ScrapeTeamsFromVlr(region);

                // Add new teams to the database and the response list
                foreach (var team in scrapedTeams)
                {
                    if (!_context.Teams.Any(t => t.Name == team.Name))
                    {
                        _context.Teams.Add(team);
                        allScrapedTeams.Add(team);
                    }
                }
            }

            // Save all changes to the database at once
            await _context.SaveChangesAsync();

            return Ok(allScrapedTeams); // Return all scraped teams
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error scraping teams from all regions: {ex.Message}");
        }
    }

}
