namespace Ux
{
    /// <summary>宏观状态变化来源，仅用于确定性调试、快照恢复和网络纠正。</summary>
    public enum StateChangeReason : byte
    {
        Initialize,
        CodeRule,
        ActionStarted,
        ActionEnded,
        ExternalRequest,
        SnapshotRestore,
    }
}
