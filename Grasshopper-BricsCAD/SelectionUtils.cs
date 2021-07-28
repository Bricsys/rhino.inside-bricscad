using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using System.Collections.Generic;
using System.Linq;
using Teigha.DatabaseServices;

namespace GH_BC
{
  public static class SelectionUtils
  {
    public static List<FullSubentityPath> SelectEntity(SubentityType subentType, SelectionFilter selectionFilter, bool multiple)
    {
      var editor = Application.DocumentManager.MdiActiveDocument.Editor;
      const string selMode = "SELECTIONMODES";
      PromptSelectionOptions pso = null;
      var oldVal = Application.GetSystemVariable(selMode);
      if (DatabaseUtils.IsSubentity(subentType))
      {
        pso = new PromptSelectionOptions();
        switch (subentType)
        {
          case SubentityType.Face:
            Application.SetSystemVariable(selMode, 2);
            break;
          case SubentityType.Edge:
            Application.SetSystemVariable(selMode, 1);
            break;
          case SubentityType.Vertex:
            Application.SetSystemVariable(selMode, 3);
            break;
        }
        pso.ForceSubSelections = true;
      }
      else
        Application.SetSystemVariable(selMode, 0);

      if (!multiple)
      {
        if (pso == null)
          pso = new PromptSelectionOptions();
        pso.SingleOnly = true;
        pso.SinglePickInSpace = true;
      }
      var selFiler = selectionFilter;
      PromptSelectionResult selection = null;
      if (pso != null && selFiler != null)
        selection = editor.GetSelection(pso, selFiler);
      else if (pso != null)
        selection = editor.GetSelection(pso);
      else if (selFiler != null)
        selection = editor.GetSelection(selFiler);
      else
        selection = editor.GetSelection();

      Application.SetSystemVariable("SELECTIONMODES", oldVal);
      if (selection.Status != PromptStatus.OK)
        return null;

      List<FullSubentityPath> selectedSubents = null;
      for (int i = 0; i < selection.Value.Count; ++i)
      {
        if (DatabaseUtils.IsSubentity(subentType))
        {
          var subents = selection.Value[i].GetSubentities();
          if (subents == null)
            continue;

          foreach (var subent in subents)
          {
            if (subent.FullSubentityPath.SubentId.Type != subentType)
              continue;

            if (selectedSubents == null)
              selectedSubents = new List<FullSubentityPath>();
            selectedSubents.Add(subent.FullSubentityPath);
          }
        }
        else
        {
          selectedSubents = selection.Value.GetObjectIds().Select(objId =>
                                     new FullSubentityPath(new ObjectId[] { objId },
                                     new SubentityId(SubentityType.Null, 0))).ToList();

        }
      }
      return selectedSubents;
    }
    public static Rhino.Geometry.Plane? SelectPlane()
    {
      var editor = Application.DocumentManager.MdiActiveDocument.Editor;
      var db = Application.DocumentManager.MdiActiveDocument.Database;
      var pso = new PromptSelectionOptions
      {
        ForceSubSelections = true,
        SingleOnly = true,
        SinglePickInSpace = true,
        MessageForAdding = "Select planar face or [XY/YX/ZX]"
      };
      pso.Keywords.Add("XY");
      pso.Keywords.Add("YX");
      pso.Keywords.Add("ZX");
      Rhino.Geometry.Plane? plane = null;
      pso.KeywordInput += (sender, args) =>
      {
        var origin = db.Ucsorg;
        Teigha.Geometry.Vector3d xAxis, yAxis;
        switch (args.Input)
        {
          case "XY":
            xAxis = db.Ucsxdir;
            yAxis = db.Ucsydir;
            break;
          case "YZ":
            xAxis = db.Ucsydir;
            yAxis = db.Ucsxdir.CrossProduct(db.Ucsydir);
            break;
          case "ZX":
            xAxis = db.Ucsxdir.CrossProduct(db.Ucsydir);
            yAxis = db.Ucsxdir;
            break;
          default: return;
        }
        plane = new Rhino.Geometry.Plane(origin.ToRhino(), xAxis.ToRhino(), yAxis.ToRhino());
      };
      const string selMode = "SELECTIONMODES";
      var oldVal = Application.GetSystemVariable(selMode);
      Application.SetSystemVariable(selMode, 2);
      while (true)
      {
        var selection = editor.GetSelection(pso);
        if (selection.Status == PromptStatus.OK)
        {
          var fsp = selection.Value[0].GetSubentities()[0].FullSubentityPath;
          using (var face = new Teigha.BoundaryRepresentation.Face(fsp))
          {
            if (face.Surface is Teigha.Geometry.ExternalBoundedSurface tdExtSur)
            {
              if (tdExtSur.BaseSurface is Teigha.Geometry.Plane tdPlane)
              {
                plane = tdPlane.ToRhino();
                break;
              }
            }
            else
            {
              editor.WriteMessage("\nFace must be planar");
            }
          }
        }
        else
          break;
      }

      Application.SetSystemVariable(selMode, oldVal);
      return plane;
    }
  }
}
