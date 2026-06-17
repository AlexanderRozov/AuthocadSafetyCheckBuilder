using Demo.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Demo.Services
{
    public static class PtObjectRepository
    {
        private static readonly List<PtObject> Objects = new List<PtObject>();
        private static readonly List<PtObjectLink> Links = new List<PtObjectLink>();
        private static readonly Dictionary<string, int> NumberCounters = new Dictionary<string, int>();

        public static IReadOnlyList<PtObject> All => Objects;

        public static IReadOnlyList<PtObjectLink> AllLinks => Links;

        public static IEnumerable<PtObject> GetByTable(Guid tableId) =>
            Objects.Where(o => o.TableId == tableId).OrderBy(o => o.ColumnIndex);

        public static PtObject Get(Guid id) =>
            Objects.FirstOrDefault(o => o.InstanceId == id);

        public static string GetNextNumber(string code)
        {
            if (!NumberCounters.ContainsKey(code))
                NumberCounters[code] = 1;

            var number = NumberCounters[code];
            NumberCounters[code] = number + 1;
            return number.ToString("D3");
        }

        public static string PeekNextNumber(string code)
        {
            if (!NumberCounters.ContainsKey(code))
                return "001";

            return NumberCounters[code].ToString("D3");
        }

        public static void Add(PtObject ptObject)
        {
            Objects.Add(ptObject);
        }

        public static PtObjectLink AddLink(Guid tableId, Guid fromId, Guid toId)
        {
            RemoveIncomingLinks(toId);

            var link = new PtObjectLink
            {
                Id = Guid.NewGuid(),
                TableId = tableId,
                FromObjectId = fromId,
                ToObjectId = toId
            };
            Links.Add(link);

            var child = Get(toId);
            if (child != null)
                child.ParentObjectId = fromId;

            return link;
        }

        public static void RemoveIncomingLinks(Guid toObjectId)
        {
            Links.RemoveAll(l => l.ToObjectId == toObjectId);
            var child = Get(toObjectId);
            if (child != null)
                child.ParentObjectId = null;
        }

        public static bool WouldCreateParentCycle(Guid fromId, Guid toId)
        {
            if (fromId == toId)
                return true;

            var current = Get(fromId);
            var visited = new HashSet<Guid>();
            while (current != null)
            {
                if (current.InstanceId == toId)
                    return true;

                if (!current.ParentObjectId.HasValue)
                    break;

                if (!visited.Add(current.ParentObjectId.Value))
                    break;

                current = Get(current.ParentObjectId.Value);
            }

            return false;
        }

        public static void RemoveLinksForObject(Guid objectId)
        {
            Links.RemoveAll(l => l.FromObjectId == objectId || l.ToObjectId == objectId);
        }

        public static void Remove(PtObject obj)
        {
            if (obj == null)
                return;

            RemoveLinksForObject(obj.InstanceId);
            Objects.Remove(obj);
        }

        public static void ClearParentReference(Guid parentId)
        {
            foreach (var child in Objects.Where(o => o.ParentObjectId == parentId))
                child.ParentObjectId = null;
        }

        public static void SetColumnOrder(Guid tableId, IReadOnlyList<Guid> orderedIds)
        {
            for (var i = 0; i < orderedIds.Count; i++)
            {
                var obj = Get(orderedIds[i]);
                if (obj != null && obj.TableId == tableId)
                    obj.ColumnIndex = i + 1;
            }
        }

        public static string GetBlockName(PtObject obj)
        {
            if (!obj.BlockGroupId.HasValue)
                return string.Empty;

            var block = PtBlockRepository.Get(obj.BlockGroupId.Value);
            return block?.Name ?? string.Empty;
        }
    }
}
