namespace DCS.Lua
{
    /// <summary>
    /// Статический мост к активному LuaManager.
    /// Используется структурами (SubscriptionNode), которые не имеют ссылки на manager.
    /// </summary>
    public static class LuaBridge
    {
        public static LuaManager Current;
    }
}