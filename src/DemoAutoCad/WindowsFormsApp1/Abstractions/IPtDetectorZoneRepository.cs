using Autodesk.AutoCAD.DatabaseServices;
using Demo.Models;
using System;
using System.Collections.Generic;

namespace Demo.Abstractions
{
    public interface IPtDetectorZoneRepository
    {
        IReadOnlyList<PtDetectorZone> All { get; }
        IEnumerable<PtDetectorZone> GetByTable(Guid tableId);
        PtDetectorZone Get(Guid id);
        PtDetectorZone Create(
            Guid tableId,
            IList<BoundaryPoint> boundary,
            double radius,
            double gridStep,
            string hatchPattern,
            DetectorGridDirection gridDirection,
            ObjectId boundaryId,
            ObjectId hatchId,
            IList<Guid> detectorObjectIds);
        void Remove(Guid zoneId);
        void UpdateHatchPattern(Guid zoneId, string pattern, ObjectId newHatchId);
    }
}
