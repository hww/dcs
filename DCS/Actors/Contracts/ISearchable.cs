using DCS.Spatial;
using System;
using System.Text;
using UnityEngine;

namespace DCS.Core
{
    /// <summary>
    /// Contract for any object that can be found via the actor registry.
    /// Separated from ILuaConfigurable because search and Lua scripting
    /// are orthogonal concerns.
    /// </summary>
    public interface ISearchable
    {
        /// <summary>Unique name used for lookup. Null or empty means not searchable by name.</ummary>
        string SearchName { get; }

        /// <summary>Semantic type used for type-filtered queries.</summary>
        ESpatialObjectType ObjectType { get; }

        /// <summary>Tags used for tag-filtered queries. Never null.</summary>
        string[] SearchTags { get; }

        /// <summary>True if this object can be found by at least one search criterion.</summary>
        bool IsSearchable { get; }
    }
}