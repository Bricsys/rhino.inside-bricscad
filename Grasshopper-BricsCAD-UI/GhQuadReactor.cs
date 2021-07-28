using Bricscad.Quad;
using Teigha.DatabaseServices;

namespace GH_BC.UI
{
  class GhQuadReactor : QuadReactor
  {
    #region QuadReactor
    public override bool appendQuadItems(QuadItems quadItems) => true;
    public override bool appendQuadItems(QuadSelection quadSelection, QuadItems quadItems)
    {
      var allData = quadSelection.fullData();
      if (!allData.isValid() || (allData.hasTypes() & 1) == 0)
        return false;

      uint numEntries = allData.length();
      bool foundGhData = false;
      for (uint i = 0; i < numEntries && !foundGhData; ++i)
      {
        if (allData.typeAt(i) == QuadSelectionData.SelectedType.Entity)
        {
          var id = (ObjectId) allData.entityAt(i);
          foundGhData = HasGhDataAttached(id);
        }
      }

      if (!foundGhData)
        return false;

      bool res = quadItems.append("clearghdata", null, null, null);
      res &= quadItems.append("bakeghdata", null, null, null);
      return res;
    }
    public override string displayName() => "Grasshopper";
    public override string GUID() => "5F6982C0-0E9A-490F-9A34-273B090E6EC9";
    public override bool registerQuadItems(QuadItemRegistry quadItemRegistry)
    {
      quadItemRegistry.append("clearghdata", "ClearGhData", "ClearGhData.png", "^c^c_clearghdata", "Clear grasshopper data attached to selected object");
      quadItemRegistry.append("bakeghdata", "BakeGhdata", "BakeGhData.png", "^c^c_bakeghdata", "Bake grasshopper data attached to selected object");
      return true;
    }
    #endregion
    public bool Register() => QuadReactor.registerQuadReactor(this);
    public bool Unregister() => QuadReactor.unregisterQuadReactor(this);
    private bool HasGhDataAttached(ObjectId id)
    {
      using (var tx = id.Database.TransactionManager.StartTransaction())
      {
        using (var ent = id.GetObject(OpenMode.ForRead) as Entity)
        {
          if (ent == null)
            return false;
          var dictId = ent.ExtensionDictionary;
          if (dictId.IsNull)
            return false;

          using (var dict = dictId.GetObject(OpenMode.ForRead) as DBDictionary)
          {
            return dict.Contains("GrasshopperData");
          }
        }
      }
    }
  }
}
