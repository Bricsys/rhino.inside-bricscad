using System.Collections.Generic;
using System.Linq;
using Teigha.DatabaseServices;
using Teigha.GraphicsInterface;

namespace GH_BC.Visualization
{
  class CompoundDrawable : Drawable
  {
    private List<PreviewDrawable> _drawables = new List<PreviewDrawable>();
    private List<PreviewDrawable> _selectedDrawables = new List<PreviewDrawable>();
    public bool IsRenderMode { get; set; }
    public System.Drawing.Color Color { get; set; }
    public System.Drawing.Color ColorSelected { get; set; }
    public override bool IsPersistent => false;
    public override ObjectId Id { get; }
    public void AddDrawable(PreviewDrawable drawable, bool isSelected)
    {
      (isSelected ? _selectedDrawables : _drawables).Add(drawable);
    }
    public void Clear()
    {
      _drawables.Clear();
      _selectedDrawables.Clear();
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
        SetColor(wd.SubEntityTraits, ColorSelected);
        drawablesForViewport += _selectedDrawables.Count(drawable => !drawable.WorldDraw(wd));
      }
      return drawablesForViewport == 0;
    }
    protected override void SubViewportDraw(ViewportDraw vd)
    {
      using (var trSt = new TraitsState(vd.SubEntityTraits))
      {
        SetColor(vd.SubEntityTraits, Color);
        _drawables.ForEach(drawable => drawable.VieportDraw(vd));
        SetColor(vd.SubEntityTraits, ColorSelected);
        _selectedDrawables.ForEach(drawable => drawable.VieportDraw(vd));
      }
    }
    protected override int SubViewportDrawLogicalFlags(ViewportDraw vd) => (int) AttributesFlags.DrawableNone;
    private bool SetColor(SubEntityTraits st, System.Drawing.Color color)
    {
      st.TrueColor = new Teigha.Colors.EntityColor(color.R, color.G, color.B);
      st.Transparency = new Teigha.Colors.Transparency(color.A);
      return true;
    }
  }
}
