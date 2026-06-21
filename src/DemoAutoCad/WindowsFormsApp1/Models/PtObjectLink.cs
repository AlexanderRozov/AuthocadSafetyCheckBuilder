using Autodesk.AutoCAD.DatabaseServices;
using System;

namespace Pt.Models
{
    public class PtObjectLink
    {
        public Guid Id { get; set; }
        public Guid TableId { get; set; }
        public Guid FromObjectId { get; set; }
        public Guid ToObjectId { get; set; }
        public ObjectId ArrowId { get; set; }
    }
}
