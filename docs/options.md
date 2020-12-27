<!-- markdownlint-disable MD033 -->

# atfadmin Configuration Options

Sections:

- [Overriding Server Default Values](#overriding-server-default-values)
- [Prefs File Settings](#prefs-file-settings)
- [Asset Tracking](#asset-tracking)

## Overriding Server Default Values

Use these settings to override the respective `$Host::<variable>` values upon server reset
(e.g., `$atfadmin::Default::Map` will override `$Host::Map`, etc.).

- $atfadmin::Default::Map
- $atfadmin::Default::MissionType
- $atfadmin::Default::Password (set to `"none"` for no password)
- $atfadmin::Default::TimeLimit
- $atfadmin::Default::TournamentMode

## Prefs File Settings

These should be changed/set in `prefs\atfadmin.cs`.

| Setting | Description | Default | Possible Values |
| ------- | ----------- | ------- | --------------- |
| $atfadmin::AdminAutoPWCount | Minimum number of player slots to reserve for admins. | `2` | any integer from `0` to `$Host::MaxPlayers` |
| $atfadmin::AdminAutoPWEnabled | Controls the Admin Auto Password feature. | `false` | `true` or `false` |
| $atfadmin::AdminAutoPWPassword | Password the server will use if it doesn't have at least `$atfadmin::AdminAutoPWCount` admins and the `$HostGamePlayerCount` is greater than or equal to `$Host::MaxPlayers` - (admins needed). | `"changeit"` | any string |
| $atfadmin::AllowAddToAdminList | Allows admins to add other players to the admin list. | `true` | `true` or `false` |
| $atfadmin::AllowAdminAddBots | Allow admins to add bots. | `false` | `true` or `false` |
| $atfadmin::AllowAdminAdmin | Allow admins to temporarily make other players admin. Resets when the server restarts. | `false` | `true` or `false` |
| $atfadmin::AllowAdminBlowupPlayers | Allow admins to blow up players. | `true` | `true` or `false` |
| $atfadmin::AllowAdminBottomPrint | Allow admins to send bottomprint messages. | `false` | `true` or `false` |
| $atfadmin::AllowAdminCancelVote | Allow admins to cancel running votes. | `true` | `true` or `false` |
| $atfadmin::AllowAdminCenterPrint | Allow admins to send centerprint messages. | `true` | `true` or `false` |
| $atfadmin::AllowAdminChangeMission | Allow admins to change the mission and/or gametype. | `2` | `0` = Do not allow admins to change the mission<br />`1` = Allow admins to change the mission<br />`2` = Allow admins to change the mission or gametype |
| $atfadmin::AllowAdminChangeMode | Allow admin to change the server to `tournament`, `pickup`, or `ffa` mode. | `false` | `true` or `false` |
| $atfadmin::AllowAdminChangeTimeLimit | Allow admins to change the time limit. | `false` | `true` or `false` |
| $atfadmin::AllowAdminChat | Allow private messages that all admins can read. | true | `true` or `false` |
| $atfadmin::AllowAdminDisableATFAdmin | Allow admins to disable or enable atfadmin. | `false` | `true` or `false` |
| $atfadmin::AllowAdminDisableVoting | Allow admins to prevent players from voting. | `true` | `true` or `false` |
| $atfadmin::AllowAdminGlobalMutePlayers | Allow admins to globally mute players. | `true` | `true` or `false` |
| $atfadmin::AllowAdminKickBan | Allow admins to kick and/or ban players. | `3` | `0` = Do not allow admins to kick or ban players<br />`1` = Allow admins to kick players<br />`2` = Allow admins to ban players<br />`3` = Allow admins to kick or ban players |
| $atfadmin::AllowAdminLightningStrike | Allow admins to strike players with lightning bolts. | `true` | `true` or `false` |
| $atfadmin::AllowAdminQueueMission | Allow admins to enqueue/dequeue missions. | `true` | `true` or `false` |
| $atfadmin::AllowAdminResetServer | Allow admins to reset the server. | `false` | `true` or `false` |
| $atfadmin::AllowAdminSetPassword | Allow admins to change the server join password. | `false` | `true` or `false` |
| $atfadmin::AllowAdminShowdownSiege | Allow admins to enable/disable Showdown Siege mode. Showdown Siege is an optional match mode for the Siege gametype that makes teams continue switching sides until one team cannot capture the switch. | `true` | `true` or `false` |
| $atfadmin::AllowAdminSkipMission | Allow admins to change the server to the next map in the rotation or queue. | `true` | `true` or `false` |
| $atfadmin::AllowAdminTeamDamage | Allow admins to enable/disable team damage. | `false` | `true` or `false` |
| $atfadmin::AllowAdminTeamInfo | Allow admin to change team info. | `false` | `true` or `false` |
| $atfadmin::AllowAdminToggleBaseRape | Allow admins to toggle base rape on/off. | `false` | `true` or `false` |
| $atfadmin::AllowAdminVoicePackBlock | Allow admin to temporarily or permanently block/unblock a player's voice packs. | `true` | `true` or `false` |
| $atfadmin::AllowComments | Allow players to record comments about the current mission. Comments appear in the server logs. | `true` | `true` or `false` |
| $atfadmin::AllowConsolePrint | Allow players to send messages directly to server console and all SuperAdmins. | `true` | `true` or `false` |
| $atfadmin::AllowObserverChat | Allow observers to globally chat. | `true` | `true` or `false` |
| $atfadmin::AllowPlayerVoteCaptain | Allow players to vote for team captains in Pickup mode. | `true` | `true` or `false` |
| $atfadmin::AllowPlayerVoteChangeMission | Allow players to vote to change the mission and/or gametype. | `1` | `0` = Do not allow players to vote to change the mission<br />`1` = Allow players to vote to change the mission<br />`2` = Allow players to vote to change the mission or gametype |
| $atfadmin::AllowPlayerVoteChangeMode | Allow players to vote for tournament, pickup, or ffa mode. | `false` | `true` or `false` |
| $atfadmin::AllowPlayerVoteGlobalMutePlayers | Allow players to vote to globally mute others. | `false` | `true` or `false` |
| $atfadmin::AllowPlayerVoteKickBan | Allow players to vote to kick and/or ban players. | `1` | `0` = Do not allow players to vote to kick or ban players<br />`1` = Allow players to vote to kick players<br />`2` = Allow players to vote to ban players<br />`3` = allow players to vote to kick or ban players |
| $atfadmin::AllowPlayerVoteQueueMission | Allow players to vote to enqueue/dequeue missions. | `true` | `true` or `false` |
| $atfadmin::AllowPlayerVoteShowdownSiege | Allow players to vote to enable/disable Showdown Siege mode. | true | `true` or `false` |
| $atfadmin::AllowPlayerVoteSkipMission | Allow players to vote to change the server to the next map in rotation or queue. | `true` | `true` or `false` |
| $atfadmin::AllowPlayerVoteTeamDamage | Allow players to vote to enable/disable team damage. | `false` | `true` or `false` |
| $atfadmin::AllowPlayerVoteTimeLimit | Allow players to vote to change the time limit. | `false` | `true` or `false` |
| $atfadmin::AllowVoicePacks | Allow voice packs. | `false` | `true` or `false` |
| $atfadmin::AssetTrack[asset, state] | Configure how assets are tracked and reported. See Asset Tracking, below. | varies | Integer values between `0` and `7`, inclusive |
| $atfadmin::BaseRapeMinimumPlayers | Minimum number of players required on a team before that team's assets can be disabled. In Siege, this affects all non-deployable assets that belong to the offensive team. In CTF, it affects gens, solars, station invos, and vehicle pads. | `10` | Integer values between `0` and `$Host::MaxPlayers`. `0` means no minimum. |
| $atfadmin::BotsMax | Maximum number of bots allowed | `0` | Integer values from `0` to `16`, inclusive |
| $atfadmin::CnHPlayerPointTime | Number of seconds after a capture in CnH that a player gets a point | `12` | any integer value |
| $atfadmin::CnHTeamPointTime | Number of seconds after a capture in CnH that a team gets a point | `12` | any integer value |
| $atfadmin::CnHTowerValue | Number of points each tower is worth in CnH | `1200` | any integer value |
| $atfadmin::ConsoleChat | Allow console chat | `true` | `true` or `false` |
| $atfadmin::DisableATFAdminForTournaments | Automatically disable atfadmin when entering tournament mode | `true` | `true` or `false` |
| $atfadmin::EnableDefenseTurretForFFA | Automatically enable Defense Turret when entering FFA mode | `false` | `true` or `false` |
| $atfadmin::EnableDefenseTurretForPickup | Automatically enable Defense Turret when entering Pickup mode | `false` | `true` or `false` |
| $atfadmin::EnableDefenseTurretForTournament | Automatically enable Defense Turret when entering Tournament mode | `true` | `true` or `false` |
| $atfadmin::EnableExtendedSiegeTourneyHalftime | Require players to click-in for Siege halftime in Tournament mode | `false` | `true` or `false` |
| $atfadmin::EnableStats | Turn on stats tracking (automatically disabled during tournament mode) | `true` | `true` or `false` |
| $atfadmin::ExportATFAdminPrefs | Write atfadmin prefs to `prefs/atfadmin.cs` upon server exit | `true` | `true` or `false` |
| $atfadmin::ExportServerPrefs | Write server prefs to prefs/ServerPrefs.cs upon server exit | `true` | `true` or `false` |
| $atfadmin::HideAdminObserverMessages | Do not display observe message to a player if an admin is observing them | `true` | `true` or `false` |
| $atfadmin::MapRotationFile | Name of file containing the custom map rotation. Will not be used if `$Host::ClassicRandomMissions` is set to true. | `"MapRotation.cs"` | any valid file path, assuming `prefs` as the current directory |
| $atfadmin::MixGametypes | Cycle missions regardless of gametype | `false` | `true` or `false` |
| $atfadmin::MOTD | Message of the day, shown to players upon joining the server. Up to three lines, newline-separated (`NL` or `\n`). | An advertisement for atfadmin | `true` or `false` |
| $atfadmin::MOTDLines | Number of lines in the MOTD | `3` | Integer values between `1` and `3`, inclusive |
| $atfadmin::NoLlamaTeamSwaps | Disallow unfair teams by preventing players from joining the team with more players | `true` | `true` or `false` |
| $atfadmin::ObserverModeRespawnTime | Time, in seconds, that a player must remain in observer mode if they've forced themselves to observe | `15` | Integer values >= `0` |
| $atfadmin::PermanentBanList | Tab-separated list (`TAB` or `\t`) of permanently-banned player GUIDs | none | Any string containing list of a tab-separated player GUIDs |
| $atfadmin::PreserveMapRotationContinuity | Return to normal map rotation after playing a voted map. Otherwise, return to first map in rotation after playing a voted map. (This option only applies if using a custom map rotation.) | `true` | `true` or `false` |
| $atfadmin::RandomTeams | Randomize the teams between map changes | `true` | `true` or `false` |
| $atfadmin::ResetShowdownSiege | Disable Showdown Siege after each mission cycle | `true` | `true` or `false` |
| $atfadmin::ResetTimeLimit | Reset the time limit to the default upon map change | `false` | `true` or `false` |
| $atfadmin::Rule[`number`] | Defines rule number `number` for in-game display, where `number` begins at `0`. (You MUST also set `$atfadmin::Rule[Count]`, see below.) | none | Any string value |
| $atfadmin::Rule[Count] | The number of rules defined | `0` | Integer values >= `0` |
| $atfadmin::ShowdownSiegeList | Tab-separated list (`TAB` or `\t`) of maps to always use with Showdown Siege | none | Any string containing a list of tab-separated map names |
| $atfadmin::ShowSiegeInfo | For Siege maps, show a centerprint blurb with pertinent match information | `true` | `true` or `false` |
| $atfadmin::TimeLimitList | List of time limits, in minutes, selectable for FFA time limit voting options. Separate times by spaces. Set to "" to use default list. | `"15 20 25 30 45 60"` | Any string containing a list of space-separated positive integer values |
| $atfadmin::TimeZone | String representing current server timezone (e.g., `"CST"`). Can also be set to offset from GMT (e.g., `"-0500 GMT"`). | `EST` | Any string value |
| $atfadmin::TournamentModeTimeLimit[`<gametype>`] | Time limit (in minutes) for tournament mode, where `<gametype>` is CTF, Siege, TR2, etc. | `30` for CTF, `20` for Siege | Integer values > 0 |
| $atfadmin::TribeMemberAnnounce | When a tribe member joins/leaves a server, announce that event to players from that same tribe | `true` | `true` or `false` |
| $Host::PickupMode | Enable Pickup mode | `false` | `true` or `false` |
| $Host::PrivilegedList | Tab-separated list of [Privileged](permissions.md) player GUIDs | `""` | Any string containing a list of tab-separated player GUIDs |

## Asset Tracking

atfadmin provides extreme control over the asset tracking and reporting. When a given asset is enabled or disabled, you have complete control over whether or not the state change is reported via popup message, teamchat, server logging, or any combination. In addition, you can completely turn off tracking for a given asset state change.

To manage asset tracking, set the following variable in the prefs file for each asset you want to manage:

```cs
$atfadmin::AssetTrack[`asset`, `state`]
```

Where `asset` is one of the following:

- GeneratorLarge
- SolarPanel
- StationInventory
- DeployedStationInventory
- StationVehicle
- SensorLargePulse
- SensorMediumPulse
- DeployedMotionSensor
- DeployedPulseSensor
- SentryTurret
- TurretBaseLarge
- TurretDeployedOutdoor
- TurretDeployedWallIndoor
- TurretDeployedFloorIndoor
- TurretDeployedCeilingIndoor

And `state` is one of the following:

- `enabled`
- `disabled`

Set the value by adding these values together to get the desired combined behavior:

- 4 = enable popup messages
- 2 = enable teamchat messages
- 1 = enable server logging
- 0 = do not track

For example, to announce that a Deployed Motion Sensor has been disabled via popup and server log messages:

```cs
$atfadmin::AssetTrack[DeployedMotionSensor, disabled] = 5; // 1 (server logging) + 4 (popup message)
```

By default, everything is at least logged and sent to teamchat. State changes for more critical assets (e.g., generators, inventory stations, etc.) are also announced via popups.

See below for a list of all available settings. Note that the possible values for any setting is an integer value between `0` and `7`, inclusive.

| Setting | Description | Default |
| ------- | ----------- | ------- |
| $atfadmin::AssetTrack[DeployedMotionSensor, disabled] | How to announce that a Deployed Motion Sensor has been disabled | 1 + 2 |
| $atfadmin::AssetTrack[DeployedMotionSensor, enabled] | How to announce that a Deployed Motion Sensor has been enabled | 1 + 2 |
| $atfadmin::AssetTrack[DeployedPulseSensor, disabled] | How to announce that a Deployed Pulse Sensor has been disabled | 1 + 2 |
| $atfadmin::AssetTrack[DeployedPulseSensor, enabled] | How to announce that a Deployed Pulse Sensor has been enabled | 1 + 2 |
| $atfadmin::AssetTrack[DeployedStationInventory, disabled] | How to announce that a Deployed Station Inventory has been disabled | 1 + 2 + 4 |
| $atfadmin::AssetTrack[DeployedStationInventory, enabled] | How to announce that a Deployed Station Inventory has been enabled | 1 + 2 + 4 |
| $atfadmin::AssetTrack[GeneratorLarge, disabled] | How to announce that a Generator has been disabled | 1 + 2 + 4 |
| $atfadmin::AssetTrack[GeneratorLarge, enabled] | How to announce that a Generator has been enabled | 1 + 2 + 4 |
| $atfadmin::AssetTrack[SensorLargePulse, disabled] | How to announce that a Large Pulse Sensor has been disabled | 1 + 2 |
| $atfadmin::AssetTrack[SensorLargePulse, enabled] | How to announce that a Large Pulse Sensor has been enabled | 1 + 2 |
| $atfadmin::AssetTrack[SensorMediumPulse, disabled] | How to announce that a Medium Pulse Sensor has been disabled | 1 + 2 |
| $atfadmin::AssetTrack[SensorMediumPulse, enabled] | How to announce that a Medium Pulse Sensor has been enabled | 1 + 2 |
| $atfadmin::AssetTrack[SentryTurret, disabled] | How to announce that a Sentry Turret has been disabled | 1 + 2 |
| $atfadmin::AssetTrack[SentryTurret, enabled] | How to announce that a Sentry Turret has been enabled | 1 + 2 |
| $atfadmin::AssetTrack[SolarPanel, disabled] | How to announce that a Solar Panel has been disabled | 1 + 2 + 4 |
| $atfadmin::AssetTrack[SolarPanel, enabled] | How to announce that a Solar Panel has been enabled | 1 + 2 + 4 |
| $atfadmin::AssetTrack[StationInventory, disabled] | How to announce that an Inventory Station has been disabled | 1 + 2 + 4 |
| $atfadmin::AssetTrack[StationInventory, enabled] | How to announce that an Inventory Station has been enabled | 1 + 2 + 4 |
| $atfadmin::AssetTrack[StationVehicle, disabled] | How to announce that a Vehicle Station has been disabled | 1 + 2 + 4 |
| $atfadmin::AssetTrack[StationVehicle, enabled] | How to announce that a Vehicle Station has been enabled | 1 + 2 + 4 |
| $atfadmin::AssetTrack[TurretBaseLarge, disabled] | How to announce that a Base Turret has been disabled | 1 + 2 + 4 |
| $atfadmin::AssetTrack[TurretBaseLarge, enabled] | How to announce that a Base Turret has been enabled | 1 + 2 + 4 |
| $atfadmin::AssetTrack[TurretDeployedCeilingIndoor, disabled] | How to announce that a ceiling-mounted Spider Clamp Turret has been disabled | 1 + 2 |
| $atfadmin::AssetTrack[TurretDeployedCeilingIndoor, enabled] | How to announce that a ceiling-mounted Spider Clamp Turret has been enabled | 1 + 2 |
| $atfadmin::AssetTrack[TurretDeployedFloorIndoor, disabled] | How to announce that a floor-mounted Spider Clamp Turret has been disabled | 1 + 2 |
| $atfadmin::AssetTrack[TurretDeployedFloorIndoor, enabled] | How to announce that a floor-mounted Spider Clamp Turret has been enabled | 1 + 2 |
| $atfadmin::AssetTrack[TurretDeployedOutdoor, disabled] | How to announce that a Landspike Turret has been disabled | 1 + 2 |
| $atfadmin::AssetTrack[TurretDeployedOutdoor, enabled] | How to announce that a Landspike Turret has been disabled | 1 + 2 |
| $atfadmin::AssetTrack[TurretDeployedWallIndoor, disabled] | How to announce that a wall-mounted Spider Clamp Turret has been disabled | 1 + 2 |
| $atfadmin::AssetTrack[TurretDeployedWallIndoor, enabled] | How to announce that a wall-mounted Spider Clamp Turret has been enabled | 1 + 2 |
