using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YooAsset;

/// <summary>
/// ��Դ��ѯ������
/// </summary>
public class DeliveryQueryServices : IDeliveryQueryServices
{
    public string GetFilePath(string packageName, string fileName)
    {
        return string.Empty;
    }

    public bool Query(string packageName, string fileName, string fileCRC)
    {
        return false;
    }

}

public class DeliveryLoadServices : IDeliveryLoadServices
{
    public AssetBundle LoadAssetBundle(DeliveryFileInfo fileInfo)
    {
        return null;
    }

    public AssetBundleCreateRequest LoadAssetBundleAsync(DeliveryFileInfo fileInfo)
    {
        return null;
    }
}
