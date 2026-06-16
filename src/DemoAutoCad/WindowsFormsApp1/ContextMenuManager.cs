using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Windows;

namespace AutoCadPlugin.UI
{
    public static class ContextMenuManager
    {
        private static ContextMenuExtension _menu;

        public static void Initialize()
        {
            _menu = new ContextMenuExtension();

            var item = new MenuItem("Поставить объект ПТ");
            item.Click += (sender, e) =>
            {
                Autodesk.AutoCAD.ApplicationServices
                    .Application
                    .DocumentManager
                    .MdiActiveDocument
                    .SendStringToExecute(
                        "PLACEPT ",
                        true,
                        false,
                        false);
            };

            _menu.MenuItems.Add(item);

            Application.AddDefaultContextMenuExtension(_menu);
        }
    }
}