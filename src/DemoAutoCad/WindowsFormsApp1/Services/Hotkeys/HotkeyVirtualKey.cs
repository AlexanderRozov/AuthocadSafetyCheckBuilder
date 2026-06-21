using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Pt.Services.Hotkeys
{
    internal static class HotkeyVirtualKey
    {
        public static string FromKeys(Keys key) =>
            key == Keys.None ? string.Empty : Normalize(key).ToString();

        public static bool TryParse(string value, out Keys key)
        {
            key = Keys.None;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            return Enum.TryParse(value.Trim(), true, out key);
        }

        public static Keys Normalize(Keys key)
        {
            if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
                return key - Keys.NumPad0 + Keys.D0;

            return key;
        }

        public static bool Matches(string configuredKey, Keys pressedKey)
        {
            if (string.IsNullOrWhiteSpace(configuredKey))
                return false;

            var normalized = Normalize(pressedKey);

            if (TryParse(configuredKey, out var expected))
                return Normalize(expected) == normalized;

            return string.Equals(normalized.ToString(), configuredKey.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsKeyDownMessage(int message) =>
            message == 0x0100 || message == 0x0104; // WM_KEYDOWN, WM_SYSKEYDOWN
    }

    internal static class HotkeyReservedKeys
    {
        private static readonly HashSet<string> Reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Escape", "Return", "Enter", "Tab", "Space"
        };

        public static bool IsReserved(string key) =>
            !string.IsNullOrWhiteSpace(key) && Reserved.Contains(key);
    }
}
