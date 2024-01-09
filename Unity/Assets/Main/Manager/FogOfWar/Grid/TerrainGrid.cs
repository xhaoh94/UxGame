using UnityEngine;
using Ux;

/// <summary>
/// 格子存放地形相关的信息
/// </summary>
public class TerrainGrid 
{
    /// <summary>
    /// 海拔高度
    /// </summary>
    readonly short[] m_altitudes;

    /// <summary>
    /// 表示所在的草丛（没草丛则是0）
    /// </summary>
    readonly short[] m_grassIds;
    public TerrainGrid(int w, int h)
    {
        m_altitudes = new short[w * h];
        m_grassIds = new short[w * h];
    }

    public void SetAltitude(int index, short a)
    {
        m_altitudes[index] = a;
    }
    public short GetAltitude(int index)
    {
        if (index < 0 || index >= m_altitudes.Length) return 0;
        return m_altitudes[index];
    }

    public void SetGrass(int index, short id)
    {
        m_grassIds[index] = id;
    }
    public short GetGrass(int index)
    {
        if (index < 0 || index >= m_altitudes.Length) return 0;
        return m_grassIds[index];
    }
}