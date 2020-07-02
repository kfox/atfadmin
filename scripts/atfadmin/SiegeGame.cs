// START package atfadmin_SiegeGame
package atfadmin_SiegeGame
{


function SiegeGame::assignClientTeam(%game, %client, %respawn)
{
	Parent::assignClientTeam(%game, %client, %respawn);

	// display round info
	if ($atfadmin::ShowSiegeInfo && !$Host::PickupMode && !$Host::TournamentMode)
		atfadmin_showSiegeInfo(%game, %client);
}

function SiegeGame::clientChangeTeam(%game, %client, %team, %fromObs)
{
	Parent::clientChangeTeam(%game, %client, %team, %fromObs);

	messageClient(%client, 'MsgCheckTeamLines', "", %client.team);

	// display round info
	if ($atfadmin::ShowSiegeInfo && !$Host::PickupMode && !$Host::TournamentMode)
		atfadmin_showSiegeInfo(%game, %client);
}

function atfadmin_showSiegeInfo(%game, %client)
{
	// no info for observers
	if (%client.team == 0)
		return;

	// get the round
	%round = %game.siegeRound;

	if ($atfadmin_showdownSiegeEnabled)
		%total = "many";
	else
		%total = "2";

	// get the team info
	%defTeam = Game.offenseTeam == 1 ? 2 : 1;

	if (%client.team == %defTeam)
	{
		%message = "<color:ffffff>You are in mission: <color:dddd00>"@$MissionDisplayName@" (Siege)\n<color:ffffff>You must <color:00dd00>DEFEND<color:ffffff> your team's base.\n<color:ffffff>This is round "@%round@" of "@%total@".";
	}
	else
	{
		%message = "<color:ffffff>You are in mission: <color:dddd00>"@$MissionDisplayName@" (Siege)\n<color:ffffff>You must <color:dd0000>CAPTURE<color:ffffff> the enemy base.\n<color:ffffff>This is round "@%round@" of "@%total@".";
	}

	centerPrint(%client, %message, 8, 3);
}

function SiegeGame::startMatch(%game)
{
	Parent::startMatch(%game);

	if (atfadmin_isOnShowdownSiegeList())
	{
		$atfadmin_showdownSiegeEnabled = true;
		$atfadmin_disableShowdownSiege = true;
	}

	if ($atfadmin_showdownSiegeEnabled)
		messageAll(0, '\c2Showdown Siege is currently enabled. This is round %1.', %game.siegeRound);
}

function SiegeGame::gameOver(%game)
{
	Parent::gameOver(%game);

	if ($atfadmin::ResetShowdownSiege)
		$atfadmin_disableShowdownSiege = true;

	if ($atfadmin_disableShowdownSiege)
	{
		$atfadmin_showdownSiegeEnabled = false;
		$atfadmin_disableShowdownSiege = false;
	}
}

function atfadmin_isOnShowdownSiegeList()
{
	// if the current mission type isn't Siege,
	// don't bother checking
	if ($MissionTypeDisplayName !$= "Siege")
		return false;

	// if the Showdown Siege list is empty, give up early
	if (!%totalRecords = getFieldCount($atfadmin::ShowdownSiegeList))
		return false;

	// check if the current mission is on the list
	for (%i = 0; %i < %totalRecords; %i++)
	{
		%record = getField(getRecord($atfadmin::ShowdownSiegeList, 0), %i);
		if (%record $= $MissionName || %record $= $MissionDisplayName)
			return true; // yup, it is
	}

	// mission is not on the list
	return false;
}

function SiegeGame::missionLoadDone(%game)
{
	Parent::missionLoadDone(%game);
	%game.siegeRound = 1;
}

function SiegeGame::shouldSwitchSides(%game)
{
	%defTeam = %game.offenseTeam == 1 ? 2 : 1;
	if (%game.siegeRound == 1 || ($atfadmin_showdownSiegeEnabled && %game.siegeRound > 1 && $teamScore[%game.offenseTeam] > 0 && $teamScore[%game.offenseTeam] < $teamScore[%defTeam]))
		return true;
	else
		return false;
}

function SiegeGame::dropFlag(%game, %player)
{
	// say bye-bye to a little more console spam
}

function SiegeGame::allObjectivesCompleted(%game)
{
	Cancel(%game.timeSync);
	Cancel(%game.timeThread);
	cancelEndCountdown();

	//store the elapsed time in the teamScore array...
	$teamScore[%game.offenseTeam] = getSimTime() - %game.startTimeMS;
	messageAll('MsgSiegeCaptured', '\c2Team %1 captured the base in %2!', $teamName[%game.offenseTeam], %game.formatTime($teamScore[%game.offenseTeam], true));

	//set the new timelimit
	%game.timeLimitMS = $teamScore[%game.offenseTeam];

	if (%game.shouldSwitchSides())
	{
		// it's halftime, let everyone know
		messageAll('MsgSiegeHalftime');
	}
	else
	{
		// game is over
		messageAll('MsgSiegeMisDone', '\c2Mission complete.');
	}
	logEcho("objective completed in "@%game.timeLimitMS);

	// check for a new high score
	if ($atfadmin::EnableStats && atfstats_isNewHighScore($teamScore[%game.offenseTeam]))
	{
		// yup, it's a high score, so add it to the top 5
		%team = %game.offenseTeam;
		atfstats_addNewHighScore(%game.capPlayer[%team], $teamScore[%team]);
	}

	// setup the next round...
	%game.schedule(0, halftime, 'objectives');
}

function SiegeGame::halftime(%game, %reason)
{
	//stop the game and the bots
	$MatchStarted = false;
	AISystemEnabled(false);

	if (%game.shouldSwitchSides())
	{
		// switch sides
		%game.switchSides(%game);

		// increment the round counter
		%game.siegeRound++;
	}
	else
	{
		// let's wrap it all up
		%game.gameOver();
		cycleMissions();
	}
}

function SiegeGame::switchSides(%game)
{
	// switch the game variables
	%game.firstHalf = %game.firstHalf == false ? true : false;
	%oldOffenseTeam = %game.offenseTeam;
	%game.offenseTeam = %game.offenseTeam == 1 ? 2 : 1;

	// send the message
	messageAll('MsgSiegeRolesSwitched', '\c2Team %1 is now on offense.', $teamName[%game.offenseTeam], %game.offenseTeam);

	// reset stations and vehicles that players were using
	%game.resetPlayers();

	// zero out the counts for deployable items (found in defaultGame.cs)
	%game.clearDeployableMaxes();

	// z0dd - ZOD, 5/17/02. Clean up deployables triggers, function in supportClassic.cs
	cleanTriggers(nameToID("MissionCleanup/Deployables"));

	// clean up the MissionCleanup group - note, this includes deleting all the player objects
	%clean = nameToID("MissionCleanup");
	%clean.housekeeping();

	// non-static objects placed in original position
	resetNonStaticObjPositions();

	// switch the teams for objects belonging to the teams
	%group = nameToID("MissionGroup/Teams");
	%group.swapTeams();

	// search for vehicle pads also
	%mcg = nameToID("MissionCleanup");
	%mcg.swapVehiclePads();

	// restore the objects
	%game.restoreObjects();

	%count = ClientGroup.getCount();
	for(%cl = 0; %cl < %count; %cl++)
	{
		%client = ClientGroup.getObject(%cl);
		if( !%client.isAIControlled() )
		{
			// put everybody in observer mode
			%client.camera.getDataBlock().setMode( %client.camera, "observerStaticNoNext" );
			%client.setControlObject( %client.camera );

			// send the halftime result info
			if ( %client.team == %oldOffenseTeam )
			{
				if ( $teamScore[%oldOffenseTeam] > 0 )
					messageClient( %client, 'MsgSiegeResult', "", '%1 captured the %2 base in %3!', %game.capPlayer[%oldOffenseTeam], $teamName[%game.offenseTeam], %game.formatTime( $teamScore[%oldOffenseTeam], true ) );
				else
					messageClient( %client, 'MsgSiegeResult', "", 'Your team failed to capture the %1 base.', $teamName[%game.offenseTeam] );   
			}
			else if ( $teamScore[%oldOffenseTeam] > 0 )
				messageClient( %client, 'MsgSiegeResult', "", '%1 captured your base in %2!', %game.capPlayer[%oldOffenseTeam], %game.formatTime( $teamScore[%oldOffenseTeam], true ) );
			else
				messageClient( %client, 'MsgSiegeResult', "", 'Your team successfully held off team %1!', $teamName[%oldOffenseTeam] );   

			// list out the team rosters
			messageClient( %client, 'MsgSiegeAddLine', "", '<spush><color:00dc00><font:univers condensed:18><clip%%:50>%1</clip><lmargin%%:50><clip%%:50>%2</clip><spop>', $TeamName[1], $TeamName[2] );
			%max = $TeamRank[1, count] > $TeamRank[2, count] ? $TeamRank[1, count] : $TeamRank[2, count];
			for ( %line = 0; %line < %max; %line++ )
			{
				%plyr1 = $TeamRank[1, %line] $= "" ? "" : $TeamRank[1, %line].name;
				%plyr2 = $TeamRank[2, %line] $= "" ? "" : $TeamRank[2, %line].name;
				messageClient( %client, 'MsgSiegeAddLine', "", '<lmargin:0><clip%%:50> %1</clip><lmargin%%:50><clip%%:50> %2</clip>', %plyr1, %plyr2 );
			}

			// show observers
			%header = false;
			for ( %i = 0; %i < %count; %i++ )
			{
				%obs = ClientGroup.getObject( %i );
				if ( %obs.team <= 0 )
				{
					if ( !%header )
					{
						messageClient( %client, 'MsgSiegeAddLine', "", '\n<lmargin:0><spush><color:00dc00><font:univers condensed:18>OBSERVERS<spop>' );
						%header = true;
					}

					messageClient( %client, 'MsgSiegeAddLine', "", ' %1', %obs.name );
				}
			}

			commandToClient( %client, 'SetHalftimeClock', $Host::Siege::Halftime / 60000 );

			// get the HUDs right
			commandToClient( %client, 'setHudMode', 'SiegeHalftime' );
			commandToClient( %client, 'ControlObjectReset' );

			clientResetTargets(%client, true);
			%client.notReady = true;
		}
	}

	%game.schedule($Host::Siege::Halftime, halftimeOver);
}

function SiegeGame::halftimeOver(%game)
{
	// why, oh why, did the devs choose to
	// use a separate countdown timer for siege?
	$CountdownStarted = false;

	// drop all players into mission
	%game.dropPlayers();

	// setup the AI for the second half
	%game.aiHalfTime();

	if (!$Host::TournamentMode || !$atfadmin::EnableExtendedSiegeTourneyHalftime)
		%game.halfTimeCountDown($Host::warmupTime);

	// redo the objective waypoints
	%game.findObjectiveWaypoints();
}

function SiegeGame::dropPlayers(%game)
{
	%count = ClientGroup.getCount();
	for (%cl = 0; %cl < %count; %cl++)
	{
		%client = ClientGroup.getObject(%cl);
		if (!%client.isAIControlled())
		{
			if ($Host::TournamentMode)
			{
				if (%client.team == 0)
				{
					// TODO ?
					%client.camera.getDataBlock().setMode(%client.camera, "justJoined");
				}
				else
				{
					%game.spawnPlayer(%client, false);

					%client.observerMode = "pregame";
					%client.notReady = true;
					%client.notReadyCount = "";

					%client.camera.getDataBlock().setMode(%client.camera, "pre-game", %client.player);
					commandToClient(%client, 'setHudMode', 'Observer');
					%client.setControlObject(%client.camera);

					centerprint(%client, "\nPress FIRE when ready.", 0 , 3);
				}
			}
			else
			{
				// keep observers in observer mode
				if (%client.team == 0)
					%client.camera.getDataBlock().setMode(%client.camera, "justJoined");
				else
				{
					%game.spawnPlayer(%client, false);

					%client.camera.getDataBlock().setMode(%client.camera, "pre-game", %client.player);
					%client.setControlObject(%client.camera);
					%client.notReady = false;
				}
			}
		}
	}
}

function SiegeGame::halfTimeCountDown(%game, %time)
{
	%game.secondHalfCountDown = true;
	$CountdownStarted = true;
	$MatchStarted = false;

	%timeMS = %time * 1000;
	%game.schedule(%timeMS, "startSecondHalf");
	notifyMatchStart(%timeMS);

	if (%timeMS > 30000)
		schedule(%timeMS - 30000, 0, "notifyMatchStart", 30000);
	if (%timeMS > 15000)
		schedule(%timeMS - 15000, 0, "notifyMatchStart", 15000);
	if (%timeMS > 10000)
		schedule(%timeMS - 10000, 0, "notifyMatchStart", 10000);
	if (%timeMS > 5000)
		schedule(%timeMS - 5000, 0, "notifyMatchStart", 5000);
	if (%timeMS > 4000)
		schedule(%timeMS - 4000, 0, "notifyMatchStart", 4000);
	if (%timeMS > 3000)
		schedule(%timeMS - 3000, 0, "notifyMatchStart", 3000);
	if (%timeMS > 2000)
		schedule(%timeMS - 2000, 0, "notifyMatchStart", 2000);
	if (%timeMS > 1000)
		schedule(%timeMS - 1000, 0, "notifyMatchStart", 1000);
}

function checkSiegeTourneyMatchStart()
{
	if (Game.secondHalfCountdown || $MatchStarted)
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
			MessageAll( 'msgHoldingUp', '\c1%1 is holding things up!', %notReady[0].name);
		else if (%notReadyCount < 4)
		{
			for (%i = 0; %i < %notReadyCount - 2; %i++)
				%str = getTaggedString(%notReady[%i].name) @ ", " @ %str;

			%str = "\c2" @ %str @ getTaggedString(%notReady[%i].name) @ " and " @ getTaggedString(%notReady[%i+1].name) 
				@ " are holding things up!";
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

		Game.halfTimeCountDown(30);
	}
}

function SiegeGame::startSecondHalf(%game)
{
	Parent::startSecondHalf(%game);
	if ($atfadmin_showdownSiegeEnabled)
		messageAll(0, '\c2Showdown Siege is currently enabled. This is round %1.', %game.siegeRound);
}

function SiegeGame::vehicleDestroyed(%game, %vehicle, %destroyer)
{
	// vehicle name
	%data = %vehicle.getDataBlock();
	%vehicleType = getTaggedString(%data.targetTypeTag);
	if (%vehicleType !$= "MPB")
		%vehicleType = strlwr(%vehicleType);

	%enemyTeam = (%destroyer.team == 1) ? 2 : 1;

	// what destroyed this vehicle
	if (%destroyer.client)
	{
		// it was a player, or his mine, satchel, whatever...
		%destroyer = %destroyer.client;
	}
	else if (%destroyer.getClassName() $= "Turret")
	{
		if (%destroyer.getControllingClient())
		{
			// manned turret
			%destroyer = %destroyer.getControllingClient();
		}
		else
		{
			%destroyerName = "A turret";
		}
	}
	else if (%destroyer.getDataBlock().catagory $= "Vehicles")
	{
		// Vehicle vs vehicle kill!
		if (%name $= "BomberFlyer" || %name $= "AssaultVehicle")
			%gunnerNode = 1;
		else
			%gunnerNode = 0;

		if (%destroyer.getMountNodeObject(%gunnerNode))
			%destroyer = %destroyer.getMountNodeObject(%gunnerNode).client;
	}
	else  // Is there anything else we care about?
		return;

	if (%destroyerName $= "")
		%destroyerName = %destroyer.name;

	if (%vehicle.team == %destroyer.team) // team kill
	{
		%pref = (%vehicleType $= "Assault Tank") ? "an" : "a";
		messageAll( 'msgVehicleTeamDestroy', '\c0%1 TEAMKILLED %3 %2!', %destroyerName, %vehicleType, %pref);
	}
	else // legit kill
	{
		teamDestroyMessage(%destroyer, 'msgVehDestroyed', '\c5%1 destroyed an enemy %2!', %destroyerName, %vehicleType);
		messageTeam(%enemyTeam, 'msgVehicleDestroy', '\c0%1 destroyed your team\'s %2.', %destroyerName, %vehicleType);
	}
}

function teamDestroyMessage(%client, %msgType, %msgString, %a1, %a2, %a3, %a4, %a5, %a6)
{
	%team = %client.team;
	%count = ClientGroup.getCount();
	for(%i = 0; %i < %count; %i++)
	{
		%recipient = ClientGroup.getObject(%i);
		if ((%recipient.team == %team) && (%recipient != %client))
			commandToClient(%recipient, 'TeamDestroyMessage', %msgType, %msgString, %a1, %a2, %a3, %a4, %a5, %a6);
	}
}

function SiegeGame::voteChangeTimeLimit(%game, %admin, %newLimit)
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
			%elapsedTimeMS = getSimTime() - %game.startTimeMS;
			%game.timeLimitMS = $Host::TimeLimit * 60 * 1000;
			%curTimeLeftMS = %game.timeLimitMS - %elapsedTimeMS;
			CancelEndCountdown();
			EndCountdown(%curTimeLeftMS);
			cancel(%game.timeSync);
			cancel(%game.timeThread);

			if (%curTimeLeftMS > 0)
				%game.timeThread = %game.schedule(%curTimeLeftMS, "timeLimitReached");

			%game.checkTimeLimit(true);
		}
	}
}

function SiegeGame::checkTimeLimit(%game, %forced)
{
	// prevent extra checks
	if (%forced && %game.timeSync !$= "")
		cancel(%game.timeSync);

	Parent::checkTimeLimit(%game, %forced);
}

function SiegeGame::updateScoreHud(%game, %client, %tag)
{
	// use a different function for pickup mode
	if ($Host::PickupMode && !$MatchStarted)
	{
		atfadmin_updatePickupModeScoreHud(%client, %tag);
		return;
	}
	else
	{
		Parent::updateScoreHud(%game, %client, %tag);
	}
}


};
// END package atfadmin_SiegeGame
