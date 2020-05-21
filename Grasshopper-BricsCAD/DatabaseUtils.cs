using System.Collections.Generic;
using System.Linq;
using Bricscad.ApplicationServices;
using Teigha.DatabaseServices;
using Teigha.GraphicsInterface;

namespace GH_BC
{
  public static class DatabaseUtils
  {
    public const string Wirframe = "Wireframe";
    public const string Realistic = "Realistic";
    public static ObjectId VisualStyleId(Database database, string visualStyleName)
    {
      using (var dictionary = database.VisualStyleDictionaryId.GetObject(OpenMode.ForRead) as DBDictionary)
      {
        var res = dictionary?.GetAt(visualStyleName) ?? ObjectId.Null;
        dictionary?.Dispose();
        return res;
      }
    }
    public static bool IsNullObjectLink(this FullSubentityPath fsp)
    {
      return (fsp.GetObjectIds() == null || fsp.GetObjectIds().Length == 0) && (fsp.SubentId.Type == SubentityType.Null);
    }
    public static bool IsSubentity(this FullSubentityPath fsp)
    {
      return IsSubentity(fsp.SubentId.Type);
    }
    public static ObjectId InsertId(this FullSubentityPath fsp)
    {
      var objIds = fsp.GetObjectIds();
      return (objIds != null && objIds.Length > 0) ? objIds[0] : ObjectId.Null;
    }
    public static FullSubentityPath ToFsp(this ObjectId id)
    {
      return new FullSubentityPath(new ObjectId[] { id }, new SubentityId());
    }
    public static bool IsSubentity(SubentityType subentType)
    {
      return subentType == SubentityType.Edge || subentType == SubentityType.Face || subentType == SubentityType.Vertex;
    }
    public static string ToString(FullSubentityPath fsp)
    {
      string res = "";
      foreach (var id in fsp.GetObjectIds())
      {
        res += id.Handle.ToString() + ":";
      }
      switch (fsp.SubentId.Type)
      {
        case SubentityType.Null: return res.Remove(res.Length - 1);
        case SubentityType.Face: res += "F_"; break;
        case SubentityType.Edge: res += "E_"; break;
        case SubentityType.Vertex: res += "V_"; break;
      }
      return res + fsp.SubentId.Index.ToString();
    }
    public static ObjectIdCollection AppendObjectsToDatabase(DBObjectCollection objects, Database database, bool createUndo)
    {
      var objIds = new ObjectIdCollection();
      if (objects.Count == 0)
        return objIds;

      if (createUndo)
        database.StartUndoRecord();

      using (var transaction = database.TransactionManager.StartTransaction())
      {
        var blockTable = transaction.GetObject(database.BlockTableId, OpenMode.ForRead) as BlockTable;
        var blockTableRecord = transaction.GetObject(blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
        foreach (var ent in objects.OfType<Entity>())
        {
          objIds.Add(blockTableRecord.AppendEntity(ent));
          transaction.AddNewlyCreatedDBObject(ent, true);
        }
        transaction.Commit();
      }
      return objIds;
    }
    public static void EraseObjects(ObjectIdCollection aId)
    {
      if (aId.Count == 0)
        return;

      using (var transaction = aId[0].Database.TransactionManager.StartTransaction())
      {
        foreach (ObjectId id in aId)
        {
          var entity = transaction.GetObject(id, OpenMode.ForWrite) as Entity;
          entity.Erase(true);
        }
        transaction.Commit();
      }
    }
    public static List<string> GetLayers(Database database)
    {
      var layers = new List<string>();
      using (Transaction tr = database.TransactionManager.StartOpenCloseTransaction())
      {
        var layerTable = tr.GetObject(database.LayerTableId, OpenMode.ForRead) as LayerTable;
        foreach (var layerId in layerTable)
        {
          var layer = tr.GetObject(layerId, OpenMode.ForRead) as LayerTableRecord;
          layers.Add(layer.Name);
        }
      }
      return layers;
    }
    public static List<string> GetMaterials(Database database)
    {
      var materials = new List<string>();
      using (Transaction tr = database.TransactionManager.StartOpenCloseTransaction())
      {
        var materialDic = tr.GetObject(database.MaterialDictionaryId, OpenMode.ForRead) as DBDictionary;
        foreach (var materialId in materialDic)
        {
          var material = tr.GetObject(materialId.Value, OpenMode.ForRead) as Material;
          materials.Add(material.Name);
        }
      }
      return materials;
    }
    public static void Highlight(FullSubentityPath fsp, bool highlight)
    {
      var objId = fsp.InsertId();
      if (!objId.IsValid || objId.IsNull || objId.IsErased)
        return;

      var entity = objId.GetObject(OpenMode.ForWrite) as Entity;
      if (entity != null)
      {
        if (IsSubentity(fsp))
        {
          if (highlight)
            entity.Highlight(fsp, true);
          else
            entity.Unhighlight(fsp, true);
        }
        else
        {
          if (highlight)
            entity.Highlight();
          else
            entity.Unhighlight();
        }
      }
      entity.Dispose();
    }
    public static string ToCategoryString(this Bricscad.Bim.BimCategory category)
    {
      switch (category)
      {
        case Bricscad.Bim.BimCategory.Standard: return string.Empty;
        case Bricscad.Bim.BimCategory.Bricsys: return "Bricsys";
        case Bricscad.Bim.BimCategory.IFC2x3: return "IFC2x3";
        case Bricscad.Bim.BimCategory.IFCCustom: return "Custom";
        case Bricscad.Bim.BimCategory.User: return "User";
        case Bricscad.Bim.BimCategory.Quantity: return "BricsysQuantity";
      }
      return string.Empty;
    }
    public static bool IsCurve(ObjectId id)
      => id.ObjectClass.IsDerivedFrom(Teigha.Runtime.RXObject.GetClass(typeof(Curve)));
    public static Document FindDocument(Database db)
    {
      foreach (Document doc in Application.DocumentManager)
      {
        if (doc.Database == db)
          return doc;
      }
      return null;
    }
    public static TransientManager TransientGraphicsManager(this Document doc)
    {
      var savedWorkingDb = HostApplicationServices.WorkingDatabase;
      HostApplicationServices.WorkingDatabase = doc.Database;
      var transientManager = TransientManager.CurrentTransientManager;
      HostApplicationServices.WorkingDatabase = savedWorkingDb;
      return transientManager;
    }
  }
}
