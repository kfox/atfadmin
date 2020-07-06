function handleAdminPrivilegedPlayerMessage(%msgType, %msgString, %client)
{
	%player = $PlayerList[%client];
	if (%player)
	{
		%player.isPrivileged = true;
		lobbyUpdatePlayer(%client);
	}
}

function handleAdminStripPrivilegedPlayerMessage(%msgType, %msgString, %client)
{
	%player = $PlayerList[%client];
	if (%player)
	{
		%player.isPrivileged = false;
		lobbyUpdatePlayer(%client);
	}
}

function handleCaptainPlayerMessage(%msgType, %msgString, %client)
{
	%player = $PlayerList[%client];
	if (%player)
	{
		%player.isCaptain = true;
		lobbyUpdatePlayer(%client);
	}
}

function handleStripCaptainPlayerMessage(%msgType, %msgString, %client)
{
	%player = $PlayerList[%client];
	if (%player)
	{
		%player.isCaptain = false;
		lobbyUpdatePlayer(%client);
	}
}

addMessageCallback('MsgAdminPrivilegedPlayer', handleAdminPrivilegedPlayerMessage);
addMessageCallback('MsgAdminStripPrivilegedPlayer', handleAdminPrivilegedPlayerMessage);
addMessageCallback('MsgCaptainPlayer', handleCaptainPlayerMessage);
addMessageCallback('MsgStripCaptainPlayer', handleStripCaptainPlayerMessage);


