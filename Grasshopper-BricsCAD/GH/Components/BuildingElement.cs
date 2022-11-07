using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.Linq;
using System;
using _OdDb = Teigha.DatabaseServices;
using GH_BC.UI;

namespace GH_BC.Components
{
  public class ElementTypePicker : GH_ValueList
  {
    public override Guid ComponentGuid => new Guid("A894B640-5F8E-4450-91A4-7A0283B6D578");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override Bitmap Icon => Properties.Resources.elementtype;
    public ElementTypePicker()
    {
      Category = "BricsCAD";
      SubCategory = GhUI.BimData;
      Name = "BIM Types";
      MutableNickName = false;
      Description = "Provides a type picker for all the BIM Types available in BricsCAD.";
      ListMode = GH_ValueListMode.DropDown;
      NickName = "BIM type";

      var selectedItems = ListItems.Where(x => x.Selected).Select(x => x.Expression).ToList();
      ListItems.Clear();
      StringCollection classificationNames = new StringCollection();
      classificationNames = Bricscad.Bim.BIMClassification.GetAllClassificationNames(false);
      foreach (var typeName in classificationNames)
      {
        var item = new GH_ValueListItem(typeName, "\"" + typeName + "\"");
        item.Selected = selectedItems.Contains(item.Expression);
        ListItems.Add(item);
      }
    }
  }

  public class BuildingElement : BakeComponent
  {
    public BuildingElement()
      : base("Bake Building Element", "BBE", "Bake the Grasshopper geometry into the current BricsCAD drawing, while adding BIM data to it. The output of Bake Building Element is a reference to the baked building element with BIM data.", "BricsCAD", UI.GhUI.BuildingElements)
    { }
    public override Guid ComponentGuid => new Guid("699E0206-56D4-4888-8453-A1A4A56926F4");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override System.Drawing.Bitmap Icon => Properties.Resources.bakebuildingelement;
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddGeometryParameter("Geometry", "G", "Geometry to bake into BricsCAD", GH_ParamAccess.item);
      pManager[pManager.AddTextParameter("ElementType", "T", "BIM Type of the building element", GH_ParamAccess.item)].Optional = true;
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

