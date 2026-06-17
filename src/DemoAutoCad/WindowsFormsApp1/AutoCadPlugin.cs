using Autodesk.AutoCAD.Runtime;

[assembly: ExtensionApplication(typeof(AutoCadPlugin.PluginEntry))]
[assembly: CommandClass(typeof(AutoCadPlugin.Commands.PtCommands))]

namespace AutoCadPlugin
{
    public class PluginEntry : IExtensionApplication
    {
        public void Initialize()
        {
            UI.ContextMenuManager.Initialize();
        }

        public void Terminate()
        {
        }
    }
}