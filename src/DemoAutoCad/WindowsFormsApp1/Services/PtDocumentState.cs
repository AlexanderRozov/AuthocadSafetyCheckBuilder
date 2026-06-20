using Demo.Models;
using System;
using System.Collections.Generic;

namespace Demo.Services
{
    public class PtDocumentState
    {
        public bool IsHydrated { get; set; }

        public List<PtTableSession> Tables { get; } = new List<PtTableSession>();
        public List<PtObject> Objects { get; } = new List<PtObject>();
        public List<PtObjectLink> Links { get; } = new List<PtObjectLink>();
        public List<PtObjectBlock> Blocks { get; } = new List<PtObjectBlock>();
        public Dictionary<string, int> NumberCounters { get; } = new Dictionary<string, int>();

        public Guid? ActiveTableId { get; set; }
        public int TableCounter { get; set; }
        public int BlockCounter { get; set; }

        public void Clear()
        {
            Tables.Clear();
            Objects.Clear();
            Links.Clear();
            Blocks.Clear();
            NumberCounters.Clear();
            ActiveTableId = null;
            TableCounter = 0;
            BlockCounter = 0;
        }
    }
}
