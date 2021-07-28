using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using _BcAp = Bricscad.ApplicationServices;
using _OdGe = Teigha.Geometry;

namespace GH_BC
{
  static class GrasshopperPlayer
  {
    public static void Run(GH_Document definition, GrasshopperData ghData, _BcAp.Document bcDoc)
    {
      bool saveState = GH_Document.EnableSolutions;
      GH_Document.EnableSolutions = true;
      definition.Enabled = true;

      var inputs = GetInputParams(definition);
      var hostEntityId = ghData.HostEntity;
      try
      {
        foreach (var input in inputs)
        {
          if (!IsInputName(input.NickName))
            continue;

          if (input is Parameters.BcEntity)
          {
            input.ClearData();
            var data = new Types.BcEntity(hostEntityId.ToFsp(), bcDoc.Name);
            input.AddVolatileData(new Grasshopper.Kernel.Data.GH_Path(0), 0, data);
            data.LoadGeometry(bcDoc);
            continue;
          }

          var prop = ghData.GetProperty(FormatName(input.NickName));
          if (prop == null)
            continue;

          input.VolatileData.ClearData();
          switch (prop)
          {
            case int intValue:
            case double doubleValue:
            case bool boolValue:
            case string strValue:
              input.AddVolatileData(new Grasshopper.Kernel.Data.GH_Path(0), 0, prop);
              break;
            case _OdGe.Point3d pntValue:
              input.AddVolatileData(new Grasshopper.Kernel.Data.GH_Path(0), 0, pntValue.ToRhino());
              break;
            case _OdGe.Vector3d vecValue:
              input.AddVolatileData(new Grasshopper.Kernel.Data.GH_Path(0), 0, vecValue.ToRhino());
              break;
          }
        }
        definition.NewSolution(false, GH_SolutionMode.Silent);
        Rhinoceros.Run();
      }
      finally
      {
        GH_Document.EnableSolutions = saveState;
      }

    }
    public static List<Tuple<string, object>> GetInputParametersValues(GH_Document definition)
    {
      var inputs = GetInputParams(definition);
      var values = new List<Tuple<string, object>>();
      foreach (var input in inputs)
      {
        object paramValue = null;
        switch (input)
        {
          case Param_Integer prInt:
            int? iVal = prInt.PersistentData.get_FirstItem(true)?.Value;
            if (iVal.HasValue)
              paramValue = iVal.Value;
            else
              paramValue = typeof(int);
            break;
          case Param_Number prNum:
            double? dVal = prNum.PersistentData.get_FirstItem(true)?.Value;
            if (dVal.HasValue)
              paramValue = dVal.Value;
            else
              paramValue = typeof(double);
            break;
          case Param_Boolean prBool:
            bool? bVal = prBool.PersistentData.get_FirstItem(true)?.Value;
            if (bVal.HasValue)
              paramValue = bVal.Value;
            else
              paramValue = typeof(bool);
            break;
          case Param_String prStr:
            string sVal = prStr.PersistentData.get_FirstItem(true)?.Value;
            if (!string.IsNullOrEmpty(sVal))
              paramValue = sVal;
            else
              paramValue = typeof(string);
            break;
          case Param_Point prPnt:
            Rhino.Geometry.Point3d? pnt = prPnt.PersistentData.get_FirstItem(true)?.Value;
            if (pnt != null)
              paramValue = pnt.Value.ToHost();
            else
              paramValue = typeof(_OdGe.Point3d);
            break;
          case Param_Vector prVec:
            Rhino.Geometry.Vector3d? vec = prVec.PersistentData.get_FirstItem(true)?.Value;
            if (vec.HasValue)
              paramValue = vec.Value.ToHost();
            else
              paramValue = typeof(_OdGe.Vector3d);
            break;
          default:
            continue;
        }
        if (paramValue != null)
          values.Add(new Tuple<string, object>(FormatName(input.NickName), paramValue));
      }
      return values;
    }
    public static IList<IGH_Param> GetInputParams(GH_Document definition)
    {
      var inputs = new List<IGH_Param>();
      foreach (var obj in definition.Objects)
      {
        if (!(obj is IGH_Param param))
          continue;

        if (param.Sources.Count != 0 || param.Recipients.Count == 0 || param.Locked)
          continue;

        if (!param.NickName.StartsWith("BcIn_"))
          continue;

        if (param.VolatileDataCount > 0)
          continue;

        inputs.Add(param);
      }

      return inputs;
    }
    private static bool IsInputName(string name) => name.StartsWith("BcIn_");
    private static string FormatName(string name) => name.Substring(5);
  }
}
