// START package atfadmin_chatConsole
package atfadmin_chatConsole
{


function atfadmin_CommandChangeMode(%sender, %paramlist)
{
	if (!%sender.isAdmin)
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2You must be an admin to use this command.');
		return;
	}

	if (!%sender.isSuperAdmin && $atfadmin::AllowAdminChangeMode)
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2This command is disabled for your admin level.');
		return;
	}

	atfadmin_GetParams(%paramlist);

	if ($atfadmin_param[0] $= "")
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2.mode usage: .mode <ffa|pickup|tournament> [mission] [type]');
		return;
	}

	%mode = $atfadmin_param[0];

	if (%mode $= "ffa")
	{
		if ($Host::TournamentMode || $Host::PickupMode)
		{
			$AdminCl = %sender;
			Game.voteFFAMode(true, %sender);
		}
	}
	else
	{
		%mission     = $atfadmin_param[1];
		%missionType = $atfadmin_param[2];

		if (%mission $= "" || %missionType $= "")
		{
			%mission     = $CurrentMission;
			%missionType = $CurrentMissionType;
		}

		if (!atfadmin_validateMission(%mission, %missionType))
		{
			messageClient(%sender, 'MsgATFAdminCommand', '\c2Cannot find that mission.');
			return;
		}

		%mis = atfadmin_getMissionIndexFromFileName(%mission);
		%type = $MissionList[%mis, TypeIndex];

		switch$ (%mode)
		{
			case "tournament":
				if (!$Host::TournamentMode)
				{
					$AdminCl = %sender;
					Game.voteTournamentMode(true, $HostMissionName[%mis], $HostTypeDisplayName[%type], %mis, %type);
				}
			case "pickup":
				if (!$Host::PickupMode)
				{
					$AdminCl = %sender;
					Game.votePickupMode(true, $HostMissionName[%mis], $HostTypeDisplayName[%type], %mis, %type);
				}
		}
	}
}

function atfadmin_CommandChangeMission(%sender, %paramlist)
{
	if (!%sender.isAdmin)
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2You must be an admin to use this command.');
		return;
	}

	// player is NOT a superadmin AND admins are NOT allowed to change the mission
	if (!%sender.isSuperAdmin && !$atfadmin::AllowAdminChangeMission)
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2This command is disabled for your admin level.');
		return;
	}

	atfadmin_GetParams(%paramlist);

	if ($atfadmin_param[0] $= "" || $atfadmin_param[1] $= "")
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2.changemission usage: .changemission <mission> <type>');
		return;
	}

	%mission     = $atfadmin_param[0];
	%missionType = $atfadmin_param[1];

	if (atfadmin_validateMission(%mission, %missionType) $= "")
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2Cannot find that mission.');
		return;
	}

	%mis = atfadmin_getMissionIndexFromFileName(%mission);
	%type = $MissionList[%mis, TypeIndex];

	logEcho(%sender.nameBase@" changed the mission to "@$mission@" ("@$missionType@")");
	Game.voteChangeMission(true, $HostMissionName[%mis], $HostTypeDisplayName[%type], %mis, %type);
}

function atfadmin_CommandShowNext(%sender)
{
	if ($atfadmin_missionQueue $= "")
	{
		%next = atfadmin_findNextCycleMissionID();
		%mission = $MissionList[%next, MissionDisplayName];
		if ($atfadmin_nextMissionID !$= "")
			%type = $MissionList[$atfadmin_nextMissionID, TypeDisplayName];
		else
			%type = $MissionList[%next, TypeDisplayName];
		messageClient(%sender, 'MsgATFAdminCommand', '\c2The next mission in rotation is %1 (%2).', %mission, %type);
	}
	else
		atfadmin_showMissionQueue(%sender);
}

function atfadmin_CommandShowTime(%sender)
{
	%datetime = formatTimeString(MM) SPC formatTimeString(d) @ "," SPC formatTimeString(yy) SPC formatTimeString(h) @ ":" @ formatTimeString(nn) @ ":" @ formatTimeString(ss) SPC formatTimeString(A) SPC $atfadmin::TimeZone;
	messageClient(%sender, 'MsgATFAdminCommand', '\c2The current server date and time is: %1.', %datetime);
}

