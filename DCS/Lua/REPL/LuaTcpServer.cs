using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace DCS.Lua
{
    public class LuaTcpServer
    {
        private readonly int _port;
        private readonly IntPtr _luaState;

        private TcpListener _listener;
        private bool _running;
        private int _nextSessionId = 1;

        private readonly ConcurrentQueue<TcpClient> _pendingClients = new();
        private readonly List<ReplConsoleSession> _sessions = new();
        private LuaRepl _replEvaluator;

        // Обычный C# конструктор. Передаем порт и стейт Lua напрямую
        public LuaTcpServer(IntPtr luaState, int port = 49155)
        {
            _luaState = luaState;
            _port = port;
        }

        public void StartServer()
        {
            if (_running) return;
            _running = true;

            try
            {
                _replEvaluator = new LuaRepl(_luaState);
                _listener = new TcpListener(IPAddress.Loopback, _port);
                _listener.Start();

                Debug.Log($"[LuaREPL] Чистый nREPL сервер запущен на 127.0.0.1:{_port}");
            }
            catch (Exception e)
            {
                _running = false;
                Debug.LogError($"[LuaREPL] Не удалось запустить сервер: {e.Message}");
            }
        }

        public void StopServer()
        {
            if (!_running) return;
            _running = false;

            try { _listener?.Stop(); } catch { }

            foreach (var session in _sessions) session.Dispose();
            _sessions.Clear();

            while (_pendingClients.TryDequeue(out var client))
            {
                try { client.Close(); } catch { }
            }

            _listener = null;
            _replEvaluator = null;
        }

        // Вместо Update() — этот метод будет вызывать LuaManager каждый кадр
        public void Tick()
        {
            if (!_running) return;

            // 1. Кооперативно принимаем новые сокеты через Pending() без всяких фоновых потоков
            while (_listener != null && _listener.Pending())
            {
                try
                {
                    var client = _listener.AcceptTcpClient();
                    var session = new ReplConsoleSession(_nextSessionId++, client);
                    session.Start();
                    _sessions.Add(session);

                    session.SendResponse(ReplMessageType.Ping, "Welcome to DCS Lua nREPL Engine!\n");
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            // 2. Обрабатываем пакеты активных сессий (в точности как RosEngine)
            for (int i = _sessions.Count - 1; i >= 0; i--)
            {
                var session = _sessions[i];

                if (!session.IsAlive)
                {
                    session.Dispose();
                    _sessions.RemoveAt(i);
                    Debug.Log($"[LuaREPL] Сессия консоли #{session.Id} закрыта.");
                    continue;
                }

                while (session.TryReadPacket(out var packet))
                {
                    switch (packet.Type)
                    {
                        case ReplMessageType.Ping:
                            session.SendResponse(ReplMessageType.Ping, "pong\n");
                            break;

                        case ReplMessageType.Eval:
                            string result = _replEvaluator.Eval(packet.Payload);
                            session.SendResponse(ReplMessageType.Eval, result);
                            break;

                        case ReplMessageType.Shutdown:
                            session.Dispose();
                            break;
                    }
                }
            }
        }
    }
}
