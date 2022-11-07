using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GH_BC.UI;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using _OdDb = Teigha.DatabaseServices;
using _OdGe = Teigha.Geometry;

namespace GH_BC.Components
{
  public class BlockTableRecords : GH_ValueList
  {
    public BlockTableRecords()
    {
      Category = "BricsCAD";
      SubCategory = GhUI.Information;
      Name = "BlockName";
      Description = "Provides a name picker for all the block definitions present in the BricsCAD drawing.";
      ListMode = GH_ValueListMode.DropDown;
      NickName = "Block name";
    }
    public override Guid ComponentGuid => new Guid("B821A3FB-B2B5-4B3B-99BB-7CEF762175A3");
    public override GH_Exposure Exposure => GH_Exposure.tertiary;
    protected override Bitmap Icon => Properties.Resources.blocks;
    public void RefreshList()
    {
      var selectedItems = ListItems.Where(x => x.Selected).Select(x => x.Expression).ToList();
      ListItems.Clear();

      var db = GhDrawingContext.LinkedDocument.Database;
      using (var transaction = db.TransactionManager.StartTransaction())
      {
        var bt = transaction.GetObject(db.BlockTableId, _OdDb.OpenMode.ForRead) as _OdDb.BlockTable;
        foreach (_OdDb.ObjectId btrId in bt)
        {
          var btr = transaction.GetObject(btrId, _OdDb.OpenMode.ForRead) as _OdDb.BlockTableRecord;
          if (btr.IsLayout || btr.IsAnonymous)
            continue;

          var item = new GH_ValueListItem(btr.Name, "\"" + btr.Handle.ToString() + "\"");
          item.Selected = selectedItems.Contains(item.Expression);
          ListItems.Add(item);
        }
      } // end transaction
    }
    protected override IGH_Goo InstantiateT() => new GH_String();
    protected override void CollectVolatileData_Custom()
    {
      RefreshList();
      base.CollectVolatileData_Custom();
    }
  }
  public class InsertBlockReference : BakeComponent
  {
    public InsertBlockReference()
      : base("InsertBlock", "IB", "Insert Block Reference at the specified location (default 0, 0, 0), rotation (default 0) and scale (default 1)", "BricsCAD", GhUI.BuildingElements)
    { }
    public override Guid ComponentGuid => new Guid("6AEC598F-2186-4151-BE8C-600F941ECEEA");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override System.Drawing.Bitmap Icon => Properties.Resources.insertblock;
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddTextParameter("Block Definition", "B", "Block Definition", GH_ParamAccess.item);
      pManager[pManager.AddPointParameter("Insertion Point", "I", "Insertion Point", GH_ParamAccess.item)].Optional = true;
      pManager[pManager.AddAngleParameter("Rotation Angle", "A", "Rotation Angle (in radians)", GH_ParamAccess.item)].Optional = true;
      pManager[pManager.AddVectorParameter("Scale", "S", "Scale as a vector", GH_ParamAccess.item)].Optional = true;
      pManager[pManager.AddBooleanParameter("Explode", "E", "Explode", GH_ParamAccess.item)].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddParameter(new Parameters.BcEntity(), "BricsCAD Entities", "B", "The Entities inserted in the drawing", GH_ParamAccess.list);
    }
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
      Menu_AppendItem(menu, "Insert block in BricsCAD", BakeItemCall);
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

      // Extract input parameters
      var btrHandle = new _OdDb.Handle(0);
      string stringHandle = null;
      if (DA.GetData("Block Definition", ref stringHandle))
        btrHandle = new _OdDb.Handle(System.Convert.ToInt64(stringHandle, 16));

      var insertionPoint = new Point3d(0, 0, 0);
      double rotation = 0.0;
      var scale = new Vector3d(1, 1, 1);
      bool explode = false;
      DA.GetData("Insertion Point", ref insertionPoint);
      DA.GetData("Rotation Angle", ref rotation);
      DA.GetData("Scale", ref scale);
      DA.GetData("Explode", ref explode);

      // Insert BlockReference
      var db = GhDrawingContext.LinkedDocument.Database;
      if (!db.TryGetObjectId(btrHandle, out var btrId))
      {
        this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid block handle");
        return;
      }
      var objIds = new _OdDb.ObjectIdCollection();
      using (var transaction = db.TransactionManager.StartTransaction())
      {
        var blockRef = new _OdDb.BlockReference(insertionPoint.ToHost(), btrId) {
          Rotation = rotation,
          ScaleFactors = new _OdGe.Scale3d(scale.X, scale.Y, scale.Z)
        };
        AssignTraits(blockRef);

        var blockTable = transaction.GetObject(db.BlockTableId, _OdDb.OpenMode.ForRead) as _OdDb.BlockTable;
        if (blockTable == null)
          return;
        var modelSpace = transaction.GetObject(blockTable[_OdDb.BlockTableRecord.ModelSpace], _OdDb.OpenMode.ForWrite) as _OdDb.BlockTableRecord;
        if (modelSpace == null)
          return;

        var blockRefId = modelSpace.AppendEntity(blockRef);

        if (explode)
        {
          if (scale.X != scale.Y || scale.X != scale.Z || scale.Y != scale.Z)
            this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Block can only be exploded with uniform scaling");

          var explodedObjects = new _OdDb.DBObjectCollection();
          blockRef.Explode(explodedObjects);
          foreach (var dbObj in explodedObjects)
          {
            if (dbObj is _OdDb.Entity ent)
            {
              AssignTraits(ent);
              var objId = modelSpace.AppendEntity(ent);
              transaction.AddNewlyCreatedDBObject(ent, true);
              objIds.Add(objId);
            }
            blockRef.Erase();
          }
        }
        else
        {
          transaction.AddNewlyCreatedDBObject(blockRef, true);
          objIds.Add(blockRefId);
        }

        transaction.Commit();
      }
      var result = new List<Types.BcEntity>();
      foreach (_OdDb.ObjectId objId in objIds)
        result.Add(new Types.BcEntity(new _OdDb.FullSubentityPath(new _OdDb.ObjectId[] { objId }, new _OdDb.SubentityId()), GhDrawingContext.LinkedDocument.Name));
      DA.SetDataList("BricsCAD Entities", result);
    }
  } // InsertBlockReference component
} // GH_BC.Components
