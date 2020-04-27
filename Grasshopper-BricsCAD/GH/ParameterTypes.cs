using Bricscad.ApplicationServices;
using Grasshopper.Kernel.Types;
using System;
using Teigha.DatabaseServices;

namespace GH_BC.Types
{
  public interface IGH_BcGeometricGoo : IGH_GeometricGoo
  {
    bool LoadGeometry(Document doc);
    FullSubentityPath Reference { get; }
    ObjectId ObjectId { get; }
    Handle PersistentRef { get; }
    SubentityType SubentType { get; }
    int SubentIndex { get; }
    string BcDocName { get; }
    Rhino.Geometry.GeometryBase getGeometry();
  }

  public abstract class GH_GeometricGoo : GH_GeometricGoo<FullSubentityPath>, IGH_BcGeometricGoo
  {
    public override string TypeName => "BricsCAD GeometryObject";
    public override string TypeDescription => "Represents BricsCAD GeometryObject";
    public override bool IsValid => !Value.IsNullObjectLink();
    public override sealed IGH_Goo Duplicate() => (IGH_Goo) MemberwiseClone();
    protected virtual Type ScriptVariableType => typeof(FullSubentityPath);

    #region IGH_GeometricGoo  
    Guid IGH_GeometricGoo.ReferenceID
    {
      // TO do
      get => Guid.Empty;
      set => throw new InvalidOperationException();
    }
    public FullSubentityPath Reference { get => Value; }
    public string BcDocName { get; private set; }
    public ObjectId ObjectId
    {
      get => Value.InsertId();
    }
    public override bool IsReferencedGeometry => PersistentRef.Value != 0;
    public override bool IsGeometryLoaded => IsValid;
    public override bool LoadGeometry() => IsValid || LoadGeometry(GhDrawingContext.LinkedDocument);
    public Handle PersistentRef { get; private set; }
    public SubentityType SubentType { get; private set; }
    public int SubentIndex { get; private set; }
    public virtual bool LoadGeometry(Document doc)
    {
      if (!Value.IsNullObjectLink())
        return true;
      
      if (doc == null || System.IO.Path.GetFileNameWithoutExtension(doc.Name) != BcDocName)
        return false;

      var id = ObjectId.Null;
      if (!doc.Database.TryGetObjectId(PersistentRef, out id))
        return false;

      var subentId = new SubentityId(SubentType, SubentIndex);
      Value = new FullSubentityPath(new ObjectId[] { id }, subentId);
      return IsValid;
    }

    public Rhino.Geometry.GeometryBase getGeometry()
    {
      if (Value.IsNullObjectLink())
        return null;
      Rhino.Geometry.GeometryBase geom = null;
      if (Value.IsSubentity())
      {
        var entity = ObjectId.GetObject(OpenMode.ForRead) as Entity;
        var subent = entity?.GetSubentity(Value);
        geom = subent?.ToRhino();
        subent?.Dispose();
        entity?.Dispose();
      }
      else
        geom = ObjectId.ToRhino();
      return geom;
    }

