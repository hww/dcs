namespace DCS.Navigation
{
    public enum NavigationSurfaceType : byte { Walkable=0, StealthGrass=1, Avoid=2 }
    public enum TraversalLinkType : byte { Jump=0, Drop=1, Vault=2, Climb=3, GapCross=4 }
}
