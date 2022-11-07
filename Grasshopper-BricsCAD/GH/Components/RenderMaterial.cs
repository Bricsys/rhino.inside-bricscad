using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System;
using _OdRx = Teigha.Runtime;
using _OdDb = Teigha.DatabaseServices;
using GH_BC.Parameters;

namespace GH_BC
{
  public class MaterialName : GH_ValueList, IGH_BcParam
  {
    public MaterialName()
    {
      Category = "BricsCAD";
      SubCategory = UI.GhUI.Information;
      Name = "Material";
      Description = "Provides a name picker for the render materials present in the BricsCAD drawing.";
      ListMode = GH_ValueListMode.DropDown;
      NickName = "Material";
    }

    public override Guid ComponentGuid => new Guid("EAB17A94-6D29-4404-BF6A-A22F2E738B18");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override Bitmap Icon => Properties.Resources.rendermaterial;
    public void RefreshList()
    {
      var selectedItems = ListItems.Where(x => x.Selected).Select(x => x.Expression).ToList();
      ListItems.Clear();
      var materialNames = new HashSet<string>();
      var materials = DatabaseUtils.GetMaterials(GhDrawingContext.LinkedDocument.Database);
      foreach (var materialName in materials)
      {
        if (!materialNames.Contains(materialName))
        {
          var item = new GH_ValueListItem(materialName, "\"" + materialName + "\"");
          item.Selected = selectedItems.Contains(item.Expression);
          ListItems.Add(item);
          materialNames.Add(materialName);
        }
      }
    }
    protected override IGH_Goo InstantiateT() => new GH_String();
    protected override void CollectVolatileData_Custom()
    {
      RefreshList();
      base.CollectVolatileData_Custom();
    }
    public bool NeedsToBeExpired(ICollection<_OdDb.Handle> modified,
                                 ICollection<_OdDb.Handle> erased,
                                 ICollection<_OdDb.Handle> added,
                                 ICollection<string> finishedCmds)
    {
      foreach (_OdDb.Handle handle in added)
      {
        var db = GhDrawingContext.LinkedDocument.Database;
        _OdDb.ObjectId objId;
        if (db.TryGetObjectId(handle, out objId) && objId.ObjectClass.IsDerivedFrom(_OdRx.RXObject.GetClass(typeof(_OdDb.Material))))
          return true;
      }
      return false;
    }
  }

}
