# Chat Commands

These commands may be used in global or team chat, if you have permissions and the commands are enabled. SuperAdmins can use any of these commands. If you're not sure which commands you can use, just type `.help` in global or team chat to see what commands are available to you.

## Global Chat Commands

Any player can use these commands if they're enabled.

| Command | Description | Applicable Settings |
| ------- | ----------- | ------------ |
| .ac &lt;message&gt; | Admin chat; sends a private message to all admins on the server | $atfadmin::AllowAdminChat |
| .cc &lt;message&gt; | Console chat; sends a private message to the server console and to all SuperAdmins on the server | $atfadmin::AllowConsolePrint |
| .com &lt;message&gt; | Leaves a comment about the server for the SuperAdmins to review. Automatically includes your name and the current map. | $atfadmin::AllowComments |
| .help | Lists the chat commands available to you | N/A |
| .listmissions | Lists the missions in the server\'s rotation. (**NOTE:** list may be shortened due to message length limits) | N/A |
| .motd | Re-displays the server\'s Message Of The Day | $atfadmin::MOTD |
| .observe &lt;id\|name&gt; | Observes the player | N/A |
| .rule &lt;number&gt; | Displays server rule number `<number>` to self | $atfadmin::Rule[Count] and $atfadmin::Rule[number - 1] |
| .rules | Displays all server rules to self | $atfadmin::Rule[Count] and $atfadmin::Rule[0 through Count-1] |
| .shownext | Shows the next mission in the map rotation | N/A |
| .time | Displays the current date and time as perceived by the server | $atfadmin::TimeZone |
| .version | Shows the version of atfadmin running on the server | N/A |

## Privileged Chat Commands

You need to be on the `$Host::PrivilegedList` or an Admin to use these commands, if they're enabled.

| Command | Description | Applicable Settings |
| ------- | ----------- | ------------ |
| .listplayers | Lists the current players on the server | N/A |

## Admin Chat Commands

You need to be an Admin to use these commands, if they're enabled.

| Command | Description | Applicable Settings |
| ------- | ----------- | ------------ |
| .addbots &lt;number&gt; [minSkill] [maxSkill] | Adds `<number>` number of bots to the server, optionally with minimum skill level `minSkill` and maximum skill level `maxSkill` | $atfadmin::AllowAdminAddBots and $Host::BotsEnabled |
| .ban &lt;id\|name&gt; | Bans a player from the server | $atfadmin::AllowAdminKickBan |
| .blowup &lt;id\|name&gt; | Blows up a player into itty bitty chunks | $atfadmin::AllowAdminBlowupPlayers |
| .bp &lt;message&gt; | Sends a bottomprint message to all players | $atfadmin::AllowAdminBottomPrint |
| .cancelvote | Cancels a running vote | $atfadmin::AllowAdminCancelVote |
| .changemission &lt;mission&gt; &lt;gametype&gt; | Loads the specified mission and gametype | $atfadmin::AllowAdminChangeMission |
| .cp &lt;message&gt; | Sends a centerprint message to all players | $atfadmin::AllowAdminCenterPrint |
| .kick &lt;id\|name&gt; | Kicks a player from the server | $atfadmin::AllowAdminKickBan |
| .mode &lt;ffa\|pickup\|tournament&gt; [mission] [gametype] | Changes the server to/from tournament mode, loading the specified mission and gametype | $atfadmin::AllowAdminTournamentMode |
| .mute &lt;id\|name&gt; | Globally mutes the player | $atfadmin::AllowAdminGlobalMutePlayers |
| .passvote | Passes a running vote | $atfadmin::AllowAdminPassVote |
| .removebots &lt;number&gt; | Removes `<number>` number of bots from the server | $Host::BotsEnabled |
| .rule &lt;number&gt; &lt;id\|name\|"all"&gt; | Sends server rule number `<number>` to the specified player, or to _all_ players if "all" is specified | $atfadmin::Rule[Count] and $atfadmin::Rule[number - 1] |
| .smite &lt;id\|name&gt; | Attempts to strike the specified player with lightning. **NOTE:** Not guaranteed to hit the player, and may cause collateral damage! | $atfadmin::AllowAdminLightningStrike |
| .teaminfo &lt;team&gt; &lt;name&gt; &lt;skin&gt; | Changes the name and skin for the specified team | $atfadmin::AllowAdminTeamInfo |
| .unmute &lt;id\|name&gt; | Globally unmutes the player | $atfadmin::AllowAdminGlobalMutePlayers |
| .warn &lt;id\|name&gt; | Warns a player for inappropriate behavior | N/A |
