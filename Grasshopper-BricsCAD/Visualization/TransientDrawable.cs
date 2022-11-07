using Teigha.Geometry;
using Teigha.GraphicsInterface;

namespace GH_BC.Visualization
{
  class PreviewDrawable
  {
    private Rhino.Geometry.GeometryBase _geometry;
    public PreviewDrawable(Rhino.Geometry.GeometryBase geo)
    {
      _geometry = geo;
    }
    public bool WorldDraw(WorldDraw wd)
    {
      if (_geometry is Rhino.Geometry.Mesh mesh)
      {
        var faces = mesh.Faces.ToHost();
        var points = new Point3dCollection(mesh.Vertices.ToHost());
        var vertexData = new VertexData();
        vertexData.SetNormalVectors(mesh.Normals.ToHost());
        bool hasVertColor = mesh.VertexColors.Count != 0;
        vertexData.SetTrueColors(hasVertColor ? mesh.VertexColors.ToHost() : null);
        wd.Geometry.Shell(points, faces, null, null, vertexData, false);
      }
      else if (_geometry is Rhino.Geometry.Curve curve)
      {
        double deviation = System.Math.Max(wd.Deviation(DeviationType.MaxDevForCurve, curve.PointAtStart.ToHost()), 0.01 * curve.GetLength());
        var polyline = curve.ToPolyline(10E+4 * Convert.VertexTolerance, Convert.AngleTolerance, deviation, 0.0);
        if (polyline != null)
        {
          var giPoly = new Polyline();
          giPoly.Points = new Point3dCollection(polyline.ToPolyline().ToArray().ToHost());
          wd.Geometry.Polyline(giPoly);
        }
#if DEBUG
        else
        {
          var tmpFile = new Rhino.FileIO.File3dm();
          tmpFile.Objects.AddCurve(curve);
          tmpFile.Write(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "BricsCAD", "CurveFail.3dm"),
            new Rhino.FileIO.File3dmWriteOptions());
        }
#endif
      }
      else if (_geometry is Rhino.Geometry.Point)
        return false;
      return true;
    }
    public void ViewportDraw(ViewportDraw vd)
    {
      if (_geometry is Rhino.Geometry.Point point)
      {
        var dbPoint = new Teigha.DatabaseServices.DBPoint(point.Location.ToHost());
        dbPoint.ViewportDraw(vd);
        dbPoint.Dispose();
      }
    }
  }
}
