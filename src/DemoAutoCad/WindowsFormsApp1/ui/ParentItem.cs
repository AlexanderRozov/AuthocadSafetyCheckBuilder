using System;

namespace Pt.ui
{
    public class ParentItem
    {
        public Guid? Id { get; set; }
        public string Label { get; set; }

        public ParentItem(Guid? id, string label)
        {
            Id = id;
            Label = label ?? string.Empty;
        }

        public override string ToString() => Label;
    }
}
