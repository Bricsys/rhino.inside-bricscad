using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel;
using Rhino.FileIO;
using Rhino.Geometry;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System;
using _OdDb = Teigha.DatabaseServices;
using GH_BC.UI;

namespace GH_BC.Components
{  
  public class BakeComponent : GH_Component
  {
    protected bool _needBake = false;
    protected string _layer = string.Empty;
    protected string _material = string.Empty;
    protected Teigha.Colors.Color _color = null;
    protected BakeComponent(string name, string nickname, string description, string category, string subCategory)
      : base(name, nickname, description, category, subCategory)
    { }
    public BakeComponent()
      : base("Bake Geometry", "BG", "Bake the Grasshopper geometry into the current BricsCAD drawing, while disregarding the BIM data attached to it. The output of Bake Geometry is a reference to the baked building element without BIM data.", "BricsCAD", GhUI.BuildingElements)
    {}
    public override Guid ComponentGuid => new Guid("8C42D2D7-16C5-4AFC-B6E0-6FC2696A1038");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    public override bool IsPreviewCapable { get { return false; } }
    public override bool IsBakeCapable { get { return false; } }
    protected override System.Drawing.Bitmap Icon => Properties.Resources.bake;
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddGeometryParameter("Geometry", "G", "Geometry to bake into BricsCAD", GH_ParamAccess.tree);
      pManager[pManager.AddTextParameter("Material", "M", "Material to assign to the Geometry in BricsCAD (Overrides the Bake Dialog Material)", GH_ParamAccess.item)].Optional = true;
    }
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddParameter(new Parameters.BcEntity(), "BuildingElement", "BE", "Building element.", GH_ParamAccess.list);
    }
    protected override void SolveInstance(IGH_DataAccess DA)
    {
      if (!_needBake || !GhDrawingContext.LinkedDocument.IsActive)
        return;
      _needBake = false;

      string material = string.Empty;
      if (DA.GetData("Material", ref material))
        _material = material;

      var geometry = new GH_Structure<IGH_GeometricGoo>();
      if (!DA.GetDataTree("Geometry", out geometry))
        return;

      var objIds = BakeGhGeometry(geometry.AllData(true));
      var res = new List<Types.BcEntity>();
      foreach (_OdDb.ObjectId objId in objIds)
        res.Add(new Types.BcEntity(new _OdDb.FullSubentityPath(new _OdDb.ObjectId[] { objId }, new _OdDb.SubentityId()), GhDrawingContext.LinkedDocument.Name));
      DA.SetDataList("BuildingElement", res);
    }
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      Menu_AppendItem(menu, "Bake into BricsCAD", BakeItemCall);
    }
    private void BakeItemCall(object sender, EventArgs e)
    {
      var bakeProperties = new BakeDialog();
      if (bakeProperties.ShowDialog(Grasshopper.Instances.DocumentEditor) == DialogResult.OK)
      {
        _color = bakeProperties.Color;
        _layer = bakeProperties.Layer;
        _material = bakeProperties.Material;
        _needBake = true;

        GhDrawingContext.LinkedDocument?.Database?.StartUndoRecord();
        ExpireSolution(true);
      }
    }
    public _OdDb.ObjectIdCollection BakeGhGeometry(IGH_StructureEnumerator se)
    {
      using (var tmpFile = new File3dm())
      {
        foreach (var paramValue in se)
          AddGeometry(tmpFile, paramValue);
        return BakeGhGeometry(tmpFile);
      }
    }
    protected _OdDb.ObjectIdCollection BakeGhGeometry(File3dm tmpFile)
    {
      _OdDb.ObjectIdCollection dbObjects = null;
      if (tmpFile.Objects.Count != 0)
      {
        string tmpPath = Path.Combine(Path.GetTempPath(), "BricsCAD", "fromrhino.3dm");
        tmpFile.Write(tmpPath, new File3dmWriteOptions());
        using (var objects = Bricscad.Rhino.RhinoUtilityFunctions.ImportRhinoFile(tmpPath, true))
        {
          foreach (var entity in objects.OfType<_OdDb.Entity>())
            AssignTraits(entity);
          dbObjects = DatabaseUtils.AppendObjectsToDatabase(objects, GhDrawingContext.LinkedDocument.Database, false);
        }
      }
      return dbObjects;
    }
    protected void AssignTraits(_OdDb.Entity entity)
    {
      entity.UpgradeOpen();
      entity.Layer = _layer;
      entity.Color = _color;
      entity.Material = _material;
      entity.DowngradeOpen();
    }
    protected void AddGeometry(File3dm file, IGH_Goo obj)
    {
      var scriptVariable = obj.ScriptVariable();
      var objects = file.Objects;
      switch (scriptVariable)
      {
        case Arc arc:             objects.AddArc(arc);                    break;
        case Box box:             objects.AddBrep(box.ToBrep());          break;
        case Brep brep:           objects.AddBrep(brep);                  break;
        case Circle circle:       objects.AddCircle(circle);              break;
        case Curve curve:         objects.AddCurve(curve);                break;
        case Ellipse ellipse:     objects.AddEllipse(ellipse);            break;
        case Extrusion extrusion: objects.AddExtrusion(extrusion);        break;
        case Hatch hatch:         objects.AddHatch(hatch);                break;
        case Line line:           objects.AddLine(line);                  break;
        case Mesh mesh:           objects.AddMesh(mesh);                  break;
        case Point3d point:       objects.AddPoint(point);                break;
        case Rectangle3d rect:    objects.AddPolyline(rect.ToPolyline()); break;
        case Sphere sphere:       objects.AddSphere(sphere);              break;
        case Surface surface:     objects.AddSurface(surface);            break;
      }
    }
    public static void BakeSelectedComponents()
    {
      var canvas = Grasshopper.Instances.ActiveCanvas;
      var definition = canvas?.Document;
      if (definition == null)
        return;

      var bakeProperties = new BakeDialog();
      if (bakeProperties.ShowDialog(Grasshopper.Instances.DocumentEditor) != DialogResult.OK)
        return;

      bool needExpire = false;
      foreach (var obj in definition.Objects.OfType<BakeComponent>())
      {
        if (obj.Locked || !obj.Attributes.Selected)
          continue;

        needExpire = true;
        Expire(obj, bakeProperties);
      }
      if (needExpire)
      {
        GhDrawingContext.LinkedDocument?.Database?.StartUndoRecord();
        definition.NewSolution(false);
      }
    }
    public static void Expire(BakeComponent bakeComponent, BakeDialog bakeProperties)
    {
      bakeComponent._needBake = true;
      bakeComponent._color = bakeProperties.Color;
      bakeComponent._layer = bakeProperties.Layer;
      bakeComponent._material = bakeProperties.Material;
      bakeComponent.ExpireSolution(false);
    }
  }

  public class BakeComponent_OBSOLETE : BakeComponent
  {
    public BakeComponent_OBSOLETE() : base() { }
    public override Guid ComponentGuid => new Guid("B862BC45-8896-4B13-93C6-621D49F214ED");
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    public override bool Obsolete => true;
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddGeometryParameter("Geometry", "G", "Geometry to bake into BricsCAD", GH_ParamAccess.tree);
    }
    protected override void SolveInstance(IGH_DataAccess DA)
    {
      if (!_needBake || !GhDrawingContext.LinkedDocument.IsActive)
        return;
      _needBake = false;

      var geometry = new GH_Structure<IGH_GeometricGoo>();
      if (!DA.GetDataTree("Geometry", out geometry))
        return;

      var objIds = BakeGhGeometry(geometry.AllData(true));
      var res = new List<Types.BcEntity>();
      foreach (_OdDb.ObjectId objId in objIds)
        res.Add(new Types.BcEntity(new _OdDb.FullSubentityPath(new _OdDb.ObjectId[] { objId }, new _OdDb.SubentityId()), GhDrawingContext.LinkedDocument.Name));
      DA.SetDataList("BuildingElement", res);
    }
  }

  public class BakeComponentUpgrader : IGH_UpgradeObject
  {
    public BakeComponentUpgrader() { }
    public DateTime Version
    {
      get { return new DateTime(2021, 8, 3, 17, 0, 0); }
    }
    public Guid UpgradeFrom
    {
      get { return new Guid("B862BC45-8896-4B13-93C6-621D49F214ED"); }
    }
    public Guid UpgradeTo
    {
      get { return new Guid("8C42D2D7-16C5-4AFC-B6E0-6FC2696A1038"); }
    }

    public IGH_DocumentObject Upgrade(IGH_DocumentObject target, GH_Document document)
    {
      IGH_Component newComponent = GH_UpgradeUtil.SwapComponents((IGH_Component) target, UpgradeTo, true);
      if (newComponent == null) { return null; }

      Grasshopper.Kernel.Parameters.Param_String param = new Grasshopper.Kernel.Parameters.Param_String();
      param.NickName = "M";
      param.Name = "Material";
      param.Description = "Material to assign to the Geometry in BricsCAD (Overrides the Bake Dialog Material)";
      param.Access = GH_ParamAccess.item;
      param.Optional = true;

      newComponent.Params.RegisterInputParam(param);
      return newComponent;
    }
  }
}
