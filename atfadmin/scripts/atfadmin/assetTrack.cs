// START package atfadmin_assetTrack

package atfadmin_assetTrack
{


function atfadmin_killCheck(%asset)
{
	// don't bother if the match hasn't started
	if (!$MatchStarted) return;

	// get the client number of the player who disabled the asset
	%damager = %asset.lastDamagedBy;

	// if the damager is not a client, treat it differently
	if (%damager && !ClientGroup.isMember(%damager))
	{
		if (isObject(%damager.getControllingClient()))
		{
			if (%damager.getControllingClient() != 0)
			{
				// asset was destroyed by controlled turret
				// or piloted vehicle
				%damager = %damager.getControllingClient();
			}
		}
		else return; // destroyed by a nearby explosion
	}

	// announce the transgression to the appropriate parties
	if (isObject(%damager))
		atfadmin_announceAssetState(%damager, %asset, "disabled");
	else
		return; // client not found
}

function atfadmin_repairCheck(%asset)
{
	// don't bother if the match hasn't started
	if (!$MatchStarted) return;

	// get the client number of the player who enabled the asset
	%repairman = %asset.repairedBy;

	// if the repairman is not a client, treat it differently
	// i'm not sure if this would ever be a problem...
	if (!ClientGroup.isMember(%repairman))
		return;

	// announce the repair message
	if (isObject(%repairman))
		atfadmin_announceAssetState(%repairman, %asset, "enabled");
	else
		return; // client not found
}

function atfadmin_logAssetMsg(%client, %asset, %state)
{
	// get the asset name tag
	%assetNameTag = atfadmin_GetPlainText(%asset.nameTag);

	// get the team
	%team = (%client.team == %asset.team) ? "same" : "other";

	logEcho(%client.nameBase SPC "(cl" SPC %client @ "/guid" SPC %client.guid @ ")" SPC $atfadmin_assetAction[%state, %team] SPC "(" @ %asset.dataBlock @ ") (" @ %assetNameTag @ ")");
}

function atfadmin_formatAssetMsg(%client, %asset, %state, %popup)
{
	// Get the actual name of the player who changed the asset state
	// include the tribe tag here
	%clientName = atfadmin_GetPlainText(%client.name);

	// if there's a nameTag associated with the asset, display it
	if (%asset.nameTag !$= "")
		%assetNameTag = getTaggedString(%asset.nameTag) @ " ";
	else
		%assetNameTag = "";

	%team = (%client.team == %asset.team) ? "same" : "other";

	// Get a nicely-formatted asset datablock string
	%assetType = atfadmin_dataBlockToString(%asset.dataBlock);

	%assetDetailString = %assetNameTag @ %assetType;

	// set the colors
	if (%popup)
	{
		%colorName = "<color:dddd00>";
		%colorAsset = "<color:dddd00>";
		%colorAction = ((%state $= "enabled" && %team $= "same") || (%state $= "disabled" && %team $= "other")) ? "<color:00dd00>" : "<color:dd0000>";
	}
	else
	{
		%ColorName = "\c2";
		%ColorAsset = "\cr";
		%colorAction = ((%state $= "enabled" && %team $= "same") || (%state $= "disabled" && %team $= "other")) ? "\c2" : "\c4";
	}

	// return the colorized message
	return %colorName @ %clientName SPC %colorAction @ $atfadmin_assetAction[%state, %team] @ %colorAsset @ ":" SPC %assetDetailString;
}

function atfadmin_announceAssetState(%client, %asset, %state)
{
	// is the asset on the same team as the player
	// or a neutral object?
	%team = (%client.team == %asset.team || %asset.team == 0) ? "same" : "other";

	%tellAdmins = ((%state $= "disabled" && %team $= "same") || (%state $= "enabled" && %team $= "other")) ? true : false;

	// log this to the server console log
	if ($atfadmin::AssetTrack[%asset.dataBlock, %state] & 1)
		atfadmin_logAssetMsg(%client, %asset, %state);

	// leave now if we aren't displaying asset messages
	if (!$atfadmin_assetTrack)
		return;

	// send the chat message
	if ($atfadmin::AssetTrack[%asset.dataBlock, %state] & 2)
	{
		if (%tellAdmins)
		{
			messageTeamExceptAdmins(%client.team, 'MsgATFAdminCommand', atfadmin_formatAssetMsg(%client, %asset, %state, false));
			messageAdmins('MsgATFAdminCommand', atfadmin_formatAssetMsg(%client, %asset, %state, false));
		}
		else
			messageTeam(%client.team, 'MsgATFAdminCommand', atfadmin_formatAssetMsg(%client, %asset, %state, false));
	}

	// send the popup message
	if ($atfadmin::AssetTrack[%asset.dataBlock, %state] & 4)
	{
		if (%tellAdmins)
		{
			bottomPrintTeamExceptAdmins(%client.team, atfadmin_formatAssetMsg(%client, %asset, %state, true), 8);
			bottomPrintAdmins(atfadmin_formatAssetMsg(%client, %asset, %state, true), 8);
		}
		else
			bottomPrintTeam(%client.team, atfadmin_formatAssetMsg(%client, %asset, %state, true), 8);
	}
}

function atfadmin_dataBlockToString(%assetType)
{
	switch$ (%assetType)
	{
		case "GeneratorLarge":
			%assetType = "Generator";

		case "SolarPanel":
			%assetType = "Solar Panel";

		case "StationInventory":
			%assetType = "Inventory Station";

		case "DeployedStationInventory":
			%assetType = "Deployed Inventory Station";

		case "StationVehicle":
			%assetType = "Vehicle Station";

		case "SensorLargePulse":
			%assetType = "Large Pulse Sensor";

		case "SensorMediumPulse":
			%assetType = "Medium Pulse Sensor";

		case "DeployedMotionSensor":
			%assetType = "Deployed Motion Sensor";

		case "DeployedPulseSensor":
			%assetType = "Deployed Pulse Sensor";

		case "SentryTurret":
			%assetType = "Sentry Turret";

		case "TurretBaseLarge":
			%assetType = "Base Turret";

		case "TurretDeployedOutdoor":
			%assetType = "Landspike Turret";

		case "TurretDeployedWallIndoor":
			%assetType = "Spider Clamp Turret";

		case "TurretDeployedFloorIndoor":
			%assetType = "Spider Clamp Turret";

		case "TurretDeployedCeilingIndoor":
			%assetType = "Spider Clamp Turret";

		default: // missed something
			%assetType = "Asset";
	}

	return %assetType;
}

function ShapeBaseData::onDisabled(%data, %obj)
{
	Parent::onDisabled(%data, %obj);

	if (atfadmin_isTrackedAsset(%obj, "disabled"))
		atfadmin_killCheck(%obj);
}

function ShapeBaseData::onEnabled(%data, %obj)
{
	Parent::onEnabled(%data, %obj);

	if (atfadmin_isTrackedAsset(%obj, "enabled"))
		atfadmin_repairCheck(%obj);
}

function atfadmin_isTrackedAsset(%asset, %state)
{
	return $atfadmin::AssetTrack[%asset.dataBlock, %state];
}

function atfadmin_assetTrackToggle(%client)
{
	if (%client.isSuperAdmin || (%client.isAdmin && $atfadmin::AllowAdminToggleAssetTracking))
	{
		%cause = "(admin:" @ %client.nameBase @ ")";

		if ($atfadmin_assetTrack)
		{
			%setto = "disabled";
			$atfadmin_assetTrack = false;
		}
		else
		{
			%setto = "enabled";
			$atfadmin_assetTrack = true;
		}

		logEcho("asset tracking" SPC %setto SPC %cause);
		messageAll('MsgAdminForce', '\c2Asset tracking has been %1 by %2.', %setto, %client.nameBase);
	}
}


};
// END package atfadmin_assetTrack

// set up message strings
$atfadmin_assetAction[enabled, same] = "enabled a team asset";
$atfadmin_assetAction[enabled, other] = "repaired an enemy asset";
$atfadmin_assetAction[disabled, same] = "TEAMKILLED a team asset";
$atfadmin_assetAction[disabled, other] = "disabled an enemy asset";
