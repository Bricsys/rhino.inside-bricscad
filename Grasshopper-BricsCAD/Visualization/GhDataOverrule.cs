using System;
using Teigha.DatabaseServices;
using Teigha.GraphicsInterface;
using Teigha.Colors;

namespace GH_BC.Visualization
{
  class TraitsState : IDisposable
  {
    EntityColor _entColor;
    Transparency _transparency;
    ObjectId _visualStyle;
    SelectionFlags _selectionFlags;
    SubEntityTraits _subEntityTraits;
    public TraitsState(SubEntityTraits subEntityTraits)
    {
      _subEntityTraits = subEntityTraits;
      _entColor = subEntityTraits.TrueColor;
      _transparency = subEntityTraits.Transparency;
      _visualStyle = subEntityTraits.VisualStyle;
      _selectionFlags = subEntityTraits.SelectionFlags;
    }
    public void Dispose()
    {
      _subEntityTraits.TrueColor = _entColor;
      _subEntityTraits.Transparency = _transparency;
      _subEntityTraits.VisualStyle = _visualStyle;
      _subEntityTraits.SelectionFlags = _selectionFlags;
    }
  }
  class GhDataOverrule : DrawableOverrule
  {
    public override bool WorldDraw(Drawable drawable, WorldDraw wd)
    {
      if (drawable is Entity ent)
      {
        var id = GrasshopperData.GetGrasshopperData(ent);
        if (!id.IsNull)
        {
          var database = id.Database;
          var docExt = GhBcConnection.GrasshopperDataExtension.GrasshopperDataManager(database);
          var ghDrawable = docExt?.GetGhDrawable(id);
          if (ghDrawable == null || ghDrawable.IsDisposed)
            return base.WorldDraw(drawable, wd);

          bool needToDraw = false;
          using (var transaction = database.TransactionManager.StartTransaction())
          {
            using (var ghData = transaction.GetObject(id, OpenMode.ForRead) as GrasshopperData)
            {
              needToDraw = ghData.IsVisible;
            }
            transaction.Commit();
          }
          if(needToDraw)
          {
            using (var trSt = new TraitsState(wd.SubEntityTraits))
            {
              wd.Geometry.Draw(ghDrawable);
            }
            wd.SubEntityTraits.Transparency = new Transparency((byte) GhDataSettings.HostTransparency);
          }
        }
      }
      return base.WorldDraw(drawable, wd);
    }
  }
}
