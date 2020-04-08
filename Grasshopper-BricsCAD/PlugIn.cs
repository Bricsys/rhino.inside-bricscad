using Bricscad.ApplicationServices;
using Rhino.Runtime.InProcess;
using System.IO;
using System.Reflection;
using System;
using Teigha.Runtime;
using System.Collections.Generic;
using System.Linq;
using _OdDb = Teigha.DatabaseServices;

// This line is not mandatory, but improves loading performances
[assembly: ExtensionApplication(typeof(GH_BC.PlugIn))]

namespace GH_BC
{
  public class PlugIn : IExtensionApplication
  {
    private static bool _grasshopperLoaded = false;
    private static bool _neewRedraw = true;
    private GrasshopperPreview _preview = null;
    static readonly string _rhinoPath = (string) Microsoft.Win32.Registry.GetValue
    (
      @"HKEY_LOCAL_MACHINE\SOFTWARE\McNeel\Rhinoceros\7.0\Install", "Path",
      Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Rhino WIP", "System")
    );
    public static Document LinkedDocument { get; private set; }
    static public PlugIn Instance { get; private set; }

    static List<_OdDb.Handle> _modified = new List<_OdDb.Handle>();
    static List<_OdDb.Handle> _erased = new List<_OdDb.Handle>();
    static List<_OdDb.Handle> _appended = new List<_OdDb.Handle>();
    static List<string> _commands = new List<string>();

    static HashSet<string> _commandToExpire = new HashSet<string>(){ "BIMSPATIALLOCATIONS" };

    #region Plugin static constructor
    static PlugIn()
    {
      // Add an assembly resolver for RhinoCommon.dll
      ResolveEventHandler OnRhinoCommonResolve = null;
      AppDomain.CurrentDomain.AssemblyResolve += OnRhinoCommonResolve = (sender, args) =>
      {
        const string RhinoCommonAssemblyName = "RhinoCommon";
        var assembly_name = new AssemblyName(args.Name).Name;

        if (assembly_name != RhinoCommonAssemblyName)
          return null;

        AppDomain.CurrentDomain.AssemblyResolve -= OnRhinoCommonResolve;
        return Assembly.LoadFrom(Path.Combine(_rhinoPath, RhinoCommonAssemblyName + ".dll"));
      };
#if DEBUG
      ResolveEventHandler OnGrasshopperResolve = null;
      AppDomain.CurrentDomain.AssemblyResolve += OnGrasshopperResolve = (sender, args) =>
      {
        const string GrasshopperCommonAssemblyName = "Grasshopper";
        var assembly_name = new AssemblyName(args.Name).Name;

        if (assembly_name != GrasshopperCommonAssemblyName)
          return null;

        AppDomain.CurrentDomain.AssemblyResolve -= OnGrasshopperResolve;
        var parDir = Directory.GetParent(Directory.GetParent(_rhinoPath).FullName).FullName;
        var path = Path.Combine(parDir, "Plug-ins", "Grasshopper", GrasshopperCommonAssemblyName + ".dll");
        return Assembly.LoadFrom(path);
      };
#endif
    }

    #endregion // Plugin static constructor

    #region IExtensionApplication Members

    public void Initialize()
    {
      Instance = this;
      var editor = Application.DocumentManager.MdiActiveDocument.Editor;
      if (!Rhinoceros.Startup())
      {
        editor.WriteMessage("\nFailed to start Rhino WIP");
        return;
      }
      var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
      editor.WriteMessage($"\nGrasshopper-BricsCAD Connection {version}");
      Application.Idle += onIdle;
      Application.DocumentManager.DocumentDestroyed += OnBcDocDestroyed;
    }
    public void Terminate()
    {
      Application.Idle -= onIdle;
      Application.DocumentManager.DocumentDestroyed -= OnBcDocDestroyed;
      Rhinoceros.Shutdown();
      try
      {
        _preview?.Dispose();
      }
      catch
      {
        // ignored
      }
    }

    #endregion // IExtensionApplication Members
    public void onIdle(object sender, EventArgs e)
    {
      OnDocumentChanged();
      if (_preview != null)
      {
        _preview.Init();
        if (_neewRedraw || _preview.SelectionPreviewChanged())
        {
          _neewRedraw = false;
          _preview.BuildScene();
          if (LinkedDocument == Application.DocumentManager.MdiActiveDocument)
            LinkedDocument.Editor.UpdateScreen();
        }
      }
    }

    public static void SetNeetRedraw() { _neewRedraw = true; }

