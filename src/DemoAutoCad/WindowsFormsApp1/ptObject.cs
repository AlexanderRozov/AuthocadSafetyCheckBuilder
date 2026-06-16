using Autodesk.AutoCAD.DatabaseServices;
using System;

namespace Demo
{
    public class ptObject
    {
        public Guid InstanceId { get; set; }

        public string CatalogId { get; set; }

        public string Number { get; set; }

        public string Type { get; set; }

        public ObjectId PolylineId { get; set; }

        public ObjectId TextId { get; set; }
    }
}
