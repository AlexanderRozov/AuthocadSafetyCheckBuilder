using Autodesk.AutoCAD.DatabaseServices;
using Demo.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Demo.Services
{
    public static class PtDetectorZoneRepository
    {
        private static PtDocumentState State => PtDocumentRegistry.Current;

        public static IReadOnlyList<PtDetectorZone> All => State.DetectorZones;

        public static IEnumerable<PtDetectorZone> GetByTable(Guid tableId) =>
            State.DetectorZones.Where(z => z.TableId == tableId);

        public static PtDetectorZone Get(Guid id) =>
            State.DetectorZones.FirstOrDefault(z => z.Id == id);

        public static PtDetectorZone Create(
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
            State.ZoneCounter++;
            var zone = new PtDetectorZone
            {
                Id = Guid.NewGuid(),
                TableId = tableId,
                Name = $"Зона {State.ZoneCounter}",
                BoundaryPoints = boundary?.ToList() ?? new List<BoundaryPoint>(),
                Radius = radius,
                GridStep = gridStep,
                HatchPattern = hatchPattern,
                GridDirection = gridDirection,
                BoundaryId = boundaryId,
                HatchId = hatchId,
                DetectorObjectIds = detectorObjectIds?.ToList() ?? new List<Guid>()
            };
            State.DetectorZones.Add(zone);
            return zone;
        }

        public static void Remove(Guid zoneId)
        {
            State.DetectorZones.RemoveAll(z => z.Id == zoneId);
        }

        public static void UpdateHatchPattern(Guid zoneId, string pattern, ObjectId newHatchId)
        {
            var zone = Get(zoneId);
            if (zone == null)
                return;

            zone.HatchPattern = pattern;
            zone.HatchId = newHatchId;
        }
    }
}
