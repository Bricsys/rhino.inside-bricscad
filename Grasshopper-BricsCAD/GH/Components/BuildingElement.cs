using Grasshopper.Kernel.Types;
using Grasshopper.Kernel;
using System.Collections.Generic;
using System;
using _OdDb = Teigha.DatabaseServices;

namespace GH_BC
{
  public class BuildingElement : BakeComponent
  {
    public BuildingElement()
      : base("Bake Building Element", "BBE", "Bake the Grasshopper geometry into the current BricsCAD drawing, while adding BIM data to it. The output of Bake Building Element is a reference to the baked building element with BIM data.", "BricsCAD", GhUI.BuildingElements)
    { }
    public override Guid ComponentGuid => new Guid("6AEC3494-C94A-4FC7-BD4C-427F5B20BC59");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override System.Drawing.Bitmap Icon => Properties.Resources.bakebuildingelement;
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddGeometryParameter("Geometry", "G", "Geometry to bake into BricsCAD", GH_ParamAccess.item);
      pManager[pManager.AddParameter(new ElementType(), "ElementType", "T", "Element Type", GH_ParamAccess.item)].Optional = true;
      pManager[pManager.AddParameter(new SpatialLocation(), "SpatialLocation", "SL", "Spatial location", GH_ParamAccess.item)].Optional = true;
      pManager[pManager.AddParameter(new Profile(), "Profile", "P", "Assigned profile", GH_ParamAccess.item)].Optional = true;
    }
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddParameter(new BcEntity(), "BuildingElement", "BE", "Building element.", GH_ParamAccess.item);
    }
    protected override void AfterSolveInstance()
    {
      _needBake = false;
      base.AfterSolveInstance();
    }
    protected override void SolveInstance(IGH_DataAccess DA)
    {
      if (!_needBake || !PlugIn.LinkedDocument.IsActive)
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
        if (dummy.SaveProfile(PlugIn.LinkedDocument.Database) == Bricscad.Bim.BimResStatus.Ok)
        {
          bimProfile = dummy;
        }
      }

      var createdProfileId = _OdDb.ObjectId.Null;
      _OdDb.ObjectEventHandler objAppended = (s, e) => createdProfileId = e.DBObject.ObjectId;
      PlugIn.LinkedDocument.Database.ObjectAppended += objAppended;
      var curvesToDelete = new _OdDb.ObjectIdCollection();
      for (int i = 0; i < objIds.Count; ++i)
      {
        var id = objIds[i];
        spatialLocation?.AssignToEntity(id);
        Bricscad.Bim.BIMClassification.ClassifyAs(id, elementType);
        bimProfile?.ApplyProfileTo(id, 0, true);
        //replace curve with created solid profile
        if (DatabaseUtils.isCurve(id) && !createdProfileId.IsNull)
        {
          curvesToDelete.Add(id);
          objIds[i] = createdProfileId;
          createdProfileId = _OdDb.ObjectId.Null;
        }
      }
      DatabaseUtils.EraseObjects(curvesToDelete);
      PlugIn.LinkedDocument.Database.ObjectAppended -= objAppended;
      var res = new List<Types.BcEntity>();
      foreach (_OdDb.ObjectId objId in objIds)
        DA.SetData("BuildingElement",
          new Types.BcEntity(new _OdDb.FullSubentityPath(new _OdDb.ObjectId[] { objId }, new _OdDb.SubentityId()), PlugIn.LinkedDocument.Name));
    }
    public _OdDb.ObjectIdCollection BakeGhGeometry(IGH_GeometricGoo gg)
    {
      var tmpFile = new Rhino.FileIO.File3dm();
      AddGeometry(tmpFile, gg);
      return BakeGhGeometry(tmpFile);
    }
  }
}
