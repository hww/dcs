using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using DCS.Lua;
using UnityEngine;

namespace DCS.Core
{
    public delegate bool RefFieldGetter<T>(ref T instance, string fieldName, IntPtr L) where T : struct;
    public delegate bool RefFieldSetter<T>(ref T instance, string fieldName, IntPtr L) where T : struct;

    public static class FieldExpressionFactory
    {
        public static RefFieldGetter<T> CreateGetter<T>() where T : struct
        {
            var type = typeof(T);
            var instanceParam = Expression.Parameter(type.MakeByRefType(), "instance");
            var nameParam = Expression.Parameter(typeof(string), "fieldName");
            var luaParam = Expression.Parameter(typeof(IntPtr), "L");

            var switchCases = new List<SwitchCase>();
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            var pushNilMethod = typeof(LuaNative).GetMethod("lua_pushnil", new[] { typeof(IntPtr) });

            foreach (var field in fields)
            {
                Expression fieldAccess = Expression.Field(instanceParam, field);

                // --- 1. PRIMITIVE TYPES ---
                if (field.FieldType == typeof(float) || field.FieldType == typeof(double))
                {
                    var pushMethod = typeof(LuaNative).GetMethod("lua_pushnumber", new[] { typeof(IntPtr), typeof(double) });
                    var block = Expression.Block(
                        Expression.Call(pushMethod, luaParam, Expression.Convert(fieldAccess, typeof(double))),
                        Expression.Constant(true)
                    );
                    switchCases.Add(Expression.SwitchCase(block, Expression.Constant(field.Name.ToLower())));
                }
                else if (field.FieldType == typeof(int) || field.FieldType == typeof(long) || field.FieldType == typeof(uint) || field.FieldType == typeof(ushort))
                {
                    var pushMethod = typeof(LuaNative).GetMethod("lua_pushinteger", new[] { typeof(IntPtr), typeof(long) });
                    var block = Expression.Block(
                        Expression.Call(pushMethod, luaParam, Expression.Convert(fieldAccess, typeof(long))),
                        Expression.Constant(true)
                    );
                    switchCases.Add(Expression.SwitchCase(block, Expression.Constant(field.Name.ToLower())));
                }
                else if (field.FieldType == typeof(bool))
                {
                    var pushMethod = typeof(LuaNative).GetMethod("lua_pushboolean", new[] { typeof(IntPtr), typeof(bool) });
                    var block = Expression.Block(
                        Expression.Call(pushMethod, luaParam, fieldAccess),
                        Expression.Constant(true)
                    );
                    switchCases.Add(Expression.SwitchCase(block, Expression.Constant(field.Name.ToLower())));
                }
                else if (field.FieldType == typeof(string))
                {
                    var pushMethod = typeof(LuaNative).GetMethod("lua_pushstring", new[] { typeof(IntPtr), typeof(string) });
                    var block = Expression.Block(
                        Expression.Call(pushMethod, luaParam, fieldAccess),
                        Expression.Constant(true)
                    );
                    switchCases.Add(Expression.SwitchCase(block, Expression.Constant(field.Name.ToLower())));
                }
                // --- 2. COMPLEX MATHEMATICAL TYPES (Vector3) ---
                else if (field.FieldType == typeof(Vector3))
                {
                    var pushNumberMethod = typeof(LuaNative).GetMethod("lua_pushnumber", new[] { typeof(IntPtr), typeof(double) });

                    var block = Expression.Block(
                        Expression.Call(pushNumberMethod, luaParam, Expression.Convert(Expression.Field(fieldAccess, "x"), typeof(double))),
                        Expression.Call(pushNumberMethod, luaParam, Expression.Convert(Expression.Field(fieldAccess, "y"), typeof(double))),
                        Expression.Call(pushNumberMethod, luaParam, Expression.Convert(Expression.Field(fieldAccess, "z"), typeof(double))),
                        Expression.Constant(true)
                    );
                    switchCases.Add(Expression.SwitchCase(block, Expression.Constant(field.Name.ToLower())));
                }
                // --- 3. COMPLEX MATHEMATICAL TYPES (Color) ---
                else if (field.FieldType == typeof(Color))
                {
                    var pushNumberMethod = typeof(LuaNative).GetMethod("lua_pushnumber", new[] { typeof(IntPtr), typeof(double) });

                    var block = Expression.Block(
                        Expression.Call(pushNumberMethod, luaParam, Expression.Convert(Expression.Field(fieldAccess, "r"), typeof(double))),
                        Expression.Call(pushNumberMethod, luaParam, Expression.Convert(Expression.Field(fieldAccess, "g"), typeof(double))),
                        Expression.Call(pushNumberMethod, luaParam, Expression.Convert(Expression.Field(fieldAccess, "b"), typeof(double))),
                        Expression.Call(pushNumberMethod, luaParam, Expression.Convert(Expression.Field(fieldAccess, "a"), typeof(double))),
                        Expression.Constant(true)
                    );
                    switchCases.Add(Expression.SwitchCase(block, Expression.Constant(field.Name.ToLower())));
                }
            }

            if (switchCases.Count == 0)
            {
                return (ref T inst, string name, IntPtr L) => { LuaNative.lua_pushnil(L); return false; };
            }

            var defaultResult = Expression.Block(
                Expression.Call(pushNilMethod, luaParam),
                Expression.Constant(false)
            );

            var switchExpr = Expression.Switch(
                Expression.Call(nameParam, typeof(string).GetMethod("ToLower", Type.EmptyTypes)),
                defaultResult,
                null,
                switchCases.ToArray()
            );

            var lambda = Expression.Lambda<RefFieldGetter<T>>(switchExpr, instanceParam, nameParam, luaParam);
            return lambda.Compile();
        }

