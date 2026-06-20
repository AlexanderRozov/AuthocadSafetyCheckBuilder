using Demo.Abstractions;
using Demo.Models;
using System;

namespace Demo.Services.Infrastructure
{
    public sealed class PtLayoutManagerImpl : ILayoutManager
    {
        public PtTableSession CreateTableAtPoint(
            Autodesk.AutoCAD.DatabaseServices.Database db,
            Autodesk.AutoCAD.Geometry.Point3d origin) =>
            PtLayoutManager.CreateTableAtPoint(db, origin);

        public PtObject AddDevice(
            Autodesk.AutoCAD.DatabaseServices.Database db,
            PlaceDeviceRequest request) =>
            PtLayoutManager.AddDevice(db, request);

        public void DeleteObject(
            Autodesk.AutoCAD.DatabaseServices.Database db,
            PtObject obj) =>
            PtLayoutManager.DeleteObject(db, obj);

        public void SyncTableColumnOrder(
            Autodesk.AutoCAD.DatabaseServices.Database db,
            Guid tableId) =>
            PtLayoutManager.SyncTableColumnOrder(db, tableId);

        public void SyncObjectToDrawing(
            Autodesk.AutoCAD.DatabaseServices.Database db,
            PtObject obj) =>
            PtLayoutManager.SyncObjectToDrawing(db, obj);

        public void CreateObjectLink(
            Autodesk.AutoCAD.DatabaseServices.Database db,
            Guid tableId,
            Guid fromId,
            Guid toId) =>
            PtLayoutManager.CreateObjectLink(db, tableId, fromId, toId);
    }
}
