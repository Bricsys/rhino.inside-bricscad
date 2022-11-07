using Grasshopper.Kernel;
using System.Collections.Generic;
using System.Linq;
using System;
using Teigha.DatabaseServices;

namespace GH_BC.Components
{  
  public class DocumentElementsComponent : GH_Component, IGH_BcComponent
  {
    public DocumentElementsComponent() : base("Document Elements", "DE", "By default, returns all the building elements present in BricsCAD. When using input parameters, returns the building elements filtered by element type and/or spatial location.", "BricsCAD", UI.GhUI.BuildingElements)
    {}
    public override Guid ComponentGuid => new Guid("83316B47-D285-497B-9DD3-00285AE70F30");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    public override bool IsPreviewCapable { get { return false; } }
    public override bool IsBakeCapable { get { return false; } }
    protected override System.Drawing.Bitmap Icon => Properties.Resources.documentelements;
    public bool NeedsToBeExpired(ICollection<Handle> modified, ICollection<Handle> erased, ICollection<Handle> added, ICollection<string> finishedCmds)
    {
      if (erased.Count > 0 || added.Count > 0)
        return true;

      foreach (var param in Params.Output.OfType<Parameters.IGH_BcParam>())
      {
        if (param.NeedsToBeExpired(modified, erased, added, finishedCmds))
          return true;
      }
      return false;
    }
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager[pManager.AddParameter(new Parameters.SpatialLocation(), "SpatialLocation", "SL", "SpatialLocation", GH_ParamAccess.list)].Optional = true;
      pManager[pManager.AddTextParameter("ElementType", "T", "BIM Type of the building elements to filter", GH_ParamAccess.list)].Optional = true;
    }
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddParameter(new Parameters.BcEntity(), "Element", "BE", "Element", GH_ParamAccess.list);
    }
    protected override void SolveInstance(IGH_DataAccess DA)
    {
      var spatialLocations = new List<Types.SpatialLocation>();
      DA.GetDataList("SpatialLocation", spatialLocations);

      var elementTypeNames = new List<string>();
      DA.GetDataList("ElementType", elementTypeNames);

      ObjectIdCollection bimElements = null;
      if(elementTypeNames.Count != 0)
      {
        bimElements = new ObjectIdCollection();
        foreach (var typeName in elementTypeNames)
          foreach (ObjectId objId in Bricscad.Bim.BIMClassification.GetAllClassifiedAs(typeName, false, GhDrawingContext.LinkedDocument.Database))
            bimElements.Add(objId);
      }
      else
        bimElements = Bricscad.Bim.BIMClassification.GetAllClassified(GhDrawingContext.LinkedDocument.Database);

      if (spatialLocations.Count != 0)
      {
        for (int i = bimElements.Count - 1; i >= 0; i--)
        {
          var assignedSpatialLocation = Bricscad.Bim.BIMSpatialLocation.AssignedSpatialLocation(bimElements[i]);
          if (!spatialLocations.Any(spatialLocation => spatialLocation.Value == assignedSpatialLocation))
            bimElements.RemoveAt(i);
        }
      }

      var res = new List<Types.BcEntity>();
      foreach(ObjectId objId in bimElements)
        res.Add(new Types.BcEntity(new FullSubentityPath(new ObjectId[] { objId } , new SubentityId()), GhDrawingContext.LinkedDocument.Name));
      DA.SetDataList("Element", res);
    }
  }

  public class DocumentElementsComponent_OBSOLETE : DocumentElementsComponent
  {
    public DocumentElementsComponent_OBSOLETE() : base() { }
    public override Guid ComponentGuid => new Guid("1A940043-04FC-411C-A9CC-A70665B246D5");
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    public override bool Obsolete => true;
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager[pManager.AddParameter(new Parameters.SpatialLocation(), "SpatialLocation", "SL", "SpatialLocation", GH_ParamAccess.list)].Optional = true;
      pManager[pManager.AddParameter(new Parameters.ElementType_OBSOLETE(), "ElementType", "T", "ElementType", GH_ParamAccess.list)].Optional = true;
    }
    protected override void SolveInstance(IGH_DataAccess DA)
    {
      var spatialLocations = new List<Types.SpatialLocation>();
      DA.GetDataList("SpatialLocation", spatialLocations);

      var elementTypes = new List<Types.ElementType>();
      DA.GetDataList("ElementType", elementTypes);

      ObjectIdCollection bimElements = null;
      if (elementTypes.Count != 0)
      {
        bimElements = new ObjectIdCollection();
        foreach (var elementType in elementTypes)
          foreach (ObjectId objId in Bricscad.Bim.BIMClassification.GetAllClassifiedAs(elementType.Value, GhDrawingContext.LinkedDocument.Database))
            bimElements.Add(objId);
      }
      else
        bimElements = Bricscad.Bim.BIMClassification.GetAllClassified(GhDrawingContext.LinkedDocument.Database);

      if (spatialLocations.Count != 0)
      {
        for (int i = bimElements.Count - 1; i >= 0; i--)
        {
          var assignedSpatialLocation = Bricscad.Bim.BIMSpatialLocation.AssignedSpatialLocation(bimElements[i]);
          if (!spatialLocations.Any(spatialLocation => spatialLocation.Value == assignedSpatialLocation))
            bimElements.RemoveAt(i);
        }
      }

      var res = new List<Types.BcEntity>();
      foreach (ObjectId objId in bimElements)
        res.Add(new Types.BcEntity(new FullSubentityPath(new ObjectId[] { objId }, new SubentityId()), GhDrawingContext.LinkedDocument.Name));
      DA.SetDataList("Element", res);
    }
  }

  public class DocumentElementsUpgrader : IGH_UpgradeObject
  {
    public DateTime Version => new DateTime(2022, 10, 18, 17, 0, 0);
    public Guid UpgradeFrom => new Guid("1A940043-04FC-411C-A9CC-A70665B246D5");
    public Guid UpgradeTo => new Guid("83316B47-D285-497B-9DD3-00285AE70F30");

    public IGH_DocumentObject Upgrade(IGH_DocumentObject target, GH_Document document)
    {
      IGH_Component upgradeComponent = GH_UpgradeUtil.SwapComponents((IGH_Component) target, UpgradeTo, true);
      if (upgradeComponent == null) { return null; }


      Grasshopper.Kernel.Parameters.Param_String typeNameParam = new Grasshopper.Kernel.Parameters.Param_String();
      typeNameParam.NickName = "T";
      typeNameParam.Name = "ElementType";
      typeNameParam.Description = "BIM Type of the building elements to filter";
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
}
