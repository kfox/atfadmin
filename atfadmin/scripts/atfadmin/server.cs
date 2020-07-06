// START package atfadmin_server
package atfadmin_server
{

function serverCmdClientJoinTeam(%client, %team, %admin)
{
	if ($atfadmin_parentMod $= "classic")
	{
		Parent::serverCmdClientJoinTeam(%client, %team, %admin);
		return;
	}
	
	if (%team == -1)
		%team = %client.team == 1 ? 2 : 1;

	%allowUnfairTeams = !$atfadmin::NoLlamaTeamSwaps;

	if (isObject(Game) && Game.kickClient != %client)
	{
		if (%client.team != %team && (%client.isAdmin || %allowUnfairTeams || %team == 0 || $TeamRank[%team, count] < $TeamRank[%client.team, count]))
		{
			%fromObs = %client.team == 0;

			if (%fromObs)
				clearBottomPrint(%client);

			if (%client.isAIControlled())
				Game.AIChangeTeam(%client, %team);
			else
				Game.clientChangeTeam(%client, %team, %fromObs);
		}
	}
}

function serverSetClientTeamState(%client)
{
	// set all player states prior to mission drop ready

	// create a new camera for this client
	%client.camera = new Camera()
	{
		dataBlock = Observer;
	};

	if (isObject(%client.rescheduleVote))
		cancel(%client.rescheduleVote);
	%client.canVote = true;
	%client.rescheduleVote = "";
	%client.observerModeRespawnTime = 0;
	%client.waitRespawn = 0;

	MissionCleanup.add(%client.camera); // we get automatic cleanup this way

	%observer = false;

	if (!$Host::TournamentMode && !$Host::PickupMode)
	{
		if (%client.justConnected)
		{
			%client.justConnected = false;
			%client.camera.getDataBlock().setMode(%client.camera, "justJoined");
		}
		else
		{
			// Check what the client's team was for the last match
			if(%client.lastTeam !$= "")
			{
				if(%client.lastTeam == 0)
				{
					// This client was an observer from the last match
					// Let them stay observer

					%client.camera.getDataBlock().setMode(%client.camera, "ObserverFly");
					%observer = true;
				}
				else
				{
					// Try to put this client on the team they were on last match

					if (Game.numTeams > 1 && %client.lastTeam <= Game.numTeams)
					{
						if ($atfadmin::RandomTeams && Game.numTeams > 1)
						{
							Game.assignRandomTeam(%client, false);
							Game.spawnPlayer(%client, false);
						}
						else
							Game.clientJoinTeam(%client, %client.lastTeam, false);
					}
					else
					{
						// Client's team from the last match doesn't exist now
						// Give them a new team

						Game.assignClientTeam(%client);
						Game.spawnPlayer(%client, false);
					}
				}
			}
			else
			{
				Game.assignClientTeam(%client);
				Game.spawnPlayer(%client, false);
			}

			if (!%observer)
			{
				if (!$MatchStarted && !$CountdownStarted)
					%client.camera.getDataBlock().setMode(%client.camera, "pre-game", %client.player);
				else if (!$MatchStarted && $CountdownStarted)
					%client.camera.getDataBlock().setMode(%client.camera, "pre-game", %client.player);
			}
		}
	}
	else
	{
		// don't need to do anything. MissionDrop will handle things from here.
	}
}

function serverCmdChangePlayersTeam(%clientRequesting, %client, %team)
{
	if (isObject(Game) && %client != Game.kickClient && %clientRequesting.isAdmin)
	{
		serverCmdClientJoinTeam(%client, %team, %clientRequesting);

		if (!$MatchStarted)
		{
			%client.observerMode = "pregame";
			%client.camera.getDataBlock().setMode(%client.camera, "pre-game", %client.player);
			%client.setControlObject(%client.camera);

			if (($Host::TournamentMode || $Host::PickupMode) && !$CountdownStarted && !%game.secondHalfCountDown)
			{
				%client.notReady = true;
				centerprint(%client, "\nPress FIRE when ready.", 0, 3);
			}
		}
		else
			commandToClient(%client, 'setHudMode', 'Standard', "", 0);

		%multiTeam = (Game.numTeams > 1);

		if (%multiTeam)
		{
			messageClient(%client, 'MsgClient', '\c1%1 has changed your team.', %clientRequesting.name);
			messageAllExcept(%client, -1, 'MsgClient', '\c1%1 forced %2 to join the %3 team.', %clientRequesting.name, %client.name, $teamName[%client.team]);
		}
		else
		{
			messageClient(%client, 'MsgClient', '\c1%1 has added you to the game.', %clientRequesting.name);
			messageAllExcept(%client, -1, 'MsgClient', '\c1%1 added %2 to the game.', %clientRequesting.name, %client.name);
		}
	}
}

function serverCmdClientAddToGame(%client, %targetClient)
{
	if (isObject(Game))
		Game.clientJoinTeam(%targetClient, 0, $MatchStarted);

	clearBottomPrint(%targetClient);

	if ($matchstarted)
	{
		%targetClient.setControlObject(%targetClient.player);
		commandToClient(%targetClient, 'setHudMode', 'Standard');
	}
	else
	{
		%targetClient.notReady = true;
		%targetClient.camera.getDataBlock().setMode(%targetClient.camera, "pre-game", %targetClient.player);
		%targetClient.setControlObject(%targetClient.camera);
	}

	if (($Host::TournamentMode || $Host::PickupMode) && !$CountdownStarted)
	{
		%targetClient.notReady = true;
		centerprint(%targetClient, "\nPress FIRE when ready.", 0, 3);
	}
}

function CreateServer(%mission, %missionType)
{
	$atfadmin_defaultTimeLimit = $Host::TimeLimit;
	Parent::CreateServer(%mission, %missionType);
}

function DestroyServer()
{
	$missionRunning = false;
	allowConnections(false);
	stopHeartbeat();
	if ( isObject( MissionGroup ) )
		MissionGroup.delete();
	if ( isObject( MissionCleanup ) )
		MissionCleanup.delete();
	if(isObject(Game))
	{
		Game.deactivatePackages();
		game.delete();
	}
	if(isObject($ServerGroup))
		$ServerGroup.delete();

	// delete all the connections:
	while(ClientGroup.getCount())
	{
		%client = ClientGroup.getObject(0);
		if (%client.isAIControlled())
			%client.drop();
		else
			%client.delete();
	}

	// delete all the data blocks...
	// this will cause problems if there are any connections
	deleteDataBlocks();

	// reset the target manager
	resetTargetManager();

	if ($atfadmin::ExportServerPrefs)
	{
		echo("exporting server prefs...");
		if (isFile("prefs/ServerPrefs.cs.dso"))
			deleteFile("prefs/ServerPrefs.cs.dso");
		export("$Host::*", "prefs/ServerPrefs.cs", false);
	}

	if ($atfadmin::ExportATFAdminPrefs)
		ATFAdminGame::ExportPrefs();

	purgeResources();

	// TR2
	// This is a failsafe way of ensuring that default gravity is always restored
	// if a game type (such as TR2) changes it.  It is placed here so that listen
	// servers will work after opening and closing different gametypes.
	if ($DefaultGravity !$= "")
		setGravity($DefaultGravity);
}

function serverCmdATFAdminRegister(%client, %version)
{
	%latestVersion = "2.3.0";

	// check and save the client version
	if (!%client.atfclient)
	{
		if (%version $= "")
		{
			logEcho("atfclient: no version reported by client" SPC %client);
		}
		else if (%version !$= %latestVersion)
		{
			// inform the client that their atfclient is outdated
			messageClient(%client, 'MsgATFAdminRegister', '\c2Your atfadmin client (version %1) is obsolete. Please obtain version %2 from http://www.the-pond.net/~wfx/misc/warning_beep.wav', %version, %latestVersion);
			logEcho("atfclient: client" SPC %client SPC "attempted to register with an old version ("@%version@")");
		}
		else
		{
			// note that this client has the current atfclient installed
			%client.atfclient = true;
			%client.atfclientVersion = %version;

			// inform the client that they have successfully registered
			messageClient(%client, 'MsgATFAdminRegister', '\c2Your atfadmin client (version %1) has been successfully registered.', %version);
			logEcho("atfclient: client" SPC %client SPC "has successfully registered");
		}
	}
}

function serverCmdWarnPlayer(%client, %target)
{
	if (%client.isSuperAdmin || (%client.isAdmin && $atfadmin::AllowAdminKickBan))
	{
		if (atfadmin_OutrankTarget(%client, %target))
		{
			messageAll('MsgAdminForce', '\c2%1 has warned %2 for inappropriate behavior.', %client.name, %target.name);
			centerprint(%target, "You are being warned by" SPC atfadmin_GetPlainText(%client.name) SPC "for inappropriate behavior.\nBehave or you will be kicked.", 10, 2);
			logEcho(%target.nameBase SPC "warned for inappropriate behavior by" SPC %client.nameBase);
		}
		else
			messageClient(%client, 'MsgATFAdminCommand', '\c2Cannot warn %1 as you do not outrank %2.', %target.name, %target.sex $= "Male" ? 'him' : 'her');
	}
}

function serverCmdMissionStartPhase3Done(%client, %seq)
{
	parent::serverCmdMissionStartPhase3Done(%client, %seq);

	if (!$Host::TournamentMode && !%client.motd)
	{
		if ($atfadmin::MOTD !$= "")
			centerprint(%client, $atfadmin::MOTD, 8, $atfadmin::MOTDLines);
		messageClient(%client, 'MsgATFAdminCommand', '\c2This server is running atfadmin %1, available from http://www.the-pond.net/', $atfadmin_Version);
		%client.motd = true;
	}
}

function atfadmin_AddNewBots(%missionName, %num, %minSkill, %maxSkill)
{
	// Add new bots
	if ($Host::botsEnabled && %num >= 1)
	{
		%missionID = atfadmin_getMissionIndexFromFileName(%missionName);

		if ($MissionList[%missionID, BotEnabled])
		{
			%minSkill = (%minSkill $= "") ? $Host::MinBotDifficulty : %minSkill;
			%maxSkill = (%maxSkill $= "") ? $Host::MaxBotDifficulty : %maxSkill;

			%numClients = ClientGroup.getCount();
			%botCount   = atfadmin_BotNum();

			if (%num + %botCount + %numClients > $Host::MaxPlayers)
				%num = $Host::MaxPlayers - (%botCount + %numClients);

			if (%num + %botCount> $atfadmin::BotsMax) %num = $atfadmin::BotsMax - %botCount;

			if (%num > 0)
			{
				AISystemEnabled(false);

				$AITimeSliceReassess = 0;
				aiConnectMultiple(%num, %minSkill, %maxSkill, -1);
				$HostGameBotCount += %num;
			}
		}
	}
}

function loadMission(%missionName, %missionType, %firstMission)
{
	// set new team names/skins
	for (%i = 0; %i < 7; %i++)
	{
		if ($atfadmin_newTeamName[%i] !$= "")
			$teamName[%i] = $atfadmin_newTeamName[%i];
		if ($atfadmin_newTeamSkin[%i] !$= "")
			$teamSkin[%i] = $atfadmin_newTeamSkin[%i];
		$atfadmin_newTeamName[%i] = $atfadmin_newTeamSkin[%i] = "";
	}

	if ($atfadmin_ResetTimeLimit)
	{
		$Host::TimeLimit = $atfadmin_defaultTimeLimit;
		messageAll('MsgATFAdminCommand', '\c2The mission time limit has been reset to %1 minutes.', $Host::TimeLimit);
		logEcho("time limit set to" SPC $Host::TimeLimit SPC "(reset)");
		$atfadmin_ResetTimeLimit = false;
	}

	if ($AutoRestart) // z0dd - ZOD, 3/26/02. Auto restart server after a specified time.
	{
		$AutoRestart = 0;
		messageAll('MsgServerRestart', '\c2SERVER IS AUTO REBOOTING! COME BACK IN 5 MINUTES.~wfx/misc/red_alert.wav');
		logEcho("Auto server restart commencing.");
		schedule(10000, 0, quit);
	}

	if (%missionType $= "TR2")
	{
		$_Camera::movementSpeed = $Camera::movementSpeed;
		$Camera::movementSpeed = 80;
	}
	else
	{
		%val = ($_Camera::movementSpeed $= "") ? $Classic::cameraSpeed : $_Camera::movementSpeed;
		$Camera::movementSpeed = %val;
	}

	$LoadingMission = true;
	disableCyclingConnections(true);
	if (!$pref::NoClearConsole)
		cls();
	if (isObject(LoadingGui))
		LoadingGui.gotLoadInfo = "";
	buildLoadInfo(%missionName, %missionType);

	// reset all of these
	ClearCenterPrintAll();
	ClearBottomPrintAll();

	if ($Host::TournamentMode || $Host::PickupMode)
		resetTournamentPlayers();

	// send load info to all the connected clients
	%count = ClientGroup.getCount();
	for (%cl = 0; %cl < %count; %cl++)
	{
		%client = ClientGroup.getObject(%cl);
		if (!%client.isAIControlled())
			sendLoadInfoToClient(%client);
	}

	// allow load condition to exit out
	schedule(0, ServerGroup, loadMissionStage1, %missionName, %missionType, %firstMission);
}

function serverCmdSADSetPassword(%client, %password)
{
	if (!%client.isAdmin)
	{
		messageClient(%client, 'MsgATFAdminCommand', '\c2You must be an admin to use this command.');
		return;
	}

	if (!%client.isSuperAdmin && !$atfadmin::AllowAdminSetPassword)
	{
		messageClient(%client, 'MsgATFAdminCommand', '\c2This command is disabled for your admin level.');
		return;
	}

	$Host::Password = %password;
	if (%password $= "")
	{
		messageAdmins('MsgAdminForce', '\c2Server password removed by %1.', %client.nameBase);
		logEcho("Server password removed by "@%client.nameBase);
	}
	else
	{
		messageAdmins('MsgAdminForce', '\c2Server password set to "%1" by %2.', %password, %client.nameBase);
		logEcho("Server password set to '"@%password@"' by "@%client.nameBase);
	}
}

function serverCmdGetMissionTypes(%client, %key)
{
	for (%type = 0; %type < $HostTypeCount; %type++)
	{
		if (%client.isSuperAdmin
		||  ((%client.isAdmin || %client.wasAdmin) && $atfadmin::AllowAdminChangeMission & 2)
		||  $atfadmin::AllowPlayerVoteChangeMission & 2
		||  $HostTypeName[%type] $= $CurrentMissionType)
		{
			messageClient(%client, 'MsgVoteItem', "", %key, %type, "", $HostTypeDisplayName[%type], true);
		}
	}
}

function serverCmdGetMissionList(%client, %key, %type)
{
	if (%type < 0 || %type >= $HostTypeCount)
		return;

	for (%i = $HostMissionCount[%type] - 1; %i >= 0; %i--)
	{
		%idx = $HostMission[%type, %i];
		%filename = $HostMissionFile[%idx];
		%typename = $HostTypeName[%type];

		// if we have bots, don't change to a mission that doesn't support bots
		if ($HostGameBotCount > 0 && !$BotEnabled[%idx])
			continue;

		// don't show this mission if its already in the mission queue
		if (atfadmin_isQueued(atfadmin_validateMission(%filename, %typename)))
			continue;

		%side = $IsClientSide[%idx] ? "C" : "S";
		%vote = $IsVoteableOnly[%idx] ? "V" : "R";

		// don't show extra info for tourneys or in practice mode
		// or if we aren't using a custom rotation
		if ($Host::TournamentMode || $CurrentMissionType $= "PracticeCTF" || !$atfadmin_customMapRotation)
			%extra = "";
		else
			%extra = %vote @ %side @ ": ";

		messageClient(%client, 'MsgVoteItem', "", %key,
			atfadmin_validateMission(%filename, %typename), // mission index, will be stored in $clVoteCmd
			"",
			%extra @ $HostMissionName[%idx],
			true);
	}
}

function atfadmin_isQueued(%missionID)
{
	if (!%totalRecords = getFieldCount($atfadmin_missionQueue))
		return false;

	for (%i = 0; %i < %totalRecords; %i++)
	{
		%index = getField(getRecord($atfadmin_missionQueue, 0), %i);
		if (%index == %missionID)
			return true;
	}

	return false;
}

function serverCmdGetQueuedMissionList(%client, %key)
{
	if (!%totalRecords = getFieldCount($atfadmin_missionQueue))
		return;

	for (%i = 0; %i < %totalRecords; %i++)
	{
		%index = getField(getRecord($atfadmin_missionQueue, 0), %i);

		%side = $MissionList[%index, ClientSide] ? "C" : "S";
		%vote = $MissionList[%index, VoteableOnly] ? "V" : "R";

		// don't show extra info for tourneys or in practice mode
		// or if we aren't using a custom rotation
		if ($Host::TournamentMode || $CurrentMissionType $= "PracticeCTF" || !$atfadmin_customMapRotation)
			%extra = "";
		else
			%extra = %vote @ %side @ ": ";

		messageClient(%client, 'MsgVoteItem', "", %key,
			%index, // mission index, will be stored in $clVoteCmd
			"",
			%extra @ $MissionList[%index, MissionDisplayName] SPC "(" @ $MissionList[%index, TypeDisplayName] @ ")",
			false);
	}
}

function atfadmin_enqueueMission(%missionId, %atStart)
{
	%count = getFieldCount($atfadmin_missionQueue);

	for (%i = 0; %i < %count; %i++)
	{
		%index = getField($atfadmin_missionQueue, %i);
		if (%index == %missionId)
			return; // already queued
	}

	if (%count == 0)
	{
		$atfadmin_missionQueue = %missionId;
		$atfadmin_nextMissionID = %missionId;
	}
	else if (%atStart)
		$atfadmin_missionQueue = %missionId TAB $atfadmin_missionQueue;
	else
		$atfadmin_missionQueue = $atfadmin_missionQueue TAB %missionId;
}

function atfadmin_dequeueMission(%missionId)
{
	%count = getFieldCount($atfadmin_missionQueue);
	if (%count <= 0) return "";

	if (%missionId $= "")
		%missionId = getField($atfadmin_missionQueue, 0);

	switch (%count)
	{
		case 1:
			%popped = $atfadmin_missionQueue;
			$atfadmin_missionQueue = "";
			$atfadmin_nextMissionID = "";

		default:
			%newList = "";
			%popped = %missionId;

			for (%i = 0; %i < %count; %i++)
			{
				%field = getField($atfadmin_missionQueue, %i);
				if (%field != %missionId)
				{
					if (%newList $= "")
					{
						%newList = %field;
						$atfadmin_nextMissionID = %field;
					}
					else
						%newList = %newList TAB %field;
				}
			}

			$atfadmin_missionQueue = %newList;
	}

	return %popped;
}

function atfadmin_isOnPermanentBanList(%client)
{
	if (!%totalRecords = getFieldCount($atfadmin::PermanentBanList))
		return false;

	// just return if there are no entries
	if (%totalRecords == 1 && getField(getRecord($atfadmin::PermanentBanList, 0), 0) $= "none")
		return false;

	for (%i = 0; %i < %totalRecords; %i++)
	{
		%record = getField(getRecord($atfadmin::PermanentBanList, 0), %i);
		if (%record == %client.guid)
			return true;
	}

	return false;
}

function atfadmin_isOnPrivilegedList(%client)
{
	if (!%totalRecords = getFieldCount($Host::PrivilegedList))
		return false;

	for (%i = 0; %i < %totalRecords; %i++)
	{
		%record = getField(getRecord($Host::PrivilegedList, 0), %i);
		if (%record == %client.guid)
			return true;
	}

	return false;
}

function atfadmin_isOnVoicePackBlockList(%client)
{
	if (!%totalRecords = getFieldCount($Host::VoicePackBlockList))
		return false;

	for (%i = 0; %i < %totalRecords; %i++)
	{
		%record = getField(getRecord($Host::VoicePackBlockList, 0), %i);
		if (%record == %client.guid)
			return true;
	}

	return false;
}

function serverCmdLightningStrike(%client, %target)
{
	if (%client.isSuperAdmin || (%client.isAdmin && $atfadmin::AllowAdminLightningStrike))
	{
		if (atfadmin_OutrankTarget(%client, %target))
		{
			messageAll('MsgAdminForce', '\c2%2 attempts to strike %1 with a lightning bolt.', %target.name, %client.name);
			centerprint(%target, "You have invoked the wrath of "@atfadmin_GetPlainText(%client.name)@"!\nClean up your act or be kicked.", 10, 2);
			atfadmin_lightningStrike(%target);
			logEcho(%client.nameBase SPC "attempts to lightning strike" SPC %target.nameBase);
		}
		else
			messageClient(%client, 'MsgATFAdminCommand', '\c2Cannot lightning strike %1 as you do not outrank %2.', %target.name, %target.sex $= "Male" ? 'him' : 'her');
	}
}

function serverCmdBlowupPlayer(%client, %target)
{
	if (%client.isSuperAdmin || (%client.isAdmin && $atfadmin::AllowAdminBlowupPlayers))
	{
		if (atfadmin_OutrankTarget(%client, %target))
		{
			messageAll('MsgAdminForce', '\c2%1 is blown to chunks by %2.~wfx/explosions/explosion_xpl03.wav', %target.name, %client.name);
			atfadmin_blowupPlayer(%target);
			logEcho(%client.nameBase SPC "blows up" SPC %target.nameBase);
		}
		else
			messageClient(%client, 'MsgATFAdminCommand', '\c2Cannot blow up %1 as you do not outrank %2.', %target.name, %target.sex $= "Male" ? 'him' : 'her');
	}
}

function atfadmin_blowupPlayer(%target)
{
	%player = %target.player;

	// return if client is dead or gone
	if (!%player)
		return;

	if (%player.getState() $= "Dead")
		return;
	
	%vec = "0 10 0";
	%player.applyImpulse(%player.position, VectorScale(%vec, %player.getDataBlock().mass*20));
	%player.blowup();
	%player.setDamageFlash(0.75);
	%player.scriptkill($DamageType::Explosion);
}

function serverCmdGlobalMutePlayer(%client, %target, %mute)
{
	if ((%client.isAdmin && $atfadmin::AllowAdminGlobalMutePlayers))
	{
		if (atfadmin_OutrankTarget(%client, %target))
		{
			if (%target.globalMute)
			{
				if (!%mute)
				{
					messageAll('MsgAdminForce', '\c2%1 has been globally unmuted by %2.', %target.name, %client.name);
					centerprint(%target, "You have been globally unmuted by "@atfadmin_GetPlainText(%client.name)@".\nYour chat will now be visible to other players.\nPlease think before you speak.", 10, 3);
					%target.globalMute = false;
					logEcho(%target.nameBase SPC "has been globally unmuted by" SPC %client.nameBase);
				}
				else
					messageClient(%client,'MsgATFAdminCommand','\c2%1 has already been globally unmuted.',%target.name);
			}
			else
			{
				if (%mute)
				{
					messageAll('MsgAdminForce', '\c2%1 has been globally muted by %2.', %target.name, %client.name);
					centerprint(%target, "You have been globally muted by "@atfadmin_GetPlainText(%client.name)@".\nNo one can hear you now.\nPerhaps you should think before you speak.", 10, 3);
					%target.globalMute = true;
					logEcho(%target.nameBase SPC "has been globally muted by" SPC %client.nameBase);
				}
				else
					messageClient(%client,'MsgATFAdminCommand','\c2%1 is already globally muted.',%target.name);
			}
		}
		else
			messageClient(%client, 'MsgATFAdminCommand', '\c2Cannot globally mute or unmute %1 as you do not outrank %2.', %target.name, %target.sex $= "Male" ? 'him' : 'her');
	}
}

function serverCmdTogglePlayerVoting(%client, %target)
{
	if (%client.isSuperAdmin || (%client.isAdmin && $atfadmin::AllowAdminDisableVoting))
	{
		if (atfadmin_OutrankTarget(%client, %target))
		{
			if (%target.votingDisabled)
			{
				messageAll('MsgAdminForce', '\c2%1 has been given the right to vote by %2.', %target.name, %client.name);
				centerprint(%target, "You have been given the right to vote by "@atfadmin_GetPlainText(%client.name)@".", 10, 3);
				%target.votingDisabled = false;
				logEcho(%target.nameBase SPC "has been given the right to vote by" SPC %client.nameBase);
			}
			else
			{
				messageAll('MsgAdminForce', '\c2%1 has been prevented from voting by %2.', %target.name, %client.name);
				centerprint(%target, "You have been prevented from voting by "@atfadmin_GetPlainText(%client.name)@".", 10, 3);
				%target.votingDisabled = true;
				logEcho(%target.nameBase SPC "has been prevented from voting by" SPC %client.nameBase);
			}
		}
		else
			messageClient(%client, 'MsgATFAdminCommand', '\c2Cannot change voting privileges for %1 as you do not outrank %2.', %target.name, %target.sex $= "Male" ? 'him' : 'her');
	}
}

function kick(%client, %admin, %guid)
{
	if(%admin)
		messageAll( 'MsgAdminForce', '\c2%2 has kicked %1.', %client.name, $AdminCl.name );
	else
		messageAll( 'MsgVotePassed', '\c2%1 was kicked by vote.', %client.name);

	messageClient(%client, 'onClientKicked', "");
	messageAllExcept( %client, -1, 'MsgClientDrop', "", %client.name, %client );

	if( %client.isAIControlled() )
	{
		$HostGameBotCount--;
		%client.drop();
	}
	else
	{
		if( $playingOnline ) // won games
		{
			%count = ClientGroup.getCount();
			%found = false;
			for( %i = 0; %i < %count; %i++ ) // see if this guy is still here...
			{
				%cl = ClientGroup.getObject( %i );
				if( %cl.guid == %guid )
				{
					%found = true;

					// kill and delete this client, their done in this server.
					if( isObject( %cl.player ) )
						%cl.player.scriptKill(0);

					if ( isObject( %cl ) )
					{
						%cl.setDisconnectReason( "You have been kicked out of the game." );
						%cl.schedule(700, "delete");
					}

					BanList::add( %guid, "0", $Host::KickBanTime );
				}
			}
			if( !%found )
				BanList::add( %guid, "0", $Host::KickBanTime ); // keep this guy out for a while since he left.
		}
		else // lan games
		{
			// kill and delete this client
			if( isObject( %client.player ) )
				%client.player.scriptKill(0);

			if ( isObject( %client ) )
			{
				%client.setDisconnectReason( "You have been kicked out of the game." );
				%client.schedule(700, "delete");
			}

			BanList::add( 0, %client.getAddress(), $Host::KickBanTime );
		}
	}
}

function ban(%client, %admin)
{
	if ( %admin )
		messageAll('MsgAdminForce', '\c2%2 has banned %1.', %client.name, $AdminCl.name);
	else
		messageAll( 'MsgVotePassed', '\c2%1 was banned by vote.', %client.name );

	messageClient(%client, 'onClientBanned', "");
	messageAllExcept( %client, -1, 'MsgClientDrop', "", %client.name, %client );

	// kill and delete this client
	if( isObject(%client.player) )
		%client.player.scriptKill(0);

	if ( isObject( %client ) )
	{
		%client.setDisconnectReason( "You have been banned from this server." );
		%client.schedule(700, "delete");
	}

	BanList::add(%client.guid, %client.getAddress(), $Host::BanTime);
}


function GameConnection::onConnect(%client, %name, %raceGender, %skin, %voice, %voicePitch)
{
	// TNHAX: lazy fix.  we must put the tribesnext code here since we can't access it otherwise
	// see t2csri\serverSide.cs in t2csri.vl2
	if( isPackage(t2csri_server) ) {

		if (%client.t2csri_serverChallenge $= "")
		{
			// check to see if the client is IP banned
			if (banList_checkIP(%client))
			{
				%client.setDisconnectReason("You are not allowed to play on this server.");
				%client.delete();
				return;
			}
	
			//echo("Client connected. Initializing pre-connection authentication phase...");
			// save these for later
			%client.tname = %name;
			%client.trgen = %raceGender;
			%client.tskin = %skin;
			%client.tvoic = %voice;
			%client.tvopi = %voicePitch;
	
			// start the 15 second count down
			%client.tterm = schedule(15000, 0, t2csri_expireClient, %client);
	
			commandToClient(%client, 't2csri_pokeClient', "T2CSRI 1.0 - 10/30/2008");
			return;
		}
		//echo("Client completed pre-authentication phase.");

		// continue connection process
		cancel(%client.tterm);

		%client.doneAuthenticating = 1;
	}
	// TNHAX: end of lazy fix
	

	// let's see if this player is undesirable...
	// get the client's unique ID
	%authInfo = %client.getAuthInfo();
	%client.guid = getField(%authInfo, 3);

	// keep this player OUT if they're permanently banned
	if (atfadmin_isOnPermanentBanList(%client))
	{
		if (isObject(%client))
		{
			%client.setDisconnectReason("You are permanently banned from this server.");
			%client.schedule(700, "delete");
		}
	}

	%client.setMissionCRC($missionCRC);
	sendLoadInfoToClient(%client);

	// if hosting this server, set this client to superAdmin
	if (%client.getAddress() $= "Local")
	{   
		%client.isAdmin = true;
		%client.isSuperAdmin = true;
		$atfadmin_adminCount++;
	}

	// Get the client's unique id:
	%authInfo = %client.getAuthInfo();
	%client.guid = getField(%authInfo, 3);

	// check admin and super admin list, and set status accordingly
	if (!%client.isSuperAdmin)
	{
		if (isOnSuperAdminList(%client))
		{   
			%client.isAdmin = true;
			%client.isSuperAdmin = true;   
			$atfadmin_adminCount++;
		}
		else if (isOnAdminList(%client))
		{
			%client.isAdmin = true;
			$atfadmin_adminCount++;
		}
		// make the user "privileged" if they are on the list
		else if (atfadmin_isOnPrivilegedList(%client))
			%client.isPrivileged = true;

		// block voice packs from the user if they are on the list
		if (atfadmin_isOnVoicePackBlockList(%client))
			%client.vpblock = true;
	}

	// Sex/Race defaults
	switch$ (%raceGender)
	{
		case "Human Male":
			%client.sex = "Male";
		%client.race = "Human";
		case "Human Female":
			%client.sex = "Female";
		%client.race = "Human";
		case "Bioderm":
			%client.sex = "Male";
		%client.race = "Bioderm";
		default:
		error("Invalid race/gender combo passed: " @ %raceGender);
		%client.sex = "Male";
		%client.race = "Human";
	}
	%client.armor = "Light";

	// Override the connect name if this server does not allow smurfs:
	%realName = getField(%authInfo, 0);
	if ($PlayingOnline && $Host::NoSmurfs)
		%name = %realName;

	if (strcmp(%name, %realName) == 0)
	{
		%client.isSmurf = false;

		// make sure the name is unique - that a smurf isn't using this name...
		%dup = -1;
		%count = ClientGroup.getCount();
		for (%i = 0; %i < %count; %i++)
		{
			%test = ClientGroup.getObject(%i);
			if (%test != %client)
			{
				%rawName = stripChars(detag(getTaggedString(%test.name)), "\cp\co\c6\c7\c8\c9");
				if (%realName $= %rawName)
				{
					%dup = %test;
					%dupName = %rawName;
					break;
				}
			}
		}

		// see if we found a duplicate name
		if (isObject(%dup))
		{
			// change the name of the dup
			%isUnique = false;
			%suffixCount = 1;
			while (!%isUnique)
			{
				%found = false;
				%testName = %dupName @ "." @ %suffixCount;
				for (%i = 0; %i < %count; %i++)
				{
					%cl = ClientGroup.getObject(%i);
					%rawName = stripChars(detag(getTaggedString(%cl.name)), "\cp\co\c6\c7\c8\c9");
					if (%rawName $= %testName)
					{
						%found = true;
						break;
					}
				}

				if (%found)
					%suffixCount++;
				else
					%isUnique = true;
			}

			// %testName will now have the new unique name...
			%oldName = %dupName;
			%newName = %testName;

			MessageAll('MsgSmurfDupName', '\c2The real \"%1\" has joined the server.', %dupName);
			MessageAll('MsgClientNameChanged', '\c2The smurf \"%1\" is now called \"%2\".', %oldName, %newName, %dup);

			%dup.name = addTaggedString(%newName);
			setTargetName(%dup.target, %dup.name);
		}

		// add the tribal tag
		%tag = getField(%authInfo, 1);
		%client.tribe = %tag;
		%append = getField(%authInfo, 2);
		if (%append)
			%name = "\cp\c6" @ %name @ "\c7" @ %tag @ "\co";
		else
			%name = "\cp\c7" @ %tag @ "\c6" @ %name @ "\co";

		%client.sendGuid = %client.guid;
	}
	else
	{
		%client.isSmurf = true;
		%client.sendGuid = 0;
		%name = stripTrailingSpaces(strToPlayerName(%name));
		if (strlen(%name) < 3)
			%name = "Poser";

		// make sure the alias is unique
		%isUnique = true;
		%count = ClientGroup.getCount();
		for (%i = 0; %i < %count; %i++)
		{
			%test = ClientGroup.getObject(%i);
			%rawName = stripChars(detag(getTaggedString(%test.name)), "\cp\co\c6\c7\c8\c9");
			if (strcmp(%name, %rawName) == 0)
			{
				%isUnique = false;
				break;
			}
		}

		// append a number to make the alias unique
		if (!%isUnique)
		{
			%suffix = 1;
			while (!%isUnique)
			{
				%nameTry = %name @ "." @ %suffix;
				%isUnique = true;

				%count = ClientGroup.getCount();
				for (%i = 0; %i < %count; %i++)
				{
					%test = ClientGroup.getObject(%i);
					%rawName = stripChars(detag(getTaggedString(%test.name)), "\cp\co\c6\c7\c8\c9");
					if (strcmp(%nameTry, %rawName) == 0)
					{
						%isUnique = false;
						break;
					}
				}

				%suffix++;
			}

			// success!
			%name = %nameTry;
		}

		%smurfName = %name;
		// tag the name with the "smurf" color
		%name = "\cp\c8" @ %name @ "\co";
	}

	%client.name = addTaggedString(%name);
	if(%client.isSmurf)
		%client.nameBase = %smurfName;
	else
		%client.nameBase = %realName;

	// Make sure that the connecting client is not trying to use a bot skin:
	%temp = detag(%skin);
	if ( %temp $= "basebot" || %temp $= "basebbot" )
		%client.skin = addTaggedString("base");
	else
		%client.skin = addTaggedString(%skin);

	%client.voice = %voice;
	%client.voiceTag = addtaggedString(%voice);

	// set the voice pitch based on a lookup table from their chosen voice
	%client.voicePitch = getValidVoicePitch(%voice, %voicePitch);

	%client.justConnected = true;
	%client.isReady = false;

	// full reset of client target manager
	clientResetTargets(%client, false);

	%client.target = allocClientTarget(%client, %client.name, %client.skin, %client.voiceTag, '_ClientConnection', 0, 0, %client.voicePitch);
	%client.score = 0;
	%client.team = 0;

	$instantGroup = ServerGroup;
	$instantGroup = MissionCleanup;

	echo("CADD: " @ %client @ " " @ %client.getAddress());
	echo("JOIN:" SPC
			"name:" @ %client.nameBase SPC
			"tag:\"" @ %client.tribe @ "\"" SPC
			"guid:" @ %client.guid SPC
			%client.getAddress());

	%count = ClientGroup.getCount();
	for (%cl = 0; %cl < %count; %cl++)
	{
		%recipient = ClientGroup.getObject(%cl);

		if ((%recipient != %client))
		{
			// These should be "silent" versions of these messages...
			messageClient(%client, 'MsgClientJoin', "", 
					%recipient.name, 
					%recipient, 
					%recipient.target, 
					%recipient.isAIControlled(), 
					%recipient.isAdmin, 
					%recipient.isSuperAdmin, 
					%recipient.isSmurf, 
					%recipient.sendGuid,
					%recipient.isPrivileged,
					%recipient.isCaptain);

			messageClient(%client, 'MsgClientJoinTeam', "", %recipient.name, $teamName[%recipient.team], %recipient, %recipient.team);
		}
	}

	commandToClient(%client, 'setBeaconNames', "Target Beacon", "Marker Beacon", "Bomb Target");

	if ($CurrentMissionType !$= "SinglePlayer") 
	{
		messageClient(%client, 'MsgClientJoin', '\c2Welcome to Tribes2 %1.', 
				%client.name, 
				%client, 
				%client.target, 
				false,   // isBot 
				%client.isAdmin, 
				%client.isSuperAdmin, 
				%client.isSmurf, 
				%client.sendGuid,
				%client.isPrivileged,
				%client.isCaptain);

		for (%cl = 0; %cl < ClientGroup.getCount(); %cl++)
		{
			%recipient = ClientGroup.getObject(%cl);

			// tribe member joined
			if ($atfadmin::TribeMemberAnnounce && %client.tribe !$= "" && %client.tribe $= %recipient.tribe && %client != %recipient)
				messageClient(%recipient, 'MsgClientJoin', '\c2Tribe member %1 has joined the game.~wfx/Bonuses/Nouns/special1.wav',
						%client.name,
						%client,
						%client.target,
						false,   // isBot
						%client.isAdmin,
						%client.isSuperAdmin,
						%client.isSmurf,
						%client.sendGuid,
						%client.isPrivileged,
						%client.isCaptain);
			else if (%client != %recipient)
				messageClient(%recipient, 'MsgClientJoin', '\c1%1 joined the game.', 
						%client.name, 
						%client, 
						%client.target, 
						false,   // isBot 
						%client.isAdmin, 
						%client.isSuperAdmin, 
						%client.isSmurf,
						%client.sendGuid,
						%client.isPrivileged,
						%client.isCaptain);
		}
	}
	else
		messageClient(%client, 'MsgClientJoin', "\c0Mission Insertion complete...", 
				%client.name, 
				%client, 
				%client.target, 
				false,	// isBot 
				false,	// isAdmin 
				false,	// isSuperAdmin 
				false,	// isSmurf
				%client.sendGuid,
				false,	// isPrivileged
				false	// isCaptain
				);

	setDefaultInventory(%client);

	if ($missionRunning)
		%client.startMission();
	$HostGamePlayerCount++;

	if ($atfadmin_parentMod $= "classic")
	{
		// z0dd - ZOD - Founder, 5/25/03. Connect log
		if ($Host::ClassicConnectLog)
		{
			$conn::new[$ConnectCount++] = "Player: " @ %client.nameBase @ " Real Name: " @ %realName @ " Guid: " @ %client.guid @ " Connected from: " @ %client.getAddress();
			%file = "connect-" @ getISO8601("date") @ ".log";
			export("$conn::*", "prefs/" @ %file, true);
		}

		// z0dd - ZOD 4/29/02. Activate the clients Classic Huds
		// and start off with 0 SAD access attempts.
		%client.SadAttempts = 0;
		messageClient(%client, 'MsgBomberPilotHud', ""); // Activate the bomber pilot hud

		// z0dd - ZOD, 8/10/02. Get player hit sounds etc.
		commandToClient(%client, 'GetClassicModSettings', 1);

		//---------------------------------------------------------
		// z0dd - ZOD, 7/12/02. New AutoPW server function. Sets
		// server join password when server reaches x player count.
		if ($Host::ClassicAutoPWEnabled && !$atfadmin::AdminAutoPWEnabled)
		{
			if (($Host::ClassicAutoPWPlayerCount != 0 && $Host::ClassicAutoPWPlayerCount !$= "") && ($HostGamePlayerCount >= $Host::ClassicAutoPWPlayerCount))
				AutoPWServer(1);
		}
	}

	AdminAutoPWCheck();
}

function AdminAutoPWServer(%value)
{
	if (%value)
	{
		if ($atfadmin::AdminAutoPWPassword !$= "changeit")
		{
			$atfadmin_oldPassword = $Host::Password;
			$Host::Password = $atfadmin::AdminAutoPWPassword;
		}
	}
	else
		$Host::Password = $atfadmin_oldPassword;
}

function AdminAutoPWCheck(%recount)
{
	if (%recount)
	{
		$atfadmin_adminCount = 0;
		for (%i = 0; %i < ClientGroup.getCount(); %i++)
			if (atfadmin_hasAdmin(ClientGroup.getObject(%i)))
				$atfadmin_adminCount++;
	}

	if ($atfadmin::AdminAutoPWEnabled)
	{
		if ($atfadmin::AdminAutoPWCount != 0 && $atfadmin::AdminAutoPWCount !$= "")
		{
			%adminsNeeded = $atfadmin::AdminAutoPWCount - $atfadmin_adminCount;
			if (%adminsNeeded < 0)
				%adminsNeeded = 0;
			if (($HostGamePlayerCount >= $Host::MaxPlayers - %adminsNeeded) && ($HostGamePlayerCount < $Host::MaxPlayers))
				AdminAutoPWServer(true);
			else
				AdminAutoPWServer(false);
		}
	}
}

function GameConnection::onDrop(%client, %reason)
{
	// TNHAX: remove disconnect messages if the client didn't finish authentication
	if (!isObject(%client) || !%client.doneAuthenticating)
		return;

	if (atfadmin_hasAdmin(%client))
		$atfadmin_adminCount--;

	AdminAutoPWCheck();

	if ($Host::PickupMode && %client.isCaptain)
		atfadmin_nextCaptain();

	echo("DROP:" SPC
		"name:"@%client.nameBase SPC
		"tag:\""@%client.tribe@"\"" SPC
		"guid:"@%client.guid SPC
		%client.getAddress());

	if ($CurrentMissionType !$= "SinglePlayer") 
	{
		for (%cl = 0; %cl < ClientGroup.getCount(); %cl++)
		{
			%recipient = ClientGroup.getObject(%cl);

			// tribe member left
			if ($atfadmin::TribeMemberAnnounce && %client.tribe !$= "" && %client.tribe $= %recipient.tribe && %client != %recipient)
				messageClient(%recipient, 'MsgClientDrop', '\c2Tribe member %1 has left the game.~wfx/Bonuses/Nouns/special3.wav', getTaggedString(%client.name), %client);
		}
	}

	// TNHAX note: even if the tribesnext package is below us, it won't break anything
	Parent::onDrop(%client, %reason);
}

function loadMissionStage1(%missionName, %missionType, %firstMission)
{
	// if a mission group was there, delete prior mission stuff
	if (isObject(MissionGroup))
	{
		// clear out the previous mission paths
		for (%clientIndex = 0; %clientIndex < ClientGroup.getCount(); %clientIndex++)
		{
			// clear ghosts and paths from all clients
			%cl = ClientGroup.getObject(%clientIndex);
			%cl.resetGhosting();
			%cl.clearPaths();
			%cl.isReady = "";
			%cl.matchStartReady = false;
		}

		Game.endMission();
		$lastMissionTeamCount = Game.numTeams;

		MissionGroup.delete();
		MissionCleanup.delete();
		Game.deactivatePackages();
		Game.delete();
		$ServerGroup.delete();
		$ServerGroup = new SimGroup(ServerGroup);
	}

	$CurrentMission = %missionName;
	$CurrentMissionType = %missionType;

	createInvBanCount();
	echo("LOADING MISSION:" SPC %missionName SPC "(" @ %missionType @ ")");

	// increment the mission sequence (used for ghost sequencing)
	$missionSequence++;

	$MissionName = %missionName;
	$missionRunning = false;

	// if this isn't the first mission, allow some time for the server
	// to transmit information to the clients
	if (!%firstMission)
		schedule(15000, ServerGroup, loadMissionStage2);
	else
		loadMissionStage2();
}

function loadMissionStage2()
{
	// create the mission group off the ServerGroup
	echo("Stage 2 load");
	$instantGroup = ServerGroup;

	new SimGroup (MissionCleanup);

	if ($CurrentMissionType $= "")
	{
		new ScriptObject(Game)
		{
			class = DefaultGame;
		};
	}
	else
	{
		new ScriptObject(Game)
		{
			class = $CurrentMissionType @ "Game";
			superClass = DefaultGame;
		};
	}

	// allow the game to activate any packages
	Game.activatePackages();

	// reset the target manager
	resetTargetManager();

	%file = "missions/" @ $missionName @ ".mis";
	if (!isFile(%file))
		return;

	// send the mission file crc to the clients (used for mission lighting)
	$missionCRC = getFileCRC(%file);
	%count = ClientGroup.getCount();

	for (%i = 0; %i < %count; %i++)
	{
		%client = ClientGroup.getObject(%i);
		if (!%client.isAIControlled())
			%client.setMissionCRC($missionCRC);
	}

	$CountdownStarted = false;
	exec(%file);
	$instantGroup = MissionCleanup;

	// pre-game mission stuff
	if (!isObject(MissionGroup))
	{
		error("No 'MissionGroup' found in mission \"" @ $missionName @ "\".");
		schedule(3000, ServerGroup, CycleMissions);
		return;
	}

	MissionGroup.cleanNonType($CurrentMissionType);

	// construct paths
	pathOnMissionLoadDone();

	$ReadyCount = 0;
	$MatchStarted = false;
	$CountdownStarted = false;
	AISystemEnabled( false );

	// set the team damage here so that the game type can override it:

	if ($Host::TournamentMode)
		$TeamDamage = 1;
	else
		$TeamDamage = $Host::TeamDamageOn;

	// z0dd - ZOD, 5/23/03. Setup the defaults
	$RandomTeams = $Host::ClassicRandomizeTeams;
	$FairTeams = $Host::ClassicFairTeams;
	$LimitArmors = $Host::ClassicLimitArmors;

	// z0dd - ZOD 5/27/03. Setup armor max counts
	countArmorAllowed();

	// z0dd - ZOD, 8/4/02. Gravity change
	if(getGravity() !$= $Classic::gravSetting)
		setGravity($Classic::gravSetting);

	// z0dd - ZOD, 5/17/03. Set a minimum flight ceiling for all maps.
	%area = nameToID("MissionGroup/MissionArea");
	if (%area.flightCeiling < 450)
		%area.flightCeiling = 450;

	Game.missionLoadDone();

	// start all the clients in the mission
	$missionRunning = true;
	for (%clientIndex = 0; %clientIndex < ClientGroup.getCount(); %clientIndex++)
		ClientGroup.getObject(%clientIndex).startMission();

	if (!$MatchStarted && $LaunchMode !$= "NavBuild" && $LaunchMode !$= "SpnBuild")
	{
		if ($Host::TournamentMode)
			checkTourneyMatchStart();
		else if ($Host::PickupMode)
			checkPickupGameStart();
		else if ($currentMissionType !$= "SinglePlayer")
			checkMissionStart();
	}

	// offline graph builder... 
	if ($LaunchMode $= "NavBuild")
		buildNavigationGraph("Nav");

	if ($LaunchMode $= "SpnBuild")
		buildNavigationGraph("Spn");

	purgeResources();
	disableCyclingConnections(false);
	$LoadingMission = false;
}

function checkTourneyMatchStart()
{
	if ($CurrentMissionType $= "Siege" && Game.siegeRound > 1 && $atfadmin::EnableExtendedSiegeTourneyHalftime)
		checkSiegeTourneyMatchStart();
	else
		Parent::checkTourneyMatchStart();
}

function serverCmdPlayAnim(%client, %anim)
{
	// allow this to work in base mod
	if ($atfadmin_parentMod !$= "classic")
		PlayAnim(%client, %anim);
}

function PlayAnim(%client, %anim)
{
	// a tiny fishie, 10/6/2002. Prevent players from manually
	// activating death, root, scoutRoot, or sitting poses.
	if (
		%anim $= "Death1" ||
		%anim $= "Death2" ||
		%anim $= "Death3" ||
		%anim $= "Death4" ||
		%anim $= "Death5" ||
		%anim $= "Death6" ||
		%anim $= "Death7" ||
		%anim $= "Death8" ||
		%anim $= "Death9" ||
		%anim $= "Death10" ||
		%anim $= "Death11" ||
		%anim $= "root" ||
		%anim $= "scoutRoot" ||
		%anim $= "sitting"
	) return;

	// don't allow animation spam if the player is muted
	if (%client.globalMute) return;

	%player = %client.player;

	// don't play animations if the player doesn't exist
	if (!isObject(%player))
		return;

	// don't play animations if player is in a vehicle
	if (%player.isMounted())
		return;

	%weapon = (%player.getMountedImage($WeaponSlot) == 0) ? "" : %player.getMountedImage($WeaponSlot).getName().item;

	// a tiny fishie, 10/6/02. Prevent rapid missile fire.
	// We lose animations while holding the missile launcher,
	// but at least the cheat is unavailable...
	if (%weapon $= "MissileLauncher")
		return;

	// a tiny fishie, 10/6/02. No animations within 3m of a forcefield
	// to prevent the forcefield clipping bug.
	InitContainerRadiusSearch(%player.position, 3, $TypeMasks::ForceFieldObjectType);
	if ((%forcefield = containerSearchNext()) != 0)
	{
		if (%forcefield.team != %client.team)
		{
			//messageAdmins(0, '\c2Possible forcefield clip attempt by %1!', %client.name);
			return;
		}
	}

	if (%weapon $= "SniperRifle")
	{
		%player.animResetWeapon = true;
		%player.lastWeapon = %weapon;  
		%player.unmountImage($WeaponSlot);
		%player.setArmThread(look);
	}      

	%player.setActionThread(%anim);
}

function resetServerDefaults()
{
	atfadmin_pickTeams(false);
	$atfadmin_teamcaptain[1] = "";
	$atfadmin_teamcaptain[2] = "";

	$atfadmin_missionQueue = "";
	$atfadmin_nextMissionID = "";

	exec("scripts/atfadminDefaults.cs");
	if (isFile("prefs/atfadmin.cs"))
		exec("prefs/atfadmin.cs");

	atfadmin_overrideServerDefaults();
	Parent::resetServerDefaults();
}

function atfadmin_overrideServerDefaults()
{
	if ($atfadmin::Default::Password $= "none")
		$Host::Password = "";
	else if ($atfadmin::Default::Password !$= "")
		$Host::Password = $atfadmin::Default::Password;

	if ($atfadmin::Default::TournamentMode !$= "")
		$Host::TournamentMode = $atfadmin::Default::TournamentMode;

	if ($atfadmin::Default::TimeLimit !$= "")
		$Host::TimeLimit = $atfadmin::Default::TimeLimit;

	if ($atfadmin::Default::MissionType !$= "")
		$Host::MissionType = $atfadmin::Default::MissionType;

	if ($atfadmin::Default::Map !$= "")
		$Host::Map = $atfadmin::Default::Map;
}

function CycleMissions()
{
	echo("cycling mission." SPC ClientGroup.getCount() SPC "clients in game.");

	if ($Host::TournamentMode && $atfadmin_missionQueue $= "")
	{
		messageAll('MsgClient', 'Loading %1 (%2)...', $MissionDisplayName, $MissionTypeDisplayName);
		loadMission($CurrentMission, $CurrentMissionType);
	}
	else
	{
		if ($atfadmin_missionQueue !$= "")
			%nextMissionID = atfadmin_dequeueMission();
		else
			%nextMissionID = atfadmin_findNextCycleMissionID();

		%missionName = $MissionList[%nextMissionID, MissionDisplayName];
		%typeName = $MissionList[%nextMissionID, TypeDisplayName];
		messageAll('MsgClient', 'Loading %1 (%2)...', %missionName, %typeName);

		%nextMission = $MissionList[%nextMissionID, MissionFileName];
		%nextType = $MissionList[%nextMissionID, TypeName];
		loadMission(%nextMission, %nextType);
	}
}

function atfadmin_findNextCycleMissionID()
{
	if ($atfadmin_nextMissionID !$= "")
		return $atfadmin_nextMissionID;
	else
		return atfadmin_getNextMissionID(atfadmin_getMissionIndexFromFileName($CurrentMission));
}

function serverCmdPrivateMessageSent(%client, %target, %text)
{
	if ((%text $= "") || spamAlert(%client))
		return;

	if (%client.isAdmin)
	{
		%snd = '~wfx/misc/diagnostic_on.wav';
		if (strlen(%text) >= $Host::MaxMessageLen)
			%text = getSubStr(%text, 0, $Host::MaxMessageLen);

		messageClient(%target, 'MsgPrivate', '\c5Message from %1: \c3%2%3', %client.name, %text, %snd);
		messageClient(%client, 'MsgPrivate', '\c5Message to %1: \c3%2%3', %target.name, %text, %snd);
	}
	else
		messageClient(%client, 'MsgError', '\c4Only admins can send private messages');
}

function ServerCmdAddToPrivilegedList(%admin, %client)
{
	if (!%admin.isAdmin)
		return;

	%count = getFieldCount($Host::PrivilegedList);

	// make sure they're not already on the list
	for (%i = 0; %i < %count; %i++)
	{
		%id = getField($Host::PrivilegedList, %i);
		if (%id == %client.guid)
			return;
	}

	if (%count == 0)
		$Host::PrivilegedList = %client.guid;
	else
		$Host::PrivilegedList = $Host::PrivilegedList TAB %client.guid;

	export("$Host::*", "prefs/ServerPrefs.cs", false);
	messageAll('MsgAdminPrivilegedPlayer', '\c2%3 has added %2 to the Privileged Players list.', %client, getTaggedString(%client.name), getTaggedString(%admin.name));
	messageAll('MsgClientJoinTeam', "", %client.name, $teamName[%client.team], %client, %client.team);
	logEcho(%admin.nameBase @ " added " @ %client.nameBase @ " (" @ %client.guid @ ") to Privileged list");
}

function ServerCmdRemoveFromPrivilegedList(%admin, %client)
{
	if (!%admin.isAdmin)
		return;

	%count = getFieldCount($Host::PrivilegedList);
	%newList = "";

	for (%i = 0; %i < %count; %i++)
	{
		%id = getField($Host::PrivilegedList, %i);
		if (%id != %client.guid)
		{
			if (%i == 0)
				%newList = %id;
			else
				%newList = %newList TAB %id;
		}
	}

	$Host::PrivilegedList = %newList;

	export("$Host::*", "prefs/ServerPrefs.cs", false);
	messageAll('MsgAdminStripPrivilegedPlayer', '\c2%3 has removed %2 from the Privileged Players list.', %client, getTaggedString(%client.name), getTaggedString(%admin.name));
	messageAll('MsgClientJoinTeam', "", %client.name, $teamName[%client.team], %client, %client.team);
	logEcho(%admin.nameBase @ " removed " @ %client.nameBase @ " (" @ %client.guid @ ") from Privileged list");
}

function serverCmdForcePlayerToObserver(%clientRequesting, %client)
{
	if (isObject(Game) && %clientRequesting.isAdmin)
	{
		$AdminCl = %clientRequesting;
		Game.forceObserver(%client, "adminForce");
	}
}        

function serverCmdTemporaryVoicePackBlock(%client, %target)
{
	if (%client.isSuperAdmin || (%client.isAdmin && $atfadmin::AllowAdminVoicePackBlock))
	{
		if (atfadmin_OutrankTarget(%client, %target))
		{
			messageAll('MsgAdminForce', '\c2%1 temporarily prevents %2 from using voice packs.', %client.name, %target.name);
			%target.vpblock = true;
			logEcho(%client.nameBase SPC "temporarily blocks voice packs from" SPC %target.nameBase);
		}
		else
			messageClient(%client, 'MsgATFAdminCommand', '\c2Cannot block voice packs from %1 as you do not outrank %2.', %target.name, %target.sex $= "Male" ? 'him' : 'her');
	}
}

function serverCmdPermanentVoicePackBlock(%client, %target)
{
	if (%client.isSuperAdmin || (%client.isAdmin && $atfadmin::AllowAdminVoicePackBlock))
	{
		if (atfadmin_OutrankTarget(%client, %target))
		{
			%count = getFieldCount($Host::VoicePackBlockList);

			for (%i = 0; %i < %count; %i++)
			{
				%id = getField($Host::VoicePackBlockList, %i);
				if (%id == %target.guid)
					return;  // They're already there!
			}

			if (%count == 0)
				$Host::VoicePackBlockList = %client.guid;
			else
				$Host::VoicePackBlockList = $Host::VoicePackBlockList TAB %client.guid;

			export("$Host::*", "prefs/ServerPrefs.cs", false);
			messageAll('MsgAdminForce', '\c2%1 permanently prevents %2 from using voice packs.', %client.name, %target.name);
			%target.vpblock = true;
			logEcho(%client.nameBase SPC "permanently blocks voice packs from" SPC %target.nameBase);
		}
		else
			messageClient(%client, 'MsgATFAdminCommand', '\c2Cannot block voice packs from %1 as you do not outrank %2.', %target.name, %target.sex $= "Male" ? 'him' : 'her');
	}
}

function ServerCmdVoicePackUnblock(%client, %target)
{
	if (%client.isSuperAdmin || (%client.isAdmin && $atfadmin::AllowAdminVoicePackBlock))
	{
		if (atfadmin_OutrankTarget(%client, %target))
		{
			%count = getFieldCount($Host::VoicePackBlockList);
			%newList = "";

			for (%i = 0; %i < %count; %i++)
			{
				%id = getField($Host::VoicePackBlockList, %i);
				if (%id != %target.guid)
				{
					if (%i == 0)
						%newList = %id;
					else
						%newList = %newList TAB %id;
				}
			}

			$Host::VoicePackBlockList = %newList;
			%target.vpblock = "";

			export("$Host::*", "prefs/ServerPrefs.cs", false);
			messageAll('MsgAdminForce', '\c2%1 allows %2 to use voice packs again.', %client.name, %target.name);
		}
		else
			messageClient(%client, 'MsgATFAdminCommand', '\c2Cannot unblock voice packs for %1 as you do not outrank %2.', %target.name, %target.sex $= "Male" ? 'him' : 'her');
	}
}

function serverCmdStripCaptain(%client, %target)
{
	if (!%client.isAdmin || !%target.isCaptain)
		return;

	if (%client == %target)
	{
		messageClient(%target, 'MsgStripCaptainPlayer', 'You have stripped yourself of captain privileges.~wfx/misc/crowd-dis2.wav', %target);
		messageAllExcept(%target, 'MsgStripCaptainPlayer', '%2 is no longer the %3 captain.~wfx/misc/crowd-dis2.wav', %target, %target.name, Game.getTeamName(%target.captainOfTeam));
	}
	else
	{
		messageClient(%target, 'MsgStripCaptainPlayer', '%2 demoted you to a regular player.~wfx/misc/crowd-dis2.wav', %target, %client.name);
		messageAllExcept(%target, 'MsgStripCaptainPlayer', '%2 is no longer the %3 captain.~wfx/misc/crowd-dis2.wav', %target, %target.name, Game.getTeamName(%target.captainOfTeam));
	}

	messageAll('MsgClientJoinTeam', "", %target.name, $teamName[%target.team], %target, %target.team);

	%target.isCaptain = 0;
	%target.captainOfTeam = 0;
	$atfadmin_teamcaptain[%target.team] = "";

	logEcho(%client.nameBase SPC "stripped captain from" SPC %target.nameBase, 1);
	return;
}

function serverCmdCaptainPlayer(%client, %target, %team)
{
	if (!%client.isAdmin || !isObject(%target) || %target.captainOfTeam == %team)
		return;

	$AdminCl = %client;
	Game.voteCaptainPlayer(true, %target, %team);
}

function serverCmdPlayContentSet(%client)
{
	if (($Host::TournamentMode || $Host::PickupMode) && !$CountdownStarted && !$MatchStarted)
		playerPickTeam(%client);
}

function serverCmdStripAdmin(%client, %admin)
{
	if (!%admin.isAdmin || !%client.isAdmin)
		return;

	if (%client == %admin)
	{
		%admin.isAdmin = 0;
		%admin.isSuperAdmin = 0;
		messageClient(%admin, 'MsgStripAdminPlayer', 'You have stripped yourself of admin privileges.', "", "", %admin);
		logEcho(%client.nameBase SPC "stripped admin from" SPC %admin.nameBase, 1);
		return;
	}
	else if (%client.isSuperAdmin)
	{
		messageAll('MsgStripAdminPlayer', '\c2%4 removed %5\'s admin privileges.', "", "", %admin, %client.name, %admin.name);
		messageClient(%admin, 'MsgStripAdminPlayer', 'You are being stripped of your admin privileges by %4.', "", "", %admin, %client.name);
		%admin.isAdmin = 0;
		%admin.isSuperAdmin = 0;
		logEcho(%client.nameBase SPC "stripped admin from" SPC %admin.nameBase, 1);
	}
	else
		messageClient(%client, 'MsgError', '\c2Only Super Admins can use this command.');
}

function playerPickTeam(%client)
{
	%numTeams = Game.numTeams;

	if (%numTeams > 1)
	{
		%client.camera.mode = "PickingTeam";
		if (!$Host::PickupMode)
			schedule( 0, 0, "commandToClient", %client, 'pickTeamMenu', Game.getTeamName(1), Game.getTeamName(2));
	}
	else
	{
		Game.clientJoinTeam(%client, 0, 0);
		%client.observerMode = "pregame";
		%client.notReady = true;
		%client.camera.getDataBlock().setMode( %client.camera, "pre-game", %client.player );
		centerprint( %client, "\nPress FIRE when ready.", 0, 3 );
		%client.setControlObject( %client.camera );
	}
}

function atfadmin_nextCaptain()
{
	if ($MatchStarted || $CountdownStarted) return;

	%captain1 = $atfadmin_teamcaptain[1];
	%captain2 = $atfadmin_teamcaptain[2];

	%curcaptain = $atfadmin_PickingCaptain;
	%otherteam = %curcaptain.team == 1 ? 2 : 1;

	if (%captain1 < 1 || !isObject(%captain1) || %captain2 < 1 || !isObject(%captain2))
	{
		atfadmin_pickTeams(false);
		messageAll('MsgClient', '\c2Need another team captain.~wfx/misc/warning_beep.wav');
		return;
	}

	if ($TeamRank[1, count] < $TeamRank[2, count])
	{
		// team one has fewer players, let them go next
		$atfadmin_PickingCaptain = $atfadmin_teamcaptain[1];
	}
	else if ($TeamRank[2, count] < $TeamRank[1, count])
	{
		// team two has fewer players, let them go next
		$atfadmin_PickingCaptain = $atfadmin_teamcaptain[2];
	}
	else if (%curcaptain > 0)
	{
		// let the other captain pick now
		$atfadmin_PickingCaptain = $atfadmin_teamcaptain[%otherteam];
	}
	else
	{
		// first pick, and teams are even
		// pick a random captain to start
		$atfadmin_PickingCaptain = $atfadmin_teamcaptain[getRandom(1, 2)];
	}

	// announce who gets the pick
	if (%curcaptain == $atfadmin_PickingCaptain)
	{
		messageAllExcept($atfadmin_PickingCaptain, 'MsgClient', '\c2%1 gets to pick again.', $atfadmin_PickingCaptain.name);
		messageClient($atfadmin_PickingCaptain, 'MsgClient', '\c2You get to pick again.');
	}
	else
	{
		messageAllExcept($atfadmin_PickingCaptain, 'MsgClient', '\c2%1 gets to pick a player.', $atfadmin_PickingCaptain.name);
		messageClient($atfadmin_PickingCaptain, 'MsgClient', '\c2It is your turn to pick a player.');
		$atfadmin_CaptainHasTraded[$atfadmin_PickingCaptain] = false;
	}
}

function atfadmin_pickTeams(%enable)
{
	// begin pickup mode stuff
	if (%enable)
	{
		$atfadmin_PickupModePickingTeams = true;
		messageAll('OpenHud', "", "scoreScreen");

		%count = ClientGroup.getCount();
		for (%i = 0; %i < %count; %i++)
		{
			%client = ClientGroup.getObject(%i);
			updateScoreHudThread(%client, 'scoreScreen');
		}

		if ($TeamRank[1, count] < $TeamRank[2, count])
		{
			// team one has fewer players, let them go first
			$atfadmin_PickingCaptain = $atfadmin_teamcaptain[1];
		}
		else if ($TeamRank[2, count] < $TeamRank[1, count])
		{
			// team two has fewer players, let them go first
			$atfadmin_PickingCaptain = $atfadmin_teamcaptain[2];
		}
		else
		{
			// pick a random captain to start
			$atfadmin_PickingCaptain = $atfadmin_teamcaptain[getRandom(1, 2)];
		}

		messageAll('MsgClient', '\c2The first pick goes to %1.~wfx/Bonuses/Nouns/captain.wav', $atfadmin_PickingCaptain.name);
	}
	else
	{
		messageAll('CloseHud', "", "scoreScreen");
		$atfadmin_PickupModePickingTeams = false;
		$atfadmin_PickingCaptain = -1;
	}
}

//function serverCmdHideHud(%client, %tag)
//{
//	%tag = getWord(%tag, 0);
//
//	switch$ (%tag)
//	{
//		case 'scoreScreen':
//			if (!$atfadmin_PickupModePickingTeams)
//			{
//				messageClient(%client, 'CloseHud', "", %tag);
//				cancel(%client.scoreHudThread);
//				%client.scoreHudThread = "";
//			}
//		default:
//			messageClient(%client, 'CloseHud', "", %tag);
//	}
//}

function serverCmdCancelMatchStart(%client)
{
	if (!%client.isAdmin || !$CountdownStarted) return;

	$AdminCl = %client;
	Game.cancelMatchStart(%client);
}

function serverCmdShowMissionQueue(%client)
{
	atfadmin_showMissionQueue(%client);
}

function checkPickupGameStart()
{
	if ($CountdownStarted || $MatchStarted)
		return;

	// loop through all the clients and see if any are still notready
	%playerCount = 0;
	%notReadyCount = 0;

	%count = ClientGroup.getCount();
	for (%i = 0; %i < %count; %i++)
	{
		%cl = ClientGroup.getObject(%i);
		if (%cl.camera.mode $= "pickingTeam")
		{
			%notReady[%notReadyCount] = %cl;
			%notReadyCount++;
		}   
		else if (%cl.camera.Mode $= "pre-game")
		{
			if (%cl.notready)
			{
				%notReady[%notReadyCount] = %cl;
				%notReadyCount++;
			}
			else
			{   
				%playerCount++;
			}
		}
		else if (%cl.camera.Mode $= "observer")
		{
			// this guy is watching
		}
	}

	if (%notReadyCount)
	{
		if (%notReadyCount == 1)
			MessageAll('msgHoldingUp', '\c1%1 is holding things up!', %notReady[0].name);
		else if (%notReadyCount < 4)
		{
			for (%i = 0; %i < %notReadyCount - 2; %i++)
				%str = getTaggedString(%notReady[%i].name) @ ", " @ %str;

			%str = "\c2" @ %str @ getTaggedString(%notReady[%i].name) @ " and " @ getTaggedString(%notReady[%i+1].name) @ " are holding things up!";
			MessageAll('msgHoldingUp', %str);
		}
		return;
	}

	if (%playerCount != 0)
	{
		%count = ClientGroup.getCount();
		for (%i = 0; %i < %count; %i++)
		{
			%cl = ClientGroup.getObject(%i);
			%cl.notready = "";
			%cl.notReadyCount = "";
			ClearCenterPrint(%cl);
			ClearBottomPrint(%cl);
		}

		if (Game.scheduleVote !$= "" && Game.voteType $= "VoteMatchStart") 
		{
			messageAll('closeVoteHud', "");
			cancel(Game.scheduleVote);
			Game.scheduleVote = "";
		}

		// close everyone's score screen hud
		atfadmin_pickTeams(false);

		Countdown(30 * 1000);
	}
}

function logEcho(%msg, %export)
{
	if ($Host::ClassicLogEchoEnabled)
	{
		$AdminLog::new = getISO8601("dateandtime") SPC %msg;
		echo("LOG: " @ $AdminLog::new);

		if (%export)
		{
			%file = "admin-" @ getISO8601("date") @ ".log";
			export("$AdminLog::*", "prefs/" @ %file, true);
		}
	}
	else
		echo("LOG:" SPC %msg);
}

function getISO8601(%type, %year, %month, %day, %hour, %minute, %second)
{
	// type may be: date, time, or dateandtime

	%datestr = "";
	%timestr = "";

	if (%type $= "date" || %type $= "dateandtime")
	{
		if (%year $= "" || strlen(%year) < 4)
			%year = formatTimeString("yy");

		if (%month $= "")
			%month = formatTimeString("mm");
		else if (strlen(%month) < 2)
			%month = "0" @ %month;

		if (%day $= "")
			%day = formatTimeString("dd");
		else if (strlen(%day) < 2)
			%day = "0" @ %day;

		%datestr = %year @ %month @ %day;
	}

	if (%type $= "time" || %type $= "dateandtime")
	{
		if (%hour $= "")
			%hour = formatTimeString("HH");
		else if (strlen(%hour) < 2)
			%hour = "0" @ %hour;

		if (%minute $= "")
			%minute = formatTimeString("nn");
		else if (strlen(%minute) < 2)
			%minute = "0" @ %minute;

		if (%second $= "")
			%second = formatTimeString("ss");
		else if (strlen(%second) < 2)
			%second = "0" @ %second;

		%timestr = %hour @ %minute @ %second;
	}

	return %datestr @ %timestr;
}


};
// END package atfadmin_server
