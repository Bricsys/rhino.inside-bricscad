using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel;
using System.Drawing;
using System.Linq;
using System;
using GH_BC.UI;

namespace GH_BC.Parameters
{
  public class PropCategory : GH_Param<Types.PropCategory>
  {
    public PropCategory()
      : base(new GH_InstanceDescription("Property Category", "PC", "BricsCAD property category.", "BricsCAD", GhUI.BimData)) { }
    public override Guid ComponentGuid => new Guid("C4D5B388-57B5-418D-8D47-7D1759F01C2E");
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    protected override Types.PropCategory InstantiateT() => new Types.PropCategory();
  }

  public class PropCategoryPicker : GH_ValueList
  {
    public override Guid ComponentGuid => new Guid("085494AC-B488-419D-8F81-478673A42187");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override Bitmap Icon => Properties.Resources.propertycategories;
    public PropCategoryPicker() : base()
    {
      Category = "BricsCAD";
      SubCategory = GhUI.BimData;
      Name = "Property Categories";
      NickName = "Property category";
      Description = "Provides a category picker for all the property categories available in BricsCAD.";
      ListMode = GH_ValueListMode.DropDown;

      ListItems.Clear();
      foreach (var suit in Enum.GetValues(typeof(Bricscad.Bim.BimCategory)))
      {
        var item = new GH_ValueListItem(suit.ToString(), ((int) suit).ToString());
        ListItems.Add(item);
      }
    }
  }
}

namespace GH_BC.Components
{
  public class GetPropertyNames : GH_Component
  {
    public GetPropertyNames() : base("Property Names", "PN", "Returns the property names, attached to a building element, in the specified property category.", "BricsCAD", GhUI.Information)
    { }
    public override Guid ComponentGuid => new Guid("CA1F74D9-9C19-4FE5-A069-0E8CF49F4CC5");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override Bitmap Icon => Properties.Resources.propertynames;
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddParameter(new Parameters.BcEntity(), "BuildingElement", "BE", "Building element to extract properties names from.", GH_ParamAccess.item);
      pManager[pManager.AddParameter(new Parameters.PropCategory(), "PropCategory", "C", "Property category.", GH_ParamAccess.item)].Optional = true;
    }
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddTextParameter("Names", "N", "Properties names.", GH_ParamAccess.item);
    }
    protected override void SolveInstance(IGH_DataAccess DA)
    {
      Types.BcEntity bcEnt = null;
      if (!DA.GetData("BuildingElement", ref bcEnt))
        return;

      Types.PropCategory propertyCategory = null;
      DA.GetData("PropCategory", ref propertyCategory);
      var objectId = bcEnt.ObjectId;
      if (propertyCategory != null)
      {
        var props = Bricscad.Bim.BIMClassification.GetPropertiesMap(objectId);
        var catStr = propertyCategory.Value.ToCategoryString();
        var res = props.Where(propData => propData.Value == catStr)
                       .Select(propData => propData.Key)
                       .ToList();
        DA.SetDataList("Names", res);
      }
      else
        DA.SetDataList("Names", Bricscad.Bim.BIMClassification.GetPropertiesString(objectId));
    }
  }

  public class GetPropertyValue : GH_Component
  {
    public GetPropertyValue() : base("Property Value", "PV", "Returns the property value, attached to a building element, for the specified property name and category.", "BricsCAD", GhUI.Information)
    { }
    public override Guid ComponentGuid => new Guid("CFD2F137-30FF-43C8-9F20-1FE5A373529C");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override Bitmap Icon => Properties.Resources.propertyvalue;
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddParameter(new Parameters.BcEntity(), "BuildingElement", "BE", "Building element to extract property value from.", GH_ParamAccess.item);
      pManager.AddTextParameter("PropName", "N", "Property name", GH_ParamAccess.item);
      pManager.AddParameter(new Parameters.PropCategory(), "PropCategory", "C", "Property category.", GH_ParamAccess.item);
    }
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("PropVal", "V", "Property value.", GH_ParamAccess.item);
    }
    protected override void SolveInstance(IGH_DataAccess DA)
    {
      Types.BcEntity bcEnt = null;
      string propertyName = null;
      Types.PropCategory propertyCategory = null;

      if (!DA.GetData("BuildingElement", ref bcEnt) ||
          !DA.GetData("PropName", ref propertyName) ||
          !DA.GetData("PropCategory", ref propertyCategory))
        return;

      var objectId = bcEnt.ObjectId;
      if (!Bricscad.Bim.BIMClassification.HasProperty(objectId, propertyName, propertyCategory.Value))
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, string.Format("Property with name \"{0}\" does not exist", propertyName));
        return;
      }
      try
      {
        var propertyValue = Bricscad.Bim.BIMClassification.GetProperty(objectId, propertyName, propertyCategory.Value);
        if (propertyValue == null)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, string.Format("Failed to obtain property with name \"{0}\"", propertyName));
          return;
        }
        DA.SetData("PropVal", propertyValue);
      }
      catch { }
    }
  }

  public class SetPropertyValue : GH_Component
  {
    public SetPropertyValue() : base("Set Property", "SP", "Sets the property value of the building element according to the specified name, category and value.", "BricsCAD", GhUI.Output)
    { }
    public override Guid ComponentGuid => new Guid("52A36D73-D167-4DD1-9312-0F28EA0B25F3");
    protected override Bitmap Icon => Properties.Resources.setproperty;
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddParameter(new Parameters.BcEntity(), "BuildingElement", "BE", "Building element to set property for.", GH_ParamAccess.item);
      pManager.AddTextParameter("PropName", "N", "Property name.", GH_ParamAccess.item);
      pManager.AddGenericParameter("PropVal", "V", "Value to set.", GH_ParamAccess.item);
      pManager.AddParameter(new Parameters.PropCategory(), "PropCategory", "C", "Property category.", GH_ParamAccess.item);
    }
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
    }
    protected override void SolveInstance(IGH_DataAccess DA)
    {
      Types.BcEntity bcEnt = null;
      string propertyName = null;
      IGH_Goo propertyValue = null;
      Types.PropCategory propertyCategory = null;

      if (!DA.GetData("BuildingElement", ref bcEnt) ||
          !DA.GetData("PropName", ref propertyName) ||
          !DA.GetData("PropVal", ref propertyValue) ||
          !DA.GetData("PropCategory", ref propertyCategory))
        return;

      var objectId = bcEnt.ObjectId;
      object val = null;
      switch (propertyValue.ScriptVariable())
      {
        case string strVal: val = strVal; break;
        case int    intVal: val = intVal; break;
        case double dblVal: val = dblVal; break;
      }
      if(val == null)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
            string.Format("Conversion failed from {0} to string, int or double", propertyValue.TypeName));
        return;
      }

      if (!Bricscad.Bim.BIMClassification.HasProperty(objectId, propertyName, propertyCategory.Value))
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, string.Format("Property with name \"{0}\" does not exist", propertyName));
        return;
      }

      if (Bricscad.Bim.BIMClassification.SetProperty(objectId, propertyName, val, propertyCategory.Value) != Bricscad.Bim.BimResStatus.Ok)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
          string.Format("Failed to set property \"{0}\" for object {1}", propertyName, bcEnt.PersistentRef));
      }
    }
  }

}
