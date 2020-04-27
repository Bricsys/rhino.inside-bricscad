using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using Grasshopper.Kernel;
using GH_BC.Types;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System;
using Teigha.DatabaseServices;
using GH_BC.UI;

namespace GH_BC.Components
{
  public interface IGH_BcComponent : IGH_Component
  {
    bool NeedsToBeExpired(ICollection<Handle> modified,
                          ICollection<Handle> erased,
                          ICollection<Handle> added,
                          ICollection<string> finishedCmds);
  }
}

namespace GH_BC.Parameters
{
  public interface IGH_BcParam : IGH_Param
  {
    bool NeedsToBeExpired(ICollection<Handle> modified,
                          ICollection<Handle> erased,
                          ICollection<Handle> added,
                          ICollection<string> finishedCmds);
  }
  public interface IGH_GeometryBcParam : IGH_BcParam
  {
    void InitBy(List<FullSubentityPath> aSubents, string docName);
  }

  public abstract class GH_PersistentGeometryParam<X> :
  Grasshopper.Kernel.GH_PersistentGeometryParam<X>, IGH_GeometryBcParam
  where X : class, IGH_BcGeometricGoo
  {
    public GH_PersistentGeometryParam(GH_InstanceDescription nTag) : base(nTag) { }
    protected override Bitmap Icon => (Bitmap) Properties.Resources.ResourceManager.GetObject(GetType().Name.ToLower());
    protected virtual SubentityType SubentType { get { return SubentityType.Null; } }
    protected virtual SelectionFilter SelFilter { get { return null; } }
    protected override GH_GetterResult Prompt_Plural(ref List<X> values)
    {
      var selected = SelectionUtils.SelectEntity(SubentType, SelFilter, true);
      if (selected == null)
        return GH_GetterResult.cancel;

      var docName = Application.DocumentManager.MdiActiveDocument.Name;
      values = selected.Select(subent => CreateParameter(subent, docName)).ToList();
      return GH_GetterResult.success;
    }
    protected abstract X CreateParameter(FullSubentityPath fsp, string s);
    protected override GH_GetterResult Prompt_Singular(ref X value)
    {
      var selected = SelectionUtils.SelectEntity(SubentType, SelFilter, false);
      if (selected == null)
        return GH_GetterResult.cancel;

      var docName = Application.DocumentManager.MdiActiveDocument.Name;
      value = CreateParameter(selected[0], docName);
      return GH_GetterResult.success;
    }
    public override void AppendAdditionalMenuItems(System.Windows.Forms.ToolStripDropDown menu)
    {
      base.AppendAdditionalMenuItems(menu);
      var data = PersistentData.AllData(true);
      if (data.Count() != 0)
      {
        var dropDownButton = new System.Windows.Forms.ToolStripMenuItem();
        dropDownButton.Text = "Zoom to";
        dropDownButton.Click += ZoomTo;
        menu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
          new System.Windows.Forms.ToolStripSeparator(), dropDownButton });
      }
    }
    private void ZoomTo(object sender, EventArgs e)
    {
      object oldCmdEcho = Application.GetSystemVariable("CMDECHO");
      Application.SetSystemVariable("CMDECHO", 0);
      string command = "(command \"ZOOM\" \"OB\" \"PRO\"";
      var args = PersistentData.AllData(true).OfType<X>()
                               .Where(data => data.BcDocName == System.IO.Path.GetFileNameWithoutExtension(GhDrawingContext.LinkedDocument.Name))
                               .Aggregate(string.Empty, (s, data) => s + " \"H\" \"" + data.PersistentRef.ToString() + "\" ");       
      if (GhDrawingContext.LinkedDocument.IsActive && args.Length > 0)
        GhDrawingContext.LinkedDocument.SendStringToExecute(command + args + " \"\" \"\")(setvar \"CMDECHO\" " + oldCmdEcho + ")(princ)\n", true, true, false);
    }

    #region IGH_GeometryBcParam

    public void InitBy(List<FullSubentityPath> aSubents, string docName)
    {
      for (int i = 0; i < aSubents.Count; ++i)
      {
        var param = CreateParameter(aSubents[i], docName);
        AddVolatileData(new Grasshopper.Kernel.Data.GH_Path(0), i, param);
      }
    }
    public bool NeedsToBeExpired(ICollection<Handle> modified,
                                 ICollection<Handle> erased,
                                 ICollection<Handle> added,
                                 ICollection<string> finishedCmds)
    {
      foreach (var data in VolatileData.AllData(true).OfType<Types.IGH_BcGeometricGoo>())
      {
        if (modified.Contains(data.PersistentRef) || erased.Contains(data.PersistentRef))
          return true;
      }
      if (added.Count != 0)
        return PersistentData.AllData(true).OfType<Types.IGH_BcGeometricGoo>().Any(data => added.Contains(data.PersistentRef));
      return false;
    }
    #endregion

    protected override void OnVolatileDataCollected()
    {
      if (SourceCount == 0)
      {
        foreach (var branch in m_data.Branches)
        {
          for (int i = 0; i < branch.Count; i++)
          {
            var item = branch[i];
            if (item == null || !item.LoadGeometry(GhDrawingContext.LinkedDocument))
            {
              string element = item != null ? item.TypeName : "Element";
              AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"A referenced {element} could not be found in the BricsCAD document.");
              branch[i] = null;
            }
          }
        }
      }
      base.OnVolatileDataCollected();
    }
  }
  public class Edge : GH_PersistentGeometryParam<Types.Edge>
  {
    public Edge()
      : base(new GH_InstanceDescription("Edge", "Edge", "Represents a BricsCAD edge.", "BricsCAD", GhUI.InputGeometry))
    { }
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    public override Guid ComponentGuid => new Guid("C0D9C298-2E91-4103-8D90-208EBEDA614C");
    protected override SubentityType SubentType { get { return SubentityType.Edge; } }
    protected override Types.Edge CreateParameter(FullSubentityPath fsp, string s) => new Types.Edge(fsp, s);
  }

  public class Face : GH_PersistentGeometryParam<Types.Face>
  {
    public Face()
      : base(new GH_InstanceDescription("Face", "Face", "Represents a BricsCAD face.", "BricsCAD", GhUI.InputGeometry))
    { }
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    public override Guid ComponentGuid => new Guid("C6BA13F0-C3A9-464B-90FD-BCF568193341");
    protected override SubentityType SubentType { get { return SubentityType.Face; } }
    protected override Types.Face CreateParameter(FullSubentityPath fsp, string s) => new Types.Face(fsp, s);
  }

  public class Vertex : GH_PersistentGeometryParam<Types.Vertex>
  {
    public Vertex()
      : base(new GH_InstanceDescription("Vertex", "Vertex", "Represents a BricsCAD vertex.", "BricsCAD", GhUI.InputGeometry))
    { }
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    public override Guid ComponentGuid => new Guid("B10D4E00-5F7F-4FDC-8DAD-602D7394947F");
    protected override SubentityType SubentType { get { return SubentityType.Vertex; } }
    protected override Types.Vertex CreateParameter(FullSubentityPath fsp, string s) => new Types.Vertex(fsp, s);
  }

  public class BcCurve : GH_PersistentGeometryParam<Types.BcCurve>
  {
    public BcCurve()
      : base(new GH_InstanceDescription("Curve", "Curve", "Represents a BricsCAD curve.", "BricsCAD", GhUI.InputGeometry))
    { }
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    public override Guid ComponentGuid => new Guid("870C54B2-1F0B-45D4-A8AA-2845B3FF84F8");
    protected override Bitmap Icon => Properties.Resources.curve;
    protected override Types.BcCurve CreateParameter(FullSubentityPath fsp, string s) => new Types.BcCurve(fsp, s);
    protected override SelectionFilter SelFilter
    {
      get
      {
        var acTypValAr = new TypedValue[] { new TypedValue((int) DxfCode.Start, "CIRCLE,LINE,POLYLINE,LWPOLYLINE,PLINE,SPLINE,ARC,ELLIPSE") };
        return new SelectionFilter(acTypValAr);
      }
    }
  }

  public class BcEntity : GH_PersistentGeometryParam<Types.BcEntity>
  {
    public BcEntity()
      : base(new GH_InstanceDescription("Entity", "Entity", "Represents a BricsCAD entity.", "BricsCAD", GhUI.InputGeometry))
    { }
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    public override Guid ComponentGuid => new Guid("0E8E5153-D6CC-4556-BD92-13B949CD2A23");
    protected override Bitmap Icon => Properties.Resources.entity;
    protected override Types.BcEntity CreateParameter(FullSubentityPath fsp, string s) => new Types.BcEntity(fsp, s);
  }

  public class BcPlane : Grasshopper.Kernel.GH_PersistentGeometryParam<Grasshopper.Kernel.Types.GH_Plane>, IGH_PreviewObject
  {
    public BcPlane()
      : base(new GH_InstanceDescription("Plane", "Plane", "Represents a plane in BricsCAD.", "BricsCAD", GhUI.InputGeometry))
    { }
    public override Guid ComponentGuid => new Guid("14C3677A-7943-4CE0-9ED4-86C2C6133943");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override Bitmap Icon => Properties.Resources.plane;
    #region IGH_PreviewObject
    public bool Hidden { get; set; }
    public bool IsPreviewCapable => !VolatileData.IsEmpty;
    public Rhino.Geometry.BoundingBox ClippingBox => Preview_ComputeClippingBox();
    void IGH_PreviewObject.DrawViewportMeshes(IGH_PreviewArgs args) => Preview_DrawMeshes(args);
    void IGH_PreviewObject.DrawViewportWires(IGH_PreviewArgs args) => Preview_DrawWires(args);
    #endregion
    protected override GH_GetterResult Prompt_Plural(ref List<Grasshopper.Kernel.Types.GH_Plane> values)
    {
      Grasshopper.Kernel.Types.GH_Plane plane = null;
      while(Prompt_Singular(ref plane) == GH_GetterResult.accept)
      {
        if (values == null)
          values = new List<Grasshopper.Kernel.Types.GH_Plane>();
        values.Add(plane);
      }
      return values == null ? GH_GetterResult.cancel : GH_GetterResult.success;
    }
    protected override GH_GetterResult Prompt_Singular(ref Grasshopper.Kernel.Types.GH_Plane value)
    {
      Rhino.Geometry.Plane? plane = SelectionUtils.SelectPlane();
      if (plane == null)
        return GH_GetterResult.cancel;

      value = new Grasshopper.Kernel.Types.GH_Plane(plane.Value);
      return GH_GetterResult.success;
    }
  }

  public class BcPoint : Grasshopper.Kernel.GH_PersistentGeometryParam<Grasshopper.Kernel.Types.GH_Point>, IGH_PreviewObject
  {
    public BcPoint()
      : base(new GH_InstanceDescription("Point", "Point", "Represents a point in BricsCAD.", "BricsCAD", GhUI.InputGeometry))
    { }
    public override Guid ComponentGuid => new Guid("56A366D5-51CB-46F4-8129-E7E6CC51A669");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override Bitmap Icon => Properties.Resources.point;
    #region IGH_PreviewObject
    public bool Hidden { get; set; }
    public bool IsPreviewCapable => !VolatileData.IsEmpty;
    public Rhino.Geometry.BoundingBox ClippingBox => Preview_ComputeClippingBox();
    void IGH_PreviewObject.DrawViewportMeshes(IGH_PreviewArgs args) => Preview_DrawMeshes(args);
    void IGH_PreviewObject.DrawViewportWires(IGH_PreviewArgs args) => Preview_DrawWires(args);
    #endregion
    protected override GH_GetterResult Prompt_Plural(ref List<Grasshopper.Kernel.Types.GH_Point> values)
    {
      Grasshopper.Kernel.Types.GH_Point point = null;
      while (Prompt_Singular(ref point) == GH_GetterResult.accept)
      {
        if (values == null)
          values = new List<Grasshopper.Kernel.Types.GH_Point>();
        values.Add(point);
      }
      return values == null ? GH_GetterResult.cancel : GH_GetterResult.success;
    }
    protected override GH_GetterResult Prompt_Singular(ref Grasshopper.Kernel.Types.GH_Point value)
    {
      var editor = Application.DocumentManager.MdiActiveDocument.Editor;
      var res = editor.GetPoint("Specify point: ");
      if (res.Status != PromptStatus.OK)
        return GH_GetterResult.cancel;

      value = new Grasshopper.Kernel.Types.GH_Point(res.Value.ToRhino());
      return GH_GetterResult.success;
    }
  }
}
