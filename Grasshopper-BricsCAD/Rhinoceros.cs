using Bricscad.ApplicationServices;
using Rhino;
using Rhino.Runtime.InProcess;
using System;
using System.IO;
using System.Reflection;

namespace GH_BC
{
  public static class Rhinoceros
  {
    static RhinoCore _rhinoCore;
    private static bool _grasshopperLoaded = false;
    static readonly string _rhinoPath = (string) Microsoft.Win32.Registry.GetValue
    (
      @"HKEY_LOCAL_MACHINE\SOFTWARE\McNeel\Rhinoceros\7.0\Install", "Path",
      Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rhino 7", "System") + "\\"
    );
    public static Grasshopper.Plugin.GH_RhinoScriptInterface Script { get; private set; }

    static Rhinoceros()
    {
      ResolveEventHandler OnRhinoCommonResolve = null;
      AppDomain.CurrentDomain.AssemblyResolve += OnRhinoCommonResolve = (sender, args) =>
      {
        const string RhinoCommonAssemblyName = "RhinoCommon";
        var assembly_name = new AssemblyName(args.Name).Name;

        if (assembly_name != RhinoCommonAssemblyName)
          return null;

        AppDomain.CurrentDomain.AssemblyResolve -= OnRhinoCommonResolve;
        return Assembly.LoadFrom(Path.Combine(_rhinoPath, RhinoCommonAssemblyName + ".dll"));
      };

      ResolveEventHandler OnGrasshopperResolve = null;
      AppDomain.CurrentDomain.AssemblyResolve += OnGrasshopperResolve = (sender, args) =>
      {
        const string GrasshopperCommonAssemblyName = "Grasshopper";
        var assembly_name = new AssemblyName(args.Name).Name;

        if (assembly_name != GrasshopperCommonAssemblyName)
          return null;

        AppDomain.CurrentDomain.AssemblyResolve -= OnGrasshopperResolve;
        var parDir = Directory.GetParent(Directory.GetParent(_rhinoPath).FullName).FullName;
        var path = Path.Combine(parDir, "Plug-ins", "Grasshopper", GrasshopperCommonAssemblyName + ".dll");
        return Assembly.LoadFrom(path);
      };
    }
    static bool idlePending = true;
    static public bool Run()
    {
      if (idlePending)
        idlePending = _rhinoCore.DoIdle();

      var active = _rhinoCore.DoEvents();
      if (active)
        idlePending = true;

      return active;
    }
    public static bool WindowVisible
    {
      get => 0 != ((int) UI.WinAPI.GetWindowLongPtr(RhinoApp.MainWindowHandle(), -16 /*GWL_STYLE*/) & 0x10000000);
      set => UI.WinAPI.ShowWindow(RhinoApp.MainWindowHandle(), value ? 8 /*SW_SHOWNA*/ : 0 /*SW_HIDE*/);
    }
    internal static bool Startup()
    {
      if (_rhinoCore is null)
      {
        try
        {
          var scheme_name = string.Format("BricsCAD.{0}", Application.Version);
          _rhinoCore = new RhinoCore(new[] { $"/scheme={scheme_name}", "/nosplash" }, WindowStyle.Hidden, Application.MainWindow.Handle);
        }
        catch
        {
          return false;
        }

        ResetDocumentUnits(RhinoDoc.ActiveDoc, Application.DocumentManager.MdiActiveDocument);
        RhinoDoc.NewDocument += OnNewRhinoDocument;
      }
      return true;
    }
    internal static bool Shutdown()
    {
      if (_rhinoCore is object)
      {
        try
        {
          _rhinoCore.Dispose();
          _rhinoCore = null;
        }
        catch (Exception)
        {
          return false;
        }
      }
      return true;
    }
    public static bool LoadGrasshopperComponents()
    {
      if (_grasshopperLoaded)
        return true;

      var LoadGHAProc = Grasshopper.Instances.ComponentServer.GetType().GetMethod("LoadGHA", BindingFlags.NonPublic | BindingFlags.Instance);
      if (LoadGHAProc == null)
        return false;

      var bCoff = Grasshopper.Instances.Settings.GetValue("Assemblies:COFF", true);
      Grasshopper.Instances.Settings.SetValue("Assemblies:COFF", false);

      var rc = (bool) LoadGHAProc.Invoke
      (
        Grasshopper.Instances.ComponentServer,
        new object[] { new Grasshopper.Kernel.GH_ExternalFile(Assembly.GetExecutingAssembly().Location), false }
      );

      Grasshopper.Instances.Settings.SetValue("Assemblies:COFF", bCoff);

      if (rc)
        Grasshopper.Kernel.GH_ComponentServer.UpdateRibbonUI();

      var GrasshopperGuid = new Guid(0xB45A29B1, 0x4343, 0x4035, 0x98, 0x9E, 0x04, 0x4E, 0x85, 0x80, 0xD9, 0xCF);
      rc = Rhino.PlugIns.PlugIn.LoadPlugIn(GrasshopperGuid);

      Script = new Grasshopper.Plugin.GH_RhinoScriptInterface();
      Script.LoadEditor();
      rc = Script.IsEditorLoaded();

      _grasshopperLoaded = true;
      return rc;
    }
    static void OnNewRhinoDocument(object sender, DocumentEventArgs e)
    {
      if (string.IsNullOrEmpty(e.Document.TemplateFileUsed))
      {
        ResetDocumentUnits(e.Document, Application.DocumentManager.MdiActiveDocument);
      }
    }
    static void ResetDocumentUnits(RhinoDoc rhinoDoc, Document bricscadDoc = null)
    {
      bool docModified = rhinoDoc.Modified;
      if (bricscadDoc == null)
      {
        rhinoDoc.ModelUnitSystem = Rhino.UnitSystem.None;
      }
      else
      {
        var units = bricscadDoc.Database.Insunits;
        rhinoDoc.ModelUnitSystem = units.ToRhino();
        bool imperial = rhinoDoc.ModelUnitSystem == Rhino.UnitSystem.Feet || rhinoDoc.ModelUnitSystem == Rhino.UnitSystem.Inches;

        {
          var modelPlane = Rhino.Geometry.Plane.WorldXY;

          var modelGridSpacing = imperial ?
          1.0 * Rhino.RhinoMath.UnitScale(Rhino.UnitSystem.Yards, rhinoDoc.ModelUnitSystem) :
          1.0 * Rhino.RhinoMath.UnitScale(Rhino.UnitSystem.Meters, rhinoDoc.ModelUnitSystem);

          var modelSnapSpacing = imperial ?
          1 / 16.0 * Rhino.RhinoMath.UnitScale(Rhino.UnitSystem.Inches, rhinoDoc.ModelUnitSystem) :
          1.0 * Rhino.RhinoMath.UnitScale(Rhino.UnitSystem.Millimeters, rhinoDoc.ModelUnitSystem);

          var modelThickLineFrequency = imperial ? 6 : 5;

          foreach (var view in rhinoDoc.Views)
          {
            var cplane = view.MainViewport.GetConstructionPlane();

            cplane.GridSpacing = modelGridSpacing;
            cplane.SnapSpacing = modelSnapSpacing;
            cplane.ThickLineFrequency = modelThickLineFrequency;

            view.MainViewport.SetConstructionPlane(cplane);

            var min = cplane.Plane.PointAt(-cplane.GridSpacing * cplane.GridLineCount, -cplane.GridSpacing * cplane.GridLineCount, 0.0);
            var max = cplane.Plane.PointAt(+cplane.GridSpacing * cplane.GridLineCount, +cplane.GridSpacing * cplane.GridLineCount, 0.0);
            var bbox = new Rhino.Geometry.BoundingBox(min, max);

            // Zoom to grid
            view.MainViewport.ZoomBoundingBox(bbox);

            // Adjust to extens in case There is anything in the viewports like Grasshopper previews.
            view.MainViewport.ZoomExtents();
          }
        }
      }

      rhinoDoc.Modified = docModified;
    }
  }
}
