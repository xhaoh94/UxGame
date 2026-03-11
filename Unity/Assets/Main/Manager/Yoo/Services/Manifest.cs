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

        // 输出格式：[4字节Magic][1字节Version][3字节保留][加密数据]
        byte[] result = new byte[HEADER_SIZE + data.Length];
        
        // 写入文件头
        BitConverter.GetBytes(MAGIC).CopyTo(result, 0);
        result[4] = VERSION;
        // result[5-7] 保留字节，可用于未来扩展
        
        // 加密数据部分（位置相关的XOR）
        for (int i = 0; i < data.Length; i++)
        {
            int resultIndex = HEADER_SIZE + i;
            
            // 第一层：与密钥循环XOR
            byte keyByte = KEY[i % KEY.Length];
            
            // 第二层：与位置相关（增加破解难度）
            byte positionByte = (byte)((i * 0x9E) ^ (i >> 3));
            
            // 第三层：与文件头部分字节混合（增加关联性）
            byte headerByte = result[(i % 4) + 1]; // 使用Version和保留字节
            
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

        // 验证文件头
        uint magic = BitConverter.ToUInt32(data, 0);
        if (magic != MAGIC)
        {
            // 文件头不匹配，可能是未加密的原始文件，直接返回
            // 或者抛出异常，根据业务需求决定
            Log.Warning("Manifest magic mismatch, treating as unencrypted");
            return data;
        }

        byte version = data[4];
        if (version != VERSION)
        {
            Log.Warning($"Manifest version mismatch: expected {VERSION}, got {version}");
            // 版本不匹配时，尝试用当前版本算法解密（向后兼容）
        }

        // 解密数据
        int payloadLength = data.Length - HEADER_SIZE;
        byte[] result = new byte[payloadLength];
        
        for (int i = 0; i < payloadLength; i++)
        {
            int dataIndex = HEADER_SIZE + i;
            
            // 逆向三层XOR
            byte keyByte = KEY[i % KEY.Length];
            byte positionByte = (byte)((i * 0x9E) ^ (i >> 3));
            byte headerByte = data[(i % 4) + 1];
            
            result[i] = (byte)(data[dataIndex] ^ keyByte ^ positionByte ^ headerByte);
        }

        return result;
    }
}