        // FIXED SIGNATURE HERE: Added <T> to RefFieldSetter template parameters
        public static RefFieldSetter<T> CreateSetter<T>() where T : struct
        {
            var type = typeof(T);
            var instanceParam = Expression.Parameter(type.MakeByRefType(), "instance");
            var nameParam = Expression.Parameter(typeof(string), "fieldName");
            var luaParam = Expression.Parameter(typeof(IntPtr), "L");

            var switchCases = new List<SwitchCase>();
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (var field in fields)
            {
                Expression assignment = null;
                Expression fieldAccess = Expression.Field(instanceParam, field);

                if (field.FieldType == typeof(float) || field.FieldType == typeof(double))
                {
                    var toNumberMethod = typeof(LuaNative).GetMethod("lua_tonumberx", new[] { typeof(IntPtr), typeof(int), typeof(IntPtr) });
                    var luaValue = Expression.Call(toNumberMethod, luaParam, Expression.Constant(-1), Expression.Constant(IntPtr.Zero));
                    assignment = Expression.Assign(fieldAccess, Expression.Convert(luaValue, field.FieldType));
                }
                else if (field.FieldType == typeof(int) || field.FieldType == typeof(long) || field.FieldType == typeof(uint) || field.FieldType == typeof(ushort))
                {
                    var toIntegerMethod = typeof(LuaNative).GetMethod("lua_tointegerx", new[] { typeof(IntPtr), typeof(int), typeof(IntPtr) });
                    var luaValue = Expression.Call(toIntegerMethod, luaParam, Expression.Constant(-1), Expression.Constant(IntPtr.Zero));
                    assignment = Expression.Assign(fieldAccess, Expression.Convert(luaValue, field.FieldType));
                }
                else if (field.FieldType == typeof(bool))
                {
                    var toBoolMethod = typeof(LuaNative).GetMethod("lua_toboolean", new[] { typeof(IntPtr), typeof(int) });
                    var luaValue = Expression.NotEqual(Expression.Call(toBoolMethod, luaParam, Expression.Constant(-1)), Expression.Constant(0));
                    assignment = Expression.Assign(fieldAccess, luaValue);
                }
                else if (field.FieldType == typeof(string))
                {
                    var tolStringMethod = typeof(LuaNative).GetMethod("lua_tolstring", new[] { typeof(IntPtr), typeof(int), typeof(IntPtr) });
                    var luaValuePtr = Expression.Call(tolStringMethod, luaParam, Expression.Constant(-1), Expression.Constant(IntPtr.Zero));
                    var marshalMethod = typeof(System.Runtime.InteropServices.Marshal).GetMethod("PtrToStringUTF8", new[] { typeof(IntPtr) });
                    var stringValue = Expression.Call(marshalMethod, luaValuePtr);
                    assignment = Expression.Assign(fieldAccess, stringValue);
                }
                // --- SETTER FOR VECTOR3 (Expects 3 numbers sequentially rotated/pulled from top of the stack) ---
                else if (field.FieldType == typeof(Vector3))
                {
                    var toNumberMethod = typeof(LuaNative).GetMethod("lua_tonumberx", new[] { typeof(IntPtr), typeof(int), typeof(IntPtr) });

                    var zVal = Expression.Convert(Expression.Call(toNumberMethod, luaParam, Expression.Constant(-1), Expression.Constant(IntPtr.Zero)), typeof(float)); var yVal = Expression.Convert(Expression.Call(toNumberMethod, luaParam, Expression.Constant(-2), Expression.Constant(IntPtr.Zero)), typeof(float)); var xVal = Expression.Convert(Expression.Call(toNumberMethod, luaParam, Expression.Constant(-3), Expression.Constant(IntPtr.Zero)), typeof(float)); var newVector = Expression.New(typeof(Vector3).GetConstructor(new[] { typeof(float), typeof(float), typeof(float) }), xVal, yVal, zVal); assignment = Expression.Assign(fieldAccess, newVector);
                }
                else if (field.FieldType == typeof(Color)) { var toNumberMethod = typeof(LuaNative).GetMethod("lua_tonumberx", new[] { typeof(IntPtr), typeof(int), typeof(IntPtr) }); var aVal = Expression.Convert(Expression.Call(toNumberMethod, luaParam, Expression.Constant(-1), Expression.Constant(IntPtr.Zero)), typeof(float)); var bVal = Expression.Convert(Expression.Call(toNumberMethod, luaParam, Expression.Constant(-2), Expression.Constant(IntPtr.Zero)), typeof(float)); var gVal = Expression.Convert(Expression.Call(toNumberMethod, luaParam, Expression.Constant(-3), Expression.Constant(IntPtr.Zero)), typeof(float)); var rVal = Expression.Convert(Expression.Call(toNumberMethod, luaParam, Expression.Constant(-4), Expression.Constant(IntPtr.Zero)), typeof(float)); var newColor = Expression.New(typeof(Color).GetConstructor(new[] { typeof(float), typeof(float), typeof(float), typeof(float) }), rVal, gVal, bVal, aVal); assignment = Expression.Assign(fieldAccess, newColor); }
            }
            if (switchCases.Count == 0) { 
                return (ref T inst, string name, IntPtr L) => false; 
            }

            var switchExpr = Expression.Switch(Expression.Call(nameParam, typeof(string).GetMethod("ToLower", Type.EmptyTypes)), Expression.Constant(false), null, switchCases.ToArray());

            var lambda = Expression.Lambda<RefFieldSetter<T>>(switchExpr, instanceParam, nameParam, luaParam);
            return lambda.Compile();
        }
    }
}