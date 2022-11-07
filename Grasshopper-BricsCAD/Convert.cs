using Rhino.Geometry;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using _OdCm = Teigha.Colors;
using _OdDb = Teigha.DatabaseServices;
using _OdGe = Teigha.Geometry;
using _OdRx = Teigha.Runtime;

namespace GH_BC
{
  public static class Convert
  {
    public static double VertexTolerance => _OdGe.Tolerance.Global.EqualPoint;
    public static double AngleTolerance => System.Math.PI / 180.0;

    static public _OdGe.Point3d ToHost(this Point3f p)
    {
      return new _OdGe.Point3d(p.X, p.Y, p.Z);
    }
    static public _OdGe.Point3d ToHost(this Point3d p)
    {
      return new _OdGe.Point3d(p.X, p.Y, p.Z);
    }
    static public _OdGe.Vector3d ToHost(this Vector3f p)
    {
      return new _OdGe.Vector3d(p.X, p.Y, p.Z);
    }
    static public _OdGe.Vector3d ToHost(this Vector3d p)
    {
      return new _OdGe.Vector3d(p.X, p.Y, p.Z);
    }
    static public _OdGe.Point3d[] ToHost(this IList<Point3f> points)
    {
      return points.Select(p => p.ToHost()).ToArray();
    }
    static public _OdGe.Point3d[] ToHost(this IList<Point3d> points)
    {
      return points.Select(p => p.ToHost()).ToArray();
    }
    static public _OdGe.Vector3d[] ToHost(this IList<Vector3f> points)
    {
      return points.Select(v => v.ToHost()).ToArray();
    }
    static public _OdCm.EntityColor ToHost(this Color c)
    {
      return new _OdCm.EntityColor(c.R, c.G, c.B);
    }
    static public _OdCm.EntityColor[] ToHost(this IEnumerable<Color> colors)
    {
      return colors.Select(v => v.ToHost()).ToArray();
    }
    static public _OdGe.IntegerCollection ToHost(this IList<MeshFace> faces)
    {
      return faces.Aggregate(new _OdGe.IntegerCollection(4 * faces.Count), (col, face) =>
      {
        col.AddRange(face.IsQuad ? new int[] { 4, face.A, face.B, face.C, face.D }
                                       : face.IsTriangle ? new int[] { 3, face.A, face.B, face.C }
                                                         : new int[] { });
        return col;
      });
    }

