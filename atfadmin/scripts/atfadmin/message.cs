// START package atfadmin_message
package atfadmin_message
{


function chatMessageTeam(%sender, %team, %msgString, %a1, %a2, %a3, %a4, %a5, %a6, %a7, %a8, %a9, %a10)
{
	if (%sender.globalMute)
	{
		messageClient(%sender, 'MsgATFAdminCommand', "\c2No one can hear you scream.");
		return;
	}

	if (%msgString $= "" ) return;

	if (strstr(%a2, ".") == 0)
		if (atfadmin_ParseCommand(%sender, %a2))
			return;

	if (spamAlert(%sender)) return;

	// block voice packs
	if (!$Host::TournamentMode && !$atfadmin::AllowVoicePacks && strstr(%a2,"~w") >= 0)
	{
		messageClient(%sender, "", '\c3VOICE PACK BLOCK:\cr Voice packs have been disabled on this server.');
		return;
	}
	else if (%sender.vpblock && strstr(%a2,"~w") >= 0)
	{
		messageClient(%sender, "", '\c3PERSONAL VOICE PACK BLOCK:\cr You do not have the right to use voice packs.');
		return;
	}

	%count = ClientGroup.getCount();
	for (%i = 0; %i < %count; %i++)
	{
		%obj = ClientGroup.getObject(%i);
		if (%obj.team == %sender.team)
			chatMessageClient(%obj, %sender, %sender.voiceTag, %sender.voicePitch, %msgString, %a1, %a2, %a3, %a4, %a5, %a6, %a7, %a8, %a9, %a10);
	}

	if ($atfadmin::ConsoleChat)
	{
		%name = atfadmin_GetPlainText(%sender.name);
		%playerteam = atfadmin_GetPlainText($TeamName[%team]);
		echo("TEAMCHAT: (", %playerteam, ":", %name, ":", %sender, "): ", %a2);
	}
}

function chatMessageAll(%sender, %msgString, %a1, %a2, %a3, %a4, %a5, %a6, %a7, %a8, %a9, %a10)
{
	if (%sender.globalMute)
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2No one can hear you scream.');
		return;
	}

	if (%msgString $= "" ) return;

	if (strstr(%a2, ".") == 0)
		if (atfadmin_ParseCommand(%sender, %a2))
			return;

	if (spamAlert(%sender)) return;

	// block voice packs
	if (!$Host::TournamentMode && !$atfadmin::AllowVoicePacks && strstr(%a2,"~w") >= 0)
	{
		messageClient(%sender, "", '\c3VOICE PACK BLOCK:\cr Voice packs have been disabled on this server.');
		return;
	}
	else if (%sender.vpblock && strstr(%a2,"~w") >= 0)
	{
		messageClient(%sender, "", '\c3PERSONAL VOICE PACK BLOCK:\cr You do not have the right to use voice packs.');
		return;
	}

	%count = ClientGroup.getCount();
	for ( %i = 0; %i < %count; %i++ )
	{
		%obj = ClientGroup.getObject( %i );
		if(%sender.team != 0)
			chatMessageClient( %obj, %sender, %sender.voiceTag, %sender.voicePitch, %msgString, %a1, %a2, %a3, %a4, %a5, %a6, %a7, %a8, %a9, %a10 );
		else
		{
			// message sender is an observer -- only send message to other observers
			if ($atfadmin::AllowObserverChat > 1 || %obj.team == %sender.team || ($atfadmin::AllowObserverChat > 0 && %sender.isAdmin))
				chatMessageClient( %obj, %sender, %sender.voiceTag, %sender.voicePitch, %msgString, %a1, %a2, %a3, %a4, %a5, %a6, %a7, %a8, %a9, %a10 );
		}
	}

	if ($atfadmin::ConsoleChat)
	{
		%name = atfadmin_GetPlainText(%sender.name);
		echo(%name, "(", %sender, "): ", %a2);
	}
}

function cannedChatMessageAll(%sender, %msgString, %name, %string, %keys)
{
	if (%sender.globalMute)
	{
		messageClient(%sender, 'MsgATFAdminCommand', '\c2No one can hear you scream.');
		return;
	}

	if ((%msgString $= "") || spamAlert(%sender))
		return;

	%count = ClientGroup.getCount();
	for (%i = 0; %i < %count; %i++)
		cannedChatMessageClient(ClientGroup.getObject(%i), %sender, %msgString, %name, %string, %keys);

	if ($atfadmin::ConsoleChat)
	{
		%name = atfadmin_GetPlainText(%sender.name);
		%text = getSubStr(%string, 0, strstr(%string, "~w"));
		echo(%name, "(", %sender, "): ", %text);
	}
}

