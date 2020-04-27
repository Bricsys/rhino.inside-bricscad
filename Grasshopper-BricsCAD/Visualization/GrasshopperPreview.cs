using Grasshopper.Kernel;
using Grasshopper;
using System.Collections.Generic;
using System.Linq;
using System;
using Teigha.GraphicsInterface;

namespace GH_BC.Visualization
{
  class GrasshopperPreview : IDisposable
  {
    private GH_Document _activeDefinition = null;
    private readonly Teigha.Geometry.IntegerCollection _vieportNums = new Teigha.Geometry.IntegerCollection();
    private List<IGH_DocumentObject> _lastSelection = new List<IGH_DocumentObject>();
    private List<IGH_Param> _bcSelection = new List<IGH_Param>();
    private CompoundDrawable _compoundDrawable = null;
    public GrasshopperPreview()
    {
      Init();
      _compoundDrawable = new CompoundDrawable();
    }
    public void Dispose()
    {
      GhDrawingContext.LinkedDocument.TransientGraphicsManager()?.EraseTransient(_compoundDrawable, _vieportNums);
      GC.SuppressFinalize(this);
    }
    public bool Init()
    {
      var editor = Instances.DocumentEditor;
      var canvas = Instances.ActiveCanvas;
      var definition = canvas?.Document;

      if (definition != _activeDefinition)
      {
        GhDrawingContext.NeedRedraw = true;
        UnhighlightBcData();
        if (_activeDefinition != null)
        {
          if (editor != null) editor.VisibleChanged -= Editor_VisibleChanged;
          _activeDefinition.SolutionEnd -= ActiveDefinition_SolutionEnd;
          _activeDefinition.SettingsChanged -= ActiveDefinition_SettingsChanged;
          GH_Document.DefaultSelectedPreviewColourChanged -= Document_DefaultPreviewColourChanged;
          GH_Document.DefaultPreviewColourChanged -= Document_DefaultPreviewColourChanged;
        }

        _activeDefinition = definition;

        if (_activeDefinition != null)
        {
          GH_Document.DefaultPreviewColourChanged += Document_DefaultPreviewColourChanged;
          GH_Document.DefaultSelectedPreviewColourChanged += Document_DefaultPreviewColourChanged;
          _activeDefinition.SettingsChanged += ActiveDefinition_SettingsChanged;
          _activeDefinition.SolutionEnd += ActiveDefinition_SolutionEnd;
          if (editor != null) editor.VisibleChanged += Editor_VisibleChanged;
        }
      }

      if (SelectionPreviewChanged())
        GhDrawingContext.NeedRedraw = true;

      return _activeDefinition != null;
    }
    private bool SelectionPreviewChanged()
    {
      if (_activeDefinition != null)
      {
        var newSelection = _activeDefinition.SelectedObjects();
        if (_lastSelection.Count != newSelection.Count || _lastSelection.Except(newSelection).Any())
        {
          _lastSelection = newSelection;
          return true;
        }
      }
      return false;
    }
    #region GH Doucement event handlers
    private static void Document_DefaultPreviewColourChanged(System.Drawing.Color colour)
    {
      GhDrawingContext.NeedRedraw = true;
    }
    private static void Editor_VisibleChanged(object sender, EventArgs e)
    {
      GhDrawingContext.NeedRedraw = true;
    }
    private void ActiveDefinition_SettingsChanged(object sender, GH_DocSettingsEventArgs e)
    {
      GhDrawingContext.NeedRedraw = true;
    }
    private void ActiveDefinition_ModifiedChanged(object sender, GH_DocModifiedEventArgs e)
    {
      GhDrawingContext.NeedRedraw = true;
    }
    private void ActiveDefinition_SolutionEnd(object sender, GH_SolutionEventArgs e)
    {
      GhDrawingContext.NeedRedraw = true;
    }
    #endregion
    public void BuildScene()
    {
      var graphicsManager = GhDrawingContext.LinkedDocument.TransientGraphicsManager();
      graphicsManager.AddTransient(_compoundDrawable, TransientDrawingMode.Main, 128, _vieportNums);
      _compoundDrawable.Clear();
      if (_activeDefinition == null)
      {
        graphicsManager.UpdateTransient(_compoundDrawable, _vieportNums);
        return;
      }
      _compoundDrawable.Color = _activeDefinition.PreviewColour;
      _compoundDrawable.ColorSelected = _activeDefinition.PreviewColourSelected;

      if (_activeDefinition.PreviewMode != GH_PreviewMode.Disabled && Instances.EtoDocumentEditor.Visible)
      {
        _compoundDrawable.IsRenderMode = _activeDefinition.PreviewMode == GH_PreviewMode.Shaded;
        Action<IGH_ActiveObject> onBcObject = (obj) =>
        {
          if (obj.Category == "BricsCAD" && obj is IGH_Param param)
            HighlightBcData(param);
        };
        Action<IGH_ActiveObject> onSuccessfulExtract = (obj) =>
        {
          obj.ObjectChanged += ObjectChanged;
        };

        GetPreview(_activeDefinition, _compoundDrawable, onBcObject, onSuccessfulExtract);
      }
      graphicsManager.UpdateTransient(_compoundDrawable, _vieportNums);
    }
    private void ObjectChanged(IGH_DocumentObject sender, GH_ObjectChangedEventArgs e)
    {
      if (e.Type == GH_ObjectEventType.Preview)
        GhDrawingContext.NeedRedraw = true;
    }
    private void HighlightBcData(IGH_Param param)
    {
      bool highlight = param.Attributes.Selected && !_bcSelection.Contains(param);
      bool dehighlight = !param.Attributes.Selected && _bcSelection.Contains(param);
      foreach (var bcRef in param.VolatileData.AllData(true).OfType<Types.IGH_BcGeometricGoo>())
      {
        if (highlight)
          DatabaseUtils.Highlight(bcRef.Reference, true);
        else if (dehighlight)
          DatabaseUtils.Highlight(bcRef.Reference, false);
      }
      if(highlight)
        _bcSelection.Add(param);
      else if(dehighlight)
        _bcSelection.Remove(param);
    }
    private void UnhighlightBcData()
    {
      foreach(var param in _bcSelection)
      {
        foreach (var bcRef in param.VolatileData.AllData(true).OfType<Types.IGH_BcGeometricGoo>())
          DatabaseUtils.Highlight(bcRef.Reference, false);
      }
      _bcSelection.Clear();
    }
    private static void ExtractGeometry(Grasshopper.Kernel.Data.IGH_Structure volatileData,
                                        ref List<Rhino.Geometry.GeometryBase> resGeom,
                                        bool isRenderMode,
                                        Rhino.Geometry.MeshingParameters meshParams)
    {
      foreach (var value in volatileData.AllData(true))
      {
        if (value is IGH_PreviewData)
          ExtractGeometry(value, ref resGeom, isRenderMode, meshParams);
      }
    }
    private static void ExtractGeometry(Grasshopper.Kernel.Types.IGH_Goo iGoo,
                                        ref List<Rhino.Geometry.GeometryBase> resGeom,
                                        bool isRenderMode,
                                        Rhino.Geometry.MeshingParameters meshParams)
    {
      if (iGoo is Grasshopper.Kernel.Types.GH_GeometryGroup group)
      {
        foreach (var geomGoo in group.Objects)
          ExtractGeometry(geomGoo, ref resGeom, isRenderMode, meshParams);
        return;
      }

      Rhino.Geometry.GeometryBase geometryBase = null;
      try
      {
        switch (iGoo.ScriptVariable())
        {
          case Rhino.Geometry.Point3d point:
            geometryBase = new Rhino.Geometry.Point(point);
            break;
          case Rhino.Geometry.Line line:
            geometryBase = new Rhino.Geometry.LineCurve(line);
            break;
          case Rhino.Geometry.Rectangle3d rect:
            geometryBase = rect.ToNurbsCurve();
            break;
          case Rhino.Geometry.Arc arc:
            geometryBase = new Rhino.Geometry.ArcCurve(arc);
            break;
          case Rhino.Geometry.Circle circle:
            geometryBase = new Rhino.Geometry.ArcCurve(circle);
            break;
          case Rhino.Geometry.Ellipse ellipse:
            geometryBase = ellipse.ToNurbsCurve();
            break;
          case Rhino.Geometry.Curve curve:
            geometryBase = curve;
            break;
          case Rhino.Geometry.Box box:
            geometryBase = Rhino.Geometry.Mesh.CreateFromBox(box, 1, 1, 1);
            break;
          case Rhino.Geometry.Mesh mesh:
            geometryBase = mesh;
            break;
          case Rhino.Geometry.Brep brep:
            {
              if (!isRenderMode)
              {
                foreach (var crv in brep.GetWireframe(-1))
                  resGeom.Add(crv);
              }
              else
              {
                var previewMesh = new Rhino.Geometry.Mesh();
                previewMesh.Append(Rhino.Geometry.Mesh.CreateFromBrep(brep, meshParams));
                geometryBase = previewMesh;
              }
              break;
            }
          case Rhino.Geometry.Plane plane:
            {
              double len = 4.0;
              var x = new Rhino.Geometry.Interval(-len, len);
              var y = new Rhino.Geometry.Interval(-len, len);
              geometryBase = Rhino.Geometry.Mesh.CreateFromPlane(plane, x, y, 5, 5);
              break;
            }
          default:
            {
              System.Diagnostics.Debug.Fail("Not supported GH type", iGoo.GetType().ToString());
              break;
            }
        }
      }
      catch (Exception e)
      {
        System.Diagnostics.Debug.Fail(e.Source, e.Message);
      }

      if(geometryBase != null)
        resGeom.Add(geometryBase);
    }
    public static void GetPreview(GH_Document definition, CompoundDrawable compoundDrawable,
                                  Action<IGH_ActiveObject> onNotDrawble = null, Action<IGH_ActiveObject> onSuccessfulExtract = null)
    {
      var meshParameters = definition.PreviewCurrentMeshParameters() ?? Rhino.Geometry.MeshingParameters.Default;
      var isRenderMode = definition.PreviewMode == GH_PreviewMode.Shaded;

      foreach (var obj in definition.Objects.OfType<IGH_ActiveObject>())
      {
        if (obj.Locked)
          continue;

        bool isSelected = obj.Attributes.Selected;
        if (definition.PreviewFilter == GH_PreviewFilter.Selected && !isSelected)
          continue;

        if (obj is IGH_PreviewObject previewObject)
        { 
          if (previewObject.IsPreviewCapable && !previewObject.Hidden)
          {
            var geometries = new List<Rhino.Geometry.GeometryBase>();
            if (obj is IGH_Component component)
            {
              foreach (var param in component.Params.Output)
                ExtractGeometry(param.VolatileData, ref geometries, isRenderMode, meshParameters);
            }
            else if (obj is IGH_Param param)
              ExtractGeometry(param.VolatileData, ref geometries, isRenderMode, meshParameters);

            if (geometries.Count != 0)
            {
              geometries.ForEach(geom => compoundDrawable.AddDrawable(new PreviewDrawable(geom), isSelected));
              onSuccessfulExtract?.Invoke(obj);
            }
          }
        }
        else
          onNotDrawble?.Invoke(obj);
      }
    }
  }
}
