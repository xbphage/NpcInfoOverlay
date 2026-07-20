using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;

namespace NpcInfoOverlay;

internal sealed class ModEntry : Mod
{
    private ModConfig Config = null!;
    private IGenericModConfigMenuApi? Gmcm;
    private long LastDebugTick = -1;
    private string? ConfigLocale;

    public override void Entry(IModHelper helper)
    {
        Config = helper.ReadConfig<ModConfig>();
        helper.Events.Display.RenderedWorld += OnRenderedWorld;
        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        Gmcm = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        RegisterConfigMenu();
        Monitor.Log($"Translation diagnostic: locale='{Helper.Translation.Locale}', display='{T("config.display")}', marriage='{T("config.marriage")}'.", LogLevel.Debug);
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (Gmcm is not null && ConfigLocale != Helper.Translation.Locale)
        {
            RegisterConfigMenu();
            Monitor.Log($"GMCM config refreshed for locale '{Helper.Translation.Locale}'.", LogLevel.Trace);
        }
    }

    private void RegisterConfigMenu()
    {
        if (Gmcm is null) return;
        Gmcm.UnregisterModConfig(ModManifest);
        Gmcm.RegisterModConfig(ModManifest, () => Config = new ModConfig(), () => Helper.WriteConfig(Config));
        Gmcm.SetDefaultIngameOptinValue(ModManifest, true);
        Gmcm.RegisterLabel(ModManifest, T("config.display"), string.Empty);
        AddBool(T("config.marriage"), () => Config.ShowMarriageEligibility, v => Config.ShowMarriageEligibility = v);
        AddBool(T("config.gift"), () => Config.ShowGiftStatus, v => Config.ShowGiftStatus = v);
        AddBool(T("config.talk"), () => Config.ShowTalkStatus, v => Config.ShowTalkStatus = v);
        AddBool(T("config.friendship"), () => Config.ShowFriendship, v => Config.ShowFriendship = v);
        AddBool(T("config.debug"), () => Config.DebugLogging, v => Config.DebugLogging = v);
        AddBool(T("config.marriageable_only"), () => Config.OnlyShowMarriageable, v => Config.OnlyShowMarriageable = v);
        AddBool(T("config.male_only"), () => Config.OnlyShowMale, v => Config.OnlyShowMale = v);
        AddBool(T("config.female_only"), () => Config.OnlyShowFemale, v => Config.OnlyShowFemale = v);
        AddBool(T("config.nearby_only"), () => Config.OnlyShowNearby, v => Config.OnlyShowNearby = v);
        AddBool(T("config.hide_events"), () => Config.HideDuringEvents, v => Config.HideDuringEvents = v);
        AddBool(T("config.require_key"), () => Config.RequireToggleKey, v => Config.RequireToggleKey = v);
        Gmcm.RegisterSimpleOption(ModManifest, T("config.toggle_key"), string.Empty, () => Config.ToggleKey, v => Config.ToggleKey = v);
        Gmcm.RegisterLabel(ModManifest, T("config.layout"), string.Empty);
        Gmcm.RegisterClampedOption(ModManifest, T("config.distance"), string.Empty, () => (int)Config.MaxDistanceTiles, v => Config.MaxDistanceTiles = v, 1, 12, 1);
        Gmcm.RegisterClampedOption(ModManifest, T("config.offset_x"), string.Empty, () => Config.OffsetX, v => Config.OffsetX = v, -200, 200, 1);
        Gmcm.RegisterClampedOption(ModManifest, T("config.offset_y"), string.Empty, () => Config.OffsetY, v => Config.OffsetY = v, -180, 20, 1);
        Gmcm.RegisterClampedOption(ModManifest, T("config.text_scale"), string.Empty, () => Config.TextScale, v => Config.TextScale = v, 0.5f, 1.2f, 0.05f);
        Gmcm.RegisterClampedOption(ModManifest, T("config.opacity"), string.Empty, () => Config.PanelOpacity, v => Config.PanelOpacity = v, 0.1f, 1f, 0.05f);
        ConfigLocale = Helper.Translation.Locale;
    }

    private void AddBool(string name, Func<bool> get, Action<bool> set) => Gmcm!.RegisterSimpleOption(ModManifest, name, string.Empty, get, set);

    private string T(string key, object? tokens = null) => tokens is null
        ? Helper.Translation.Get(key)
        : Helper.Translation.Get(key, tokens);

