using System.Collections.Generic;
using System.Linq;
using Teigha.DatabaseServices;
using Teigha.GraphicsInterface;

namespace GH_BC.Visualization
{
  class CompoundDrawable : Drawable
  {
    private List<PreviewDrawable> _drawables = new List<PreviewDrawable>();
    private List<BlockReference> _blockRefs = new List<BlockReference>();
    private List<PreviewDrawable> _selectedDrawables = new List<PreviewDrawable>();
    private List<BlockReference> _selectedblockRefs = new List<BlockReference>();
    public bool IsRenderMode { get; set; }
    public System.Drawing.Color Color { get; set; }
    public System.Drawing.Color ColorSelected { get; set; }
    public override bool IsPersistent => false;
    public override ObjectId Id { get; }
    public void AddDrawable(PreviewDrawable drawable, bool isSelected)
    {
      (isSelected ? _selectedDrawables : _drawables).Add(drawable);
    }
    public void AddBlockRef(BlockReference blockReference, bool isSelected)
    {
      (isSelected ? _selectedblockRefs : _blockRefs).Add(blockReference);
    }
    public void Clear()
    {
      _drawables.Clear();
      _blockRefs.Clear();
      _selectedDrawables.Clear();
      _selectedblockRefs.Clear();
    }
    protected override int SubSetAttributes(DrawableTraits traits)
    {
      if (traits != null)
      {
        if (traits is SubEntityTraits subEntTraits)
        {
          var db = Bricscad.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Database;
          subEntTraits.VisualStyle = DatabaseUtils.VisualStyleId(db, IsRenderMode ? DatabaseUtils.Realistic :
                                                                                    DatabaseUtils.Wirframe);
          subEntTraits.SelectionFlags = SelectionFlags.SelectionIgnore;
        }
      }
      return (int) AttributesFlags.DrawableIsAnEntity;
    }
    protected override bool SubWorldDraw(WorldDraw wd)
    {
      int drawablesForViewport = 0;
      using (var trSt = new TraitsState(wd.SubEntityTraits))
      {
        SetColor(wd.SubEntityTraits, Color);
        drawablesForViewport = _drawables.Count(drawable => !drawable.WorldDraw(wd));
        drawablesForViewport += _blockRefs.Count(blockRef => worldDrawBlockRef(blockRef, wd, Color));
        SetColor(wd.SubEntityTraits, ColorSelected);
        drawablesForViewport += _selectedDrawables.Count(drawable => !drawable.WorldDraw(wd));
        drawablesForViewport += _selectedblockRefs.Count(blockRef => worldDrawBlockRef(blockRef, wd, ColorSelected));
      }
      return drawablesForViewport == 0;
    }
    protected override void SubViewportDraw(ViewportDraw vd)
    {
      using (var trSt = new TraitsState(vd.SubEntityTraits))
      {
        SetColor(vd.SubEntityTraits, Color);
        _drawables.ForEach(drawable => drawable.ViewportDraw(vd));
        _blockRefs.ForEach(blockRef => vpDrawBlockRef(blockRef, vd, Color));
        SetColor(vd.SubEntityTraits, ColorSelected);
        _selectedDrawables.ForEach(drawable => drawable.ViewportDraw(vd));
        _selectedblockRefs.ForEach(blockRef => vpDrawBlockRef(blockRef, vd, ColorSelected));
      }
    }
    protected override int SubViewportDrawLogicalFlags(ViewportDraw vd) => (int) AttributesFlags.DrawableNone;
    private void SetColor(SubEntityTraits st, System.Drawing.Color color)
    {
      st.TrueColor = new Teigha.Colors.EntityColor(color.R, color.G, color.B);
      st.Transparency = new Teigha.Colors.Transparency(color.A);
    }

    private bool worldDrawBlockRef(BlockReference blockRef, WorldDraw wd, System.Drawing.Color color)
    {

      DBObjectCollection dbcol = new DBObjectCollection();
      bool isVpDrawable = false;
      blockRef.Explode(dbcol);
      foreach (var dbObj in dbcol)
      {
        if (dbObj is BlockReference blockReference)
        {
          if (!worldDrawBlockRef(blockReference, wd, color))
            isVpDrawable = true;
        }
        else if (dbObj is Entity ent)
        {
          ent.Color = Teigha.Colors.Color.FromColor(color);
          if (!ent.WorldDraw(wd))
            isVpDrawable = true;
        }
      }
      return !isVpDrawable;
    }

    private void vpDrawBlockRef(BlockReference blockRef, ViewportDraw vd, System.Drawing.Color color)
    {
      DBObjectCollection dbcol = new DBObjectCollection();
      blockRef.Explode(dbcol);
      foreach (var dbObj in dbcol)
      {
        if (dbObj is BlockReference blockReference)
          vpDrawBlockRef(blockReference, vd, color);
        else if (dbObj is Entity ent)
        {
          ent.Color = Teigha.Colors.Color.FromColor(color);
          ent.ViewportDraw(vd);
        }
      }
    }

  }
}
