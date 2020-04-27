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
    public override Guid ComponentGuid => new Guid("1A940043-04FC-411C-A9CC-A70665B246D5");
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
      pManager[pManager.AddParameter(new Parameters.ElementType(), "ElementType", "T", "ElementType", GH_ParamAccess.list)].Optional = true;
    }
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddParameter(new Parameters.BcEntity(), "Element", "BE", "Element", GH_ParamAccess.list);
    }
    protected override void SolveInstance(IGH_DataAccess DA)
    {
      var spatialLocations = new List<Types.SpatialLocation>();
      DA.GetDataList("SpatialLocation", spatialLocations);

      var elementTypes = new List<Types.ElementType>();
      DA.GetDataList("ElementType", elementTypes);

      ObjectIdCollection bimElements = null;
      if(elementTypes.Count != 0)
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
      foreach(ObjectId objId in bimElements)
        res.Add(new Types.BcEntity(new FullSubentityPath(new ObjectId[] { objId } , new SubentityId()), GhDrawingContext.LinkedDocument.Name));
      DA.SetDataList("Element", res);
    }
  }
}
