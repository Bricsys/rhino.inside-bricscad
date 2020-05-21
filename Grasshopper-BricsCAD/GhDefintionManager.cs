using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using _BcAp = Bricscad.ApplicationServices;
using _OdRx = Teigha.Runtime;
using _OdDb = Teigha.DatabaseServices;
using GH_BC.Visualization;

namespace GH_BC
{
  class GhDefinitionManager
  {
    private Dictionary<string, string> _nameToPath = new Dictionary<string, string>();
    private Dictionary<string, GH_Archive> _docs = new Dictionary<string, GH_Archive>();
    public IEnumerable<KeyValuePair<string, string>> LoadedDefinitions => _nameToPath.AsEnumerable();
    public GH_Document Definition(string fileName)
    {
      var filePath = FindFile(fileName, new string[] { });
      if (string.IsNullOrEmpty(filePath))
        return null;

      if (_docs.TryGetValue(filePath, out var doc))
      {
        var definition = new GH_Document();
        if (doc.ExtractObject(definition, "Definition"))
          return definition;
        definition?.Dispose();
      }
      return null;
    }
    public void Reload(string defName)
    {
      var filePath = FindFile(defName, new string[] { });
      if (!string.IsNullOrEmpty(filePath))
      {
        var doc = ReadFromFile(filePath);
        _docs[filePath] = doc;
        Reloaded?.Invoke(this, defName);
      }
    }
    public void Load(string fileName, string[] extraSearchPath)
    {
      var filePath = FindFile(fileName, extraSearchPath);
      if (string.IsNullOrEmpty(filePath))
        return;

      if (!_docs.ContainsKey(filePath))
      {
        var doc = ReadFromFile(filePath);
        if (doc != null)
          _docs[filePath] = doc;
      }
    }
    private static GH_Archive ReadFromFile(string filePath)
    {
      try
      {
        var archive = new GH_Archive();
        archive.ReadFromFile(filePath);
        return archive;
      }
      catch (Exception)
      {
        return null;
      }
    }
    private string FindFile(string fileName, string[] extraSearchPath)
    {
      if (_nameToPath.TryGetValue(fileName, out var keptPath))
        return keptPath;

      foreach (var extPath in extraSearchPath)
      {
        string foundPath = Path.Combine(extPath, fileName);
        if (File.Exists(foundPath))
        {
          _nameToPath[fileName] = foundPath;
          return foundPath;
        }
      }

      var srchPath = _BcAp.Application.GetSystemVariable("SRCHPATH") as string;
      var paths = srchPath.Split(';');
      foreach (var path in paths)
      {
        var filePath = Path.Combine(path, fileName);
        if (File.Exists(filePath))
        {
          _nameToPath[fileName] = filePath;
          return filePath;
        }
      }
      return null;
    }
    public event EventHandler<string> Reloaded;
  }
  class GhDataManager
  {
    private string DwgPath => Path.GetDirectoryName(Document.Name);
    private Dictionary<_OdDb.ObjectId, CompoundDrawable> _grasshopperData = new Dictionary<_OdDb.ObjectId, CompoundDrawable>();
    private HashSet<_OdDb.ObjectId> _toUpdate = new HashSet<_OdDb.ObjectId>();
    public _BcAp.Document Document { get; private set; }
    public GhDefinitionManager DefinitionManager { get; private set; }
    public bool NeedHardUpdate { get; set; }
    public bool NeedSoftUpdate { get; set; }
    public GhDataManager(_BcAp.Document doc)
    {
      Document = doc;
      DefinitionManager = new GhDefinitionManager();
      DefinitionManager.Reloaded += (s, def) =>
      {
        using (var transaction = Document.Database.TransactionManager.StartTransaction())
        {
          foreach (var it in _grasshopperData)
          {
            var ghDataId = it.Key;
            var ghData = transaction.GetObject(ghDataId, _OdDb.OpenMode.ForRead) as GrasshopperData;
            if (ghData == null)
              continue;
            _toUpdate.Add(ghDataId);
          }
        }
      };
      var database = doc.Database;
      using (var transaction = database.TransactionManager.StartTransaction())
      {
        var blockTable = transaction.GetObject(database.BlockTableId, _OdDb.OpenMode.ForRead) as _OdDb.BlockTable;
        var searchPath = new string[] { DwgPath };
        foreach (var blockId in blockTable)
        {
          var block = transaction.GetObject(blockId, _OdDb.OpenMode.ForRead) as _OdDb.BlockTableRecord;
          foreach (var id in block)
          {
            var entity = transaction.GetObject(id, _OdDb.OpenMode.ForRead) as _OdDb.Entity;
            var ghDataId = GrasshopperData.GetGrasshopperData(entity);
            if (ghDataId.IsNull)
              continue;

            var ghData = transaction.GetObject(ghDataId, _OdDb.OpenMode.ForRead) as GrasshopperData;
            DefinitionManager.Load(ghData.Definition, searchPath);
            _toUpdate.Add(ghDataId);
          }
        }
        transaction.Commit();
      }
      EnableReactors();
    }
    public CompoundDrawable Drawable(_OdDb.ObjectId id)
    {
      if (_grasshopperData.TryGetValue(id, out var drawable))
        return drawable;
      return null;
    }
    public void Proccess()
    {
      DisableReactors();
      if (NeedHardUpdate)
      {
        foreach (var pair in _grasshopperData)
          _toUpdate.Add(pair.Key);
      }
      else if (NeedSoftUpdate)
      {
        var ghColor = GhDataSettings.Color;
        using (var transaction = Document.Database.TransactionManager.StartTransaction())
        {
          foreach (var pair in _grasshopperData)
          {
            pair.Value.Color = ghColor;
            pair.Value.ColorSelected = ghColor;
            if (!_toUpdate.Contains(pair.Key))
            {
              var ghData = transaction.GetObject(pair.Key, _OdDb.OpenMode.ForRead) as GrasshopperData;
              var hostEnt = transaction.GetObject(ghData.HostEntity, _OdDb.OpenMode.ForWrite) as _OdDb.Entity;
              hostEnt?.RecordGraphicsModified(true);
            }
          }
          transaction.Commit();
        }
      }

      if (_toUpdate.Count != 0)
      {
        var saveDoc = GhDrawingContext.LinkedDocument;
        GhDrawingContext.LinkedDocument = Document; //all bc components use it for computation

        using (var transaction = Document.Database.TransactionManager.StartTransaction())
        {
          using (var form = new UI.ModalForm())
          {
            foreach (var ghDataId in _toUpdate)
            {
              var ghData = transaction.GetObject(ghDataId, _OdDb.OpenMode.ForRead) as GrasshopperData;
              if (ghData == null)
                continue;

              UpdateDrawable(ghData);
              var hostEnt = transaction.GetObject(ghData.HostEntity, _OdDb.OpenMode.ForWrite) as _OdDb.Entity;
              hostEnt?.RecordGraphicsModified(true);
            }
          }
          transaction.Commit();
        }

        GhDrawingContext.LinkedDocument = saveDoc;
      }

      _toUpdate.Clear();
      NeedHardUpdate = false;
      NeedSoftUpdate = false;
      EnableReactors();
    }
    public bool AddGrasshopperData(GrasshopperData grasshopperData)
    {
      var objId = grasshopperData.ObjectId;
      if (objId.IsNull || _grasshopperData.ContainsKey(objId))
        return false;

      DefinitionManager.Load(grasshopperData.Definition, new string[] { DwgPath });
      var definition = DefinitionManager.Definition(grasshopperData.Definition);
      if (definition == null)
        return false;

      foreach (var param in GrasshopperPlayer.GetInputParametersValues(definition))
      {
        if (param.Item2 is Type type)
          grasshopperData.AddProperty(param.Item1, type);
        else
          grasshopperData.AddProperty(param.Item1, param.Item2);
      }
      definition.Dispose();
      return true;
    }
    public void Bake(List<_OdDb.ObjectId> ghDataIds, UI.BakeDialog bakeProperties)
    {
      DisableReactors();
      var saveDoc = GhDrawingContext.LinkedDocument;
      GhDrawingContext.LinkedDocument = Document;
      using (var transaction = Document.Database.TransactionManager.StartTransaction())
      {
        using (var form = new UI.ModalForm())
        {
          foreach (var ghDataId in ghDataIds)
          {
            var ghData = transaction.GetObject(ghDataId, _OdDb.OpenMode.ForRead) as GrasshopperData;
            if (ghData == null)
              continue;

            var definition = DefinitionManager.Definition(ghData.Definition);
            if (definition == null)
              continue;
            
            foreach (var obj in definition.Objects.OfType<Components.BakeComponent>())
            {
              if (!obj.Locked)
                Components.BakeComponent.Expire(obj, bakeProperties);
            }
            GrasshopperPlayer.Run(definition, ghData, Document);
          }
        }
        transaction.Commit();
      }
      GhDrawingContext.LinkedDocument = saveDoc;
      EnableReactors();
    }
    private void UpdateDrawable(GrasshopperData grasshopperData)
    {
      if (!grasshopperData.IsVisible)
        return;

      var definition = DefinitionManager.Definition(grasshopperData.Definition);
      if (definition == null)
        return;

      GrasshopperPlayer.Run(definition, grasshopperData, Document);
      var newDrawable = new CompoundDrawable
      {
        Color = GhDataSettings.Color,
        ColorSelected = GhDataSettings.Color,
        IsRenderMode = GhDataSettings.VisualStyle == GH_PreviewMode.Shaded
      };
      GrasshopperPreview.GetPreview(definition, newDrawable);
      definition.Dispose();
      _grasshopperData[grasshopperData.ObjectId] = newDrawable;
    }
    #region DbObjects reactors
    private void EnableReactors()
    {
      Document.Database.ObjectModified += OnObjectModified;
      Document.Database.ObjectErased += OnObjectErased;
    }
    private void DisableReactors()
    {
      Document.Database.ObjectErased -= OnObjectErased;
      Document.Database.ObjectModified -= OnObjectModified;
    }
    private void OnObjectModified(object sender, _OdDb.ObjectEventArgs e)
    {
      var objId = e.DBObject.ObjectId;
      if (objId.ObjectClass.IsDerivedFrom(_OdRx.RXObject.GetClass(typeof(GrasshopperData))))
        _toUpdate.Add(objId);
      else if (e.DBObject is _OdDb.Entity ent)
      {
        var id = GrasshopperData.GetGrasshopperData(ent);
        if (!id.IsNull)
          _toUpdate.Add(id);
      }
      else if (e.DBObject is _OdDb.BlockTableRecord btr)
      {
        foreach (_OdDb.ObjectId refId in btr.GetBlockReferenceIds(true, false))
        {
          using (var blockRef = refId.GetObject(_OdDb.OpenMode.ForRead) as _OdDb.Entity)
          {
            var id = GrasshopperData.GetGrasshopperData(blockRef);
            if (!id.IsNull)
              _toUpdate.Add(id);
          }
        }
      }
    }
    private void OnObjectErased(object sender, _OdDb.ObjectErasedEventArgs e)
    {
      var obj = e.DBObject;
      var ghId = _OdDb.ObjectId.Null;
      if (obj is GrasshopperData)
        ghId = obj.ObjectId;
      else if(obj is _OdDb.Entity ent)
        ghId = GrasshopperData.GetGrasshopperData(ent);

      if(!ghId.IsNull)
      {
        if (obj.IsErased)
          _grasshopperData.Remove(obj.ObjectId);
        else
          _toUpdate.Add(obj.ObjectId);
      }
    }
    #endregion
  }
  class GhDataSettings : _BcAp.Settings
  {
    public GhDataSettings()
      : base("Grasshopper", Path.Combine(GhBcConnection.DllPath, "grasshopper_settings.xml"))
    {}
    public static GH_PreviewMode VisualStyle
    {
      get
      {
        short val = (short) _BcAp.Application.GetSystemVariable("GhVisualStyle");
        return (GH_PreviewMode) val;
      }
    }
    public static Rhino.Geometry.MeshingParameters MeshParamsParameters
    {
      get
      {
        int val = (int) _BcAp.Application.GetSystemVariable("GhMeshQuality");
        switch (val)
        {
          case 0: return Rhino.Geometry.MeshingParameters.FastRenderMesh;
          case 2: return Rhino.Geometry.MeshingParameters.QualityRenderMesh;
          default:
            return Rhino.Geometry.MeshingParameters.Default;
        }
      }
    }
    public static short HostTransparency => (short) _BcAp.Application.GetSystemVariable("GhHostTransparency");
    public override bool Set(string VarName, object VarValue) 
    {
      Changed?.Invoke(this, VarName);
      return base.Set(VarName, VarValue);
    }
    public static System.Drawing.Color Color
    {
      get
      {
        short a = (short) _BcAp.Application.GetSystemVariable("GhTransparency");
        int r = 0, g = 0, b = 0;
        var color = _BcAp.Application.GetSystemVariable("GhColor") as string;

        var m = new System.Text.RegularExpressions.Regex("RGB:(\\d+),(\\d+),(\\d+)").Match(color);
        if (m.Success)
        {
          r = int.Parse(m.Groups[1].Value);
          g = int.Parse(m.Groups[2].Value);
          b = int.Parse(m.Groups[3].Value);
        }
        else if (short.TryParse(color, out short intVal))
        {
          var tColor = Teigha.Colors.Color.FromColorIndex(Teigha.Colors.ColorMethod.ByAci, intVal);
          r = tColor.Red;
          g = tColor.Green;
          b = tColor.Blue;
        }
        else if (color[0] == '#')
        {
          r = int.Parse(color.Substring(1, 2), System.Globalization.NumberStyles.HexNumber);
          g = int.Parse(color.Substring(3, 2), System.Globalization.NumberStyles.HexNumber);
          b = int.Parse(color.Substring(5, 2), System.Globalization.NumberStyles.HexNumber);
        }
        else
        {
          var baseCol = System.Drawing.Color.FromName(color);
          if (baseCol != null)
            return System.Drawing.Color.FromArgb(a, baseCol);
        }
        
        return System.Drawing.Color.FromArgb(a, r, g, b);
      }
    }
    public event EventHandler<string> Changed;
  }
  class GhDataExtension
  {
    private Dictionary<_BcAp.Document, GhDataManager> _ghManMap = new Dictionary<_BcAp.Document, GhDataManager>();
    private GhDataSettings _ghSettings = new GhDataSettings();
    private GhDataOverrule _overrule = new GhDataOverrule();
    public GhDataExtension() {}
    public void Initialize()
    {
      _BcAp.Application.DocumentManager.DocumentToBeDestroyed += OnBcDocDestroyed;
      _BcAp.Application.DocumentManager.DocumentCreated += OnBcDocCreated;
      _BcAp.Settings.Register(_ghSettings);
      RegisterOverrule();
      _ghSettings.Changed += SettingsChanged;

      foreach (_BcAp.Document doc in _BcAp.Application.DocumentManager)
      {
        //when opened on proxy detection, doc throw exception on Database property
        bool isValidDoc = false;
        try { isValidDoc = doc.Database != null; } catch (Exception) { }
        if (isValidDoc)
          GrasshopperDataManager(doc, true);
      }
    }
    public void Terminate()
    {
      UnregisterOverrule();
      _BcAp.Settings.Unregister(_ghSettings);
      _BcAp.Application.DocumentManager.DocumentCreated -= OnBcDocCreated;
      _BcAp.Application.DocumentManager.DocumentToBeDestroyed -= OnBcDocDestroyed;
    }
    public GhDataManager GrasshopperDataManager(_OdDb.Database database, bool createIfNotExist = false)
    {
      var bcDoc = DatabaseUtils.FindDocument(database);
      return GrasshopperDataManager(bcDoc, createIfNotExist);
    }
    public GhDataManager GrasshopperDataManager(_BcAp.Document doc, bool createIfNotExist = false)
    {
      if (doc == null)
        return null;

      if (_ghManMap.TryGetValue(doc, out var ghMan))
        return ghMan;

      if (!createIfNotExist)
        return null;

      ghMan = new GhDataManager(doc);
      _ghManMap[doc] = ghMan;
      return ghMan;
    }
    public void Update(_BcAp.Document doc)
    {
      var docExt = GrasshopperDataManager(doc.Database);
      docExt?.Proccess();
    }
    private void SettingsChanged(object sender, string setting)
    {
      if (setting == "GhHostTransparency" ||
          setting == "GhTransparency" ||
          setting == "GhColor")
      {
        foreach (var entr in _ghManMap.Values)
          entr.NeedSoftUpdate = true;
      }
      else if (setting == "GhVisualStyle" ||
               setting == "GhMeshQuality")
      {
        foreach (var entr in _ghManMap.Values)
          entr.NeedHardUpdate = true;
      }
    }
    #region BcDoc reactors
    private void OnBcDocDestroyed(object sender, _BcAp.DocumentCollectionEventArgs e)
    {
      _ghManMap.Remove(e.Document);
    }
    private void OnBcDocCreated(object sender, _BcAp.DocumentCollectionEventArgs e)
    {
      _ghManMap[e.Document] = new GhDataManager(e.Document);
    }
    #endregion
    #region Overrule registration
    private void RegisterOverrule()
    {
      _OdRx.Overrule.AddOverrule(_OdRx.RXObject.GetClass(typeof(_OdDb.Entity)), _overrule, false);
      _OdRx.Overrule.Overruling = true;
    }
    private void UnregisterOverrule()
    {
      _OdRx.Overrule.RemoveOverrule(_OdRx.RXObject.GetClass(typeof(_OdDb.Entity)), _overrule);
      _OdRx.Overrule.Overruling = false;
      _overrule = null;
    }
    #endregion
  }
}
