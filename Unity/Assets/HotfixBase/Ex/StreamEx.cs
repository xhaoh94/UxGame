using System;
using System.IO;

namespace Ux
{
    public static class StreamEx
    {       
        public static void WriteMessage(this MemoryStream stream, object message, int offset)
        {
            stream.Seek(offset, SeekOrigin.Begin);
            ProtoBuf.Serializer.Serialize(stream, message);
        }        
        public static object ReadMessage(this MemoryStream stream, Type type, int offset)
        {
            stream.Seek(offset, SeekOrigin.Begin);
            return ProtoBuf.Serializer.Deserialize(type, stream);
        }

    }
}
