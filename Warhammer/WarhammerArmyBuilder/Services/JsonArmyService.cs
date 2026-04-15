using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace WarhammerArmyBuilder.Services
{
    public class JsonArmyService
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true
        };
        public void SaveToFile(Army army, string path)
        {
            var dto = ArmyDto.FromModel(army);
            var json = JsonSerializer.Serialize(dto, Options);
            File.WriteAllText(path, json);
        }
        public Army LoadFromFile(string path)
        {
            var json = File.ReadAllText(path);
            var dto = JsonSerializer.Deserialize<ArmyDto>(json, Options)
                ?? throw new InvalidOperationException("Invalid army JSON.");
            return dto.ToModel();
        }


        private record ArmyDto(Guid Id, string Name, string Faction, DateTime CreatedAtUtc, DateTime LastModifiedUtc, List<UnitDto> Units)
        {
            public static ArmyDto FromModel(Army army) => new(army.Id, army.Name, army.Faction, army.CreatedAtUtc, army.LastModifiedUtc,army.Units.Select(UnitDto.FromModel).ToList());

            public Army ToModel()
            {
                var army = new Army
                {
                    Id = Id,
                    Name = Name,
                    Faction = Faction,
                    CreatedAtUtc = CreatedAtUtc,
                    LastModifiedUtc = LastModifiedUtc
                };
                foreach (var u in Units)
                    army.Units.Add(u.ToModel());
                return army;
            }
        }

        private record UnitDto(string Name, string BattlefieldRole, string Keywords, int Points, DateTime CreatedAtUtc, string Notes)
        {
            public static UnitDto FromModel(Unit u)
                => new(u.Name, u.BattlefieldRole, u.Keywords, u.Points, u.CreatedAtUtc, u.Notes);

            public Unit ToModel()
                => new()
                {
                    Name = Name,
                    BattlefieldRole = BattlefieldRole,
                    Keywords = Keywords,
                    Points = Points,
                    CreatedAtUtc = CreatedAtUtc,
                    Notes = Notes
                };
        }
    }
}
namespace System.Runtime.CompilerServices
{
    // This is required to use Record type.
    // The reason that records are needed instead of using a base class is that my code requires the ability 
    // to reference and compare data to be able to edit the army and units in the future and records is a simpler way to do that online without having tons of boilerplate code.
    public class IsExternalInit { }
}