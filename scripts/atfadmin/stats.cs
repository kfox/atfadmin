$DamageName[$DamageType::Blaster] = "Blaster";
$DamageName[$DamageType::Plasma] = "Plasma";
$DamageName[$DamageType::Bullet] = "Bullet";
$DamageName[$DamageType::Disc] = "Disc";
$DamageName[$DamageType::Grenade] = "Grenade";
$DamageName[$DamageType::Laser] = "Laser";
$DamageName[$DamageType::ELF] = "ELF";
$DamageName[$DamageType::Mortar] = "Mortar";
$DamageName[$DamageType::Missile] = "Missile";
$DamageName[$DamageType::ShockLance] = "ShockLance";
$DamageName[$DamageType::Mine] = "Mine";
$DamageName[$DamageType::Explosion] = "Explosion";
$DamageName[$DamageType::Impact] = "Impact";

//$DamageName[$DamageType::Ground] = "Ground";
//$DamageName[$DamageType::Turret] = "Turret";
//$DamageName[$DamageType::PlasmaTurret] = "PlasmaTurret";
//$DamageName[$DamageType::AATurret] = "AATurret";
//$DamageName[$DamageType::ElfTurret] = "ElfTurret";
//$DamageName[$DamageType::MortarTurret] = "MortarTurret";
//$DamageName[$DamageType::MissileTurret] = "MissileTurret";
//$DamageName[$DamageType::IndoorDepTurret] = "IndoorDepTurret";
//$DamageName[$DamageType::OutdoorDepTurret] = "OutdoorDepTurret";
//$DamageName[$DamageType::SentryTurret] = "SentryTurret";
//$DamageName[$DamageType::OutOfBounds] = "OutOfBounds";
//$DamageName[$DamageType::Lava] = "Lava";
//$DamageName[$DamageType::ShrikeBlaster] = "ShrikeBlaster";
//$DamageName[$DamageType::BellyTurret] = "BellyTurret";
//$DamageName[$DamageType::BomberBombs] = "BomberBombs";
//$DamageName[$DamageType::TankChaingun] = "TankChaingun";
//$DamageName[$DamageType::TankMortar] = "TankMortar";
//$DamageName[$DamageType::SatchelCharge] = "SatchelCharge";
//$DamageName[$DamageType::MPBMissile] = "MPBMissile";
//$DamageName[$DamageType::Lightning] = "Lightning";
//$DamageName[$DamageType::VehicleSpawn] = "VehicleSpawn";
//$DamageName[$DamageType::ForceFieldPowerup] = "ForceFieldPowerup";
//$DamageName[$DamageType::Crash] = "Crash";
//$DamageName[$DamageType::NexusCamping] = "NexusCamping";
//$DamageName[$DamageType::Suicide] = "Suicide";

