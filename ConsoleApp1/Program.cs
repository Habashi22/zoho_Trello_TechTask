using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;

class ZohoTrelloIntegration
{
    // TODO: Fill these from your app registration / accounts
    private static string zohoClientId = "1000.JA3OPM23CFMIFLMJI3KXRHVSOSUDII";
    private static string zohoClientSecret = "5ef12d80145dcfa401220eeeac6f4fd1d76906d1c5";
    private static string zohoRefreshToken = "YOUR_ZOHO_REFRESH_TOKEN";

    private static string trelloApiKey = "YOUR_TRELLO_API_KEY";
    private static string trelloToken = "YOUR_TRELLO_TOKEN";

    private static HttpClient httpClient = new HttpClient();

    static async Task Main()
    {
        // 1. Get Zoho access token from refresh token
        var zohoAccessToken = await GetZohoAccessToken();

        // 2. Poll Deals modified recently
        var deals = await GetRecentDeals(zohoAccessToken);

        foreach (var deal in deals)
        {
            if (IsTriggerDeal(deal))
            {
                Console.WriteLine($"Creating Trello board for deal: {deal["Deal_Name"]}");

                var boardId = await CreateTrelloBoard(deal["Deal_Name"].GetString());

                await AddTrelloListsAndCards(boardId);

                await UpdateZohoDealBoardId(zohoAccessToken, deal["id"].GetString(), boardId);
            }
        }
    }

    static async Task<string> GetZohoAccessToken()
    {
        var url = $"https://accounts.zoho.com/oauth/v2/token?refresh_token={zohoRefreshToken}&client_id={zohoClientId}&client_secret={zohoClientSecret}&grant_type=refresh_token";

        var resp = await httpClient.PostAsync(url, null);
        var content = await resp.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(content);
        return doc.RootElement.GetProperty("access_token").GetString();
    }

    static async Task<JsonElement[]> GetRecentDeals(string accessToken)
    {
        var url = "https://www.zohoapis.com/crm/v2/Deals/search?criteria=(Modified_Time:after:2025-01-01T00:00:00+05:30)"; // Adjust date/time accordingly

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Zoho-oauthtoken", accessToken);

        var resp = await httpClient.GetAsync(url);
        var content = await resp.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(content);

        if (doc.RootElement.TryGetProperty("data", out var data))
        {
            return data.EnumerateArray().ToArray();
        }
        return Array.Empty<JsonElement>();
    }

    static bool IsTriggerDeal(JsonElement deal)
    {
        var stage = deal.GetProperty("Stage").GetString();
        var type = deal.GetProperty("Type").GetString();

        var boardIdExists = deal.TryGetProperty("Project_Board_ID__c", out var boardId) && !string.IsNullOrEmpty(boardId.GetString());

        return stage == "Project Kickoff" && type == "New Implementation Project" && !boardIdExists;
    }

    static async Task<string> CreateTrelloBoard(string dealName)
    {
        var boardName = Uri.EscapeDataString($"Project: {dealName}");
        var url = $"https://api.trello.com/1/boards/?name={boardName}&key={trelloApiKey}&token={trelloToken}";

        var resp = await httpClient.PostAsync(url, null);
        var content = await resp.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(content);
        return doc.RootElement.GetProperty("id").GetString();
    }

    static async Task AddTrelloListsAndCards(string boardId)
    {
        string[] lists = { "To Do", "In Progress", "Done" };
        string[] toDoCards = { "Kickoff Meeting Scheduled", "Requirements Gathering", "System Setup" };

        foreach (var listName in lists)
        {
            var url = $"https://api.trello.com/1/lists?name={Uri.EscapeDataString(listName)}&idBoard={boardId}&key={trelloApiKey}&token={trelloToken}";
            var resp = await httpClient.PostAsync(url, null);
            var content = await resp.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(content);
            var listId = doc.RootElement.GetProperty("id").GetString();

            if (listName == "To Do")
            {
                foreach (var cardName in toDoCards)
                {
                    var cardUrl = $"https://api.trello.com/1/cards?name={Uri.EscapeDataString(cardName)}&idList={listId}&key={trelloApiKey}&token={trelloToken}";
                    await httpClient.PostAsync(cardUrl, null);
                }
            }
        }
    }

    static async Task UpdateZohoDealBoardId(string accessToken, string dealId, string boardId)
    {
        var url = "https://www.zohoapis.com/crm/v2/Deals";

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Zoho-oauthtoken", accessToken);

        var body = new
        {
            data = new[]
            {
                new
                {
                    id = dealId,
                    Project_Board_ID__c = boardId
                }
            }
        };

        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var resp = await httpClient.PutAsync(url, content);
        resp.EnsureSuccessStatusCode();
    }
}
