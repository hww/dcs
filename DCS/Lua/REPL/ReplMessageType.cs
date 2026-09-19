using System;

namespace DCS.Lua
{
    public enum ReplMessageType : uint
    {
        Ping = 0,
        Eval = 10,
        Shutdown = 20
    }

    /// <summary>
    /// Контейнер данных, передаваемый из сетевого потока в главный поток Unity.
    /// </summary>
    public struct ReplPacket
    {
        public int ConsoleId;
        public ReplMessageType Type;
        public string Payload;
    }
}
