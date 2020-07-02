package atfadmin_loadingGui
{


function sendModInfoToClient(%client)
{
	if ($atfadmin::DisableATFAdminForTournaments && $Host::TournamentMode)
	{
		Parent::sendModInfoToClient(%client);
		return;
	}

	%singlePlayer = $CurrentMissionType $= "SinglePlayer";
	messageClient(%client, 'MsgLoadInfo', "", $CurrentMission, "", "");

	if (!$Host::ClassicRandomMissions)
	{
		%nextMisFileID = atfadmin_findNextCycleMissionID();
		%nextMission = $MissionList[%nextMisFileID, MissionDisplayName];
		%nextType = $MissionList[%nextMisFileID, TypeDisplayName];
		%nmis = "<font:verdana bold:12><color:33CCCC>* Next mission: <color:FFFFFF>" @ %nextMission @ " (" @ %nextType @ ")";
	}
	else
		%nmis = "<font:verdana bold:12><color:33CCCC>* Next mission: <color:FFFFFF>Randomly selected";

	messageClient(%client, 'MsgLoadQuoteLine', "", "<spush><font:sui generis:22><color:33CCCC><just:center>" @ $Host::GameName @ "<spop>");
	messageClient(%client, 'MsgLoadQuoteLine', "", "");
	messageClient(%client, 'MsgLoadQuoteLine', "", "<spush><font:verdana bold:16><color:33CCCC>Running atfadmin version: <color:FFFFFF>" @ $atfadmin_Version @ "<spop>");
	messageClient(%client, 'MsgLoadQuoteLine', "", "<spush><font:verdana bold:16><color:33CCCC>Available from: <color:FFFFFF><a:WWWLINK\twww.the-pond.net>http://www.the-pond.net/</a><spop>");
	if (!%client.atfclient)
		messageClient(%client, 'MsgLoadQuoteLine', "", "<spush><font:verdana bold:12><color:33CCCC>NOTE: <font:verdana:12><color:FFFFFF> An optional atfclient is also available.<spop>");
	messageClient(%client, 'MsgLoadQuoteLine', "", "");
	messageClient(%client, 'MsgLoadQuoteLine', "", "<spush><font:verdana bold:16><color:33CCCC>Coding by: <color:FFFFFF><a:PLAYER\ta tiny fish-e>a tiny fishie</a><spop>");
	messageClient(%client, 'MsgLoadQuoteLine', "", "");
	messageClient(%client, 'MsgLoadQuoteLine', "", "<spush><font:verdana bold:14>" @ $Host::Info @ "<spop>");
	messageClient(%client, 'MsgLoadQuoteLine', "", "");
	messageClient(%client, 'MsgLoadQuoteLine', "", "");

	messageClient(%client, 'MsgLoadRulesLine', "", %nmis);
	messageClient(%client, 'MsgLoadRulesLine', "", "<color:33CCCC>* Time limit: <color:FFFFFF>" @ $Host::TimeLimit, false);
	messageClient(%client, 'MsgLoadRulesLine', "", "<color:33CCCC>* Team damage: <color:FFFFFF>" @ ($TeamDamage ? "On" : "Off"));
	messageClient(%client, 'MsgLoadRulesLine', "", "<color:33CCCC>* Minimum players for base rape: <color:FFFFFF>" @ $atfadmin::BaseRapeMinimumPlayers);
	messageClient(%client, 'MsgLoadRulesLine', "", "<color:33CCCC>* Voice packs: <color:FFFFFF>" @ ($atfadmin::AllowVoicePacks ? "Allowed" : "Disabled"));
	messageClient(%client, 'MsgLoadRulesLine', "", "<color:33CCCC>* CRC checking: <color:FFFFFF>" @ ($Host::CRCTextures ? "On" : "Off"));
	messageClient(%client, 'MsgLoadRulesLine', "", "<color:33CCCC>* Smurfs: <color:FFFFFF>" @ ($Host::NoSmurfs ? "No" : "Yes"));
	messageClient(%client, 'MsgLoadRulesLine', "", "<color:33CCCC>* Fair Teams: <color:FFFFFF>" @ ($FairTeams ? "On" : "Off"));
	messageClient(%client, 'MsgLoadRulesLine', "", "<color:33CCCC>* Random teams: <color:FFFFFF>" @ ($RandomTeams ? "On" : "Off"));
	messageClient(%client, 'MsgLoadRulesLine', "", "<color:33CCCC>* Packet rate / size: <color:FFFFFF>" @ $pref::Net::PacketRateToClient SPC "/" SPC $pref::Net::PacketSize);

	messageClient(%client, 'MsgLoadInfoDone');

	// z0dd - ZOD, 5/12/02. Send mission info again so as not to conflict with cs scripts.
	schedule(12000, 0, "sendLoadInfoToClient", %client, true);
}


};