function atfadmin_CommandHelp(%sender)
{
	messageClient(%sender, 'MsgATFAdminCommand', '\c2Chat console commands:');

	if ($atfadmin::AllowAdminChat)
		messageClient(%sender, 'MsgATFAdminCommand', '\c2.ac <msg>');

	if ($Host::BotsEnabled && %sender.isAdmin)
		messageClient(%sender, 'MsgATFAdminCommand', '\c2.addbots <num> [minSkill] [maxSkill]');

	if ($atfadmin::AllowAdminKickBan & 2 && %sender.isAdmin)
		messageClient(%sender, 'MsgATFAdminCommand', '\c2.ban <id|name>');

	if ($atfadmin::AllowAdminBlowupPlayers && %sender.isAdmin)
		messageClient(%sender, 'MsgATFAdminCommand', '\c2.blowup <id|name>');

	if ($atfadmin::AllowAdminBottomPrint && %sender.isAdmin)
		messageClient(%sender, 'MsgATFAdminCommand', '\c2.bp <msg>');

	if ($atfadmin::AllowAdminCancelVote && %sender.isAdmin)
		messageClient(%sender, 'MsgATFAdminCommand', '\c2.cancelvote');

	if ($atfadmin::AllowAdminPassVote && %sender.isAdmin)
		messageClient(%sender, 'MsgATFAdminCommand', '\c2.passvote');

	if ($atfadmin::AllowConsolePrint)
		messageClient(%sender, 'MsgATFAdminCommand', '\c2.cc <msg>');

	if ($atfadmin::AllowAdminChangeMission && %sender.isAdmin)
		messageClient(%sender, 'MsgATFAdminCommand', '\c2.changemission <mission> <type>');

	if ($atfadmin::AllowComments)
		messageClient(%sender, 'MsgATFAdminCommand', '\c2.com <comment>');

	if ($atfadmin::AllowAdminCenterPrint && %sender.isAdmin)
		messageClient(%sender, 'MsgATFAdminCommand', '\c2.cp <msg>');

	messageClient(%sender, 'MsgATFAdminCommand', '\c2.help');

	if ($atfadmin::AllowAdminKickBan & 1 && %sender.isAdmin)
		messageClient(%sender, 'MsgATFAdminCommand', '\c2.kick <id|name>');

	if (%sender.isPrivileged || %sender.isAdmin)
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2.listplayers');
	}

	if ($atfadmin::AllowAdminChangeMode && %sender.isAdmin)
		messageClient(%sender, 'MsgATFAdminCommand', '\c2.mode <ffa|pickup|tournament> <mission> <type>');

	messageClient(%sender, 'MsgATFAdminCommand', '\c2.motd');

	if ($atfadmin::AllowAdminGlobalMutePlayers && %sender.isAdmin)
		messageClient(%sender, 'MsgATFAdminCommand', '\c2.mute <id|name>');

	messageClient(%sender, 'MsgATFAdminCommand', '\c2.observe <id|name>');

	//if (%sender.isAdmin)
		//messageClient(%sender, 'MsgATFAdminCommand', '\c2.pm <id|name> <msg>');

	if ($Host::BotsEnabled && %sender.isAdmin)
		messageClient(%sender, 'MsgATFAdminCommand', '\c2.removebots <num>');

	//messageClient(%sender, 'MsgATFAdminCommand', '\c2.reply <msg>');

	if ($atfadmin::Rule[Count] > 0)
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2.rule <number>');

		if (%sender.isAdmin)
			messageClient(%sender, 'MsgATFAdminCommand', '\c2.rule <number> <id|name>');

		messageClient(%sender, 'MsgATFAdminCommand', '\c2.rules');
	}

	messageClient(%sender, 'MsgATFAdminCommand', '\c2.shownext');

	if ($atfadmin::AllowAdminLightningStrike && %sender.isAdmin)
		messageClient(%sender, 'MsgATFAdminCommand', '\c2.smite <id|name>');

	if ($atfadmin::AllowAdminTeamInfo && %sender.isAdmin)
		messageClient(%sender, 'MsgATFAdminCommand', '\c2.teaminfo <team> <name> <skin>');

	messageClient(%sender, 'MsgATFAdminCommand', '\c2.time');

	if ($atfadmin::AllowAdminGlobalMutePlayers && %sender.isAdmin)
		messageClient(%sender, 'MsgATFAdminCommand', '\c2.unmute <id|name>');

	messageClient(%sender, 'MsgATFAdminCommand', '\c2.version');

	if (%sender.isAdmin)
		messageClient(%sender, 'MsgATFAdminCommand', '\c2.warn <id|name>');
}

