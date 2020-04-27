using Bricscad.ApplicationServices;
using System.Reflection;
using System;
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
      var editor = Application.DocumentManager.MdiActiveDocument.Editor;
      if (!Rhinoceros.Startup() || !Rhinoceros.LoadGrasshopperComponents())
      {
        editor.WriteMessage("\nFailed to start Rhino WIP");
        return;
      }
      var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
      editor.WriteMessage($"\nGrasshopper-BricsCAD Connection {version}");

      GrasshopperDataExtension = new GhDataExtension();
      GrasshopperDataExtension.Initialize();
      GhDrawingContext.Initialize();
      Application.Idle += OnIdle;
    }
    public void Terminate()
    {
      Application.Idle -= OnIdle;
      GhDrawingContext.Terminate();
      GrasshopperDataExtension.Terminate();
      Rhinoceros.Shutdown();
    }
    private void OnIdle(object sender, EventArgs e)
    {
      var activeDoc = Application.DocumentManager.MdiActiveDocument;
      if (activeDoc != null)
        GrasshopperDataExtension.Update(activeDoc);
      GhDrawingContext.Process();
    }
  }
}
