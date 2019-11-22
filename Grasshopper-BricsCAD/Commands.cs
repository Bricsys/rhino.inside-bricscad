using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Teigha.DatabaseServices;
using Teigha.Runtime;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(GH_BC.TestCommands))]

namespace GH_BC
{
  // This class is instantiated by BricsCAD for each document when
  // a command is called by the user the first time in the context
  // of a given document
  public class TestCommands
  {
    public static void ResetDocumentUnits(Rhino.RhinoDoc rhinoDoc, Document bricscadDoc = null)
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

    [CommandMethod("Rhino")]
    public static void StartRhino()
    {
      var MainWindow = Rhino.UI.RhinoEtoApp.MainWindow;
      if (MainWindow.Visible)
      {
        MainWindow.Visible = false;
        var newDoc = Rhino.RhinoDoc.Create(null);
        ResetDocumentUnits(newDoc, Application.DocumentManager.MdiActiveDocument);
      }
      else
      {
        MainWindow.Visible = true;
        try { MainWindow.WindowState = Eto.Forms.WindowState.Normal; }
        catch { }
        ResetDocumentUnits(Rhino.RhinoDoc.ActiveDoc, Application.DocumentManager.MdiActiveDocument);
      }
    }
    [CommandMethod("Grasshopper")]
    public static void StartGrasshopper()
    {
      PlugIn.LoadGrasshopperComponents();
      var ghDocEditor = Grasshopper.Instances.DocumentEditor;
      if (ghDocEditor == null || !ghDocEditor.Visible)
      {
        if (System.Convert.ToInt16(Application.GetSystemVariable("DWGTITLED")) == 0)
        {
          System.Windows.Forms.MessageBox.Show("Bricscad drawing must be saved before using Grasshopper");
          return;
        }
      }
      Rhino.RhinoApp.RunScript("!_-Grasshopper _W _T ENTER", false);
      if (Grasshopper.Instances.DocumentEditor.Visible)
        PlugIn.RelinkToDoc(Application.DocumentManager.MdiActiveDocument);
      GhUI.CustomizeUI();
    }

    [CommandMethod("ToGrasshopper")]
    public static void ToGrasshopper()
    {
      if (PlugIn.LinkedDocument == null && Application.DocumentManager.MdiActiveDocument != PlugIn.LinkedDocument)
        return;

      var editor = Application.DocumentManager.MdiActiveDocument.Editor;
      var ghDoc = Grasshopper.Instances.ActiveCanvas.Document;
      if (ghDoc == null)
      {
        editor.WriteMessage("No active gh document\n");
        return;
      }

      var pso = new PromptSelectionOptions();
      pso.AllowSubSelections = true;
      var selection = editor.GetSelection(pso);
      if (selection.Status != PromptStatus.OK)
        return;

      var selectedObjects = new List<FullSubentityPath>();
      for (int i = 0; i < selection.Value.Count; ++i)
      {
        var subents = selection.Value[i].GetSubentities();
        if (subents != null)
        {
          foreach (var subent in subents)
            selectedObjects.Add(subent.FullSubentityPath);
        }
        else
          selectedObjects.Add(new FullSubentityPath(new ObjectId[] { selection.Value[i].ObjectId },
                                                    new SubentityId(SubentityType.Null, 0)));
      }
      if (selectedObjects.Count == 0)
        return;

      var type = selectedObjects[0].SubentId.Type;
      bool theSameType = selectedObjects.All(fsp => fsp.SubentId.Type == type);
      if (!theSameType)
      {
        editor.WriteMessage("Mixed selection set is not allowed\n");
        return;
      }

      IGH_GeometryBcParam monitor = null;
      switch (type)
      {
        case SubentityType.Null:
          bool isCurve = selectedObjects.All(fsp => DatabaseUtils.isCurve(fsp.InsertId()));
          if (isCurve)
            monitor = new BcCurve();
          else
            monitor = new BcEntity();
          break;
        case SubentityType.Face:
          monitor = new Face(); break;
        case SubentityType.Edge:
          monitor = new Edge(); break;
        case SubentityType.Vertex:
          monitor = new Vertex(); break;
      }
      if (monitor == null)
        return;

      monitor.InitBy(selectedObjects, PlugIn.LinkedDocument.Name);
      var ghDocObj = (Grasshopper.Kernel.GH_DocumentObject) monitor;
      if (ghDocObj != null)
      {
        var bounds = Grasshopper.Instances.ActiveCanvas.Viewport.VisibleRegion;
        ghDocObj.CreateAttributes();
        ghDocObj.Attributes.Selected = true;
        ghDocObj.Attributes.Pivot = new PointF(bounds.Left + ghDocObj.Attributes.Bounds.Width,
                                               bounds.Top + ghDocObj.Attributes.Bounds.Height);
        ghDoc.AddObject(ghDocObj, true);
      }
    }
  }
}
