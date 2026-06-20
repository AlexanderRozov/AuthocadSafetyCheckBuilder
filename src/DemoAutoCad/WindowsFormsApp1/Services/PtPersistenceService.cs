using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Demo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace Demo.Services
{
    public static class PtPersistenceService
    {
        private const string NodKey = "DEMO_PT_PLUGIN";
        private const int ChunkSize = 255;

        public static void Save(Database db, PtDocumentState state)
        {
            if (db == null || state == null)
                return;

            var snapshot = BuildSnapshot(state);
            var json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Serialize(snapshot);

            using (var tr = db.TransactionManager.StartTransaction())
            {
                WriteJsonToNod(tr, db, json);
                tr.Commit();
            }
        }

        public static void Load(Database db, PtDocumentState state)
        {
            if (db == null || state == null)
                return;

            state.Clear();

            string json;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                json = ReadJsonFromNod(tr, db);
                tr.Commit();
            }

            if (string.IsNullOrWhiteSpace(json))
                return;

            PtDrawingSnapshot snapshot;
            try
            {
                snapshot = new JavaScriptSerializer { MaxJsonLength = int.MaxValue }
                    .Deserialize<PtDrawingSnapshot>(json);
            }
            catch
            {
                return;
            }

            if (snapshot == null || snapshot.Version <= 0)
                return;

            ApplySnapshot(db, state, snapshot);
        }

        private static PtDrawingSnapshot BuildSnapshot(PtDocumentState state)
        {
            var snapshot = new PtDrawingSnapshot
            {
                Version = 3,
                ActiveTableId = state.ActiveTableId?.ToString(),
                TableCounter = state.TableCounter,
                BlockCounter = state.BlockCounter,
                ZoneCounter = state.ZoneCounter,
                NumberCounters = new Dictionary<string, int>(state.NumberCounters)
            };

            foreach (var table in state.Tables)
            {
                snapshot.Tables.Add(new TableSnapshot
                {
                    Id = table.Id.ToString(),
                    Name = table.Name,
                    OriginX = table.Origin.X,
                    OriginY = table.Origin.Y,
                    OriginZ = table.Origin.Z,
                    TableHandle = GetHandleSafe(table.TableId),
                    BusLineHandle = GetHandleSafe(table.BusLineId),
                    DeviceCount = table.DeviceCount
                });
            }

            foreach (var obj in state.Objects)
            {
                snapshot.Objects.Add(new ObjectSnapshot
                {
                    InstanceId = obj.InstanceId.ToString(),
                    TableId = obj.TableId.ToString(),
                    CatalogId = obj.CatalogId,
                    Code = obj.Code,
                    Number = obj.Number,
                    Label = obj.Label,
                    FullName = obj.FullName,
                    FdCode = obj.FdCode,
                    JsCode = obj.JsCode,
                    ColumnIndex = obj.ColumnIndex,
                    ColumnNumber = obj.ColumnNumber,
                    FontSize = obj.FontSize,
                    BlockName = obj.BlockName,
                    ShapeId = obj.ShapeId,
                    CenterX = obj.Center.X,
                    CenterY = obj.Center.Y,
                    CenterZ = obj.Center.Z,
                    ParentObjectId = obj.ParentObjectId?.ToString(),
                    BlockGroupId = obj.BlockGroupId?.ToString(),
                    GroupHandle = GetHandleSafe(obj.GroupId),
                    EntityHandle = GetHandleSafe(obj.EntityId),
                    LabelTextHandle = GetHandleSafe(obj.LabelTextId),
                    IdTextHandle = GetHandleSafe(obj.IdTextId)
                });
            }

            foreach (var link in state.Links)
            {
                snapshot.Links.Add(new LinkSnapshot
                {
                    Id = link.Id.ToString(),
                    TableId = link.TableId.ToString(),
                    FromObjectId = link.FromObjectId.ToString(),
                    ToObjectId = link.ToObjectId.ToString(),
                    ArrowHandle = GetHandleSafe(link.ArrowId)
                });
            }

            foreach (var block in state.Blocks)
            {
                snapshot.Blocks.Add(new BlockSnapshot
                {
                    Id = block.Id.ToString(),
                    TableId = block.TableId.ToString(),
                    Name = block.Name
                });
            }

            foreach (var zone in state.DetectorZones)
            {
                snapshot.DetectorZones.Add(new DetectorZoneSnapshot
                {
                    Id = zone.Id.ToString(),
                    TableId = zone.TableId.ToString(),
                    Name = zone.Name,
                    Boundary = zone.BoundaryPoints.Select(p => new BoundaryPointSnapshot { X = p.X, Y = p.Y }).ToList(),
                    Radius = zone.Radius,
                    GridStep = zone.GridStep,
                    HatchPattern = zone.HatchPattern,
                    GridDirection = zone.GridDirection.ToString(),
                    DetectorObjectIds = zone.DetectorObjectIds.Select(id => id.ToString()).ToList(),
                    BoundaryHandle = GetHandleSafe(zone.BoundaryId),
                    HatchHandle = GetHandleSafe(zone.HatchId)
                });
            }

            foreach (var template in state.PrecreatedTemplates)
            {
                snapshot.PrecreatedTemplates.Add(new PrecreatedTemplateSnapshot
                {
                    Id = template.Id,
                    Code = template.Code,
                    Name = template.Name,
                    Description = template.Description,
                    BlockName = template.BlockName,
                    SourceFile = template.SourceFile,
                    ShapeHalfHeight = template.ShapeHalfHeight,
                    PreviewImageBase64 = template.PreviewImageBase64
                });
            }

            return snapshot;
        }

        private static long? GetHandleSafe(ObjectId id)
        {
            if (id.IsNull)
                return null;

            try
            {
                return id.Handle.Value;
            }
            catch
            {
                return null;
            }
        }

        private static void ApplySnapshot(Database db, PtDocumentState state, PtDrawingSnapshot snapshot)
        {
            state.TableCounter = snapshot.TableCounter;
            state.BlockCounter = snapshot.BlockCounter;
            state.ZoneCounter = snapshot.ZoneCounter;
            state.ActiveTableId = ParseGuid(snapshot.ActiveTableId);

            if (snapshot.NumberCounters != null)
            {
                foreach (var pair in snapshot.NumberCounters)
                    state.NumberCounters[pair.Key] = pair.Value;
            }

            foreach (var tableDto in snapshot.Tables ?? Enumerable.Empty<TableSnapshot>())
            {
                var id = ParseGuid(tableDto.Id);
                if (!id.HasValue)
                    continue;

                state.Tables.Add(new PtTableSession
                {
                    Id = id.Value,
                    Name = tableDto.Name,
                    Origin = new Point3d(tableDto.OriginX, tableDto.OriginY, tableDto.OriginZ),
                    TableId = HandleHelper.FromHandle(db, tableDto.TableHandle),
                    BusLineId = HandleHelper.FromHandle(db, tableDto.BusLineHandle),
                    DeviceCount = tableDto.DeviceCount
                });
            }

            foreach (var blockDto in snapshot.Blocks ?? Enumerable.Empty<BlockSnapshot>())
            {
                var id = ParseGuid(blockDto.Id);
                var tableId = ParseGuid(blockDto.TableId);
                if (!id.HasValue || !tableId.HasValue)
                    continue;

                state.Blocks.Add(new PtObjectBlock
                {
                    Id = id.Value,
                    TableId = tableId.Value,
                    Name = blockDto.Name
                });
            }

            foreach (var objDto in snapshot.Objects ?? Enumerable.Empty<ObjectSnapshot>())
            {
                var id = ParseGuid(objDto.InstanceId);
                var tableId = ParseGuid(objDto.TableId);
                if (!id.HasValue || !tableId.HasValue)
                    continue;

                var entityId = HandleHelper.FromHandle(db, objDto.EntityHandle);
                var labelId = HandleHelper.FromHandle(db, objDto.LabelTextHandle);
                var idTextId = HandleHelper.FromHandle(db, objDto.IdTextHandle);
                var groupId = HandleHelper.FromHandle(db, objDto.GroupHandle);

                using (var tr = db.TransactionManager.StartTransaction())
                {
                    if (!HandleHelper.IsValid(tr, entityId))
                    {
                        tr.Commit();
                        continue;
                    }

                    tr.Commit();
                }

                state.Objects.Add(new PtObject
                {
                    InstanceId = id.Value,
                    TableId = tableId.Value,
                    CatalogId = objDto.CatalogId,
                    Code = objDto.Code,
                    Number = objDto.Number,
                    Label = objDto.Label,
                    FullName = objDto.FullName,
                    FdCode = objDto.FdCode,
                    JsCode = objDto.JsCode,
                    ColumnIndex = objDto.ColumnIndex,
                    ColumnNumber = objDto.ColumnNumber,
                    FontSize = objDto.FontSize,
                    BlockName = objDto.BlockName,
                    ShapeId = objDto.ShapeId,
                    Center = new Point3d(objDto.CenterX, objDto.CenterY, objDto.CenterZ),
                    ParentObjectId = ParseGuid(objDto.ParentObjectId),
                    BlockGroupId = ParseGuid(objDto.BlockGroupId),
                    GroupId = groupId,
                    EntityId = entityId,
                    LabelTextId = labelId,
                    IdTextId = idTextId
                });
            }

            foreach (var linkDto in snapshot.Links ?? Enumerable.Empty<LinkSnapshot>())
            {
                var id = ParseGuid(linkDto.Id);
                var tableId = ParseGuid(linkDto.TableId);
                var fromId = ParseGuid(linkDto.FromObjectId);
                var toId = ParseGuid(linkDto.ToObjectId);
                if (!id.HasValue || !tableId.HasValue || !fromId.HasValue || !toId.HasValue)
                    continue;

                if (state.Objects.All(o => o.InstanceId != fromId.Value) ||
                    state.Objects.All(o => o.InstanceId != toId.Value))
                    continue;

                state.Links.Add(new PtObjectLink
                {
                    Id = id.Value,
                    TableId = tableId.Value,
                    FromObjectId = fromId.Value,
                    ToObjectId = toId.Value,
                    ArrowId = HandleHelper.FromHandle(db, linkDto.ArrowHandle)
                });
            }

            foreach (var link in state.Links)
            {
                var child = state.Objects.FirstOrDefault(o => o.InstanceId == link.ToObjectId);
                if (child != null)
                    child.ParentObjectId = link.FromObjectId;
            }

            foreach (var zoneDto in snapshot.DetectorZones ?? Enumerable.Empty<DetectorZoneSnapshot>())
            {
                var id = ParseGuid(zoneDto.Id);
                var tableId = ParseGuid(zoneDto.TableId);
                if (!id.HasValue || !tableId.HasValue)
                    continue;

                var boundaryId = HandleHelper.FromHandle(db, zoneDto.BoundaryHandle);
                var hatchId = HandleHelper.FromHandle(db, zoneDto.HatchHandle);

                var detectorIds = new List<Guid>();
                foreach (var detIdStr in zoneDto.DetectorObjectIds ?? Enumerable.Empty<string>())
                {
                    var detId = ParseGuid(detIdStr);
                    if (detId.HasValue && state.Objects.Any(o => o.InstanceId == detId.Value))
                        detectorIds.Add(detId.Value);
                }

                state.DetectorZones.Add(new PtDetectorZone
                {
                    Id = id.Value,
                    TableId = tableId.Value,
                    Name = zoneDto.Name,
                    BoundaryPoints = (zoneDto.Boundary ?? new List<BoundaryPointSnapshot>())
                        .Select(p => new BoundaryPoint(p.X, p.Y)).ToList(),
                    Radius = zoneDto.Radius,
                    GridStep = zoneDto.GridStep,
                    HatchPattern = zoneDto.HatchPattern,
                    GridDirection = ParseGridDirection(zoneDto.GridDirection),
                    DetectorObjectIds = detectorIds,
                    BoundaryId = boundaryId,
                    HatchId = hatchId
                });
            }

            foreach (var templateDto in snapshot.PrecreatedTemplates ?? Enumerable.Empty<PrecreatedTemplateSnapshot>())
            {
                if (string.IsNullOrWhiteSpace(templateDto?.Id) ||
                    string.IsNullOrWhiteSpace(templateDto.BlockName))
                    continue;

                state.PrecreatedTemplates.Add(new PrecreatedTemplate
                {
                    Id = templateDto.Id,
                    Code = templateDto.Code,
                    Name = templateDto.Name,
                    Description = templateDto.Description,
                    BlockName = templateDto.BlockName,
                    SourceFile = templateDto.SourceFile,
                    ShapeHalfHeight = templateDto.ShapeHalfHeight,
                    PreviewImageBase64 = templateDto.PreviewImageBase64
                });
            }

            RebuildNumberCounters(state);
        }

        private static DetectorGridDirection ParseGridDirection(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return DetectorGridDirection.Auto;

            return Enum.TryParse(value, out DetectorGridDirection dir)
                ? dir
                : DetectorGridDirection.Auto;
        }

        private static void RebuildNumberCounters(PtDocumentState state)
        {
            foreach (var obj in state.Objects)
            {
                if (string.IsNullOrEmpty(obj.Code) || !int.TryParse(obj.Number, out var num))
                    continue;

                if (!state.NumberCounters.ContainsKey(obj.Code) || state.NumberCounters[obj.Code] <= num)
                    state.NumberCounters[obj.Code] = num + 1;
            }
        }

        private static Guid? ParseGuid(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return Guid.TryParse(value, out var id) ? id : (Guid?)null;
        }

        private static void WriteJsonToNod(Transaction tr, Database db, string json)
        {
            var nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForWrite);
            Xrecord xrec;

            if (nod.Contains(NodKey))
            {
                xrec = (Xrecord)tr.GetObject(nod.GetAt(NodKey), OpenMode.ForWrite);
            }
            else
            {
                xrec = new Xrecord();
                nod.SetAt(NodKey, xrec);
                tr.AddNewlyCreatedDBObject(xrec, true);
            }

            var buffer = new ResultBuffer();
            for (var i = 0; i < json.Length; i += ChunkSize)
            {
                var len = Math.Min(ChunkSize, json.Length - i);
                buffer.Add(new TypedValue(1000, json.Substring(i, len)));
            }

            xrec.Data = buffer;
        }

        private static string ReadJsonFromNod(Transaction tr, Database db)
        {
            var nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
            if (!nod.Contains(NodKey))
                return null;

            var xrec = (Xrecord)tr.GetObject(nod.GetAt(NodKey), OpenMode.ForRead);
            if (xrec?.Data == null)
                return null;

            var sb = new StringBuilder();
            foreach (TypedValue tv in xrec.Data)
            {
                if (tv.TypeCode == 1000 && tv.Value is string chunk)
                    sb.Append(chunk);
            }

            return sb.Length > 0 ? sb.ToString() : null;
        }
    }
}
