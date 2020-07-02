// START package atfadmin_TeamHuntersGame
package atfadmin_TeamHuntersGame
{


function TeamHuntersGame::updateScoreHud(%game, %client, %tag)
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
// END package atfadmin_TeamHuntersGame