function cannedChatMessageTeam(%sender, %team, %msgString, %name, %string, %keys)
{
	if (%sender.globalMute)
	{
		messageClient(%sender, 'MsgATFAdminCommand', "\c2No one can hear you scream.");
		return;
	}

	if ((%msgString $= "" ) || spamAlert(%sender))
		return;

	// block voice packs
	if (!$Host::TournamentMode && !$atfadmin::AllowVoicePacks && strstr(%a2,"~w") >= 0)
	{
		messageClient(%sender, "", '\c3VOICE PACK BLOCK:\cr Voice packs have been disabled on this server.');
		return;
	}
	else if (%sender.vpblock && strstr(%a2,"~w") >= 0)
	{
		messageClient(%sender, "", '\c3PERSONAL VOICE PACK BLOCK:\cr You do not have the right to use voice packs.');
		return;
	}

	%count = ClientGroup.getCount();
	for (%i = 0; %i < %count; %i++)
	{
		%obj = ClientGroup.getObject(%i);
		if (%obj.team == %sender.team)
			cannedChatMessageClient(%obj, %sender, %msgString, %name, %string, %keys);
	}

	if ($atfadmin::ConsoleChat)
	{
		%name = atfadmin_GetPlainText(%sender.name);
		%playerteam = atfadmin_GetPlainText($TeamName[%team]);
		%text = getSubStr(%string, 0, strstr(%string, "~w"));
		echo("TEAMCHAT: (", %playerteam, ":", %name, ":", %sender, "): ", %text);
	}
}

function messageAdmins(%msgType, %msgString, %a1, %a2, %a3, %a4, %a5, %a6, %a7, %a8, %a9, %a10, %a11, %a12, %a13)
{
	for (%i = 0; %i < ClientGroup.getCount(); %i++)
	{
		%client = ClientGroup.getObject(%i);
		if (%client.isAdmin || %client.wasAdmin)
			messageClient(%client, %msgType, %msgString, %a1, %a2, %a3, %a4, %a5, %a6, %a7, %a8, %a9, %a10, %a11, %a12, %a13);
	}
}

function messageAdminsExcept(%admin, %msgType, %msgString, %a1, %a2, %a3, %a4, %a5, %a6, %a7, %a8, %a9, %a10, %a11, %a12, %a13)
{
	for (%i = 0; %i < ClientGroup.getCount(); %i++)
	{
		%client = ClientGroup.getObject(%i);
		if ((%client.isAdmin || %client.wasAdmin) && %client != %admin)
			messageClient(%client, %msgType, %msgString, %a1, %a2, %a3, %a4, %a5, %a6, %a7, %a8, %a9, %a10, %a11, %a12, %a13);
	}
}

function messageTeamExceptAdmins(%team, %msgType, %msgString, %a1, %a2, %a3, %a4, %a5, %a6, %a7, %a8, %a9, %a10, %a11, %a12, %a13)
{
	for (%i = 0; %i < ClientGroup.getCount(); %i++)
	{
		%client = ClientGroup.getObject(%i);
		%hasAdmin = (%client.isAdmin || %client.wasAdmin);
		if ((%client.team == %team) && !%hasAdmin)
			messageClient(%client, %msgType, %msgString, %a1, %a2, %a3, %a4, %a5, %a6, %a7, %a8, %a9, %a10, %a11, %a12, %a13);
	}
}

function bottomPrintTeamExceptAdmins(%team, %message, %time, %lines)
{
	if (%lines $= "" || ((%lines > 3) || (%lines < 1)))
		%lines = 1;

	for (%i = 0; %i < ClientGroup.getCount(); %i++)
	{
		%client = ClientGroup.getObject(%i);
		%hasAdmin = (%client.isAdmin || %client.wasAdmin);
		if ((%client.team == %team) && !%hasAdmin)
			commandToClient(%client, 'BottomPrint', %message, %time, %lines);
	}
}

function bottomPrintTeam(%team, %message, %time, %lines)
{
	if (%lines $= "" || ((%lines > 3) || (%lines < 1)))
		%lines = 1;

	for (%i = 0; %i < ClientGroup.getCount(); %i++)
	{
		%client = ClientGroup.getObject(%i);
		if (%client.team == %team)
			commandToClient(%client, 'BottomPrint', %message, %time, %lines);
	}
}

function bottomPrintAdmins(%message, %time, %lines)
{
	if (%lines $= "" || ((%lines > 3) || (%lines < 1)))
		%lines = 1;

	for (%i = 0; %i < ClientGroup.getCount(); %i++)
	{
		%client = ClientGroup.getObject(%i);
		if (%client.isAdmin || %client.wasAdmin)
			commandToClient(%client, 'BottomPrint', %message, %time, %lines);
	}
}


};
// END package atfadmin_message
