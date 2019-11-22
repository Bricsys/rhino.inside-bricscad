using Bricscad.ApplicationServices;
using Teigha.Runtime;

[assembly: ExtensionApplication(typeof(GH_BC.UI.SetUpUI))]

namespace GH_BC.UI
{
  public class SetUpUI : IExtensionApplication
  {
    public void Initialize()
    {
      //Check if loaded
      if (Bricscad.Windows.ComponentManager.Ribbon.FindTab("Grasshopper.Grasshopper-BricsCAD") == null)
      {
        var cuiFile = System.Reflection.Assembly.GetExecutingAssembly().Location;
        cuiFile = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(cuiFile), "Grasshopper-BricsCAD Connection.cui");
        Application.LoadPartialMenu(cuiFile);
      }
    }

    public void Terminate()
    {
    }
  }
}
