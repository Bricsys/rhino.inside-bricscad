using System;
using System.Drawing;
using System.Windows.Forms;
using _BcAp = Bricscad.ApplicationServices;

namespace GH_BC.UI
{
  partial class GhDefinitionDialog : Form
  {
    private ContextMenuStrip strip = new ContextMenuStrip();
    public GhDefinitionDialog()
    {
      InitializeComponent();

      Icon = Icon.ExtractAssociatedIcon(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
      var lookUp = new ToolStripMenuItem
      {
        Text = "Reload"
      };
      lookUp.Click += OnReload;
      strip.Items.Add(lookUp);
      LoadData();
    }
    private void OnMouseDown(object sender, MouseEventArgs e)
    {
      if (e.Button == MouseButtons.Right)
      {
        var hti = dataGridView1.HitTest(e.X, e.Y);
        dataGridView1.ClearSelection();
        if (hti.RowIndex > -1)
        {
          dataGridView1.Rows[hti.RowIndex].Selected = true;
          strip.Show(dataGridView1, e.X, e.Y);
        }
      }
    }
    private void LoadData()
    {
      dataGridView1.Rows.Clear();
      var docExt = GhBcConnection.GrasshopperDataExtension.GrasshopperDataManager(_BcAp.Application.DocumentManager.MdiActiveDocument, false);
      if (docExt == null)
        return;

      foreach (var data in docExt.DefinitionManager.LoadedDefinitions)
        dataGridView1.Rows.Add(data.Key, data.Value);
    }
    private void OnReload(object sender, EventArgs e)
    {
      var docExt = GhBcConnection.GrasshopperDataExtension.GrasshopperDataManager(_BcAp.Application.DocumentManager.MdiActiveDocument, false);
      if (docExt == null)
        return;

      foreach (DataGridViewRow row in dataGridView1.SelectedRows)
      {
        var val = row.Cells[0].Value as string;
        docExt.DefinitionManager.Reload(val);
      }
    }
  }
}
