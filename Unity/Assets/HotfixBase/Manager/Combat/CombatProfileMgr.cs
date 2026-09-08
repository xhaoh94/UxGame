namespace Ux
{
    /// <summary>角色战斗配置资源入口。所有运行时状态都保存在每个 Unit 的 CombatController 中。</summary>
    public sealed class CombatProfileMgr : Singleton<CombatProfileMgr>
    {
        public CharacterCombatProfile LoadAsset(string assetName)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                return null;
            }
            var location = string.Format(PathHelper.Res.Combat, assetName);
            return ResMgr.Ins.LoadAsset<CharacterCombatProfile>(location);
        }
    }
}
