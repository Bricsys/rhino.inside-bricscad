using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Teigha.DatabaseServices;
using Teigha.Runtime;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(GH_BC.Commands))]

namespace GH_BC
{
  // This class is instantiated by BricsCAD for each document when
  // a command is called by the user the first time in the context
  // of a given document
  public class Commands
  {
    [CommandMethod("Rhino")]
    public static void StartRhino()
    {
      Rhinoceros.WindowVisible = !Rhinoceros.WindowVisible;
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