    private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (!Context.IsWorldReady || (Config.HideDuringEvents && Game1.eventUp) || (Config.RequireToggleKey && !Helper.Input.IsDown(Config.ToggleKey))) return;
        Farmer player = Game1.player;
        foreach (NPC npc in Game1.currentLocation.characters.OfType<NPC>())
        {
            if (npc is Child || npc.IsMonster || string.IsNullOrWhiteSpace(npc.Name)) continue;
            if (Config.OnlyShowMarriageable && !CanMarry(npc)) continue;
            if (Config.OnlyShowMale != Config.OnlyShowFemale)
            {
                if (Config.OnlyShowMale && !IsGender(npc, "Male")) continue;
                if (Config.OnlyShowFemale && !IsGender(npc, "Female")) continue;
            }
            float distance = Vector2.Distance(player.Position, npc.Position) / 64f;
            if (Config.OnlyShowNearby && distance > Config.MaxDistanceTiles) continue;
            if (Config.DebugLogging && Game1.ticks != LastDebugTick && Game1.ticks % 60 == 0)
            {
                Friendship? debugFriendship = null;
                player.friendshipData.TryGetValue(npc.Name, out debugFriendship);
                Monitor.Log($"NPC={npc.Name}, Gender={GetMember(npc, "Gender")}, Marriageable={CanMarry(npc)}, Points={debugFriendship?.Points.ToString() ?? "none"}, GiftsToday={GetInt(debugFriendship, "GiftsToday")}, GiftsThisWeek={GetInt(debugFriendship, "GiftsThisWeek")}, TalkedToday={GetBool(debugFriendship, "TalkedToToday")}, Locale={Helper.Translation.Locale}", LogLevel.Debug);
            }
            DrawInfo(e.SpriteBatch, npc, player);
        }
        if (Config.DebugLogging && Game1.ticks % 60 == 0) LastDebugTick = Game1.ticks;
    }

    private void DrawInfo(SpriteBatch batch, NPC npc, Farmer player)
    {
        Friendship? friendship = null;
        player.friendshipData.TryGetValue(npc.Name, out friendship);
        List<(string Text, Color Color)> lines = new();
        bool isMale = IsGender(npc, "Male");
        bool isFemale = IsGender(npc, "Female");
        string gender = isMale ? T("gender.male") : isFemale ? T("gender.female") : string.Empty;
        Color nameColor = isMale ? Color.CornflowerBlue : isFemale ? Color.HotPink : Color.White;
        lines.Add((T("display.name", new { name = npc.Name, gender }), nameColor));
        if (Config.ShowMarriageEligibility)
        {
            bool canMarry = CanMarry(npc);
            lines.Add((T("display.marriage", new { status = T(canMarry ? "status.marriage_yes" : "status.marriage_no") }), canMarry ? Color.LimeGreen : Color.IndianRed));
        }
        if (Config.ShowGiftStatus && friendship is not null)
        {
            int giftsThisWeek = GetInt(friendship, "GiftsThisWeek");
            bool cannotGift = giftsThisWeek >= 2;
            bool giftedToday = GetInt(friendship, "GiftsToday") > 0;
            string giftStatus = T(cannotGift ? "status.cannot_gift" : giftedToday ? "status.gifted" : "status.not_gifted");
            Color giftColor = cannotGift ? Color.IndianRed : giftedToday ? Color.LimeGreen : Color.Yellow;
            lines.Add((T("display.gift", new { status = giftStatus }), giftColor));
        }
        if (Config.ShowTalkStatus && friendship is not null)
        {
            bool talked = GetBool(friendship, "TalkedToToday");
            lines.Add((T("display.talk", new { status = T(talked ? "status.talked" : "status.not_talked") }), talked ? Color.LimeGreen : Color.Yellow));
        }
        if (Config.ShowFriendship && friendship is not null)
            lines.Add((T("display.friendship", new { hearts = Math.Max(0, friendship.Points) / 250 }), Color.White));
        if (lines.Count == 0) return;

        Vector2 pos = Game1.GlobalToLocal(Game1.viewport, npc.Position) + new Vector2(Config.OffsetX, Config.OffsetY);
        float scale = Config.TextScale;
        Vector2 size = Vector2.Zero;
        foreach (var line in lines) size.X = Math.Max(size.X, Game1.smallFont.MeasureString(line.Text).X * scale);
        size.Y = lines.Count * Game1.smallFont.LineSpacing * scale;
        pos.X = MathHelper.Clamp(pos.X - size.X / 2f, 4, Game1.viewport.Width - size.X - 4);
        pos.Y = MathHelper.Clamp(pos.Y - size.Y, 4, Game1.viewport.Height - size.Y - 4);
        Rectangle box = new((int)pos.X - 5, (int)pos.Y - 4, (int)size.X + 10, (int)size.Y + 8);
        batch.Draw(Game1.fadeToBlackRect, box, Color.Black * Config.PanelOpacity);
        for (int i = 0; i < lines.Count; i++)
            batch.DrawString(Game1.smallFont, lines[i].Text, pos + new Vector2(0, i * Game1.smallFont.LineSpacing * scale), lines[i].Color, 0, Vector2.Zero, scale, SpriteEffects.None, 1f);
    }

    private static bool CanMarry(NPC npc) => npc.datable.Value && !npc.isMarried();

    private static bool IsGender(NPC npc, string gender)
    {
        object? value = GetMember(npc, "Gender");
        return value?.ToString()?.Equals(gender, StringComparison.OrdinalIgnoreCase) == true;
    }

    private static object? GetMember(object obj, string name)
    {
        var type = obj.GetType();
        return type.GetProperty(name)?.GetValue(obj) ?? type.GetField(name)?.GetValue(obj);
    }

    private static int GetInt(object? obj, string name) => obj is not null && GetMember(obj, name) is int value ? value : 0;
    private static bool GetBool(object? obj, string name) => obj is not null && GetMember(obj, name) is bool value && value;
}