function atfadmin_CommandVersion(%sender)
{
	messageClient(%sender, 'MsgATFAdminCommand', '\c2This server is running atfadmin %1.', $atfadmin_Version);
}

function atfadmin_CommandMOTD(%sender)
{
	if ($atfadmin::MOTD $= "")
		messageClient(%sender, 'MsgATFAdminCommand', '\c2This server does not have a message of the day.');
	else
		centerprint(%sender, $atfadmin::MOTD, 10, $atfadmin::MOTDLines);
}

function atfadmin_CommandObservePlayer(%sender, %param)
{
	if (%sender.team != 0) return;

	%target = atfadmin_FindTarget(%param);
	if (%target == -1)
		messageClient(%sender, 'MsgATFAdminCommand', '\c2.observe usage: .observe <id|name>');
	else if (%target.team != 0)
	{
		serverCmdObserveClient(%sender, %target);
		displayObserverHud(%sender, %target);
	}
}

function atfadmin_CommandWarnPlayer(%sender, %param)
{
	if (!%sender.isAdmin)
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2You must be an admin to use this command.');
		return;
	}

	if (!%sender.isSuperAdmin && !$atfadmin::AllowAdminKickBan)
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2This command is disabled for your admin level.');
		return;
	}

	%target = atfadmin_FindTarget(%param);
	if (%target == -1)
		messageClient(%sender, 'MsgATFAdminCommand', '\c2.warn usage: .warn <id|name>');
	else
	{
		$AdminCl = %sender;
		serverCmdWarnPlayer(%sender, %target);
	}
}

function atfadmin_CommandKickPlayer(%sender, %param)
{
	if (!%sender.isAdmin)
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2You must be an admin to use this command.');
		return;
	}

	if (!%sender.isSuperAdmin && !($atfadmin::AllowAdminKickBan & 1))
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2This command is disabled for your admin level.');
		return;
	}

	%target = atfadmin_FindTarget(%param);
	if (%target == -1)
		messageClient(%sender, 'MsgATFAdminCommand', '\c2.kick usage: .kick <id|name>');
	else if (atfadmin_OutrankTarget(%sender, %target))
	{
		$AdminCl = %sender;
		logEcho(%sender.nameBase@" kicked "@%target.nameBase);
		Game.voteKickPlayer(true, %target);
	}
	else
		messageClient(%sender, 'MsgATFAdminCommand', '\c2Cannot kick %1 as you do not outrank %2.', %target.name, %target.sex $= "Male" ? 'him' : 'her');
}

function atfadmin_CommandBanPlayer(%sender, %param)
{
	if (!%sender.isAdmin)
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2You must be an admin to use this command.');
		return;
	}

	if (!%sender.isSuperAdmin && !($atfadmin::AllowAdminKickBan & 2))
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2This command is disabled for your admin level.');
		return;
	}

	%target = atfadmin_FindTarget(%param);
	if (%target == -1)
		messageClient(%sender, 'MsgATFAdminCommand', '\c2.ban usage: .ban <id|name>');
	else
	{
		if (%target.isAIControlled())
			messageClient(%sender, 'MsgATFAdminCommand', '\c2Cannot ban a bot, use kick instead.');
		else if (atfadmin_OutrankTarget(%sender, %target))
		{
			logEcho(%sender.nameBase@" banned "@%target.nameBase);
			Game.voteBanPlayer(true, %target);
		}
		else
			messageClient(%sender, 'MsgATFAdminCommand', '\c2Cannot ban %1 as you do not outrank %2.', %target.name, %target.sex $= "Male" ? 'him' : 'her');
	}
}

