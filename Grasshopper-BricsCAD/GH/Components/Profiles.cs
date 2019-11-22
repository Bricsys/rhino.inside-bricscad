using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System;

namespace GH_BC
{
  public class Profile : GH_Param<Types.Profile>
  {
    public Profile(GH_InstanceDescription nTag) : base(nTag) { }
    public Profile()
      : base(new GH_InstanceDescription("Profile", "Profile", "Represents a BricsCAD profile.", "BricsCAD", "Profile")) { }
    public override Guid ComponentGuid => new Guid("E6C7786A-AE55-468D-A4E2-1D65958CAA1C");
    public override GH_Exposure Exposure => GH_Exposure.hidden;
  }
  
  public class ProfileName : GH_ValueList
  {
    public ProfileName()
    {
      Category = "BricsCAD";
      SubCategory = GhUI.BimData;
      Name = "Profile Names";
      Description = "Provides a name picker for all the profiles present in Profiles in BricsCAD.";
      ListMode = GH_ValueListMode.DropDown;
      NickName = "Profile name";
    }
    public override Guid ComponentGuid => new Guid("23DD8D14-6248-4EB0-9EE5-B3CB89757517");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override Bitmap Icon => Properties.Resources.profilename;
    public void RefreshList()
    {
      var selectedItems = ListItems.Where(x => x.Selected).Select(x => x.Expression).ToList();
      ListItems.Clear();
      var profileNames = new HashSet<string>();
      var standards = Bricscad.Bim.BIMProfile.GetAllProfileStandards(null);
      foreach (var standard in standards)
      {
        foreach (var entry in Bricscad.Bim.BIMProfile.GetAllProfileNames(standard, null))
        {
          foreach (var profileName in entry.Value)
          {
            if (!profileNames.Contains(profileName))
            {
              var item = new GH_ValueListItem(profileName, "\"" + profileName + "\"");
              item.Selected = selectedItems.Contains(item.Expression);
              ListItems.Add(item);
              profileNames.Add(profileName);
            }
          }
        }
      }
    }
    protected override IGH_Goo InstantiateT() => new GH_String();
    protected override void CollectVolatileData_Custom()
    {
      RefreshList();
      base.CollectVolatileData_Custom();
    }
  }

  public class ProfileSizes : GH_Component
  {
    public ProfileSizes() : base("Profile Sizes", "PS", "Returns all the sizes attached to the input profile.", "BricsCAD", GhUI.BimData)
    { }
    public override Guid ComponentGuid => new Guid("58F684AF-BA42-4DB9-AF2C-60A7D18DEFD7");
    public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override Bitmap Icon => Properties.Resources.profilesize;
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager[pManager.AddTextParameter("ProfileName", "N", "Profile name", GH_ParamAccess.item)].Optional = true;
    }
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddTextParameter("ProfileSize", "S", "Profile size", GH_ParamAccess.list);
    }
    protected override void SolveInstance(IGH_DataAccess DA)
    {
      string profileName = null;
      if (!DA.GetData("ProfileName", ref profileName))
        return;

      var db = PlugIn.LinkedDocument.Database;
      var profileTypes = Enum.GetValues(typeof(Bricscad.Bim.ProfileType));
      var profileSizes = new List<string>();
      foreach (var standard in Bricscad.Bim.BIMProfile.GetAllProfileStandards(null))
      {
        foreach (Bricscad.Bim.ProfileType profileType in profileTypes)
        {
          foreach (var profileSize in Bricscad.Bim.BIMProfile.GetAllProfileSizes(standard, profileName, profileType, null))
          {
            profileSizes.Add(profileSize);
          }
        }
      }
      DA.SetDataList("ProfileSize", profileSizes);
    }
  }

  public class LibraryProfiles : GH_Component
  {
    public LibraryProfiles() : base("Library Profile", "LP", "Returns a profile from the library, according to the given name and size.", "BricsCAD", GhUI.Information)
    { }
    public override Guid ComponentGuid => new Guid("E7380308-C270-49FD-9E12-FE58FBC1236C");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override Bitmap Icon => Properties.Resources.profile;
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager[pManager.AddTextParameter("ProfileName", "N", "Profile name", GH_ParamAccess.item)].Optional = true;
      pManager[pManager.AddTextParameter("ProfileSize", "S", "Profile size", GH_ParamAccess.item)].Optional = true;
    }
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddParameter(new Profile(), "Profile", "P", "Library profile", GH_ParamAccess.list);
    }
    protected override void SolveInstance(IGH_DataAccess DA)
    {
      string profileName = null;
      string profileSize = null;
      DA.GetData("ProfileName", ref profileName);
      DA.GetData("ProfileSize", ref profileSize);
      if (profileName != null && profileSize != null)
      {
        var profileTypes = Enum.GetValues(typeof(Bricscad.Bim.ProfileType));
        var res = new List<Types.Profile>();
        foreach (var standard in Bricscad.Bim.BIMProfile.GetAllProfileStandards(null))
        {
          foreach (Bricscad.Bim.ProfileType profileType in profileTypes)
          {
            var profile = Bricscad.Bim.BIMProfile.GetProfile(standard, profileName, profileSize, profileType, null);
            if (profile.IsValid())
              res.Add(new Types.Profile(profile));
          }
        }
        DA.SetDataList("Profile", res);
      }
      else
      {
        var libProfiles = Bricscad.Bim.BIMProfile.GetAllLibraryProfiles(PlugIn.LinkedDocument.Database);
        var res = libProfiles.Where(profile => (profileName != null ? profile.GetName == profileName : true)
                                            && (profileSize != null ? profile.GetShape == profileSize : true))
                             .Select(profile => new Types.Profile(profile));
        DA.SetDataList("Profile", res);
      }
    }
  }

  public class ProfileInfo : GH_Component
  {
    public ProfileInfo() : base("Profile Info", "PI", "Returns the information (name, size, standard and curves) of the specified profile.", "BricsCAD", GhUI.Information)
    { }
    public override Guid ComponentGuid => new Guid("25ED8B1F-547E-448C-8637-CE56C3AA70D9");
    public override GH_Exposure Exposure => GH_Exposure.primary;
    protected override Bitmap Icon => Properties.Resources.profileinfo;
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddParameter(new Profile(), "Profile", "P", "Library profile", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddTextParameter("ProfileName", "N", "Profile name", GH_ParamAccess.item);
      pManager.AddTextParameter("ProfileSize", "S", "Profile size", GH_ParamAccess.item);
      pManager.AddTextParameter("ProfileStandard", "ST", "Profile standard", GH_ParamAccess.item);
      pManager.AddCurveParameter("ProfileCurves", "C", "Profile curves", GH_ParamAccess.tree);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      Types.Profile profile = null;
      if (!DA.GetData("Profile", ref profile))
        return;
      DA.SetData("ProfileName", profile.Value.GetName);
      DA.SetData("ProfileSize", profile.Value.GetShape);
      DA.SetData("ProfileStandard", profile.Value.GetStandard);
      var profileLoops = profile.Value.GetProfileCurves();
      var treeArray = new GH_Structure<GH_Curve>();
      for (int i = 0; i < profileLoops.Count; i++)
      {
        foreach(var curve in profileLoops[i])
        {
          var ghPath = new GH_Path(i);
          var ghCurve = new GH_Curve(curve.ToRhino());
          treeArray.Append(ghCurve, ghPath);
        }
      }
      DA.SetDataTree(3, treeArray);
    }
  }
}
