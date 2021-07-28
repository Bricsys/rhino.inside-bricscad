using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;

using Rhino;
using static GH_BC.UI.WinAPI;

namespace GH_BC.UI
{
  class ModalForm : System.Windows.Forms.Form
  {
    public static IntPtr MWHBricscad => Bricscad.ApplicationServices.Application.MainWindow.Handle;
    class BricsadMainWindow : IWin32Window { IntPtr IWin32Window.Handle => MWHBricscad; }
    public static IWin32Window OwnerWindow = new BricsadMainWindow();
    public static new ModalForm ActiveForm { get; private set; }
    readonly bool WasEnabled = IsWindowEnabled(MWHBricscad);

    public ModalForm()
    {
      EnableWindow(MWHBricscad, false);
      ActiveForm = this;
      ShowIcon = false;
      ShowInTaskbar = false;
      BackColor = System.Drawing.Color.White;
      Opacity = 0.1;
      Show(OwnerWindow);
    }

    protected override void Dispose(bool disposing)
    {
      base.Dispose(disposing);
      ActiveForm = null;
      EnableWindow(MWHBricscad, WasEnabled);
      if(WasEnabled)
      {
        SetActiveWindow(MWHBricscad);
      }
    }

    protected override bool ShowWithoutActivation => true;
    protected override CreateParams CreateParams
    {
      get
      {
        var createParams = base.CreateParams;
        createParams.Style = 0x00000000;
        var bcWnd = Bricscad.ApplicationServices.Application.MainWindow;
        if(bcWnd != null)
        { 
          var loc = bcWnd.GetLocation();
          var size = bcWnd.GetSize();
          createParams.X = loc.X;
          createParams.Y = loc.Y;
          createParams.Width = size.Width;
          createParams.Height = size.Height;
          createParams.Parent = MWHBricscad;
        }
        return createParams;
      }
    }

    public class EditScope : IDisposable
    {
      readonly bool WasExposed = Rhinoceros.WindowVisible;
      readonly bool WasVisible = ActiveForm?.Visible ?? false;
      readonly bool WasEnabled = IsWindowEnabled(MWHBricscad);
      public EditScope()
      {
        SetActiveWindow(MWHBricscad);
        if (WasVisible) ShowOwnedPopups(false);
        if (WasExposed) Rhinoceros.WindowVisible = false;
        if (ActiveForm != null) ActiveForm.Visible = false;
        EnableWindow(MWHBricscad, true);
      }
      void IDisposable.Dispose()
      {
        EnableWindow(MWHBricscad, WasEnabled);
        if (ActiveForm != null) ActiveForm.Visible = WasVisible;
        if (WasExposed) Rhinoceros.WindowVisible = WasExposed;
        if (WasVisible) ShowOwnedPopups(true);

        var activePopup = GetEnabledPopup();
        if (activePopup == IntPtr.Zero || WasExposed)
          RhinoApp.SetFocusToMainWindow();
        else
          BringWindowToTop(activePopup);
      }
    }

    public static bool ParentEnabled
    {
      get => ActiveForm?.Enabled ?? false;
      set
      {
        if (value)
        {
          if (ActiveForm != null)
            ActiveForm.Enabled = true;

          EnableWindow(MWHBricscad, true);
        }
        else
        {
          EnableWindow(MWHBricscad, false);

          if (ActiveForm != null)
            ActiveForm.Enabled = false;
        }
      }
    }
  }
}
