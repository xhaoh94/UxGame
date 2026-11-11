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

public class UxDecryption : IBundleMemoryDecryptor
{
    /// <summary>
    /// v3 只保留「整包解密」一种入口：按文件头判断加密类型，返回去掉头/解密后的 AssetBundle 字节。
    /// （v2 的 offset/stream 分派没有了，因为同一包裹里允许混用多种加密类型。）
    /// </summary>
    byte[] IBundleMemoryDecryptor.GetDecryptedData(BundleDecryptArgs args)
    {
        byte[] fileData = args.FileData ?? File.ReadAllBytes(args.FilePath);

        YooMgr.EncyptionType encryptionType;
        using (var ms = new MemoryStream(fileData))
        {
            if (!BundleHeader.TryReadHeader(ms, out encryptionType))
                return fileData; // 没有自定义头：当作明文 AB
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

        return payload;
    }
}

#endregion

