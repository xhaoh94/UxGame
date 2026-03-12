using System;
using YooAsset;

public sealed class UxManifestCryptoServices : IManifestProcessServices, IManifestRestoreServices
{
    byte[] IManifestProcessServices.ProcessManifest(byte[] fileData)
    {
        return XorCrypto.Encrypt(fileData);
    }

    byte[] IManifestRestoreServices.RestoreManifest(byte[] fileData)
    {
        return XorCrypto.Decrypt(fileData);
    }
}

public class XorCrypto
{
    // 密钥：16字节（128位）
    private static readonly byte[] KEY = {
        0x3A, 0x7F, 0xC2, 0x15, 0x8E, 0x2B, 0x9F, 0x4D,
        0x6C, 0x1A, 0x7B, 0xE5, 0x2F, 0x9A, 0x8B, 0x3C
    };
    
    // 文件头魔数：用于验证文件是否被篡改
    private static readonly uint MAGIC = 0x584D414E; // "XMAN" (Xor Manifest)
    private static readonly byte VERSION = 1;
    private const int HEADER_SIZE = 8; // 4字节Magic + 1字节Version + 3字节保留

    /// <summary>
    /// 加密数据：添加文件头 + 位置相关XOR
    /// </summary>
    public static byte[] Encrypt(byte[] data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        byte[] result = new byte[HEADER_SIZE + data.Length];
        
        // 显式小端写入（避免 BitConverter 分配）
        result[0] = (byte)MAGIC;
        result[1] = (byte)(MAGIC >> 8);
        result[2] = (byte)(MAGIC >> 16);
        result[3] = (byte)(MAGIC >> 24);
        result[4] = VERSION;
        
        // 预计算 header 字节（避免循环中重复读取）
        Span<byte> headerBytes = stackalloc byte[4] { result[1], result[2], result[3], result[4] };
        
        for (int i = 0; i < data.Length; i++)
        {
            int resultIndex = HEADER_SIZE + i;
            
            // 取模改为位运算（& 15 = % 16，& 3 = % 4）
            byte keyByte = KEY[i & 15];
            byte positionByte = (byte)((i * 0x9E) ^ (i >> 3));
            byte headerByte = headerBytes[i & 3];
            
            result[resultIndex] = (byte)(data[i] ^ keyByte ^ positionByte ^ headerByte);
        }

        return result;
    }

    /// <summary>
    /// 解密数据：验证文件头 + 逆向XOR
    /// </summary>
    public static byte[] Decrypt(byte[] data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        if (data.Length < HEADER_SIZE)
            throw new ArgumentException("Invalid manifest data: too short", nameof(data));

        // 显式小端读取（避免 BitConverter 分配和端序问题）
        uint magic = (uint)(data[0] | (data[1] << 8) | (data[2] << 16) | (data[3] << 24));
        if (magic != MAGIC)
        {
            Log.Warning("Manifest magic mismatch, treating as unencrypted");
            return data;
        }

        byte version = data[4];
        if (version != VERSION)
        {
            Log.Warning($"Manifest version mismatch: expected {VERSION}, got {version}");
        }

        // 预计算 header 字节
        Span<byte> headerBytes = stackalloc byte[4] { data[1], data[2], data[3], data[4] };

        int payloadLength = data.Length - HEADER_SIZE;
        byte[] result = new byte[payloadLength];
        
        for (int i = 0; i < payloadLength; i++)
        {
            int dataIndex = HEADER_SIZE + i;
            
            // 取模改为位运算
            byte keyByte = KEY[i & 15];
            byte positionByte = (byte)((i * 0x9E) ^ (i >> 3));
            byte headerByte = headerBytes[i & 3];
            
            result[i] = (byte)(data[dataIndex] ^ keyByte ^ positionByte ^ headerByte);
        }

        return result;
    }
}