function atfadmin_CommandListPlayers(%sender)
{
	if (!%sender.isAdmin && !%sender.isPrivileged)
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2You must be privileged or an admin to use this command.');
		return;
	}

	if ($PlayingOnline)
		messageClient(%sender, 'MsgATFAdminCommand', '\c2WON Id\tName');
	else
		messageClient(%sender, 'MsgATFAdminCommand', '\c2Client\tName');

	for (%i = 0; %i < ClientGroup.getCount(); %i++)
	{
		%client = ClientGroup.getObject(%i);

		if (%client.isAiControlled())
			%level = " (Bot)";
		else if (%client.isSuperAdmin || %client.wasSuperAdmin)
			%level = " (SuperAdmin)";
		else if (%client.isAdmin || %client.wasAdmin)
			%level = " (Admin)";
		else if (%client.isPrivileged)
			%level = " (Privileged)";
		else
			%level = "";

		if ($PlayingOnline)
		{
			if (%client.isAiControlled())
				%id =  atfadmin_PadWithZeros(%client, 6);
			else
				%id =  atfadmin_PadWithZeros(%client.guid, 6);
		}
		else
			%id =  atfadmin_PadWithZeros(%client, 4);

		messageClient(%sender, 'MsgATFAdminCommand', '\c2%2\t%1%3', %client.name, %id, %level);
	}
}

function atfadmin_CommandBottomPrint(%sender, %text)
{
	if (!%sender.isAdmin)
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2You must be an admin to use this command.');
		return;
	}

	if (!%sender.isSuperAdmin && !$atfadmin::AllowAdminBottomPrint)
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2This command is disabled for your admin level.');
		return;
	}

	if (%text $= "")
		messageClient(%sender, 'MsgATFAdminCommand', '\c2.bp usage: .bp <text>');
	else
	{
		%name = atfadmin_GetPlainText(%sender.name);
		bottomPrintAll(%name@": "@%text, 5, 1);
		logEcho(%sender.nameBase@" bottomprinted: "@%text);
	}
}

function atfadmin_CommandConsolePrint(%sender, %text)
{
	if (!%sender.isSuperAdmin && !$atfadmin::AllowConsolePrint)
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2This command is currently disabled.');
		return;
	}

	if (%text $= "")
		messageClient(%sender, 'MsgATFAdminCommand', '\c2.cc usage: .cc <text>');
	else
	{
		%name = atfadmin_GetPlainText(%sender.name);
		echo("CONSOLE: "@%name@"(cl "@%sender@"): "@%text);
		for (%i = 0; %i < ClientGroup.getCount(); %i++)
		{
			%client = ClientGroup.getObject(%i);
			if (%client.isSuperAdmin || %client == %sender)
				messageClient(%client, 'MsgATFAdminCommand', "\c2TO SA ONLY: "@%name@": \c5"@%text);
		}
	}
}

function atfadmin_CommandMakeComment(%sender, %text)
{
	if (!%sender.isSuperAdmin && !$atfadmin::AllowComments)
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2This command is currently disabled.');
		return;
	}

	if (%text $= "")
		messageClient(%sender, 'MsgATFAdminCommand', '\c2.com usage: .com <comment>');
	else
	{
		%name = atfadmin_GetPlainText(%sender.name);
		echo("COMMENT: player:"@%name@" guid:"@%sender.guid@" map:"@$MissionDisplayName@" gametype:"@$CurrentMissionType@" text:"@%text);
		messageClient(%sender, 'MsgATFAdminCommand', "\c2Your comment has been recorded and will be reviewed by a SuperAdmin.");
	}
}

