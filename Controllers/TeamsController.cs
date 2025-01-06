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
            // Check if the scrape for this region was recently run
            if (await IsScrapeRecent($"scrape_region_{region.ToLower()}", TimeSpan.FromHours(1)))
            {
                Console.WriteLine($"Returning cached teams for region: {region}");
                return Ok(await _context.Teams.Where(t => t.Region.ToLower() == region.ToLower()).ToListAsync());
            }

            // Scrape the teams from the region
            var scrapedTeams = await _scraperService.ScrapeTeamsFromVlr(region);

            foreach (var team in scrapedTeams)
            {
                if (!_context.Teams.Any(t => t.Name == team.Name))
                {
                    _context.Teams.Add(team);
                }
            }

            await _context.SaveChangesAsync();
            await UpdateScrapeLog($"scrape_region_{region.ToLower()}");

            return Ok(scrapedTeams);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error scraping teams for region {region}: {ex.Message}");
        }
    }


    [HttpGet("scrape/all")]
    public async Task<ActionResult<IEnumerable<Team>>> ScrapeAllRegions()
    {
        try
        {
            // Check if the scrape-all operation was recently run
            if (await IsScrapeRecent("scrape_all_teams", TimeSpan.FromHours(6)))
            {
                Console.WriteLine("Returning cached teams.");
                return Ok(await _context.Teams.ToListAsync());
            }

            var regions = new List<string> { "emea", "pacific", "americas", "china" };
            var allScrapedTeams = new List<Team>();

            foreach (var region in regions)
            {
                var scrapedTeams = await _scraperService.ScrapeTeamsFromVlr(region);

                foreach (var team in scrapedTeams)
                {
                    var existingTeam = await _context.Teams.FirstOrDefaultAsync(t => t.Name == team.Name);
                    if (existingTeam != null)
                    {
                        existingTeam.Region = team.Region;
                        existingTeam.LogoUrl = team.LogoUrl;
                    }
                    else
                    {
                        _context.Teams.Add(team);
                    }

                    allScrapedTeams.Add(team);
                }
            }

            await _context.SaveChangesAsync();
            await UpdateScrapeLog("scrape_all_teams");

            return Ok(allScrapedTeams);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error scraping teams from all regions: {ex.Message}");
        }
    }


}
