using UnityEngine;
namespace DCS.Interaction.Authoring
{
    [AddComponentMenu("DCS/Gameplay/Region")]
    public sealed class Region : BaseRegion
    {
        [TextArea(2, 5)] [SerializeField] private string _description;
        public string Description { get => _description; set => _description = value; }
        public override string ToString() => string.IsNullOrEmpty(Key) ? name : Key;
    }
}
