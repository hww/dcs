using DCS.Core;
using DCS.Lua;
using System;
using UnityEngine;

public class CameraActor : BaseActor
{
    [Header("Entity Actor - Physics")]
    public Animator Animator;

    [Header("Entity Actor - Facts")]
    public DynamicFacts Facts;
    public EFactsLifetime FactsLifetime;

    // ============================================================
    //  IFieldAccess Реализация для быстрого шлюза в Lua
    // ============================================================

    public override bool GetField(string fieldName, IntPtr L)
    {
        // Используем быстрый Ordinal хэш-маппинг или switch строк
        switch (fieldName)
        {
            case "position":
                Vector3 pos = transform.position;
                // Пушим координаты напрямую в Lua стек через ваш API
                LuaNative.lua_pushnumber(L, pos.x);
                LuaNative.lua_pushnumber(L, pos.y);
                LuaNative.lua_pushnumber(L, pos.z);
                break;


            case "hasAnimator":
                // Быстрая проверка без GetComponent!
                LuaNative.lua_pushboolean(L, (Animator != null) ? 1 : 0);
                break;
            default:
                return base.GetField(fieldName, L);
        }
        return false;
    }

    public override bool SetField(string fieldName, IntPtr L)
    {
        switch (fieldName)
        {
            case "position":
                // Читаем из стека Lua напрямую в структуру Vector3
                float z = (float)LuaNative.lua_tonumberx(L, -1, IntPtr.Zero);
                float y = (float)LuaNative.lua_tonumberx(L, -2, IntPtr.Zero);
                float x = (float)LuaNative.lua_tonumberx(L, -3, IntPtr.Zero);
                transform.position = new Vector3(x, y, z);
                break;

            case "animatorState":
                if (Animator != null)
                {
                    int stateNameHash = (int)LuaNative.lua_tointegerx(L, -1, IntPtr.Zero);
                    Animator.Play(stateNameHash, -1, float.NegativeInfinity);
                }
                break;
            default:
                return base.SetField(fieldName, L);
        }
        return false;
    }
}
