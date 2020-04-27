using Grasshopper;
using System.Windows.Forms;
using System.IO;
using _BcAp = Bricscad.ApplicationServices;

namespace GH_BC.UI
{
  static class GhUI
  {
    public const string InputGeometry = " Input geometry";
    public const string BimData = "BIM Data";
    public const string BuildingElements = "Building Elements";
    public const string Information = "Information";
    public const string Output = "Output";
    static bool _customized = false;
    public static void CustomizeUI()
    {
      if (_customized)
        return;
      _customized = true;

      if (Instances.DocumentEditor.Controls[4] is Grasshopper.GUI.GH_MenuStrip menuStrip)
      {
        var items = menuStrip.Items.Find("mnuSpecialFolders", true);
        if (items.Length != 0 && items[0] is ToolStripMenuItem specFold)
        {
          var samplesItem = new ToolStripMenuItem("Grasshopper-BricsCAD Connection");
          samplesItem.Image = specFold.DropDownItems[0].Image;
          samplesItem.Click += (s, e) =>
          {
            try
            {
              System.Diagnostics.Process.Start(@Path.Combine(GhBcConnection.DllPath, "Samples"));
            }
            catch (System.Exception) { }
          };
          specFold.DropDownItems.Add(samplesItem);
        }
      }

      if (Instances.DocumentEditor.Controls[0].Controls[1] is ToolStrip toolbar)
      {
        var linkButton = new ToolStripButton(Properties.Resources.link)
        {
          ToolTipText = "Link with current BricsCAD document"
        };
        linkButton.Click += (s, e) =>
        {
          if (System.Convert.ToInt16(_BcAp.Application.GetSystemVariable("DWGTITLED")) == 0)
            MessageBox.Show("Bricscad drawing must be saved before using Grasshopper");          
          else
            GhDrawingContext.RelinkToDoc(Bricscad.ApplicationServices.Application.DocumentManager.MdiActiveDocument);
        };
        toolbar.Items.Add(linkButton);

        var bakeSelectedButton = new ToolStripButton(Properties.Resources.bake)
        {
          ToolTipText = "Bakes the geometry from the selected Bake Geometry and Building Element components"
        };
        bakeSelectedButton.Click += (s, e) => { Components.BakeComponent.BakeSelectedComponents(); };
        toolbar.Items.Add(bakeSelectedButton);
      }
    }
  }
}
