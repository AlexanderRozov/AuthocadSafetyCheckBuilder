using Autodesk.AutoCAD.ApplicationServices;
using System;

namespace Demo.Services
{
    public static class PtInteractionScheduler
    {
        private static string _pendingCommand;

        public static void RunCommandWhenIdle(string commandName)
        {
            if (string.IsNullOrWhiteSpace(commandName))
                return;

            _pendingCommand = commandName;
            Application.Idle -= OnIdleRunCommand;
            Application.Idle += OnIdleRunCommand;
        }

        public static void RunWhenIdle(Action action)
        {
            if (action == null)
                return;

            void Handler(object sender, EventArgs e)
            {
                Application.Idle -= Handler;
                action();
            }

            Application.Idle += Handler;
        }

        private static void OnIdleRunCommand(object sender, EventArgs e)
        {
            Application.Idle -= OnIdleRunCommand;

            var command = _pendingCommand;
            _pendingCommand = null;
            if (string.IsNullOrWhiteSpace(command))
                return;

            var doc = Application.DocumentManager.MdiActiveDocument;
            doc?.SendStringToExecute(command + " ", true, false, false);
        }
    }
}
