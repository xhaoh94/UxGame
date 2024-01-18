using FairyGUI;

namespace Ux
{
    public abstract class UIWindow : UIBase
    {        
        protected Window window
        {
            get
            {
                if (GObject == null) return null;
                return GObject as Window;
            }
        }

        protected override GObject CreateObject()
        {
            string pkg = PkgName;
            string res = ResName;
            if (string.IsNullOrEmpty(pkg) || string.IsNullOrEmpty(res))
            {
                Log.Fatal("没有指定pkgName或是resName");
            }

            GObject gObject = UIPackage.CreateObject(pkg, res);
            var _window = new Window
            {
                contentPane = gObject.asCom,
                modal = IsModal()
            };
            return _window;
        }

        protected override void AddToStage()
        {
            window?.Show();
        }

        protected override void RemoveToStage()
        {
            window?.Hide();
        }

        protected virtual bool IsModal()
        {
            return true;
        }

        protected void ShowModalWait()
        {
            this.window?.ShowModalWait();
        }

        protected void CloseModalWait()
        {
            this.window?.CloseModalWait();
        }

        protected override void OnLayout()
        {
            if (window == null) return;
            SetLayout(UILayout.Size);
        }
    }
}