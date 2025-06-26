using System;
using System.IO;
using System.Text;
using System.Collections;
using UnityEngine;
using NUnit.Framework;
using YooAsset;

/// <summary>
/// �ļ������ܷ�ʽ
/// </summary>
public class FileStreamEncryption : IEncryptionServices
{
    public EncryptResult Encrypt(EncryptFileInfo fileInfo)
    {
        // ˵������TestRes3��ԴĿ¼���м���
        if (fileInfo.BundleName.Contains("_testres3_"))
        {
            var fileData = File.ReadAllBytes(fileInfo.FileLoadPath);
            for (int i = 0; i < fileData.Length; i++)
            {
                fileData[i] ^= BundleStream.KEY;
            }

            EncryptResult result = new EncryptResult();
            result.Encrypted = true;
            result.EncryptedData = fileData;
            return result;
        }
        else
        {
            EncryptResult result = new EncryptResult();
            result.Encrypted = false;
            return result;
        }
    }
}

/// <summary>
/// �ļ�ƫ�Ƽ��ܷ�ʽ
/// </summary>
public class FileOffsetEncryption : IEncryptionServices
{
    public EncryptResult Encrypt(EncryptFileInfo fileInfo)
    {
        // ˵������TestRes3��ԴĿ¼���м���
        if (fileInfo.BundleName.Contains("_testres3_"))
        {
            int offset = 32;
            byte[] fileData = File.ReadAllBytes(fileInfo.FileLoadPath);
            var encryptedData = new byte[fileData.Length + offset];
            Buffer.BlockCopy(fileData, 0, encryptedData, offset, fileData.Length);

            EncryptResult result = new EncryptResult();
            result.Encrypted = true;
            result.EncryptedData = encryptedData;
            return result;
        }
        else
        {
            EncryptResult result = new EncryptResult();
            result.Encrypted = false;
            return result;
        }
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

/// <summary>
/// ��Դ�ļ���������
/// </summary>
public class FileStreamDecryption : IDecryptionServices
{
    /// <summary>
    /// ͬ����ʽ��ȡ���ܵ���Դ������
    /// ע�⣺��������������Դ�������ͷŵ�ʱ����Զ��ͷ�
    /// </summary>
    DecryptResult IDecryptionServices.LoadAssetBundle(DecryptFileInfo fileInfo)
    {
        BundleStream bundleStream = new BundleStream(fileInfo.FileLoadPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        DecryptResult decryptResult = new DecryptResult();
        decryptResult.ManagedStream = bundleStream;
        decryptResult.Result = AssetBundle.LoadFromStream(bundleStream, fileInfo.FileLoadCRC, GetManagedReadBufferSize());
        return decryptResult;
    }

    /// <summary>
    /// �첽��ʽ��ȡ���ܵ���Դ������
    /// ע�⣺��������������Դ�������ͷŵ�ʱ����Զ��ͷ�
    /// </summary>
    DecryptResult IDecryptionServices.LoadAssetBundleAsync(DecryptFileInfo fileInfo)
    {
        BundleStream bundleStream = new BundleStream(fileInfo.FileLoadPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        DecryptResult decryptResult = new DecryptResult();
        decryptResult.ManagedStream = bundleStream;
        decryptResult.CreateRequest = AssetBundle.LoadFromStreamAsync(bundleStream, fileInfo.FileLoadCRC, GetManagedReadBufferSize());
        return decryptResult;
    }

    /// <summary>
    /// ��ȡ���ܵ��ֽ�����
    /// </summary>
    byte[] IDecryptionServices.ReadFileData(DecryptFileInfo fileInfo)
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// ��ȡ���ܵ��ı�����
    /// </summary>
    string IDecryptionServices.ReadFileText(DecryptFileInfo fileInfo)
    {
        throw new System.NotImplementedException();
    }

    private static uint GetManagedReadBufferSize()
    {
        return 1024;
    }
}

/// <summary>
/// ��Դ�ļ�ƫ�ƽ�����
/// </summary>
public class FileOffsetDecryption : IDecryptionServices
{
    /// <summary>
    /// ͬ����ʽ��ȡ���ܵ���Դ������
    /// ע�⣺��������������Դ�������ͷŵ�ʱ����Զ��ͷ�
    /// </summary>
    DecryptResult IDecryptionServices.LoadAssetBundle(DecryptFileInfo fileInfo)
    {
        DecryptResult decryptResult = new DecryptResult();
        decryptResult.ManagedStream = null;
        decryptResult.Result = AssetBundle.LoadFromFile(fileInfo.FileLoadPath, fileInfo.FileLoadCRC, GetFileOffset());
        return decryptResult;
    }

    /// <summary>
    /// �첽��ʽ��ȡ���ܵ���Դ������
    /// ע�⣺��������������Դ�������ͷŵ�ʱ����Զ��ͷ�
    /// </summary>
    DecryptResult IDecryptionServices.LoadAssetBundleAsync(DecryptFileInfo fileInfo)
    {
        DecryptResult decryptResult = new DecryptResult();
        decryptResult.ManagedStream = null;
        decryptResult.CreateRequest = AssetBundle.LoadFromFileAsync(fileInfo.FileLoadPath, fileInfo.FileLoadCRC, GetFileOffset());
        return decryptResult;
    }

    /// <summary>
    /// ��ȡ���ܵ��ֽ�����
    /// </summary>
    byte[] IDecryptionServices.ReadFileData(DecryptFileInfo fileInfo)
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// ��ȡ���ܵ��ı�����
    /// </summary>
    string IDecryptionServices.ReadFileText(DecryptFileInfo fileInfo)
    {
        throw new System.NotImplementedException();
    }

    private static ulong GetFileOffset()
    {
        return 32;
    }
}

/// <summary>
/// WebGLƽ̨������
/// ע�⣺WebGLƽ̨֧���ڴ����
/// </summary>
public class WebFileStreamDecryption : IWebDecryptionServices
{
    public WebDecryptResult LoadAssetBundle(WebDecryptFileInfo fileInfo)
    {
        /*
        byte[] copyData = new byte[fileInfo.FileData.Length];
        Buffer.BlockCopy(fileInfo.FileData, 0, copyData, 0, fileInfo.FileData.Length);

        for (int i = 0; i < copyData.Length; i++)
        {
            copyData[i] ^= BundleStream.KEY;
        }

        WebDecryptResult decryptResult = new WebDecryptResult();
        decryptResult.Result = AssetBundle.LoadFromMemory(copyData);
        return decryptResult;
        */

        for (int i = 0; i < fileInfo.FileData.Length; i++)
        {
            fileInfo.FileData[i] ^= BundleStream.KEY;
        }

        WebDecryptResult decryptResult = new WebDecryptResult();
        decryptResult.Result = AssetBundle.LoadFromMemory(fileInfo.FileData);
        return decryptResult;
    }
}