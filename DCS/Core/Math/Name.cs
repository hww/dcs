using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DCS.Core
{
    /// <summary>
    /// Lightweight interned string identifier.
    /// A Name is a 32-bit id produced from a string via CRC32.
    /// Two Names with the same id are considered equal.
    /// Use <see cref="Names.ToStringSafe"/> when the source string may be unknown.
    /// </summary>
    public struct Name : IEquatable<Name>, IComparable<Name>
    {
        private uint _id;

        /// <summary>Raw 32-bit id.</summary>
        public uint Id => _id;

        /// <summary>True if this Name equals <see cref="Names.NULL_ID"/>.</summary>
        public bool IsNull => _id == Names.NULL_ID;

        /// <summary>Copy constructor.</summary>
        public Name(Name name) { _id = name._id; }

        /// <summary>Construct from a raw id. The id is not validated.</summary>
        public Name(uint id) { _id = id; }

        /// <summary>
        /// Construct from a string. The string is interned (registered) in <see cref="Names"/>.
        /// If the same string was interned before, the same id is returned.
        /// </summary>
        public Name(string name) { _id = Names.Intern(name); }

        /// <summary>Returns the original string, or "null" if unknown.</summary>
        public override string ToString() => Names.ToStringSafe(_id);

        /// <summary>Returns the original string, or the fallback if unknown.</summary>
        public string ToString(string fallback)
            => Names.ToStringSafe(_id, fallback);

        // ---- Equality ----

        public bool Equals(Name other) => _id == other._id;
        public override bool Equals(object obj) => obj is Name other && Equals(other);
        public override int GetHashCode() => (int)_id;

        public static bool operator ==(Name a, Name b) => a._id == b._id;
        public static bool operator !=(Name a, Name b) => a._id != b._id;

        // ---- Comparison (ordinal by id, deterministic) ----

        public int CompareTo(Name other) => _id.CompareTo(other._id);
        public static bool operator <(Name a, Name b) => a._id < b._id;
        public static bool operator >(Name a, Name b) => a._id > b._id;
        public static bool operator <=(Name a, Name b) => a._id <= b._id;
        public static bool operator >=(Name a, Name b) => a._id >= b._id;

        // ---- Convenience ----

        /// <summary>Implicit conversion from string (interns the string).</summary>
        public static implicit operator Name(string s) => new Name(s);

        /// <summary>Explicit conversion to uint id.</summary>
        public static explicit operator uint(Name n) => n._id;

        /// <summary>Explicit conversion from uint id.</summary>
        public static explicit operator Name(uint id) => new Name(id);
    }

    /// <summary>
    /// Global interning table for <see cref="Name"/>.
    /// Maps CRC32(string) -> original string.
    /// Not thread-safe. Intended for main-thread use.
    /// </summary>
    public static class Names
    {
        /// <summary>Fallback string for unknown ids.</summary>
        public const string NULL_STRING = "null";

        /// <summary>Id of the fallback string.</summary>
        public static readonly uint NULL_ID = Crc32.Get(NULL_STRING);

        private static readonly Dictionary<uint, string> _names = new Dictionary<uint, string>(1024);

        /// <summary>Number of interned names.</summary>
        public static int Count => _names.Count;

        /// <summary>
        /// Intern a string and return its id.
        /// On CRC32 collision (different string, same id), logs an error
        /// and keeps the first interned string.
        /// </summary>
        public static uint Intern(string name)
        {
            if (string.IsNullOrEmpty(name)) return NULL_ID;

            uint id = Crc32.Get(name);
            if (_names.TryGetValue(id, out string existing))
            {
                if (existing != name)
                    Debug.LogError(
                        $"[Names] CRC32 collision: '{name}' and '{existing}' share id 0x{id:X8}");
                return id;
            }
            _names.Add(id, name);
            return id;
        }

        /// <summary>
        /// Resolve an id back to its string.
        /// Logs an error if the id is unknown.
        /// </summary>
        public static string ToString(uint id)
        {
            if (_names.TryGetValue(id, out string name))
                return name;

            Debug.LogError($"[Names] Unknown id 0x{id:X8}");
            return NULL_STRING;
        }

        /// <summary>
        /// Resolve an id back to its string without logging.
        /// Returns <see cref="NULL_STRING"/> if unknown.
        /// </summary>
        public static string ToStringSafe(uint id)
        {
            return _names.TryGetValue(id, out string name) ? name : NULL_STRING;
        }

        /// <summary>
        /// Resolve an id back to its string without logging.
        /// Returns <paramref name="fallback"/> if unknown.
        /// </summary>
        public static string ToStringSafe(uint id, string fallback)
        {
            return _names.TryGetValue(id, out string name) ? name : (fallback ?? NULL_STRING);
        }

        /// <summary>Returns true if the id is known.</summary>
        public static bool Contains(uint id) => _names.ContainsKey(id);

        /// <summary>Returns true if the string is interned.</summary>
        public static bool Contains(string name)
            => !string.IsNullOrEmpty(name) && _names.ContainsKey(Crc32.Get(name));

        /// <summary>Remove all interned names.</summary>
        public static void Clear() => _names.Clear();

        /// <summary>
        /// Enumerate all interned (id, name) pairs.
        /// Order is unspecified.
        /// </summary>
        public static IEnumerable<KeyValuePair<uint, string>> Entries => _names;

        /// <summary>
        /// Enumerate all interned names as <see cref="Name"/> values.
        /// </summary>
        public static IEnumerable<Name> All
        {
            get
            {
                foreach (var kv in _names)
                    yield return new Name(kv.Key);
            }
        }

        /// <summary>
        /// Enumerate all interned strings.
        /// </summary>
        public static IEnumerable<string> Strings
        {
            get
            {
                foreach (var kv in _names)
                    yield return kv.Value;
            }
        }
    }
}