package atfclient
{ // START package atfclient


function lobbyUpdatePlayer(%clientId)
{
	%player = $PlayerList[%clientId];
	if (!isObject(%player))
	{
		warn("lobbyUpdatePlayer( " @ %clientId @ " ) - there is no client object with that id!");
		return;
	}

	// Build the text:
	if (%player.isCaptain)
		%tag = "C";
	else if (%player.isSuperAdmin)
		%tag = "SA";
	else if (%player.isAdmin)
		%tag = "A";
	else if (%player.isPrivileged)
		%tag = "P";
	else if (%player.isBot)
		%tag = "B";
	else
		%tag = " ";

	if (%player.canListen)
	{
		if (%player.voiceEnabled)
		{
			%voiceIcons = "lobby_icon_speak";
			if (%player.isListening)
				%voiceIcons = %voiceIcons @ ":lobby_icon_listen";
		}
		else
			%voiceIcons = %player.isListening ? "lobby_icon_listen" : "";
	}
	else
		%voiceIcons = "shll_icon_timedout";

	if ($clTeamCount > 1)
	{
		if (%player.teamId == 0)
			%teamName = "Observer";
		else
			%teamName = $clTeamScore[%player.teamId, 0] $= "" ? "-" : $clTeamScore[%player.teamId, 0];
		%text = %tag TAB %voiceIcons TAB %player.name TAB %teamName TAB %player.score TAB %player.ping TAB %player.packetLoss;
	}
	else
		%text = %tag TAB %voiceIcons TAB %player.name TAB %player.score TAB %player.ping TAB %player.packetLoss;

	if (LobbyPlayerList.getRowNumById(%clientId) == -1)
		LobbyPlayerList.addRow(%clientId, %text);
	else
		LobbyPlayerList.setRowById(%clientId, %text);

	if ($InLobby)
		LobbyPlayerList.sort();
}

function handleClientJoin(%msgType, %msgString, %clientName, %clientId, %targetId, %isAI, %isAdmin, %isSuperAdmin, %isSmurf, %guid, %isPrivileged)
{
	logEcho("got client join: " @ detag(%clientName) @ " : " @ %clientId);

	//create the player list group, and add it to the ClientConnectionGroup...
	if (!isObject("PlayerListGroup"))
	{
		%newGroup = new SimGroup("PlayerListGroup");
		ClientConnectionGroup.add(%newGroup);
	}

	%player = new ScriptObject()
	{
		className = "PlayerRep";
		name = detag(%clientName);
		guid = %guid;
		clientId = %clientId;
		targetId = %targetId;
		teamId = 0; // start unassigned
		score = 0;
		ping = 0;
		packetLoss = 0;
		chatMuted = false;
		canListen = false;
		voiceEnabled = false;
		isListening = false;
		isBot = %isAI;
		isPrivileged = %isPrivileged;
		isCaptain = %isCaptain;
		isAdmin = %isAdmin;
		isSuperAdmin = %isSuperAdmin;
		isSmurf = %isSmurf;
	};
	PlayerListGroup.add(%player);
	$PlayerList[%clientId] = %player;

	if (!%isAI)
		getPlayerPrefs(%player);

	lobbyUpdatePlayer(%clientId);
}

function atfclient_superAdminPlayer(%msgType, %msgString, %clientId)
{
	if (strstr(%msgString, " has become a Super Admin by force.") != -1)
	{
		%player = $PlayerList[%clientId];
		if (%player)
		{
			%player.isAdmin = true;
			%player.isSuperAdmin = true;
			lobbyUpdatePlayer(%clientId);
		}
	}
	else if (strstr(%msgString, " has become an Admin by force.") != -1)
	{
		%player = $PlayerList[%clientId];
		if (%player)
		{
			%player.isAdmin = true;
			%player.isSuperAdmin = false;
			lobbyUpdatePlayer(%clientId);
		}
	}
}

function atfclient_unblockVoicePack(%client)
{
	commandToServer('VoicePackUnblock', %client);
}

//
// -- Overrides ---
//

function LobbyPlayerPopup::onSelect(%this, %id, %text)
{
	parent::onSelect(%this, %id, %text);

	// atfadmin codes start at 601
	switch (%id)
	{
		case 606: // Add Privileged Status
			commandToServer('AddToPrivilegedList', %this.player.clientId);

		case 607: // Remove Privileged Status
			commandToServer('RemoveFromPrivilegedList', %this.player.clientId);

		case 601: // Warn player
			commandToServer('WarnPlayer', %this.player.clientId);

		case 602: // Observe player
			commandToServer('ProcessGameLink', %this.player.clientId);

		case 603:  // Globally mute
			lobbyPlayerVote("VoteGlobalMutePlayer", "globally mute player", %this.player.clientId);

		case 604:  // Globally unmute
			lobbyPlayerVote("VoteGlobalMutePlayer", "globally unmute player", %this.player.clientId);

		case 609: // Allow player voting
			commandToServer('TogglePlayerVoting', %this.player.clientId);

		case 610: // Disallow player voting
			commandToServer('TogglePlayerVoting', %this.player.clientId);

		case 605: // Lightning strike
			commandToServer('LightningStrike', %this.player.clientId);

		case 608: // Blow up player
			commandToServer('BlowupPlayer', %this.player.clientId);

		case 611:  // Voice Pack temporary block
			commandToServer('TemporaryVoicePackBlock', %this.player.clientId);

		case 612:  // Voice Pack permanent block
			commandToServer('PermanentVoicePackBlock', %this.player.clientId);

		case 613:  // Voice Pack unblock
			commandToServer('VoicePackUnblock', %this.player.clientId);

		case 614:  // Strip Captain
			commandToServer('StripCaptain', %this.player.clientId);

		case 615:  // Make Captain for team 1
			commandToServer('CaptainPlayer', %this.player.clientId, 1);

		case 616:  // Make Captain for team 2
			commandToServer('CaptainPlayer', %this.player.clientId, 2);
	}

	Canvas.popDialog(LobbyPlayerActionDlg);
}

function fillLobbyMissionTypeMenu()
{
	if (!LobbyVoteMenu.enqueueChoose)
	{
		Parent::fillLobbyMissionTypeMenu();
		return;
	}

	LobbyVoteMenu.key++;
	LobbyVoteMenu.clear();
	LobbyVoteMenu.mode = "enqueueType";
	commandToServer('GetMissionTypes', LobbyVoteMenu.key);
	LobbyCancelBtn.setVisible(true);
}

function fillLobbyMissionMenu(%type, %typeName)
{
	if (LobbyVoteMenu.enqueueChoose)
	{
		LobbyVoteMenu.key++;
		LobbyVoteMenu.clear();
		LobbyVoteMenu.mode = "enqueueMission";
		LobbyVoteMenu.missionType = %type;
		LobbyVoteMenu.typeName = %typeName;
		commandToServer('GetMissionList', LobbyVoteMenu.key, %type);
		LobbyCancelBtn.setVisible(true);
	}
	else if (LobbyVoteMenu.dequeueChoose)
	{
		LobbyVoteMenu.key++;
		LobbyVoteMenu.clear();
		LobbyVoteMenu.mode = "dequeueMission";
		commandToServer('GetQueuedMissionList', LobbyVoteMenu.key);
		LobbyCancelBtn.setVisible(true);
	}
	else
	{
		Parent::fillLobbyMissionMenu(%type, %typename);
		return;
	}
}

function fillLobbyVoteMenu()
{
	LobbyVoteMenu.enqueueChoose = false;
	LobbyVoteMenu.dequeueChoose = false;
	Parent::fillLobbyVoteMenu();
}

function LobbyVoteMenu::reset(%this)
{
	%this.enqueueChoose = false;
	%this.dequeueChoose = false;
	Parent::reset(%this);
}

function lobbyVote()
{
	%id = LobbyVoteMenu.getSelectedId();
	%text = LobbyVoteMenu.getRowTextById(%id);

	switch$ (LobbyVoteMenu.mode)
	{
		case "": // default case...
			// test for special cases
			switch$ ($clVoteCmd[%id])
			{
				case "VoteEnqueueMission":
					LobbyVoteMenu.enqueueChoose = true;
					fillLobbyMissionTypeMenu();
					return;

				case "VoteDequeueMission":
					LobbyVoteMenu.dequeueChoose = true;
					fillLobbyMissionMenu();
					return;

				case "ShowMissionQueue":
					commandToServer('ShowMissionQueue');
					return;

				case "CancelMatchStart":
					commandToServer('CancelMatchStart');
					LobbyVoteMenu.reset();
					return;

				default:
					Parent::lobbyVote();
					return;
			}

		case "enqueueType":
			fillLobbyMissionMenu($clVoteCmd[%id], %text);
			return;

		case "enqueueMission":
			startNewVote("VoteEnqueueMission", 
						%text,                        // mission display name 
						LobbyVoteMenu.typeName,       // mission type display name 
						$clVoteCmd[%id],              // mission id                              
						LobbyVoteMenu.missionType );  // mission type id
			LobbyVoteMenu.reset();
			LobbyVoteMenu.enqueueChoose = false;
			return;

		case "dequeueMission":
			startNewVote("VoteDequeueMission", $clVoteCmd[%id]);
			LobbyVoteMenu.reset();
			LobbyVoteMenu.dequeueChoose = false;
			return;

		default:
			Parent::lobbyVote();
			return;
	}

	startNewVote($clVoteCmd[%id], $clVoteAction[%id]);
	fillLobbyVoteMenu();
}

function DispatchLaunchMode()
{
	addMessageCallback('MsgSuperAdminPlayer', atfclient_superAdminPlayer);
	Parent::DispatchLaunchMode();
}

function clientCmdMissionStartPhase2(%seq)
{
	// register with the server to receive extras
	commandToServer('ATFAdminRegister', $atfclient_version);
	Parent::clientCmdMissionStartPhase2(%seq);
}


}; // END package atfclient

$atfclient_version = "2.3.0";
activatePackage(atfclient);
