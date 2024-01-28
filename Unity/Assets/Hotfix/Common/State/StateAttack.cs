using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ux
{
    internal class StateAttack : UnitStateNode
    {
        protected override void OnEnter(object args = null)
        {
            base.OnEnter(args);

        }

        async UniTaskVoid OnGetSkillAsync()
        {
            var asset = await SkillMgr.Ins.GetSkillAssetAsync("ZS_Attack");
            var Unit = (Machine.Owner as Unit);
            Unit.Director.SetPlayableAsset(asset);
            Unit.Director.Play();
        }
    }
}
