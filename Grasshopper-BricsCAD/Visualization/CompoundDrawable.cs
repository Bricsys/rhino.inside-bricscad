using System.Collections.Generic;
using System.Linq;
using Teigha.DatabaseServices;
using Teigha.GraphicsInterface;

namespace GH_BC
{
  class CompoundDrawable : Drawable
  {
    List<PreviewDrawable> Drawables = new List<PreviewDrawable>();
    List<PreviewDrawable> SelectedDrawables = new List<PreviewDrawable>();
    public bool IsRenderMode { get; set; }
    public System.Drawing.Color Colour { get; set; }
    public System.Drawing.Color ColourSelected { get; set; }
    protected override int SubSetAttributes(DrawableTraits traits)
    {
      if (traits != null)
      {
        if (traits is SubEntityTraits subEntTraits)
        {
          subEntTraits.VisualStyle = DatabaseUtils.VisualStyleId(PlugIn.LinkedDocument.Database, IsRenderMode ? DatabaseUtils.Realistic : DatabaseUtils.Wirframe);
          subEntTraits.SelectionFlags = SelectionFlags.SelectionIgnore;
        }
      }
      return (int) AttributesFlags.DrawableIsAnEntity;
    }
    bool SetColor(SubEntityTraits st, System.Drawing.Color color)
    {
      st.TrueColor = new Teigha.Colors.EntityColor(color.R, color.G, color.B);
      st.Transparency = new Teigha.Colors.Transparency(color.A);
      return true;
    }
    protected override bool SubWorldDraw(WorldDraw wd)
    {
      SetColor(wd.SubEntityTraits, Colour);
      int drawablesForViewport = Drawables.Count(drawable => !drawable.WorldDraw(wd));
      SetColor(wd.SubEntityTraits, ColourSelected);
      drawablesForViewport += SelectedDrawables.Count(drawable => !drawable.WorldDraw(wd));
      return drawablesForViewport == 0;
    }
    protected override void SubViewportDraw(ViewportDraw vd)
    {
      SetColor(vd.SubEntityTraits, Colour);
      Drawables.ForEach(drawable => drawable.VieportDraw(vd));
      SetColor(vd.SubEntityTraits, ColourSelected);
      SelectedDrawables.ForEach(drawable => drawable.VieportDraw(vd));
    }
    protected override int SubViewportDrawLogicalFlags(ViewportDraw vd) => (int) AttributesFlags.DrawableNone;
    public override bool IsPersistent => false;
    public override ObjectId Id { get; }
    public void AddDrawable(PreviewDrawable drawable, bool isSelected)
    {
      (isSelected ? SelectedDrawables : Drawables).Add(drawable);
    }
    public void Clear()
    {
      Drawables.Clear();
      SelectedDrawables.Clear();
    }
  }
}
