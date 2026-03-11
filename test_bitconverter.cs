using System;

public class Test
{
    public static void Main()
    {
        Span<byte> span = stackalloc byte[4];
        BitConverter.TryWriteBytes(span, 0x584D414E);
        Console.WriteLine("Success");
    }
}
