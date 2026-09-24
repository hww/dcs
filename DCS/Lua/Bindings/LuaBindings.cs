using System;
using System.Runtime.InteropServices;

namespace DCS.Lua.Bindings
{
    /// <summary>
    /// Single entry point for registering the entire DCS Lua binding set.
    /// Called once during Lua state initialization (see <c>LuaManager.InitializeGlobalEngine</c>).
    /// </summary>
    /// <remarks>
    /// Each child binding (<see cref="DcsBindings"/>, <see cref="EventBindings"/>, etc.)
    /// creates its own global table in Lua (<c>DCS</c>, <c>World</c>, <c>Spatial</c>, …)
    /// and registers methods inside it. The order of <see cref="RegisterAll"/> matters
    /// only in one place: <see cref="EventBindings"/> and <see cref="DcsBindings"/> append
    /// methods to an already-existing <c>DCS</c> table, so <see cref="DcsBindings"/> must
    /// be called first.
    /// </remarks>
    public static class LuaBindings
    {
        // ------------------------------------------------------------
        //  Full binding set registration
        // ------------------------------------------------------------

        /// <summary>
        /// Registers the full DCS binding set into the given Lua state.
        /// </summary>
        /// <param name="L">Pointer to a valid, open <c>lua_State</c>.</param>
        /// <remarks>
        /// After the call, the following globals become available in Lua:
        /// <list type="bullet">
        ///   <item><description><c>DCS</c> — components, events, host operations (see <see cref="DcsBindings"/>).</description></item>
        ///   <item><description><c>Domain</c> — mapping of domain names to their Ids (created in <see cref="DcsBindings.RegisterDomains"/>).</description></item>
        ///   <item><description><c>World</c> — actor access, field/fact read and write (see <see cref="WorldBindings"/>).</description></item>
        ///   <item><description><c>Spatial</c> — spatial queries: point, radius, raycast (see <see cref="SpatialBindings"/>).</description></item>
        ///   <item><description><c>Map</c> — map streaming and load status (see <see cref="MapBindings"/>).</description></item>
        ///   <item><description><c>AI</c> — combat roles (see <see cref="AIBindings"/>).</description></item>
        ///   <item><description><c>Input</c>, <c>KeyCode</c> — wrappers over <c>UnityEngine.Input</c> (see <see cref="InputBindings"/>).</description></item>
        ///   <item><description><c>facts_get</c>, <c>facts_set</c>, <c>facts_try_get</c> — global functions for <c>DynamicFacts</c> (see <see cref="DynamicFactsBinding"/>).</description></item>
        /// </list>
        /// </remarks>
        public static void RegisterAll(IntPtr L)
        {
            DcsBindings.Register(L);
            DynamicFactsBinding.Register(L);
            EventBindings.Register(L);
            SpatialBindings.Register(L);
            WorldBindings.Register(L);
            MapBindings.Register(L);
            AIBindings.Register(L);
            InputBindings.Register(L);
        }

        // ------------------------------------------------------------
        //  Registration helpers
        // ------------------------------------------------------------

        /// <summary>
        /// Registers a C# function as a global Lua function under the given name.
        /// </summary>
        /// <param name="L">Pointer to the <c>lua_State</c>.</param>
        /// <param name="fn">Delegate marked with <c>[AOT.MonoPInvokeCallback]</c>.
        /// Must return the number of values pushed onto the stack.</param>
        /// <param name="name">Global function name in Lua (e.g. <c>"facts_get"</c>).</param>
        /// <remarks>
        /// Internally: <c>Marshal.GetFunctionPointerForDelegate</c> converts the delegate
        /// into a native pointer, <c>lua_pushcclosure(L, ptr, 0)</c> pushes the C function
        /// onto the stack, and <c>lua_setglobal(L, name)</c> stores it as a global.
        /// </remarks>
        /// <example>
        /// <code>
        /// LuaBindings.RegisterGlobalFunction(L, Lua_GetFact, "facts_get");
        /// -- in Lua: facts_get(facts, "speed")
        /// </code>
        /// </example>
        public static void RegisterGlobalFunction(IntPtr L, Func<IntPtr, int> fn, string name)
        {
            IntPtr ptr = Marshal.GetFunctionPointerForDelegate(fn);
            LuaNative.lua_pushcclosure(L, ptr, 0);
            LuaNative.lua_setglobal(L, name);
        }

        /// <summary>
        /// Registers a C# function as a method of the table currently on top of the stack.
        /// </summary>
        /// <param name="L">Pointer to the <c>lua_State</c>. The table must be on top of the
        /// stack (typically created via <c>lua_newtable</c>).</param>
        /// <param name="fn">Delegate marked with <c>[AOT.MonoPInvokeCallback]</c>.</param>
        /// <param name="name">Method name inside the table (e.g. <c>"CreateComponent"</c>).</param>
        /// <remarks>
        /// Internally: <c>lua_pushstring(L, name)</c> pushes the key,
        /// <c>lua_pushcclosure</c> pushes the value, <c>lua_settable(L, -3)</c> performs
        /// <c>table[name] = value</c> and pops both. The table itself remains on the stack,
        /// so the stack is unchanged relative to entry.
        /// </remarks>
        /// <example>
        /// <code>
        /// LuaNative.lua_newtable(L);
        /// LuaBindings.RegisterMethod(L, Lua_CreateComponent, "CreateComponent");
        /// LuaNative.lua_setglobal(L, "DCS");
        /// </code>
        /// </example>
        public static void RegisterMethod(IntPtr L, Func<IntPtr, int> fn, string name)
        {
            IntPtr ptr = Marshal.GetFunctionPointerForDelegate(fn);
            LuaNative.lua_pushstring(L, name);
            LuaNative.lua_pushcclosure(L, ptr, 0);
            LuaNative.lua_settable(L, -3);
        }
    }
}