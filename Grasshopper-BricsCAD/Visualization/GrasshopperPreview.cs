using Grasshopper.Kernel;
using Grasshopper;
using System.Collections.Generic;
using System.Linq;
using System;

using Teigha.GraphicsInterface;

namespace GH_BC
{
  class GrasshopperPreview : IDisposable
  {
    public static GH_Document ActiveDefinition = null;
    Teigha.Geometry.IntegerCollection VieportNums = new Teigha.Geometry.IntegerCollection();
    private List<IGH_DocumentObject> LastSelection = new List<IGH_DocumentObject>();
    private List<IGH_Param> _bcSelection = new List<IGH_Param>();
    TransientManager GraphicsManager;
    CompoundDrawable CompoundDrawable = null;
    public GrasshopperPreview()
    {
      Init();
      CompoundDrawable = new CompoundDrawable();
      GraphicsManager = TransientManager.CurrentTransientManager;
      GraphicsManager.AddTransient(CompoundDrawable, TransientDrawingMode.Main, 128, VieportNums);
    }
    public void Dispose()
    {
      GraphicsManager?.EraseTransient(CompoundDrawable, VieportNums);
      GraphicsManager?.Dispose();
      GC.SuppressFinalize(this);
    }
    Rhino.Geometry.MeshingParameters MeshParameters => ActiveDefinition.PreviewCurrentMeshParameters() ?? Rhino.Geometry.MeshingParameters.Default;
    public bool Init()
    {
      var editor = Instances.DocumentEditor;
      var canvas = Instances.ActiveCanvas;
      var definition = canvas?.Document;

      if (definition != ActiveDefinition)
      {
        PlugIn.SetNeetRedraw();
        UnhighlightBcData();
        if (ActiveDefinition != null)
        {
          if (editor != null) editor.VisibleChanged -= Editor_VisibleChanged;
          ActiveDefinition.SolutionEnd -= ActiveDefinition_SolutionEnd;
          ActiveDefinition.SettingsChanged -= ActiveDefinition_SettingsChanged;
          GH_Document.DefaultSelectedPreviewColourChanged -= Document_DefaultPreviewColourChanged;
          GH_Document.DefaultPreviewColourChanged -= Document_DefaultPreviewColourChanged;
        }

        ActiveDefinition = definition;

        if (ActiveDefinition != null)
        {
          GH_Document.DefaultPreviewColourChanged += Document_DefaultPreviewColourChanged;
          GH_Document.DefaultSelectedPreviewColourChanged += Document_DefaultPreviewColourChanged;
          ActiveDefinition.SettingsChanged += ActiveDefinition_SettingsChanged;
          ActiveDefinition.SolutionEnd += ActiveDefinition_SolutionEnd;
          if (editor != null) editor.VisibleChanged += Editor_VisibleChanged;
        }
      }
      return ActiveDefinition != null;
    }

    public bool SelectionPreviewChanged()
    {
      if (Instances.ActiveCanvas?.Document != ActiveDefinition)
        return true;

      if (ActiveDefinition != null)
      {
        var newSelection = ActiveDefinition.SelectedObjects();
        if (LastSelection.Count != newSelection.Count || LastSelection.Except(newSelection).Any())
        {
          LastSelection = newSelection;
          return true;
        }
      }

      return false;
    }

    #region GH Doucement event handlers
    private static void Document_DefaultPreviewColourChanged(System.Drawing.Color colour)
    {
      PlugIn.SetNeetRedraw();
    }
    private static void Editor_VisibleChanged(object sender, EventArgs e)
    {
      PlugIn.SetNeetRedraw();
    }
    private void ActiveDefinition_SettingsChanged(object sender, GH_DocSettingsEventArgs e)
    {
      PlugIn.SetNeetRedraw();
    }
    private void ActiveDefinition_ModifiedChanged(object sender, GH_DocModifiedEventArgs e)
    {
      PlugIn.SetNeetRedraw();
    }
    private void ActiveDefinition_SolutionEnd(object sender, GH_SolutionEventArgs e)
    {
      PlugIn.SetNeetRedraw();
    }
    #endregion

