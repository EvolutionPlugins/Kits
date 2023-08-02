# Kits
OpenMod universal plugin. Adds the kits system.

[![Nuget](https://img.shields.io/nuget/v/DiFFoZ.Kits)](https://www.nuget.org/packages/DiFFoZ.Kits/)
[![Nuget](https://img.shields.io/nuget/dt/DiFFoZ.Kits?label=NuGet%20downloads)](https://www.nuget.org/packages/DiFFoZ.Kits/)
[![Discord](https://img.shields.io/discord/764502843906064434?label=Discord%20chat)](https://discord.gg/5MT2yke)

## How install\update plugin
Run command `openmod install DiFFoZ.Kits`

# Commands
_Maybe is outdated so check help.md to get all commands_
- kit &lt;name&gt; - Give a kit to yourself.
- kit create &lt;name&gt; [cooldown] [cost] [money] [vehicleId] - Create a kit.
- kit remove &lt;name&gt; - Remove a kit.
- kits - Shows a list of available kits for player.

# Permissions
_Maybe is outdated so check help.md to get all permissions_
- Kits:commands.kit
- Kits:commands.kit.create
- Kits:commands.kit.remove
- Kits:commands.kits
- Kits:nocooldown - Allows give a kit without waiting for cooldown 

# How create kit
In-game run command `/kit create <name> [cooldown]`. If you get an error `IPlayer doesn't have compatibility IHasInventory` then your game not support interacting with an inventory.
Also, your inventory is supposed to be not empty.

# How to get permission to XYZ kit
Add permission `Kits:kits.XYZ` to the role _(XYZ is a kit name)_.

# Where kits are saving
In plugin folder. Path `<Game_Path>/<Server_Path>/OpenMod/plugins/Kits/kits.yaml`.

# Where I can get support
You can get support in the my [discord server](https://discord.gg/5MT2yke)
