using System;
using System.IO;
using Ux;
using YooAsset;
using static Ux.YooMgr;

public class UxEncryption : IEncryptionServices
{
    private const uint MAGIC = 0x58425558; // UXBX
    private const byte VERSION = 1;
    private const int HEADER_SIZE = 16;
    private const long MAX_XXTEA_SIZE = 10 * 1024 * 1024; // 10MB

    public EncryptResult Encrypt(EncryptFileInfo fileInfo)
    {
        // 先读取文件内容
        byte[] rawData = File.ReadAllBytes(fileInfo.FileLoadPath);
        long fileSize = rawData.Length;
        
        // 根据文件名和大小判断加密类型
        EncyptionType encryptionType = DetermineEncryptionType(fileInfo.BundleName, fileSize);
        
        byte[] encryptedPayload = rawData;

        switch (encryptionType)
        {
            case EncyptionType.None:
                break;

            case EncyptionType.Offset:
                {
                    encryptedPayload = new byte[rawData.Length + BundleHeader.OFFSET_SIZE];
                    Buffer.BlockCopy(rawData, 0, encryptedPayload, BundleHeader.OFFSET_SIZE, rawData.Length);
                    break;
                }

            case EncyptionType.XOR:
                encryptedPayload = XORHelper.Encrypt(rawData);
                break;

            case EncyptionType.XXTEA:
                encryptedPayload = XXTEAHelper.Encrypt(rawData);
                break;
        }

        // 写工业级头
        byte[] finalData = new byte[HEADER_SIZE + encryptedPayload.Length];
        using (MemoryStream ms = new MemoryStream(finalData))
        using (BinaryWriter bw = new BinaryWriter(ms))
        {
            bw.Write(MAGIC);
            bw.Write(VERSION);
            bw.Write((byte)encryptionType);
            bw.Write((ushort)0);
            bw.Write((uint)rawData.Length);
            bw.Write((uint)0);

            bw.Write(encryptedPayload);
        }

        return new EncryptResult
        {
            Encrypted = encryptionType != EncyptionType.None,
            EncryptedData = finalData
        };
    }
    
    private EncyptionType DetermineEncryptionType(string bundleName, long fileSize)
    {
        if (bundleName.Contains("_xxtea_"))
        {
            if (fileSize > MAX_XXTEA_SIZE)
            {
                Log.Warning($"Bundle {bundleName} 大小 {fileSize / 1024 / 1024}MB 超过 10MB，降级使用 XOR 加密");
                return EncyptionType.XOR;
            }
            return EncyptionType.XXTEA;
        }

        if (bundleName.Contains("_xor_"))
        {
            return EncyptionType.XOR;
        }

        if (bundleName.Contains("_testres3_"))
        {
            return EncyptionType.Offset;
        }

        return EncyptionType.None;
    }
}