    public override sealed IGH_GeometricGoo DuplicateGeometry() => (IGH_BcGeometricGoo) MemberwiseClone();
    public override Rhino.Geometry.BoundingBox Boundingbox => GetBoundingBox(Rhino.Geometry.Transform.Identity);
    public override IGH_GeometricGoo Morph(Rhino.Geometry.SpaceMorph xmorph) => null;
    public override IGH_GeometricGoo Transform(Rhino.Geometry.Transform xform) => null;
    public override string ToString()
    {
      string res;
      switch (Value.SubentId.Type)
      {
        case SubentityType.Face: res = "Face"; break;
        case SubentityType.Edge: res = "Edge"; break;
        case SubentityType.Vertex: res = "Vertex"; break;
        default: res = "Entity"; break;
      }
      return res + " " + DatabaseUtils.ToString(Value);
    }
    public override bool Read(GH_IO.Serialization.GH_IReader reader)
    {
      BcDocName = reader.GetString("BcDocName");
      PersistentRef = new Handle(reader.GetInt64("Handle"));
      SubentType = (SubentityType) reader.GetByte("SubentityType");
      SubentIndex = reader.GetInt32("Index");
      return true;
    }
    public override bool Write(GH_IO.Serialization.GH_IWriter writer)
    {
      if (!string.IsNullOrEmpty(BcDocName) && PersistentRef.Value != 0)
      {
        writer.SetString("BcDocName", BcDocName);
        writer.SetInt64("Handle", PersistentRef.Value);
        writer.SetByte("SubentityType", (byte) SubentType);
        writer.SetInt32("Index", SubentIndex);
      }
      return true;
    }
    #endregion
    public GH_GeometricGoo() { }
    protected GH_GeometricGoo(FullSubentityPath reference, string docName)
    {
      PersistentRef = reference.InsertId().Handle;
      SubentType = reference.SubentId.Type;
      SubentIndex = reference.SubentId.Index;
      BcDocName = System.IO.Path.GetFileNameWithoutExtension(docName);
    }
    public override Rhino.Geometry.BoundingBox GetBoundingBox(Rhino.Geometry.Transform xform)
    {
      var geometry = getGeometry();
      if (geometry == null)
        return Rhino.Geometry.BoundingBox.Empty;

      bool IsIdentity = xform == Rhino.Geometry.Transform.Identity;
      
      var bbox = IsIdentity ? geometry.GetBoundingBox(true) : geometry.GetBoundingBox(xform);
      return bbox;
    }
  }

  public class Vertex : GH_GeometricGoo
  {
    public override string TypeName => "BricsCAD Vertex";
    public override string TypeDescription => "Represents a BricsCAD Vertex";
    public Vertex() { }
    public Vertex(FullSubentityPath fullSubentityPath, string docName) : base(fullSubentityPath, docName) { }
    public override bool CastTo<Q>(ref Q target)
    {
      var geometry = getGeometry();
      if (typeof(Q).IsAssignableFrom(typeof(GH_Point)) && geometry is Rhino.Geometry.Point point)
      {
        target = (Q) (object) new GH_Point(point.Location);
        return true;
      }
      return base.CastTo<Q>(ref target);
    }
  }

  public class Edge : GH_GeometricGoo
  {
    public override string TypeName => "BricsCAD Edge";
    public override string TypeDescription => "Represents BricsCAD Edge";
    public Edge() { }
    public Edge(FullSubentityPath fullSubentityPath, string docName) : base(fullSubentityPath, docName) { }
    public override bool CastTo<Q>(ref Q target)
    {
      if (!(getGeometry() is Rhino.Geometry.Curve geometry))
        return false;

      if (typeof(Q).IsAssignableFrom(typeof(GH_Curve)))
      {
        target = (Q) (object) new GH_Curve(geometry);
        return true;
      }
      return base.CastTo<Q>(ref target);
    }
  }

  public class Face : GH_GeometricGoo
  {
    public override string TypeName => "BricsCAD Face";
    public override string TypeDescription => "Represents BricsCAD Face";
    public Face() { }
    public Face(FullSubentityPath fullSubentityPath, string docName) : base(fullSubentityPath, docName) { }
    public override bool CastTo<Q>(ref Q target)
    {
      if (!(getGeometry() is Rhino.Geometry.Brep geometry))
        return false;

      if (typeof(Q).IsAssignableFrom(typeof(GH_Brep)))
      {
        target = (Q) (object) new GH_Brep(geometry);
        return true;
      }
      else if (typeof(Q).IsAssignableFrom(typeof(GH_Surface)))
      {
        target = (Q) (object) new GH_Surface(geometry);
        return true;
      }
      return base.CastTo<Q>(ref target);
    }
  }

