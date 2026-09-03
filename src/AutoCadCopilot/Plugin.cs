using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;

namespace AutoCadCopilot
{
    public class Plugin : IExtensionApplication
    {
        public void Initialize()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc != null)
            {
                doc.Editor.WriteMessage("\n[AutoCadCopilot] Plugin initialisé avec succès. Tapez TEST_READ_ENTITIES ou TEST_CREATE_BLOCK pour commencer.\n");
            }
        }

        public void Terminate()
        {
            // Nettoyage si nécessaire à la fermeture du plugin
        }
    }
}
