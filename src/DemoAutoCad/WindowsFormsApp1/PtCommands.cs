using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using Demo.Services;
using Demo.Services.Infrastructure;
using Demo.ui;
using System;
using System.Windows;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace AutoCadPlugin.Commands
{
    public class PtCommands
    {
        private static readonly Demo.Abstractions.ILayoutManager Layout = PtServiceRegistry.LayoutManager;

        private static PtMainWindow _window;

        [CommandMethod("PLACEPT")]
        public void PlacePt()
        {
            PtInteractionScheduler.RunWhenIdle(ShowMainWindowNow);
        }

        [CommandMethod("PTPANEL")]
        public void ShowPanel()
        {
            PtInteractionScheduler.RunWhenIdle(ShowMainWindowNow);
        }

        [CommandMethod("PTPICKADD", CommandFlags.Modal | CommandFlags.Interruptible)]
        public void PickAddDevice()
        {
            ExecutePickAdd();
        }

        [CommandMethod("PTPICKNEWTABLE", CommandFlags.Modal | CommandFlags.Interruptible)]
        public void PickNewTable()
        {
            ExecutePickNewTable();
        }

        [CommandMethod("PTPICKDETECTOR", CommandFlags.Modal | CommandFlags.Interruptible)]
        public void PickDetectorZone()
        {
            ExecutePickDetector();
        }

        private static void ExecutePickAdd()
        {
            if (PtInteractionSession.Type != PtInteractionSession.InteractionType.AddDevice)
                return;

            var request = PtInteractionSession.DeviceRequest;
            PtInteractionSession.Clear();

            var doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null || request == null || request.DeviceType == null)
            {
                NotifyInteractionFinished(false);
                return;
            }

            HidePluginWindow();

            var picked = doc.Editor.GetPoint("\nЩёлкните точку вставки объекта (позиция курсора):");
            if (picked.Status != PromptStatus.OK)
            {
                NotifyInteractionFinished(false);
                return;
            }

            request.InsertionPoint = picked.Value;
            request.Number = PtObjectRepository.GetNextNumber(request.DeviceType.Code);

            try
            {
                var ptObject = Layout.AddDevice(doc.Database, request);
                doc.Editor.WriteMessage($"\nОбъект {ptObject.Label} добавлен в таблицу.");
                doc.Editor.UpdateScreen();
            }
            catch (System.Exception ex)
            {
                doc.Editor.WriteMessage($"\nОшибка: {ex.Message}");
                NotifyInteractionFinished(false);
                return;
            }

            NotifyInteractionFinished(true);
        }

        private static void ExecutePickNewTable()
        {
            if (PtInteractionSession.Type != PtInteractionSession.InteractionType.CreateTable)
                return;

            PtInteractionSession.Clear();

            var doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                NotifyInteractionFinished(false);
                return;
            }

            HidePluginWindow();

            var picked = doc.Editor.GetPoint("\nУкажите точку вставки таблицы:");
            if (picked.Status != PromptStatus.OK)
            {
                NotifyInteractionFinished(false);
                return;
            }

            try
            {
                var session = Layout.CreateTableAtPoint(doc.Database, picked.Value);
                doc.Editor.WriteMessage($"\nТаблица \"{session.Name}\" создана.");
                doc.Editor.UpdateScreen();
            }
            catch (System.Exception ex)
            {
                doc.Editor.WriteMessage($"\nОшибка: {ex.Message}");
                NotifyInteractionFinished(false);
                return;
            }

            NotifyInteractionFinished(true);
        }

        private static void ExecutePickDetector()
        {
            if (PtInteractionSession.Type != PtInteractionSession.InteractionType.PlaceDetectors)
                return;

            var tableId = PtInteractionSession.DetectorTableId;
            var radius = PtInteractionSession.DetectorRadius;
            var direction = PtInteractionSession.DetectorDirection;
            var hatchPattern = PtInteractionSession.DetectorHatchPattern;
            PtInteractionSession.Clear();

            var doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                NotifyInteractionFinished(false);
                return;
            }

            HidePluginWindow();

            var boundary = DetectorAreaPicker.PickPolyline(doc.Editor);
            if (boundary == null || boundary.Count < 3)
            {
                NotifyInteractionFinished(false);
                return;
            }

            try
            {
                var zone = DetectorPlacementService.PlaceInArea(
                    doc.Database,
                    tableId,
                    boundary,
                    radius,
                    null,
                    direction,
                    hatchPattern);

                doc.Editor.WriteMessage(
                    $"\nЗона «{zone.Name}»: расставлено {zone.DetectorObjectIds.Count} извещателей.");
                doc.Editor.UpdateScreen();
            }
            catch (System.Exception ex)
            {
                doc.Editor.WriteMessage($"\nОшибка: {ex.Message}");
                NotifyInteractionFinished(false);
                return;
            }

            NotifyInteractionFinished(true);
        }

        private static void HidePluginWindow()
        {
            if (_window == null)
                return;

            void Hide()
            {
                if (_window.Visibility == Visibility.Visible)
                    _window.Hide();
            }

            if (_window.Dispatcher.CheckAccess())
                Hide();
            else
                _window.Dispatcher.Invoke(Hide);
        }

        private static void NotifyInteractionFinished(bool success)
        {
            if (_window == null)
                return;

            _window.Dispatcher.Invoke(new Action(() =>
                _window.AfterDrawingInteraction(success)));
        }

        private static void ShowMainWindowNow()
        {
            var doc = AcApp.DocumentManager.MdiActiveDocument;
            if (doc == null)
                return;

            PtDocumentRegistry.EnsureLoaded(doc);

            if (_window == null)
            {
                _window = new PtMainWindow();
                AcApp.ShowModelessWindow(_window);
            }
            else
            {
                _window.ReloadFromDocument();
                _window.Show();
            }
        }
    }
}