    //ToRhino
    static public Rhino.UnitSystem ToRhino(this _OdDb.UnitsValue units)
    {
      switch (units)
      {
        case _OdDb.UnitsValue.Inches: return Rhino.UnitSystem.Inches;
        case _OdDb.UnitsValue.Feet: return Rhino.UnitSystem.Feet;
        case _OdDb.UnitsValue.Miles: return Rhino.UnitSystem.Miles;
        case _OdDb.UnitsValue.Millimeters: return Rhino.UnitSystem.Millimeters;
        case _OdDb.UnitsValue.Centimeters: return Rhino.UnitSystem.Centimeters;
        case _OdDb.UnitsValue.Meters: return Rhino.UnitSystem.Meters;
        case _OdDb.UnitsValue.Kilometers: return Rhino.UnitSystem.Kilometers;
        case _OdDb.UnitsValue.MicroInches: return Rhino.UnitSystem.Microinches;
        case _OdDb.UnitsValue.Mils: return Rhino.UnitSystem.Mils;
        case _OdDb.UnitsValue.Angstroms: return Rhino.UnitSystem.Angstroms;
        case _OdDb.UnitsValue.Nanometers: return Rhino.UnitSystem.Nanometers;
        case _OdDb.UnitsValue.Microns: return Rhino.UnitSystem.Microns;
        case _OdDb.UnitsValue.Decimeters: return Rhino.UnitSystem.Decimeters;
        case _OdDb.UnitsValue.Dekameters: return Rhino.UnitSystem.Dekameters;
        case _OdDb.UnitsValue.Hectometers: return Rhino.UnitSystem.Hectometers;
        case _OdDb.UnitsValue.Gigameters: return Rhino.UnitSystem.Gigameters;
        case _OdDb.UnitsValue.Astronomical: return Rhino.UnitSystem.AstronomicalUnits;
        case _OdDb.UnitsValue.LightYears: return Rhino.UnitSystem.LightYears;
        case _OdDb.UnitsValue.Parsecs: return Rhino.UnitSystem.Parsecs;
      }
      return Rhino.UnitSystem.None;
    }
    static public Point3d ToRhino(this _OdGe.Point3d p)
    {
      return new Point3d(p.X, p.Y, p.Z);
    }
    static public Vector3d ToRhino(this _OdGe.Vector3d p)
    {
      return new Vector3d(p.X, p.Y, p.Z);
    }
    static public Plane ToRhino(this _OdGe.Plane plane)
    {
      var coordSystem = plane.GetCoordinateSystem();
      return new Plane(coordSystem.Origin.ToRhino(),
                       coordSystem.Xaxis.ToRhino(),
                       coordSystem.Yaxis.ToRhino());
    }
    static public Curve ToRhino(this _OdGe.Curve3d curve)
    {      
      switch (curve)
      {
        case _OdGe.LineSegment3d line: return line.ToRhino();
        case _OdGe.CircularArc3d arc: return arc.ToRhino();
        case _OdGe.EllipticalArc3d ellarc: return ellarc.ToRhino();
        case _OdGe.NurbCurve3d nurb: return nurb.ToRhino();
      }
      return null;
    }
    static private Curve ToRhino(this _OdGe.LineSegment3d crv)
    {
      return new LineCurve(crv.StartPoint.ToRhino(), crv.EndPoint.ToRhino());
    }
    static private Curve ToRhino(this _OdGe.CircularArc3d crv)
    {
      double param = 0.5 * (crv.GetParameterOf(crv.StartPoint) + crv.GetParameterOf(crv.EndPoint));
      return crv.IsClosed() ? new ArcCurve(new Circle(crv.GetPlane().ToRhino(), crv.Radius))
                            : new ArcCurve(new Arc(crv.StartPoint.ToRhino(), crv.EvaluatePoint(param).ToRhino(), crv.EndPoint.ToRhino()));
    }
    static private Curve ToRhino(this _OdGe.EllipticalArc3d crv)
    {
      double param = 0.5 * (crv.GetParameterOf(crv.StartPoint) + crv.GetParameterOf(crv.EndPoint));
      var ellipse = crv.IsClosed() ? new Ellipse(crv.GetPlane().ToRhino(), crv.MajorRadius, crv.MinorRadius)
        : new Ellipse(crv.StartPoint.ToRhino(), crv.EvaluatePoint(param).ToRhino(), crv.EndPoint.ToRhino());
      return NurbsCurve.CreateFromEllipse(ellipse);
    }
    static private Curve ToRhino(this _OdGe.NurbCurve3d crv)
    {
      var nurbsCurve = new NurbsCurve(3, crv.IsRational, crv.Order, crv.NumberOfControlPoints);

      if (crv.IsRational)
      {
        for (int i = 0; i < crv.NumberOfControlPoints; ++i)
        {
          var w = crv.GetWeightAt(i);
          nurbsCurve.Points.SetPoint(i, crv.ControlPointAt(i).ToRhino() * w, crv.GetWeightAt(i));
        }
      }
      else
      {
        for (int i = 0; i < crv.NumberOfControlPoints; ++i)
          nurbsCurve.Points.SetPoint(i, crv.ControlPointAt(i).ToRhino());
      }
      for (int i = 1; i < crv.Knots.Count-1; ++i)
        nurbsCurve.Knots[i-1] = crv.Knots[i];
      return nurbsCurve;
    }
    static private Curve ToRhino(this _OdDb.Curve crv)
    {
      return crv.GetGeCurve().ToRhino();
    }
    static public GeometryBase ToRhino(this _OdDb.Entity ent)
    {
      if (ent is _OdDb.Curve curve)
      {
        var geometry = curve.ToRhino();
        if (geometry != null)
          return geometry;
      }
      string tmpPath = Path.Combine(Path.GetTempPath(), "BricsCAD", "torhino.3dm");
      using (var aObj = new _OdDb.DBObjectCollection() { ent })
      {
        if (_OdRx.ErrorStatus.OK != Bricscad.Rhino.RhinoUtilityFunctions.ExportRhinoFile(aObj, tmpPath))
          return null;
      }
      return ExtractGeometryFromFile(tmpPath);
    }
    static public GeometryBase ToRhino(this _OdDb.ObjectId id)
    {
      if (DatabaseUtils.IsCurve(id))
      {
        using (var transaction = id.Database.TransactionManager.StartTransaction())
        {
          using (var dbCurve = transaction.GetObject(id, _OdDb.OpenMode.ForRead) as _OdDb.Curve)
          {
            var geometry = dbCurve?.ToRhino();
            if (geometry != null)
              return geometry;
          }
        }
      }

      string tmpPath = Path.Combine(Path.GetTempPath(), "BricsCAD", "torhino.3dm");
      using (var aId = new _OdDb.ObjectIdCollection() { id })
      {
        if (_OdRx.ErrorStatus.OK != Bricscad.Rhino.RhinoUtilityFunctions.ExportRhinoFile(aId, tmpPath))
          return null;
      }
      return ExtractGeometryFromFile(tmpPath);
    }
    static private GeometryBase ExtractGeometryFromFile(string filename)
    {
      GeometryBase geometry = null;
      using (var rhinoFile = Rhino.FileIO.File3dm.Read(filename))
      {
        foreach (var fileObj in rhinoFile.Objects)
        {
          if (fileObj.Geometry is InstanceReferenceGeometry)
            continue;
          geometry = fileObj.Geometry?.Duplicate();
          if (geometry != null)
            break;
        }
      }
      return geometry;
    }
  }
}
