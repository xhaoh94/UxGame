using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ux
{
    public class SplashScreenUx 
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        static void OnBeforeSplashScreen()
        {
            if (!Application.HasProLicense())
            {
#if !UNITY_EDITOR
                SkipSplashScreen();
#endif

            }
        }   

        static void SkipSplashScreen()
        {
#if UNITY_WEBGL
            void HandleFocusChanged(bool obj)
            {
                Application.focusChanged -= HandleFocusChanged;
                SplashScreen.Stop(SplashScreen.StopBehavior.StopImmediate);
            }
            Application.focusChanged += HandleFocusChanged;
#else

            System.Threading.Tasks.Task.Run(() =>
            {
                SplashScreen.Stop(SplashScreen.StopBehavior.StopImmediate);
            });
#endif
        }
    }
}