using System;

namespace GH_BC.UI
{
  public partial class BakeDialog : System.Windows.Forms.Form
  {
    private System.Windows.Forms.ComboBox LayerBox;
    private System.Windows.Forms.ComboBox MaterialBox;
    private System.Windows.Forms.Button ColorButton;
    private System.Windows.Forms.Button OkButton;
    private System.Windows.Forms.Button CancelB;
    private System.Windows.Forms.Label MaterialLabel;
    private System.Windows.Forms.ToolTip MaterialTooltip;
    private System.ComponentModel.IContainer components;
    public Teigha.Colors.Color Color { get; private set; }
    public string Layer { get; private set; }
    public string Material { get; private set; }
    public BakeDialog()
    {
      InitializeComponent();

      Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
      var database = Bricscad.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Database;
      DatabaseUtils.GetLayers(database).ForEach(layer => LayerBox.Items.Add(layer));
      LayerBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      LayerBox.SelectedIndex = 0;

      DatabaseUtils.GetMaterials(database).ForEach(material => MaterialBox.Items.Add(material));      
      MaterialBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;      
      MaterialBox.SelectedIndex = 0;

      Color = Teigha.Colors.Color.FromDictionaryName("ByLayer");
      this.CancelButton = CancelB;
      this.AcceptButton = OkButton;
    }

    private void InitializeComponent()
    {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.Label label1;
            System.Windows.Forms.Label label3;
            System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
            this.LayerBox = new System.Windows.Forms.ComboBox();
            this.MaterialBox = new System.Windows.Forms.ComboBox();
            this.ColorButton = new System.Windows.Forms.Button();
            this.OkButton = new System.Windows.Forms.Button();
            this.CancelB = new System.Windows.Forms.Button();
            this.MaterialTooltip = new System.Windows.Forms.ToolTip(this.components);
            label1 = new System.Windows.Forms.Label();
            this.MaterialLabel = new System.Windows.Forms.Label();
            label3 = new System.Windows.Forms.Label();
            tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            label1.Anchor = System.Windows.Forms.AnchorStyles.None;
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(34, 11);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(48, 17);
            label1.TabIndex = 0;
            label1.Text = "Layer:";
            // 
            // label3
            // 
            label3.Anchor = System.Windows.Forms.AnchorStyles.None;
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(36, 91);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(45, 17);
            label3.TabIndex = 2;
            label3.Text = "Color:";
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 2;
            tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            tableLayoutPanel1.Controls.Add(label1, 0, 0);
            tableLayoutPanel1.Controls.Add(this.MaterialLabel, 0, 1);
            tableLayoutPanel1.Controls.Add(label3, 0, 2);
            tableLayoutPanel1.Controls.Add(this.LayerBox, 1, 0);
            tableLayoutPanel1.Controls.Add(this.MaterialBox, 1, 1);
            tableLayoutPanel1.Controls.Add(this.ColorButton, 1, 2);
            tableLayoutPanel1.Controls.Add(this.OkButton, 0, 3);
            tableLayoutPanel1.Controls.Add(this.CancelB, 1, 3);
            tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 4;
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            tableLayoutPanel1.Size = new System.Drawing.Size(234, 161);
            tableLayoutPanel1.TabIndex = 0;
            // 
            // MaterialLabel
            // 
            this.MaterialLabel.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.MaterialLabel.AutoSize = true;
            this.MaterialLabel.Location = new System.Drawing.Point(27, 51);
            this.MaterialLabel.Name = "MaterialLabel";
            this.MaterialLabel.Size = new System.Drawing.Size(62, 17);
            this.MaterialLabel.TabIndex = 1;
            this.MaterialLabel.Text = "Material:";
            this.MaterialLabel.MouseHover += new System.EventHandler(this.MaterialLabel_Hover);
            // 
            // LayerBox
            // 
            this.LayerBox.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.LayerBox.FormattingEnabled = true;
            this.LayerBox.Location = new System.Drawing.Point(126, 7);
            this.LayerBox.Name = "LayerBox";
            this.LayerBox.Size = new System.Drawing.Size(98, 24);
            this.LayerBox.TabIndex = 3;
            // 
            // MaterialBox
            // 
            this.MaterialBox.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.MaterialBox.FormattingEnabled = true;
            this.MaterialBox.Location = new System.Drawing.Point(126, 47);
            this.MaterialBox.Name = "MaterialBox";
            this.MaterialBox.Size = new System.Drawing.Size(98, 24);
            this.MaterialBox.TabIndex = 4;
            this.MaterialBox.MouseHover += new System.EventHandler(this.MaterialBox_Hover);
            // 
            // ColorButton
            // 
            this.ColorButton.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.ColorButton.Location = new System.Drawing.Point(126, 88);
            this.ColorButton.Name = "ColorButton";
            this.ColorButton.Size = new System.Drawing.Size(98, 23);
            this.ColorButton.TabIndex = 5;
            this.ColorButton.Text = "ByLayer";
            this.ColorButton.UseVisualStyleBackColor = true;
            this.ColorButton.Click += new System.EventHandler(this.ColorButton_Click);
            // 
            // OkButton
            // 
            this.OkButton.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.OkButton.Location = new System.Drawing.Point(21, 129);
            this.OkButton.Name = "OkButton";
            this.OkButton.Size = new System.Drawing.Size(75, 23);
            this.OkButton.TabIndex = 6;
            this.OkButton.Text = "Ok";
            this.OkButton.UseVisualStyleBackColor = true;
            this.OkButton.Click += new System.EventHandler(this.OkButton_Click);
            // 
            // CancelButton
            // 
            this.CancelB.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.CancelB.Location = new System.Drawing.Point(138, 129);
            this.CancelB.Name = "CancelButton";
            this.CancelB.Size = new System.Drawing.Size(75, 23);
            this.CancelB.TabIndex = 7;
            this.CancelB.Text = "Cancel";
            this.CancelB.UseVisualStyleBackColor = true;
            // 
            // BakeDialog
            // 
            this.ClientSize = new System.Drawing.Size(234, 161);
            this.Controls.Add(tableLayoutPanel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "BakeDialog";
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

    }

    private void ColorButton_Click(object sender, EventArgs e)
    {
      var dialog = new Bricscad.Windows.ColorDialog();
      var dialogResult = dialog.ShowDialog();
      if (dialogResult == System.Windows.Forms.DialogResult.OK)
      {
        Color = dialog.Color;
        if (dialog.Color.IsByBlock)
        {
          ColorButton.Text = "ByBlock";
          ColorButton.BackColor = System.Drawing.SystemColors.ButtonFace;
        }
        else if (dialog.Color.IsByLayer)
        {
          ColorButton.Text = "ByLayer";
          ColorButton.BackColor = System.Drawing.SystemColors.ButtonFace;
        }
        else
        {
          ColorButton.Text = "";
          ColorButton.BackColor = Color.ColorValue;
        }
      }
    }

    private void OkButton_Click(object sender, EventArgs e)
    {      
      Layer = LayerBox.SelectedItem as string;
      Material = MaterialBox.SelectedItem as string;
      DialogResult = System.Windows.Forms.DialogResult.OK;
    }

    private void MaterialLabel_Hover(object sender, EventArgs e)
    {
      MaterialTooltip.Show("Material to use if not set in the bake component", MaterialLabel);
    }

    private void MaterialBox_Hover(object sender, EventArgs e)
    {
      MaterialTooltip.Show("Material to use if not set in the bake component", MaterialBox);
    }
  }
}
