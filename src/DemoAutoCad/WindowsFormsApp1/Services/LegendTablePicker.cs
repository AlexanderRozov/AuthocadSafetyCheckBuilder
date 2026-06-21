using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace Pt.Services
{
    public static class LegendTablePicker
    {
        public static ObjectId? PickTable(Editor editor)
        {
            if (editor == null)
                return null;

            var options = new PromptEntityOptions("\nВыберите таблицу условных обозначений на чертеже:")
            {
                AllowNone = false
            };
            options.SetRejectMessage("\nНужна таблица AutoCAD (Table).");
            options.AddAllowedClass(typeof(Table), true);

            var result = editor.GetEntity(options);
            if (result.Status != PromptStatus.OK)
                return null;

            return result.ObjectId;
        }
    }
}
