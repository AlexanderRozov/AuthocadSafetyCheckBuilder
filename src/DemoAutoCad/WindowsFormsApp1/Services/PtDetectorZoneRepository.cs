using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using Pt.Abstractions;
using Pt.Models;
using Pt.Services.Infrastructure;

namespace Pt.Services
{
    public static class PtDetectorZoneRepository
    {
        private static IPtDetectorZoneRepository Impl => PtServiceRegistry.Current.DetectorZones;

        public static IReadOnlyList<PtDetectorZone> All => Impl.All;

        public static IEnumerable<PtDetectorZone> GetByTable(Guid tableId) => Impl.GetByTable(tableId);

        public static PtDetectorZone Get(Guid id) => Impl.Get(id);

        public static PtDetectorZone Create(
            Guid tableId,
            IList<BoundaryPoint> boundary,
            double radius,
            double gridStep,
            string hatchPattern,
            DetectorGridDirection gridDirection,
            ObjectId boundaryId,
            ObjectId hatchId,
            IList<Guid> detectorObjectIds) =>
            Impl.Create(tableId, boundary, radius, gridStep, hatchPattern, gridDirection,
                boundaryId, hatchId, detectorObjectIds);

        public static void Remove(Guid zoneId) => Impl.Remove(zoneId);

        public static void UpdateHatchPattern(Guid zoneId, string pattern, ObjectId newHatchId) =>
            Impl.UpdateHatchPattern(zoneId, pattern, newHatchId);
    }
}
