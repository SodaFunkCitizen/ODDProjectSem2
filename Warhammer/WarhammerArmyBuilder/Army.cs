using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WarhammerArmyBuilder
{ 
    public class Army
    {
        private string _id = "0";
        private string _name = "New Army";
        private string _faction = "Unknown Faction";
        private DateTime _createdAtUtc = DateTime.UtcNow;
        private DateTime _lastModifiedUtc = DateTime.UtcNow;
        public string Id
        {
            get;
            set;

        }

        public string Name { get; set; }
        public string Faction { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime LastModifiedUtc { get; set; }
        public List<ArmyUnit> Units { get; set; } = new List<ArmyUnit>();
    }


    public class ArmyUnit
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string BattlefieldRole { get; set; }
        public string Keywords { get; set; }
        public int Points { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public string Notes { get; set; }
    }
}
