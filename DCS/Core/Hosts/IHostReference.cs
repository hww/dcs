using DCS.Spatial;
using System;
using System.Text;
using UnityEngine;

namespace DCS.Core
{
    /// <summary>
    /// The object can be associated with a Host
    /// </summary>
    public interface IHostReference
    {
        Host Host { get; }
        void LinkToHost(Host host);
        void UnlinkFromHost();
    }
}