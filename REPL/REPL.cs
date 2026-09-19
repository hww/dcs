using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace DCS.Lua.Client
{
    public enum ReplMessageType : uint
    {
        Ping = 0,
        Eval = 10,
        Shutdown = 20,
        CheckComplete = 30,
        StatusComplete = 31,
        StatusIncomplete = 32
    }

    class Program
    {
        private const string HOST = "127.0.0.1";
        private const int PORT = 49155;
        private static bool _alive = true;
        private static bool _handshakeDone = false;
        private static readonly object _consoleLock = new object();

        // Сигналы ответа от Unity для основного потока ввода
        private static bool _waitingForStatus = false;
        private static bool _isServerCodeComplete = true;

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            using var client = new TcpClient();

            try
            {
                Console.WriteLine($"[Client] Подключение к nREPL серверу {HOST}:{PORT}...");
                client.Connect(HOST, PORT);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[Client] Ошибка подключения: {ex.Message}");
                Console.ResetColor();
                return;
            }

            var stream = client.GetStream();
            var rxThread = new Thread(() => ReceiveLoop(stream)) { IsBackground = true, Name = "ReplClient-Rx" };
            rxThread.Start();

            while (!_handshakeDone && _alive) Thread.Sleep(5);

            PrintHelpMessage();

            var multiLineAccumulator = new StringBuilder();

            try
            {
                while (_alive)
                {
                    // Рисуем промт: если буфер пуст — начало строки "lua> ", если пишем блок — "   ... "
                    lock (_consoleLock)
                    {
                        Console.ForegroundColor = multiLineAccumulator.Length == 0 ? ConsoleColor.Cyan : ConsoleColor.DarkGray;
                        Console.Write(multiLineAccumulator.Length == 0 ? "lua> " : "   ... ");
                        Console.ResetColor();
                    }

                    string currentLine = Console.ReadLine();
                    if (currentLine == null) break;

                    string trimmedLine = currentLine.Trim();
                    string lowerLine = trimmedLine.ToLower();

                    // Системные команды обрабатываем мгновенно (только на чистом буфере)
                    if (multiLineAccumulator.Length == 0 && (lowerLine == "exit" || lowerLine == "quit" || lowerLine == "clear" || lowerLine == "help"))
                    {
                        if (lowerLine == "exit" || lowerLine == "quit") { SendPacket(stream, ReplMessageType.Shutdown, ""); _alive = false; break; }
                        if (lowerLine == "clear") { Console.Clear(); continue; }
                        if (lowerLine == "help") { PrintHelpMessage(); continue; }
                    }

                    // Добавляем текущую строку в накопительный буфер OpenGOAL-style
                    if (multiLineAccumulator.Length > 0) multiLineAccumulator.Append("\n");
                    multiLineAccumulator.Append(currentLine);

                    string fullPendingCode = multiLineAccumulator.ToString();

                    // Запрашиваем у Unity проверку завершенности скобок/блоков кода Lua
                    _waitingForStatus = true;
                    SendPacket(stream, ReplMessageType.CheckComplete, fullPendingCode);

                    // Блокируем ввод, пока кооперативный ответ от Update() в Unity не вернется по сети
                    while (_waitingForStatus && _alive)
                    {
                        Thread.Sleep(2);
                    }

                    // Если Unity сказала, что код полностью завершен (баланс блоков равен нулю)
                    if (_isServerCodeComplete)
                    {
                        lock (_consoleLock)
                        {
                            SendPacket(stream, ReplMessageType.Eval, fullPendingCode);
                        }
                        multiLineAccumulator.Clear();

                        // Даем микропаузу, чтобы ReceiveLoop успел выплюнуть результат до отрисовки следующего "lua> "
                        Thread.Sleep(50);
                    }
                    // Если код не завершен (висят открытые function, if, таблицы) — просто уходим на следующий виток цикла,
                    // буфер multiLineAccumulator сохраняется, и терминал выведет "   ... "
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[Client] Ошибка: {ex.Message}");
                Console.ResetColor();
            }
            finally
            {
                _alive = false;
                stream.Close();
                client.Close();
            }
        }

        private static void PrintHelpMessage()
        {
            lock (_consoleLock)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n======================= REPL CONTROLS =======================");
                Console.ResetColor();
                Console.WriteLine("  help  - Справка | clear - Очистить экран | exit - Выход");
                Console.WriteLine("=============================================================");
                Console.WriteLine("  Поддерживается многострочный ввод OpenGOAL-style!");
                Console.WriteLine("  Нажмите Enter в конце блока (end), чтобы отправить его.\n");
            }
        }

        private static void SendPacket(NetworkStream stream, ReplMessageType type, string payload)
        {
            byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);
            byte[] packetBytes = new byte[8 + payloadBytes.Length];

            uint len = (uint)payloadBytes.Length;
            uint t = (uint)type;

            packetBytes[0] = (byte)(len & 0xFF);
            packetBytes[1] = (byte)((len >> 8) & 0xFF);
            packetBytes[2] = (byte)((len >> 16) & 0xFF);
            packetBytes[3] = (byte)((len >> 24) & 0xFF);

            packetBytes[4] = (byte)(t & 0xFF);
            packetBytes[5] = (byte)((t >> 8) & 0xFF);
            packetBytes[6] = (byte)((t >> 16) & 0xFF);
            packetBytes[7] = (byte)((t >> 24) & 0xFF);

            if (payloadBytes.Length > 0)
            {
                Buffer.BlockCopy(payloadBytes, 0, packetBytes, 8, payloadBytes.Length);
            }

            stream.Write(packetBytes, 0, packetBytes.Length);
            stream.Flush();
        }

        private static void ReceiveLoop(NetworkStream stream)
        {
            var headerBuffer = new byte[8];
            try
            {
                while (_alive)
                {
                    ReadExact(stream, headerBuffer, 8);

                    uint length = 0;
                    length |= headerBuffer[0];
                    length |= (uint)headerBuffer[1] << 8;
                    length |= (uint)headerBuffer[2] << 16;
                    length |= (uint)headerBuffer[3] << 24;

                    uint typeRaw = 0;
                    typeRaw |= headerBuffer[4];
                    typeRaw |= (uint)headerBuffer[5] << 8;
                    typeRaw |= (uint)headerBuffer[6] << 16;
                    typeRaw |= (uint)headerBuffer[7] << 24;

                    var type = (ReplMessageType)typeRaw;

                    string payload = string.Empty;
                    if (length > 0)
                    {
                        var bodyBuffer = new byte[length];
                        ReadExact(stream, bodyBuffer, (int)length);
                        payload = Encoding.UTF8.GetString(bodyBuffer);
                    }

                    // Обрабатываем статусы валидации строк от Unity
                    if (type == ReplMessageType.StatusComplete)
                    {
                        _isServerCodeComplete = true;
                        _waitingForStatus = false;
                        continue;
                    }
                    if (type == ReplMessageType.StatusIncomplete)
                    {
                        _isServerCodeComplete = false;
                        _waitingForStatus = false;
                        continue;
                    }

                    lock (_consoleLock)
                    {
                        if (type == ReplMessageType.Ping)
                        {
                            PrintAnsiNative(payload);
                            _handshakeDone = true;
                        }
                        else if (type == ReplMessageType.Eval)
                        {
                            PrintAnsiNative(payload);
                        }
                    }
                }
            }
            catch
            {
                _alive = false;
            }
        }

        private static void PrintAnsiNative(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            int i = 0;
            while (i < text.Length)
            {
                if (text[i] == '\u001b' && i + 1 < text.Length && text[i + 1] == '[')
                {
                    int start = i + 2;
                    int end = start;
                    while (end < text.Length && !char.IsLetter(text[end])) end++;
                    if (end < text.Length)
                    {
                        string code = text.Substring(start, end - start);
                        if (code == "0") Console.ResetColor();
                        else if (code == "91") Console.ForegroundColor = ConsoleColor.Red; // Светло-красный
                        else if (code == "93") Console.ForegroundColor = ConsoleColor.Yellow; // Золотой return
                        else if (code == "32") Console.ForegroundColor = ConsoleColor.Green; else if (code == "90") Console.ForegroundColor = ConsoleColor.DarkGray; else if (code.Contains("38;5;208")) Console.ForegroundColor = ConsoleColor.DarkYellow; i = end + 1; continue;
                    }
                }
                Console.Write(text[i]); i++;
            }
            Console.ResetColor();
        }
        private static void ReadExact(NetworkStream stream, byte[] buffer, int bytesToRead) { int totalRead = 0; while (totalRead < bytesToRead) { int read = stream.Read(buffer, totalRead, bytesToRead - totalRead); if (read <= 0) throw new EndOfStreamException(); totalRead += read; } }
    }
}