      string typeName = string.Empty;
      DA.GetData("ElementType", ref typeName);

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
        Bricscad.Bim.BIMClassification.ClassifyAs(id, typeName, false);
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
    public override Guid ComponentGuid => new Guid("B1DFB1E2-E393-49F8-BB00-7EDB37AF971D");
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    public override bool Obsolete => true;
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddGeometryParameter("Geometry", "G", "Geometry to bake into BricsCAD", GH_ParamAccess.item);
      pManager[pManager.AddParameter(new Parameters.ElementType_OBSOLETE(), "ElementType", "T", "Element Type", GH_ParamAccess.item)].Optional = true;
      pManager[pManager.AddParameter(new Parameters.SpatialLocation(), "SpatialLocation", "SL", "Spatial location", GH_ParamAccess.item)].Optional = true;
      pManager[pManager.AddParameter(new Parameters.Profile(), "Profile", "P", "Assigned profile", GH_ParamAccess.item)].Optional = true;
      pManager[pManager.AddTextParameter("Material", "M", "Material to assign to the Geometry in BricsCAD (Overrides the Bake Dialog Material)", GH_ParamAccess.item)].Optional = true;
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
      foreach (_OdDb.ObjectId objId in objIds)
        DA.SetData("BuildingElement",
          new Types.BcEntity(new _OdDb.FullSubentityPath(new _OdDb.ObjectId[] { objId }, new _OdDb.SubentityId()), GhDrawingContext.LinkedDocument.Name));
    }
  }

  public class BuildingElementNoMaterial_OBSOLETE : BuildingElement
  {
    public BuildingElementNoMaterial_OBSOLETE() : base() { }
    public override Guid ComponentGuid => new Guid("6AEC3494-C94A-4FC7-BD4C-427F5B20BC59");
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    public override bool Obsolete => true;
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddGeometryParameter("Geometry", "G", "Geometry to bake into BricsCAD", GH_ParamAccess.item);
      pManager[pManager.AddParameter(new Parameters.ElementType_OBSOLETE(), "ElementType", "T", "Element Type", GH_ParamAccess.item)].Optional = true;
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
      foreach (_OdDb.ObjectId objId in objIds)
        DA.SetData("BuildingElement",
          new Types.BcEntity(new _OdDb.FullSubentityPath(new _OdDb.ObjectId[] { objId }, new _OdDb.SubentityId()), GhDrawingContext.LinkedDocument.Name));
    }
  }

  public class BuildingElementUpgrader : IGH_UpgradeObject
  {
    public DateTime Version => new DateTime(2022, 10, 18, 17, 0, 0);
    public Guid UpgradeFrom => new Guid("B1DFB1E2-E393-49F8-BB00-7EDB37AF971D");
    public Guid UpgradeTo => new Guid("699E0206-56D4-4888-8453-A1A4A56926F4");

    public IGH_DocumentObject Upgrade(IGH_DocumentObject target, GH_Document document)
    {
      IGH_Component upgradeComponent = GH_UpgradeUtil.SwapComponents((IGH_Component) target, UpgradeTo, true);
      if (upgradeComponent == null) { return null; }


      Grasshopper.Kernel.Parameters.Param_String typeNameParam = new Grasshopper.Kernel.Parameters.Param_String();
      typeNameParam.NickName = "T";
      typeNameParam.Name = "ElementType";
      typeNameParam.Description = "BIM Type of the building element)";
      typeNameParam.Access = GH_ParamAccess.item;
      typeNameParam.Optional = true;

      int paramIndex = upgradeComponent.Params.IndexOfInputParam("ElementType");
      if (paramIndex == -1)
        return null;

      upgradeComponent.Params.Input.RemoveAt(paramIndex);
      upgradeComponent.Params.RegisterInputParam(typeNameParam, paramIndex);
      upgradeComponent.Params.OnParametersChanged();
      return upgradeComponent;
    }
  }

  public class BuildingElementNoMaterialUpgrader : IGH_UpgradeObject
  {
    public DateTime Version => new DateTime(2022, 10, 18, 17, 0, 0);

    public Guid UpgradeFrom => new Guid("6AEC3494-C94A-4FC7-BD4C-427F5B20BC59");

    public Guid UpgradeTo => new Guid("699E0206-56D4-4888-8453-A1A4A56926F4");

    public IGH_DocumentObject Upgrade(IGH_DocumentObject target, GH_Document document)
    {
      IGH_Component upgradeComponent = GH_UpgradeUtil.SwapComponents((IGH_Component) target, UpgradeTo, true);
      if (upgradeComponent == null) { return null; }

      Grasshopper.Kernel.Parameters.Param_String materialParam = new Grasshopper.Kernel.Parameters.Param_String();
      materialParam.NickName = "M";
      materialParam.Name = "Material";
      materialParam.Description = "Material to assign to the Geometry in BricsCAD (Overrides the Bake Dialog Material)";
      materialParam.Access = GH_ParamAccess.item;
      materialParam.Optional = true;

      Grasshopper.Kernel.Parameters.Param_String typeNameParam = new Grasshopper.Kernel.Parameters.Param_String();
      typeNameParam.NickName = "T";
      typeNameParam.Name = "ElementType";
      typeNameParam.Description = "BIM Type of the building element)";
      typeNameParam.Access = GH_ParamAccess.item;
      typeNameParam.Optional = true;

      int paramIndex = upgradeComponent.Params.IndexOfInputParam("ElementType");
      if (paramIndex == -1)
        return null;

      upgradeComponent.Params.RegisterInputParam(materialParam);
      upgradeComponent.Params.Input.RemoveAt(paramIndex);
      upgradeComponent.Params.RegisterInputParam(typeNameParam, paramIndex);
      upgradeComponent.Params.OnParametersChanged();
      return upgradeComponent;
    }
  }
}
