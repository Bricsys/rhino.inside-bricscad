using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Teigha.DatabaseServices;
using Teigha.Runtime;
using _WF = System.Windows.Forms;
using GH_BC.Parameters;

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
      if (Rhinoceros.Script.IsEditorVisible())
        Rhinoceros.Script.HideEditor();
      else
      {
        if (System.Convert.ToInt16(Application.GetSystemVariable("DWGTITLED")) == 0)
        {
          _WF.MessageBox.Show("Bricscad drawing must be saved before using Grasshopper");
          return;
        }
        Rhinoceros.Script.ShowEditor();
        GhDrawingContext.RelinkToDoc(Application.DocumentManager.MdiActiveDocument);
      }
      UI.GhUI.CustomizeUI();
    }

    [CommandMethod("ToGrasshopper")]
    public static void ToGrasshopper()
    {
      if (GhDrawingContext.LinkedDocument == null &&
          Application.DocumentManager.MdiActiveDocument != GhDrawingContext.LinkedDocument)
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
          bool isCurve = selectedObjects.All(fsp => DatabaseUtils.IsCurve(fsp.InsertId()));
          if (isCurve)
            monitor = new BcCurve();
          else
            monitor = new BcEntity();
          break;
        case SubentityType.Face:
          monitor = new Parameters.Face(); break;
        case SubentityType.Edge:
          monitor = new Parameters.Edge(); break;
        case SubentityType.Vertex:
          monitor = new Parameters.Vertex(); break;
      }
      if (monitor == null)
        return;

      monitor.InitBy(selectedObjects, GhDrawingContext.LinkedDocument.Name);
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

    [CommandMethod("AttachGhData")]
    public static void AttachGhData()
    {
      var editor = Application.DocumentManager.MdiActiveDocument.Editor;
      var database = editor.Document.Database;
      var selection = editor.GetSelection();
      if (selection.Status != PromptStatus.OK)
        return;

      string ghDef = null;
      var srchPath = Application.GetSystemVariable("SRCHPATH") as string;
      var dwgPath = Path.GetDirectoryName(Application.DocumentManager.MdiActiveDocument.Name);
      while (true)
      {
        if (!GH_IO.Serialization.GH_Archive.OpenFileDialog("Select grasshopper defintion", ref ghDef, null)
          || string.IsNullOrEmpty(ghDef))
          return;
        var ghDir = Path.GetDirectoryName(ghDef);
        if (!srchPath.Contains(ghDir) && dwgPath != ghDir)
        {
          _WF.MessageBox.Show("Directory should be equal to the current DWG location or be contained in SRCHPATH");
          continue;
        }
        break;
      }
      if (string.IsNullOrEmpty(ghDef))
        return;

      using (var transaction = database.TransactionManager.StartTransaction())
      {
        for (int i = 0; i < selection.Value.Count; ++i)
        {
          var ghData = new GrasshopperData(Path.GetFileName(ghDef))
          {
            IsVisible = true
          };

          var entity = transaction.GetObject(selection.Value[i].ObjectId, OpenMode.ForWrite) as Entity;
          if (GrasshopperData.AttachGrasshopperData(entity, ghData))
          {
            var docExt = GhBcConnection.GrasshopperDataExtension.GrasshopperDataManager(Application.DocumentManager.MdiActiveDocument, true);
            docExt.AddGrasshopperData(ghData);
          }
          ghData.Dispose();
        }
        transaction.Commit();
      }
    }
    [CommandMethod("ClearGhData")]
    public static void ClearGhData()
    {
      var editor = Application.DocumentManager.MdiActiveDocument.Editor;
      var database = editor.Document.Database;
      var selection = editor.GetSelection();
      if (selection.Status != PromptStatus.OK)
        return;

      using (var transaction = database.TransactionManager.StartTransaction())
      {
        for (int i = 0; i < selection.Value.Count; ++i)
        {
          var entity = transaction.GetObject(selection.Value[i].ObjectId, OpenMode.ForWrite) as Entity;
          GrasshopperData.RemoveGrasshopperData(entity);
          entity?.RecordGraphicsModified(true);
        }
        transaction.Commit();
      }
    }
    [CommandMethod("BakeGhData")]
    public static void BakeGhData()
    {
      var editor = Application.DocumentManager.MdiActiveDocument.Editor;
      var selection = editor.GetSelection();
      if (selection.Status != PromptStatus.OK)
        return;

      var database = editor.Document.Database;
      var ghDataToBake = new List<ObjectId>();
      using (var transaction = database.TransactionManager.StartTransaction())
      {
        for (int i = 0; i < selection.Value.Count; ++i)
        {
          var entity = transaction.GetObject(selection.Value[i].ObjectId, OpenMode.ForRead) as Entity;
          var id = GrasshopperData.GetGrasshopperData(entity);
          if (!id.IsNull)
            ghDataToBake.Add(id);
        }
        transaction.Commit();
      }

      if (ghDataToBake.Count == 0)
        return;

      var bakeProperties = new UI.BakeDialog();
      if (bakeProperties.ShowDialog() != _WF.DialogResult.OK)
        return;

      var docExt = GhBcConnection.GrasshopperDataExtension.GrasshopperDataManager(Application.DocumentManager.MdiActiveDocument, true);
      docExt?.Bake(ghDataToBake, bakeProperties);
    }
    [CommandMethod("GhDefinitions")]
    public static void GhDefinitions()
    {
      var dlg = new UI.GhDefinitionDialog();
      dlg.ShowDialog();
    }
  }
}
