using UnityEngine;

/// <summary>
/// 存放整个地图的视野信息
/// </summary>
public class VisionGrid
{
    /// <summary>
    /// 存放所有格子数据，表示当前哪些玩家有视野
    /// </summary>
    public int[] m_values;

    /// <summary>
    /// 存放所有格子数据，表示玩家对应访问过的视野
    /// </summary>
    public int[] m_visited;

    public VisionGrid(int w, int h)
    {
        m_values = new int[w * h];
        m_visited = new int[w * h];
    }
    public void Reset()
    {
        for (int i = 0; i < m_values.Length; i++)
            m_values[i] = 0;
    }

    public void SetVisible(int index, int entityMask)
    {        
        m_values[index] |= entityMask;
        m_visited[index] |= entityMask;
    }

    public bool IsVisible(int index, int entityMask)
    {
        return (m_values[index] & entityMask) > 0;
    }

    public bool WasVisible(int index, int entityMask)
    {
        return (m_visited[index] & entityMask) > 0;
    }
}