using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Demo.Models;
using System;

namespace Demo.Abstractions
{
    public interface ILayoutManager
    {
        PtTableSession CreateTableAtPoint(Database db, Point3d origin);
        PtObject AddDevice(Database db, PlaceDeviceRequest request);
        void DeleteObject(Database db, PtObject obj);
        void SyncTableColumnOrder(Database db, Guid tableId);
        void SyncObjectToDrawing(Database db, PtObject obj);
        void CreateObjectLink(Database db, Guid tableId, Guid fromId, Guid toId);
    }
}
