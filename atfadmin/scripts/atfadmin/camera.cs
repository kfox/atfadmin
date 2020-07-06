// START package atfadmin_camera
package atfadmin_camera
{


function Observer::onTrigger(%data,%obj,%trigger,%state)
{
	// state = 0 means that a trigger key was released
	if (%state == 0)
		return;

	//first, give the game the opportunity to prevent the observer action
	if (!Game.ObserverOnTrigger(%data, %obj, %trigger, %state))
		return;

	//now observer functions if you press the "throw"
	if (%trigger >= 4)
		return;

	//trigger types:   0:fire 1:altTrigger 2:jump 3:jet 4:throw
	%client = %obj.getControllingClient();
	if (%client == 0)
		return;

	switch$ (%obj.mode)
	{
		case "justJoined":
			//press FIRE
			if (%trigger == 0)
			{
				// clear intro message
				clearBottomPrint( %client );

				//spawn the player
				commandToClient(%client, 'setHudMode', 'Standard');
				Game.assignClientTeam(%client);
				Game.spawnPlayer( %client, $MatchStarted );

				if( $MatchStarted )
				{
					%client.camera.setFlyMode();
					%client.setControlObject( %client.player );
				}
				else
				{
					%client.camera.getDataBlock().setMode( %client.camera, "pre-game", %client.player );
					%client.setControlObject( %client.camera );
				}
			}

			//press JET
			else if (%trigger == 3)
			{
				//cycle throw the static observer spawn points
				%markerObj = Game.pickObserverSpawn(%client, true);
				%transform = %markerObj.getTransform();
				%obj.setTransform(%transform);
				%obj.setFlyMode();
			}

			//press JUMP
			else if (%trigger == 2)
			{
				//switch the observer mode to observing clients
				if (isObject(%client.observeFlyClient))
					serverCmdObserveClient(%client, %client.observeFlyClient);
				else
					serverCmdObserveClient(%client, -1);

				displayObserverHud(%client, %client.observeClient);
				if (!%client.isAdmin || (%client.isAdmin && !$atfadmin::HideAdminObserverMessages))
					messageClient(%client.observeClient, 'Observer', '\c1%1 is now observing you.', %client.name);
			}

		case "playerDeath":
			// Attached to a dead player - spawn regardless of trigger type
			if(!%client.waitRespawn && getSimTime() > %client.suicideRespawnTime)
			{
				commandToClient(%client, 'setHudMode', 'Standard');
				Game.spawnPlayer( %client, true );
				%client.camera.setFlyMode();
				%client.setControlObject(%client.player);
			}

		case "PreviewMode":
			if (%trigger == 0)
			{
				commandToClient(%client, 'setHudMode', 'Standard');
				if( %client.lastTeam )
					Game.clientJoinTeam( %client, %client.lastTeam );
				else
				{
					Game.assignClientTeam( %client, true );

					// Spawn the player:
					Game.spawnPlayer( %client, false );
				}

				%client.camera.setFlyMode();
				%client.setControlObject( %client.player );
			}

		case "toggleCameraFly":
			// this is the default camera mode

		case "observerFly":
			// Free-flying observer camera

			if (%trigger == 0 && getSimTime() > %client.observerModeRespawnTime)
			{
				if (!$Host::TournamentMode && $MatchStarted)
				{
					// reset observer params
					clearBottomPrint(%client);
					commandToClient(%client, 'setHudMode', 'Standard');

					if (%client.lastTeam !$= "" && %client.lastTeam != 0 && Game.numTeams > 1)
					{
						Game.clientJoinTeam(%client, %client.lastTeam, $MatchStarted);
						%client.camera.setFlyMode();
						%client.setControlObject(%client.player);
					}
					else
					{
						Game.assignClientTeam(%client);

						// Spawn the player:
						Game.spawnPlayer(%client, true);
						%client.camera.setFlyMode();
						%client.setControlObject(%client.player);
						ClearBottomPrint(%client);
					}
				}
				else if (!$Host::TournamentMode && !$Host::PickupMode)
				{
					clearBottomPrint(%client);
					Game.assignClientTeam(%client);

					// Spawn the player:
					Game.spawnPlayer( %client, false );
					%client.camera.getDataBlock().setMode( %client.camera, "pre-game", %client.player );
					%client.setControlObject( %client.camera );
				}
			}

			//press JET
			else if (%trigger == 3)
			{
				%markerObj = Game.pickObserverSpawn(%client, true);
				%transform = %markerObj.getTransform();
				%obj.setTransform(%transform);
				%obj.setFlyMode();
			}

			//press JUMP
			else if (%trigger == 2)
			{
				//switch the observer mode to observing clients
				if (isObject(%client.observeFlyClient))
					serverCmdObserveClient(%client, %client.observeFlyClient);
				else
					serverCmdObserveClient(%client, -1);

				observerFollowUpdate( %client, %client.observeClient, false );
				displayObserverHud(%client, %client.observeClient);
				if (!%client.isAdmin || (%client.isAdmin && !$atfadmin::HideAdminObserverMessages))
					messageClient(%client.observeClient, 'Observer', '\c1%1 is now observing you.', %client.name);
			}

		case "observerStatic":
			// Non-moving observer camera
			%next = (%trigger == 3 ? true : false);
			%markerObj = Game.pickObserverSpawn(%client, %next);
			%transform = %markerObj.getTransform();
			%obj.setTransform(%transform);
			%obj.setFlyMode();

		case "observerStaticNoNext":
			// Non-moving, non-cycling observer camera

		case "observerTimeout":
			// Player didn't respawn quickly enough
			if (%trigger == 0)
			{
				clearBottomPrint(%client);
				commandToClient(%client, 'setHudMode', 'Standard');
				if( %client.lastTeam )
					Game.clientJoinTeam( %client, %client.lastTeam, true );
				else
				{
					Game.assignClientTeam( %client );

					// Spawn the player:
					Game.spawnPlayer( %client, true );
				}

				%client.camera.setFlyMode();
				%client.setControlObject( %client.player );
			}

			//press JET
			else if (%trigger == 3)
			{
				%markerObj = Game.pickObserverSpawn(%client, true);
				%transform = %markerObj.getTransform();
				%obj.setTransform(%transform);
				%obj.setFlyMode();
			}

			//press JUMP
			else if (%trigger == 2)
			{
				// switch the observer mode to observing clients
				if (isObject(%client.observeFlyClient))
					serverCmdObserveClient(%client, %client.observeFlyClient);
				else
					serverCmdObserveClient(%client, -1);

				// update the observer list for this client
				observerFollowUpdate( %client, %client.observeClient, false );

				displayObserverHud(%client, %client.observeClient);
				if (!%client.isAdmin || (%client.isAdmin && !$atfadmin::HideAdminObserverMessages))
					messageClient(%client.observeClient, 'Observer', '\c1%1 is now observing you.', %client.name);
			}

		case "observerFollow":
			// Observer attached to a moving object (assume player for now...)
			// press FIRE - cycle to next client
			if (%trigger == 0)
			{
				%nextClient = findNextObserveClient(%client);
				%prevObsClient = %client.observeClient;
				if (%nextClient > 0 && %nextClient != %client.observeClient)
				{
					// update the observer list for this client
					observerFollowUpdate( %client, %nextClient, true );

					// set the new object
					%transform = %nextClient.player.getTransform();
					if( !%nextClient.isMounted() )
					{
						%obj.setOrbitMode(%nextClient.player, %transform, 0.5, 4.5, 4.5);
						%client.observeClient = %nextClient;
					}
					else
					{
						%mount = %nextClient.player.getObjectMount();
						if( %mount.getDataBlock().observeParameters $= "" )
							%params = %transform;
						else
							%params = %mount.getDataBlock().observeParameters;

						%obj.setOrbitMode(%mount, %mount.getTransform(), getWord( %params, 0 ), getWord( %params, 1 ), getWord( %params, 2 ));
						%client.observeClient = %nextClient;
					}

					//send the message(s)
					displayObserverHud(%client, %nextClient);
					if (!%client.isAdmin || (%client.isAdmin && !$atfadmin::HideAdminObserverMessages))
					{
						messageClient(%nextClient, 'Observer', '\c1%1 is now observing you.', %client.name);
						messageClient(%prevObsClient, 'ObserverEnd', '\c1%1 is no longer observing you.', %client.name);
					}
				}
			}

			// press JET - cycle to prev client
			else if (%trigger == 3)
			{
				%prevClient = findPrevObserveClient(%client);
				%prevObsClient = %client.observeClient;
				if (%prevClient > 0 && %prevClient != %client.observeClient)
				{
					// update the observer list for this client
					observerFollowUpdate( %client, %prevClient, true );

					// set the new object
					%transform = %prevClient.player.getTransform();
					if( !%prevClient.isMounted() )
					{
						%obj.setOrbitMode(%prevClient.player, %transform, 0.5, 4.5, 4.5);
						%client.observeClient = %prevClient;
					}
					else
					{
						%mount = %prevClient.player.getObjectMount();
						if( %mount.getDataBlock().observeParameters $= "" )
							%params = %transform;
						else
							%params = %mount.getDataBlock().observeParameters;

						%obj.setOrbitMode(%mount, %mount.getTransform(), getWord( %params, 0 ), getWord( %params, 1 ), getWord( %params, 2 ));
						%client.observeClient = %prevClient;
					}

					// send the message(s)
					displayObserverHud(%client, %prevClient);
					if (!%client.isAdmin || (%client.isAdmin && !$atfadmin::HideAdminObserverMessages))
					{
						messageClient(%prevClient, 'Observer', '\c1%1 is now observing you.', %client.name);
						messageClient(%prevObsClient, 'ObserverEnd', '\c1%1 is no longer observing you.', %client.name);
					}
				}
			}

			// press JUMP
			else if (%trigger == 2)
			{
				// update the observer list for this client
				observerFollowUpdate( %client, -1, false );

				// toggle back to observer fly mode
				%obj.mode = "observerFly";
				%obj.setFlyMode();
				updateObserverFlyHud(%client);
				if (!%client.isadmin)
					messageClient(%client.observeClient, 'ObserverEnd', '\c1%1 is no longer observing you.', %client.name);
			}

		case "pre-game":
			if ((!$Host::TournamentMode && !$Host::PickupMode) || $CountdownStarted)
				return;

			%matchtype = $Host::PickupMode ? "pickup" : "match";

			if (%client.notReady)
			{
				%client.notReady = "";
				MessageAll(0, '\c1%1 is READY.', %client.name);
				if (%client.notReadyCount < 3)
					centerprint(%client, "\nWaiting for" SPC %matchtype SPC "start (FIRE if not ready)", 0, 3);
				else
					centerprint(%client, "\nWaiting for" SPC %matchtype SPC "start", 0, 3);
			}
			else
			{
				%client.notReadyCount++;
				if (%client.notReadyCount < 4)
				{
					%client.notReady = true;
					MessageAll(0, '\c1%1 is not READY.', %client.name);
					centerprint(%client, "\nPress FIRE when ready.", 0, 3);
				}
				return;
			}
			CheckTourneyMatchStart();
	}
}


};
// END package atfadmin_camera
