using Demo.Abstractions;
using Demo.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Demo.Services.Infrastructure
{
    public sealed class PtObjectRepositoryImpl : IPtObjectRepository
    {
        private readonly PtDocumentState _state;
        private readonly IPtBlockRepository _blocks;

        public PtObjectRepositoryImpl(PtDocumentState state, IPtBlockRepository blocks)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _blocks = blocks ?? throw new ArgumentNullException(nameof(blocks));
        }

        public IReadOnlyList<PtObject> All => _state.Objects;

        public IReadOnlyList<PtObjectLink> AllLinks => _state.Links;

        public IEnumerable<PtObject> GetByTable(Guid tableId) =>
            _state.Objects.Where(o => o.TableId == tableId).OrderBy(o => o.ColumnIndex);

        public PtObject Get(Guid id) =>
            _state.Objects.FirstOrDefault(o => o.InstanceId == id);

        public string GetNextNumber(string code)
        {
            if (!_state.NumberCounters.ContainsKey(code))
                _state.NumberCounters[code] = 1;

            var number = _state.NumberCounters[code];
            _state.NumberCounters[code] = number + 1;
            return number.ToString("D3");
        }

        public string PeekNextNumber(string code)
        {
            if (!_state.NumberCounters.ContainsKey(code))
                return "001";

            return _state.NumberCounters[code].ToString("D3");
        }

        public void Add(PtObject ptObject) => _state.Objects.Add(ptObject);

        public PtObjectLink AddLink(Guid tableId, Guid fromId, Guid toId)
        {
            RemoveIncomingLinks(toId);

            var link = new PtObjectLink
            {
                Id = Guid.NewGuid(),
                TableId = tableId,
                FromObjectId = fromId,
                ToObjectId = toId
            };
            _state.Links.Add(link);

            var child = Get(toId);
            if (child != null)
                child.ParentObjectId = fromId;

            return link;
        }

        public void RemoveIncomingLinks(Guid toObjectId)
        {
            _state.Links.RemoveAll(l => l.ToObjectId == toObjectId);
            var child = Get(toObjectId);
            if (child != null)
                child.ParentObjectId = null;
        }

        public bool WouldCreateParentCycle(Guid fromId, Guid toId)
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

        public void RemoveLinksForObject(Guid objectId) =>
            _state.Links.RemoveAll(l => l.FromObjectId == objectId || l.ToObjectId == objectId);

        public void Remove(PtObject obj)
        {
            if (obj == null)
                return;

            RemoveLinksForObject(obj.InstanceId);
            _state.Objects.Remove(obj);
        }

        public void ClearParentReference(Guid parentId)
        {
            foreach (var child in _state.Objects.Where(o => o.ParentObjectId == parentId))
                child.ParentObjectId = null;
        }

        public void SetColumnOrder(Guid tableId, IReadOnlyList<Guid> orderedIds)
        {
            for (var i = 0; i < orderedIds.Count; i++)
            {
                var obj = Get(orderedIds[i]);
                if (obj != null && obj.TableId == tableId)
                    obj.ColumnIndex = i + 1;
            }
        }

        public string GetBlockName(PtObject obj)
        {
            if (!obj.BlockGroupId.HasValue)
                return string.Empty;

            var block = _blocks.Get(obj.BlockGroupId.Value);
            return block?.Name ?? string.Empty;
        }
    }
}
