using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using Pt.Models;

namespace Pt.Services.Drawing
{
    internal static class DeviceComposer
    {
        public static PtObject DrawDevice(
            Transaction tr,
            Database db,
            BlockTableRecord ms,
            PlaceDeviceRequest request,
            Point3d center,
            int columnIndex,
            Guid tableId)
        {
            var fontSize = request.FontSize > 0
                ? request.FontSize
                : PtLayoutConstants.DefaultTextHeight;

            var entityId = ShapeDrawer.DrawDeviceShape(tr, db, ms, request.BlockTemplate, center, request.DetectorRadius);

            var shapeHalfHeight = ShapeDrawer.GetShapeHalfHeight(request.BlockTemplate, request.DetectorRadius);
            var idOffsetY = shapeHalfHeight + PtLayoutConstants.IdTextOffsetY;
            var innerBounds = ShapeDrawer.GetShapeInnerBounds(request.BlockTemplate, request.DetectorRadius);

            var labelId = LabelTextFitter.DrawInsideLabel(
                tr, ms, center, request.Label, fontSize, innerBounds);

            var idPosition = new Point3d(center.X, center.Y - idOffsetY, 0);

            var idTextId = AnnotationDrawer.DrawMText(
                tr, ms, idPosition,
                request.FullId,
                fontSize,
                AttachmentPoint.TopCenter);

            var groupId = CreateObjectGroup(tr, db, request.Label, entityId, labelId, idTextId);

            return new PtObject
            {
                InstanceId = Guid.NewGuid(),
                TableId = tableId,
                CatalogId = request.DeviceType.Id,
                Code = request.DeviceType.Code,
                Number = request.Number,
                Label = request.Label,
                FullName = request.DeviceType.Name,
                ColumnIndex = columnIndex,
                FontSize = fontSize,
                BlockName = request.BlockTemplate?.BlockName,
                ShapeId = request.BlockTemplate?.Id,
                BlockGroupId = request.BlockGroupId,
                Center = center,
                EntityId = entityId,
                LabelTextId = labelId,
                IdTextId = idTextId,
                GroupId = groupId
            };
        }

        public static ObjectId CreateObjectGroup(
            Transaction tr,
            Database db,
            string name,
            params ObjectId[] entityIds)
        {
            var gd = (DBDictionary)tr.GetObject(db.GroupDictionaryId, OpenMode.ForWrite);
            var group = new Group($"PT_{name}", true);
            foreach (var id in entityIds)
            {
                if (!id.IsNull)
                    group.Append(id);
            }

            var key = Guid.NewGuid().ToString("N");
            gd.SetAt(key, group);
            tr.AddNewlyCreatedDBObject(group, true);
            return group.ObjectId;
        }
    }
}
