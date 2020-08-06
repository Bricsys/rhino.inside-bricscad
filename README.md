# Grasshopper-BricsCAD Connection
Grasshopper-BricsCAD Connection is a plugin based on the [Rhino.Inside](https://github.com/mcneel/rhino.inside) technology.
It provides a bi-directional connection between Grasshopper and BricsCAD. BricsCAD geometry can be used as input parameters in Grasshopper, as well as Grasshopper geometry can be converted back to native BricsCAD geometry.
For additional information and documentation see [BricsCAD Help Center](https://help.bricsys.com/hc/en-us/articles/360025542454-Rhino-Grasshopper-Integration).
The latest installer can be found at [BricsCAD Application Store](https://www.bricsys.com/applications/a/?rhino/grasshopper-connection-for-bricscad-bim-a1353-al2360)

## Project structure
* *GrasshopperData* - BRX project where custom object (DbGrasshopperData) and corresponding property extension are defined. DbGrasshopperData is database object which links database entity and grasshopper script. DbGrasshopperData keeps properties defined in grasshopper script, these properties are used as input parameters for the script. Property changes and modification of linked database entity are processed in Grasshopper-BricsCAD-Connection project.
* *GrasshopperDataManaged* - managed wrapper for *GrasshopperData* project. This project is loaded by *Grasshopper-BricsCAD-Connection* project and register *GrasshopperData* as BRX extension.
* *Grasshopper-BricsCAD-UI* is responsible for the UI initialization in BricsCAD. It loads a partial CUI file and enables grasshopper tools in menu, toolbar, quad, and ribbon. This module is autoloaded at BricsCAD start.
* *Grasshopper-BricsCAD* is the implementation of the connection between BricsCAD and Grasshopper. This module is loaded on demand.

## Build from source
### Prerequisites
* Visual Studio ([download](https://visualstudio.microsoft.com/downloads/))
* .NET Framework 4.5.1 ([download](https://dotnet.microsoft.com/download/visual-studio-sdks))
* Rhino WIP ([download](https://www.rhino3d.com/download/rhino/wip))
* BricsCAD V20 ([download](https://www.bricsys.com/common/download.jsp))
* Download [BRX](https://www.bricsys.com/bricscad/help/en_US/CurVer/DevRef/source/BRX_01.htm) library and set *BRX_SDK_PATH* system variable (optional, nessesary for *GrasshopperData* and *GrasshopperDataManaged* build)

### Getting Source & Build
1. Clone the repository.
2. In Visual Studio: open Grasshopper-BricsCAD-Connection.sln.
3. Update path to BricsCAD and Rhino references. _Copy Local_ property should be False. If there is no necessity to build *GrasshopperData* and *GrasshopperDataManaged* projects, you can use prebuild binaries [GhDataApp.dll](https://github.com/Bricsys/rhino.inside-bricscad/blob/master/GrasshopperData/prebuild/GhDataApp.dll) and [GhDataManaged.dll](https://github.com/Bricsys/rhino.inside-bricscad/blob/master/GrasshopperDataManaged/prebuild/GhDataManaged.dll).
4. Navigate to _Build_ > _Build Solution_ to begin your build.

### Launch
* Run *NETLOAD* command in BricsCAD to load the .NET application. 
* Or edit the Windows Registry to enable mechanism of DLL AutoLoad or DemandLoad:  
  1. Add a folder to the following path : 
     ```
     HKEY_LOCAL_MACHINE\SOFTWARE\Bricsys\Bricscad\V20x64\en_US\Applications\Grasshopper-BricsCAD-Connection
     ```
  2. Add the following keys :
     ```
     "LOADER"="Grasshopper-BricsCAD-Connection.dll" ("Grasshopper-BricsCAD-Connection.UI.dll")
     "DESCRIPTION"="Grasshopper-BricsCAD-Connection"
     "LOADCTRLS"=dword:0000000e for AutoLoad or 0000000c for DemandLoad
     "MANAGED"=dword:00000001
     ```
