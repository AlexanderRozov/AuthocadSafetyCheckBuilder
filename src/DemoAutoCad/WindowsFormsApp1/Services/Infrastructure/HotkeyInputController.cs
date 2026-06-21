using Demo.Abstractions;
using Demo.Models;
using Demo.Services.Hotkeys;
using System;
using System.Linq;
using System.Timers;
using System.Windows.Forms;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Demo.Services.Infrastructure
{
    public sealed class HotkeyInputController : IHotkeyInputController
    {
        private readonly IHotkeyConfigRepository _configRepository;
        private readonly IHotkeyBindableCatalog _catalog;
        private HotkeyUserConfig _config;
        private readonly System.Timers.Timer _timeoutTimer;
        private bool _attached;

        public HotkeyInputController(
            IHotkeyConfigRepository configRepository,
            IHotkeyBindableCatalog catalog)
        {
            _configRepository = configRepository;
            _catalog = catalog;
            _config = _configRepository.Load();

            _timeoutTimer = new System.Timers.Timer { AutoReset = false };
            _timeoutTimer.Elapsed += (_, __) => Disarm("таймаут");
        }

        public bool IsArmed { get; private set; }

        public event Action<HotkeySelection> SelectionApplied;
        public event Action<bool> ArmedStateChanged;

        public void ReloadConfig() => _config = _configRepository.Load();

        public void Attach()
        {
            if (_attached)
                return;

            AcApp.PreTranslateMessage += OnPreTranslateMessage;
            _attached = true;
        }

        public void Detach()
        {
            if (!_attached)
                return;

            AcApp.PreTranslateMessage -= OnPreTranslateMessage;
            _attached = false;
            Disarm(null);
        }

        /// <summary>
        /// Fallback when WPF panel still has keyboard focus.
        /// </summary>
        public bool TryProcessKey(Keys key)
        {
            if (!_config.Enabled)
                return false;

            if (!TryGetActiveDocument(out var doc))
                return false;

            if (IsAutoCadCommandActive(doc))
                return false;

            return ProcessKey(key, doc);
        }

        private void OnPreTranslateMessage(object sender, Autodesk.AutoCAD.ApplicationServices.PreTranslateMessageEventArgs e)
        {
            if (e.Handled || !_config.Enabled)
                return;

            if (!HotkeyVirtualKey.IsKeyDownMessage(e.Message.message))
                return;

            if (!TryGetActiveDocument(out var doc))
                return;

            if (IsAutoCadCommandActive(doc))
                return;

            var key = HotkeyVirtualKey.Normalize((Keys)(int)e.Message.wParam);
            if (!ProcessKey(key, doc))
                return;

            e.Handled = true;
        }

        private bool ProcessKey(Keys key, Autodesk.AutoCAD.ApplicationServices.Document doc)
        {
            if (!IsArmed)
            {
                if (!HotkeyVirtualKey.Matches(_config.ModeTriggerKey, key))
                    return false;

                Arm(doc);
                return true;
            }

            if (key == Keys.Escape)
            {
                Disarm("отмена");
                return true;
            }

            var binding = _config.Bindings?
                .FirstOrDefault(b => HotkeyVirtualKey.Matches(b.Key, key));

            if (binding == null)
                return false;

            if (!TryParseListKind(binding.ListKind, out var listKind))
                return false;

            if (_catalog.Find(listKind, binding.ItemId) == null)
                return false;

            Disarm(null);

            var selection = new HotkeySelection(listKind, binding.ItemId);
            WriteMessage(doc, $"ПТ: выбрано {DescribeSelection(selection)}");
            SelectionApplied?.Invoke(selection);
            return true;
        }

        private void Arm(Autodesk.AutoCAD.ApplicationServices.Document doc)
        {
            IsArmed = true;
            ArmedStateChanged?.Invoke(true);

            _timeoutTimer.Interval = Math.Max(2, _config.TimeoutSeconds) * 1000;
            _timeoutTimer.Stop();
            _timeoutTimer.Start();

            AutoCadFocusHelper.ReturnFocusToDrawing();

            HotkeyArmedIndicator.ShowIndicator(
                "ПТ — РЕЖИМ ВЫБОРА: нажмите клавишу элемента",
                _config.TimeoutSeconds);

            WriteMessage(doc, $"\n>>> ПТ: РЕЖИМ ВЫБОРА ({_config.TimeoutSeconds} с). Нажмите клавишу элемента или Esc. <<<");
        }

        private void Disarm(string reason)
        {
            if (!IsArmed)
                return;

            IsArmed = false;
            _timeoutTimer.Stop();
            HotkeyArmedIndicator.HideIndicator();
            ArmedStateChanged?.Invoke(false);

            if (!string.IsNullOrWhiteSpace(reason) && TryGetActiveDocument(out var doc))
                WriteMessage(doc, $"\nПТ: режим выбора завершён ({reason}).");
        }

        private static bool TryGetActiveDocument(out Autodesk.AutoCAD.ApplicationServices.Document doc)
        {
            doc = AcApp.DocumentManager.MdiActiveDocument;
            return doc != null;
        }

        private static bool IsAutoCadCommandActive(Autodesk.AutoCAD.ApplicationServices.Document doc)
        {
            try
            {
                return !string.IsNullOrEmpty(doc.CommandInProgress);
            }
            catch
            {
                return false;
            }
        }

        private string DescribeSelection(HotkeySelection selection)
        {
            var item = _catalog.Find(selection.ListKind, selection.ItemId);
            if (item == null)
                return selection.ItemId;

            return selection.ListKind == HotkeyListKind.Device
                ? $"тип «{item.DisplayLabel}»"
                : $"форма «{item.DisplayLabel}»";
        }

        private static bool TryParseListKind(string value, out HotkeyListKind listKind)
        {
            listKind = HotkeyListKind.Device;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            if (string.Equals(value, "shape", StringComparison.OrdinalIgnoreCase))
            {
                listKind = HotkeyListKind.Shape;
                return true;
            }

            if (string.Equals(value, "device", StringComparison.OrdinalIgnoreCase))
                return true;

            return Enum.TryParse(value, true, out listKind);
        }

        private static void WriteMessage(Autodesk.AutoCAD.ApplicationServices.Document doc, string text)
        {
            try
            {
                doc.Editor.WriteMessage($"\n{text}");
            }
            catch
            {
                // ignore
            }
        }
    }
}
