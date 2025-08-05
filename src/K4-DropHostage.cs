using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;

namespace CS2DropHostage;

[MinimumApiVersion(300)]
public class Plugin : BasePlugin, IPluginConfig<PluginConfig>
{
	public override string ModuleName => "CS2 Drop Hostages";
	public override string ModuleAuthor => "K4ryuu @ KitsuneLab";
	public override string ModuleDescription => "Drop hostages in CS2 using a command or drop weapon key.";
	public override string ModuleVersion => "1.1.0";

	public PluginConfig Config { get; set; } = new();
	public void OnConfigParsed(PluginConfig config) { this.Config = config; }

	private static readonly MemoryFunctionVoid<CBaseEntity, Vector, bool> CHostage_DropHostage
		= new(GameData.GetSignature("CHostage_DropHostage"));

	private readonly MemoryFunctionVoid<CCSPlayerPawn, CBasePlayerWeapon> CCSPlayer_HandleDropWeapon
		= new(GameData.GetSignature("CCSPlayer_HandleDropWeapon"));

	private bool _pluginEnabled = false;

	public override void Load(bool hotReload)
	{
		if (Config.HookDropButton)
			CCSPlayer_HandleDropWeapon.Hook(WeaponDrop_Hook, HookMode.Pre);

		if (Config.Commands.Length != 0)
		{
			foreach (var command in Config.Commands)
			{
				var _command = command.StartsWith("css_") ? command : $"css_{command}";
				AddCommand(_command, "Drop the hostage you're carrying.", OnDropHostageCommand);
			}
		}

		if (hotReload)
			CheckAndTogglePlugin();
	}

	public override void Unload(bool hotReload)
	{
		if (Config.HookDropButton)
			CCSPlayer_HandleDropWeapon.Unhook(WeaponDrop_Hook, HookMode.Pre);
	}

	[GameEventHandler]
	public HookResult Event_RoundStart(EventRoundStart @event, GameEventInfo info)
	{
		CheckAndTogglePlugin();
		return HookResult.Continue;
	}

	private void CheckAndTogglePlugin()
	{
		var hostages = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("hostage_entity");
		_pluginEnabled = hostages.Any();
	}

	private void OnDropHostageCommand(CCSPlayerController? player, CommandInfo info)
	{
		if (!_pluginEnabled)
			return;

		if (player == null || !player.IsValid)
			return;

		var pawn = player.PlayerPawn.Value;
		if (pawn == null || !pawn.IsValid)
			return;

		var hostage = pawn.HostageServices?.CarriedHostage.Value;
		if (hostage == null || !hostage.IsValid)
			return;

		Vector dropPosition = GetPropPosition(pawn, 30);
		CHostage_DropHostage.Invoke(hostage, dropPosition, false);
	}

	private HookResult WeaponDrop_Hook(DynamicHook hook)
	{
		if (!_pluginEnabled)
			return HookResult.Continue;

		var pawn = hook.GetParam<CCSPlayerPawn>(0);
		if (pawn == null || !pawn.IsValid)
			return HookResult.Continue;

		var hostage = pawn.HostageServices?.CarriedHostage.Value;
		if (hostage == null || !hostage.IsValid)
			return HookResult.Continue;

		Vector dropPosition = GetPropPosition(pawn, 30);
		CHostage_DropHostage.Invoke(hostage, dropPosition, false);
		return HookResult.Handled;
	}

	private static Vector GetPropPosition(CCSPlayerPawn pawn, float distance)
	{
		Vector playerPosition = pawn.AbsOrigin ?? Vector.Zero;
		QAngle playerRotation = pawn.AbsRotation ?? QAngle.Zero;

		float radianY = playerRotation.Y * (float)(Math.PI / 180);
		Vector forward = new((float)Math.Cos(radianY) * distance, (float)Math.Sin(radianY) * distance, 0);

		return playerPosition + forward;
	}
}

public class PluginConfig : BasePluginConfig
{
	[JsonPropertyName("HookDropButton")]
	public bool HookDropButton { get; set; } = true;

	[JsonPropertyName("Commands")]
	public string[] Commands { get; set; } = ["drophostage", "dh"];

	[JsonPropertyName("ConfigVersion")]
	public override int Version { get; set; } = 1;
}
