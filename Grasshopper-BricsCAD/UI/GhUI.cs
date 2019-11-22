using Grasshopper;
using System.Windows.Forms;
using System.Linq;
using System.IO;

namespace GH_BC
{
  public class GhUI
  {
    public static string InputGeometry = " Input geometry";
    public static string BimData = "BIM Data";
    public static string BuildingElements = "Building Elements";
    public static string Information = "Information";
    public static string Output = "Output";
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
              string directory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
              System.Diagnostics.Process.Start(@Path.Combine(directory, "Samples"));
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
        linkButton.Click += (s, e) => { PlugIn.RelinkToDoc(Bricscad.ApplicationServices.Application.DocumentManager.MdiActiveDocument); };
        toolbar.Items.Add(linkButton);

        var bakeSelectedButton = new ToolStripButton(Properties.Resources.bake)
        {
          ToolTipText = "Bakes the geometry from the selected Bake Geometry and Building Element components"
        };
        bakeSelectedButton.Click += (s, e) => { BakeComponent.BakeSelectedComponents(); };
        toolbar.Items.Add(bakeSelectedButton);
      }
    }
  }
}
