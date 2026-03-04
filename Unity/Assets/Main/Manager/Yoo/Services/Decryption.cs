using System;
using System.IO;
using UnityEngine;
using Ux;
using YooAsset;

#region ===== Header 定义 =====

public static class BundleHeader
{
    public const uint MAGIC = 0x58425558; // "UXBX"
    public const int HEADER_SIZE = 16;
    public const int OFFSET_SIZE = 32;

    public static bool TryReadHeader(Stream stream, out YooMgr.EncyptionType type)
    {
        type = YooMgr.EncyptionType.None;
        long pos = stream.Position;

        try
        {
            if (stream.Length - stream.Position < HEADER_SIZE)
                return false;

            using (BinaryReader br = new BinaryReader(stream, System.Text.Encoding.UTF8, true))
            {
                uint magic = br.ReadUInt32();
                if (magic != MAGIC)
                {
                    stream.Position = pos;
                    return false;
                }

                br.ReadByte(); // version
                type = (YooMgr.EncyptionType)br.ReadByte();
                br.ReadUInt16();
                br.ReadUInt32();
                br.ReadUInt32();
            }
            return true;
        }
        catch
        {
            stream.Position = pos;
            return false;
        }
    }
}

#endregion

#region ===== 本地解密 =====

public class UxDecryption : IDecryptionServices
{
    DecryptResult IDecryptionServices.LoadAssetBundle(DecryptFileInfo fileInfo)
    {
        return LoadAssetBundleInternal(fileInfo, false);
    }

    DecryptResult IDecryptionServices.LoadAssetBundleAsync(DecryptFileInfo fileInfo)
    {
        return LoadAssetBundleInternal(fileInfo, true);
    }

    private DecryptResult LoadAssetBundleInternal(DecryptFileInfo fileInfo, bool isAsync)
    {
        DecryptResult result = new DecryptResult();
        FileStream fs = null;

        try
        {
            fs = new FileStream(fileInfo.FileLoadPath, FileMode.Open, FileAccess.Read, FileShare.Read);

            if (!BundleHeader.TryReadHeader(fs, out var encryptionType))
            {
                fs.Dispose();
                fs = null;

                if (isAsync)
                {
                    result.CreateRequest = AssetBundle.LoadFromFileAsync(fileInfo.FileLoadPath, fileInfo.FileLoadCRC);
                }
                else
                {
                    result.Result = AssetBundle.LoadFromFile(fileInfo.FileLoadPath, fileInfo.FileLoadCRC);
                }
                return result;
            }

            const int payloadOffset = BundleHeader.HEADER_SIZE;

            switch (encryptionType)
            {
                case YooMgr.EncyptionType.None:
                    fs.Dispose();
                    fs = null;

                    if (isAsync)
                    {
                        result.CreateRequest = AssetBundle.LoadFromFileAsync(fileInfo.FileLoadPath, fileInfo.FileLoadCRC, (ulong)payloadOffset);
                    }
                    else
                    {
                        result.Result = AssetBundle.LoadFromFile(fileInfo.FileLoadPath, fileInfo.FileLoadCRC, (ulong)payloadOffset);
                    }
                    break;

                case YooMgr.EncyptionType.Offset:
                    fs.Dispose();
                    fs = null;

                    ulong offset = (ulong)(payloadOffset + BundleHeader.OFFSET_SIZE);
                    if (isAsync)
                    {
                        result.CreateRequest = AssetBundle.LoadFromFileAsync(fileInfo.FileLoadPath, fileInfo.FileLoadCRC, offset);
                    }
                    else
                    {
                        result.Result = AssetBundle.LoadFromFile(fileInfo.FileLoadPath, fileInfo.FileLoadCRC, offset);
                    }
                    break;

                case YooMgr.EncyptionType.XOR:
                    var xorStream = new XORStreamWrapper(fs, payloadOffset, ownsBase: true);
                    fs = null;
                    result.ManagedStream = xorStream;

                    if (isAsync)
                    {
                        result.CreateRequest = AssetBundle.LoadFromStreamAsync(xorStream, fileInfo.FileLoadCRC);
                    }
                    else
                    {
                        result.Result = AssetBundle.LoadFromStream(xorStream, fileInfo.FileLoadCRC);
                    }
                    break;

                case YooMgr.EncyptionType.XXTEA:
                    fs.Position = payloadOffset;
                    int encLength = (int)(fs.Length - fs.Position);
                    byte[] encData = new byte[encLength];
                    int bytesRead = fs.Read(encData, 0, encLength);
                    if (bytesRead != encLength)
                    {
                        fs.Dispose();
                        throw new IOException($"Expected to read {encLength} bytes but only read {bytesRead} bytes");
                    }
                    fs.Dispose();
                    fs = null;

                    byte[] decData = XXTEAHelper.Decrypt(encData);
                    var ms = new MemoryStream(decData);
                    result.ManagedStream = ms;

                    if (isAsync)
                    {
                        result.CreateRequest = AssetBundle.LoadFromStreamAsync(ms, fileInfo.FileLoadCRC);
                    }
                    else
                    {
                        result.Result = AssetBundle.LoadFromStream(ms, fileInfo.FileLoadCRC);
                    }
                    break;

                default:
                    fs.Dispose();
                    fs = null;

                    if (isAsync)
                    {
                        result.CreateRequest = AssetBundle.LoadFromFileAsync(fileInfo.FileLoadPath, fileInfo.FileLoadCRC, (ulong)payloadOffset);
                    }
                    else
                    {
                        result.Result = AssetBundle.LoadFromFile(fileInfo.FileLoadPath, fileInfo.FileLoadCRC, (ulong)payloadOffset);
                    }
                    break;
            }

            return result;
        }
        catch
        {
            fs?.Dispose();
            throw;
        }
    }

