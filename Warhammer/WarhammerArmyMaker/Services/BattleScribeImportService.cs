using System.Net.Http;
using System.Text.Json;
using System.Xml.Linq;
using WarhammerArmyMaker.Models;

namespace WarhammerArmyMaker.Services;

public sealed class BattleScribeImportService
{
    private static readonly HttpClient Http = new();

    public async Task<List<ImportedUnit>> ImportFromUrlAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("A catalogue URL is required.");
        }

        var normalizedUrl = NormalizeGitHubUrl(url.Trim());
        var xml = await Http.GetStringAsync(normalizedUrl);
        return ParseCatalogue(xml);
    }

    public static string NormalizeGitHubUrl(string url)
    {
        if (url.Contains("github.com", StringComparison.OrdinalIgnoreCase) &&
            url.Contains("/blob/", StringComparison.OrdinalIgnoreCase))
        {
            return url
                .Replace("https://github.com/", "https://raw.githubusercontent.com/", StringComparison.OrdinalIgnoreCase)
                .Replace("/blob/", "/", StringComparison.OrdinalIgnoreCase);
        }

        return url;
    }

    public List<ImportedUnit> ParseCatalogue(string xml)
    {
        var document = XDocument.Parse(xml);
        var ns = document.Root?.Name.Namespace ?? XNamespace.None;

        var sharedUnits = document
            .Descendants(ns + "sharedSelectionEntries")
            .Elements(ns + "selectionEntry")
            .Where(x => string.Equals((string?)x.Attribute("type"), "unit", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(x => (string?)x.Attribute("id") ?? string.Empty, x => x);

        var entryLinks = document
            .Descendants(ns + "entryLinks")
            .Elements(ns + "entryLink")
            .Where(x => string.Equals((string?)x.Attribute("type"), "selectionEntry", StringComparison.OrdinalIgnoreCase));

        var results = new List<ImportedUnit>();

        foreach (var entryLink in entryLinks)
        {
            var targetId = (string?)entryLink.Attribute("targetId") ?? string.Empty;
            var unitName = (string?)entryLink.Attribute("name") ?? "Unknown Unit";

            sharedUnits.TryGetValue(targetId, out var sharedEntry);

            var categories = sharedEntry?
                .Descendants(ns + "categoryLink")
                .Select(x => (string?)x.Attribute("name") ?? string.Empty)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList() ?? new List<string>();

            var primaryCategory = entryLink
                .Descendants(ns + "categoryLink")
                .Where(x => string.Equals((string?)x.Attribute("primary"), "true", StringComparison.OrdinalIgnoreCase))
                .Select(x => (string?)x.Attribute("name") ?? string.Empty)
                .FirstOrDefault() ?? categories.FirstOrDefault() ?? string.Empty;

            var statsProfile = sharedEntry?
                .Descendants(ns + "profile")
                .FirstOrDefault(x => string.Equals((string?)x.Attribute("typeName"), "Unit", StringComparison.OrdinalIgnoreCase));

            var stats = statsProfile?
                .Descendants(ns + "characteristic")
                .ToDictionary(
                    x => (string?)x.Attribute("name") ?? string.Empty,
                    x => (x.Value ?? string.Empty).Trim())
                ?? new Dictionary<string, string>();

            var abilities = sharedEntry?
                .Descendants(ns + "profile")
                .Where(x => string.Equals((string?)x.Attribute("typeName"), "Abilities", StringComparison.OrdinalIgnoreCase))
                .Select(x =>
                {
                    var title = (string?)x.Attribute("name") ?? "Ability";
                    var description = string.Join(" ", x.Descendants(ns + "characteristic").Select(c => c.Value.Trim()));
                    return $"{title}: {description}".Trim();
                })
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList() ?? new List<string>();

            var profileDump = sharedEntry?
                .Elements(ns + "profiles")
                .Elements(ns + "profile")
                .Select(p => new UnitProfile
                {
                    Name = (string?)p.Attribute("name") ?? string.Empty,
                    Type = (string?)p.Attribute("typeName") ?? string.Empty,
                    Characteristics = p.Elements(ns + "characteristics")
                    .Elements(ns + "characteristic")
                    .ToDictionary(
                    c => (string?)c.Attribute("name") ?? string.Empty,
                    c => (c.Value ?? string.Empty).Trim())
                })
    .ToList() ?? new List<UnitProfile>();

            results.Add(new ImportedUnit
            {
                Id = (string?)entryLink.Attribute("id") ?? string.Empty,
                TargetId = targetId,
                Name = unitName,
                PrimaryCategory = primaryCategory,
                Categories = categories,
                Stats = stats,
                Abilities = abilities,
                RawProfilesJson = JsonSerializer.Serialize(
                    profileDump,
                    new JsonSerializerOptions { WriteIndented = true })
            });
        }

        return results
            .GroupBy(x => x.Name)
            .Select(g => g.First())
            .OrderBy(x => x.Name)
            .ToList();
    }
}
