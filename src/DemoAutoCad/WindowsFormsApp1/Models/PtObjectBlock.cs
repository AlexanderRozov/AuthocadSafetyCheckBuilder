using System;

namespace Pt.Models
{
    public class PtObjectBlock
    {
        public Guid Id { get; set; }
        public Guid TableId { get; set; }
        public string Name { get; set; }

        public override string ToString() => Name;
    }
}
