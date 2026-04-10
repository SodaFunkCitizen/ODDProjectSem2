using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using WarhammerArmyBuilder.Services;

namespace WarhammerArmyBuilder.Services
{
    public class Catalogue
    {

        private static readonly XNamespace BsNs = "http://www.battlescribe.net/schema/catalogueSchema";
        public ObservableCollection<UnitTemplate> LoadEmbeddedSample()
        {
            // A tiny subset based on the user's pasted catalogue snippet.
            const string xml = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<catalogue xmlns=""http://www.battlescribe.net/schema/catalogueSchema"" name=""Imperium - Adeptus Astartes - Black Templars"">
  <sharedSelectionEntries>
    <selectionEntry type=""unit"" name=""Chaplain Grimaldus"">
      <categoryLinks>
        <categoryLink name=""Infantry"" />
        <categoryLink name=""Faction: Black Templars"" />
      </categoryLinks>
      <profiles>
        <profile name=""Grimaldus"" typeName=""Unit"">
          <characteristics>
            <characteristic name=""M"">6""</characteristic>
            <characteristic name=""T"">4</characteristic>
            <characteristic name=""SV"">3+</characteristic>
            <characteristic name=""W"">4</characteristic>
            <characteristic name=""LD"">5+</characteristic>
            <characteristic name=""OC"">1</characteristic>
          </characteristics>
        </profile>
        <profile name=""Litanies of the Devout"" typeName=""Abilities"">
          <characteristics>
            <characteristic name=""Description"">While this unit is leading a unit and contains a Chaplain Grimaldus model, each time a model in that unit makes a melee attack, you can re-roll the Hit roll.</characteristic>
          </characteristics>
        </profile>
      </profiles>
    </selectionEntry>
  </sharedSelectionEntries>
</catalogue>";
            return ParseCatalogueXml(xml, sourceLabel: "Embedded Sample");
        }

        public async Task<ObservableCollection<UnitTemplate>> LoadFromUrlAsync(string url)
        {
            var http = new HttpClient();
            http.Timeout = TimeSpan.FromSeconds(30);

            var xml = await http.GetStringAsync(url);
            return ParseCatalogueXml(xml, sourceLabel: url);
        }

        public ObservableCollection<UnitTemplate> LoadFromFile(string path)
        {
            var xml = File.ReadAllText(path);
            return ParseCatalogueXml(xml, sourceLabel: path);
        }

        public ObservableCollection<UnitTemplate> ParseCatalogueXml(string xml, string sourceLabel)
        {
            var list = new ObservableCollection<UnitTemplate>();
            var doc = XDocument.Parse(xml);

            var root = doc.Root;
            if (root is null) return list;

            var shared = root.Element(BsNs + "sharedSelectionEntries");
            if (shared is null) return list;

            foreach (var se in shared.Elements(BsNs + "selectionEntry"))
            {
                var type = (string)se.Attribute("type") ?? "";
                if (!type.Equals("unit", StringComparison.OrdinalIgnoreCase))
                    continue;

                var name = (string)se.Attribute("name") ?? "Unknown Unit";

                // Categories -> keywords
                var cats = se.Element(BsNs + "categoryLinks")?
                    .Elements(BsNs + "categoryLink")
                    .Select(c => (string)c.Attribute("name"))
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Select(n => n.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList() ?? new List<string>();

                // Profiles
                string statline = "";
                var unitProfile = se.Element(BsNs + "profiles")?
                    .Elements(BsNs + "profile")
                    .FirstOrDefault(p => ((string)p.Attribute("typeName"))?.Equals("Unit", StringComparison.OrdinalIgnoreCase) == true);

                if (unitProfile is not null)
                {
                    var chars = unitProfile.Element(BsNs + "characteristics")?
                        .Elements(BsNs + "characteristic")
                        .Select(ch => $"{(string)ch.Attribute("name")}: {ch.Value}".Trim())
                        .ToList();

                    if (chars is { Count: > 0 })
                        statline = string.Join("  |  ", chars);
                }

                var abilities = se.Element(BsNs + "profiles")?
                    .Elements(BsNs + "profile")
                    .Where(p => ((string)p.Attribute("typeName"))?.Equals("Abilities", StringComparison.OrdinalIgnoreCase) == true)
                    .Select(p =>
                    {
                        var an = (string)p.Attribute("name") ?? "Ability";
                        var desc = p.Element(BsNs + "characteristics")?
                            .Elements(BsNs + "characteristic")
                            .FirstOrDefault(ch => ((string?)ch.Attribute("name"))?.Equals("Description", StringComparison.OrdinalIgnoreCase) == true)
                            ?.Value ?? "";
                        return string.IsNullOrWhiteSpace(desc) ? an : $"{an}: {desc}";
                    })
                    .ToList() ?? new List<string>();

                var template = new UnitTemplate
                {
                    Name = name,
                    BattlefieldRole = GuessRoleFromCategories(cats),
                    Keywords = string.Join(", ", cats),
                    Statline = statline,
                    Abilities = string.Join(Environment.NewLine + Environment.NewLine, abilities),
                    Points = 0, //user sets points when adding to army
                };

                list.Add(template);
            }

            return list;
        }

        private static string GuessRoleFromCategories(IReadOnlyList<string> cats)
        {
            if (cats.Any(c => c.Contains("Vehicle", StringComparison.OrdinalIgnoreCase))) return "Vehicle";
            if (cats.Any(c => c.Contains("Character", StringComparison.OrdinalIgnoreCase))) return "Character";
            if (cats.Any(c => c.Contains("Infantry", StringComparison.OrdinalIgnoreCase))) return "Infantry";
            return "Unit";
        }
    }
}
