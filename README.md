# Grasshopper-BricsCAD Connection
Grasshopper-BricsCAD Connection is a plugin based on the [Rhino.Inside](https://github.com/mcneel/rhino.inside) technology.
It provides a bi-directional connection between Grasshopper and BricsCAD. BricsCAD geometry can be used as input parameters in Grasshopper, as well as Grasshopper geometry can be converted back to native BricsCAD geometry.
For additional information and documentation see [BricsCAD Help Center](https://help.bricsys.com/hc/en-us/articles/360025542454-Rhino-Grasshopper-Integration).
The latest installer can be found at [BricsCAD Application Store](https://www.bricsys.com/applications/a/?rhino/grasshopper-connection-for-bricscad-bim-a1353-al2360)

## Project structure
* *Grasshopper-BricsCAD-UI* is responsible for the UI initialization in BricsCAD. It loads a partial CUI file and enables grasshopper tools in menu, toolbar, quad, and ribbon. This module is autoloaded at BricsCAD start.
* *Grasshopper-BricsCAD* is the implementation of the connection between BricsCAD and Grasshopper. This module is loaded on demand, after the call of *RHINO* and *GRASSHOPPER* commands.

## Build from source
### Prerequisites
* Visual Studio ([download](https://visualstudio.microsoft.com/downloads/))
* .NET Framework 4.5.1 ([download](https://dotnet.microsoft.com/download/visual-studio-sdks))
* Rhino WIP ([download](https://www.rhino3d.com/download/rhino/wip))
* BricsCAD V21 ([download](https://www.bricsys.com/common/download.jsp))

### Getting Source & Build
1. Clone the repository.
2. In Visual Studio: open Grasshopper-BricsCAD-Connection.sln.
3. Update path to BricsCAD and Rhino references. _Copy Local_ property should be False.
4. Navigate to _Build_ > _Build Solution_ to begin your build.

### Launch
* Run *NETLOAD* command in BricsCAD to load the .NET application. 
* Or edit the Windows Registry to enable mechanism of DLL AutoLoad or DemandLoad:  
  1. Add a folder to the following path : 
     ```
     HKEY_LOCAL_MACHINE\SOFTWARE\Bricsys\Bricscad\V21x64\en_US\Applications\Grasshopper-BricsCAD-Connection
     ```
  2. Add the following keys :
     ```
     "LOADER"="Grasshopper-BricsCAD-Connection.dll" ("Grasshopper-BricsCAD-Connection.UI.dll")
     "DESCRIPTION"="Grasshopper-BricsCAD-Connection"
     "LOADCTRLS"=dword:0000000e for AutoLoad or 0000000c for DemandLoad
     "MANAGED"=dword:00000001
     ```
