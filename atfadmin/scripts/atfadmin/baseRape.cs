package atfadmin_baseRape
{


function StaticShapeData::damageObject(%data, %targetObject, %sourceObject, %position, %amount, %damageType) 
{
	%baseRapeAllowed = baseRapeAllowed(%targetObject.team);

	switch$ (Game.class)
	{
		case "CnHGame":

			// Capture and Hold:
			//   Don't allow either team's station invos,
			//   gens, or vehicle pads to be TK'ed or
			//   raped if the owning team has less than
			//   $atfadmin::BaseRapeMinimumPlayers players

			if (!%baseRapeAllowed && (%targetObject.dataBlock $= "GeneratorLarge" || %targetObject.dataBlock $= "SolarPanel" || %targetObject.dataBlock $= "StationInventory" || %targetObject.dataBlock $= "StationVehicle"))
			{
				if (%sourceObject.client > 0)
				{
					if (%sourceObject.client.team != %targetObject.team)
					{
						// object belongs to the other team
						if (%sourceObject.client.rapeObj != %targetObject)
						{
							messageClient(%sourceObject.client, 'MsgATFAdminCommand', '\c2You cannot rape the other team\'s base when they have less than %1 players.', $atfadmin::BaseRapeMinimumPlayers);
							messageAdmins('MsgATFAdminCommand', '\c2%1 base rape attempt by %2.', $teamname[%targetObject.team], %sourceObject.client.name);
							%sourceObject.client.rapeObj = %targetObject;
						}
					}
					else
					{
						// object belongs to the same team
						if (%sourceObject.client.rapeObj != %targetObject)
						{
							messageClient(%sourceObject.client, 'MsgATFAdminCommand', '\c2You cannot teamkill your team\'s equipment when your team has less than %1 players.', $atfadmin::BaseRapeMinimumPlayers);
							messageAdmins('MsgATFAdminCommand', '\c2%1 base teamkill attempt by %2.', $teamname[%targetObject.team], %sourceObject.client.name);
							%sourceObject.client.rapeObj = %targetObject;
						}
					}
				}
			}
			else
				Parent::damageObject(%data, %targetObject, %sourceObject, %position, %amount, %damageType);

		case "CTFGame":

			// Capture the Flag:
			//   Don't allow either team's station invos,
			//   gens, or vehicle pads to be raped if the
			//   other team has less than
			//   $atfadmin::BaseRapeMinimumPlayers players

			if (!%baseRapeAllowed && (%targetObject.dataBlock $= "GeneratorLarge" || %targetObject.dataBlock $= "SolarPanel" || %targetObject.dataBlock $= "StationInventory" || %targetObject.dataBlock $= "StationVehicle"))
			{
				if (%sourceObject.client && %sourceObject.client.team != %targetObject.team)
				{
					if (%sourceObject.client.rapeObj != %targetObject)
					{
						messageClient(%sourceObject.client, 'MsgATFAdminCommand', '\c2You cannot rape the other team\'s base when they have less than %1 players.', $atfadmin::BaseRapeMinimumPlayers);
						messageAdmins('MsgATFAdminCommand', '\c2%1 base rape attempt by %2.', $teamname[%targetObject.team], %sourceObject.client.name);
						%sourceObject.client.rapeObj = %targetObject;
					}
				}
			}
			else
				Parent::damageObject(%data, %targetObject, %sourceObject, %position, %amount, %damageType);

		case "DnDGame":

			// Defend and Destroy:
			//   Don't allow any non-objective object to
			//   be destroyed if the owning team has less than
			//   $atfadmin::BaseRapeMinimumPlayers players

			if (%targetObject.scoreValue && %sourceObject.client > 0 && %sourceObject.client.team == %targetObject.team)
			{
				// don't allow players to tk their own team's objectives... DUH!
				if (%sourceObject.client.rapeObj != %targetObject)
				{
					messageClient(%sourceObject.client, 'MsgATFAdminCommand', '\c2You cannot destroy your own team\'s objectives.');
					messageAdmins('MsgATFAdminCommand', '\c2Objective teamkill attempt by %1.', %sourceObject.client.name);
					%sourceObject.client.rapeObj = %targetObject;
				}
			}
			else if (!%targetObject.scoreValue && !%baseRapeAllowed)
			{
				if (%sourceObject.client && %sourceObject.client.team != %targetObject.team)
				{
					if (%sourceObject.client.rapeObj != %targetObject)
					{
						messageClient(%sourceObject.client, 'MsgATFAdminCommand', '\c2You cannot rape the other team\'s base when they have less than %1 players.', $atfadmin::BaseRapeMinimumPlayers);
						messageAdmins('MsgATFAdminCommand', '\c2%1 base rape attempt by %2.', $teamname[%targetObject.team], %sourceObject.client.name);
						%sourceObject.client.rapeObj = %targetObject;
					}
				}
			}
			else
				Parent::damageObject(%data, %targetObject, %sourceObject, %position, %amount, %damageType);

		case "SiegeGame":

			// Siege:
			//   Don't allow any non-deployable object to
			//   be destroyed if it belongs to the Offense
			//   AND the offensive team has less than
			//   $atfadmin::BaseRapeMinimumPlayers players

			if (!%targetObject.getDataBlock().deployedObject && %targetObject.team == Game.offenseTeam && !%baseRapeAllowed)
			{
				if (%sourceObject.client && %sourceObject.client.team != %targetObject.team)
				{
					if (%sourceObject.client.rapeObj != %targetObject)
					{
						messageClient(%sourceObject.client, 'MsgATFAdminCommand', '\c2You cannot rape the offensive base when the offensive team has less than %1 players.', $atfadmin::BaseRapeMinimumPlayers);
						messageAdmins('MsgATFAdminCommand', '\c2Offensive base rape attempt by %1.', %sourceObject.client.name);
						%sourceObject.client.rapeObj = %targetObject;
					}
				}
			}
			else
				Parent::damageObject(%data, %targetObject, %sourceObject, %position, %amount, %damageType);

		default:
			// we have no clue (or don't care) about other gametypes
			Parent::damageObject(%data, %targetObject, %sourceObject, %position, %amount, %damageType);
	}
}

function baseRapeAllowed(%team)
{
	return (Game.teamCount[%team] >= $atfadmin::BaseRapeMinimumPlayers || $atfadmin_alwaysAllowBaseRape) ? true : false;
}

function atfadmin_baseRapeToggle(%client)
{
	if (%client.isSuperAdmin || (%client.isAdmin && $atfadmin::AllowAdminToggleBaseRape))
	{
		%cause = "(admin:" @ %client.nameBase @ ")";

		if ($atfadmin_alwaysAllowBaseRape)
		{
			%setto = "disabled";
			$atfadmin_alwaysAllowBaseRape = false;
		}
		else
		{
			%setto = "enabled";
			$atfadmin_alwaysAllowBaseRape = true;
		}

		logEcho("base rape" SPC %setto SPC %cause);
		messageAll('MsgAdminForce', '\c2Unconditional base rape has been %1 by %2.', %setto, %client.nameBase);
	}
}


};
