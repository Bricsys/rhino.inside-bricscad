using Bricscad.ApplicationServices;
using Rhino;
using Rhino.Runtime.InProcess;
using System;
using System.Runtime.InteropServices;

namespace GH_BC
{
  public static class Rhinoceros
  {
    static RhinoCore _rhinoCore;

    [DllImport("USER32", SetLastError = true)]
    internal static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    public static bool WindowVisible
    {
      get => 0 != ((int) GetWindowLongPtr(RhinoApp.MainWindowHandle(), -16 /*GWL_STYLE*/) & 0x10000000);
      set => ShowWindow(RhinoApp.MainWindowHandle(), value ? 8 /*SW_SHOWNA*/ : 0 /*SW_HIDE*/);
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

        ResetDocumentUnits(Rhino.RhinoDoc.ActiveDoc, Application.DocumentManager.MdiActiveDocument);
        Rhino.RhinoDoc.NewDocument += OnNewRhinoDocument;        
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
