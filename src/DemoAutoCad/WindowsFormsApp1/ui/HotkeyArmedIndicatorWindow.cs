using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Interop;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Demo.ui
{
    /// <summary>
    /// Small always-on-top bar — visible while hotkey pick mode is active (does not steal focus).
    /// </summary>
    public sealed class HotkeyArmedIndicatorWindow : Window
    {
        private static HotkeyArmedIndicatorWindow _instance;
        private readonly TextBlock _messageText;

        public static void ShowIndicator(string message, int timeoutSeconds)
        {
            EnsureApplication();
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                EnsureInstance();
                _instance._messageText.Text =
                    message + Environment.NewLine + $"Esc — выход · таймаут {timeoutSeconds} с";
                _instance.Reposition();
                _instance.ShowWithoutActivation();
            }));
        }

        public static void HideIndicator()
        {
            if (Application.Current == null)
                return;

            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                if (_instance == null)
                    return;

                _instance.Hide();
            }));
        }

        private static void EnsureApplication()
        {
            if (Application.Current != null)
                return;

            new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        }

        private static void EnsureInstance()
        {
            if (_instance != null)
                return;

            _instance = new HotkeyArmedIndicatorWindow();
        }

        private HotkeyArmedIndicatorWindow()
        {
            Width = 560;
            Height = 76;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;
            ShowActivated = false;
            ResizeMode = ResizeMode.NoResize;

            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(230, 126, 34)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(180, 90, 20)),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(14, 10, 14, 10)
            };

            _messageText = new TextBlock
            {
                Foreground = Brushes.White,
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center
            };

            border.Child = _messageText;
            Content = border;
        }

        private void Reposition()
        {
            try
            {
                var acadHandle = AcApp.MainWindow.Handle;
                if (acadHandle != IntPtr.Zero)
                {
                    var helper = new WindowInteropHelper(this) { Owner = acadHandle };
                    helper.EnsureHandle();
                }
            }
            catch
            {
                // ignore
            }

            Left = (SystemParameters.WorkArea.Width - Width) / 2 + SystemParameters.WorkArea.Left;
            Top = SystemParameters.WorkArea.Top + 12;
        }

        private void ShowWithoutActivation()
        {
            const int swShownaNoActivate = 4;
            var helper = new WindowInteropHelper(this);
            helper.EnsureHandle();
            Show();
            NativeMethods.ShowWindow(helper.Handle, swShownaNoActivate);
        }

        private static class NativeMethods
        {
            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        }
    }
}
