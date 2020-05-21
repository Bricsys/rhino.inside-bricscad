using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System;
using Teigha.DatabaseServices;
using GH_BC.UI;

namespace GH_BC.Parameters
{
  public class SpatialLocation : GH_Param<Types.SpatialLocation>
  {
    public SpatialLocation(GH_InstanceDescription nTag) : base(nTag) { }
    public SpatialLocation()
      : base(new GH_InstanceDescription("SpatialLocation", "SpatialLocation", "Represents a SpatialLocation.", "BricsCAD", "Input")) { }
    public override Guid ComponentGuid => new Guid("5B51A61A-33F7-4CB6-ABE1-489681E0FACC");
    public override GH_Exposure Exposure => GH_Exposure.hidden;
  }

  public class ElementType : GH_Param<Types.ElementType>
  {
    public ElementType()
      : base(new GH_InstanceDescription("Element Type", "ET", "BricsCAD BIM type.", "BricsCAD", GhUI.BimData)) { }
    public override Guid ComponentGuid => new Guid("10FBB25D-E738-4B10-BFFD-E32D8C80976E");
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    protected override Types.ElementType InstantiateT() => new Types.ElementType();
  }

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
      foreach (var suit in Enum.GetValues(typeof(Bricscad.Bim.BimTypeElement)))
      {
        var item = new GH_ValueListItem(suit.ToString(), ((int) suit).ToString());
        item.Selected = selectedItems.Contains(item.Expression);
        ListItems.Add(item);
      }
    }
  }

  public class DocumentBuildingsPicker : GH_ValueList, IGH_BcParam
  {
    public override Guid ComponentGuid => new Guid("BD6A74F3-8C46-4506-87D9-B34BD96747DA");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override Bitmap Icon => Properties.Resources.building;
    public DocumentBuildingsPicker()
    {
      Category = "BricsCAD";
      SubCategory = GhUI.BimData;
      Name = "Buildings";
      MutableNickName = false;
      Description = "Provides a name picker for all the buildings present in Spatial Locations in BricsCAD.";
      ListMode = GH_ValueListMode.CheckList;
      NickName = "Building";
    }
    public void RefreshList()
    {
      var selectedItems = ListItems.Where(x => x.Selected).Select(x => x.Expression).ToList();
      ListItems.Clear();

      if (GhDrawingContext.LinkedDocument.Database != null)
      {
        var allStory = Bricscad.Bim.BIMBuilding.AllObjectBuildings(GhDrawingContext.LinkedDocument.Database);
        allStory.ForEach(building =>
        {
          var item = new GH_ValueListItem(building.Name, "\"" + building.Name + "\"");
          item.Selected = selectedItems.Contains(item.Expression);
          ListItems.Add(item);
        });
      }
    }
    protected override IGH_Goo InstantiateT()
    {
      return new Types.SpatialLocation();
    }
    protected override void CollectVolatileData_Custom()
    {
      RefreshList();
      base.CollectVolatileData_Custom();
    }
    public bool NeedsToBeExpired(ICollection<Handle> modified,
                                 ICollection<Handle> erased,
                                 ICollection<Handle> added,
                                 ICollection<string> finishedCmds) => finishedCmds.Contains("BIMSPATIALLOCATIONS");
  }
}

namespace GH_BC.Components
{
    public class BuildingStories : GH_Component, IGH_BcComponent
  {
    public BuildingStories() : base("Stories", "ST", "Returns all the stories attached to the input building.", "BricsCAD", GhUI.BimData)
    { }
    public override Guid ComponentGuid => new Guid("7A92E9DC-1E7A-4A72-A55F-3197FDA92A7C");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override Bitmap Icon => Properties.Resources.story;
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddParameter(new Parameters.SpatialLocation(), "Building", "Building", "Building", GH_ParamAccess.item);
    }
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddParameter(new Parameters.SpatialLocation(), "Story", "Story", "Story", GH_ParamAccess.list);
    }
    protected override void SolveInstance(IGH_DataAccess DA)
    {
      Types.SpatialLocation building = null;
      if (!DA.GetData("Building", ref building))
        return;

      var stories = Bricscad.Bim.BIMBuilding.AllObjectStories(GhDrawingContext.LinkedDocument.Database, building.Value.Name)
                                            .Select(story => new Types.SpatialLocation(story)).ToList();
      if (stories.Count != 0)
        DA.SetDataList("Story", stories);
    }
    public bool NeedsToBeExpired(ICollection<Handle> modified,
                                 ICollection<Handle> erased,
                                 ICollection<Handle> added,
                                 ICollection<string> finishedCmds) => finishedCmds.Contains("BIMSPATIALLOCATIONS");
  }
}
