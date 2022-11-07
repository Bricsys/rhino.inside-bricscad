using System;
using System.Collections.Generic;
using System.Linq;
using _BcAp = Bricscad.ApplicationServices;
using _OdRx = Teigha.Runtime;
using _OdDb = Teigha.DatabaseServices;

namespace GH_BC
{
  static class GhDrawingContext
  {
    static private List<_OdDb.Handle> _modified = new List<_OdDb.Handle>();
    static private List<_OdDb.Handle> _erased = new List<_OdDb.Handle>();
    static private List<_OdDb.Handle> _appended = new List<_OdDb.Handle>();
    static private List<string> _commands = new List<string>();
    static private Visualization.GrasshopperPreview _preview = null;
    static readonly HashSet<string> _commandToExpire = new HashSet<string>() { "BIMSPATIALLOCATIONS" };
    static public _BcAp.Document LinkedDocument { get; set; }
    static public bool NeedRedraw { get; set; }
    static public void Process()
    {
      OnDocumentChanged();
      if (_preview != null)
      {
        _preview.Init();
        if (NeedRedraw)
        {
          NeedRedraw = false;
          _preview.BuildScene();
          if (LinkedDocument == _BcAp.Application.DocumentManager.MdiActiveDocument)
            LinkedDocument.Editor.UpdateScreen();
        }
      }
    }
    static public void Initialize()
    {
      _BcAp.Application.DocumentManager.DocumentBecameCurrent += OnDocumentBecameCurrent;
    }
    static public void Terminate()
    {
      _BcAp.Application.DocumentManager.DocumentBecameCurrent -= OnDocumentBecameCurrent;
      _preview?.Dispose();
    }
    static public void RelinkToDoc(_BcAp.Document document)
    {
      if (LinkedDocument == document)
        return;

      if (LinkedDocument != null)
      {
        LinkedDocument.CommandEnded -= OnCommandEnded;
        LinkedDocument.Database.ObjectAppended -= OnObjectAppended;
        LinkedDocument.Database.ObjectErased -= OnObjectErased;
        LinkedDocument.Database.ObjectModified -= OnObjectModified;
        LinkedDocument.CloseWillStart -= OnBcDocCloseWillStart;
      }
      _preview?.Dispose();
      LinkedDocument = document;
      LinkedDocument.CloseWillStart += OnBcDocCloseWillStart;
      LinkedDocument.Database.ObjectModified += OnObjectModified;
      LinkedDocument.Database.ObjectErased += OnObjectErased;
      LinkedDocument.Database.ObjectAppended += OnObjectAppended;
      LinkedDocument.CommandEnded += OnCommandEnded;

      _preview = new Visualization.GrasshopperPreview();
      ExpireGH();
      NeedRedraw = true;
    }
    #region Bricscad reactors
    static void OnObjectModified(object sender, _OdDb.ObjectEventArgs e)
    {
      var objId = e.DBObject.ObjectId;
      if (objId.ObjectClass.IsDerivedFrom(_OdRx.RXObject.GetClass(typeof(_OdDb.Entity))))
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
      if (objId.ObjectClass.IsDerivedFrom(_OdRx.RXObject.GetClass(typeof(_OdDb.Entity))) ||
          objId.ObjectClass.IsDerivedFrom(_OdRx.RXObject.GetClass(typeof(_OdDb.Material))))
        _appended.Add(objId.Handle);
    }
    static void OnCommandEnded(object sender, _BcAp.CommandEventArgs e) => _commands.Add(e.GlobalCommandName);
    static void OnDocumentBecameCurrent(object sender, _BcAp.DocumentCollectionEventArgs e)
    {
      if (LinkedDocument == null)
        return;

      if (e.Document != LinkedDocument)
      {
        _preview?.Dispose();
        _preview = null;
      }
      else
        _preview = new Visualization.GrasshopperPreview();
    }
    static void OnBcDocCloseWillStart(object sender, EventArgs e)
    {
      LinkedDocument.CloseWillStart -= OnBcDocCloseWillStart;
      _preview?.Dispose();
      _preview = null;
      LinkedDocument = null;
      Rhinoceros.Script.HideEditor();
      ExpireGH();
    }
    #endregion
    static private void OnDocumentChanged()
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
          if (obj is Parameters.IGH_BcParam persistentParam)
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
          else if (obj is Components.IGH_BcComponent persistentComponent)
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
    static private void ExpireGH()
    {
      foreach (Grasshopper.Kernel.GH_Document definition in Grasshopper.Instances.DocumentServer)
      {
        bool expired = false;
        foreach (var obj in definition.Objects.OfType<Parameters.IGH_BcParam>())
        {
          expired = true;
          obj.ExpireSolution(false);
        }
        foreach (var obj in definition.Objects.OfType<Components.IGH_BcComponent>())
        {
          expired = true;
          obj.ExpireSolution(false);
        }
        if (expired)
          definition.NewSolution(false);
      }
    }
  }
}