function atfadmin_CommandAdminChat(%sender, %text)
{
	if (!%sender.isSuperAdmin && !$atfadmin::AllowAdminChat)
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2This command is currently disabled.');
		return;
	}

	if (%text $= "")
		messageClient(%sender, 'MsgATFAdminCommand', '\c2.ac usage: .ac <text>');
	else
	{
		%name = atfadmin_GetPlainText(%sender.name);
		echo("ADMINCHAT: "@%name@"(cl "@%sender@"): "@%text);
		messageAdmins('MsgATFAdminCommand', "\c2"@%name@": \c5"@%text);
		if (!atfadmin_hasAdmin(%sender))
			messageClient(%sender, 'MsgATFAdminCommand', "\c2"@%name@": \c5"@%text);
	}
}

function atfadmin_CommandCenterPrint(%sender, %text)
{
	if (!%sender.isAdmin)
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2You must be an admin to use this command.');
		return;
	}

	if (!%sender.isSuperAdmin && !$atfadmin::AllowAdminCenterPrint)
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2This command is disabled for your admin level.');
		return;
	}

	if (%text $= "")
		messageClient(%sender, 'MsgATFAdminCommand', '\c2.cp usage: .cp <text>');
	else
	{
		%name = atfadmin_GetPlainText(%sender.name);
		centerPrintAll(%name@": "@%text, 5, 1);
		logEcho(%sender.nameBase@" centerprinted: "@%text);
	}
}

function atfadmin_CommandAddbots(%sender, %paramList)
{
	if (!%sender.isAdmin)
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2You must be an admin to use this command.');
		return;
	}

	if (!%sender.isSuperAdmin && !$atfadmin::AllowAdminAddBots)
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2This command is disabled for your admin level.');
		return;
	}

	if (!$Host::BotsEnabled)
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2Bots are not enabled on this server.');
		return;
	}

	atfadmin_GetParams(%paramlist);

	// Only first param is required
	if (!($atfadmin_param[0] >= 0))
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2.addbots usage: .addbots <num> [minSkill] [maxSkill]');
		return;
	}

	$atfadmin_addbotsNum = $atfadmin_param[0];
	$atfadmin_addbotsMin = $atfadmin_param[1];
	$atfadmin_addbotsMax = $atfadmin_param[2];

	messageClient(%sender, 'MsgATFAdminCommand', '\c2Will add %1 bot%2 at the start of the next mission if bots are enabled.', $atfadmin_addbotsNum, $atfadmin_addbotsNum > 1 ? "s" : "");
}

function atfadmin_CommandRemovebots(%sender, %param)
{
	if (!%sender.isAdmin)
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2You must be an admin to use this command.');
		return;
	}

	if (!%sender.isSuperAdmin && !$atfadmin::AllowAdminAddBots)
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2This command is disabled for your admin level.');
		return;
	}

	// Only first param is required
	if (!(%param >= 1))
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2.removebots usage: .removebots <num> [minSkill] [maxSkill]');
		return;
	}

	%removed = 0;
	for (%i = 0; %i < ClientGroup.getCount() && %removed < %param; %i++)
	{
		%cl = ClientGroup.getObject(%i);
		if (%cl.isAIcontrolled())
		{
			%removed++;
			$HostGameBotCount--;
			%cl.drop();
			%i = 0;
		}
	}

	messageClient(%sender, 'MsgATFAdminCommand', '\c2Removed %1 bot%2.', %removed, %removed > 1 ? "s" : "");
}

