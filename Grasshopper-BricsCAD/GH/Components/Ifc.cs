using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System;
using _OdDb = Teigha.DatabaseServices;

namespace GH_BC
{
  public class IfcExport : GH_Component
  {
    public IfcExport() : base("IFC Export", "IFC", "Exports the specified building elements to IFC.", "BricsCAD", GhUI.Output)
    { }
    public override Guid ComponentGuid => new Guid("84402A64-B75B-4313-BAE4-0CA509154109");
    protected override Bitmap Icon => Properties.Resources.ifcexport;
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddParameter(new BcEntity(), "BuildingElement", "BE", "Building elements to be exported.", GH_ParamAccess.list);
      pManager.AddParameter(new Param_FilePath(), "FileName", "F", "File path to export.", GH_ParamAccess.item);
    }
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
    }
    protected override void SolveInstance(IGH_DataAccess DA)
    {
      var bcEnt = new List<Types.BcEntity>();
      string filepath = null;
      if (!DA.GetDataList("BuildingElement", bcEnt) ||
          !DA.GetData("FileName", ref filepath))
        return;
      var opt = new Bricscad.Ifc.IFCExportOptions();
      opt.ObjectsToExport = new _OdDb.ObjectIdCollection(bcEnt.Select(ent => ent.ObjectId).ToArray());
      var res = Bricscad.Ifc.IfcUtilityFunctions.ExportIfcFile(PlugIn.LinkedDocument, filepath, opt);
      if (res != Bricscad.Bim.BimResStatus.Ok)
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, string.Format("IFC export failed with error \"{0}\"", res.ToString()));
    }
  }
}
