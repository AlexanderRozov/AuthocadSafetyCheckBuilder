using System;

namespace Demo.ui
{
    public class ParentItem
    {
        public Guid? Id { get; }
        public string Label { get; }

        public ParentItem(Guid? id, string label)
        {
            Id = id;
            Label = label;
        }

        public override string ToString() => Label;
    }
}
