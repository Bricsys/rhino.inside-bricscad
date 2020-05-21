using Bricscad.ApplicationServices;
using Teigha.Runtime;

[assembly: ExtensionApplication(typeof(GH_BC.UI.SetUpUI))]

namespace GH_BC.UI
{
  public class SetUpUI : IExtensionApplication
  {
    GhQuadReactor _quadReactor = null;
    public void Initialize()
    {
      if (!Application.IsMenuGroupLoaded("Grasshopper"))
      {
        var cuiFile = System.Reflection.Assembly.GetExecutingAssembly().Location;
        cuiFile = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(cuiFile), "Grasshopper-BricsCAD Connection.cui");
        Application.LoadPartialMenu(cuiFile);
      }
      _quadReactor = new GhQuadReactor();
      _quadReactor.Register();
    }

    public void Terminate()
    {
      _quadReactor?.Unregister();
      _quadReactor?.Dispose();
    }
  }
}
