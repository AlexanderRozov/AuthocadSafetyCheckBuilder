using System;
using System.Runtime.InteropServices;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace Demo.Services.Hotkeys
{
    internal static class AutoCadFocusHelper
    {
        public static void ReturnFocusToDrawing()
        {
            try
            {
                var handle = AcApp.MainWindow.Handle;
                if (handle == IntPtr.Zero)
                    return;

                SetForegroundWindow(handle);
            }
            catch
            {
                // ignore
            }
        }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}
