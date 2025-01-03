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

    // GET: api/Teams
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Team>>> GetTeams()
    {
        return await _context.Teams
            .Include(t => t.Players) // Include related players
            .ToListAsync();
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

    // GET: api/Teams/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Team>> GetTeam(int id)
    {
        var team = await _context.Teams
            .Include(t => t.Players)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (team == null)
        {
            return NotFound();
        }

        return team;
    }

    // POST: api/Teams
    [HttpPost]
    public async Task<ActionResult<Team>> PostTeam(Team team)
    {
        _context.Teams.Add(team);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTeam), new { id = team.Id }, team);
    }

    // PUT: api/Teams/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> PutTeam(int id, Team team)
    {
        if (id != team.Id)
        {
            return BadRequest();
        }

        _context.Entry(team).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Teams.Any(e => e.Id == id))
            {
                return NotFound();
            }
            throw;
        }

        return NoContent();
    }

    // DELETE: api/Teams/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTeam(int id)
    {
        var team = await _context.Teams.FindAsync(id);
        if (team == null)
        {
            return NotFound();
        }

        _context.Teams.Remove(team);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