function atfadmin_CommandSetTeamInfo(%sender, %paramList)
{
	if (!%sender.isAdmin)
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2You must be an admin to use this command.');
		return;
	}

	if (!%sender.isSuperAdmin && !$atfadmin::AllowAdminTeamInfo)
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2This command is disabled for your admin level.');
		return;
	}

	atfadmin_GetParams(%paramlist);

	if (!($atfadmin_param[0] >= 1 && $atfadmin_param[0] <= 6) || $atfadmin_param[1] $= "" || $atfadmin_param[2] $= "")
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2.teaminfo usage: .teaminfo <team> <name> <skin>');
		return;
	}

	%team = $atfadmin_param[0];
	%name = addTaggedString($atfadmin_param[1]);
	%skin = addTaggedString($atfadmin_param[2]);

	$atfadmin_newTeamName[%team] = %name;
	$atfadmin_newTeamSkin[%team] = %skin;
	messageAll('MsgAdminForce', '\c2%1 set team %2 name to %3 and skin to %4; changes take place next mission.', %sender.name, %team, %name, %skin);
}

function atfadmin_ParseCommand(%sender, %text)
{
	if ($atfadmin::DisableATFAdminForTournaments && $Host::TournamentMode) return false;

	// Player commands

	if (strstr(%text, ".help") == 0)
	{
		atfadmin_CommandHelp(%sender);
		return true;
	}

	if (strstr(%text, ".version") == 0)
	{
		atfadmin_CommandVersion(%sender);
		return true;
	}

	if (strstr(%text, ".motd") == 0)
	{
		atfadmin_CommandMOTD(%sender);
		return true;
	}

	if (strstr(%text, ".observe") == 0)
	{
		atfadmin_CommandObservePlayer(%sender, getSubStr(%text, 9, 256));
		return true;
	}

	if (strstr(%text, ".ac") == 0)
	{
		atfadmin_CommandAdminChat(%sender, getSubStr(%text, 4, 256));
		return true;
	}

	// Admin commands
	// Parse command even if not admin in case user forgot to admin up
	// So check for admin is in the atfadmin_Command functions

	if (strstr(%text, ".bp") == 0)
	{
		atfadmin_CommandBottomPrint(%sender, getSubStr(%text, 4, 256));
		return true;
	}

	if (strstr(%text, ".cc") == 0)
	{
		atfadmin_CommandConsolePrint(%sender, getSubStr(%text, 4, 256));
		return true;
	}

	if (strstr(%text, ".com") == 0)
	{
		atfadmin_CommandMakeComment(%sender, getSubStr(%text, 5, 256));
		return true;
	}

	if (strstr(%text, ".cp") == 0)
	{
		atfadmin_CommandCenterPrint(%sender, getSubStr(%text, 4, 256));
		return true;
	}

	if (strstr(%text, ".addbots") == 0)
	{
		atfadmin_CommandAddbots(%sender, getSubStr(%text, 9, 256));
		return true;
	}

	if (strstr(%text, ".removebots") == 0)
	{
		atfadmin_CommandRemovebots(%sender, getSubStr(%text, 12, 256));
		return true;
	}

	if (strstr(%text, ".smite") == 0)
	{
		atfadmin_CommandSmitePlayer(%sender, getSubStr(%text, 7, 256));
		return true;
	}

	if (strstr(%text, ".blowup") == 0)
	{
		atfadmin_CommandBlowupPlayer(%sender, getSubStr(%text, 8, 256));
		return true;
	}

	if (strstr(%text, ".cancelvote") == 0)
	{
		atfadmin_CommandCancelVote(Game, %sender);
		return true;
	}

	if (strstr(%text, ".passvote") == 0)
	{
		atfadmin_CommandPassVote(Game, %sender);
		return true;
	}

	if (strstr(%text, ".teaminfo") == 0)
	{
		atfadmin_CommandSetTeamInfo(%sender, getSubStr(%text, 10, 256));
		return true;
	}

	if (strstr(%text, ".listplayers") == 0)
	{
		atfadmin_CommandListPlayers(%sender);
		return true;
	}

	if (strstr(%text, ".warn") == 0)
	{
		atfadmin_CommandWarnPlayer(%sender, getSubStr(%text, 6, 256));
		return true;
	}

	if (strstr(%text, ".kick") == 0)
	{
		atfadmin_CommandKickPlayer(%sender, getSubStr(%text, 6, 256));
		return true;
	}

	if (strstr(%text, ".ban") == 0)
	{
		atfadmin_CommandBanPlayer(%sender, getSubStr(%text, 5, 256));
		return true;
	}

	if (strstr(%text, ".mute") == 0)
	{
		atfadmin_CommandGlobalMutePlayer(%sender, getSubStr(%text, 6, 256));
		return true;
	}

	if (strstr(%text, ".unmute") == 0)
	{
		atfadmin_CommandGlobalUnMutePlayer(%sender, getSubStr(%text, 8, 256));
		return true;
	}

	if (strstr(%text, ".shownext") == 0)
	{
		atfadmin_CommandShowNext(%sender);
		return true;
	}

	if (strstr(%text, ".changemission") == 0)
	{
		atfadmin_CommandChangeMission(%sender, getSubStr(%text, 15, 256));
		return true;
	}

	if (strstr(%text, ".mode") == 0)
	{
		atfadmin_CommandChangeMode(%sender, getSubStr(%text, 6, 256));
		return true;
	}

	if (strstr(%text, ".rule") == 0)
	{
		atfadmin_CommandShowRules(%sender, %text);
		return true;
	}

	if (strstr(%text, ".time") == 0)
	{
		atfadmin_CommandShowTime(%sender);
		return true;
	}

	return false;
}

