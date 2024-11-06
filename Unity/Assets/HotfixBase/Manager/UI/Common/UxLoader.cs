using FairyGUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.U2D;

namespace Ux
{
    public class UxLoader : GLoader
    {
        protected override void LoadExternal()
        {
            //base.LoadExternal();
            var sa = ResMgr.Ins.LoadAsset<SpriteAtlas>($"{PathHelper.Res.Atlas}/items", YooType.Main);
            var sp = sa.GetSprite(url);
            if (sp == null)
            {
                Log.Debug(url);
                onExternalLoadFailed();
            }
            else
            {
                onExternalLoadSuccess(new NTexture(sp));
            }
        }
        protected override void FreeExternal(NTexture texture)
        {
            base.FreeExternal(texture);
        }
    }
}
