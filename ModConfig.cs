using StardewModdingAPI;

namespace NpcInfoOverlay;

internal sealed class ModConfig
{
    public bool ShowMarriageEligibility { get; set; } = true;
    public bool ShowGiftStatus { get; set; } = true;
    public bool ShowTalkStatus { get; set; } = true;
    public bool ShowFriendship { get; set; } = true;
    public bool OnlyShowMarriageable { get; set; } = false;
    public bool OnlyShowMale { get; set; } = false;
    public bool OnlyShowFemale { get; set; } = false;
    public bool HideDuringEvents { get; set; } = true;
    public bool OnlyShowNearby { get; set; } = false;
    public float MaxDistanceTiles { get; set; } = 6f;
    public int OffsetX { get; set; } = 0;
    public int OffsetY { get; set; } = -76;
    public float TextScale { get; set; } = 0.75f;
    public float PanelOpacity { get; set; } = 0.78f;
    public SButton ToggleKey { get; set; } = SButton.LeftAlt;
    public bool RequireToggleKey { get; set; } = false;
}