function atfadmin_CommandShowRules(%sender, %text)
{
	if (!$atfadmin::Rule[Count])
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2There are no server rules defined.');
		return;
	}

	switch$ (getWord(%text, 0))
	{
		case ".rule":
			switch (getWordCount(%text))
			{
				case 1:
					messageClient(%sender, 'MsgATFAdminCommand', '\c2You must specify a rule number.');
					return;

				case 2:
					%rule = getWord(%text, 1);
					if (%rule > $atfadmin::Rule[Count] || %rule < 1)
					{
						messageClient(%sender, 'MsgATFAdminCommand', '\c2Rule number must be between 1 and %1, inclusive.', $atfadmin::Rule[Count]);
						return;
					}
					centerPrint(%sender, "Rule "@ %rule @": "@$atfadmin::Rule[%rule - 1], 12, 3);
					messageClient(%sender, 'MsgATFAdminCommand', '~wfx/misc/red_alert_short.wav');

				default:
					if (!%sender.isAdmin)
					{
						messageClient(%sender, 'MsgATFAdminCommand', '\c2.rule usage: .rule <number>');
						return;
					}

					%rule = getWord(%text, 1);
					if (%rule > $atfadmin::Rule[Count] || %rule < 1)
					{
						messageClient(%sender, 'MsgATFAdminCommand', '\c2Rule number must be between 1 and %1, inclusive.', $atfadmin::Rule[Count]);
						return;
					}

					// get the contents of the third "field"
					%temp = getWord(%text, 0) SPC getWord(%text, 1);
					%text = strreplace(%text, "  ", " ");

					%field = getSubStr(%text, strlen(%temp) + 1, 256);
					%target = atfadmin_FindTarget(%field);
					if (%target == -1 && %field !$= "all")
					{
						messageClient(%sender, 'MsgATFAdminCommand', '\c2.rule usage: .rule <number> <id|name>');
						return;
					}

					if (%field $= "all")
					{
						centerPrintAll("Rule "@ %rule @": "@$atfadmin::Rule[%rule - 1], 12, 3);
						messageAll('MsgATFAdminCommand', '%1 has shown you rule %2.~wfx/misc/red_alert_short.wav', %sender.name, %rule);
						messageClient(%sender, 'MsgATFAdminCommand', '\c2Rule %1 has been shown to everyone.', %rule);
					}
					else
					{
						centerPrint(%target, "Rule "@ %rule @": "@$atfadmin::Rule[%rule - 1], 12, 3);
						messageClient(%target, 'MsgATFAdminCommand', '~wfx/misc/red_alert_short.wav');
						messageClient(%sender, 'MsgATFAdminCommand', '\c2Rule %1 has been shown to %2.', %rule, %target.nameBase);
					}
			}

		case ".rules":
			messageClient(%sender, 'MsgATFAdminCommand', '\c2Server rules:');
			for (%rule = 0; %rule < $atfadmin::Rule[Count]; %rule++)
				messageClient(%sender, 'MsgATFAdminCommand', '\c2%1. %2', %rule + 1, $atfadmin::Rule[%rule]);
	}
}

