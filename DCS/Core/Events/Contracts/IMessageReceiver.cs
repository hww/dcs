using DCS.Spatial;
using System;
using System.Text;
using UnityEngine;

namespace DCS.Core
{
    /// <summary>
    /// Interface for components that can receive messages (events).
    /// </summary>
    /// <remarks>
    /// Implemented by processes (FSM) and states subscribed to events.
    /// ReceiveMessage is called by the event delivery system (EventSystem).
    /// </remarks>
    public interface IMessageReceiver
    {
        /// <summary>
        /// Handles an incoming message.
        /// </summary>
        /// <param name="msgHandle">Message handle containing type and data.</param>
        void ReceiveMessage(int msgTypeId, Handle msgHandle);
    }
}