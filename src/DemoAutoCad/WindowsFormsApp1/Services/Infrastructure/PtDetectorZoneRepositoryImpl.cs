using Autodesk.AutoCAD.DatabaseServices;
using Demo.Abstractions;
using Demo.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Demo.Services.Infrastructure
{
    public sealed class PtDetectorZoneRepositoryImpl : IPtDetectorZoneRepository
    {
        private readonly PtDocumentState _state;

        public PtDetectorZoneRepositoryImpl(PtDocumentState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public IReadOnlyList<PtDetectorZone> All => _state.DetectorZones;

        public IEnumerable<PtDetectorZone> GetByTable(Guid tableId) =>
            _state.DetectorZones.Where(z => z.TableId == tableId);

        public PtDetectorZone Get(Guid id) =>
            _state.DetectorZones.FirstOrDefault(z => z.Id == id);

        public PtDetectorZone Create(
            Guid tableId,
            IList<BoundaryPoint> boundary,
            double radius,
            double gridStep,
            string hatchPattern,
            DetectorGridDirection gridDirection,
            ObjectId boundaryId,
            ObjectId hatchId,
            IList<Guid> detectorObjectIds)
        {
            _state.ZoneCounter++;
            var zone = new PtDetectorZone
            {
                Id = Guid.NewGuid(),
                TableId = tableId,
                Name = $"Зона {_state.ZoneCounter}",
                BoundaryPoints = boundary?.ToList() ?? new List<BoundaryPoint>(),
                Radius = radius,
                GridStep = gridStep,
                HatchPattern = hatchPattern,
                GridDirection = gridDirection,
                BoundaryId = boundaryId,
                HatchId = hatchId,
                DetectorObjectIds = detectorObjectIds?.ToList() ?? new List<Guid>()
            };
            _state.DetectorZones.Add(zone);
            return zone;
        }

        public void Remove(Guid zoneId) =>
            _state.DetectorZones.RemoveAll(z => z.Id == zoneId);

        public void UpdateHatchPattern(Guid zoneId, string pattern, ObjectId newHatchId)
        {
            var zone = Get(zoneId);
            if (zone == null)
                return;

            zone.HatchPattern = pattern;
            zone.HatchId = newHatchId;
        }
    }
}
