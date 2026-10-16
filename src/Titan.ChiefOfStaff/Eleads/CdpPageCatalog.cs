using System.Text.Json;

namespace Titan.ChiefOfStaff.Eleads;

public sealed class CdpPageTarget
{
    public string? Id { get; set; }
    public string? Type { get; set; }
    public string? Url { get; set; }
    public string? WebSocketDebuggerUrl { get; set; }
}

public static class CdpPageCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static IReadOnlyList<CdpPageTarget> Parse(string json)
    {
        List<CdpPageTarget>? rows;
        try
        {
            rows = JsonSerializer.Deserialize<List<CdpPageTarget>>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return [];
        }

        if (rows is null)
        {
            return [];
        }

        var pages = new List<CdpPageTarget>();
        foreach (var row in rows)
        {
            if (string.Equals(row.Type, "page", StringComparison.OrdinalIgnoreCase))
            {
                pages.Add(row);
            }
        }

        return pages;
    }

    public static async Task<IReadOnlyList<CdpPageTarget>> ListAsync()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            var json = await client.GetStringAsync(ChromeAttach.Cdp + "/json/list");
            return Parse(json);
        }
        catch
        {
            return [];
        }
    }
}