    public void BuildScene()
    {
      CompoundDrawable.Clear();
      if (ActiveDefinition == null)
      {
        GraphicsManager.UpdateTransient(CompoundDrawable, VieportNums);
        return;
      }
      CompoundDrawable.Colour = ActiveDefinition.PreviewColour;
      CompoundDrawable.ColourSelected = ActiveDefinition.PreviewColourSelected;

      if (ActiveDefinition.PreviewMode != GH_PreviewMode.Disabled && Instances.EtoDocumentEditor.Visible)
      {
        CompoundDrawable.IsRenderMode = ActiveDefinition.PreviewMode == GH_PreviewMode.Shaded;

        foreach (var obj in ActiveDefinition.Objects.OfType<IGH_ActiveObject>())
        {
          if (obj.Locked)
            continue;

          if (obj is IGH_PreviewObject previewObject)
          {
            if (previewObject.IsPreviewCapable)
            {
              if (obj is IGH_Component component)
              {
                foreach (var param in component.Params.Output)
                  DrawData(param.VolatileData, obj);
              }
              else if (obj is IGH_Param param)
                DrawData(param.VolatileData, obj);
            }
          }
          else if(obj.Category == "BricsCAD" && obj is IGH_Param param)
              HighlightBcData(param);
        }
      }
      GraphicsManager.UpdateTransient(CompoundDrawable, VieportNums);
    }
    void ObjectChanged(IGH_DocumentObject sender, GH_ObjectChangedEventArgs e)
    {
      if (e.Type == GH_ObjectEventType.Preview)
        PlugIn.SetNeetRedraw();
    }
    void HighlightBcData(IGH_Param param)
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
    void UnhighlightBcData()
    {
      foreach(var param in _bcSelection)
      {
        foreach (var bcRef in param.VolatileData.AllData(true).OfType<Types.IGH_BcGeometricGoo>())
          DatabaseUtils.Highlight(bcRef.Reference, false);
      }
      _bcSelection.Clear();
    }
    void DrawData(Grasshopper.Kernel.Data.IGH_Structure volatileData, IGH_DocumentObject docObject)
    {
      if (docObject is IGH_PreviewObject preview)
      {
        if (preview.Hidden)
          return;
      }

      if (!volatileData.IsEmpty)
      {
        foreach (var value in volatileData.AllData(true))
        {
          if (value is IGH_PreviewData)
          {
            bool isSelected = docObject.Attributes.Selected;
            if (ActiveDefinition.PreviewFilter == GH_PreviewFilter.Selected && !isSelected)
              continue;
            var geometries = new List<Rhino.Geometry.GeometryBase>();
            ExtractGeometry(value, ref geometries);
            if (geometries.Count != 0)
            {
              geometries.ForEach(geometryBase => AddDrawable(geometryBase, isSelected));
              docObject.ObjectChanged += ObjectChanged;
            }
          }
        }
      }
    }
    private void ExtractGeometry(Grasshopper.Kernel.Types.IGH_Goo iGoo, ref List<Rhino.Geometry.GeometryBase> resGeom)
    {
      if (iGoo is Grasshopper.Kernel.Types.GH_GeometryGroup group)
      {
        foreach (var geomGoo in group.Objects)
          ExtractGeometry(geomGoo, ref resGeom);
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
            geometryBase = Rhino.Geometry.Mesh.CreateFromBox(box, 1, 1, 1); break;
          case Rhino.Geometry.Mesh mesh:
            geometryBase = mesh; break;
          case Rhino.Geometry.Brep brep:
            {
              if (!CompoundDrawable.IsRenderMode)
              {
                foreach (var crv in brep.GetWireframe(-1))
                  resGeom.Add(crv);
              }
              else
              {
                var previewMesh = new Rhino.Geometry.Mesh();
                previewMesh.Append(Rhino.Geometry.Mesh.CreateFromBrep(brep, MeshParameters));
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
    private void AddDrawable(Rhino.Geometry.GeometryBase geom, bool isSelected)
    {
      CompoundDrawable.AddDrawable(new PreviewDrawable(geom), isSelected);
    }
  }
}
