using DCS.Core;
using UnityEngine;

public class SoldierLink : MonoBehaviour, IHostReference
{
    public Host Host { get; private set; }
    public void LinkToHost(Host host) => Host = host;
    public void UnlinkFromHost() => Host = new Host { Id = HandleConfig.NULL_INDEX };
}