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
        private Guid _id = Guid.NewGuid();
        private DateTime _createdAtUtc = DateTime.UtcNow;
        private DateTime _lastModifiedUtc = DateTime.UtcNow;
        public Guid Id
        {
            /*Why Guid well see if a user created a list on 
             one verison of the app then downloaded it to a
            different computer the id value would reset meaning 
            they would have two identical army ids which can 
            create confusion on the backend also it a cool feature */
            get => _id;
            set => _id = value;

        }

        public string Name { get; set; }
        public string Faction { get; set; }
        public DateTime CreatedAtUtc { get=> _createdAtUtc; set => _createdAtUtc = value; }
        public DateTime LastModifiedUtc { get => _lastModifiedUtc; set =>_lastModifiedUtc = value; }
        public List<Unit> Units { get; set; } = new List<Unit>();
    }


    public class UnitBase
    {
        private DateTime _createdAtUtc = DateTime.UtcNow;
        public string Id { get; set; }
        public string Name { get; set; }
        public string BattlefieldRole { get; set; }
        public string Keywords { get; set; }
        public int Points { get; set; }
        public DateTime CreatedAtUtc { get => _createdAtUtc; set => _createdAtUtc = value; }
        public string Notes { get; set; }
    }

    public class Unit:UnitBase
    {
        private Guid _id = Guid.NewGuid();
        private string _notes = "";

        public Guid Id
        {
            get => _id;
            set => _id = value;
        }

        public string Notes
        {
            get => _notes;
            set => _notes = value;
        }
    }

    public class UnitTemplate:UnitBase
    {
        private string _source = "Unknown Source";
        public string Statline { get; set; }
        public string Abilities { get; set; }
        public string source { get => _source; set => _source = value; }
    }
}