package atfadmin_stats
{


function atfadmin_initStats()
{
	// start with a clean slate
	deleteVariables("$atfstats::*");

	// MA height is 12m for TR2, 6m for everything else
	$atfstats::MAHeight = ($CurrentMissionType !$= "TR2") ? 6 : 12;

	// keep 5 high scores
	$atfstats::TopScores = 5;

	// list of damage types to track
	$atfstats::damageTypes =
		"Blaster\tBlaster" TAB
		"Plasma\tPlasma Rifle" TAB
		"Bullet\tChaingun" TAB
		"Disc\tSpinfusor" TAB
		"Grenade\tGrenade" TAB
		"Laser\tLaser Rifle" TAB
		"Mortar\tFusion Mortar" TAB
		"Missile\tMissile Launcher" TAB
		"ShockLance\tShocklance" TAB
		"Mine\tMine" TAB
		"Explosion\tExplosion" TAB
		"Impact\tImpact"
	;

	// default file name for stats storage
	%file = "scores/" @ $CurrentMissionType @ "/" @ $CurrentMission @ ".cs";

	if (isFile(%file))
	{
		// load the high scores
		exec(%file);
	}
}

function atfstats_clientMissionDropReady(%client)
{
	// add this client to the clientlist for later processing
	atfstats_addToClientList(%client);

	// store the name of this client
	// in case they drop before stats are shown
	$atfstats::names[%client] = %client.nameBase;

	// zero out the various stats counters for this client
	$atfstats::selfkills[%client] = 0;
	$atfstats::teamkills[%client] = 0;
	$atfstats::genkills[%client] = 0;
	$atfstats::genrepairs[%client] = 0;
	$atfstats::headshots[%client, Count] = 0;
	$atfstats::headshots[%client, MaxDistance] = 0;
	$atfstats::rearshots[%client, Count] = 0;
	$atfstats::rearshots[%client, MaxDistance] = 0;

	for (%i = 0; (%type = getField($atfstats::damageTypes, %i)) !$= ""; %i+=2)
	{
		$atfstats::damage[%client, %type] = 0;
		$atfstats::kills[%client, %type] = 0;
		$atfstats::deaths[%client, %type] = 0;
		$atfstats::midairs[%client, %type, Count] = 0;
		$atfstats::midairs[%client, %type, MaxDistance] = 0;
	}
}

function atfstats_addToClientList(%client)
{
	%count = getFieldCount($atfstats::clientlist);

	for (%i = 0; %i < %count; %i++)
	{
		%field = getField($atfstats::clientlist, %i);
		%cl = getWord(%field, 0);
		%id = getWord(%field, 1);

		if (%id == %client.guid)
		{
			// someone dropped and rejoined
			// before the mission was over...
			// update the info and bail out
			$atfstats::clientlist = setField($atfstats::clientlist, %i, %client SPC %client.guid);

			// set the new values and
			// erase the old values
			$atfstats::names[%cl] = "";

			// teamkills
			$atfstats::teamkills[%client] = $atfstats::teamkills[%cl];
			$atfstats::teamkills[%cl] = "";

			// selfkills
			$atfstats::selfkills[%client] = $atfstats::selfkills[%cl];
			$atfstats::selfkills[%cl] = "";

			// genkills
			$atfstats::genkills[%client] = $atfstats::genkills[%cl];
			$atfstats::genkills[%cl] = "";

			// genrepairs
			$atfstats::genrepairs[%client] = $atfstats::genrepairs[%cl];
			$atfstats::genrepairs[%cl] = "";

			// headshots
			$atfstats::headshots[%client, Count] = $atfstats::headshots[%cl, Count];
			$atfstats::headshots[%cl, Count] = "";
			$atfstats::headshots[%client, MaxDistance] = $atfstats::headshots[%cl, MaxDistance];
			$atfstats::headshots[%cl, MaxDistance] = "";

			// rearshots
			$atfstats::rearshots[%client, Count] = $atfstats::rearshots[%cl, Count];
			$atfstats::rearshots[%cl, Count] = "";
			$atfstats::rearshots[%client, MaxDistance] = $atfstats::rearshots[%cl, MaxDistance];
			$atfstats::rearshots[%cl, MaxDistance] = "";

			for (%i = 0; (%type = getField($atfstats::damageTypes, %i)) !$= ""; %i+=2)
			{
				// kills
				$atfstats::kills[%client, %type] = $atfstats::kills[%cl, %type];
				$atfstats::kills[%cl, %type] = "";

				// deaths
				$atfstats::deaths[%client, %type] = $atfstats::deaths[%cl, %type];
				$atfstats::deaths[%cl, %type] = "";

				// damage
				$atfstats::damage[%client, %type] = $atfstats::damage[%cl, %type];
				$atfstats::damage[%cl, %type] = 0;

				// midairs
				$atfstats::midairs[%client, %type, Count] = $atfstats::midairs[%cl, %type, Count];
				$atfstats::midairs[%cl, %type, Count] = "";
				$atfstats::midairs[%client, %type, MaxDistance] = $atfstats::midairs[%cl, %type, MaxDistance];
				$atfstats::midairs[%cl, %type, MaxDistance] = "";
			}

			return;
		}
	}

	// not found, so add to the list
	%field = %client SPC %client.guid;

	if (%count == 0)
		$atfstats::clientlist = %field;
	else
		$atfstats::clientlist = $atfstats::clientlist TAB %field;
}

function Armor::damageObject(%data, %targetObject, %sourceObject, %position, %amount, %damageType, %momVec, %mineSC)
{
	Parent::damageObject(%data, %targetObject, %sourceObject, %position, %amount, %damageType, %momVec, %mineSC);

	if (!$Host::TournamentMode)
		atfstats_handleDamageStat(%data, %targetObject, %sourceObject, %position, %amount, %damageType, %momVec, %mineSC);
}

function ProjectileData::onCollision(%data, %projectile, %targetObject, %modifier, %position, %normal)
{
	if (isObject(%targetObject) && !$Host::TournamentMode)
		atfstats_handleMAStat(%data, %projectile, %targetObject, %modifier, %position, %normal);

	Parent::onCollision(%data, %projectile, %targetObject, %modifier, %position, %normal);
}

function SniperProjectileData::onCollision(%data, %projectile, %targetObject, %modifier, %position, %normal)
{
	if (isObject(%targetObject) && !$Host::TournamentMode)
		atfstats_handleMAStat(%data, %projectile, %targetObject, %modifier, %position, %normal);

	Parent::onCollision(%data, %projectile, %targetObject, %modifier, %position, %normal);
}

function Generator::onEnabled(%data, %obj, %prevState)
{
	Parent::onEnabled(%data, %obj, %prevState);

	%repairman = %obj.repairedBy;
	if (%repairman.team == %obj.team)
		$atfstats::genrepairs[%repairman]++;
}

function Generator::onDisabled(%data, %obj)
{
	Parent::onDisabled(%data, %obj);

	%disabler = %obj.lastDamagedBy;
	if (%disabler.team != %obj.team)
		$atfstats::genkills[%disabler]++;
}

function atfstats_handleDamageStat(%data, %targetObject, %implement, %position, %amount, %damageType, %momVec, %mineSC)
{
	// sanity checks
	if (!isObject(%implement) || %implement $= "") return;
	if (!isObject(%targetObject) || %targetObject $= "") return;

	// don't count damage done to vehicles
	if (%targetObject.isMounted()) return;

	// get the victim
	%victim = %targetObject.client;
	%killer = %implement.client;

	if (%damageType == $DamageType::Impact) // run down by vehicle
	{
		if ((%controller = %implement.getControllingClient()) > 0)
		{
			// ... controlled by someone
			%killer = %controller;
		}
		else
		{
			// ... unmanned
			return;
		}
	}
	else if (isObject(%implement) && (%implement.getClassName() $= "Turret" || %implement.getClassName() $= "VehicleTurret" || %implement.getClassName() $= "FlyingVehicle" || %implement.getClassName() $= "HoverVehicle"))
	{
		if (%implement.getControllingClient() != 0)
		{
			// turret is being controlled
			%killer = %implement.getControllingClient();
		}
		// use the handle associated with the deployed object to verify valid owner
		else if (isObject(%implement.owner))
		{
			%killer = %implement.owner;

			if (%damageType == $DamageType::Default)
			{
				// automated kill, no owner
				return;
			}
		}
		else
		{
			// turret is not a placed (owned) turret (or owner is no longer on it's team),
			// and is not being controlled
			return;
		}
	}

	// no team damage stats are stored
	if (%killer.team == %victim.team) return;

	// store the damage
	$atfstats::damage[%killer, $DamageName[%damageType]] += %amount;

	// is it a laser damage?
	if (%damageType == $DamageType::Laser)
	{
		// only consider shots that have been fired with 60% of total energy
		if (%implement.getEnergyLevel() / %implement.getDataBlock().maxEnergy < 0.6) return;

		%distance = mFloor(VectorDist(%position, %implement.getWorldBoxCenter()));

		// is it a headshot?
		if (%victim.headshot)
		{
			$atfstats::headshots[%killer, Count]++;
			bottomPrint(%killer, "HEADSHOT #" @ $atfstats::headshots[%killer, Count] @ " -- Distance:" SPC %distance SPC "meters.", 4);
		}
		else // no
		{
			bottomPrint(%killer, "HIT! Distance:" SPC %distance SPC "meters.", 3);
		}

		if (%distance > $atfstats::headshots[%killer, MaxDistance])
			$atfstats::headshots[%killer, MaxDistance] = %distance;
	}
	else if (%damageType == $DamageType::ShockLance && %victim.rearshot)
	{
		$atfstats::rearshots[%killer, Count]++;
		%distance = mFloor(VectorDist(%position, %implement.getWorldBoxCenter()));
		bottomPrint(%killer, "REARSHOT #" @ $atfstats::rearshots[%killer, Count] @ " -- Distance:" SPC %distance SPC "meters.", 4);

		if (%distance > $atfstats::rearshots[%killer, MaxDistance])
			$atfstats::rearshots[%killer, MaxDistance] = %distance;
	}
}

function atfstats_handleKillStat(%game, %victim, %killer, %damageType, %implement, %damageLocation)
{
	if (%damageType == $DamageType::Impact) // run down by vehicle
	{
		if ((%controller = %implement.getControllingClient()) > 0)
		{
			// ... controlled by someone
			%killer = %controller;
		}
		else
		{
			// ... unmanned
			%killer = 0;
		}
	}
	else if (isObject(%implement) && (%implement.getClassName() $= "Turret" || %implement.getClassName() $= "VehicleTurret" || %implement.getClassName() $= "FlyingVehicle" || %implement.getClassName() $= "HoverVehicle"))
	{
		if (%implement.getControllingClient() != 0)
		{
			// turret is being controlled
			%killer = %implement.getControllingClient();
		}
		// use the handle associated with the deployed object to verify valid owner
		else if (isObject(%implement.owner))
		{
			%killer = %implement.owner;
		}
		else
		{
			// turret is not a placed (owned) turret (or owner is no longer on it's team),
			// and is not being controlled
			%killer = 0;
		}
	}

	if (isObject(%killer))
	{
		if (%game.numTeams > 1 && %killer.team == %victim.team && %killer != %victim)
		{
			// a teamkill
			$atfstats::teamkills[%killer]++;
		}
		else if (%killer == %victim)
		{
			// self-kill
			$atfstats::selfkills[%killer]++;
		}
		else
		{
			// normal kill
			$atfstats::kills[%killer, $DamageName[%damageType]]++;
		}
	}

	if (isObject(%victim))
	{
		$atfstats::deaths[%victim, $DamageName[%damageType]]++;
	}
}

function atfstats_handleMAStat(%data, %projectile, %targetObject, %modifier, %position, %normal)
{
	// sanity check
	if (!isObject(%targetObject) || %targetObject $= "") return;
	if (!isObject(%projectile.sourceObject) || %projectile.sourceObject $= "") return;

	// set the victim and attacker
	%victim = %targetObject.client;
	%attacker = %projectile.sourceObject.client;

	// sanity check
	if (!isObject(%victim) || !isObject(%attacker))
	{
		// someone doesn't exist...
		return;
	}

	// is the target in mid-air?
	if (!atfstats_heightQualifiesForMA(%targetObject))
	{
		// target was less than $atfstats::MAHeight above a surface
		return;
	}

	// is it a teamkill?
	if (Game.numTeams > 1 && %attacker.team == %victim.team)
	{
		// we don't track tk MA's for multi-team games
		return;
	}

	// get the name of the weapon used
	switch$ (%data.getName())
	{
		case "DiscProjectile":
			%projectileName = "Disc";
			%damageType = %data.radiusDamageType;

		case "PlasmaBolt":
			%projectileName = "Plasma";
			%damageType = %data.radiusDamageType;

		case "EnergyBolt":
			%projectileName = "Blaster";
			%damageType = %data.directDamageType;

		case "ChaingunBullet":
			%projectileName = "Chaingun";
			%damageType = %data.directDamageType;

		case "BasicSniperShot":
			%projectileName = "Sniper Rifle";
			%damageType = %data.directDamageType;

		default:
			%projectileName = %data.getName();
			%damageType = (%data.radiusDamageType $= "") ? %data.directDamageType : %data.radiusDamageType;
			echo("got:"@%data.getName());
	}

	// sanity check
	if (%damageType $= "")
	{
		// no damage type found... ?
		return;
	}

	if (%projectileName $= "") %projectileName = "Unknown";

	echo("MA attacker:"@%attacker SPC "target:"@%victim SPC "projectileName:"@%projectileName SPC "name:"@$DamageName[%damageType]);

	// increment the MA count for this projectile type
	$atfstats::midairs[%attacker, $DamageName[%damageType], Count]++;

	// how far was the shot?
	%distance = mFloor(VectorDist(%position, %projectile.initialPosition));

	// check if the distance for this MA is greater than
	// the player's personal best MA distance for this projectile type
	if (%distance > $atfstats::midairs[%attacker, $DamageName[%damageType], MaxDistance])
	{
		$atfstats::midairs[%attacker, $DamageName[%damageType], MaxDistance] = %distance;
	}

	// tell the attacker that he's r337, p3t1t3, and g0n3
	bottomPrint(%attacker, %projectileName SPC "MID-AIR #" @ $atfstats::midairs[%attacker, $DamageName[%damageType], Count] @ " -- Distance:" SPC %distance SPC "meters.", 4);
}

function atfstats_isNewHighScore(%score)
{
	%lastHighScore = $atfstats::TopScores - 1;

	if ($CurrentMissionType $= "Siege")
	{
		if ($atfstats::scores[%lastHighScore, Score] $= "" || %score < $atfstats::scores[%lastHighScore, Score])
			return true;
	}
	else
	{
		if ($atfstats::scores[%lastHighScore, Score] $= "" || %score > $atfstats::scores[%lastHighScore, Score])
			return true;
	}

	return false;
}

function atfstats_addNewHighScore(%playerName, %score)
{
	%lastHighScore = $atfstats::TopScores - 1;

	// we already know this is a high score,
	// so put it at the bottom of the list
	$atfstats::scores[%lastHighScore, Score] = %score;
	$atfstats::scores[%lastHighScore, Player] = %playerName;

	for (%i = (%lastHighScore - 1); %i >= 0; %i--)
	{
		if ($CurrentMissionType $= "Siege")
		{
			// for siege, lower capture times are better
			if (($atfstats::scores[%i, Score] > $atfstats::scores[%i + 1, Score]) || $atfstats::scores[%i, Score] $= "")
				%swap = true;
			else
				break;
		}
		else
		{
			// for other gametypes, higher point scores are better
			if (($atfstats::scores[%i, Score] < $atfstats::scores[%i + 1, Score]) || $atfstats::scores[%i, Score] $= "")
				%swap = true;
			else
				break;
		}

		if (%swap)
		{
			// swap scores
			%temp = $atfstats::scores[%i, Score];
			$atfstats::scores[%i, Score] = $atfstats::scores[%i + 1, Score];
			$atfstats::scores[%i + 1, Score] = %temp;

			// swap players
			%temp = $atfstats::scores[%i, Player];
			$atfstats::scores[%i, Player] = $atfstats::scores[%i + 1, Player];
			$atfstats::scores[%i + 1, Player] = %temp;

			%swap = false;
		}
	}
}

function BountyGame::sendDebriefing(%game, %client)
{
	if (!$Host::TournamentMode && $atfadmin::EnableStats)
		messageClient(%client, 'MsgDebriefResult', "", '<just:center>%1 (%2)', $MissionDisplayName, $MissionTypeDisplayName);

	Parent::sendDebriefing(%game, %client);

	if (!$Host::TournamentMode && $atfadmin::EnableStats)
		sendATFStatsDebriefing(%client);
}

function CnHGame::sendDebriefing(%game, %client)
{
	if (!$Host::TournamentMode && $atfadmin::EnableStats)
		messageClient(%client, 'MsgDebriefResult', "", '<just:center>%1 (%2)', $MissionDisplayName, $MissionTypeDisplayName);

	Parent::sendDebriefing(%game, %client);

	if (!$Host::TournamentMode && $atfadmin::EnableStats)
		sendATFStatsDebriefing(%client);
}

function CTFGame::sendDebriefing(%game, %client)
{
	if (!$Host::TournamentMode && $atfadmin::EnableStats)
		messageClient(%client, 'MsgDebriefResult', "", '<just:center>%1 (%2)', $MissionDisplayName, $MissionTypeDisplayName);

	Parent::sendDebriefing(%game, %client);

	if (!$Host::TournamentMode && $atfadmin::EnableStats)
		sendATFStatsDebriefing(%client);
}

function DMGame::sendDebriefing(%game, %client)
{
	if (!$Host::TournamentMode && $atfadmin::EnableStats)
		messageClient(%client, 'MsgDebriefResult', "", '<just:center>%1 (%2)', $MissionDisplayName, $MissionTypeDisplayName);

	Parent::sendDebriefing(%game, %client);

	if (!$Host::TournamentMode && $atfadmin::EnableStats)
		sendATFStatsDebriefing(%client);
}

function DnDGame::sendDebriefing(%game, %client)
{
	if (!$Host::TournamentMode && $atfadmin::EnableStats)
		messageClient(%client, 'MsgDebriefResult', "", '<just:center>%1 (%2)', $MissionDisplayName, $MissionTypeDisplayName);

	Parent::sendDebriefing(%game, %client);

	if (!$Host::TournamentMode && $atfadmin::EnableStats)
		sendATFStatsDebriefing(%client);
}

function HuntersGame::sendDebriefing(%game, %client)
{
	if (!$Host::TournamentMode && $atfadmin::EnableStats)
		messageClient(%client, 'MsgDebriefResult', "", '<just:center>%1 (%2)', $MissionDisplayName, $MissionTypeDisplayName);

	Parent::sendDebriefing(%game, %client);

	if (!$Host::TournamentMode && $atfadmin::EnableStats)
		sendATFStatsDebriefing(%client);
}

function RabbitGame::sendDebriefing(%game, %client)
{
	if (!$Host::TournamentMode && $atfadmin::EnableStats)
		messageClient(%client, 'MsgDebriefResult', "", '<just:center>%1 (%2)', $MissionDisplayName, $MissionTypeDisplayName);

	Parent::sendDebriefing(%game, %client);

	if (!$Host::TournamentMode && $atfadmin::EnableStats)
		sendATFStatsDebriefing(%client);
}

function SiegeGame::sendDebriefing(%game, %client)
{
	if (!$Host::TournamentMode && $atfadmin::EnableStats)
		messageClient(%client, 'MsgDebriefResult', "", '<just:center>%1 (%2)', $MissionDisplayName, $MissionTypeDisplayName);

	Parent::sendDebriefing(%game, %client);

	if (!$Host::TournamentMode && $atfadmin::EnableStats)
		sendATFStatsDebriefing(%client);
}

function TeamHuntersGame::sendDebriefing(%game, %client)
{
	if (!$Host::TournamentMode && $atfadmin::EnableStats)
		messageClient(%client, 'MsgDebriefResult', "", '<just:center>%1 (%2)', $MissionDisplayName, $MissionTypeDisplayName);

	Parent::sendDebriefing(%game, %client);

	if (!$Host::TournamentMode && $atfadmin::EnableStats)
		sendATFStatsDebriefing(%client);
}

function TR2Game::sendDebriefing(%game, %client)
{
	if (!$Host::TournamentMode && $atfadmin::EnableStats)
		messageClient(%client, 'MsgDebriefResult', "", '<just:center>%1 (%2)', $MissionDisplayName, $MissionTypeDisplayName);

	Parent::sendDebriefing(%game, %client);

	if (!$Host::TournamentMode && $atfadmin::EnableStats)
		sendATFStatsDebriefing(%client);
}

function sendATFStatsDebriefing(%client)
{
	//
	// high scores
	//

	if ($atfstats::scores[0, Score] !$= "")
	{
		if ($CurrentMissionType $= "Siege")
		{
			messageClient(%client, 'MsgDebriefAddLine', "", '\n<lmargin:0><spush><color:dddd00><font:univers condensed:22>TOP %1 FASTEST BASE CAPTURES<spop>', $atfstats::TopScores);
			messageClient(%client, 'MsgDebriefAddLine', "", '<lmargin:0><spush><color:bbbb00><font:univers condensed:18>RANK<lmargin%%:20>PLAYER<lmargin%%:60>TIME<spop>');
		}
		else
		{
			messageClient(%client, 'MsgDebriefAddLine', "", '\n<lmargin:0><spush><color:dddd00><font:univers condensed:22>TOP %1 HIGH SCORES<spop>', $atfstats::TopScores);
			messageClient(%client, 'MsgDebriefAddLine', "", '<lmargin:0><spush><color:bbbb00><font:univers condensed:18>RANK<lmargin%%:20>PLAYER<lmargin%%:60>SCORE<spop>');
		}

		for (%i = 0; $atfstats::scores[%i, Score] !$= ""; %i++)
		{
			messageClient(%client, 'MsgDebriefAddLine', "", '<lmargin:0><spush><color:00dc00><font:univers condensed:18>#%1<lmargin%%:20><clip%%:40>%2</clip><lmargin%%:60><clip%%:50>%3<spop>', %i + 1, $atfstats::scores[%i, Player], atfstats_formatScore($atfstats::scores[%i, Score]));
		}
	}

	// blank line
	messageClient(%client, 'MsgDebriefAddLine', "", '\n');

	//
	// genkills
	//

	if ($atfstats::detected["genkills"])
	{
		%champ = $atfstats::champs["genkills"];

		if (%champ)
		{
			%plural = ($atfstats::genkills[%champ] > 1) ? "s" : "";
			messageClient(%client, 'MsgDebriefAddLine', "", '<lmargin:0><spush><color:bbbb00><clip%%:30><font:univers condensed:18>TOP GENKILLER:</clip><color:00dc00><lmargin%%:30><clip%%:60>%1</clip><lmargin%%:60><clip%%:40>%2 genkill%3</clip><spop>', $atfstats::names[%champ], $atfstats::genkills[%champ], %plural);
		}
	}

	//
	// genrepairs
	//

	if ($atfstats::detected["genrepairs"])
	{
		%champ = $atfstats::champs["genrepairs"];

		if (%champ)
		{
			%plural = ($atfstats::genrepairs[%champ] > 1) ? "s" : "";
			messageClient(%client, 'MsgDebriefAddLine', "", '<lmargin:0><spush><color:bbbb00><clip%%:30><font:univers condensed:18>TOP GENREPPER:</clip><color:00dc00><lmargin%%:30><clip%%:60>%1</clip><lmargin%%:60><clip%%:40>%2 genrepair%3</clip><spop>', $atfstats::names[%champ], $atfstats::genrepairs[%champ], %plural);
		}
	}

	// blank line
	//messageClient(%client, 'MsgDebriefAddLine', "", '\n');

	//
	// midair
	//

	if ($atfstats::detected["midairs"])
	{
		messageClient(%client, 'MsgDebriefAddLine', "", '\n<lmargin:0><spush><color:dddd00><font:univers condensed:22>MID-AIR ACES<spop>');
		messageClient(%client, 'MsgDebriefAddLine', "", '<lmargin:0><spush><color:bbbb00><font:univers condensed:18>WEAPON<lmargin%%:30>PLAYER<lmargin%%:60>MAX DISTANCE<spop>');

		for (%i = 0; (%type = getField($atfstats::damageTypes, %i)) !$= ""; %i+=2)
		{
			%displayType = getField($atfstats::damageTypes, %i + 1);
			%champ = $atfstats::champs["midairs", %type];

			if (%champ)
			{
				messageClient(%client, 'MsgDebriefAddLine', "", '<lmargin:0><spush><color:00dc00><clip%%:40><font:univers condensed:18>%1</clip><lmargin%%:30><clip%%:60>%2 (%3 MA%5)</clip><lmargin%%:60><clip%%:60>%4m</clip><spop>', %displayType, $atfstats::names[%champ], $atfstats::midairs[%champ, %type, Count], $atfstats::midairs[%champ, %type, MaxDistance], ($atfstats::midairs[%champ, %type, Count] > 1) ? "s" : "");
			}
		}
	}

	//
	// kills
	//

	if ($atfstats::detected["kills"])
	{
		messageClient(%client, 'MsgDebriefAddLine', "", '\n<lmargin:0><spush><color:dddd00><font:univers condensed:22>TOP KILLERS<spop>');
		messageClient(%client, 'MsgDebriefAddLine', "", '<lmargin:0><spush><color:bbbb00><font:univers condensed:18>TYPE<lmargin%%:30>PLAYER<lmargin%%:60>KILLS<spop>');

		for (%i = 0; (%type = getField($atfstats::damageTypes, %i)) !$= ""; %i+=2)
		{
			%displayType = getField($atfstats::damageTypes, %i + 1);
			%champ = $atfstats::champs["kills", %type];

			if (%champ)
			{
				messageClient(%client, 'MsgDebriefAddLine', "", '<lmargin:0><spush><color:00dc00><clip%%:30><font:univers condensed:18>%1</clip><lmargin%%:30><clip%%:60>%2</clip><lmargin%%:60><clip%%:40>%3</clip><spop>', %displayType, $atfstats::names[%champ], $atfstats::kills[%champ, %type]);
			}
		}
	}

	//
	// damage
	//

	if ($atfstats::detected["damage"])
	{
		messageClient(%client, 'MsgDebriefAddLine', "", '\n<lmargin:0><spush><color:dddd00><font:univers condensed:22>TOP DAMAGERS<spop>');
		messageClient(%client, 'MsgDebriefAddLine', "", '<lmargin:0><spush><color:bbbb00><font:univers condensed:18>TYPE<lmargin%%:30>PLAYER<lmargin%%:60>DAMAGE<spop>');

		for (%i = 0; (%type = getField($atfstats::damageTypes, %i)) !$= ""; %i+=2)
		{
			%displayType = getField($atfstats::damageTypes, %i + 1);
			%champ = $atfstats::champs["damage", %type];

			if (%champ)
			{
				messageClient(%client, 'MsgDebriefAddLine', "", '<lmargin:0><spush><color:00dc00><clip%%:30><font:univers condensed:18>%1</clip><lmargin%%:30><clip%%:60>%2</clip><lmargin%%:60><clip%%:40>%3</clip><spop>', %displayType, $atfstats::names[%champ], $atfstats::damage[%champ, %type]);
			}
		}
	}

	//
	// deaths
	//

	if ($atfstats::detected["deaths"])
	{
		messageClient(%client, 'MsgDebriefAddLine', "", '\n<lmargin:0><spush><color:dddd00><font:univers condensed:22>TOP VICTIMS<spop>');
		messageClient(%client, 'MsgDebriefAddLine', "", '<lmargin:0><spush><color:bbbb00><font:univers condensed:18>TYPE<lmargin%%:30>PLAYER<lmargin%%:60>DEATHS<spop>');

		for (%i = 0; (%type = getField($atfstats::damageTypes, %i)) !$= ""; %i+=2)
		{
			%displayType = getField($atfstats::damageTypes, %i + 1);
			%champ = $atfstats::champs["deaths", %type];

			if (%champ)
			{
				messageClient(%client, 'MsgDebriefAddLine', "", '<lmargin:0><spush><color:00dc00><clip%%:30><font:univers condensed:18>%1</clip><lmargin%%:30><clip%%:60>%2</clip><lmargin%%:60><clip%%:40>%3</clip><spop>', %displayType, $atfstats::names[%champ], $atfstats::deaths[%champ, %type]);
			}
		}
	}

	// blank line
	messageClient(%client, 'MsgDebriefAddLine', "", '\n');

	//
	// rearshots
	//

	if ($atfstats::detected["rearshots"])
	{
		%champ = $atfstats::champs["rearshots"];

		if (%champ)
		{
			%plural = ($atfstats::rearshots[%champ, Count] > 1) ? "s" : "";
			messageClient(%client, 'MsgDebriefAddLine', "", '<lmargin:0><spush><color:bbbb00><clip%%:30><font:univers condensed:18>TOP REARSHOOTER:</clip><color:00dc00><lmargin%%:30><clip%%:60>%1</clip><lmargin%%:60><clip%%:40>%2 rearshot%3</clip><spop>', $atfstats::names[%champ], $atfstats::rearshots[%champ, Count], %plural);
		}
	}

	//
	// headshots
	//

	if ($atfstats::detected["headshots"])
	{
		%champ = $atfstats::champs["headshots"];

		if (%champ)
		{
			%plural = ($atfstats::headshots[%champ, Count] > 1) ? "s" : "";
			messageClient(%client, 'MsgDebriefAddLine', "", '<lmargin:0><spush><color:bbbb00><clip%%:30><font:univers condensed:18>TOP HEADSHOOTER:</clip><color:00dc00><lmargin%%:30><clip%%:60>%1</clip><lmargin%%:60><clip%%:40>%2 headshot%3</clip><spop>', $atfstats::names[%champ], $atfstats::headshots[%champ, Count], %plural);
		}
	}

	//
	// teamkills
	//

	if ($atfstats::detected["teamkills"])
	{
		%champ = $atfstats::champs["teamkills"];

		if (%champ)
		{
			%plural = ($atfstats::teamkills[%champ] > 1) ? "s" : "";
			messageClient(%client, 'MsgDebriefAddLine', "", '<lmargin:0><spush><color:bbbb00><clip%%:30><font:univers condensed:18>TOP TEAMKILLER:</clip><color:00dc00><lmargin%%:30><clip%%:60>%1</clip><lmargin%%:60><clip%%:40>%2 teamkill%3</clip><spop>', $atfstats::names[%champ], $atfstats::teamkills[%champ], %plural);
		}
	}

	//
	// selfkills
	//

	if ($atfstats::detected["selfkills"])
	{
		%champ = $atfstats::champs["selfkills"];

		if (%champ)
		{
			%plural = ($atfstats::selfkills[%champ] > 1) ? "s" : "";
			messageClient(%client, 'MsgDebriefAddLine', "", '\n<lmargin:0><spush><color:bbbb00><clip%%:30><font:univers condensed:18>TOP SELFKILLER:</clip><color:00dc00><lmargin%%:30><clip%%:60>%1</clip><lmargin%%:60><clip%%:40>%2 selfkill%3</clip><spop>', $atfstats::names[%champ], $atfstats::selfkills[%champ], %plural);
		}
	}
}

function DefaultGame::gameOver(%game)
{
	if ($atfadmin::EnableStats && !$Host::TournamentMode)
	{
		//if (atfstats_isNewHighScore(%cl.score))
		//{
		//	// yup, it's a high score, so add it to the top 5
		//	%name = stripChars(getTaggedString(%cl.name), "\cp\co\c6\c7\c8\c9");
		//	atfstats_addNewHighScore(%name, %cl.score);
		//}

		// save the high scores
		export("$atfstats::scores*", "scores/" @ $CurrentMissionType @ "/" @ $CurrentMission @ ".cs", false);

		// get the champions of each category

		%list = "midairs kills deaths damage";
		for (%i = 0; (%type = getField($atfstats::damageTypes, %i)) !$= ""; %i+=2)
		{
			for (%j = 0; (%category = getWord(%list, %j)) !$= ""; %j++)
			{
				eval("%detected = (($atfstats::champs[\"" @ %category @ "\", " @ %type @ "] = atfstats_findChampion(\"" @ %category @ "\", " @ %type @ ")) !$= \"\") ? true : false;");
				if (%detected) eval("$atfstats::detected[\"" @ %category @ "\"] = true;");
				//echo("category:"@%category SPC "type:"@%type SPC "detected:"@$atfstats::detected[%category] SPC "champ:"@$atfstats::champs[%category, %type]);
			}
		}

		%list = "genkills genrepairs selfkills teamkills headshots rearshots";
		for (%j = 0; (%category = getWord(%list, %j)) !$= ""; %j++)
		{
			eval("%detected = (($atfstats::champs[\"" @ %category @ "\"] = atfstats_findChampion(\"" @ %category @ "\")) !$= \"\") ? true : false;");
			if (%detected) eval("$atfstats::detected[\"" @ %category @ "\"] = true;");
			//echo("category:"@%category SPC "detected:"@$atfstats::detected[%category] SPC "champ:"@$atfstats::champs[%category]);
		}
	}

	Parent::gameOver(%game);
}

function atfstats_heightQualifiesForMA(%target)
{
	%origin = getWords(%target.getTransform(), 0, 2);
	%down = VectorAdd(%origin, "0 0 -10000");
	%mask = $TypeMasks::TerrainObjectType | $TypeMasks::StaticShapeObjectType | $TypeMasks::InteriorObjectType;
	%object = getWords(containerRayCast(%origin, %down, %mask, 0), 1, 3);

	if (VectorDist(%origin, %object) > $atfstats::MAHeight)
	{
		// target is in mid-air
		return true;
	}
	else
	{
		// target isn't in mid-air
		return false;
	}
}

function atfstats_invToName(%item)
{
	%name = "Unknown";

	%index = 0;
	%found = false;
	while (%name !$= "")
	{
		eval("%name = $InvWeapon[" @ %index @ "];");
		if (%name !$= "" && %item $= $NameToInv[%name])
		{
			%found = true;
			break;
		}
		%index++;
	}
	if (!%found)
		%name = "Unknown";

	return %name;
}

function atfstats_formatScore(%score)
{
	if ($CurrentMissionType $= "Siege")
	{
		%text = Game.formatTime(%score, true);
	}
	else
	{
		%text = %score;
	}

	return %text;
}

function atfstats_findChampion(%category, %type)
{
	%client = "";
	%numFields = getFieldCount($atfstats::clientlist);
	%max = 0;
	%count = 0;

	for (%i = 0; %i < %numFields; %i++)
	{
		%field = getField($atfstats::clientlist, %i);
		%cl = getWord(%field, 0);

		switch$ (%category)
		{
			case "midairs":
				eval("%count = $atfstats::" @ %category @ "[" @ %cl @ ", " @ %type @ ", Count];");

			case "damage":
				eval("%count = $atfstats::" @ %category @ "[" @ %cl @ ", " @ %type @ "];");

			case "headshots":
				eval("%count = $atfstats::" @ %category @ "[" @ %cl @ ", Count];");

			case "rearshots":
				eval("%count = $atfstats::" @ %category @ "[" @ %cl @ ", Count];");

			case "kills":
				eval("%count = $atfstats::" @ %category @ "[" @ %cl @ ", " @ %type @ "];");

			case "deaths":
				eval("%count = $atfstats::" @ %category @ "[" @ %cl @ ", " @ %type @ "];");

			case "genkills":
				eval("%count = $atfstats::" @ %category @ "[" @ %cl @ "];");

			case "genrepairs":
				eval("%count = $atfstats::" @ %category @ "[" @ %cl @ "];");

			case "selfkills":
				eval("%count = $atfstats::" @ %category @ "[" @ %cl @ "];");

			case "teamkills":
				eval("%count = $atfstats::" @ %category @ "[" @ %cl @ "];");

			default:
				error("unknown category:" SPC %category);
		}

		if (%count > %max)
		{
			%max = %count;
			%client = %cl;
		}
	}

	return %client;
}

function atfstats_dump()
{
	for(%i = 0; %i < $atfstats::TopScores; %i++)
	{
		echo("score:" SPC %i SPC $atfstats::scores[%i, Score] SPC $atfstats::scores[%i, Player]);
	}
}


};
