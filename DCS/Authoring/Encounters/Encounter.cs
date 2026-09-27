using DCS.Core;
using UnityEngine;

namespace DCS.Gameplay
{
    [AddComponentMenu("DCS/Gameplay/Encounter")]
    public sealed class Encounter : BaseActor
    {
        [SerializeField] private string _key;
        [SerializeField] private string _scriptPath;
        [SerializeField] private bool _enabled = true;
        [SerializeField] private bool _autoStart;

        public DynamicFacts Facts;
        public EFactsLifetime FactsLifetime;

        public string Key { get => _key; set => _key = value; }
        public string ScriptPath { get => _scriptPath; set => _scriptPath = value; }
        public bool Enabled   { get => _enabled; set => _enabled = value; }
        public bool AutoStart { get => _autoStart; set => _autoStart = value; }

        public override string ToString() => string.IsNullOrEmpty(_key) ? name : _key;
    }
}
