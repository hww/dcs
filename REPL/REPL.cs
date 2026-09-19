using System;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
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

        private static bool _waitingForStatus = false;
        private static bool _isServerCodeComplete = true;

        // --- Win32 API для включения поддержки ANSI-последовательностей в Windows ---
        private const int STD_OUTPUT_HANDLE = -11;
        private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            // Включаем нативную поддержку ANSI escape-кодов
            TryEnableAnsiSupport();

            using var client = new TcpClient();

            try
            {
                Console.WriteLine($"\u001b[90m:: Connecting to nREPL server at {HOST}:{PORT}...\u001b[0m");
                client.Connect(HOST, PORT);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\u001b[91m[Client] Connection error: {ex.Message}\u001b[0m");
                return;
            }

            var stream = client.GetStream();
            var rxThread = new Thread(() => ReceiveLoop(stream)) { IsBackground = true, Name = "ReplClient-Rx" };
            rxThread.Start();

            while (!_handshakeDone && _alive) Thread.Sleep(5);

            // Показываем стартовый баннер при подключении
            PrintWelcomeMessage();

            var multiLineAccumulator = new StringBuilder();

            try
            {
                while (_alive)
                {
                    lock (_consoleLock)
                    {
                        // Промт dcs> в бирюзовом цвете, блоки кода ... в сером
                        if (multiLineAccumulator.Length == 0)
                            Console.Write("\u001b[1;\u001b[36mdcs> \u001b[0m");
                        else
                            Console.Write("\u001b[90m   ... \u001b[0m");
                    }

                    string currentLine = Console.ReadLine();
                    if (currentLine == null) break;

                    string trimmedLine = currentLine.Trim();
                    string lowerLine = trimmedLine.ToLower();

                    // Системные команды REPL оболочки
                    if (multiLineAccumulator.Length == 0 && (lowerLine == "exit" || lowerLine == "quit" || lowerLine == "clear" || lowerLine == "help"))
                    {
                        if (lowerLine == "exit" || lowerLine == "quit") { SendPacket(stream, ReplMessageType.Shutdown, ""); _alive = false; break; }
                        if (lowerLine == "clear") { Console.Clear(); continue; }
                        if (lowerLine == "help") { PrintHelpCommands(); continue; }
                    }

                    if (multiLineAccumulator.Length > 0) multiLineAccumulator.Append("\n");
                    multiLineAccumulator.Append(currentLine);

                    string fullPendingCode = multiLineAccumulator.ToString();

                    _waitingForStatus = true;
                    SendPacket(stream, ReplMessageType.CheckComplete, fullPendingCode);

                    while (_waitingForStatus && _alive)
                    {
                        Thread.Sleep(2);
                    }

                    if (_isServerCodeComplete)
                    {
                        lock (_consoleLock)
                        {
                            SendPacket(stream, ReplMessageType.Eval, fullPendingCode);
                        }
                        multiLineAccumulator.Clear();
                        Thread.Sleep(50);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n\u001b[91m[Client] Ошибка: {ex.Message}\u001b[0m");
            }
            finally
            {
                _alive = false;
                stream.Close();
                client.Close();
            }
        }

        private const string CORE_VERSION = "v1.0 DCS";
        private const string BUILD_SHA = "9854123";

        // Метод отрисовки стартового экрана (Welcome) в стиле SOOT
        private static void PrintWelcomeMessage()
        {
            lock (_consoleLock)
            {
                var welcome = new StringBuilder();
                welcome.AppendLine("\u001b[90m--------------------------------------------------\u001b[0m");
                welcome.AppendLine("                   \u001b[1mD C S\u001b[0m");
                welcome.AppendLine("          \u001b[90mDynamic Component System\u001b[0m");
                welcome.AppendLine("\u001b[90m--------------------------------------------------\u001b[0m");

                welcome.AppendLine($"\u001b[90mcore:\u001b[0m     \u001b[1m{CORE_VERSION}\u001b[0m");
                welcome.AppendLine($"\u001b[90mbuild:\u001b[0m    \u001b[36msha:{BUILD_SHA}\u001b[0m \u001b[90mtag:\u001b[0m");
                welcome.AppendLine($"\u001b[90mtype:\u001b[0m     \u001b[33mInteractive Shell (REPL)\u001b[0m");
                welcome.AppendLine("\u001b[90m--------------------------------------------------\u001b[0m");

                // Исправлено: убрали упоминание несуществующей команды (keybinds)
                welcome.AppendLine("Type \u001b[36mhelp\u001b[0m for list of available shell commands\n");

                Console.Write(welcome.ToString());
            }
        }

        // Реальный вывод справки по системным командам на команду 'help'
        private static void PrintHelpCommands()
        {
            lock (_consoleLock)
            {
                var help = new StringBuilder();
                help.AppendLine("\n\u001b[1mДоступные команды REPL оболочки:\u001b[0m");
                help.AppendLine("  \u001b[36mhelp\u001b[0m  - Показать это справочное сообщение");
                help.AppendLine("  \u001b[36mclear\u001b[0m - Очистить экран терминала");
                help.AppendLine("  \u001b[36mexit\u001b[0m  - Завершить сессию REPL и закрыть сервер");
                help.AppendLine("  \u001b[36mquit\u001b[0m  - То же, что и exit\n");

                help.AppendLine("\u001b[90mЛюбые другие выражения будут отправлены на сервер как код Lua.\u001b[0m\n");
                Console.Write(help.ToString());
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
                    lock (_consoleLock) { if (type == ReplMessageType.Ping || type == ReplMessageType.Eval) { Console.Write(payload); if (type == ReplMessageType.Ping) _handshakeDone = true; } }
                }
            }
            catch { _alive = false; }
        }
        private static void ReadExact(NetworkStream stream, byte[] buffer, int bytesToRead) { int totalRead = 0; while (totalRead < bytesToRead) { int read = stream.Read(buffer, totalRead, bytesToRead - totalRead); if (read <= 0) throw new EndOfStreamException(); totalRead += read; } }
        private static void TryEnableAnsiSupport() { if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { var iStdOut = GetStdHandle(STD_OUTPUT_HANDLE); if (GetConsoleMode(iStdOut, out uint lpMode)) { lpMode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING; SetConsoleMode(iStdOut, lpMode); } } }
    }
}