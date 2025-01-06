# Valorant Franchising API

This API is designed to scrape and manage data for franchised Valorant teams and players from the 2025 kickoff tournaments in EMEA, Americas, Pacific, and China regions. It supports endpoints to fetch and scrape data for teams, players, and player statistics. Cached results are utilized when available to reduce redundant requests and improve performance.

## Base URL

All endpoints are prefixed with the base URL:

```
https://vlr-franchising-api-eze5a7bxc3ehh4ga.canadacentral-01.azurewebsites.net/api
```

## Endpoints

### Teams

#### Scrape Teams by Region

**GET** `/Teams/scrape/{region}`

Scrapes teams for a specific region and saves them to the database if not already present.

- **Parameters**: `{region}` (string) - one of `emea`, `pacific`, `americas`, or `china`.
- **Response**: List of teams in the specified region.
- **Caching**: If teams for the region were scraped within the last hour, cached results are returned. Otherwise, a fresh scrape is performed.

#### Scrape All Teams

**GET** `/Teams/scrape/all`

Scrapes teams for all regions (`emea`, `pacific`, `americas`, `china`) and saves them to the database.

- **Response**: List of all franchised teams across all regions.
- **Caching**: If teams were scraped within the last 6 hours, cached results are returned. Otherwise, a fresh scrape is performed.

---

### Players

#### Scrape Players by Team ID

**GET** `/Players/scrape/{teamId}`

Scrapes players for a specific team and saves them to the database if not already present.

- **Parameters**: `{teamId}` (integer) - the ID of the team.
- **Response**: List of players for the specified team.
- **Caching**: If players for the team were scraped within the last hour, cached results are returned. Otherwise, a fresh scrape is performed.

#### Scrape All Players

**GET** `/Players/scrape/all`

Scrapes players for all teams in the database and saves them if not already present.

- **Response**: List of all players across all teams.
- **Caching**: If all players were scraped within the last 6 hours, cached results are returned. Otherwise, a fresh scrape is performed.

---

### Player Statistics

#### Scrape Player Statistics by Player ID

**GET** `/PlayerStats/scrape/{playerId}`

Scrapes detailed statistics for a specific player.

- **Parameters**: `{playerId}` (integer) - the ID of the player.
- **Response**: A JSON object containing the player's stats, including social media handles, current team, past teams, total winnings, recent results, and top agents.
- **Caching**: If stats for the player were scraped within the last hour, cached results are returned. Otherwise, a fresh scrape is performed.

---

## Data Flow and Caching

1. **Teams**: Scraped by region or all regions and saved to the database.
2. **Players**: Scraped by team ID or all teams, leveraging cached teams in the database.
3. **Player Statistics**: Scraped by player ID with caching.

## Usage Instructions

1. Start by scraping teams using `/Teams/scrape/all` or `/Teams/scrape/{region}`.
2. Scrape players for specific teams using `/Players/scrape/{teamId}` or for all teams using `/Players/scrape/all`.
3. Fetch detailed statistics for a specific player using `/PlayerStats/scrape/{playerId}`.

## Rate Limits

- **Teams (All)**: Cached for 6 hours.
- **Teams (Region)**: Cached for 1 hour.
- **Players (All)**: Cached for 6 hours.
- **Players (Team)**: Cached for 1 hour.
- **Player Statistics**: Cached for 1 hour.

## How to Find Team and Player IDs

To scrape data for a specific team or player, you need the respective team or player ID:

- **Team ID**: Found in the team URL on VLR.gg. For example:
  - `https://www.vlr.gg/team/120/100-thieves` -> Team ID is `120`.
  - The `/100-thieves` part is unnecessary.
- **Player ID**: Found in the player URL on VLR.gg. For example:
  - `https://www.vlr.gg/player/604/boostio` -> Player ID is `604`.
  - The `/boostio` part is unnecessary.

## Deployment

The API is deployed on Azure and accessible at the provided base URL.

## Error Handling

- Returns appropriate HTTP status codes (e.g., 400 for bad requests, 500 for server errors).
- Logs detailed error messages to the console for debugging.

---

## Example Usage

### Fetch Teams in EMEA

```bash
curl -X GET "https://vlr-franchising-api-eze5a7bxc3ehh4ga.canadacentral-01.azurewebsites.net/api/Teams/scrape/emea"
```

### Fetch All Players

```bash
curl -X GET "https://vlr-franchising-api-eze5a7bxc3ehh4ga.canadacentral-01.azurewebsites.net/api/Players/scrape/all"
```

### Fetch Player Stats by ID

```bash
curl -X GET "https://vlr-franchising-api-eze5a7bxc3ehh4ga.canadacentral-01.azurewebsites.net/api/PlayerStats/scrape/1"
```

---

## Notes

- This API only includes franchised teams and players from the 2025 kickoff tournaments.
- Cached results reduce redundant scraping and improve performance.

## Contact

For questions or issues, contact me.
