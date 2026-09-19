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
        Shutdown = 20
    }

    class Program
    {
        private const string HOST = "127.0.0.1";
        private const int PORT = 49155;
        private static bool _alive = true;
        private static bool _handshakeDone = false;
        private static readonly object _consoleLock = new object();

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
                Console.WriteLine("[Client] Убедитесь, что Unity запущена и находится в Play Mode.");
                return;
            }

            var stream = client.GetStream();

            // Фоновый поток вычитки ответов из Unity
            var rxThread = new Thread(() => ReceiveLoop(stream)) { IsBackground = true, Name = "ReplClient-Rx" };
            rxThread.Start();

            // Кооперативно ждем Welcome-пакет от сервера
            while (!_handshakeDone && _alive) Thread.Sleep(5);

            PrintHelpMessage();

            try
            {
                while (_alive)
                {
                    // Выводим бирюзовый промт перед вводом
                    lock (_consoleLock)
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write("lua> ");
                        Console.ResetColor();
                    }

                    // Используем стандартный ReadLine — он неубиваем, поддерживает историю и буфер
                    string input = Console.ReadLine()?.Trim();
                    if (string.IsNullOrEmpty(input)) continue;

                    string lowerInput = input.ToLower();

                    // ЖЕЛЕЗНЫЙ ПАРСИНГ КОМАНД КОНСОЛИ
                    if (lowerInput == "exit" || lowerInput == "quit" || lowerInput == ":exit" || lowerInput == ":quit")
                    {
                        SendPacket(stream, ReplMessageType.Shutdown, "");
                        _alive = false;
                        break;
                    }
                    else if (lowerInput == "clear" || lowerInput == ":clear")
                    {
                        Console.Clear();
                        continue;
                    }
                    else if (lowerInput == "help" || lowerInput == ":help")
                    {
                        PrintHelpMessage();
                        continue;
                    }
                    else if (lowerInput == "ping" || lowerInput == ":ping")
                    {
                        SendPacket(stream, ReplMessageType.Ping, "");
                        Thread.Sleep(50); // Пауза для красивого вывода ответа
                        continue;
                    }

                    // Если это не команда, гоним как код в Lua
                    lock (_consoleLock)
                    {
                        SendPacket(stream, ReplMessageType.Eval, input);
                    }

                    // Небольшая пауза, чтобы Rx поток успел напечатать ответ до следующего цикла lua>
                    Thread.Sleep(60);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[Client] Ошибка отправки: {ex.Message}");
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

                Console.ForegroundColor = ConsoleColor.Cyan; Console.Write("  help  "); Console.ResetColor();
                Console.WriteLine("        - Показать это окно справки");

                Console.ForegroundColor = ConsoleColor.Cyan; Console.Write("  clear "); Console.ResetColor();
                Console.WriteLine("        - Очистить экран консоли");

                Console.ForegroundColor = ConsoleColor.Cyan; Console.Write("  ping  "); Console.ResetColor();
                Console.WriteLine("        - Проверить пинг до Lua-машины");

                Console.ForegroundColor = ConsoleColor.Cyan; Console.Write("  exit / quit"); Console.ResetColor();
                Console.WriteLine("  - Закрыть сессию и выйти");

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("=============================================================");
                Console.ResetColor();
                Console.WriteLine("  Вводите любой Lua-код (например: 1+1 или DCS)\n");
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

                    lock (_consoleLock)
                    {
                        // Парсим и выводим ответ с честной обработкой цветов
                        PrintAnsiNative(payload);
                        _handshakeDone = true;
                    }
                }
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n[Client] Соединение с сервером Unity разорвано.");
                Console.ResetColor();
                _alive = false;
            }
        }

        /// <summary>
        /// Честный посимвольный парсер ANSI-кодов для любой версии консоли Windows
        /// </summary>
        private static void PrintAnsiNative(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            int i = 0;
            while (i < text.Length)
            {
                // Ищем начало ANSI-последовательности Esc [
                if (text[i] == '\u001b' && i + 1 < text.Length && text[i + 1] == '[')
                {
                    int start = i + 2;
                    int end = start;
                    while (end < text.Length && !char.IsLetter(text[end]))
                    {
                        end++;
                    }

                    if (end < text.Length)
                    {
                        string code = text.Substring(start, end - start);
                        ApplyAnsiColor(code);
                        i = end + 1; // Пропускаем весь код
                        continue;
                    }
                }

                Console.Write(text[i]);
                i++;
            }
            Console.ResetColor(); // Сбрасываем в конце пакета
        }

        private static void ApplyAnsiColor(string code)
        {
            if (code == "0") Console.ResetColor();
            else if (code == "1") Console.ForegroundColor = ConsoleColor.White; // Bold
            else if (code == "91") Console.ForegroundColor = ConsoleColor.Red;   // Errors
            else if (code == "32") Console.ForegroundColor = ConsoleColor.Green; // Live status
            else if (code == "93") Console.ForegroundColor = ConsoleColor.Yellow;// Return values
            else if (code == "96") Console.ForegroundColor = ConsoleColor.Cyan;  // Prompts
            else if (code == "90") Console.ForegroundColor = ConsoleColor.DarkGray; // System specs
            else if (code.Contains("38;5;208")) Console.ForegroundColor = ConsoleColor.DarkYellow; // Логотип (Оранжевый)
        }
        private static void ReadExact(NetworkStream stream, byte[] buffer, int bytesToRead) { int totalRead = 0; while (totalRead < bytesToRead) { int read = stream.Read(buffer, totalRead, bytesToRead - totalRead); if (read <= 0) throw new EndOfStreamException(); totalRead += read; } }
    }
}