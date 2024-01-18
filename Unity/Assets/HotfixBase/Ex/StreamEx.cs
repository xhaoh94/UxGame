using System;
using System.IO;

namespace Ux
{
    public static class StreamEx
    {       
        public static void WriteToMessage(this MemoryStream stream, object message, int offset)
        {
            stream.Seek(offset, SeekOrigin.Begin);
            ProtoBuf.Serializer.Serialize(stream, message);
        }        
        public static object ReadToMessage(this MemoryStream stream, Type type, int offset)
        {
            stream.Seek(offset, SeekOrigin.Begin);
            return ProtoBuf.Serializer.Deserialize(type, stream);
        }

    }
}
