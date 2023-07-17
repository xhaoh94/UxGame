using System;

namespace Ux
{
    public enum ParserState
    {
        PacketSize,
        PacketBody
    }

    public class PacketParser
    {
        private readonly ByteArray buffer;
        private int packetSize;
        private ParserState state;
        private bool isOK;
        private readonly int packetSizeLength = 2;

        public PacketParser(ByteArray buffer)
        {
            this.buffer = buffer;
        }

        public bool Parse()
        {
            if (this.isOK)
            {
                return true;
            }

            bool finish = false;
            while (!finish)
            {
                switch (this.state)
                {
                    case ParserState.PacketSize:
                        if (this.buffer.Length < this.packetSizeLength)
                        {
                            finish = true;
                        }
                        else
                        {
                            this.packetSize = this.buffer.PopUInt16();
                            if (this.packetSize > ushort.MaxValue)
                            {
                                throw new Exception($"recv packet size error: {this.packetSize}");
                            }
                            this.state = ParserState.PacketBody;
                        }
                        break;
                    case ParserState.PacketBody:
                        if (this.buffer.Length < this.packetSize)
                        {
                            finish = true;
                        }
                        else
                        {
                            this.isOK = true;
                            this.state = ParserState.PacketSize;
                            finish = true;
                        }
                        break;
                }
            }
            return this.isOK;
        }

        public int PacketSize()
        {
            if (isOK)
            {
                isOK = false;
                return this.packetSize;
            }
            return 0;
        }
    }
}