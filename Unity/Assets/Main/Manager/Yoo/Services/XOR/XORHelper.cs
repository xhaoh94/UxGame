using System;
using System.IO;

public static class XORHelper
{
    public static readonly byte[] KEY =
    {
        0x3A,0x7F,0xC2,0x15,
        0x8E,0x2B,0x9F,0x4D,
        0x6C,0x1A,0x7B,0xE5,
        0x2F,0x9A,0x8B,0x3C
    };

    public static byte[] Encrypt(byte[] data)
    {
        if (data == null || data.Length == 0)
            return data;
            
        byte[] result = new byte[data.Length];
        Buffer.BlockCopy(data, 0, result, 0, data.Length);
        
        for (int i = 0; i < result.Length; i++)
        {
            result[i] ^= KEY[i % KEY.Length];
        }
        
        return result;
    }

    public static byte[] Decrypt(byte[] data)
    {
        return Encrypt(data); // XOR 对称
    }
}

/// <summary>
/// XOR 流包装器，包装任意 Stream 进行透明 XOR 解密
/// 并且隐藏底层的 payloadOffset（如 Header 的大小）
/// </summary>
public class XORStreamWrapper : Stream
{
    private readonly Stream _base;
    private readonly bool _ownsBase;
    private readonly long _payloadOffset; // 负载的起始位置

    /// <param name="stream">底层流</param>
    /// <param name="payloadOffset">资源包数据在流中的起始偏移</param>
    /// <param name="ownsBase">是否在 Dispose 时一并释放底层流</param>
    public XORStreamWrapper(Stream stream, long payloadOffset, bool ownsBase = false)
    {
        _base = stream;
        _payloadOffset = payloadOffset;
        _ownsBase = ownsBase;

        // 确保流位于有效起始位置
        _base.Position = payloadOffset;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        // 使用相对位置 logicalPos 来计算密钥索引
        long logicalPos = Position;
        int read = _base.Read(buffer, offset, count);

        for (int i = 0; i < read; i++)
        {
            buffer[offset + i] ^= XORHelper.KEY[(logicalPos + i) % XORHelper.KEY.Length];
        }

        return read;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _ownsBase)
        {
            _base?.Dispose();
        }
        base.Dispose(disposing);
    }

    // 彻底隔离基础流和 Unity 的交互
    public override long Position
    {
        get => _base.Position - _payloadOffset;
        set => _base.Position = value + _payloadOffset;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        switch (origin)
        {
            case SeekOrigin.Begin:
                _base.Position = offset + _payloadOffset;
                break;
            case SeekOrigin.Current:
                _base.Position += offset;
                break;
            case SeekOrigin.End:
                _base.Position = _base.Length + offset;
                break;
        }
        return Position;
    }

    public override long Length => _base.Length - _payloadOffset;
    public override bool CanRead => _base.CanRead;
    public override bool CanSeek => _base.CanSeek;
    public override bool CanWrite => false;
    public override void Flush() => _base.Flush();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}