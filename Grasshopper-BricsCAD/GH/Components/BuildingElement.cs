using Grasshopper.Kernel.Types;
using Grasshopper.Kernel;
using System.Collections.Generic;
using System;
using _OdDb = Teigha.DatabaseServices;

namespace GH_BC.Components
{
  public class BuildingElement : BakeComponent
  {
    public BuildingElement()
      : base("Bake Building Element", "BBE", "Bake the Grasshopper geometry into the current BricsCAD drawing, while adding BIM data to it. The output of Bake Building Element is a reference to the baked building element with BIM data.", "BricsCAD", UI.GhUI.BuildingElements)
    { }
    public override Guid ComponentGuid => new Guid("B1DFB1E2-E393-49F8-BB00-7EDB37AF971D");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override System.Drawing.Bitmap Icon => Properties.Resources.bakebuildingelement;
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddGeometryParameter("Geometry", "G", "Geometry to bake into BricsCAD", GH_ParamAccess.item);
      pManager[pManager.AddParameter(new Parameters.ElementType(), "ElementType", "T", "Element Type", GH_ParamAccess.item)].Optional = true;
      pManager[pManager.AddParameter(new Parameters.SpatialLocation(), "SpatialLocation", "SL", "Spatial location", GH_ParamAccess.item)].Optional = true;
      pManager[pManager.AddParameter(new Parameters.Profile(), "Profile", "P", "Assigned profile", GH_ParamAccess.item)].Optional = true;
      pManager[pManager.AddTextParameter("Material", "M", "Material to assign to the Geometry in BricsCAD (Overrides the Bake Dialog Material)", GH_ParamAccess.item)].Optional = true;
    }
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddParameter(new Parameters.BcEntity(), "BuildingElement", "BE", "Building element.", GH_ParamAccess.item);
    }
    protected override void AfterSolveInstance()
    {
      _needBake = false;
      base.AfterSolveInstance();
    }
    protected override void SolveInstance(IGH_DataAccess DA)
    {
      if (!_needBake || !GhDrawingContext.LinkedDocument.IsActive)
        return;

      /*Extract input parameters*/
      IGH_GeometricGoo geometry = null;
      if (!DA.GetData("Geometry", ref geometry))
        return;

      var elementType = Bricscad.Bim.BimTypeElement.BimGenericBuildingElt;
      Types.ElementType type = null;
      if (DA.GetData("ElementType", ref type))
        elementType = type.Value;

      Bricscad.Bim.BIMSpatialLocation spatialLocation = null;
      Types.SpatialLocation location = null;
      if (DA.GetData("SpatialLocation", ref location))
        spatialLocation = location.Value;

      string material = string.Empty;
      if (DA.GetData("Material", ref material))
        _material = material;

      var objIds = BakeGhGeometry(geometry);
      if (objIds == null)
        return;

      Bricscad.Bim.BIMProfile bimProfile = null;
      Types.Profile profile = null;
      if (DA.GetData("Profile", ref profile))
      {
        var dummy = new Bricscad.Bim.BIMProfile(profile.Value);
        if (dummy.SaveProfile(GhDrawingContext.LinkedDocument.Database) == Bricscad.Bim.BimResStatus.Ok)
        {
          bimProfile = dummy;
        }
      }

      var createdProfileId = _OdDb.ObjectId.Null;
      _OdDb.ObjectEventHandler objAppended = (s, e) => createdProfileId = e.DBObject.ObjectId;
      GhDrawingContext.LinkedDocument.Database.ObjectAppended += objAppended;
      var curvesToDelete = new _OdDb.ObjectIdCollection();
      for (int i = 0; i < objIds.Count; ++i)
      {
        var id = objIds[i];
        spatialLocation?.AssignToEntity(id);
        Bricscad.Bim.BIMClassification.ClassifyAs(id, elementType);
        bimProfile?.ApplyProfileTo(id, 0, true);
        //replace curve with created solid profile
        if (DatabaseUtils.IsCurve(id) && !createdProfileId.IsNull)
        {
          curvesToDelete.Add(id);
          objIds[i] = createdProfileId;
          createdProfileId = _OdDb.ObjectId.Null;
        }
      }
      if (profile != null)
        DatabaseUtils.EraseObjects(curvesToDelete);
      GhDrawingContext.LinkedDocument.Database.ObjectAppended -= objAppended;
      var res = new List<Types.BcEntity>();
      foreach (_OdDb.ObjectId objId in objIds)
        DA.SetData("BuildingElement",
          new Types.BcEntity(new _OdDb.FullSubentityPath(new _OdDb.ObjectId[] { objId }, new _OdDb.SubentityId()), GhDrawingContext.LinkedDocument.Name));
    }
    public _OdDb.ObjectIdCollection BakeGhGeometry(IGH_GeometricGoo gg)
    {
      using (var tmpFile = new Rhino.FileIO.File3dm())
      {
        AddGeometry(tmpFile, gg);
        return BakeGhGeometry(tmpFile);
      }
    }
  }

  public class BuildingElement_OBSOLETE : BuildingElement
  {
    public BuildingElement_OBSOLETE() : base() { }
    public override Guid ComponentGuid => new Guid("6AEC3494-C94A-4FC7-BD4C-427F5B20BC59");
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    public override bool Obsolete => true;
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddGeometryParameter("Geometry", "G", "Geometry to bake into BricsCAD", GH_ParamAccess.item);
      pManager[pManager.AddParameter(new Parameters.ElementType(), "ElementType", "T", "Element Type", GH_ParamAccess.item)].Optional = true;
      pManager[pManager.AddParameter(new Parameters.SpatialLocation(), "SpatialLocation", "SL", "Spatial location", GH_ParamAccess.item)].Optional = true;
      pManager[pManager.AddParameter(new Parameters.Profile(), "Profile", "P", "Assigned profile", GH_ParamAccess.item)].Optional = true;
    }
    protected override void SolveInstance(IGH_DataAccess DA)
    {
      if (!_needBake || !GhDrawingContext.LinkedDocument.IsActive)
        return;

      /*Extract input parameters*/
      IGH_GeometricGoo geometry = null;
      if (!DA.GetData("Geometry", ref geometry))
        return;

      var elementType = Bricscad.Bim.BimTypeElement.BimGenericBuildingElt;
      Types.ElementType type = null;
      if (DA.GetData("ElementType", ref type))
        elementType = type.Value;

      Bricscad.Bim.BIMSpatialLocation spatialLocation = null;
      Types.SpatialLocation location = null;
      if (DA.GetData("SpatialLocation", ref location))
        spatialLocation = location.Value;

      var objIds = BakeGhGeometry(geometry);
      if (objIds == null)
        return;

      Bricscad.Bim.BIMProfile bimProfile = null;
      Types.Profile profile = null;
      if (DA.GetData("Profile", ref profile))
      {
        var dummy = new Bricscad.Bim.BIMProfile(profile.Value);
        if (dummy.SaveProfile(GhDrawingContext.LinkedDocument.Database) == Bricscad.Bim.BimResStatus.Ok)
        {
          bimProfile = dummy;
        }
      }

      var createdProfileId = _OdDb.ObjectId.Null;
      _OdDb.ObjectEventHandler objAppended = (s, e) => createdProfileId = e.DBObject.ObjectId;
      GhDrawingContext.LinkedDocument.Database.ObjectAppended += objAppended;
      var curvesToDelete = new _OdDb.ObjectIdCollection();
      for (int i = 0; i < objIds.Count; ++i)
      {
        var id = objIds[i];
        spatialLocation?.AssignToEntity(id);
        Bricscad.Bim.BIMClassification.ClassifyAs(id, elementType);
        bimProfile?.ApplyProfileTo(id, 0, true);
        //replace curve with created solid profile
        if (DatabaseUtils.IsCurve(id) && !createdProfileId.IsNull)
        {
          curvesToDelete.Add(id);
          objIds[i] = createdProfileId;
          createdProfileId = _OdDb.ObjectId.Null;
        }
      }
      if (profile != null)
        DatabaseUtils.EraseObjects(curvesToDelete);
      GhDrawingContext.LinkedDocument.Database.ObjectAppended -= objAppended;
      var res = new List<Types.BcEntity>();
      foreach (_OdDb.ObjectId objId in objIds)
        DA.SetData("BuildingElement",
          new Types.BcEntity(new _OdDb.FullSubentityPath(new _OdDb.ObjectId[] { objId }, new _OdDb.SubentityId()), GhDrawingContext.LinkedDocument.Name));
    }
  }

  public class BuildingElementUpgrader : IGH_UpgradeObject
  {
    public BuildingElementUpgrader() { }
    public DateTime Version
    {
      get { return new DateTime(2021, 8, 3, 17, 0, 0); }
    }
    public Guid UpgradeFrom
    {
      get { return new Guid("6AEC3494-C94A-4FC7-BD4C-427F5B20BC59"); }
    }
    public Guid UpgradeTo
    {
      get { return new Guid("B1DFB1E2-E393-49F8-BB00-7EDB37AF971D"); }
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
