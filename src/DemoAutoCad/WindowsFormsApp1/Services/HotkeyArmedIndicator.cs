namespace Demo.Services
{
    /// <summary>
    /// Facade so input layer does not reference WPF UI directly.
    /// </summary>
    public static class HotkeyArmedIndicator
    {
        public static System.Action<string, int> ShowHandler { get; set; }
        public static System.Action HideHandler { get; set; }

        public static void ShowIndicator(string message, int timeoutSeconds) =>
            ShowHandler?.Invoke(message, timeoutSeconds);

        public static void HideIndicator() =>
            HideHandler?.Invoke();
    }
}
