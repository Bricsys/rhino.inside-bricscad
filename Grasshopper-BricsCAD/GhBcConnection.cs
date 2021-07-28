using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using System.Reflection;
using System;
using System.Linq;
using Teigha.Runtime;

// This line is not mandatory, but improves loading performances
[assembly: ExtensionApplication(typeof(GH_BC.GhBcConnection))]

namespace GH_BC
{
  public class GhBcConnection : IExtensionApplication
  {
    internal static GhDataExtension GrasshopperDataExtension { get; private set; }
    internal static string DllPath => System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    static GhBcConnection()
    {
      //force load GhData extension
      using (new GrasshopperData()) { }
    }
    public void Initialize()
    {
      Editor editor = Application.DocumentManager.MdiActiveDocument.Editor;
      if (!Rhinoceros.Startup() || !Rhinoceros.LoadGrasshopperComponents())
      {
        editor.WriteMessage("\nFailed to start Rhino");
        return;
      }
      var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
      editor.WriteMessage($"\nGrasshopper-BricsCAD Connection {version}");

      GrasshopperDataExtension = new GhDataExtension();
      GrasshopperDataExtension.Initialize();
      GhDrawingContext.Initialize();
      Application.MainWindow.Focus();
      Application.Idle += OnIdle;
      Application.QuitWillStart += OnQuitWillStart;
      editor.EnteringQuiescentState += OnEnteringQuiescentState;
    }

    public void Terminate()
    {
      Document activeDoc = Application.DocumentManager.MdiActiveDocument;
      if (activeDoc != null)
        activeDoc.Editor.EnteringQuiescentState -= OnEnteringQuiescentState;
      Application.Idle -= OnIdle;
      Application.QuitWillStart -= OnQuitWillStart;
      GhDrawingContext.Terminate();
      GrasshopperDataExtension.Terminate();
      Rhinoceros.Shutdown();
    }

    private void OnIdle(object sender, EventArgs e)
    {
      Document activeDoc = Application.DocumentManager.MdiActiveDocument;
      if (activeDoc == null)
        return;
      var docExt = GrasshopperDataExtension.GrasshopperDataManager(activeDoc);
      if (docExt == null)
        return;
      if (activeDoc.Editor.IsQuiescent && docExt.HasPendingUpdates())
        activeDoc.SendStringToExecute("'_GHREGEN\n", false, true, true);
      if (Rhinoceros.Script.IsEditorVisible() && !docExt.DefinitionManager.LoadedDefinitions.Any())
        updatePreview(activeDoc, docExt);
    }

    private void OnEnteringQuiescentState(object sender, EventArgs e)
    {
      Document activeDoc = Application.DocumentManager.MdiActiveDocument;
      if (activeDoc == null)
        return;
      var docExt = GrasshopperDataExtension.GrasshopperDataManager(activeDoc);
      if (docExt != null)
      {
        updatePreview(activeDoc, docExt);
      }
    }

    private void updatePreview(Document activeDoc, GhDataManager docExt)
    {
      if (activeDoc == null || docExt == null)
        return;
      try
      {
        using (DocumentLock docLock = activeDoc.LockDocument(DocumentLockMode.ProtectedAutoWrite, "GHREGEN", "GHREGEN", false))
        {
          GrasshopperDataExtension.Update(docExt);
        }
      }
      catch (System.Exception)
      { }
    }
    private void OnQuitWillStart(object sender, EventArgs e)
    {
      Grasshopper.Plugin.GH_PluginUtil.UnloadGrasshopper();
      Grasshopper.Plugin.GH_PluginUtil.SaveSettings();
    }

  }
}
