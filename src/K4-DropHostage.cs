using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;

namespace CS2DropHostage;

[MinimumApiVersion(300)]
public class Plugin : BasePlugin, IPluginConfig<PluginConfig>
{
	public override string ModuleName => "CS2 Drop Hostages";
	public override string ModuleAuthor => "K4ryuu @ KitsuneLab";
	public override string ModuleDescription => "Drop hostages in CS2 using a command or drop weapon key.";
	public override string ModuleVersion => "1.0.1";

	public PluginConfig Config { get; set; } = new();
	public void OnConfigParsed(PluginConfig config) { this.Config = config; }

	private static readonly MemoryFunctionVoid<CBaseEntity, Vector, bool> DropHostage
		= new("55 48 89 E5 41 57 41 56 41 55 49 89 F5 41 54 41 89 D4 53 48 89 FB 48 81 EC 88 01 00 00"); // ! TODO: Add gamedata file for both Linux and Windows

	private readonly MemoryFunctionVoid<CCSPlayerPawn, CBasePlayerWeapon> DropWeapon
		= new("55 48 89 E5 41 57 41 56 41 55 49 89 F5 41 54 53 48 89 FB 48 83 EC ? 48 8D 05 ? ? ? ? 48 8B 00");

	public override void Load(bool hotReload)
	{
		if (Config.HookDropButton)
			DropWeapon.Hook(WeaponDrop_Hook, HookMode.Pre);

		if (Config.RegisterCommand)
		{
			foreach (var command in Config.Commands)
			{
				var _command = command.StartsWith("css_") ? command : $"css_{command}";
				AddCommand(_command, "Drop the hostage you're carrying.", (player, info) =>
				{
					if (player == null || !player.IsValid)
						return;

					var pawn = player.PlayerPawn.Value;
					if (pawn == null || !pawn.IsValid)
						return;

					var hostage = pawn.HostageServices?.CarriedHostage.Value;
					if (hostage == null || !hostage.IsValid)
						return;

					Vector dropPosition = GetPropPosition(pawn, 30);
					DropHostage.Invoke(hostage, dropPosition, false);
				});
			}
		}
	}

	public override void Unload(bool hotReload)
	{
		if (Config.HookDropButton)
			DropWeapon.Unhook(WeaponDrop_Hook, HookMode.Pre);
	}

	private HookResult WeaponDrop_Hook(DynamicHook hook)
	{
		var pawn = hook.GetParam<CCSPlayerPawn>(0);
		if (pawn == null || !pawn.IsValid)
			return HookResult.Continue;

		var hostage = pawn.HostageServices?.CarriedHostage.Value;
		if (hostage == null || !hostage.IsValid)
			return HookResult.Continue;

		Vector dropPosition = GetPropPosition(pawn, 30);
		DropHostage.Invoke(hostage, dropPosition, false);
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

	[JsonPropertyName("RegisterCommand")]
	public bool RegisterCommand { get; set; } = false;

	[JsonPropertyName("Commands")]
	public string[] Commands { get; set; } = ["drophostage", "dh"];

	[JsonPropertyName("ConfigVersion")]
	public override int Version { get; set; } = 1;
}
