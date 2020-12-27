# atfadmin

A server admin mod for Tribes 2 and TribesNext

## About

a tiny fishie\'s Admin Mod (atfadmin) for Tribes 2 is a set of modified Tribes 2 server-side functions to make managing a server easier for admins and players. It is designed to be as minimally-invasive as possible. There is no client-side code within atfadmin, though an optional atfclient is available. atfadmin changes no gameplay physics whatsoever, and only functions as an add-on to help control your server's behavior. It works alongside any other gametype mods you are currently running on your server, provided those mods don't incorrectly override core game functionality.

atfadmin was originally based on an old StarSiege:Tribes admin mod, bwadmin.

Suggestions and contributions welcomed!

## Features

Many atfadmin features can be altered via the prefs file before the server is started. Most of those features can also be changed in-game by vote or by admin. Here are some of the notable features:

- Minimally-invasive. NO changes to game physics, weapons, armors, packs, vehicles, and other objects. Packaged code means fewer chances for interfering with other server mods.
- Works with Base or Classic, and other game mods that correctly override default server behavior.
- Admin and player control. Many prefs settings give fine-grained control over what admins and players are allowed to do.
- Auto-deactivation for tournaments. atfadmin can be automatically or manually disabled when entering Tournament Mode without a server reset.
- Customizable Map Rotation. Specify minimum and maximum player limits for maps and indicate if they're client-side or server-side.
- Chat commands. Chat commands can be used in-game to make things easier for admins and players.
- Asset Tracking. Custom notifications for enable/disable events on a per-asset basis.
- Base Rape Control. Set a minimum number of players required for base raping.
- Voice Pack Block. Voice packs may be blocked for the entire server or on a per-player basis, temporarily or permanently.
- Enhanced logging and auditing. Better tracking of player and admin actions, as well as more detailed information about damage types.
- Supports all gametypes, but also has many fixes and enhancements for the Siege (mostly), Capture and Hold, and Team Hunters gametypes.

See the [Options documentation](docs/options.md) for more detail on available configuration options.

## Client Installation

To play on a server running atfadmin, you don't need anything except Tribes 2 installed. However, there is an optional client that can be installed to give you additional lobby popup menu options, and is especially helpful for server admins. To install the client, download the latest atfclient.vl2 to your `Tribes2\Gamedata\base` directory and restart Tribes 2.

## Server Installation

You'll only need to follow these steps if you're installing atfadmin on a Tribes 2 game server.

1. Download the VL2 of the [latest release](https://github.com/kfox/atfadmin/releases/latest) and put it in your server's `GameData\base` directory.
2. Restart your server. (This creates a default `prefs\atfadmin.cs` file for you.)
3. Stop your server.
4. Edit the `prefs\atfadmin.cs` file to set/select server [preferences](docs/options.md).
5. (optional) Edit the `prefs\MapRotation.cs` file to select the map rotation.
6. Start your server and have fun!

## Documentation

- [Configuration options](docs/options.md)
- [Player permissions](docs/permissions.md)
- [Chat commands reference](docs/chat-commands.md)
- [List of changes by version](docs/changes.txt)
- A collection of possibly-helpful [tools](tools/README.md) for mod, map, and skin pack development

## Credits

- Author: a tiny fishie
- Known contributors: Crunchy, Deathace, Poker, Red Shifter, ruWank

## Thanks

- The Tribes 2 development team for a great game
- The TribesNext team for keeping Tribes 2 alive
- All the players on "the pond" and |RDS| servers for suggestions and feedback
- The original bwadmin authors
