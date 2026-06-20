using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Demo.Models;
using System;
using System.Collections.Generic;

namespace Demo.Services
{
    /// <summary>
    /// Facade over focused drawing components (SRP). Preserves the existing public API.
    /// </summary>
    public static class DrawingService
    {
        public static void EnsureLayers(Transaction tr, Database db) =>
            Drawing.LayerSetup.EnsureLayers(tr, db);

        public static ObjectId DrawRectangle(Transaction tr, BlockTableRecord ms, Point3d center, double width, double height) =>
            Drawing.ShapeDrawer.DrawRectangle(tr, ms, center, width, height);

        public static ObjectId DrawSquare(Transaction tr, BlockTableRecord ms, Point3d center, double side) =>
            Drawing.ShapeDrawer.DrawSquare(tr, ms, center, side);

        public static ObjectId DrawTriangle(Transaction tr, BlockTableRecord ms, Point3d center, double width, double height) =>
            Drawing.ShapeDrawer.DrawTriangle(tr, ms, center, width, height);

        public static ObjectId DrawCircle(Transaction tr, BlockTableRecord ms, Point3d center, double radius) =>
            Drawing.ShapeDrawer.DrawCircle(tr, ms, center, radius);

        public static ObjectId DrawDiamond(Transaction tr, BlockTableRecord ms, Point3d center, double width, double height) =>
            Drawing.ShapeDrawer.DrawDiamond(tr, ms, center, width, height);

        public static double GetShapeHalfHeight(BlockTemplate template, double? customRadius = null) =>
            Drawing.ShapeDrawer.GetShapeHalfHeight(template, customRadius);

        public static double GetShapeHalfHeight(BlockTemplate template) =>
            Drawing.ShapeDrawer.GetShapeHalfHeight(template);

        public static double GetShapeHalfHeight(PtObject obj)
        {
            var template = BlockCatalog.GetById(obj?.ShapeId);
            return template != null
                ? Drawing.ShapeDrawer.GetShapeHalfHeight(template)
                : PtLayoutConstants.RectangleHeight / 2;
        }

        public static ObjectId DrawDeviceShape(
            Transaction tr,
            Database db,
            BlockTableRecord ms,
            BlockTemplate template,
            Point3d center,
            double? customRadius = null) =>
            Drawing.ShapeDrawer.DrawDeviceShape(tr, db, ms, template, center, customRadius);

        public static ObjectId InsertBlockOrRectangle(
            Transaction tr,
            Database db,
            BlockTableRecord ms,
            BlockTemplate blockTemplate,
            Point3d center) =>
            DrawDeviceShape(tr, db, ms, blockTemplate, center);

        public static ObjectId DrawMText(
            Transaction tr,
            BlockTableRecord ms,
            Point3d position,
            string text,
            double textHeight,
            AttachmentPoint attachment) =>
            Drawing.AnnotationDrawer.DrawMText(tr, ms, position, text, textHeight, attachment);

        public static ObjectId DrawBusLine(Transaction tr, BlockTableRecord ms, Point3d start, Point3d end) =>
            Drawing.AnnotationDrawer.DrawBusLine(tr, ms, start, end);

        public static void UpdateBusLine(Transaction tr, ObjectId busLineId, Point3d start, Point3d end) =>
            Drawing.AnnotationDrawer.UpdateBusLine(tr, busLineId, start, end);

        public static ObjectId DrawArrow(
            Transaction tr,
            BlockTableRecord ms,
            Point3d from,
            Point3d to,
            double fromHalfHeight = 0,
            double toHalfHeight = 0) =>
            Drawing.AnnotationDrawer.DrawArrow(tr, ms, from, to, fromHalfHeight, toHalfHeight);

        public static PtObject DrawDevice(
            Transaction tr,
            Database db,
            BlockTableRecord ms,
            PlaceDeviceRequest request,
            Point3d center,
            int columnIndex,
            Guid tableId) =>
            Drawing.DeviceComposer.DrawDevice(tr, db, ms, request, center, columnIndex, tableId);

        public static ObjectId CreateObjectGroup(
            Transaction tr,
            Database db,
            string name,
            params ObjectId[] entityIds) =>
            Drawing.DeviceComposer.CreateObjectGroup(tr, db, name, entityIds);

        public static void EraseObject(Transaction tr, PtObject obj) =>
            Drawing.EntityEraser.EraseObject(tr, obj);

        public static void EraseEntity(Transaction tr, ObjectId id) =>
            Drawing.EntityEraser.EraseEntity(tr, id);

        public static ObjectId DrawZoneBoundary(Transaction tr, BlockTableRecord ms, IList<BoundaryPoint> points) =>
            Drawing.ZoneDrawer.DrawZoneBoundary(tr, ms, points);

        public static ObjectId DrawZoneHatch(
            Transaction tr,
            Database db,
            BlockTableRecord ms,
            ObjectId boundaryId,
            string patternName) =>
            Drawing.ZoneDrawer.DrawZoneHatch(tr, db, ms, boundaryId, patternName);

        public static ObjectId RecreateZoneHatch(
            Transaction tr,
            Database db,
            BlockTableRecord ms,
            ObjectId boundaryId,
            ObjectId oldHatchId,
            string patternName) =>
            Drawing.ZoneDrawer.RecreateZoneHatch(tr, db, ms, boundaryId, oldHatchId, patternName);
    }
}