  public class BcCurve : GH_GeometricGoo
  {
    public override string TypeName => "BricsCAD Curve";
    public override string TypeDescription => "Represents BricsCAD Curve";
    public BcCurve() { }
    public BcCurve(FullSubentityPath fullSubentityPath, string docName) : base(fullSubentityPath, docName) { }
    public override bool CastTo<Q>(ref Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(GH_Curve)))
      {
        if (!(getGeometry() is Rhino.Geometry.Curve geometry))
          return false;

        target = (Q) (object) new GH_Curve(geometry);
        return true;
      }
      return base.CastTo<Q>(ref target);
    }
  }

  public class BcEntity : GH_GeometricGoo
  {
    public override string TypeName => "BricsCAD Entity";
    public override string TypeDescription => "Represents BricsCAD Entity";
    public BcEntity() { }
    public BcEntity(FullSubentityPath fullSubentityPath, string docName) : base(fullSubentityPath, docName) { }
    public override bool CastTo<Q>(ref Q target)
    {
      var geometry = getGeometry();
      if (geometry == null)
        return false;

      switch (geometry.ObjectType)
      {
        case Rhino.DocObjects.ObjectType.Point:
          if (typeof(Q).IsAssignableFrom(typeof(GH_Point)))
          {
            if (!(getGeometry() is Rhino.Geometry.Point point))
              return false;
            target = (Q) (object) new GH_Point((geometry as Rhino.Geometry.Point).Location);
            return true;
          }
          break;
        case Rhino.DocObjects.ObjectType.Curve:
          if (typeof(Q).IsAssignableFrom(typeof(GH_Curve)))
          {
            target = (Q) (object) new GH_Curve(geometry as Rhino.Geometry.Curve);
            return true;
          }
          break;
        case Rhino.DocObjects.ObjectType.Brep:
          if (typeof(Q).IsAssignableFrom(typeof(GH_Brep)))
          {
            target = (Q) (object) new GH_Brep(geometry as Rhino.Geometry.Brep);
            return true;
          }
          else if(typeof(Q).IsAssignableFrom(typeof(GH_Surface)))
          {
            target = (Q) (object) new GH_Surface(geometry as Rhino.Geometry.Brep);
            return true;
          }
          break;
        case Rhino.DocObjects.ObjectType.Mesh:
          if (typeof(Q).IsAssignableFrom(typeof(GH_Mesh)))
          {
            target = (Q) (object) new GH_Mesh(geometry as Rhino.Geometry.Mesh);
            return true;
          }
          break;
      }

      return base.CastTo<Q>(ref target);
    }
  }

  public class ElementType : GH_Goo<Bricscad.Bim.BimTypeElement>
  {
    public override bool IsValid => true;
    public override string TypeName => "ElementType";
    public override string TypeDescription => "Element type";
    public ElementType(ElementType elementType)
    {
      Value = elementType.Value;
    }
    public ElementType()
    {
      Value = Bricscad.Bim.BimTypeElement.NoBuildingElement;
    }
    public override IGH_Goo Duplicate() => new ElementType(this);
    public override string ToString() => Value.ToString();
    public override bool Read(GH_IO.Serialization.GH_IReader reader)
    {
      Value = (Bricscad.Bim.BimTypeElement) reader.GetInt32("ElementType");
      return true;
    }
    public override bool Write(GH_IO.Serialization.GH_IWriter writer)
    {
      writer.SetInt32("ElementType", (int) Value);
      return true;
    }
    public override bool CastFrom(object source)
    {
      if (source != null)
      {
        if (Grasshopper.Kernel.GH_Convert.ToInt32(source, out int val, Grasshopper.Kernel.GH_Conversion.Both))
        {
          if (Enum.IsDefined(typeof(Bricscad.Bim.BimTypeElement), val))
          {
            Value = (Bricscad.Bim.BimTypeElement) val;
            return true;
          }
        }
      }
      return false;
    }

    public override bool CastTo<Q>(ref Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(int)))
      {
        target = (Q) (object) Value;
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Integer)))
      {
        target = (Q) (object) new GH_Integer((int) Value);
        return true;
      }
      return false;
    }
  }

  public class PropCategory : GH_Goo<Bricscad.Bim.BimCategory>
  {
    public override bool IsValid => true;
    public override string TypeName => "BimCategory";
    public override string TypeDescription => "Bricscad property category";
    public PropCategory(PropCategory elementType)
    {
      Value = elementType.Value;
    }
    public PropCategory()
    {
      Value = Bricscad.Bim.BimCategory.Bricsys;
    }
    public override IGH_Goo Duplicate() => new PropCategory(this);
    public override string ToString() => Value.ToString();
    public override bool Read(GH_IO.Serialization.GH_IReader reader)
    {
      Value = (Bricscad.Bim.BimCategory) reader.GetInt32("Category");
      return true;
    }
    public override bool Write(GH_IO.Serialization.GH_IWriter writer)
    {
      writer.SetInt32("Category", (int) Value);
      return true;
    }
    public override bool CastFrom(object source)
    {
      if (source != null)
      {
        if (Grasshopper.Kernel.GH_Convert.ToInt32(source, out int val, Grasshopper.Kernel.GH_Conversion.Both))
        {
          if (Enum.IsDefined(typeof(Bricscad.Bim.BimCategory), val))
          {
            Value = (Bricscad.Bim.BimCategory) val;
            return true;
          }
        }
      }
      return false;
    }
    public override bool CastTo<Q>(ref Q target)
    {
      if (typeof(Q).IsAssignableFrom(typeof(int)))
      {
        target = (Q) (object) Value;
        return true;
      }

      if (typeof(Q).IsAssignableFrom(typeof(GH_Integer)))
      {
        target = (Q) (object) new GH_Integer((int) Value);
        return true;
      }
      return false;
    }
  }

  public abstract class BimObject<X> : GH_Goo<X> where X : Bricscad.Bim.BIMObject
  {
    public override bool IsValid => Value != null;
    public override string TypeName => "BimObject";
    public override string TypeDescription => "Bim Object";
    public override string ToString() => Value.Name;
  }

  public class SpatialLocation : BimObject<Bricscad.Bim.BIMSpatialLocation>
  {
    public override string TypeName => "SpatialLocation";
    public override string TypeDescription => "Spatial location";
    public SpatialLocation() { }
    public SpatialLocation(SpatialLocation spatialLocation)
    {
      Value = spatialLocation.Value;
    }
    public SpatialLocation(Bricscad.Bim.BIMSpatialLocation spatialLocation)
    {
      Value = spatialLocation;
    }
    public override IGH_Goo Duplicate() => new SpatialLocation(this);
    public override bool CastFrom(object source)
    {
      if (source != null)
      {
        if (Grasshopper.Kernel.GH_Convert.ToString(source, out string val, Grasshopper.Kernel.GH_Conversion.Both))
        {
          Value = Bricscad.Bim.BIMBuilding.GetBuilding(GhDrawingContext.LinkedDocument.Database, val);
          return IsValid;
        }
      }
      return false;
    }
  }

  public class Profile : GH_Goo<Bricscad.Bim.BIMProfile>
  {
    public override string TypeName => "Profile";
    public override string TypeDescription => "Profile";
    public override bool IsValid => Value != null;
    public Profile() { }
    public Profile(Profile spatialLocation)
    {
      Value = spatialLocation.Value;
    }
    public Profile(Bricscad.Bim.BIMProfile profile)
    {
      Value = profile;
    }
    public override IGH_Goo Duplicate() => new Profile(this);
    public override string ToString()
    {
      return "Profile " + Value.GetStandard + " " + Value.GetName + " " + Value.GetShape;
    }
  }
}