function atfadmin_CommandSmitePlayer(%sender, %param)
{
	if (!%sender.isAdmin)
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2You must be an admin to use this command.');
		return;
	}

	if (!%sender.isSuperAdmin && !$atfadmin::AllowAdminLightningStrike)
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2This command is disabled for your admin level.');
		return;
	}

	%target = atfadmin_FindTarget(%param);
	if (%target == -1)
		messageClient(%sender, 'MsgATFAdminCommand', '\c2.smite usage: .smite <id|name>');
	else
		serverCmdLightningStrike(%sender, %target);
}

function atfadmin_CommandBlowupPlayer(%sender, %param)
{
	if (!%sender.isAdmin)
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2You must be an admin to use this command.');
		return;
	}

	if (!%sender.isSuperAdmin && !$atfadmin::AllowAdminBlowupPlayers)
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2This command is disabled for your admin level.');
		return;
	}

	%target = atfadmin_FindTarget(%param);
	if (%target == -1)
		messageClient(%sender, 'MsgATFAdminCommand', '\c2.blowup usage: .blowup <id|name>');
	else
		serverCmdBlowupPlayer(%sender, %target);
}

function atfadmin_CommandCancelVote(%game, %sender)
{
	if (!%sender.isAdmin)
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2You must be an admin to use this command.');
		return;
	}

	if (!%sender.isSuperAdmin && !$atfadmin::AllowAdminCancelVote)
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2This command is disabled for your admin level.');
		return;
	}

	if (%game.scheduleVote !$= "")
	{
		logEcho(%sender.nameBase SPC "cancelled a vote");
		DefaultGame::cancelVote(%game, %sender);
	}
	else
		messageClient(%sender, 'MsgATFAdminCommand', '\c2There is no vote in progress.');
}

function atfadmin_CommandPassVote(%game, %sender)
{
	if (!%sender.isAdmin)
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2You must be an admin to use this command.');
		return;
	}

	if (!%sender.isSuperAdmin && !$atfadmin::AllowAdminPassVote)
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2This command is disabled for your admin level.');
		return;
	}

	if (%game.scheduleVote !$= "")
	{
		logEcho(%sender.nameBase SPC "passed a vote");
		DefaultGame::passVote(%game, %sender);
	}
	else
		messageClient(%sender, 'MsgATFAdminCommand', '\c2There is no vote in progress.');
}

function atfadmin_CommandGlobalMutePlayer(%sender, %param)
{
	if (!%sender.isAdmin)
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2You must be an admin to use this command.');
		return;
	}

	if (!%sender.isSuperAdmin && !$atfadmin::AllowAdminGlobalMutePlayers)
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2This command is disabled for your admin level.');
		return;
	}

	%target = atfadmin_FindTarget(%param);
	if (%target == -1)
		messageClient(%sender, 'MsgATFAdminCommand', '\c2.mute usage: .mute <id|name>');
	else
		serverCmdGlobalMutePlayer(%sender, %target, true);
}

function atfadmin_CommandGlobalUnMutePlayer(%sender, %param)
{
	if (!%sender.isAdmin)
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2You must be an admin to use this command.');
		return;
	}

	if (!%sender.isSuperAdmin && !$atfadmin::AllowAdminGlobalMutePlayers)
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2This command is disabled for your admin level.');
		return;
	}

	%target = atfadmin_FindTarget(%param);
	if (%target == -1)
		messageClient(%sender, 'MsgATFAdminCommand', '\c2.unmute usage: .unmute <id|name>');
	else
		serverCmdGlobalMutePlayer(%sender, %target, false);
}


};
// END package atfadmin_chatConsole
