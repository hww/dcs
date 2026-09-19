using System;

namespace DCS.Lua
{
    public enum ReplMessageType : uint
    {
        Ping = 0,
        Eval = 10,
        Shutdown = 20,
        CheckComplete = 30, // Запрос от клиента: "Код завершен?"
        StatusComplete = 31, // Ответ от Unity: "Да, завершен, можно выполнять"
        StatusIncomplete = 32 // Ответ от Unity: "Нет, оборван на <eof>, жду еще строк"
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
