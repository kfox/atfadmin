// START package atfadmin_defaultGame
package atfadmin_defaultGame
{


function DefaultGame::clientMissionDropReady(%game, %client)
{
	// synchronize the clock HUD
	messageClient(%client, 'MsgSystemClock', "", 0, 0);

	%game.sendClientTeamList(%client);
	%game.setupClientHuds(%client);

	if ($CurrentMissionType $= "SinglePlayer")
	{
		//CommandToClient(%client, 'setPlayContent');
		return;
	}

	%observer = false;
	if (!$Host::TournamentMode && !$Host::PickupMode)
	{
		if (%client.camera.mode $= "observerFly" || %client.camera.mode $= "justJoined")
		{
			%observer = true;
			commandToClient(%client, 'setHudMode', 'Observer');
			%client.setControlObject(%client.camera);
			updateObserverFlyHud(%client);
		}

		if (!%observer)
		{
			if (!$MatchStarted && !$CountdownStarted) // server has not started anything yet
			{
				%client.setControlObject(%client.camera);
				commandToClient(%client, 'setHudMode', 'Observer');
			}
			else if (!$MatchStarted && $CountdownStarted) // server has started the countdown
			{
				commandToClient(%client, 'setHudMode', 'Observer');
				%client.setControlObject(%client.camera);
			}
			else
			{
				commandToClient(%client, 'setHudMode', 'Standard'); // the game has already started
				%client.setControlObject(%client.player);
			}
		}
	}
	else if ($Host::TournamentMode || $Host::PickupMode)
	{
		// set all players into obs mode. setting the control object will handle further procedures...
		%client.camera.getDataBlock().setMode(%client.camera, "ObserverFly");
		commandToClient(%client, 'setHudMode', 'Observer');
		%client.setControlObject(%client.camera);
		messageAll('MsgClientJoinTeam', "", %client.name, $teamName[0], %client, 0);
		%client.team = 0;

		if (!$MatchStarted && !$CountdownStarted)
		{
			if ($TeamDamage)
				%damMess = "ENABLED";
			else
				%damMess = "DISABLED";

			if (%game.numTeams > 1)
			{
				if ($Host::TournamentMode)
					BottomPrint(%client, "Server is Running in Tournament Mode.\nPick a Team\nTeam Damage is" SPC %damMess, 0, 3);
				else // $Host::PickupMode
					BottomPrint(%client, "Server is Running in Pickup Mode.\nWait for a captain to assign you to a team.\nTeam Damage is" SPC %damMess, 0, 3);
			}
		}
		else
		{
			if ($Host::TournamentMode)
				BottomPrint(%client, "\nServer is Running in Tournament Mode", 0, 3);
			else
				BottomPrint(%client, "\nServer is Running in Pickup Mode", 0, 3);
		}
	}

	// make sure the objective HUD indicates your team on top and in green...
	if (%client.team > 0)
		messageClient(%client, 'MsgCheckTeamLines', "", %client.team);

	// were ready to go.
	%client.matchStartReady = true;
	echo("Client" SPC %client SPC "is ready.");

	if ($atfadmin::EnableStats && !$Host::TournamentMode)
		atfstats_clientMissionDropReady(%client);
}

function DefaultGame::voteChangeTimeLimit(%game, %admin, %newLimit)
{
	if (%newLimit == 999)
		%display = "unlimited";
	else
		%display = %newLimit;

	%cause = "";
	if (%admin)
	{
		messageAll('MsgAdminForce', '\c2%2 changed the mission time limit to %1 minutes.', %newLimit, $AdminCl.name);
		$Host::TimeLimit = %newLimit;

		%cause = "(admin:"@$AdminCl.nameBase@")";
		$AdminCl = "";
	}
	else
	{
		%totalVotes = %game.totalVotesFor + %game.totalVotesAgainst;
		if (%totalVotes > 0 && (%game.totalVotesFor / (ClientGroup.getCount() - $HostGameBotCount)) >= ($Host::VotePasspercent / 100))
		{
			messageAll('MsgVotePassed', '\c2The mission time limit was set to %1 minutes by vote.', %newLimit);
			$Host::TimeLimit = %newLimit;
			if ($atfadmin::ResetTimeLimit)
				$atfadmin_ResetTimeLimit = true;

			%cause = "(vote)";
		}
		else
			messageAll('MsgVoteFailed', '\c2The vote to change the mission time limit did not pass: %1 percent.', mFloor(%game.totalVotesFor/(ClientGroup.getCount() - $HostGameBotCount) * 100));
	}

	// if the time limit was actually changed...
	if (%cause !$= "")
	{
		logEcho("time limit set to "@%display SPC %cause);

		//if the match has been started, reset the end of match countdown
		if ($matchStarted)
		{
			//schedule the end of match countdown
			%elapsedTimeMS = getSimTime() - $missionStartTime;
			%curTimeLeftMS = ($Host::TimeLimit * 60 * 1000) - %elapsedTimeMS;
			error("time limit="@$Host::TimeLimit@", elapsed="@(%elapsedTimeMS / 60000)@", curtimeleftms="@%curTimeLeftMS);
			CancelEndCountdown();
			warn("starting new end countdown with "@%curTimeLeftMS@"ms left");
			EndCountdown(%curTimeLeftMS);
			cancel(%game.timeSync);
			%game.checkTimeLimit(true);
		}
	}
}

function DefaultGame::voteResetServer(%game, %admin, %client)
{
	%cause = "";
	if ( %admin )
	{
		messageAll('AdminResetServer', '\c2%1 has reset the server.', $AdminCl.name);
		resetServerDefaults();
		%cause = "(admin:"@$AdminCl.nameBase@")";
		$AdminCl = "";
	}
	else
	{
		%totalVotes = %game.totalVotesFor + %game.totalVotesAgainst;
		if(%totalVotes > 0 && (%game.totalVotesFor / (ClientGroup.getCount() - $HostGameBotCount)) >= ($Host::VotePasspercent / 100))
		{
			messageAll('MsgVotePassed', '\c2The Server has been reset by vote.' );
			resetServerDefaults();
			%cause = "(vote)";
		}
		else
			messageAll('MsgVoteFailed', '\c2The vote to reset Server to defaults did not pass: %1 percent.', mFloor(%game.totalVotesFor/(ClientGroup.getCount() - $HostGameBotCount) * 100));
	}

	if(%cause !$= "")
		logEcho("server reset" SPC %cause);
}

function atfadmin_deAdmin(%client)
{
	if (%client.isSuperAdmin || %client.isAdmin)
	{
		if (%client.isAdmin)
		{
			%client.isAdmin = false;
			%client.wasAdmin = true;
		}

		if (%client.isSuperAdmin)
		{
			%client.isSuperAdmin = false;
			%client.wasSuperAdmin = true;
		}

		%gender = (%client.sex $= "Male" ? 'him' : 'her');
		messageAdminsExcept(%client, 'MsgATFAdminCommand', '\c2%1 has de-adminned %2self.~wfx/misc/rolechange.wav', %client.name, %gender);
		messageClient(%client, 'MsgATFAdminCommand', '\c2You have de-adminned yourself.~wfx/misc/rolechange.wav');

		logEcho(%client.nameBase SPC "de-adminned self");
		updateCanListenState(%client);
	}
}

function atfadmin_reAdmin(%client)
{
	if (%client.wasSuperAdmin || %client.wasAdmin)
	{
		%gender = (%client.sex $= "Male" ? 'him' : 'her');
		messageAdminsExcept(%client, 'MsgATFAdminCommand', '\c2%1 has re-adminned %2self.~wfx/misc/rolechange.wav', %client.name, %gender);
		messageClient(%client, 'MsgATFAdminCommand', '\c2You have re-adminned yourself.~wfx/misc/rolechange.wav');

		if (%client.wasAdmin)
		{
			%client.isAdmin = true;
			%client.wasAdmin = false;
		}

		if (%client.wasSuperAdmin)
		{
			%client.isSuperAdmin = true;
			%client.wasSuperAdmin = false;
		}

		logEcho(%client.nameBase SPC "re-adminned self");
		updateCanListenState(%client);
	}
}

function DefaultGame::voteAdminPlayer(%game, %admin, %client)
{
	%cause = "";

	if (%admin)
	{
		messageAll('MsgAdminAdminPlayer', '\c2%3 made %2 an admin.', %client, %client.name, $AdminCl.name);
		messageAll('MsgClientJoinTeam', "", %client.name, $teamName[%client.team], %client, %client.team);
		%client.isAdmin = 1;
		%cause = "(admin:"@$AdminCl.nameBase@")";
		$AdminCl = "";
	}
	else
	{
		%totalVotes = %game.totalVotesFor + %game.totalVotesAgainst;
		if(%totalVotes > 0 && (%game.totalVotesFor / (ClientGroup.getCount() - $HostGameBotCount)) >= ($Host::VotePasspercent / 100))
		{
			messageAll('MsgAdminPlayer', '\c2%2 was made an admin by vote.', %client, %client.name);
			messageAll('MsgClientJoinTeam', "", %client.name, $teamName[%client.team], %client, %client.team);
			%client.isAdmin = 1;
			%cause = "(vote)";
		}
		else
			messageAll('MsgVoteFailed', '\c2Vote to make %1 an admin did not pass.', %client.name);
	}

	if(%cause !$= "")
	{
		logEcho(%client.nameBase@" (cl "@%client@") made admin "@%cause);
		updateCanListenState(%client);
	}
}

function DefaultGame::voteCaptainPlayer(%game, %admin, %client, %team)
{
	%cause = "";

	// shouldn't happen, but just in case...
	if (%team $= "" || %team == 0) return;

	// this team already has a captain...
	if ($atfadmin_teamcaptain[%team] > 0 && $atfadmin_teamcaptain[%team] !$= "")
		return;

	if (%admin)
	{
		messageAll('MsgCaptainPlayer', '\c2%3 made %2 the %4 team captain.~wfx/misc/crowd-clap.wav', %client, %client.name, $AdminCl.name, %game.getTeamName(%team), %team);
		messageAll('MsgClientJoinTeam', "", %client.name, $teamName[%client.team], %client, %client.team);
		%client.isCaptain = true;
		%client.captainOfTeam = %team;
		%cause = "(admin:"@$AdminCl.nameBase@")";
		$AdminCl = "";
	}
	else
	{
		%totalVotes = %game.totalVotesFor + %game.totalVotesAgainst;
		if(%totalVotes > 0 && (%game.totalVotesFor / (ClientGroup.getCount() - $HostGameBotCount)) >= ($Host::VotePasspercent / 100))
		{
			messageAll('MsgCaptainPlayer', '\c2%2 was chosen as %3 team captain by vote.~wfx/misc/crowd-clap.wav', %client, %client.name, %game.getTeamName(%team), %team);
			messageAll('MsgClientJoinTeam', "", %client.name, $teamName[%client.team], %client, %client.team);
			%client.isCaptain = true;
			%client.captainOfTeam = %team;
			%cause = "(vote)";
		}
		else
			messageAll('MsgVoteFailed', '\c2Vote to make %1 a captain did not pass.', %client.name);
	}

	$atfadmin_teamcaptain[%team] = %client;

	// change the player's team
	if (%client.team != %team)
	{
		if (%client.team > 0)
			%game.clientChangeTeam(%client, %team, false);
		else
			%game.clientChangeTeam(%client, %team, true);
	}

	%client.notReady = true;

	if (%cause !$= "")
		logEcho(%client.nameBase@" (cl "@%client@") made captain "@%cause);

	// determine the other team
	%otherteam = %client.team == 1 ? 2 : 1;

	// did we switch one team's captain to the other team?
	if ($atfadmin_teamcaptain[%otherteam] == %client)
		$atfadmin_teamcaptain[%otherteam] = "";

	// time to start the picking process?
	if ($atfadmin_teamcaptain[%otherteam] > 0 && isObject($atfadmin_teamcaptain[%otherteam]) && !$MatchStarted && !$CountdownStarted)
		schedule(0, 0, "atfadmin_pickTeams", true);
}

function DefaultGame::voteGlobalMutePlayer(%game, %admin, %client)
{
	if (%admin)
	{
		if(%client.globalMute)
		{
			messageAllExcept(%client, 'MsgAdminForce', '\c2%1 has been globally unmuted by %2.', %client.name, $AdminCl.name);
			messageClient(%client, 'MsgAdminForce', '\c2You have been globally unmuted by %1.', $AdminCl.name);
			%client.globalMute = false;
			%setto = "unmuted";
		}
		else
		{
			messageAllExcept(%client, 'MsgAdminForce', '\c2%1 has been globally muted by %2.~wfx/Bonuses/Nouns/donkey.wav', %client.name, $AdminCl.name);
			messageClient(%client, 'MsgAdminForce', '\c2You have been globally muted by %1.~wfx/Bonuses/Nouns/donkey.wav', $AdminCl.name);
			%client.globalMute = true;
			%setto = "muted";
		}
		%cause = "(admin:"@$AdminCl.nameBase@")";
		$AdminCl = "";
	}
	else
	{
		%totalVotes = %game.totalVotesFor + %game.totalVotesAgainst;
		if(%totalVotes > 0 && (%game.totalVotesFor / (ClientGroup.getCount() - $HostGameBotCount)) >= ($Host::VotePasspercent / 100))
		{
			if (%client.globalMute)
			{
				messageAll('MsgVotePassed', '\c2%1 has been globally unmuted by vote.', %client.name);
				%client.globalMute = false;
				%setto = "unmuted";
			}
			else
			{
				messageAll('MsgVotePassed', '\c2%1 has been globally muted by vote.~wfx/Bonuses/Nouns/donkey.wav');
				%client.globalMute = true;
				%setto = "muted";
			}
			%cause = "(vote)";
		}
		else
		{
			if (%client.globalMute)
				messageAll('MsgVoteFailed', '\c2Vote to globally unmute %1 did not pass: %2 percent.', %client.name, mFloor(%game.totalVotesFor/(ClientGroup.getCount() - $HostGameBotCount) * 100));
			else
				messageAll('MsgVoteFailed', '\c2Vote to globally mute %1 did not pass: %2 percent.', %client.name, mFloor(%game.totalVotesFor/(ClientGroup.getCount() - $HostGameBotCount) * 100));
		}
	}

	if (%setto !$= "")
		logEcho(%client.nameBase SPC "globally" SPC %setto SPC %cause);
}

function DefaultGame::voteKickPlayer(%game, %admin, %client)
{
	%cause = "";
	%kicked = %client;

	if(%admin)
	{
		kick(%client, %admin, %client.guid);
		%cause = "(admin:"@$AdminCl.nameBase@")";
		$AdminCl = "";
	}
	else
	{
		%team = %client.team;
		%totalVotes = %game.votesFor[%game.kickTeam] + %game.votesAgainst[%game.kickTeam];
		if(%totalVotes > 0 && (%game.votesFor[%game.kickTeam] / %totalVotes) >= ($Host::VotePasspercent / 100))
		{
			kick(%client, %admin, %game.kickGuid);
			%cause = "(vote)";
		}
		else
		{
			for ( %idx = 0; %idx < ClientGroup.getCount(); %idx++ )
			{
				%cl = ClientGroup.getObject( %idx );

				if (%cl.team == %game.kickTeam && !%cl.isAIControlled())
					messageClient( %cl, 'MsgVoteFailed', '\c2Kick player vote did not pass.' );
			}
		}
	}

	%game.kickTeam = "";
	%game.kickGuid = "";
	%game.kickClientName = "";
	%game.kickClient = "";

	if(%cause !$= "")
		logEcho(%kicked.nameBase@" (cl " @ %kicked @ ") kicked " @ %cause);

}

function DefaultGame::banPlayer(%game, %admin, %client)
{
	%cause = "";
	%name = %client.nameBase;
	if( %admin )
	{
		ban(%client, %admin);
		%cause = "(admin:"@$AdminCl.nameBase@")";
		$AdminCl = "";
	}
	else
		%cause = "(vote)";

	if(%cause !$= "")
		logEcho(%name@" (cl "@%client@") banned "@%cause);
}

function DefaultGame::voteChangeMission(%game, %admin, %missionDisplayName, %typeDisplayName, %missionId, %missionTypeId)
{
	%mission = $MissionList[%missionId, MissionFileName];
	if (%missionId $= "")
	{
		error("Invalid mission index passed to DefaultGame::voteChangeMission!");
		return;
	}

	%missionType = $HostTypeName[%missionTypeId];
	if (%missionType $= "")
	{
		error("Invalid mission type id passed to DefaultGame::voteChangeMission!");
		return;
	}

	if (%admin)
	{
		messageAll('MsgAdminChangeMission', '\c2%3 has changed the mission to %1 (%2).', %missionDisplayName, %typeDisplayName, $AdminCl.name);
		%missionDisplayName = $MissionList[%missionId, MissionDisplayName];
		logEcho("mission changed to "@%missionDisplayName@"/"@%typeDisplayName@" (admin:"@$AdminCl.nameBase@")");
		$AdminCl = "";

		%nextMissionID = atfadmin_findNextCycleMissionID();
		%nextMission = $MissionList[%nextMissionID, MissionFileName];

		if ((%mission !$= %nextMission || (%mission $= %nextMission && %missionType !$= $CurrentMissionType))
		&& $atfadmin::PreserveMapRotationContinuity
		&& !$Host::TournamentMode)
		{
			if ($atfadmin_nextMissionID $= "" && $IsVoteableOnly[%missionId])
				$atfadmin_nextMissionID = %nextMissionID;
			else if (!$IsVoteableOnly[%missionId])
				$atfadmin_nextMissionID = "";
		}
		%game.gameOver();

		if ($atfadmin_missionQueue !$= "")
			$atfadmin_nextMissionID = atfadmin_dequeueMission();

		loadMission(%mission, %missionType, false);
	}
	else
	{
		%totalVotes = %game.totalVotesFor + %game.totalVotesAgainst;
		if(%totalVotes > 0 && (%game.totalVotesFor / (ClientGroup.getCount() - $HostGameBotCount)) >= ($Host::VotePasspercent / 100))
		{
			messageAll('MsgVotePassed', '\c2The mission was changed to %1 (%2) by vote.', %missionDisplayName, %typeDisplayName);
			%missionDisplayName = $MissionList[%missionId, MissionDisplayName];
			logEcho("mission changed to "@%missionDisplayName@"/"@%typeDisplayName@" (vote)");

			%nextMissionID = atfadmin_findNextCycleMissionID();
			%nextMission = $MissionList[%nextMissionID, MissionFileName];

			if (%mission !$= %nextMission && $atfadmin::PreserveMapRotationContinuity && !$Host::TournamentMode)
			{
				if ($atfadmin_nextMissionID $= "" && $IsVoteableOnly[%missionId])
					$atfadmin_nextMissionID = %nextMissionID;
				else if (!$IsVoteableOnly[%missionId])
					$atfadmin_nextMissionID = "";
			}
			%game.gameOver();

			if ($atfadmin_missionQueue !$= "")
				$atfadmin_nextMissionID = atfadmin_dequeueMission();

			loadMission(%mission, %missionType, false);
		}
		else
			messageAll('MsgVoteFailed', '\c2Change mission vote did not pass: %1 percent.', mFloor(%game.totalVotesFor/(ClientGroup.getCount() - $HostGameBotCount) * 100));
	}
}

function DefaultGame::voteEnqueueMission(%game, %admin, %missionDisplayName, %typeDisplayName, %missionId, %missionTypeId)
{
	if (%missionId $= "")
	{
		error("Invalid mission index passed to DefaultGame::voteEnqueueMission!");
		return;
	}

	%missionType = $HostTypeName[%missionTypeId];
	if (%missionType $= "")
	{
		error("Invalid mission type id passed to DefaultGame::voteEnqueueMission!");
		return;
	}

	%missionDisplayName = $Missionlist[%missionId, MissionDisplayName];
	%typeDisplayName = $Missionlist[%missionId, TypeDisplayName];

	if (%admin)
	{
		messageAll('MsgAdminEnqueueMission', '\c2%3 has enqueued mission %1 (%2).~wgui/launchMenuOver.wav', %missionDisplayName, %typeDisplayName, $AdminCl.name);
		logEcho("enqueued "@%missionDisplayName@"/"@%typeDisplayName@" (admin:"@$AdminCl.nameBase@")");
		$AdminCl = "";
		atfadmin_enqueueMission(%missionId);
	}
	else
	{
		%totalVotes = %game.totalVotesFor + %game.totalVotesAgainst;
		if(%totalVotes > 0 && (%game.totalVotesFor / (ClientGroup.getCount() - $HostGameBotCount)) >= ($Host::VotePasspercent / 100))
		{
			messageAll('MsgVotePassed', '\c2Mission %1 (%2) has been enqueued by vote.', %missionDisplayName, %typeDisplayName);
			logEcho("enqueued "@%missionDisplayName@"/"@%typeDisplayName@" (vote)");
			atfadmin_enqueueMission(%missionId);
		}
		else
			messageAll('MsgVoteFailed', '\c2Enqueue vote did not pass: %1 percent.', mFloor(%game.totalVotesFor/(ClientGroup.getCount() - $HostGameBotCount) * 100));
	}
}

function DefaultGame::voteDequeueMission(%game, %admin, %missionId)
{
	%mission = $MissionList[%missionId, MissionFileName];
	if (%mission $= "")
	{
		error("Invalid mission index passed to DefaultGame::voteDequeueMission!");
		return;
	}

	%missionDisplayName = $MissionList[%missionId, MissionDisplayName];
	%typeDisplayName = $MissionList[%missionId, TypeDisplayName];

	if (%admin)
	{
		messageAll('MsgAdminDequeueMission', '\c2%3 has dequeued mission %1 (%2).~wgui/objective_notification.wav', %missionDisplayName, %typeDisplayName, $AdminCl.name);
		logEcho("dequeued "@%missionDisplayName@"/"@%typeDisplayName@" (admin:"@$AdminCl.nameBase@")");
		$AdminCl = "";
		atfadmin_dequeueMission(%missionId);
	}
	else
	{
		%totalVotes = %game.totalVotesFor + %game.totalVotesAgainst;
		if(%totalVotes > 0 && (%game.totalVotesFor / (ClientGroup.getCount() - $HostGameBotCount)) >= ($Host::VotePasspercent / 100))
		{
			messageAll('MsgVotePassed', '\c2Mission %1 (%2) has been dequeued by vote.', %missionDisplayName, %typeDisplayName);
			logEcho("dequeued "@%missionDisplayName@"/"@%typeDisplayName@" (vote)");
			atfadmin_dequeueMission(%missionId);
		}
		else
			messageAll('MsgVoteFailed', '\c2Dequeue vote did not pass: %1 percent.', mFloor(%game.totalVotesFor/(ClientGroup.getCount() - $HostGameBotCount) * 100));
	}
}

function DefaultGame::voteTeamDamage(%game, %admin)
{
	%setto = "";
	%cause = "";
	if(%admin)
	{
		if($teamDamage)
		{
			messageAll('MsgAdminForce', '\c2%1 has disabled team damage.', $AdminCl.name);
			$Host::TeamDamageOn = $TeamDamage = 0;
			%setto = "disabled";
		}
		else
		{
			messageAll('MsgAdminForce', '\c2%1 has enabled team damage.', $AdminCl.name);
			$Host::TeamDamageOn = $TeamDamage = 1;
			%setto = "enabled";
		}
		%cause = "(admin:"@$AdminCl.nameBase@")";
		$AdminCl = "";
	}
	else
	{
		%totalVotes = %game.totalVotesFor + %game.totalVotesAgainst;
		if(%totalVotes > 0 && (%game.totalVotesFor / (ClientGroup.getCount() - $HostGameBotCount)) >= ($Host::VotePasspercent / 100))
		{
			if($teamDamage)
			{
				messageAll('MsgVotePassed', '\c2Team damage was disabled by vote.');
				$Host::TeamDamageOn = $TeamDamage = 0;
				%setto = "disabled";
			}
			else
			{
				messageAll('MsgVotePassed', '\c2Team damage was enabled by vote.');
				$Host::TeamDamageOn = $TeamDamage = 1;
				%setto = "enabled";
			}
			%cause = "(vote)";
		}
		else
		{
			if($teamDamage)
				messageAll('MsgVoteFailed', '\c2Disable team damage vote did not pass: %1 percent.', mFloor(%game.totalVotesFor/(ClientGroup.getCount() - $HostGameBotCount) * 100));
			else
				messageAll('MsgVoteFailed', '\c2Enable team damage vote did not pass: %1 percent.', mFloor(%game.totalVotesFor/(ClientGroup.getCount() - $HostGameBotCount) * 100));
		}
	}
	if(%setto !$= "")
		logEcho("team damage "@%setto SPC %cause);
}

function DefaultGame::voteTournamentMode(%game, %admin, %missionDisplayName, %typeDisplayName, %missionId, %missionTypeId)
{
	%mission = $MissionList[%missionId, MissionFileName];
	if (%mission $= "")
	{
		error("Invalid mission index passed to DefaultGame::voteTournamentMode!");
		return;
	}

	%missionType = $HostTypeName[%missionTypeId];
	if (%missionType $= "")
	{
		error("Invalid mission type id passed to DefaultGame::voteTournamentMode!");
		return;
	}

	%cause = "";
	if (%admin)
	{
		messageAll('MsgAdminForce', '\c2%2 has switched the server to Tournament mode (%1).', %missionDisplayName, $AdminCl.name);
		setModeTournament(%mission, %missionType);
		%cause = "(admin:"@$AdminCl.nameBase@")";
		$AdminCl = "";
	}
	else
	{
		%totalVotes = %game.totalVotesFor + %game.totalVotesAgainst;
		if(%totalVotes > 0 && (%game.totalVotesFor / (ClientGroup.getCount() - $HostGameBotCount)) >= ($Host::VotePasspercent / 100))
		{
			messageAll('MsgVotePassed', '\c2Server switched to Tournament mode by vote (%1): %2 percent.', %missionDisplayName, mFloor(%game.totalVotesFor/(ClientGroup.getCount() - $HostGameBotCount) * 100));
			setModeTournament(%mission, %missionType);
			%cause = "(vote)";
		}
		else
			messageAll('MsgVoteFailed', '\c2Tournament mode vote did not pass: %1 percent.', mFloor(%game.totalVotesFor/(ClientGroup.getCount() - $HostGameBotCount) * 100));
	}

	if(%cause !$= "")
		logEcho("tournament mode set "@%cause);
}

function DefaultGame::voteFFAMode(%game, %admin, %client)
{
	%cause = "";
	%name = getTaggedString(%client.name);

	if (%admin)
	{
		messageAll('MsgAdminForce', '\c2%1 has switched the server to Free For All mode.', $AdminCl.name);
		setModeFFA($CurrentMission, $CurrentMissionType);
		%cause = "(admin:"@$AdminCl.nameBase@")";
		$AdminCl = "";
	}
	else
	{
		%totalVotes = %game.totalVotesFor + %game.totalVotesAgainst;
		if(%totalVotes > 0 && (%game.totalVotesFor / (ClientGroup.getCount() - $HostGameBotCount)) >= ($Host::VotePasspercent / 100))
		{
			messageAll('MsgVotePassed', '\c2Server switched to Free For All mode by vote.', %client);
			setModeFFA($CurrentMission, $CurrentMissionType);
			%cause = "(vote)";
		}
		else
			messageAll('MsgVoteFailed', '\c2Free For All mode vote did not pass: %1 percent.', mFloor(%game.totalVotesFor/(ClientGroup.getCount() - $HostGameBotCount) * 100));
	}

	if (%cause !$= "")
		logEcho("free for all set "@%cause);
}

function DefaultGame::votePickupMode(%game, %admin, %missionDisplayName, %typeDisplayName, %missionId, %missionTypeId)
{
	%mission = $MissionList[%missionId, MissionFileName];
	if (%mission $= "")
	{
		error("Invalid mission index passed to DefaultGame::votePickupMode!");
		return;
	}

	%missionType = $HostTypeName[%missionTypeId];
	if (%missionType $= "")
	{
		error("Invalid mission type id passed to DefaultGame::votePickupMode!");
		return;
	}

	%cause = "";
	if (%admin)
	{
		messageAll('MsgAdminForce', '\c2%1 has switched the server to Pickup mode.', $AdminCl.name);
		setModePickup(%missionId);
		%cause = "(admin:"@$AdminCl.nameBase@")";
		$AdminCl = "";
	}
	else
	{
		%totalVotes = %game.totalVotesFor + %game.totalVotesAgainst;
		if(%totalVotes > 0 && (%game.totalVotesFor / (ClientGroup.getCount() - $HostGameBotCount)) >= ($Host::VotePasspercent / 100))
		{
			messageAll('MsgVotePassed', '\c2Server switched to Pickup mode by vote: %2 percent.', %missionDisplayName, mFloor(%game.totalVotesFor/(ClientGroup.getCount() - $HostGameBotCount) * 100));
			setModePickup(%missionId);
			%cause = "(vote)";
		}
		else
			messageAll('MsgVoteFailed', '\c2Pickup mode vote did not pass: %1 percent.', mFloor(%game.totalVotesFor/(ClientGroup.getCount() - $HostGameBotCount) * 100));
	}

	if (%cause !$= "")
		logEcho("pickup mode set "@%cause);
}

function DefaultGame::assignRandomTeam(%game, %client, %respawn)
{
	// Work out team sizes
	%numPlayers = ClientGroup.getCount();
	for (%i = 0; %i <= %game.numTeams; %i++) %numTeamPlayers[%i] = 0;
		for (%i = 0; %i < %numPlayers; %i = %i + 1)
		{
			%cl = ClientGroup.getObject(%i);
			if (%cl != %client) %numTeamPlayers[%cl.team]++;
		}

	// Get smallest team and check to see if the teams are equal
	%leastPlayers = %numTeamPlayers[1];
	%leastTeam = 1;
	%equal = true;
	for (%i = 2; %i <= %game.numTeams; %i++)
	{
		if (%numTeamPlayers[%i] < %leastPlayers)
		{
			%equal = false;
			%leastTeam = %i;
			%leastPlayers = %numTeamPlayers[%i];
		}
		else if (%numTeamPlayers[1] < %numTeamPlayers[%i])
			%equal = false;
	}

	if (%equal)
		%team = getRandom(1, %game.numTeams);
	else
		%team = %leastTeam;

	%game.clientJoinTeam(%client, %team, %respawn);
}

function DefaultGame::cancelMatchStart(%game, %admin)
{
	CancelCountdown();

	%count = ClientGroup.getCount();
	for (%i = 0; %i < %count; %i++)
	{
		%cl = ClientGroup.getObject(%i);
		messageClient(%cl, 'msgClient', '\c2Match start cancelled by %1.~wfx/misc/warning_beep.wav', $AdminCl.name);
		// sync client clocks to zero so they don't think match is still starting
		messageClient(%cl, 'MsgSystemClock', "", 0, 0);

		// bring back "ready" pop-up
		%cl.notready = 1;
		%cl.notReadyCount = "";
		centerprint(%cl, "\nPress FIRE when ready.", 0, 3);
	}

	logEcho("cancelled match start (admin:"@$AdminCl.nameBase@")");
	$AdminCl = "";
}

function DefaultGame::cancelVote(%game, %admin)
{
	// cancel the vote
	messageAll('closeVoteHud', "");
	cancel(Game.scheduleVote);
	Game.scheduleVote = "";
	clearVotes();

	// inform everyone whodunnit
	messageAll('MsgVoteFailed', '\c2Vote cancelled by %1.', $AdminCl.name);
	logEcho("vote cancelled (admin:"@$AdminCl.nameBase@")");
	$AdminCl = "";
}

function DefaultGame::passVote(%game, %admin)
{
	// pass the vote
	messageAll('closeVoteHud', "");
	cancel(Game.scheduleVote);
	Game.scheduleVote = "";
	clearVotes();

	// inform everyone whodunnit
	$atfadmin_voteinfo = %typeName TAB %arg1 TAB %arg2 TAB %arg3 TAB %arg4;
	messageAll('MsgVotePassed', '\c2Vote passed by %1.', $AdminCl.name);
	logEcho("vote passed (admin:"@$AdminCl.nameBase@")");
	$AdminCl = "";

	%typeName = getField($atfadmin_voteinfo, 0);
	%arg1 = getField($atfadmin_voteinfo, 1);
	%arg2 = getField($atfadmin_voteinfo, 2);
	%arg3 = getField($atfadmin_voteinfo, 3);
	%arg4 = getField($atfadmin_voteinfo, 4);
	$atfadmin_voteinfo = "";

	%game.evalVote(%typename, false, %arg1, %arg2, %arg3, %arg4);
}

function DefaultGame::sendTimeLimitList(%game, %client, %key)
{
	if (!$Host::TournamentMode)
		%list = $atfadmin::TimeLimitList;
	else
		%list = "";

	if (%list $= "")
		parent::sendTimeLimitList(%game, %client, %key);
	else
	{
		// Run through list sending them to the client
		for (%i = 0; (%time = getWord(%list, %i)) !$= ""; %i++)
		{
			%text = (%time == 999) ? 'No time limit' : %time SPC "minute time limit";
			messageClient(%client, 'MsgVoteItem', "", %key, %time, "", %text);
		}
	}
}

function DefaultGame::banPlayer(%game, %admin, %client)
{
	%cause = "";
	%name = %client.nameBase;

	if (%admin)
	{
		ban(%client, %admin);
		%cause = "(admin:"@$AdminCl.nameBase@")";
		$AdminCl = "";
	}
	else
	{
		//%team = %client.team;
		%totalVotes = %game.votesFor[%game.banTeam] + %game.votesAgainst[%game.banTeam];
		if (%totalVotes > 0 && (%game.votesFor[%game.banTeam] / %totalVotes) >= ($Host::VotePasspercent / 100))
		{
			ban(%client, %admin);
			%cause = "(vote)";
		}
		else
			messageAll('MsgVoteFailed', '\c2Ban player vote did not pass');
	}

	%game.banTeam = "";

	if (%cause !$= "") logEcho(%name@" (cl "@%client@") banned "@%cause);
}

function DefaultGame::sendGamePlayerPopupMenu(%game, %client, %targetClient, %key)
{
	if (!%targetClient.matchStartReady)
		return;

	%isPrivileged = %client.isPrivileged;
	%isAdmin = (%client.isAdmin || %client.isSuperAdmin);
	%isSuperAdmin = %client.isSuperAdmin;
	%isTargetSelf = (%client == %targetClient);
	%isTargetAdmin = (%targetClient.isAdmin || %targetClient.isSuperAdmin || %targetClient.wasAdmin || %targetClient.wasSuperAdmin);
	%isTargetPrivileged = %targetClient.isPrivileged;
	%isTargetCaptain = %targetClient.isCaptain;
	%isTargetBot = %targetClient.isAIControlled();
	%isTargetObserver = (%targetClient.team == 0);
	%isTargetAllowedToVote = !%targetClient.votingDisabled;
	%isObserver = (%client.team == 0);
	%isTargetMuted = %client.globalMute;
	%isTargetVPBlocked = %targetClient.vpblock;

	%outrankTarget = atfadmin_OutrankTarget(%client, %targetClient);

	// ATFADMIN POPUP MENU CODES:

	// 601 warn player (admin only)
	// 602 observe player
	// 603 global mute
	// 604 global unmute
	// 605 lightning strike (admin only)
	// 606 add to privileged list (admin only)
	// 607 remove from privileged list (admin only)
	// 608 blow up player (admin only)
	// 609 allow single-player voting (admin only)
	// 610 disallow single-player voting (admin only)
	// 611 voice pack temporary block (admin only)
	// 612 voice pack permanent block (admin only)
	// 613 voice pack unblock (admin only)
	// 614 strip captain (admin only)
	// 615 make captain of team 1 (admin only)
	// 616 make captain of team 2 (admin only)

	// add privileged player
	if (%isAdmin && %targetClient.guid != 0 && !%isTargetAdmin && $atfadmin::ExportServerPrefs && %client.atfclient)
	{
		if (%isTargetPrivileged)
			messageClient(%client, 'MsgPlayerPopupItem', "", %key, "RemoveFromPrivilegedList", "", 'Remove from Privileged Players List', 607);
		else
			messageClient(%client, 'MsgPlayerPopupItem', "", %key, "AddToPrivilegedList", "", 'Add to Privileged Players List', 606);
	}

	// add admins
	if ($atfadmin::AllowAddToAdminList && %isSuperAdmin && !isOnAdminList(%targetClient) && %targetClient.guid != 0 && $atfadmin::ExportServerPrefs)
		messageClient(%client, 'MsgPlayerPopupItem', "", %key, "addAdmin", "", 'Add to Server Admin List', 10);

	// add superadmins
	if ($atfadmin::AllowAddToAdminList && %isSuperAdmin && !isOnSuperAdminList(%targetClient) && %targetClient.guid != 0 && $atfadmin::ExportServerPrefs)
		messageClient(%client, 'MsgPlayerPopupItem', "", %key, "addSuperAdmin", "", 'Add to Server SuperAdmin List', 11);

	if (!%isTargetSelf)
	{
		// mute options
		if (%client.muted[%targetClient])
			messageClient(%client, 'MsgPlayerPopupItem', "", %key, "MutePlayer", "", 'Unmute Text Chat', 1);
		else
			messageClient(%client, 'MsgPlayerPopupItem', "", %key, "MutePlayer", "", 'Mute Text Chat', 1);

		// voicecom options
		if (!%isTargetBot && %client.canListenTo(%targetClient))
		{
			if (%client.getListenState(%targetClient))
				messageClient(%client, 'MsgPlayerPopupItem', "", %key, "ListenPlayer", "", 'Disable Voice Com', 9);
			else
				messageClient(%client, 'MsgPlayerPopupItem', "", %key, "ListenPlayer", "", 'Enable Voice Com', 9);
		}

		// observer
		if (%isObserver && !%isTargetObserver)
			messageClient(%client, 'MsgPlayerPopupItem', "", %key, "ObservePlayer", "", 'Observe Player', 12);
	}

	if (!%client.canVote && !%isAdmin) return;

	// regular vote options on players
	if (%game.scheduleVote $= "" && !%isAdmin && !%isTargetAdmin)
	{
		if ($Host::allowAdminPlayerVotes && !%isTargetBot)
			messageClient(%client, 'MsgPlayerPopupItem', "", %key, "AdminPlayer", "", 'Vote to Make Admin', 2);

		if (!%isTargetSelf && !%isTargetPrivileged)
		{
			if ($atfadmin::AllowPlayerVoteKickBan & 1)
				messageClient(%client, 'MsgPlayerPopupItem', "", %key, "KickPlayer", "", 'Vote to Kick', 3);

			if ($atfadmin::AllowPlayerVoteKickBan & 2 && !%isTargetBot)
				messageClient(%client, 'MsgPlayerPopupItem', "", %key, "BanPlayer", "", 'Vote to Ban', 4);

			if ($atfadmin::AllowPlayerVoteGlobalMutePlayers & 1)
			{
				if (%isTargetMuted)
				{
					%what = "Vote to unmute";
					messageClient(%client, 'MsgPlayerPopupItem', "", %key, "GlobalMutePlayer", "", %what, 604);
				}
				else
				{
					%what = "Vote to mute";
					messageClient(%client, 'MsgPlayerPopupItem', "", %key, "GlobalMutePlayer", "", %what, 603);
				}
			}
		}
	}

	// Admin-only options on players
	else if (%isAdmin)
	{
		if ((%isSuperAdmin || $atfadmin::AllowAdminAdmin)
			&& !%isTargetBot && !%isTargetAdmin)
		{
			messageClient(%client, 'MsgPlayerPopupItem', "", %key, "AdminPlayer", "", 'Make Admin', 2);
		}

		if (!%isTargetSelf && !%isTargetBot && $atfadmin_parentMod $= "classic")
			messageClient(%client, 'MsgPlayerPopupItem', "", %key, "SendMessage", "", 'Send Private Message', 15);

		if (!%isTargetObserver && !%isTargetBot && (%outrankTarget || %isTargetSelf))
			messageClient(%client, 'MsgPlayerPopupItem', "", %key, "ToObserver", "", 'Force observer', 5);

		if (%isAdmin && $Host::PickupMode && %client.atfclient && %client.atfclientversion $= "2.3.0")
		{
			if (%isTargetCaptain)
				messageClient(%client, 'MsgPlayerPopupItem', "", %key, "StripCaptain", "", 'Strip captain', 614);

			if (%targetClient.captainOfTeam != 1)
			{
				%str = "Make" SPC getTaggedString(%game.getTeamName(1)) SPC "Captain";
				messageClient(%client, 'MsgPlayerPopupItem', "", %key, "CaptainPlayer", "", %str, 615);
			}

			if (%targetClient.captainOfTeam != 2)
			{
				%str = "Make" SPC getTaggedString(%game.getTeamName(2)) SPC "Captain";
				messageClient(%client, 'MsgPlayerPopupItem', "", %key, "CaptainPlayer", "", %str, 616);
			}
		}

		if (!%isTargetSelf && %outrankTarget)
		{
			if (%isSuperAdmin || $atfadmin::AllowAdminKickBan & 1)
				messageClient(%client, 'MsgPlayerPopupItem', "", %key, "KickPlayer", "", 'Kick', 3);

			if (!%isTargetBot)
			{
				if (%isSuperAdmin || $atfadmin::AllowAdminKickBan & 2)
					messageClient(%client, 'MsgPlayerPopupItem', "", %key, "BanPlayer", "", 'Ban', 4);

				messageClient(%client, 'MsgPlayerPopupItem', "", %key, "Warn", "", 'Warn player', 13);

				if (%isTargetAdmin && %isSuperAdmin && $atfadmin_parentMod $= "classic")
					messageClient(%client, 'MsgPlayerPopupItem', "", %key, "StripAdmin", "", 'Strip admin', 14);

				if (%client.atfclient)
				{
					if ($atfadmin::AllowAdminGlobalMutePlayers)
					{
						if (%isTargetMuted)
						{
							%what = "Globally unmute";
							messageClient(%client, 'MsgPlayerPopupItem', "", %key, "GlobalMutePlayer", "", %what, 604);
						}
						else
						{
							%what = "Globally mute";
							messageClient(%client, 'MsgPlayerPopupItem', "", %key, "GlobalMutePlayer", "", %what, 603);
						}
					}

					if ($atfadmin::AllowAdminDisableVoting)
					{
						if (%isTargetAllowedToVote)
						{
							%what = "Disable voting";
							messageClient(%client, 'MsgPlayerPopupItem', "", %key, "TogglePlayerVoting", "", %what, 610);
						}
						else
						{
							%what = "Enable voting";
							messageClient(%client, 'MsgPlayerPopupItem', "", %key, "TogglePlayerVoting", "", %what, 609);
						}
					}

					if ($atfadmin::AllowAdminLightningStrike)
						messageClient(%client, 'MsgPlayerPopupItem', "", %key, "LightningStrike", "", 'Lightning strike', 605);

					if ($atfadmin::AllowAdminBlowupPlayers)
						messageClient(%client, 'MsgPlayerPopupItem', "", %key, "BlowupPlayer", "", 'Blow Up', 608);

					if ($atfadmin::AllowAdminVoicePackBlock)
					{
						if (%isTargetVPBlocked)
						{
							%what = "Allow Voice Packs";
							messageClient(%client, 'MsgPlayerPopupItem', "", %key, "VoicePackUnblock", "", %what, 613);
						}
						else
						{
							messageClient(%client, 'MsgPlayerPopupItem', "", %key, "TemporaryVoicePackBlock", "", "Temp VP Block", 611);
							if ($atfadmin::ExportServerPrefs)
								messageClient(%client, 'MsgPlayerPopupItem', "", %key, "PermanentVoicePackBlock", "", "Perm VP Block", 612);
						}
					}
				}
			}
		}

		if (%isTargetSelf || %outrankTarget)
		{
			if (%game.numTeams > 1)
			{
				if (%isTargetObserver)
				{
					%action = %isTargetSelf ? "Join " : "Change to ";
					%str1 = %action @ getTaggedString( %game.getTeamName(1) );      
					%str2 = %action @ getTaggedString( %game.getTeamName(2) );      
					messageClient(%client, 'MsgPlayerPopupItem', "", %key, "ChangeTeam", "", %str1, 6);
					messageClient(%client, 'MsgPlayerPopupItem', "", %key, "ChangeTeam", "", %str2, 7);
				}
				else
				{
					%changeTo = %targetClient.team == 1 ? 2 : 1;
					%str = "Switch to " @ getTaggedString( %game.getTeamName(%changeTo) );
					%caseId = 5 + %changeTo;

					messageClient(%client, 'MsgPlayerPopupItem', "", %key, "ChangeTeam", "", %str, %caseId);
				}
			}
			else if (%isTargetObserver)
			{
				%str = %isTargetSelf ? 'Join the Game' : 'Add to Game';
				messageClient(%client, 'MsgPlayerPopupItem', "", %key, "JoinGame", "", %str, 8);
			}
		}
	}
}

function DefaultGame::sendGameVoteMenu(%game, %client, %key)
{
	if ($LoadingMission || $ResettingServer) return;

	if ($atfadmin::DisableATFAdminForTournaments && $Host::TournamentMode)
	{
		//
		// --- Cancel Match Start ---
		//

		if (!$MatchStarted && $CountdownStarted && %client.isAdmin && %client.atfclient && %client.atfclientversion $= "2.3.0")
			messageClient(%client, 'MsgVoteItem', "", %key, 'CancelMatchStart', 'Cancel Start', 'Cancel Match Start');

		//
		// --- Enqueue Mission ---
		//

		if (%game.scheduleVote $= "" && %client.atfclient && %client.atfclientversion $= "2.3.0")
		{
			if (%client.isSuperAdmin || (%client.isAdmin && $atfadmin::AllowAdminQueueMission))
				messageClient(%client, 'MsgVoteItem', "", %key, 'VoteEnqueueMission', 'enqueue mission', 'Enqueue Mission');
			else if ($atfadmin::AllowPlayerVoteQueueMission)
				messageClient(%client, 'MsgVoteItem', "", %key, 'VoteEnqueueMission', 'enqueue mission', 'Vote to Enqueue Mission');
		}

		//
		// --- Show Mission Queue ---
		//

		if ($atfadmin_missionQueue !$= "")
		{
			messageClient(%client, 'MsgVoteItem', "", %key, 'ShowMissionQueue', '', 'Show Mission Queue');
		}

		//
		// --- Dequeue Mission ---
		//

		if (%game.scheduleVote $= "" && %client.atfclient && %client.atfclientversion $= "2.3.0")
		{
			if ($atfadmin_missionQueue !$= "")
			{
				if (%client.isSuperAdmin || (%client.isAdmin && $atfadmin::AllowAdminQueueMission))
					messageClient(%client, 'MsgVoteItem', "", %key, 'VoteDequeueMission', 'dequeue mission', 'Dequeue Mission');
				else if ($atfadmin::AllowPlayerVoteQueueMission)
					messageClient(%client, 'MsgVoteItem', "", %key, 'VoteDequeueMission', 'dequeue mission', 'Vote to Dequeue Mission');
			}
		}

		Parent::sendGameVoteMenu(%game, %client, %key);
		return;
	}

	%isAdmin = (%client.isAdmin || %client.isSuperAdmin);
	%isCaptain = %client.isCaptain;
	%wasAdmin = (%client.wasAdmin || %client.wasSuperAdmin);
	%multipleTeams = %game.numTeams > 1;

	//
	// --- Change team ---
	//

	if ($MatchStarted && !$Host::TournamentMode && !$Host::PickupMode)
	{
		if (%client.team != 0)
		{
			if (%multipleTeams)
				messageClient(%client, 'MsgVoteItem', "", %key, 'ChooseTeam', "", 'Change your Team');

			messageClient(%client, 'MsgVoteItem', "", %key, 'MakeObserver', "", 'Become an Observer');
		}
		else if (!%multipleTeams)
			messageClient(%client, 'MsgVoteItem', "", %key, 'JoinGame', "", 'Join the Game');
	}

	if (!%client.canVote && !(%isAdmin || %wasAdmin)) return;

	//
	// --- Reset Server ---
	//

	if (%client.isSuperAdmin
	||  (%client.isAdmin && $atfadmin::AllowAdminResetServer))
	{
		messageClient(%client, 'MsgVoteItem', "", %key, 'VoteResetServer', 'reset server defaults', 'Reset the Server');
	}

	//
	// --- De-Admin / Re-Admin ---
	//

	if ($atfadmin::AllowAdminDeAdminSelf)
	{
		//
		// --- De-Admin ---
		//

		if (%client.isAdmin || %client.isSuperAdmin)
		{
			messageClient(%client, 'MsgVoteItem', "", %key, 'VoteDeAdmin', 'de-admin yourself', 'De-Admin Yourself');
		}

		//
		// --- Re-Admin ---
		//

		if (%client.wasSuperAdmin || %client.wasAdmin)
		{
			messageClient(%client, 'MsgVoteItem', "", %key, 'VoteReAdmin', 're-admin yourself', 'Re-Admin Yourself');
		}
	}

	//
	// --- Base Rape ---
	//

	if ($atfadmin::BaseRapeMinimumPlayers > 0 && (%client.isSuperAdmin || (%client.isAdmin && $atfadmin::AllowAdminToggleBaseRape)))
	{
		if ($atfadmin_alwaysAllowBaseRape)
			messageClient(%client, 'MsgVoteItem', "", %key, 'VoteBaseRapeToggle', 'disable base rape', 'Prevent Base Rape (<%1 players)', $atfadmin::BaseRapeMinimumPlayers);
		else
			messageClient(%client, 'MsgVoteItem', "", %key, 'VoteBaseRapeToggle', 'enable base rape', 'Allow Base Rape');
	}

	//
	// --- Asset Tracking ---
	//

	if (%client.isSuperAdmin || (%client.isAdmin && $atfadmin::AllowAdminToggleAssetTracking))
	{
		if ($atfadmin_assetTrack)
			messageClient(%client, 'MsgVoteItem', "", %key, 'VoteToggleAssetTracking', 'disable asset tracking', 'Disable Asset Tracking');
		else
			messageClient(%client, 'MsgVoteItem', "", %key, 'VoteToggleAssetTracking', 'enable asset tracking', 'Enable Asset Tracking');
	}

	//
	// --- Show Mission Queue ---
	//

	if ($atfadmin_missionQueue !$= "")
	{
		messageClient(%client, 'MsgVoteItem', "", %key, 'ShowMissionQueue', '', 'Show Mission Queue');
	}

	if (%game.scheduleVote $= "")
	{
		//
		// --- Change Mission ---
		//

		if ($atfadmin_missionQueue $= "")
		{
			if (%client.isSuperAdmin ||
					(%client.isAdmin && $atfadmin::AllowAdminChangeMission > 0))
				messageClient(%client, 'MsgVoteItem', "", %key, 'VoteChangeMission', 'change the mission to', 'Change the Mission');
			else if ($atfadmin::AllowPlayerVoteChangeMission > 0)
				messageClient(%client, 'MsgVoteItem', "", %key, 'VoteChangeMission', 'change the mission to', 'Vote to Change the Mission');
		}

		//
		// --- Skip Current Mission ---
		//

		if ($atfadmin_nextMissionID !$= "")
		{
			%nextMission = $MissionList[$atfadmin_nextMissionID, MissionDisplayName];
			%nextType = $MissionList[$atfadmin_nextMissionID, TypeDisplayName];
		}
		else
		{
			%misID = atfadmin_findNextCycleMissionID();
			%nextMission = $MissionList[%misID, MissionDisplayName];
			%nextType = $MissionList[%misID, TypeDisplayName];
		}

		if (%client.isSuperAdmin || (%client.isAdmin && $atfadmin::AllowAdminSkipMission > 0))
			messageClient(%client, 'MsgVoteItem', "", %key, 'VoteSkipMission', 'skip to the next mission', 'Skip to the Next Mission: %1 (%2)', %nextMission, %nextType);
		else if ($atfadmin::AllowPlayerVoteSkipMission > 0)
			messageClient(%client, 'MsgVoteItem', "", %key, 'VoteSkipMission', 'skip to the next mission', 'Vote for the Next Mission: %1 (%2)', %nextMission, %nextType);

		//
		// --- Enqueue Mission ---
		//

		if (%client.atfclient && %client.atfclientversion $= "2.3.0")
		{
			if (%client.isSuperAdmin || (%client.isAdmin && $atfadmin::AllowAdminQueueMission))
				messageClient(%client, 'MsgVoteItem', "", %key, 'VoteEnqueueMission', 'enqueue mission', 'Enqueue Mission');
			else if ($atfadmin::AllowPlayerVoteQueueMission)
				messageClient(%client, 'MsgVoteItem', "", %key, 'VoteEnqueueMission', 'enqueue mission', 'Vote to Enqueue Mission');
		}

		//
		// --- Dequeue Mission ---
		//

		if (%client.atfclient && %client.atfclientversion $= "2.3.0")
		{
			if ($atfadmin_missionQueue !$= "")
			{
				if (%client.isSuperAdmin || (%client.isAdmin && $atfadmin::AllowAdminQueueMission))
					messageClient(%client, 'MsgVoteItem', "", %key, 'VoteDequeueMission', 'dequeue mission', 'Dequeue Mission');
				else if ($atfadmin::AllowPlayerVoteQueueMission)
					messageClient(%client, 'MsgVoteItem', "", %key, 'VoteDequeueMission', 'dequeue mission', 'Vote to Dequeue Mission');
			}
		}

		//
		// --- Change Mode ---
		//

		if (%client.isSuperAdmin ||
		    (%client.isAdmin && $atfadmin::AllowAdminChangeMode))
		{
			if ($Host::TournamentMode)
			{
				messageClient(%client, 'MsgVoteItem', "", %key, 'VoteFFAMode', 'Change server to Free For All', 'Free For All Mode');
				messageClient(%client, 'MsgVoteItem', "", %key, 'VotePickupMode', 'Change server to Pickup', 'Pickup Mode');

				if (!$MatchStarted && !$CountdownStarted)
					messageClient(%client, 'MsgVoteItem', "", %key, 'VoteMatchStart', 'Start Match', 'Start the Match');
			}
			else if ($Host::PickupMode)
			{
				messageClient(%client, 'MsgVoteItem', "", %key, 'VoteFFAMode', 'Change server to Free For All', 'Free For All Mode');
				messageClient(%client, 'MsgVoteItem', "", %key, 'voteTournamentMode', 'Change server to Tournament.', 'Tournament Mode');
			}
			else
			{
				messageClient(%client, 'MsgVoteItem', "", %key, 'VotePickupMode', 'Change server to Pickup', 'Pickup Mode');
				messageClient(%client, 'MsgVoteItem', "", %key, 'voteTournamentMode', 'Change server to Tournament.', 'Tournament Mode');
			}
		}
		else if ($atfadmin::AllowPlayerVoteChangeMode)
		{
			if ($Host::TournamentMode)
			{
				messageClient(%client, 'MsgVoteItem', "", %key, 'VoteFFAMode', 'Change server to Free For All', 'Vote Free For All Mode');
				messageClient(%client, 'MsgVoteItem', "", %key, 'VotePickupMode', 'Change server to Pickup', 'Vote Pickup Mode');
			}
			else if ($Host::PickupMode)
			{
				messageClient(%client, 'MsgVoteItem', "", %key, 'VoteFFAMode', 'Change server to Free For All', 'Vote Free For All Mode');
				messageClient(%client, 'MsgVoteItem', "", %key, 'voteTournamentMode', 'Change server to Tournament.', 'Vote Tournament Mode');
			}
			else
			{
				messageClient(%client, 'MsgVoteItem', "", %key, 'VotePickupMode', 'Change server to Pickup', 'Vote Pickup Mode');
				messageClient(%client, 'MsgVoteItem', "", %key, 'voteTournamentMode', 'Change server to Tournament.', 'Vote Tournament Mode');
			}
		}

		//
		// --- Match Started ---
		//

		if (($Host::TournamentMode || $Host::PickupMode) && !$MatchStarted)
		{
			if ($CountdownStarted && %client.isAdmin)
				messageClient(%client, 'MsgVoteItem', "", %key, 'CancelMatchStart', 'Cancel Start', 'Cancel Match Start');
		}

		//
		// --- Team Damage ---
		//

		if (%multipleTeams)
		{
			if (%client.isSuperAdmin
			||  (%client.isAdmin && $atfadmin::AllowAdminTeamDamage))
			{
				if ($teamDamage)
					messageClient(%client, 'MsgVoteItem', "", %key, 'VoteTeamDamage', 'disable team damage', 'Disable Team Damage');
				else
					messageClient(%client, 'MsgVoteItem', "", %key, 'VoteTeamDamage', 'enable team damage', 'Enable Team Damage');
			}
			else if ($atfadmin::AllowPlayerVoteTeamDamage)
			{
				if ($teamDamage)
					messageClient(%client, 'MsgVoteItem', "", %key, 'VoteTeamDamage', 'disable team damage', 'Vote to Disable Team Damage');
				else
					messageClient(%client, 'MsgVoteItem', "", %key, 'VoteTeamDamage', 'enable team damage', 'Vote to Enable Team Damage');
			}
		}

		//
		// --- Change Time Limit ---
		//

		if ($CurrentMissionType !$= "Siege" || ($CurrentMissionType $= "Siege" && %game.siegeRound == 1))
		{
			if (%client.isSuperAdmin
			||  (%client.isAdmin && $atfadmin::AllowAdminChangeTimeLimit))
				messageClient(%client, 'MsgVoteItem', "", %key, 'VoteChangeTimeLimit', 'change the time limit', 'Change the Time Limit');
			else if ($atfadmin::AllowPlayerVoteTimeLimit)
				messageClient(%client, 'MsgVoteItem', "", %key, 'VoteChangeTimeLimit', 'change the time limit', 'Vote Change the Time Limit');
		}

		//
		// --- Showdown Siege ---
		//
		if ($CurrentMissionType $= "Siege")
		{
			if (%client.isSuperAdmin
			||  (%client.isAdmin && $atfadmin::AllowAdminShowdownSiege))
			{
				if ($atfadmin_showdownSiegeEnabled)
					messageClient(%client, 'MsgVoteItem', "", %key, 'VoteShowdownSiege', 'disable Showdown Siege', 'Disable Showdown Siege');
				else
					messageClient(%client, 'MsgVoteItem', "", %key, 'VoteShowdownSiege', 'enable Showdown Siege', 'Enable Showdown Siege');
			}
			else if ($atfadmin::AllowPlayerVoteShowdownSiege)
			{
				if ($atfadmin_showdownSiegeEnabled)
					messageClient(%client, 'MsgVoteItem', "", %key, 'VoteShowdownSiege', 'disable Showdown Siege', 'Vote to Disable Showdown Siege');
				else
					messageClient(%client, 'MsgVoteItem', "", %key, 'VoteShowdownSiege', 'enable Showdown Siege', 'Vote to Enable Showdown Siege');
			}
		}
	}
	else
	{
		//
		// --- Cancel Vote ---
		//

		if (%client.isAdmin && $atfadmin::AllowAdminCancelVote)
			messageClient(%client, 'MsgVoteItem', "", %key, 'CancelVote', 'cancel vote', 'Cancel Running Vote');

		//
		// --- Pass Vote ---
		//

		if (%client.isAdmin && $atfadmin::AllowAdminPassVote)
			messageClient(%client, 'MsgVoteItem', "", %key, 'PassVote', 'pass vote', 'Pass Running Vote');
	}
}

function DefaultGame::onClientKilled(%game, %clVictim, %clKiller, %damageType, %implement, %damageLocation)
{
	Parent::onClientKilled(%game, %clVictim, %clKiller, %damageType, %implement, %damageLocation);

	// if the death was "default", prevent respawning for an
	// additional amount of time as specified in
	// $atfadmin::observerModeRespawnTime
	if (%damageType == $DamageType::Default)
	{
		if ($atfadmin::ObserverModeRespawnTime !$= "")
			%respawnTime = $atfadmin::ObserverModeRespawnTime * 1000;
		else
			%respawnTime = 10 * 1000; // default to 10 seconds

		%clVictim.observerModeRespawnTime = getSimTime() + %respawnTime;
	}

	if (!$Host::TournamentMode)
		atfstats_handleKillStat(%game, %clVictim, %clKiller, %damageType, %implement, %damageLocation);
}

function DefaultGame::displayDeathMessages(%game, %clVictim, %clKiller, %damageType, %implement)
{
	%victimGender = (%clVictim.sex $= "Male" ? 'him' : 'her');
	%victimPoss = (%clVictim.sex $= "Male" ? 'his' : 'her');
	%killerGender = (%clKiller.sex $= "Male" ? 'him' : 'her');
	%killerPoss = (%clKiller.sex $= "Male" ? 'his' : 'her');
	%victimName = %clVictim.name;
	%killerName = %clKiller.name;
	//error("DamageType = " @ %damageType @ ", implement = " @ %implement @ ", implement class = " @ %implement.getClassName() @ ", is controlled = " @ %implement.getControllingClient());

	if(%damageType == $DamageType::Impact) // run down by vehicle
	{
		if( ( %controller = %implement.getControllingClient() ) > 0)
		{
			%killerGender = (%controller.sex $= "Male" ? 'him' : 'her');
			%killerPoss = (%controller.sex $= "Male" ? 'his' : 'her');           
			%killerName = %controller.name;
			messageAll('msgVehicleKill', $DeathMessageVehicle[mFloor(getRandom() * $DeathMessageVehicleCount)], %victimName, %victimGender, %victimPoss, %killerName ,%killerGender, %killerPoss, %damageType, $DamageTypeText[%damageType]);
			logEcho(%clVictim.nameBase@" (pl "@%clVictim.player@"/cl "@%clVictim@") killed by a vehicle controlled by "@%controller.nameBase);
		}
		else
		{
			messageAll('msgVehicleKill', $DeathMessageVehicleUnmanned[mFloor(getRandom() * $DeathMessageVehicleUnmannedCount)], %victimName, %victimGender, %victimPoss, %killerName, %killerGender, %killerPoss, %damageType, $DamageTypeText[%damageType]);
			logEcho(%clVictim.nameBase@" (pl "@%clVictim.player@"/cl "@%clVictim@") killed by a vehicle (unmanned)");
		}
	}
	else if (isObject(%implement) && (%implement.getClassName() $= "Turret" || %implement.getClassName() $= "VehicleTurret" || %implement.getClassName() $= "FlyingVehicle" || %implement.getClassName() $= "HoverVehicle"))
	{
		if (%implement.getControllingClient() != 0)  //is turret being controlled?
		{
			%controller = %implement.getControllingClient();
			%killerGender = (%controller.sex $= "Male" ? 'him' : 'her');
			%killerPoss = (%controller.sex $= "Male" ? 'his' : 'her');           
			%killerName = %controller.name;

			if (%controller == %clVictim)
				messageAll('msgTurretSelfKill', $DeathMessageTurretSelfKill[mFloor(getRandom() * $DeathMessageTurretSelfKillCount)],%victimName, %victimGender, %victimPoss, %killerName, %killerGender, %killerPoss, %damageType, $DamageTypeText[%damageType]);
			else if (%controller.team == %clVictim.team) // controller TK'd a friendly     
				messageAll('msgCTurretKill', $DeathMessageCTurretTeamKill[%damageType, mFloor(getRandom() * $DeathMessageCTurretTeamKillCount)],%victimName, %victimGender, %victimPoss, %killerName, %killerGender, %killerPoss, %damageType, $DamageTypeText[%damageType]);
			else // controller killed an enemy
				messageAll('msgCTurretKill', $DeathMessageCTurretKill[%damageType, mFloor(getRandom() * $DeathMessageCTurretKillCount)],%victimName, %victimGender, %victimPoss, %killerName, %killerGender, %killerPoss, %damageType, $DamageTypeText[%damageType]);

			if (%damageType == $DamageType::TankMortar)
			{
				logEcho(%clVictim.nameBase@" (pl "@%clVictim.player@"/cl "@%clVictim@") killed by "@%controller.nameBase@" (pl "@%controller.player@"/cl "@%controller@") using tank mortar");
			}
			else if (%damageType == $DamageType::TankChaingun)
			{
				logEcho(%clVictim.nameBase@" (pl "@%clVictim.player@"/cl "@%clVictim@") killed by "@%controller.nameBase@" (pl "@%controller.player@"/cl "@%controller@") using tank chaingun");
			}
			else if (%damageType == $DamageType::BomberBombs)
			{
				logEcho(%clVictim.nameBase@" (pl "@%clVictim.player@"/cl "@%clVictim@") killed by "@%controller.nameBase@" (pl "@%controller.player@"/cl "@%controller@") using bomber bomb");
			}
			else if (%damageType == $DamageType::BellyTurret)
			{
				logEcho(%clVictim.nameBase@" (pl "@%clVictim.player@"/cl "@%clVictim@") killed by "@%controller.nameBase@" (pl "@%controller.player@"/cl "@%controller@") using belly turret");
			}
			else if (%damageType == $DamageType::ShrikeBlaster)
			{
				logEcho(%clVictim.nameBase@" (pl "@%clVictim.player@"/cl "@%clVictim@") killed by "@%controller.nameBase@" (pl "@%controller.player@"/cl "@%controller@") using shrike blaster");
			}
			else if (%damageType == $DamageType::IndoorDepTurret)
			{
				logEcho(%clVictim.nameBase@" (pl "@%clVictim.player@"/cl "@%clVictim@") killed by "@%controller.nameBase@" (pl "@%controller.player@"/cl "@%controller@") using clamp turret");
			}
			else if (%damageType == $DamageType::OutdoorDepTurret)
			{
				logEcho(%clVictim.nameBase@" (pl "@%clVictim.player@"/cl "@%clVictim@") killed by "@%controller.nameBase@" (pl "@%controller.player@"/cl "@%controller@") using spike turret");
			}
			else
				logEcho(%clVictim.nameBase@" (pl "@%clVictim.player@"/cl "@%clVictim@") killed by a turret controlled by "@%controller.nameBase);
		}
		// use the handle associated with the deployed object to verify valid owner
		else if (isObject(%implement.owner))
		{        
			%owner = %implement.owner;
			//error("Owner is " @ %owner @ "   Handle is " @ %implement.ownerHandle);
			//error("Turret is still owned");

			// turret is uncontrolled, but is owned - treat the same as controlled
			%killerGender = (%owner.sex $= "Male" ? 'him' : 'her');
			%killerPoss = (%owner.sex $= "Male" ? 'his' : 'her');          
			%killerName = %owner.name;

			if (%owner == %clVictim) // whoops... automated self-kill
				messageAll('msgTurretSelfKill', $DeathMessageTurretSelfKill[mFloor(getRandom() * $DeathMessageTurretSelfKillCount)],%victimName, %victimGender, %victimPoss, %killerName, %killerGender, %killerPoss, %damageType, $DamageTypeText[%damageType]);
			else if (%owner.team == %clVictim.team) // player got in the way of a teammate's deployed but uncontrolled turret
				messageAll('msgCTurretKill', $DeathMessageCTurretAccdtlKill[%damageType,mFloor(getRandom() * $DeathMessageCTurretAccdtlKillCount)],%victimName, %victimGender, %victimPoss, %killerName, %killerGender, %killerPoss, %damageType, $DamageTypeText[%damageType]);
			else // deployed, uncontrolled turret killed an enemy
				messageAll('msgCTurretKill', $DeathMessageCTurretKill[%damageType,mFloor(getRandom() * $DeathMessageCTurretKillCount)],%victimName, %victimGender, %victimPoss, %killerName, %killerGender, %killerPoss, %damageType, $DamageTypeText[%damageType]);

			if (%damageType == $DamageType::IndoorDepTurret)
			{
				logEcho(%clVictim.nameBase@" (pl "@%clVictim.player@"/cl "@%clVictim@") killed by "@%owner.nameBase@" (pl "@%owner.player@"/cl "@%owner@") using clamp turret (automated)");
			}
			else if (%damageType == $DamageType::OutdoorDepTurret)
			{
				logEcho(%clVictim.nameBase@" (pl "@%clVictim.player@"/cl "@%clVictim@") killed by "@%owner.nameBase@" (pl "@%owner.player@"/cl "@%owner@") using spike turret (automated)");
			}
			else
				logEcho(%clVictim.nameBase@" (pl "@%clVictim.player@"/cl "@%clVictim@") was killed by turret (automated)");
		}
		else // turret is not a placed (owned) turret (or owner is no longer on it's team), and is not being controlled
		{
			messageAll('msgTurretKill', $DeathMessageTurretKill[%damageType,mFloor(getRandom() * $DeathMessageTurretKillCount)],%victimName, %victimGender, %victimPoss, %killerName, %killerGender, %killerPoss, %damageType, $DamageTypeText[%damageType]);
			logEcho(%clVictim.nameBase@" (pl "@%clVictim.player@"/cl "@%clVictim@") killed by turret");
		}
	}
	else // fall through to the default parent function
		Parent::displayDeathMessages(%game, %clVictim, %clKiller, %damageType, %implement);
}

function DefaultGame::forceObserver(%game, %client, %reason)
{
	// make sure we have a valid client...
	if (%client <= 0)
		return;

	// kill this player
	if (%client.player)
		%client.player.scriptKill(0);

	if (%client.respawnTimer)
		cancel(%client.respawnTimer);

	%client.respawnTimer = "";

	// remove them from the team rank array
	%game.removeFromTeamRankArray(%client);

	// place them in observer mode
	%client.lastObserverSpawn = -1;
	%client.observerStartTime = getSimTime();
	%adminForce = 0;

	switch$ (%reason)
	{
		case "playerChoose":
			%client.camera.getDataBlock().setMode(%client.camera, "observerFly");
			messageClient(%client, 'MsgClientJoinTeam', '\c2You have become an observer.', %client.name, %game.getTeamName(0), %client, 0);
			logEcho(%client.nameBase@" (cl "@%client@") entered observer mode");
			%client.lastTeam = %client.team;

		case "AdminForce":
			%admin = $AdminCl.nameBase;
			%client.camera.getDataBlock().setMode(%client.camera, "observerFly");
			messageClient(%client, 'MsgClientJoinTeam', '\c2You have been forced into observer mode by %5.', %client.name, %game.getTeamName(0), %client, 0, %admin);
			logEcho(%client.nameBase@" (cl "@%client@") was forced into observer mode by" SPC %admin);
			%client.lastTeam = %client.team;
			%adminForce = 1;

			if ($Host::TournamentMode || $Host::PickupMode)
			{
				if (!$matchStarted)
				{   
					if (%client.camera.Mode $= "pickingTeam")
					{
						if (!$Host::PickupMode)
							commandToClient(%client, 'processPickTeam');
						clearBottomPrint(%client);
					}
					else
					{
						clearCenterPrint(%client);
						%client.notReady = true;
					}
				}
			}

		case "spawnTimeout":
			%client.camera.getDataBlock().setMode(%client.camera, "observerTimeout");
			messageClient(%client, 'MsgClientJoinTeam', '\c2You have been placed in observer mode due to delay in respawning.', %client.name, %game.getTeamName(0), %client, 0);
			logEcho(%client.nameBase@" (cl "@%client@") was placed in observer mode due to spawn delay");
			// save the team the player was on - only if this was a delay in respawning
			%client.lastTeam = %client.team;

		case "captainForce":
			messageClient(%client, 'MsgClientJoinTeam', '\c2You have become an observer.', %client.name, %game.getTeamName(0), %client, 0 );
			%client.lastTeam = %client.team;										 
	}

	// if pickup mode, remove captain attributes
	if ($Host::PickupMode && ($atfadmin_teamcaptain[%client.team] == %client))
	{
		$atfadmin_teamcaptain[%client.team] = "";
		%client.captainOfTeam = 0;
		%client.isCaptain = false;
		atfadmin_nextCaptain(); // force a refresh
	}

	// switch client to team 0 (observer)
	%client.team = 0;
	%client.player.team = 0;
	setTargetSensorGroup(%client.target, %client.team);
	%client.setSensorGroup(%client.team);

	// set their control to the obs. cam
	%client.setControlObject( %client.camera );
	commandToClient(%client, 'setHudMode', 'Observer');

	// display the hud
	updateObserverFlyHud(%client);

	// message everyone about this event
	if (!%adminForce)
		messageAllExcept(%client, -1, 'MsgClientJoinTeam', '\c2%1 has become an observer.', %client.name, %game.getTeamName(0), %client, 0);
	else
	{
		messageAllExcept(%client, -1, 'MsgClientJoinTeam', '\c2%5 has forced %1 to become an observer.', %client.name, %game.getTeamName(0), %client, 0, %admin);
		$AdminCl = "";
	}

	updateCanListenState(%client);

	// call the onEvent for this game type
	%game.onClientEnterObserverMode(%client); // Bounty uses this to remove this client from others' hit lists 
}

function DefaultGame::processGameLink(%game, %client, %targetClient, %arg2, %arg3, %arg4, %arg5)
{
	if ($Host::PickupMode && !$MatchStarted && !$CountdownStarted && %client.isCaptain && isObject(%targetClient))
	{
		// we're in pickup mode and the match hasn't started...
		// the client is a captain and the target exists

		%otherteam = %client.team == 1 ? 2 : 1;

		if (%targetClient.team == 0 && $atfadmin_PickingCaptain == %client)
		{
			// the target is waiting to be picked
			// it is this captain's turn to pick...
			// move the client to the captain's team
			%game.clientChangeTeam(%targetClient, %client.team, true);
			messageAll('MsgClient', '%1 has picked %2 for team %3.~wfx/misc/crowdtransition%4b.wav', %client.name, %targetClient.name, %game.getTeamName(%client.team), getRandom(1, 3));
			atfadmin_nextCaptain();
		}
		else if (%targetClient.team == %client.team && $atfadmin_PickingCaptain == %client && !$atfadmin_CaptainHasTraded[%client])
		{
			if (!$atfadmin_CaptainHasTraded[%client])
			{
				// the target is already on the captain's team
				// move the client to the other captain's team
				messageAll('MsgClient', '%1 has removed %2 from team %3.~wfx/Bonuses/Nouns/llama.wav', %client.name, %targetClient.name, %game.getTeamName(%client.team));
				%game.forceObserver(%targetClient, "captainForce");
				$atfadmin_CaptainHasTraded[%client] = true;
				atfadmin_nextCaptain();
			}
			else
			{
				messageClient(%client, 'MsgClient', 'You have already traded a player this round.~wfx/misc/misc.error.wav');
			}
		}
//		else
//		{
//			// the target is on the other captain's team
//			// request that the other captain give up this player
//			%gender = %client.sex $= "Male" ? 'his' : 'her';
//			messageClient($atfadmin_teamcaptain[%otherteam], 'msgClient', '\c2%1 requests that you switch %2 to %3 team.~wgui/vote_pass.wav', %client.name, %targetClient.name, %gender);
//		}
	}
	else if ((%client.team == 0) && isObject(%targetClient) && (%targetClient.team != 0))
	{
		// the default behavior when clicking on a game link is to start observing that client
		%prevObsClient = %client.observeClient;

		// update the observer list for this client
		observerFollowUpdate(%client, %targetClient, %prevObsClient !$= "");

		serverCmdObserveClient(%client, %targetClient);
		displayObserverHud(%client, %targetClient);

		if (%client.isAdmin && $atfadmin::HideAdminObserverMessages)
			return;

		if (%targetClient != %prevObsClient)
		{
			messageClient(%targetClient, 'Observer', '\c1%1 is now observing you.', %client.name);  
			messageClient(%prevObsClient, 'ObserverEnd', '\c1%1 is no longer observing you.', %client.name);  
		}
	}
}

function DefaultGame::voteShowdownSiege(%game, %admin)
{
	%setto = "";
	%cause = "";
	if(%admin)
	{
		if ($atfadmin_showdownSiegeEnabled)
		{
			messageAll('MsgAdminForce', '\c2%1 has disabled Showdown Siege.', $AdminCl.name);
			$atfadmin_showdownSiegeEnabled = false;
			%setto = "disabled";
		}
		else
		{
			messageAll('MsgAdminForce', '\c2%1 has enabled Showdown Siege.', $AdminCl.name);
			$atfadmin_showdownSiegeEnabled = true;
			%setto = "enabled";
		}
		%cause = "(admin:"@$AdminCl.nameBase@")";
		$AdminCl = "";
	}
	else
	{
		%totalVotes = %game.totalVotesFor + %game.totalVotesAgainst;
		if(%totalVotes > 0 && (%game.totalVotesFor / (ClientGroup.getCount() - $HostGameBotCount)) >= ($Host::VotePasspercent / 100))
		{
			if($atfadmin_showdownSiegeEnabled)
			{
				messageAll('MsgVotePassed', '\c2Showdown Siege was disabled by vote.');
				$atfadmin_ShowdownSiegeEnabled = false;
				%setto = "disabled";
			}
			else
			{
				messageAll('MsgVotePassed', '\c2Showdown Siege was enabled by vote.');
				$atfadmin_ShowdownSiegeEnabled = true;
				%setto = "enabled";
			}
			%cause = "(vote)";
		}
		else
		{
			if ($atfadmin_showdownSiegeEnabled)
				messageAll('MsgVoteFailed', '\c2Disable Showdown Siege vote did not pass: %1 percent.', mFloor(%game.totalVotesFor/(ClientGroup.getCount() - $HostGameBotCount) * 100));
			else
				messageAll('MsgVoteFailed', '\c2Enable Showdown Siege vote did not pass: %1 percent.', mFloor(%game.totalVotesFor/(ClientGroup.getCount() - $HostGameBotCount) * 100));
		}
	}
	if(%setto !$= "")
		logEcho("showdown siege" SPC %setto SPC %cause);
}

function DefaultGame::voteSkipMission(%game, %admin)
{
	if ($atfadmin_nextMissionID !$= "")
		%missionId = $atfadmin_nextMissionID;
	else if ($Host::TournamentMode && $atfadmin_missionQueue $= "")
		%missionId = atfadmin_validateMission($CurrentMission, $CurrentMissionType);
	else
		%missionId = atfadmin_findNextCycleMissionID();

	
	%missionDisplayName = $MissionList[%missionId, MissionDisplayName];
	%typeDisplayName = $MissionList[%missionId, TypeDisplayName];

	if (%admin)
	{
		messageAll('MsgAdminChangeMission', '\c2%3 has skipped to the next mission: %1 (%2).', %missionDisplayName, %typeDisplayName, $AdminCl.name );
		logEcho("mission skipped to "@%missionDisplayName@"/"@%typeDisplayName@" (admin:"@$AdminCl.nameBase@")");
		$AdminCl = "";
		%game.gameOver();
		CycleMissions();
	}
	else
	{
		%totalVotes = %game.totalVotesFor + %game.totalVotesAgainst;
		if(%totalVotes > 0 && (%game.totalVotesFor / (ClientGroup.getCount() - $HostGameBotCount)) >= ($Host::VotePasspercent / 100))
		{
			messageAll('MsgVotePassed', '\c2The mission was skipped to %1 (%2) by vote.', %missionDisplayName, %typeDisplayName );
			logEcho("mission skipped to "@%missionDisplayName@"/"@%typeDisplayName@" (vote)");
			%game.gameOver();
			CycleMissions();
		}
		else
			messageAll('MsgVoteFailed', '\c2Skip mission vote did not pass: %1 percent.', mFloor(%game.totalVotesFor/(ClientGroup.getCount() - $HostGameBotCount) * 100));
	}
}

function DefaultGame::voteMatchStart(%game, %admin)
{
	%cause = "";
	%ready = forceTourneyMatchStart();
	if (%admin)
	{
		if (!%ready)
		{
			messageClient(%client, 'msgClient', '\c2No players are ready yet.');
			return;
		}
		else
		{
			messageAll('msgMissionStart', '\c2%1 has forced the match to start.', $AdminCl.name);
			startTourneyCountdown();
		}
		%cause = "(admin:"@$AdminCl.nameBase@")";
		$AdminCl = "";
	}
	else
	{
		if (!%ready)
		{
			messageAll('msgClient', '\c2Vote passed to start match, but no players are ready yet.');
			return;
		}
		else
		{
			%totalVotes = %game.totalVotesFor + %game.totalVotesAgainst;
			if (%totalVotes > 0 && (%game.totalVotesFor / (ClientGroup.getCount() - $HostGameBotCount)) >= ($Host::VotePasspercent / 100))
			{
				messageAll('MsgVotePassed', '\c2The match has been started by vote: %1 percent.', mFloor(%game.totalVotesFor/(ClientGroup.getCount() - $HostGameBotCount) * 100));
				startTourneyCountdown();
			}
			else
				messageAll('MsgVoteFailed', '\c2Start Match vote did not pass: %1 percent.', mFloor(%game.totalVotesFor/(ClientGroup.getCount() - $HostGameBotCount) * 100));
		}
	}

	if (%cause !$= "")
		logEcho("start match" SPC %cause);
}

function DefaultGame::clearTeamRankArray(%game, %team)
{
	// let's improve upon this a bit...
	deleteVariables("$TeamRank" @ %team @ "_*");
	$TeamRank[%team, count] = 0;
}

function DefaultGame::startMatch(%game)
{
	echo("START MATCH");
	MessageAll('MsgMissionStart', "\c2Match started!");

	// the match has been started... clear the team rank array
	for (%i = 0; %i < 32; %i++)
		%game.clearTeamRankArray(%i);

	$MatchStarted = true;

	%game.clearDeployableMaxes();

	$missionStartTime = getSimTime();
	%curTimeLeftMS = ($Host::TimeLimit * 60 * 1000);

	// schedule first timeLimit check for 20 seconds
	if (%game.class !$= "SiegeGame")
		%game.timeCheck = %game.schedule(20000, "checkTimeLimit");

	// schedule the end of match countdown
	EndCountdown($Host::TimeLimit * 60 * 1000);

	// reset everyone's score and add them to the team rank array
	for (%i = 0; %i < ClientGroup.getCount(); %i++)
	{
		%cl = ClientGroup.getObject(%i);
		%game.resetScore(%cl);
		%game.populateTeamRankArray(%cl);
	}

	// set all clients control to their player
	%count = ClientGroup.getCount();
	for (%i = 0; %i < %count; %i++)
	{
		%cl = ClientGroup.getObject(%i);

		// Siege will set the clock differently
		if (%game.class !$= "SiegeGame")
			messageClient(%cl, 'MsgSystemClock', "", $Host::TimeLimit, %curTimeLeftMS);

		if (!$Host::TournamentMode && !$Host::PickupMode && %cl.matchStartReady && %cl.camera.mode $= "pre-game")
		{
			commandToClient(%cl, 'setHudMode', 'Standard');
			%cl.setControlObject( %cl.player );
		}
		else
		{
			if (%cl.matchStartReady)
			{
				if (%cl.camera.mode $= "pre-game")
				{
					%cl.observerMode = "";
					commandToClient(%cl, 'setHudMode', 'Standard');

					if (isObject(%cl.player))
						%cl.setControlObject(%cl.player);
					else
						echo("can't set control for client: " @ %cl @ ", no player object found!");
				}
				else
					%cl.observerMode = "observerFly";
			}
		}
	}

	// on with the show this is it!
	AISystemEnabled(true);
}


function DefaultGame::missionLoadDone(%game)
{
	if ($atfadmin::EnableStats && !$Host::TournamentMode)
		atfadmin_initStats();

	Parent::missionLoadDone(%game);

	if ($atfadmin_nextMissionID !$= "" && $atfadmin_missionQueue $= "")
		messageAll(0, '\c2After this mission, the regular map rotation will resume with %1 (%2).', $Missionlist[$atfadmin_nextMissionID, MissionDisplayName], $Missionlist[$atfadmin_nextMissionID, TypeDisplayName]);
	else if ($atfadmin_missionQueue !$= "")
		atfadmin_showMissionQueue();
}

function atfadmin_showMissionQueue(%client)
{
	%count = getFieldCount($atfadmin_missionQueue);
	if (%count <= 0) return;

	if (%client $= "")
		messageAll(0, '\c2After this mission, the following mission(s) will be played:');
	else if (!isObject(%client))
		return;
	else
		messageClient(%client, 'MsgClient', '\c2After this mission, the following mission(s) will be played:');

	for (%i = 0; %i < %count; %i++)
	{
		%missionID = getField(getRecord($atfadmin_missionQueue, 0), %i);
		if (%client $= "")
			messageAll(0, '\c2%1. %2 (%3)', %i + 1, $MissionList[%missionID, MissionDisplayName], $MissionList[%missionID, TypeDisplayName]);
		else
			messageClient(%client, 'MsgClient', '\c2%1. %2 (%3)', %i + 1, $MissionList[%missionID, MissionDisplayName], $MissionList[%missionID, TypeDisplayName]);
	}
}

//
//
// MOD HUD stuff
//
//

function serverCMDModHudInitialize(%client, %value)
{
	Game.InitModHud(%client, %value);
}

function serverCmdModUpdateSettings(%client, %option, %value)
{
	// %option is the index # of the hud list option
	// %value is the index # of the hud list setting

	%option = deTag(%option);
	%value = deTag(%value);
	Game.UpdateModHudSet(%client, %option, %value);
}

function serverCmdModButtonSet(%client, %button, %value)
{
	%button = deTag(%button);
	%value = deTag(%value);
	Game.ModButtonCmd(%client, %button, %value);
}

function DefaultGame::InitModHud(%game, %client, %value)
{
	return;

	// Clear out any previous settings
	commandToClient(%client, 'InitializeModHud', "atfadmin");

	// Send the hud labels                 |  Hud Label  |  | Option label | | Setting label |
	commandToClient(%client, 'ModHudHead', "atfadmin hud",      "Option:",       "Setting:");

	// Send the Option list and settings per option    | Option |    | Setting |
	//commandToClient(%client, 'ModHudPopulate', "Example1", "Empty");
	//commandToClient(%client, 'ModHudPopulate', "Example2", "Setting1", "Setting2", "Setting3", "Setting4", "Setting5", "Setting6", "Setting7", "Setting8", "Setting9", "Setting10");

	commandToClient(%client, 'ModHudPopulate', "Allow Showdown Siege", "True", "False");
	commandToClient(%client, 'ModHudPopulate', "Allow Admin Skip Mission", "True", "False");
	commandToClient(%client, 'ModHudPopulate', "Allow Admin Team Damage", "True", "False");

	// Send the button labels and visual settings  |  Button  |  | Label |  | Visible |  | Active |
	//commandToClient(%client, 'ModHudBtn1', "Apply", true, true);
	//commandToClient(%client, 'ModHudBtn2', "Close", true, true);

	// We're done!
	commandToClient(%client, 'ModHudDone');
}

function DefaultGame::UpdateModHudSet(%game, %client, %option, %value)
{
	return;

	// 1 = Example1
	// 2 = Example2

	switch (%option)
	{
		case 1:
			%msg = '\c2Something set to: %2 %3 (Showdown Siege).';

		case 2:
			%msg = '\c2Something set to: %2 %3 (Skip Mission).';

		case 3:
			%msg = '\c2Something set to: %2 %3 (Team Damage).';

		default:
			%msg = '\c2Invalid setting.';
	}
	messageClient(%client, 'MsgModHud', %msg, %option, %value);
}

function DefaultGame::ModButtonCmd(%game, %client, %button, %value)
{
	return;

	// 11 = Button 1
	// 12 = Button 2
	// 13 = Button 3
	// 14 = Button 4

	switch (%button)
	{
		case 11:
			%msg = '\c2Button 1 set to: %2 %3.';

		case 12:
			%msg = '\c2Button 2 set to: %2 %3.';

		case 13:
			%msg = '\c2Button 3 set to: %2 %3.';

		case 14:
			%msg = '\c2Button 4 set to: %2 %3.';

		default:
			%msg = '\c2Invalid setting.';
	}
	messageClient(%client, 'MsgModHud', %msg, %button, %value);
}

function DefaultGame::evalVote(%game, %typeName, %admin, %arg1, %arg2, %arg3, %arg4)
{
	if (%typeName $= "VoteSkipMission")
		%game.voteSkipMission(%admin, %arg1, %arg2, %arg3, %arg4);
	else
		Parent::evalVote(%game, %typeName, %admin, %arg1, %arg2, %arg3, %arg4);

	switch$ (%typeName)
	{
		case "VoteGlobalMutePlayer":
			%game.voteGlobalMutePlayer(%admin, %arg1, %arg2, %arg3, %arg4);
		case "VoteShowdownSiege":
			%game.voteShowdownSiege(%admin, %arg1, %arg2, %arg3, %arg4);
		case "VoteToggleAssetTracking":
			%game.voteToggleAssetTracking(%admin, %arg1, %arg2, %arg3, %arg4);
		case "VotePickupMode":
			%game.votePickupMode(%admin, %arg1, %arg2, %arg3, %arg4);
		case "VoteCaptainPlayer":
			%game.voteCaptainPlayer(%admin, %arg1, %arg2, %arg3, %arg4);
		case "VoteEnqueueMission":
			%game.voteEnqueueMission(%admin, %arg1, %arg2, %arg3, %arg4);
		case "VoteDequeueMission":
			%game.voteDequeueMission(%admin, %arg1, %arg2, %arg3, %arg4);
	}
}

function DefaultGame::updateScoreHud(%game, %client, %tag)
{
	if (%game.numTeams > 1)
	{
		// use a different function for pickup mode
		if ($Host::PickupMode && !$MatchStarted)
		{
			atfadmin_updatePickupModeScoreHud(%client, %tag);
			return;
		}

		// header
		messageClient(%client, 'SetScoreHudHeader', "", '<tab:15,315>\t%1<rmargin:260><just:right>%2<rmargin:560><just:left>\t%3<just:right>%4', %game.getTeamName(1), $TeamScore[1], %game.getTeamName(2), $TeamScore[2]);

		// subheader
		messageClient(%client, 'SetScoreHudSubheader', "", '<tab:15,315>\tPLAYERS (%1)<rmargin:260><just:right>SCORE<rmargin:560><just:left>\tPLAYERS (%2)<just:right>SCORE', $TeamRank[1, count], $TeamRank[2, count]);

		%index = 0;
		while (true)
		{
			if (%index >= $TeamRank[1, count]+2 && %index >= $TeamRank[2, count]+2)
				break;

			// get the team1 client info
			%team1Client = "";
			%team1ClientScore = "";
			%col1Style = "";
			if (%index < $TeamRank[1, count])
			{
				%team1Client = $TeamRank[1, %index];
				%team1ClientScore = %team1Client.score $= "" ? 0 : %team1Client.score;
				%col1Style = %team1Client == %client ? "<color:dcdcdc>" : "";
				%team1playersTotalScore += %team1Client.score;
			}
			else if (%index == $teamRank[1, count] && $teamRank[1, count] != 0 && %game.class $= "CTFGame")
			{
				%team1ClientScore = "--------------";
			}
			else if (%index == $teamRank[1, count]+1 && $teamRank[1, count] != 0 && %game.class $= "CTFGame")
			{
				%team1ClientScore = %team1playersTotalScore != 0 ? %team1playersTotalScore : 0;
			}

			// get the team2 client info
			%team2Client = "";
			%team2ClientScore = "";
			%col2Style = "";
			if (%index < $TeamRank[2, count])
			{
				%team2Client = $TeamRank[2, %index];
				%team2ClientScore = %team2Client.score $= "" ? 0 : %team2Client.score;
				%col2Style = %team2Client == %client ? "<color:dcdcdc>" : "";
				%team2playersTotalScore += %team2Client.score;
			}
			else if (%index == $teamRank[2, count] && $teamRank[2, count] != 0 && %game.class $= "CTFGame")
			{
				%team2ClientScore = "--------------";
			}
			else if (%index == $teamRank[2, count]+1 && $teamRank[2, count] != 0 && %game.class $= "CTFGame")
			{
				%team2ClientScore = %team2playersTotalScore != 0 ? %team2playersTotalScore : 0;
			}

			// if the client is not an observer, send the message
			if (%client.team != 0)
			{
				messageClient(%client, 'SetLineHud', "", %tag, %index, '<tab:20,320>\t<spush>%5<clip:200>%1</clip><rmargin:260><just:right>%2<spop><rmargin:560><just:left>\t%6<clip:200>%3</clip><just:right>%4', %team1Client.name, %team1ClientScore, %team2Client.name, %team2ClientScore, %col1Style, %col2Style);
			}
			// else for observers, create an anchor around the player name so they can be observed
			else
			{
				messageClient(%client, 'SetLineHud', "", %tag, %index, '<tab:20,320>\t<spush>%5<clip:200><a:gamelink\t%7>%1</a></clip><rmargin:260><just:right>%2<spop><rmargin:560><just:left>\t%6<clip:200><a:gamelink\t%8>%3</a></clip><just:right>%4', %team1Client.name, %team1ClientScore, %team2Client.name, %team2ClientScore, %col1Style, %col2Style, %team1Client, %team2Client);
			}

			%index++;
		}
	}
	else
	{
		// for single team games, just use the original function
		Parent::updateScoreHud(%game, %client, %tag);
		return;
	}

	// tack on the list of observers
	%observerCount = 0;
	for (%i = 0; %i < ClientGroup.getCount(); %i++)
	{
		%cl = ClientGroup.getObject(%i);
		if (%cl.team == 0)
			%observerCount++;
	}

	if (%observerCount > 0)
	{
		messageClient( %client, 'SetLineHud', "", %tag, %index, "");
		%index++;
		messageClient(%client, 'SetLineHud', "", %tag, %index, '<tab:10, 310><spush><font:Univers Condensed:22>\tOBSERVERS (%1)<rmargin:260><just:right>TIME<spop>', %observerCount);
		%index++;
		for (%i = 0; %i < ClientGroup.getCount(); %i++)
		{
			%cl = ClientGroup.getObject(%i);
			//if this is an observer
			if (%cl.team == 0)
			{
				%obsTime = getSimTime() - %cl.observerStartTime;
				%obsTimeStr = %game.formatTime(%obsTime, false);
				messageClient(%client, 'SetLineHud', "", %tag, %index, '<tab:20, 310>\t<clip:150>%1</clip><rmargin:260><just:right>%2', %cl.name, %obsTimeStr);
				%index++;
			}
		}
	}

	// clear the rest of hud to remove old lines
	messageClient(%client, 'ClearHud', "", %tag, %index);
}

function atfadmin_updatePickupModeScoreHud(%client, %tag)
{
	// leave if we shouldn't be here
	if (!$Host::PickupMode || $MatchStarted || Game.numTeams < 2) return;

	// build header
	messageClient(%client, 'SetScoreHudHeader', "", '<tab:15,315>\t%1<rmargin:560><just:left>\t%2', Game.getTeamName(1), Game.getTeamName(2));

	// build subheader
	messageClient(%client, 'SetScoreHudSubheader', "", '<tab:15,315>\tPLAYERS (%1)<lmargin:250><just:left>STATUS<rmargin:560><just:left>\tPLAYERS (%2)<lmargin:450><just:left>STATUS', ($TeamRank[1, count] $= "") ? 0 : $TeamRank[1, count], ($TeamRank[2, count] $= "") ? 0 : $TeamRank[2, count]);

	// loop through all teamed players
	%index = 0;
	while (true)
	{
		if (%index >= $TeamRank[1, count]+2 && %index >= $TeamRank[2, count]+2)
			break;

		// get the team client info
		for (%i = 1; %i <= 2; %i++)
		{
			%teamclient[%i] = "";
			%style[%i] = "";
			%status[%i] = "";

			if (%index < $TeamRank[%i, count])
			{
				%teamclient[%i] = $TeamRank[%i, %index];
				%status[%i] = %teamclient[%i].notReady ? "NOT READY" : "READY";

				if (%teamclient[%i] == %client)
					%style[%i] = "<color:dcdcdc>";
				else if (%teamclient[%i].isCaptain)
					%style[%i] = "<color:ff00ff>";
				else if (%teamclient[%i].notReady)
					%style[%i] = "<color:ffff00>";
			}
		}

		if (%client.isCaptain)
		{
			// add a special link if the client is a captain on the player's team
			if (%client.team == 1)
			{
				messageClient(%client, 'SetLineHud', "", %tag, %index, '<tab:20,320>\t<spush>%5<clip:200><a:gamelink\t%7>%1</a></clip><rmargin:260><just:right>%2<spop><rmargin:560><just:left>\t%6<clip:200>%3</clip><just:right>%4', %teamclient[1].name, %status[1], %teamclient[2].name, %status[2], %style[1], %style[2], %teamclient[1]);
			}
			else // %client.team == 2
			{
				messageClient(%client, 'SetLineHud', "", %tag, %index, '<tab:20,320>\t<spush>%5<clip:200>%1</clip><rmargin:260><just:right>%2<spop><rmargin:560><just:left>\t%6<clip:200><a:gamelink\t%7>%3</a></clip><just:right>%4', %teamclient[1].name, %status[1], %teamclient[2].name, %status[2], %style[1], %style[2], %teamclient[2]);
			}
		}
		else
		{
			// regular players see this
			messageClient(%client, 'SetLineHud', "", %tag, %index, '<tab:20,320>\t<spush>%5<clip:200>%1</clip><rmargin:260><just:right>%2<spop><rmargin:560><just:left>\t%6<clip:200>%3</clip><just:right>%4', %teamclient[1].name, %status[1], %teamclient[2].name, %status[2], %style[1], %style[2]);
		}

		%index++;
	}

	// tack on the list of observers
	%observerCount = 0;
	for (%i = 0; %i < ClientGroup.getCount(); %i++)
	{
		%cl = ClientGroup.getObject(%i);
		if (%cl.team == 0)
			%observerCount++;
	}

	// observers
	if (atfadmin_getPlayerCount(0) > 0)
	{
		messageClient(%client, 'SetLineHud', "", %tag, %index, "");
		%index++;
		messageClient(%client, 'SetLineHud', "", %tag, %index, '<tab:10, 310><spush><font:Univers Condensed:22>\tOBSERVERS (%1)<rmargin:260><just:right>TIME<spop>', %observerCount);
		%index++;
		for (%i = 0; %i < ClientGroup.getCount(); %i++)
		{
			%cl = ClientGroup.getObject(%i);
			// if this is an observer
			if (%cl.team == 0)
			{
				%obsTime = getSimTime() - %cl.observerStartTime;
				%obsTimeStr = Game.formatTime(%obsTime, false);
				messageClient(%client, 'SetLineHud', "", %tag, %index, '<tab:20, 310>\t<clip:150>%1</clip><rmargin:260><just:right>%2', %cl.name, %obsTimeStr);
				%index++;
			}
		}
	}

	// clear the rest of hud to remove old lines
	messageClient(%client, 'ClearHud', "", %tag, %index);
}


};
// END package atfadmin_defaultGame
