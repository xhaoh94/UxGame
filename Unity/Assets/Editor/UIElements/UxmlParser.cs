using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

/// <summary>
/// UXML 解析接口 - 兼容所有Unity版本
/// 功能：传入UXML字符串/路径，获取所有设置了name的子节点
/// </summary>
public interface IUxmlParser
{
    /// <summary>
    /// 从UXML字符串中获取所有带name属性的节点
    /// </summary>
    /// <param name="uxmlContent">UXML字符串</param>
    /// <returns>带name的节点字典 (name -> 节点对象)</returns>
    Dictionary<string, XmlNode> GetNamedNodesFromUxmlString(string uxmlContent);

    /// <summary>
    /// 从UXML文件路径中获取所有带name属性的节点
    /// </summary>
    /// <param name="uxmlFilePath">UXML文件路径</param>
    /// <returns>带name的节点字典 (name -> 节点对象)</returns>
    Dictionary<string, XmlNode> GetNamedNodesFromUxmlFile(string uxmlFilePath);
}

/// <summary>
/// UXML解析器实现（跨Unity版本兼容）
/// </summary>
public class UxmlParser : IUxmlParser
{
    // 兼容所有Unity UXML命名空间
    private readonly string[] _uxmlNamespaces = {
        "UnityEngine.UIElements",
        "UnityEditor.UIElements",
        "http://schemas.unity3d.com/2019/UIElements",
        "http://schemas.unity3d.com/2020/UIElements",
        "http://schemas.unity3d.com/2021/UIElements",
        "http://schemas.unity3d.com/2022/UIElements"
    };

    #region 公开方法
    /// <summary>
    /// 从UXML字符串解析命名节点
    /// </summary>
    public Dictionary<string, XmlNode> GetNamedNodesFromUxmlString(string uxmlContent)
    {
        var result = new Dictionary<string, XmlNode>();
        
        if (string.IsNullOrWhiteSpace(uxmlContent))
        {
            Debug.LogWarning("UXML字符串为空！");
            return result;
        }

        try
        {
            var xmlDoc = new XmlDocument();
            // 禁用XML解析限制，兼容复杂UXML
            xmlDoc.XmlResolver = null;
            
            // 加载UXML
            xmlDoc.LoadXml(uxmlContent);
            
            // 递归遍历所有节点
            TraverseNodes(xmlDoc.DocumentElement, result);
        }
        catch (Exception e)
        {
            Debug.LogError($"UXML解析失败：{e.Message}");
        }

        return result;
    }

    /// <summary>
    /// 从UXML文件解析命名节点
    /// </summary>
    public Dictionary<string, XmlNode> GetNamedNodesFromUxmlFile(string uxmlFilePath)
    {
        try
        {
            string uxmlContent = System.IO.File.ReadAllText(uxmlFilePath);
            return GetNamedNodesFromUxmlString(uxmlContent);
        }
        catch (Exception e)
        {
            Debug.LogError($"UXML文件读取失败：{e.Message}");
            return new Dictionary<string, XmlNode>();
        }
    }
    #endregion

    #region 私有工具方法
    /// <summary>
    /// 递归遍历所有节点，提取带name属性的节点
    /// </summary>
    private void TraverseNodes(XmlNode currentNode, Dictionary<string, XmlNode> resultDict)
    {
        if (currentNode == null) return;

        // 跳过注释、声明等非元素节点
        if (currentNode.NodeType != XmlNodeType.Element)
        {
            TraverseChildNodes(currentNode, resultDict);
            return;
        }

        // 检查节点是否有 name 属性（兼容大小写）
        XmlAttribute nameAttr = currentNode.Attributes["name"] ?? currentNode.Attributes["Name"];
        
        if (nameAttr != null && !string.IsNullOrWhiteSpace(nameAttr.Value))
        {
            string nodeName = nameAttr.Value.Trim();
            
            // 避免重复name覆盖
            if (!resultDict.ContainsKey(nodeName))
            {
                resultDict.Add(nodeName, currentNode);
            }
        }

        // 递归遍历子节点
        TraverseChildNodes(currentNode, resultDict);
    }

    /// <summary>
    /// 遍历子节点
    /// </summary>
    private void TraverseChildNodes(XmlNode parentNode, Dictionary<string, XmlNode> resultDict)
    {
        if (parentNode.ChildNodes == null || parentNode.ChildNodes.Count == 0) return;
        
        foreach (XmlNode child in parentNode.ChildNodes)
        {
            TraverseNodes(child, resultDict);
        }
    }
    #endregion
}