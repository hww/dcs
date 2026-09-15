namespace DCS.Gameplay
{
    /// <summary>Runtime AI roles described in the Uncharted 4 talk. Not scene authoring objects.</summary>
    public enum CombatRole : byte
    {
        None = 0,
        Engager = 1,
        Ambusher = 2,
        Defender = 3,
        GrenadeThrower = 4,
        Flanker = 5
    }

    /// <summary>Posts are generated from authored/world data; they are not hand-authored scene components.</summary>
    public enum PostType : byte
    {
        Open = 0,
        Cover = 1,
        Perch = 2,
        Climb = 3
    }
}
