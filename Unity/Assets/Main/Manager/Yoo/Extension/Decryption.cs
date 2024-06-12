using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using YooAsset;

/// <summary>
/// ��Դ�ļ������ؽ�����
/// </summary>
public class FileStreamDecryption : IDecryptionServices
{
    /// <summary>
    /// ͬ����ʽ��ȡ���ܵ���Դ������
    /// ע�⣺��������������Դ�������ͷŵ�ʱ����Զ��ͷ�
    /// </summary>
    AssetBundle IDecryptionServices.LoadAssetBundle(DecryptFileInfo fileInfo, out Stream managedStream)
    {
        BundleStream bundleStream = new BundleStream(fileInfo.FileLoadPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        managedStream = bundleStream;
        return AssetBundle.LoadFromStream(bundleStream, fileInfo.ConentCRC, GetManagedReadBufferSize());
    }

    /// <summary>
    /// �첽��ʽ��ȡ���ܵ���Դ������
    /// ע�⣺��������������Դ�������ͷŵ�ʱ����Զ��ͷ�
    /// </summary>
    AssetBundleCreateRequest IDecryptionServices.LoadAssetBundleAsync(DecryptFileInfo fileInfo, out Stream managedStream)
    {
        BundleStream bundleStream = new BundleStream(fileInfo.FileLoadPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        managedStream = bundleStream;
        return AssetBundle.LoadFromStreamAsync(bundleStream, fileInfo.ConentCRC, GetManagedReadBufferSize());
    }

    private static uint GetManagedReadBufferSize()
    {
        return 1024;
    }
}

/// <summary>
/// ��Դ�ļ�ƫ�Ƽ��ؽ�����
/// </summary>
public class FileOffsetDecryption : IDecryptionServices
{
    /// <summary>
    /// ͬ����ʽ��ȡ���ܵ���Դ������
    /// ע�⣺��������������Դ�������ͷŵ�ʱ����Զ��ͷ�
    /// </summary>
    AssetBundle IDecryptionServices.LoadAssetBundle(DecryptFileInfo fileInfo, out Stream managedStream)
    {
        managedStream = null;
        return AssetBundle.LoadFromFile(fileInfo.FileLoadPath, fileInfo.ConentCRC, GetFileOffset());
    }

    /// <summary>
    /// �첽��ʽ��ȡ���ܵ���Դ������
    /// ע�⣺��������������Դ�������ͷŵ�ʱ����Զ��ͷ�
    /// </summary>
    AssetBundleCreateRequest IDecryptionServices.LoadAssetBundleAsync(DecryptFileInfo fileInfo, out Stream managedStream)
    {
        managedStream = null;
        return AssetBundle.LoadFromFileAsync(fileInfo.FileLoadPath, fileInfo.ConentCRC, GetFileOffset());
    }

    private static ulong GetFileOffset()
    {
        return 32;
    }
}


/// <summary>
/// ��Դ�ļ�������
/// </summary>
public class BundleStream : FileStream
{
    public const byte KEY = 64;

    public BundleStream(string path, FileMode mode, FileAccess access, FileShare share) : base(path, mode, access, share)
    {
    }
    public BundleStream(string path, FileMode mode) : base(path, mode)
    {
    }

    public override int Read(byte[] array, int offset, int count)
    {
        var index = base.Read(array, offset, count);
        for (int i = 0; i < array.Length; i++)
        {
            array[i] ^= KEY;
        }
        return index;
    }
}