    DecryptResult IDecryptionServices.LoadAssetBundleFallback(DecryptFileInfo fileInfo)
    {
        byte[] fileData = File.ReadAllBytes(fileInfo.FileLoadPath);

        using MemoryStream ms = new MemoryStream(fileData);
        if (!BundleHeader.TryReadHeader(ms, out var encryptionType))
        {
            return new DecryptResult
            {
                Result = AssetBundle.LoadFromMemory(fileData)
            };
        }

        const int payloadOffset = BundleHeader.HEADER_SIZE;
        byte[] payload = new byte[fileData.Length - payloadOffset];
        Buffer.BlockCopy(fileData, payloadOffset, payload, 0, payload.Length);

        switch (encryptionType)
        {
            case YooMgr.EncyptionType.Offset:
                {
                    int off = BundleHeader.OFFSET_SIZE;
                    byte[] tmp = new byte[payload.Length - off];
                    Buffer.BlockCopy(payload, off, tmp, 0, tmp.Length);
                    payload = tmp;
                    break;
                }
            case YooMgr.EncyptionType.XOR:
                payload = XORHelper.Decrypt(payload);
                break;
            case YooMgr.EncyptionType.XXTEA:
                payload = XXTEAHelper.Decrypt(payload);
                break;
        }

        return new DecryptResult
        {
            Result = AssetBundle.LoadFromMemory(payload)
        };
    }

    byte[] IDecryptionServices.ReadFileData(DecryptFileInfo fileInfo) => throw new NotImplementedException();
    string IDecryptionServices.ReadFileText(DecryptFileInfo fileInfo) => throw new NotImplementedException();
}

#endregion

#region ===== WebGL 解密 =====

public class WebFileMemoryDecryption : IWebDecryptionServices
{
    public WebDecryptResult LoadAssetBundle(WebDecryptFileInfo fileInfo)
    {
        byte[] src = fileInfo.FileData;

        using MemoryStream ms = new MemoryStream(src);
        if (!BundleHeader.TryReadHeader(ms, out var encryptionType))
        {
            return new WebDecryptResult
            {
                Result = AssetBundle.LoadFromMemory(src)
            };
        }

        const int payloadOffset = BundleHeader.HEADER_SIZE;
        byte[] payload = new byte[src.Length - payloadOffset];
        Buffer.BlockCopy(src, payloadOffset, payload, 0, payload.Length);

        switch (encryptionType)
        {
            case YooMgr.EncyptionType.Offset:
                {
                    int off = BundleHeader.OFFSET_SIZE;
                    byte[] tmp = new byte[payload.Length - off];
                    Buffer.BlockCopy(payload, off, tmp, 0, tmp.Length);
                    payload = tmp;
                    break;
                }
            case YooMgr.EncyptionType.XOR:
                payload = XORHelper.Decrypt(payload);
                break;
            case YooMgr.EncyptionType.XXTEA:
                payload = XXTEAHelper.Decrypt(payload);
                break;
        }

        return new WebDecryptResult
        {
            Result = AssetBundle.LoadFromMemory(payload)
        };
    }
}

#endregion
