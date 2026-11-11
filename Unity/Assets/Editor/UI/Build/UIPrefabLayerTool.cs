using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Ux.Editor.Build.UI
{
    /// <summary>
    /// 扫描并修正 UI 预制体里跑偏的 Layer。
    ///
    /// 背景：UICamera 只渲染 UI / UIModel 层，预制体历史上大量节点留在 Default，
    /// 运行时靠 <c>SetLayer</c> 兜底（每个列表项实例化都要遍历一次整棵子树）。
    /// 把 Layer 在预制体上修好，运行时那次遍历就可以省掉。
    ///
    /// 只改 <b>Default 层</b>的节点；其它非 UI 层（UIModel / RTModel 等）保持原样，
    /// 那些是运行时有意设置的特殊层。
    /// </summary>
    public static class UIPrefabLayerTool
    {
        private const string RootFolder = "Assets/GameRes/Res/UI";

        private readonly struct Stats
        {
            public readonly int DefaultCount;
            public readonly int OtherCount;
            public readonly string OtherSummary;

            public Stats(int defaultCount, int otherCount, string otherSummary)
            {
                DefaultCount = defaultCount;
                OtherCount = otherCount;
                OtherSummary = otherSummary;
            }
        }      
    }
}
