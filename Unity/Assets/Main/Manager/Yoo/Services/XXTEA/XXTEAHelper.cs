using System;
using System.Buffers;
using System.Runtime.InteropServices;

/// <summary>
/// XXTEA 加解密辅助
/// 使用 Span<T> 和 ArrayPool 减少内存分配
/// </summary>
public static class XXTEAHelper
{
    public static readonly byte[] KEY =
    {
        0xD4,0x1E,0xA3,0x67,
        0x5B,0xF0,0x82,0xC9,
        0x3F,0x76,0xAD,0x48,
        0xE1,0x0C,0x95,0xBA
    };

    private const uint DELTA = 0x9E3779B9;

    /// <summary>
    /// 加密数据
    /// 返回格式: [4字节原始长度][加密数据]
    /// </summary>
    public static byte[] Encrypt(byte[] data)
    {
        if (data == null || data.Length == 0)
            return data;

        uint originalLength = (uint)data.Length;
        
        // 计算需要填充到 4 字节对齐的大小
        int paddedLength = (data.Length + 3) & ~3;
        
        // 从池中租用缓冲区，避免频繁 GC
        byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(paddedLength);
        try
        {
            // 复制数据到租用的缓冲区
            Buffer.BlockCopy(data, 0, rentedBuffer, 0, data.Length);
            // 零填充剩余部分
            if (paddedLength > data.Length)
            {
                Array.Clear(rentedBuffer, data.Length, paddedLength - data.Length);
            }

            // 使用 Span 进行零拷贝转换
            Span<uint> v = MemoryMarshal.Cast<byte, uint>(rentedBuffer.AsSpan(0, paddedLength));
            ReadOnlySpan<uint> k = MemoryMarshal.Cast<byte, uint>(KEY.AsSpan());

            EncryptUint(v, k);

            // 分配结果数组 [4字节长度][加密数据]
            byte[] result = new byte[paddedLength + 4];
            BitConverter.GetBytes(originalLength).CopyTo(result, 0);
            Buffer.BlockCopy(rentedBuffer, 0, result, 4, paddedLength);
            
            return result;
        }
        finally
        {
            // 归还租用的缓冲区
            ArrayPool<byte>.Shared.Return(rentedBuffer, clearArray: true);
        }
    }

    /// <summary>
    /// 解密数据
    /// 输入格式: [4字节原始长度][加密数据]
    /// </summary>
    public static byte[] Decrypt(byte[] data)
    {
        if (data == null || data.Length < 4)
            return data;

        uint originalLength = BitConverter.ToUInt32(data, 0);
        int encLen = data.Length - 4;
        
        if (encLen <= 0 || (encLen & 3) != 0) // 检查是否为 4 的倍数
            return data;

        // 从池中租用缓冲区
        byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(encLen);
        try
        {
            // 复制加密数据到租用的缓冲区
            Buffer.BlockCopy(data, 4, rentedBuffer, 0, encLen);

            // 使用 Span 进行零拷贝转换
            Span<uint> v = MemoryMarshal.Cast<byte, uint>(rentedBuffer.AsSpan(0, encLen));
            ReadOnlySpan<uint> k = MemoryMarshal.Cast<byte, uint>(KEY.AsSpan());

            DecryptUint(v, k);

            // 分配结果数组（精确大小）
            byte[] result = new byte[originalLength];
            Buffer.BlockCopy(rentedBuffer, 0, result, 0, (int)originalLength);
            
            return result;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rentedBuffer, clearArray: true);
        }
    }

    // ====== XXTEA 核心算法 ======

    private static void EncryptUint(Span<uint> v, ReadOnlySpan<uint> k)
    {
        int n = v.Length;
        if (n < 2) return;

        uint z = v[n - 1], y, sum = 0;
        int rounds = 6 + 52 / n;

        for (int i = 0; i < rounds; i++)
        {
            sum += DELTA;
            uint e = (sum >> 2) & 3;
            for (int p = 0; p < n - 1; p++)
            {
                y = v[p + 1];
                v[p] += MX(z, y, sum, k[(int)((p & 3) ^ e)]);
                z = v[p];
            }
            y = v[0];
            v[n - 1] += MX(z, y, sum, k[(int)(((n - 1) & 3) ^ e)]);
            z = v[n - 1];
        }
    }

    private static void DecryptUint(Span<uint> v, ReadOnlySpan<uint> k)
    {
        int n = v.Length;
        if (n < 2) return;

        uint y = v[0], z, sum;
        int rounds = 6 + 52 / n;
        sum = (uint)(rounds * DELTA);

        for (int i = 0; i < rounds; i++)
        {
            uint e = (sum >> 2) & 3;
            for (int p = n - 1; p > 0; p--)
            {
                z = v[p - 1];
                v[p] -= MX(z, y, sum, k[(int)((p & 3) ^ e)]);
                y = v[p];
            }
            z = v[n - 1];
            v[0] -= MX(z, y, sum, k[(int)e]);
            y = v[0];
            sum -= DELTA;
        }
    }

    private static uint MX(uint z, uint y, uint sum, uint k)
    {
        return (((z >> 5) ^ (y << 2)) + ((y >> 3) ^ (z << 4))) ^ ((sum ^ y) + (k ^ z));
    }
}