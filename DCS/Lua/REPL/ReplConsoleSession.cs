using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace DCS.Lua
{
    public class ReplConsoleSession : IDisposable
    {
        public int Id { get; }
        private readonly TcpClient _client;
        private readonly NetworkStream _stream;

        private readonly ConcurrentQueue<ReplPacket> _incomingPackets = new();
        private readonly ConcurrentQueue<byte[]> _outgoingRawData = new();

        private Thread _readThread;
        private Thread _writeThread;
        private volatile bool _alive = true;

        public bool IsAlive => _alive && _client.Connected;

        public ReplConsoleSession(int id, TcpClient client)
        {
            Id = id;
            _client = client;
            _stream = client.GetStream();
        }

        public void Start()
        {
            _readThread = new Thread(ReadLoop) { IsBackground = true, Name = $"Repl-Read-{Id}" };
            _writeThread = new Thread(WriteLoop) { IsBackground = true, Name = $"Repl-Write-{Id}" };
            _readThread.Start();
            _writeThread.Start();
        }

        public bool HasPacket() => !_incomingPackets.IsEmpty;

        public bool TryReadPacket(out ReplPacket packet)
        {
            return _incomingPackets.TryDequeue(out packet);
        }

        /// <summary>
        /// Отправить бинарный структурированный ответ клиенту (Упаковка: Длина + Тип + Тело)
        /// </summary>
        public void SendResponse(ReplMessageType type, string text)
        {
            if (!_alive) return;

            byte[] textBytes = Encoding.UTF8.GetBytes(text);
            byte[] packetBytes = new byte[8 + textBytes.Length];

            // Записываем 4 байта длины payload и 4 байта типа сообщения
            Buffer.BlockCopy(BitConverter.GetBytes((uint)textBytes.Length), 0, packetBytes, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes((uint)type), 0, packetBytes, 4, 4);

            if (textBytes.Length > 0)
            {
                Buffer.BlockCopy(textBytes, 0, packetBytes, 8, textBytes.Length);
            }

            _outgoingRawData.Enqueue(packetBytes);
        }

        private void ReadLoop()
        {
            var headerBuffer = new byte[8];

            try
            {
                while (_alive)
                {
                    // 1. Вычитываем строго заголовок (8 байт)
                    ReadExact(_stream, headerBuffer, 8);

                    uint length = BitConverter.ToUInt32(headerBuffer, 0);
                    uint typeRaw = BitConverter.ToUInt32(headerBuffer, 4);
                    var type = (ReplMessageType)typeRaw;

                    // 2. Вычитываем тело (если оно есть)
                    string payload = string.Empty;
                    if (length > 0)
                    {
                        var bodyBuffer = new byte[length];
                        ReadExact(_stream, bodyBuffer, (int)length);
                        payload = Encoding.UTF8.GetString(bodyBuffer);
                    }

                    // 3. Пакет успешно собран, перекидываем в главный поток
                    _incomingPackets.Enqueue(new ReplPacket
                    {
                        ConsoleId = Id,
                        Type = type,
                        Payload = payload
                    });
                }
            }
            catch
            {
                // Обрыв связи или закрытие стрима
            }
            finally
            {
                _alive = false;
            }
        }

        private void ReadExact(NetworkStream stream, byte[] buffer, int bytesToRead)
        {
            int totalRead = 0;
            while (totalRead < bytesToRead)
            {
                int read = stream.Read(buffer, totalRead, bytesToRead - totalRead);
                if (read <= 0) throw new EndOfStreamException("Соединение закрыто удаленным клиентом.");
                totalRead += read;
            }
        }

        private void WriteLoop()
        {
            try
            {
                while (_alive)
                {
                    if (_outgoingRawData.TryDequeue(out var data))
                    {
                        _stream.Write(data, 0, data.Length);
                        _stream.Flush();
                    }
                    else
                    {
                        Thread.Sleep(5);
                    }
                }
            }
            catch { }
            finally { _alive = false; }
        }

        public void Dispose()
        {
            _alive = false;
            try { _stream?.Close(); } catch { }
            try { _client?.Close(); } catch { }
        }
    }
}
