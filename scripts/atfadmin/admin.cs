// START package atfadmin_admin
package atfadmin_admin
{


function atfadmin_AdminCommand(%client, %typename, %arg1, %arg2, %arg3, %arg4)
{
	if (!%client.isAdmin) return;

	if (Game.scheduleVote !$= "" && Game.voteType $= %typeName)
	{
		messageAll('closeVoteHud', "");
		cancel(Game.scheduleVote);
		Game.scheduleVote = "";
		$atfadmin_voteinfo = "";
	}

	//eval("Game." @ %typeName @ "(true,\"" @ %arg1 @ "\",\"" @ %arg2 @ "\",\"" @ %arg3 @ "\",\"" @ %arg4 @ "\");");
	Game.evalVote(%typename, true, %arg1, %arg2, %arg3, %arg4);
}

function atfadmin_PlayerVote(%client, %typename, %arg1, %arg2, %arg3, %arg4, %clientsVoting, %teamSpecific)
{
	if (%client.votingDisabled) return;

	// open the vote hud for all clients that will participate in this vote
	if (%teamSpecific)
	{
		for (%clientIndex = 0; %clientIndex < ClientGroup.getCount(); %clientIndex++)
		{
			%cl = ClientGroup.getObject(%clientIndex);

			if (%cl.team == %client.team && !%cl.isAIControlled())
				messageClient(%cl, 'openVoteHud', "", %clientsVoting, ($Host::VotePassPercent / 100));
		}
	}
	else
	{
		for (%clientIndex = 0; %clientIndex < ClientGroup.getCount(); %clientIndex++)
		{
			%cl = ClientGroup.getObject(%clientIndex);
			if (!%cl.isAIControlled())
				messageClient(%cl, 'openVoteHud', "", %clientsVoting, ($Host::VotePassPercent / 100));
		}
	}

	clearVotes();
	Game.voteType = %typeName;
	Game.scheduleVote = schedule(($Host::VoteTime * 1000), 0, "calcVotes", %typeName, %arg1, %arg2, %arg3, %arg4);
	$atfadmin_voteinfo = %typeName TAB %arg1 TAB %arg2 TAB %arg3 TAB %arg4;

	%client.vote = true;
	messageAll('addYesVote', "");

	if (!%client.team == 0) clearBottomPrint(%client);

	%client.canVote = false;
	%client.rescheduleVote = schedule(($Host::voteSpread * 1000) + ($Host::voteTime * 1000) , 0, "resetVotePrivs", %client);
}

function serverCmdStartNewVote(%client, %typeName, %arg1, %arg2, %arg3, %arg4, %arg5)
{
	if ($atfadmin::DisableATFAdminForTournaments && $Host::TournamentMode && %typename !$= "VoteEnqueueMission" && %typename !$= "VoteDequeueMission" && %typename !$= "CancelMatchStart")
	{
		Parent::serverCmdStartNewVote(%client, %typeName, %arg1, %arg2, %arg3, %arg4, %arg5);
		return;
	}

	if (%client.votingDisabled)
	{
		messageClient(%client, 'ATFAdminCommand', "\c2You do not have the right to vote.");
		return;
	}

	if (!%client.canVote && !(%client.isAdmin || %client.isSuperAdmin || %client.wasAdmin || %client.wasSuperAdmin))
		return;

	%clientsVoting = 0;

	switch$ (%typeName)
	{
		case "VoteKickPlayer":
			if (%client.isSuperAdmin || (%client.isAdmin && $atfadmin::AllowAdminKickBan & 1))
			{
				if (!atfadmin_OutrankTarget(%client, %arg1))
				{
					messageClient(%sender, 'ATFAdminCommand', '\c2Cannot kick %1 as you do not outrank %2.', %arg1.name, %arg1.sex $= "Male" ? 'him' : 'her');
					return;
				}

				$AdminCl = %client;
				atfadmin_AdminCommand(%client, %typename, %arg1, %arg2, %arg3, %arg4);
			}
			else if ($atfadmin::AllowPlayerVoteKickBan & 1)
			{
				if (%arg1.isSuperAdmin)
				{
					messageClient(%client, 'ATFAdminCommand', '\c2You cannot %1 %2, %3 is a Super Admin!', "Kick Player", %arg1.name, %arg1.sex $= "Male" ? 'he' : 'she');
					logEcho(%client.nameBase@" (guid:"@%client.guid@") attempted to kick "@%arg1.nameBase);
					return;
				}

				if (%arg1.isPrivileged)
				{
					messageClient(%client, 'ATFAdminCommand', '\c2You cannot %1 %2, %3 is a privileged player!', "Kick Player", %arg1.name, %arg1.sex $= "Male" ? 'he' : 'she');
					logEcho(%client.nameBase@" (guid:"@%client.guid@") attempted to kick "@%arg1.nameBase);
					return;
				}

				// Players must either be on same team or the target an observer
				if (%client.team != %arg1.team && %arg1.team != 0)
				{
					messageClient(%client, 'ATFAdminCommand', "\c2Kick player votes must be team based.");
					return;
				}

				if (Game.scheduleVote !$= "")
				{
					messageClient(%client, 'voteAlreadyRunning', '\c2A vote is already in progress.');
					return;
				}

				Game.kickClient = %arg1;
				Game.kickClientName = %arg1.name;
				Game.kickGuid = %arg1.guid;
				Game.kickTeam = %arg1.team;

				if(%arg1.team == 0)
				{
					for (%idx = 0; %idx < ClientGroup.getCount(); %idx++)
					{
						%cl = ClientGroup.getObject(%idx);
						if (!%cl.isAIControlled())
						{
							messageClient(%cl, 'VoteStarted', '\c2%1 initiated a vote to %2 %3.', %client.name, "Kick Player", %arg1.name);
							%clientsVoting++;
						}
					}
					logEcho(%client.nameBase@" initiated a vote to kick player "@%arg1.nameBase);
					atfadmin_PlayerVote(%client, %typename, %arg1, %arg2, %arg3, %arg4, %clientsVoting);
				}
				else
				{
					for (%idx = 0; %idx < ClientGroup.getCount(); %idx++)
					{
						%cl = ClientGroup.getObject( %idx );

						if (%cl.team == %client.team && !%cl.isAIControlled())
						{
							messageClient(%cl, 'VoteStarted', '\c2%1 initiated a vote to %2 %3.', %client.name, "Kick Player", %arg1.name);
							%clientsVoting++;
						}
						else if (atfadmin_hasAdmin(%cl))
							messageClient(%cl, 'ATFAdminCommand', '\c2%1 initiated a vote to %2 %3.', %client.name, "Kick Player", %arg1.name);
					}
					logEcho(%client.nameBase@" initiated a vote to kick player "@%arg1.nameBase);
					atfadmin_PlayerVote(%client, %typename, %arg1, %arg2, %arg3, %arg4, %clientsVoting, true);
				}
			}
			else return;

		case "BanPlayer":
			if (%client.isSuperAdmin || (%client.isAdmin && $atfadmin::AllowAdminKickBan & 2))
			{
				if (!atfadmin_OutrankTarget(%client, %arg1))
				{
					messageClient(%sender, 'ATFAdminCommand', '\c2Cannot ban %1 as you do not outrank %2.', %arg1.name, %arg1.sex $= "Male" ? 'him' : 'her');
					return;
				}

				$AdminCl = %client;
				atfadmin_AdminCommand(%client, %typename, %arg1, %arg2, %arg3, %arg4);
			}
			else if ($atfadmin::AllowPlayerVoteKickBan & 2)
			{
				if (%arg1.isSuperAdmin)
				{
					messageClient(%client, 'ATFAdminCommand', '\c2You cannot %1 %2, %3 is a Super Admin!', "Ban Player", %arg1.name, %arg1.sex $= "Male" ? 'he' : 'she');
					logEcho(atfadmin_getPlainText(%client.name)@" (guid:"@%client.guid@") attempted to BAN "@%arg1.name);
					return;
				}

				if (%arg1.isPrivileged)
				{
					messageClient(%client, 'ATFAdminCommand', '\c2You cannot %1 %2, %3 is a privileged player!', "Ban Player", %arg1.name, %arg1.sex $= "Male" ? 'he' : 'she');
					logEcho(atfadmin_getPlainText(%client.name)@" (guid:"@%client.guid@") attempted to BAN "@%arg1.name);
					return;
				}

				// Players must either be on same team or the target an observer
				if (%client.team != %arg1.team && %arg1.team != 0)
				{
					messageClient(%client, 'ATFAdminCommand', "\c2Ban player votes must be team based.");
					return;
				}

				if (Game.scheduleVote !$= "")
				{
					messageClient(%client, 'voteAlreadyRunning', '\c2A vote is already in progress.');
					return;
				}

				Game.kickClient = %arg1;
				Game.kickClientName = %arg1.name;
				Game.kickGuid = %arg1.guid;
				Game.banTeam = %arg1.team;

				if(%arg1.team == 0)
				{
					for (%idx = 0; %idx < ClientGroup.getCount(); %idx++)
					{
						%cl = ClientGroup.getObject(%idx);
						if (!%cl.isAIControlled())
						{
							messageClient(%cl, 'VoteStarted', '\c2%1 initiated a vote to %2 %3.', %client.name, "Ban Player", %arg1.name);
							%clientsVoting++;
						}
					}
					logEcho(%client.nameBase@" initiated a vote to ban player "@%arg1.nameBase);
					atfadmin_PlayerVote(%client, %typename, %arg1, %arg2, %arg3, %arg4, %clientsVoting);
				}
				else
				{
					for (%idx = 0; %idx < ClientGroup.getCount(); %idx++)
					{
						%cl = ClientGroup.getObject( %idx );

						if (%cl.team == %client.team && !%cl.isAIControlled())
						{
							messageClient( %cl, 'VoteStarted', '\c2%1 initiated a vote to %2 %3.', %client.name, "Ban Player", %arg1.name);
							%clientsVoting++;
						}
					}
					logEcho(atfadmin_getPlainText(%client.name)@" initiated a vote to ban player "@%arg1.name);
					atfadmin_PlayerVote(%client, %typename, %arg1, %arg2, %arg3, %arg4, %clientsVoting, true);
				}
			}
			else return;

		case "VoteGlobalMutePlayer":
			if (%client.isSuperAdmin || (%client.isAdmin && $atfadmin::AllowAdminGlobalMutePlayers))
			{
				if (!atfadmin_OutrankTarget(%client, %arg1))
				{
					if (%arg1.globalMute)
						%what = "unmute";
					else
						%what = "mute";
					messageClient(%sender, 'ATFAdminCommand', '\c2Cannot %1 %2 as you do not outrank %3.', %what, %arg1.name, %arg1.sex $= "Male" ? 'him' : 'her');
					return;
				}

				$AdminCl = %client;
				atfadmin_AdminCommand(%client, %typename, %arg1, %arg2, %arg3, %arg4);
			}
			else if ($atfadmin::AllowPlayerVoteGlobalMutePlayers)
			{
				if (%arg1.globalMute)
					%what = "unmute";
				else
					%what = "mute";

				if (%arg1.isSuperAdmin)
				{
					messageClient(%client, 'ATFAdminCommand', '\c2You cannot %1 %2, %3 is a Super Admin!', %what, %arg1.name, %arg1.sex $= "Male" ? 'he' : 'she');
					logEcho(atfadmin_getPlainText(%client.name)@" (guid:"@%client.guid@") attempted to "@%what@" "@%arg1.name);
					return;
				}

				if (%arg1.isPrivileged)
				{
					messageClient(%client, 'ATFAdminCommand', '\c2You cannot %1 %2, %3 is a privileged player!', %what, %arg1.name, %arg1.sex $= "Male" ? 'he' : 'she');
					logEcho(atfadmin_getPlainText(%client.name)@" (guid:"@%client.guid@") attempted to "@%what@" "@%arg1.name);
					return;
				}

				if (Game.scheduleVote !$= "")
				{
					messageClient(%client, 'voteAlreadyRunning', '\c2A vote is already in progress.');
					return;
				}

				Game.muteClient = %arg1;
				Game.muteClientName = %arg1.name;
				Game.muteGuid = %arg1.guid;

				for (%idx = 0; %idx < ClientGroup.getCount(); %idx++)
				{
					%cl = ClientGroup.getObject(%idx);
					if (!%cl.isAIControlled())
					{
						messageClient(%cl, 'VoteStarted', '\c2%1 initiated a vote to %2 %3.', %client.name, %what, %arg1.name);
						%clientsVoting++;
					}
				}
				logEcho(%client.nameBase@" initiated a vote to "@%what@" player "@%arg1.nameBase);
				atfadmin_PlayerVote(%client, %typename, %arg1, %arg2, %arg3, %arg4, %clientsVoting);
			}
			else return;

		case "VoteAdminPlayer":
			if (%client.isSuperAdmin || (%client.isAdmin && $atfadmin::AllowAdminAdmin))
			{
				$AdminCl = %client;
				atfadmin_AdminCommand(%client, %typename, %arg1, %arg2, %arg3, %arg4);
			}
			else if ($Host::allowAdminPlayerVotes)
			{
				if (Game.scheduleVote !$= "")
				{
					messageClient(%client, 'voteAlreadyRunning', '\c2A vote is already in progress.');
					return;
				}

				for (%idx = 0; %idx < ClientGroup.getCount(); %idx++)
				{
					%cl = ClientGroup.getObject(%idx);
					if (!%cl.isAIControlled())
					{
						messageClient(%cl, 'VoteStarted', '\c2%1 initiated a vote to %2 %3.', %client.name, "Admin Player", %arg1.name);
						%clientsVoting++;
					}
				}
				logEcho(%client.nameBase@" initiated a vote to admin player "@%arg1.nameBase);
				atfadmin_PlayerVote(%client, %typename, %arg1, %arg2, %arg3, %arg4, %clientsVoting);
			}
			else return;

		case "VoteCaptainPlayer":
			if ($Host::PickupMode && (%client.isSuperAdmin || %client.isAdmin))
			{
				$AdminCl = %client;
				atfadmin_AdminCommand(%client, %typename, %arg1, %arg2, %arg3, %arg4);
			}
			else if ($Host::PickupMode && $atfadmin::AllowCaptainPlayerVotes)
			{
				if (Game.scheduleVote !$= "")
				{
					messageClient(%client, 'voteAlreadyRunning', '\c2A vote is already in progress.');
					return;
				}

				for (%idx = 0; %idx < ClientGroup.getCount(); %idx++)
				{
					%cl = ClientGroup.getObject(%idx);
					if (!%cl.isAIControlled())
					{
						messageClient(%cl, 'VoteStarted', '\c2%1 initiated a vote to make %2 the %3 team captain.', %client.name, %arg1.name, %game.getTeamName(%arg2));
						%clientsVoting++;
					}
				}
				logEcho(%client.nameBase@" initiated a vote to captain player "@%arg1.nameBase);
				atfadmin_PlayerVote(%client, %typename, %arg1, %arg2, %arg3, %arg4, %clientsVoting);
			}
			else return;

		case "VoteChangeMission":
			if (%client.isSuperAdmin || (%client.isAdmin && $atfadmin::AllowAdminChangeMission))
			{
				$AdminCl = %client;
				atfadmin_AdminCommand(%client, %typename, %arg1, %arg2, %arg3, %arg4);
			}
			else if ($atfadmin::AllowPlayerVoteChangeMission)
			{
				if (Game.scheduleVote !$= "")
				{
					messageClient(%client, 'voteAlreadyRunning', '\c2A vote is already in progress.');
					return;
				}

				for (%idx = 0; %idx < ClientGroup.getCount(); %idx++)
				{
					%cl = ClientGroup.getObject(%idx);
					if (!%cl.isAIControlled())
					{
						messageClient(%cl, 'VoteStarted', '\c2%1 initiated a vote to %2 %3 (%4).', %client.name, "change the mission to", %arg1, %arg2);
						%clientsVoting++;
					}
				}

				logEcho(%client.nameBase@" initiated a vote to change the mission to "@%arg1@" ("@%arg2@")");
				atfadmin_PlayerVote(%client, %typename, %arg1, %arg2, %arg3, %arg4, %clientsVoting);
			}
			else return;

		case "VoteSkipMission":

			%next = atfadmin_findNextCycleMissionID();

			%arg1 = $MissionList[%next, MissionDisplayName];
			%arg2 = $MissionList[%next, TypeDisplayName];
			%arg3 = %next;
			%arg4 = $MissionList[%next, TypeIndex];

			if (%client.isSuperAdmin || (%client.isAdmin && $atfadmin::AllowAdminSkipMission))
			{
				$AdminCl = %client;
				atfadmin_AdminCommand(%client, %typename, %arg1, %arg2, %arg3, %arg4);
			}
			else if ($atfadmin::AllowPlayerVoteSkipMission)
			{
				if (Game.scheduleVote !$= "")
				{
					messageClient(%client, 'voteAlreadyRunning', '\c2A vote is already in progress.');
					return;
				}

				for (%idx = 0; %idx < ClientGroup.getCount(); %idx++)
				{
					%cl = ClientGroup.getObject(%idx);
					if (!%cl.isAIControlled())
					{
						messageClient(%cl, 'VoteStarted', '\c2%1 initiated a vote to %2: %3 (%4).', %client.name, "skip to the next map in rotation", %arg1, %arg2);
						%clientsVoting++;
					}
				}

				logEcho(%client.nameBase@" initiated a vote to skip the mission to "@%arg1@" ("@%arg2@")");
				atfadmin_PlayerVote(%client, %typename, %arg1, %arg2, %arg3, %arg4, %clientsVoting);
			}
			else return;

		case "VoteEnqueueMission":
			if (!%client.atfclient || %client.atfclientVersion !$= "2.3.0") return;

			if (%client.isSuperAdmin || (%client.isAdmin && $atfadmin::AllowAdminQueueMission))
			{
				$AdminCl = %client;
				atfadmin_AdminCommand(%client, %typename, %arg1, %arg2, %arg3, %arg4);
			}
			else if ($atfadmin::AllowPlayerVoteQueueMission)
			{
				if (Game.scheduleVote !$= "")
				{
					messageClient(%client, 'voteAlreadyRunning', '\c2A vote is already in progress.');
					return;
				}

				for (%idx = 0; %idx < ClientGroup.getCount(); %idx++)
				{
					%cl = ClientGroup.getObject(%idx);
					if (!%cl.isAIControlled())
					{
						messageClient(%cl, 'VoteStarted', '\c2%1 initiated a vote to %2 %3 (%4).', %client.name, "enqueue mission", %arg1, %arg2);
						%clientsVoting++;
					}
				}

				logEcho(%client.nameBase@" initiated a vote to enqueue mission "@%arg1@" ("@%arg2@")");
				atfadmin_PlayerVote(%client, %typename, %arg1, %arg2, %arg3, %arg4, %clientsVoting);
			}
			else return;

		case "VoteDequeueMission":
			if ($atfadmin_missionQueue $= "") return;
			if (!%client.atfclient || %client.atfclientVersion !$= "2.3.0") return;

			if (%client.isSuperAdmin || (%client.isAdmin && $atfadmin::AllowAdminQueueMission))
			{
				$AdminCl = %client;
				atfadmin_AdminCommand(%client, %typename, %arg1, %arg2, %arg3, %arg4);
			}
			else if ($atfadmin::AllowPlayerVoteQueueMission)
			{
				if (Game.scheduleVote !$= "")
				{
					messageClient(%client, 'voteAlreadyRunning', '\c2A vote is already in progress.');
					return;
				}

				for (%idx = 0; %idx < ClientGroup.getCount(); %idx++)
				{
					%cl = ClientGroup.getObject(%idx);
					if (!%cl.isAIControlled())
					{
						messageClient(%cl, 'VoteStarted', '\c2%1 initiated a vote to %2 %3 (%4).', %client.name, "dequeue mission", %arg1, %arg2);
						%clientsVoting++;
					}
				}

				logEcho(%client.nameBase@" initiated a vote to dequeue mission "@%arg1@" ("@%arg2@")");
				atfadmin_PlayerVote(%client, %typename, %arg1, %arg2, %arg3, %arg4, %clientsVoting);
			}
			else return;

		case "VoteFFAMode":
			if (%client.isSuperAdmin || (%client.isAdmin && $atfadmin::AllowAdminChangeMode))
			{
				$AdminCl = %client;
				atfadmin_AdminCommand(%client, %typename, %arg1, %arg2, %arg3, %arg4);
			}
			else if ($atfadmin::AllowPlayerVoteChangeMode)
			{
				if (Game.scheduleVote !$= "")
				{
					messageClient(%client, 'voteAlreadyRunning', '\c2A vote is already in progress.');
					return;
				}

				for (%idx = 0; %idx < ClientGroup.getCount(); %idx++)
				{
					%cl = ClientGroup.getObject(%idx);
					if (!%cl.isAIControlled())
					{
						messageClient( %cl, 'VoteStarted', '\c2%1 initiated a vote to %2 Free For All Mode.', %client.name, "change the server to");
						%clientsVoting++;
					}
				}

				logEcho(%client.nameBase@" initiated a vote to change the server to Free For All Mode");
				atfadmin_PlayerVote(%client, %typename, %arg1, %arg2, %arg3, %arg4, %clientsVoting);
			}
			else return;

		case "VoteTournamentMode":
			if (%client.isSuperAdmin || (%client.isAdmin && $atfadmin::AllowAdminChangeMode))
			{
				$AdminCl = %client;
				atfadmin_AdminCommand(%client, %typename, %arg1, %arg2, %arg3, %arg4);
			}
			else if ($atfadmin::AllowPlayerVoteChangeMode)
			{
				if (getAdmin() == 0)
				{
					messageClient(%client, 'clientMsg', 'There must be a server admin to play in Tournament Mode.');
					return;
				}

				if (Game.scheduleVote !$= "")
				{
					messageClient(%client, 'voteAlreadyRunning', '\c2A vote is already in progress.');
					return;
				}

				for (%idx = 0; %idx < ClientGroup.getCount(); %idx++)
				{
					%cl = ClientGroup.getObject(%idx);
					if (!%cl.isAIControlled())
					{
						messageClient( %cl, 'VoteStarted', '\c2%1 initiated a vote to %2 Tournament Mode (%3).', %client.name, "change the server to", %arg1);
						%clientsVoting++;
					}
				}

				logEcho(%client.nameBase@" initiated a vote to change the server to Tournament Mode ("@%arg1@")");
				atfadmin_PlayerVote(%client, %typename, %arg1, %arg2, %arg3, %arg4, %clientsVoting);
			}
			else return;

		case "VotePickupMode":
			if (%client.isSuperAdmin || (%client.isAdmin && $atfadmin::AllowAdminChangeMode))
			{
				$AdminCl = %client;
				atfadmin_AdminCommand(%client, %typename, %arg1, %arg2, %arg3, %arg4);
			}
			else if ($atfadmin::AllowPlayerVotePickupMode)
			{
				if (getAdmin() == 0)
				{
					messageClient(%client, 'msgClient', 'There must be a server admin online to play in Pickup Mode.');
					return;
				}

				if (Game.scheduleVote !$= "")
				{
					messageClient(%client, 'voteAlreadyRunning', '\c2A vote is already in progress.');
					return;
				}

				for (%idx = 0; %idx < ClientGroup.getCount(); %idx++)
				{
					%cl = ClientGroup.getObject(%idx);
					if (!%cl.isAIControlled())
					{
						messageClient( %cl, 'VoteStarted', '\c2%1 initiated a vote to %2 Pickup Mode (%3).', %client.name, "change the server to", %arg1);
						%clientsVoting++;
					}
				}

				logEcho(%client.nameBase@" initiated a vote to change the server to Pickup Mode ("@%arg1@")");
				atfadmin_PlayerVote(%client, %typename, %arg1, %arg2, %arg3, %arg4, %clientsVoting);
			}
			else return;

		case "VoteMatchStart":
			if (%client.isSuperAdmin || %client.isAdmin)
			{
				$AdminCl = %client;
				atfadmin_AdminCommand(%client, %typename, %arg1, %arg2, %arg3, %arg4);
			}
			else
			{
				if (Game.scheduleVote !$= "")
				{
					messageClient(%client, 'voteAlreadyRunning', '\c2A vote is already in progress.');
					return;
				}

				for (%idx = 0; %idx < ClientGroup.getCount(); %idx++)
				{
					%cl = ClientGroup.getObject(%idx);
					if (!%cl.isAIControlled())
					{
						messageClient( %cl, 'VoteStarted', '\c2%1 initiated a vote to %2.', %client.name, "start the match");
						%clientsVoting++;
					}
				}

				logEcho(%client.nameBase@" initiated a vote to start the match");
				atfadmin_PlayerVote(%client, %typename, %arg1, %arg2, %arg3, %arg4, %clientsVoting);
			}

		case "CancelMatchStart":
			if (%client.isSuperAdmin || %client.isAdmin)
			{
				$AdminCl = %client;
				atfadmin_AdminCommand(%client, %typename, %arg1, %arg2, %arg3, %arg4);
			}

		case "CancelVote":
			if (%client.isSuperAdmin || %client.isAdmin)
			{
				$AdminCl = %client;
				atfadmin_AdminCommand(%client, %typename, %arg1, %arg2, %arg3, %arg4);
			}

		case "PassVote":
			if (%client.isSuperAdmin || %client.isAdmin)
			{
				$AdminCl = %client;
				atfadmin_AdminCommand(%client, %typename, %arg1, %arg2, %arg3, %arg4);
			}

		case "VoteTeamDamage":
			if (%client.isSuperAdmin || (%client.isAdmin && $atfadmin::AllowAdminTeamDamage))
			{
				$AdminCl = %client;
				atfadmin_AdminCommand(%client, %typename, %arg1, %arg2, %arg3, %arg4);
			}
			else if ($atfadmin::AllowPlayerVoteTeamDamage)
			{
				if (Game.scheduleVote !$= "")
				{
					messageClient(%client, 'voteAlreadyRunning', '\c2A vote is already in progress.');
					return;
				}

				%actionMsg = $TeamDamage ? "disable team damage" : "enable team damage";

				for (%idx = 0; %idx < ClientGroup.getCount(); %idx++)
				{
					%cl = ClientGroup.getObject(%idx);
					if (!%cl.isAIControlled())
					{
						messageClient( %cl, 'VoteStarted', '\c2%1 initiated a vote to %2.', %client.name, %actionMsg);
						%clientsVoting++;
					}
				}

				logEcho(%client.nameBase@" initiated a vote to "@%actionMsg);
				atfadmin_PlayerVote(%client, %typename, %arg1, %arg2, %arg3, %arg4, %clientsVoting);
			}
			else return;

		case "VoteChangeTimeLimit":
			if (%client.isSuperAdmin || (%client.isAdmin && $atfadmin::AllowAdminChangeTimeLimit))
			{
				$AdminCl = %client;
				atfadmin_AdminCommand(%client, %typename, %arg1, %arg2, %arg3, %arg4);
			}
			else if ($atfadmin::AllowPlayerVoteTimeLimit)
			{
				if (Game.scheduleVote !$= "")
				{
					messageClient(%client, 'voteAlreadyRunning', '\c2A vote is already in progress.');
					return;
				}

				for (%idx = 0; %idx < ClientGroup.getCount(); %idx++)
				{
					%cl = ClientGroup.getObject(%idx);
					if (!%cl.isAIControlled())
					{
						messageClient( %cl, 'VoteStarted', '\c2%1 initiated a vote to %2 %3.', %client.name, "change the time limit to", %arg1);
						%clientsVoting++;
					}
				}

				logEcho(%client.nameBase@" initiated a vote to change the time limit to "@%arg1);
				atfadmin_PlayerVote(%client, %typename, %arg1, %arg2, %arg3, %arg4, %clientsVoting);
			}
			else return;

		case "VoteResetServer":
			if (%client.isSuperAdmin || (%client.isAdmin && $atfadmin::AllowAdminResetServer))
			{
				$AdminCl = %client;
				atfadmin_AdminCommand(%client, %typename, %arg1, %arg2, %arg3, %arg4);
			}
			else return;

		case "VoteDeAdmin":
			if (%client.isSuperAdmin || %client.isAdmin)
				atfadmin_deAdmin(%client);
			else return;

		case "VoteReAdmin":
			if (%client.wasSuperAdmin || %client.wasAdmin)
				atfadmin_reAdmin(%client);
			else return;

		case "VoteGreedMode":
			if (%client.isSuperAdmin || %client.isAdmin)
			{
				$AdminCl = %client;
				atfadmin_AdminCommand(%client, %typename, %arg1, %arg2, %arg3, %arg4);
			}
			else
			{
				if (Game.scheduleVote !$= "")
				{
					messageClient(%client, 'voteAlreadyRunning', '\c2A vote is already in progress.');
					return;
				}

				%actionMsg = Game.greedMode ? "disable Greed mode" : "enable Greed mode";

				for (%idx = 0; %idx < ClientGroup.getCount(); %idx++)
				{
					%cl = ClientGroup.getObject(%idx);
					if (!%cl.isAIControlled())
					{
						messageClient( %cl, 'VoteStarted', '\c2%1 initiated a vote to %2.', %client.name, %actionMsg);
						%clientsVoting++;
					}
				}

				logEcho(%client.nameBase@" initiated a vote to "@%actionMsg);
				atfadmin_PlayerVote(%client, %typename, %arg1, %arg2, %arg3, %arg4, %clientsVoting);
			}

		case "VoteHoardMode":
			if (%client.isSuperAdmin || %client.isAdmin)
			{
				$AdminCl = %client;
				atfadmin_AdminCommand(%client, %typename, %arg1, %arg2, %arg3, %arg4);
			}
			else
			{
				if (Game.scheduleVote !$= "")
				{
					messageClient(%client, 'voteAlreadyRunning', '\c2A vote is already in progress.');
					return;
				}

				%actionMsg = Game.hoardMode ? "disable Hoard mode" : "enable Hoard mode";

				for (%idx = 0; %idx < ClientGroup.getCount(); %idx++)
				{
					%cl = ClientGroup.getObject(%idx);
					if (!%cl.isAIControlled())
					{
						messageClient( %cl, 'VoteStarted', '\c2%1 initiated a vote to %2.', %client.name, %actionMsg);
						%clientsVoting++;
					}
				}

				logEcho(%client.nameBase@" initiated a vote to "@%actionMsg);
				atfadmin_PlayerVote(%client, %typename, %arg1, %arg2, %arg3, %arg4, %clientsVoting);
			}

		case "VoteShowdownSiege":
			if (%client.isSuperAdmin || (%client.isAdmin && $atfadmin::AllowAdminShowdownSiege))
			{
				$AdminCl = %client;
				atfadmin_AdminCommand(%client, %typename, %arg1, %arg2, %arg3, %arg4);
			}
			else if ($atfadmin::AllowPlayerVoteShowdownSiege)
			{
				if (Game.scheduleVote !$= "")
				{
					messageClient(%client, 'voteAlreadyRunning', '\c2A vote is already in progress.');
					return;
				}

				%actionMsg = $atfadmin_showdownSiegeEnabled ? "disable Showdown Siege" : "enable Showdown Siege";

				for (%idx = 0; %idx < ClientGroup.getCount(); %idx++)
				{
					%cl = ClientGroup.getObject(%idx);
					if (!%cl.isAIControlled())
					{
						messageClient( %cl, 'VoteStarted', '\c2%1 initiated a vote to %2.', %client.name, %actionMsg);
						%clientsVoting++;
					}
				}

				logEcho(%client.nameBase@" initiated a vote to "@%actionMsg);
				atfadmin_PlayerVote(%client, %typename, %arg1, %arg2, %arg3, %arg4, %clientsVoting);
			}
			else return;

		case "VoteBaseRapeToggle":
			if (%client.isSuperAdmin || %client.isAdmin)
				atfadmin_baseRapeToggle(%client);
			else return;

		case "VoteToggleAssetTracking":
			if (%client.isSuperAdmin || %client.isAdmin)
				atfadmin_assetTrackToggle(%client);
			else return;
	}
}

function setModeFFA(%mission, %missionType)
{
	if ($Host::TournamentMode || $Host::PickupMode)
	{
		if ($atfadmin_oldTimeLimit !$= "")
		{
			$Host::TimeLimit = $atfadmin_oldTimeLimit;
			messageAll('\c2Time limit set to %1 minutes for Free For All mode.', $Host::TimeLimit);
			$atfadmin_oldTimeLimit = "";
		}

		if (!$atfadmin_assetTrack && $atfadmin::AssetTracking)
		{
			%cause = "(ffa)";
			%setto = "enabled";
			logEcho("asset tracking" SPC %setto SPC %cause);
			messageAll('\c2Enabling asset tracking for Free For All mode.');
			$atfadmin_assetTrack = true;
		}

		// default base rape behavior in free for all mode
		$atfadmin_alwaysAllowBaseRape = false;

		// disable Pickup Mode
		if ($Host::PickupMode)
		{
			$Host::PickupMode = false;
			$atfadmin_teamcaptain[1] = 0;
			$atfadmin_teamcaptain[2] = 0;

			for (%i = 0; %i < ClientGroup.getCount(); %i++)
			{
				%client = ClientGroup.getObject(%i);

				%client.isCaptain = false;
				%client.captainOfTeam = 0;
			}
		}

		// Defense Turret
		if ($atfadmin::EnableDefenseTurretForFFA)
			$Host::DefenseTurret::Active = 1;
		else
			$Host::DefenseTurret::Active = 0;
	}

	if ($atfadmin_missionQueue !$= "")
		$atfadmin_nextMissionID = atfadmin_dequeueMission();

	Parent::setModeFFA(%mission, %missionType);
}

function setModeTournament(%mission, %missionType)
{
	if (!$Host::TournamentMode)
	{
		if ($atfadmin::TournamentModeTimeLimit[%missionType] !$= "")
		{
			$atfadmin_oldTimeLimit = $Host::TimeLimit;
			$Host::TimeLimit = $atfadmin::TournamentModeTimeLimit[%missionType];
			messageAll('\c2Time limit set to %1 minutes for Tournament mode.', $Host::TimeLimit);
		}

		if ($atfadmin_assetTrack)
		{
			if ($atfadmin::AssetTracking < 2)
			{
				%cause = "(tournament)";
				%setto = "disabled";
				logEcho("asset tracking" SPC %setto SPC %cause);
				messageAll('\c2Disabling asset tracking for Tournament mode.');
				$atfadmin_assetTrack = false;
			}
		}

		// always allow base rape in tournament mode
		$atfadmin_alwaysAllowBaseRape = true;

		// disable Pickup Mode
		if ($Host::PickupMode)
		{
			$Host::PickupMode = false;
			$atfadmin_teamcaptain[1] = 0;
			$atfadmin_teamcaptain[2] = 0;

			for (%i = 0; %i < ClientGroup.getCount(); %i++)
			{
				%client = ClientGroup.getObject(%i);

				%client.isCaptain = false;
				%client.captainOfTeam = 0;
			}
		}

		// Defense Turret
		if ($atfadmin::EnableDefenseTurretForTournament)
			$Host::DefenseTurret::Active = 1;
		else
			$Host::DefenseTurret::Active = 0;
	}

	if ($atfadmin_missionQueue !$= "")
		$atfadmin_nextMissionID = atfadmin_dequeueMission();

	Parent::setModeTournament(%mission, %missionType);
}

function setModePickup(%missionID)
{
	if (!$Host::PickupMode)
	{
		$Host::PickupMode = true;

		%mission = $MissionList[%missionID, MissionFileName];
		%missionType = $MissionList[%missionID, TypeName];

		if ($atfadmin::PickupModeTimeLimit[%missionType] !$= "")
		{
			$atfadmin_oldTimeLimit = $Host::TimeLimit;
			$Host::TimeLimit = $atfadmin::PickupModeTimeLimit[%missionType];
			messageAll('\c2Time limit set to %1 minutes for Pickup mode.', $Host::TimeLimit);
		}

		// disable Tournament Mode
		$Host::TournamentMode = false;

		// Defense Turret
		if ($atfadmin::EnableDefenseTurretForPickup)
			$Host::DefenseTurret::Active = 1;
		else
			$Host::DefenseTurret::Active = 0;

		if (isObject(Game))
			Game.gameOver();

		if ($atfadmin_missionQueue !$= "")
			$atfadmin_nextMissionID = atfadmin_dequeueMission();

		loadMission(%mission, %missionType, false);
	}
}


};
// END package atfadmin_admin