    static void OnObjectModified(object sender, _OdDb.ObjectEventArgs e)
    {
      var objId = e.DBObject.ObjectId;
      if (objId.ObjectClass.IsDerivedFrom(RXObject.GetClass(typeof(_OdDb.Entity))))
        _modified.Add(e.DBObject.ObjectId.Handle);
    }
    static void OnObjectErased(object sender, _OdDb.ObjectErasedEventArgs e)
    {
      var obj = e.DBObject;
      (obj.IsErased ? _erased : _appended).Add(e.DBObject.ObjectId.Handle);
    }
    static void OnObjectAppended(object sender, _OdDb.ObjectEventArgs e)
    {
      var objId = e.DBObject.ObjectId;
      if (objId.ObjectClass.IsDerivedFrom(RXObject.GetClass(typeof(_OdDb.Entity))))
        _appended.Add(e.DBObject.ObjectId.Handle);
    }
    static void OnCommandEnded(object sender, CommandEventArgs e) => _commands.Add(e.GlobalCommandName);
    public static void RelinkToDoc(Document document)
    {
      if (LinkedDocument == document)
        return;

      if (LinkedDocument != null)
      {
        LinkedDocument.CloseWillStart -= OnBcDocCloseWillStart;
        LinkedDocument.Database.ObjectModified -= OnObjectModified;
        LinkedDocument.Database.ObjectErased -= OnObjectErased;
        LinkedDocument.Database.ObjectAppended -= OnObjectAppended;
        LinkedDocument.CommandEnded -= OnCommandEnded;
      }

      LinkedDocument = document;
      LinkedDocument.CloseWillStart += OnBcDocCloseWillStart;
      LinkedDocument.Database.ObjectModified += OnObjectModified;
      LinkedDocument.Database.ObjectErased += OnObjectErased;
      LinkedDocument.Database.ObjectAppended += OnObjectAppended;
      LinkedDocument.CommandEnded += OnCommandEnded;

      Instance._preview?.Dispose();
      Instance._preview = new GrasshopperPreview();

      ExpireGH();
      SetNeetRedraw();
    }
    private static void ExpireGH()
    {
      foreach (Grasshopper.Kernel.GH_Document definition in Grasshopper.Instances.DocumentServer)
      {
        bool expired = false;
        foreach (var obj in definition.Objects.OfType<IGH_BcParam>())
        {
          expired = true;
          obj.ExpireSolution(false);
        }
        foreach (var obj in definition.Objects.OfType<IGH_BcComponent>())
        {
          expired = true;
          obj.ExpireSolution(false);
        }
        if (expired)
          definition.NewSolution(false);
      }
    }
    private static void OnBcDocDestroyed(object sender, DocumentDestroyedEventArgs e)
    {
      if (LinkedDocument == null)
        ExpireGH();
    }
    protected static void OnBcDocCloseWillStart(object sender, EventArgs e)
    {
      LinkedDocument.CloseWillStart -= OnBcDocCloseWillStart;
      LinkedDocument = null;
      Instance?._preview.Dispose();
      Instance._preview = null;
    }
    public static bool LoadGrasshopperComponents()
    {
      if (_grasshopperLoaded)
        return true;

      var LoadGHAProc = Grasshopper.Instances.ComponentServer.GetType().GetMethod("LoadGHA", BindingFlags.NonPublic | BindingFlags.Instance);
      if (LoadGHAProc == null)
        return false;

      var bCoff = Grasshopper.Instances.Settings.GetValue("Assemblies:COFF", true);
      Grasshopper.Instances.Settings.SetValue("Assemblies:COFF", false);

      var rc = (bool) LoadGHAProc.Invoke
      (
        Grasshopper.Instances.ComponentServer,
        new object[] { new Grasshopper.Kernel.GH_ExternalFile(Assembly.GetExecutingAssembly().Location), false }
      );

      Grasshopper.Instances.Settings.SetValue("Assemblies:COFF", bCoff);

      if (rc)
        Grasshopper.Kernel.GH_ComponentServer.UpdateRibbonUI();

      var GrasshopperGuid = new Guid(0xB45A29B1, 0x4343, 0x4035, 0x98, 0x9E, 0x04, 0x4E, 0x85, 0x80, 0xD9, 0xCF);
      Rhino.PlugIns.PlugIn.LoadPlugIn(GrasshopperGuid);
      _grasshopperLoaded = true;
      return rc;
    }

    static void OnDocumentChanged()
    {
      if (_commands.Count != 0)
        _commands = _commands.Intersect(_commandToExpire).ToList();

      if (_erased.Count == 0 && _modified.Count == 0 && _appended.Count == 0 && _commands.Count == 0)
        return;

      foreach (Grasshopper.Kernel.GH_Document definition in Grasshopper.Instances.DocumentServer)
      {
        bool expireNow = Grasshopper.Kernel.GH_Document.EnableSolutions &&
                         Grasshopper.Instances.ActiveCanvas.Document == definition &&
                         definition.Enabled &&
                         definition.SolutionState != Grasshopper.Kernel.GH_ProcessStep.Process;
        bool objExpired = false;
        foreach (var obj in definition.Objects)
        {
          if (obj is IGH_BcParam persistentParam)
          {
            if (persistentParam.DataType == Grasshopper.Kernel.GH_ParamData.remote)
              continue;

            if (persistentParam.Phase == Grasshopper.Kernel.GH_SolutionPhase.Blank)
              continue;

            if (persistentParam.NeedsToBeExpired(_modified, _erased, _appended, _commands))
            {
              persistentParam.ExpireSolution(false);
              objExpired = true;
            }
          }
          else if (obj is IGH_BcComponent persistentComponent)
          {
            if (persistentComponent.NeedsToBeExpired(_modified, _erased, _appended, _commands))
            {
              persistentComponent.ExpireSolution(false);
              objExpired = true;
            }
          }
        }

        if (expireNow && objExpired)
          definition.NewSolution(false);
      }
      _erased.Clear();
      _modified.Clear();
      _appended.Clear();
      _commands.Clear();
    }
  }
}
