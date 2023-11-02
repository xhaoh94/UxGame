using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Ux
{

    public class SkillMgr : Singleton<SkillMgr>
    {
        Dictionary<string, SkillAsset> resToData = new Dictionary<string, SkillAsset>();
        public async UniTask<SkillAsset> GetSkillAssetAsync(string res)
        {
            if (resToData.TryGetValue(res, out var data))
            {
                return data;
            }
            var handle = ResMgr.Ins.LoadAssetAsync<TimelineAsset>(res);
            await handle.ToUniTask();
            var asset = handle.AssetObject as TimelineAsset;

            data = new SkillAsset(asset);
            resToData[res] = data;
            return data;
        }
    }
}
