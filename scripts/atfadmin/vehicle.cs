package atfadmin_vehicle
{


function VehicleData::damageObject(%data, %targetObject, %sourceObject, %position, %amount, %damageType, %momVec, %theClient, %proj)
{
	Parent::damageObject(%data, %targetObject, %sourceObject, %position, %amount, %damageType, %momVec, %theClient, %proj);

	// leave now if tournament mode is enabled
	if ($Host::TournamentMode) return;

	%damager = %targetObject.lastDamagedBy;
	%targetTeam = getTargetSensorGroup(%targetObject.getTarget());

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

	if (isObject(%targetObject))
	{
		%data = %targetObject.getDataBlock();
		%vehicleType = getTaggedString(%data.targetTypeTag);
		if (%vehicleType !$= "MPB")
			%vehicleType = strlwr(%vehicleType);

		if (%damager.team == %targetTeam && %damager.lastVehicleDamaged != %targetObject)
		{
			for (%i = 0; %i < %targetObject.getDatablock().numMountPoints; %i++)
			{
				if (%targetObject.getMountNodeObject(%i))
				{
					%passenger = %targetObject.getMountNodeObject(%i);
					if (isObject(%passenger))
						messageClient(%passenger.client, 'MsgATFAdminCommand', '\c5%1 damaged your %2!', %damager.name, %vehicleType);
				}
			}
			%gender = (%damager.sex $= "Male" ? 'his' : 'her');
			messageAdmins('MsgATFAdminCommand', '\c5%1 damaged %2 own team\'s %3!', %damager.name, %gender, %vehicleType);
			%damager.lastVehicleDamaged = %targetObject;
		}
	}
}

// the next two functions are to fix a bug with
// deploying MPBs at tourney match start
function VehicleData::onAdd(%data, %obj)
{
	Parent::onAdd(%data, %obj);

	if ((%data.sensorData !$= "") && (%obj.getTarget() != -1))
		setTargetSensorData(%obj.getTarget(), %data.sensorData);

	%obj.setRechargeRate(%data.rechargeRate);
	// set full energy
	%obj.setEnergyLevel(%data.MaxEnergy);

	if (%obj.disableMove)
		%obj.immobilized = true;

	if (%obj.deployed)
	{
		if ($CountdownStarted)
			%data.schedule(($Host::WarmupTime * 1000) / 2, "vehicleDeploy", %obj, 0, 1);
		else if ($Host::TournamentMode || $Host::PickupMode)
			%data.schedule(500, "checkDeployTime", %obj);
		else
		{
			$VehiclesDeploy[$NumVehiclesDeploy] = %obj;
			$NumVehiclesDeploy++;
		}
	}

	if (%obj.mountable || %obj.mountable $= "")
		%data.isMountable(%obj, true);
	else
		%data.isMountable(%obj, false);

	%obj.setSelfPowered();
}

function VehicleData::checkDeployTime(%data, %obj)
{
	if ($CountdownStarted)
		%data.schedule((30 * 1000) / 2, "vehicleDeploy", %obj, 0, 1);
	else
		%data.schedule(500, "checkDeployTime", %obj);
}


};
