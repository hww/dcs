// Naughty Dog style high-performance runtime JIT compiler for universal structural layout marshalling.
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using DCS.Lua;

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
                var expressions = new List<Expression>();

                // Process field layout via fully recursive meta-data evaluation paths
                if (AppendGetterExpressions(field.FieldType, fieldAccess, luaParam, expressions))
                {
                    expressions.Add(Expression.Constant(true));
                    var block = Expression.Block(expressions);
                    switchCases.Add(Expression.SwitchCase(block, Expression.Constant(field.Name.ToLower())));
                }
            }

            if (switchCases.Count == 0)
            {
                return delegate (ref T inst, string name, IntPtr L) { LuaNative.lua_pushnil(L); return false; };
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
                Expression fieldAccess = Expression.Field(instanceParam, field);
                var expressions = new List<Expression>();
                int stackOffset = -1; // Pulling items sequentially in reverse order from stack top

                if (AppendSetterExpressions(field.FieldType, fieldAccess, luaParam, expressions, ref stackOffset))
                {
                    expressions.Add(Expression.Constant(true));
                    var block = Expression.Block(expressions);
                    switchCases.Add(Expression.SwitchCase(block, Expression.Constant(field.Name.ToLower())));
                }
            }

            if (switchCases.Count == 0)
            {
                return delegate (ref T inst, string name, IntPtr L) { return false; };
            }

            var switchExpr = Expression.Switch(
                Expression.Call(nameParam, typeof(string).GetMethod("ToLower", Type.EmptyTypes)),
                Expression.Constant(false),
                null,
                switchCases.ToArray()
            );

            var lambda = Expression.Lambda<RefFieldSetter<T>>(switchExpr, instanceParam, nameParam, luaParam);
            return lambda.Compile();
        }

        // --- PRIVATE CORE EVALUATORS ---

        private static bool AppendGetterExpressions(Type targetType, Expression accessExpr, ParameterExpression luaParam, List<Expression> exprs)
        {
            // Handle standard Game Enums automatically by casting underlying values straight to integers
            if (targetType.IsEnum)
            {
                var pushMethod = typeof(LuaNative).GetMethod("lua_pushinteger", new[] { typeof(IntPtr), typeof(long) });
                exprs.Add(Expression.Call(pushMethod, luaParam, Expression.Convert(accessExpr, typeof(long))));
                return true;
            }

            if (targetType == typeof(float) || targetType == typeof(double))
            {
                var pushMethod = typeof(LuaNative).GetMethod("lua_pushnumber", new[] { typeof(IntPtr), typeof(double) });
                exprs.Add(Expression.Call(pushMethod, luaParam, Expression.Convert(accessExpr, typeof(double))));
                return true;
            }

            if (targetType == typeof(int) || targetType == typeof(long) || targetType == typeof(uint) || targetType == typeof(ushort) || targetType == typeof(byte))
            {
                var pushMethod = typeof(LuaNative).GetMethod("lua_pushinteger", new[] { typeof(IntPtr), typeof(long) });
                exprs.Add(Expression.Call(pushMethod, luaParam, Expression.Convert(accessExpr, typeof(long))));
                return true;
            }

            if (targetType == typeof(bool))
            {
                var pushMethod = typeof(LuaNative).GetMethod("lua_pushboolean", new[] { typeof(IntPtr), typeof(bool) });
                exprs.Add(Expression.Call(pushMethod, luaParam, accessExpr));
                return true;
            }

            if (targetType == typeof(string))
            {
                var pushMethod = typeof(LuaNative).GetMethod("lua_pushstring", new[] { typeof(IntPtr), typeof(string) });
                exprs.Add(Expression.Call(pushMethod, luaParam, accessExpr));
                return true;
            }

            // RECURSIVE LAYOUT PARSING: Auto-unboxing nested structural sub-objects (Vector3, Color, custom structs)
            if (targetType.IsValueType && !targetType.IsPrimitive)
            {
                var subFields = targetType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                if (subFields.Length == 0) return false;

                bool subValid = false;
                foreach (var subField in subFields)
                {
                    Expression subFieldAccess = Expression.Field(accessExpr, subField);
                    if (AppendGetterExpressions(subField.FieldType, subFieldAccess, luaParam, exprs))
                    {
                        subValid = true;
                    }
                }
                return subValid;
            }

            return false;
        }

        private static bool AppendSetterExpressions(Type targetType, Expression accessExpr, ParameterExpression luaParam, List<Expression> exprs, ref int stackOffset)
        {
            if (targetType.IsEnum)
            {
                var toIntegerMethod = typeof(LuaNative).GetMethod("lua_tointegerx", new[] { typeof(IntPtr), typeof(int), typeof(IntPtr) });
                var luaValue = Expression.Call(toIntegerMethod, luaParam, Expression.Constant(stackOffset--), Expression.Constant(IntPtr.Zero));
                exprs.Add(Expression.Assign(accessExpr, Expression.Convert(luaValue, targetType)));
                return true;
            }

            if (targetType == typeof(float) || targetType == typeof(double))
            {
                var toNumberMethod = typeof(LuaNative).GetMethod("lua_tonumberx", new[] { typeof(IntPtr), typeof(int), typeof(IntPtr) });
                var luaValue = Expression.Call(toNumberMethod, luaParam, Expression.Constant(stackOffset--), Expression.Constant(IntPtr.Zero));
                exprs.Add(Expression.Assign(accessExpr, Expression.Convert(luaValue, targetType)));
                return true;
            }

            if (targetType == typeof(int) || targetType == typeof(long) || targetType == typeof(uint) || targetType == typeof(ushort) || targetType == typeof(byte))
            {
                var toIntegerMethod = typeof(LuaNative).GetMethod("lua_tointegerx", new[] { typeof(IntPtr), typeof(int), typeof(IntPtr) });
                var luaValue = Expression.Call(toIntegerMethod, luaParam, Expression.Constant(stackOffset--), Expression.Constant(IntPtr.Zero));
                exprs.Add(Expression.Assign(accessExpr, Expression.Convert(luaValue, targetType)));
                return true;
            }

            if (targetType == typeof(bool))
            {
                var toBoolMethod = typeof(LuaNative).GetMethod("lua_toboolean", new[] { typeof(IntPtr), typeof(int) });
                var luaValue = Expression.NotEqual(Expression.Call(toBoolMethod, luaParam, Expression.Constant(stackOffset--)), Expression.Constant(0));
                exprs.Add(Expression.Assign(accessExpr, luaValue));
                return true;
            }

            if (targetType == typeof(string))
            {
                var tolStringMethod = typeof(LuaNative).GetMethod("lua_tolstring", new[] { typeof(IntPtr), typeof(int), typeof(IntPtr) });
                var luaValuePtr = Expression.Call(tolStringMethod, luaParam, Expression.Constant(stackOffset--), Expression.Constant(IntPtr.Zero));
                var marshalMethod = typeof(System.Runtime.InteropServices.Marshal).GetMethod("PtrToStringUTF8", new[] { typeof(IntPtr) });
                var stringValue = Expression.Call(marshalMethod, luaValuePtr); exprs.Add(Expression.Assign(accessExpr, stringValue)); return true;
            }// RECURSIVE STRUCT LAYOUT SETTER: Reconstruct subsets from stack offsets top-to-bottom sequentially
            if (targetType.IsValueType && !targetType.IsPrimitive)
            {
                var subFields = targetType.GetFields(BindingFlags.Public | BindingFlags.Instance); if (subFields.Length == 0) return false;
                // Value types require processing fields in complete reverse structural order to mirror sequential stack logic
                var reverseFields = new List<FieldInfo>(subFields);
                reverseFields.Reverse();
                var subAssigns = new List<Expression>();
                bool subValid = false; foreach (var subField in reverseFields)
                {
                    //Expression 
                    var subFieldAccess = Expression.Field(accessExpr, subField);
                    if (AppendSetterExpressions(subField.FieldType, subFieldAccess, luaParam, subAssigns, ref stackOffset)) { subValid = true; }
                }
                if (subValid)
                {
                    // Reverse the assignment expressions block to maintain original storage execution directions safely 
                    subAssigns.Reverse();
                    exprs.AddRange(subAssigns);
                    return true;
                }
            }
            return false;
        }
    }
}