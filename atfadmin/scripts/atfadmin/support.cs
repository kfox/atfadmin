// START package atfadmin_support
package atfadmin_support
{


function atfadmin_GetPlainText(%tag)
{
	return stripChars(detag(getTaggedString(%tag)), "\cp\co\c6\c7\c8\c9");
}

function atfadmin_BotNum()
{
	%botCount = 0;
	for (%i = 0; %i < ClientGroup.getCount(); %i++)
	{
		%cl = ClientGroup.getObject(%i);
		if (%cl.isAIcontrolled()) %botCount++;
	}

	return %botCount;
}

function atfadmin_FindTarget(%param)
{
	for (%i = 0; %i < ClientGroup.getCount(); %i++)
	{
		%target = ClientGroup.getObject(%i);
		%name = %target.nameBase;
		if (($PlayingOnline && %target.guid == %param) || %name $= %param || %target == %param)
			return %target;
	}

	return -1;
}

function atfadmin_OutrankTarget(%client, %target)
{
	if (%client.isSuperAdmin)
		return !(%target.isSuperAdmin || %target.wasSuperAdmin);
	else if (%client.isAdmin)
		return !(%target.isAdmin || %target.wasAdmin || %target.wasSuperAdmin);
	else if (%client.isCaptain)
		return !%target.isCaptain;
	else if (%client.isPrivileged)
		return !%target.isPrivileged;

	return false;
}

function atfadmin_PadWithZeros(%num, %len)
{
	%current = strlen(%num);

	for (%i = 0; %i < (%len-%current); %i++)
		%num = "0"@%num;

	return %num;
}

function atfadmin_GetParams(%paramlist)
{
	deleteVariables("$atfadmin_param*");
	%i = %pos = 0;
	while (%pos < 256 && %i < 20)
	{
		%pos = strstr(%paramlist, "\"");
		if (%pos == 0)
		{
			%paramList = getSubStr(%paramList, 1, 256);
			%pos = strstr(%paramlist, "\"");
			if (%pos == -1) return -1;
			$atfadmin_param[%i] = getSubStr(%paramList, 0, %pos);
			%paramList = getSubStr(%paramList, %pos+2, 256);
		}
		else
		{
			%pos = strstr(%paramlist, " ");
			if (%pos == -1) %pos = 256;
			$atfadmin_param[%i] = getSubStr(%paramList, 0, %pos);
			if (%pos < 256) %paramList = getSubStr(%paramList, %pos+1, 256);
		}
		%i++;
	}

	return %i;
}

function atfadmin_getMissionNameFromFileName(%filename)
{
	for (%i = 0; %i < $HostMissionCount; %i++)
		if ($HostMissionFile[%i] $= %filename)
			return $HostMissionName[%i];

	error("couldn't find mission name for" SPC %filename);
	return $HostMissionName[0]; // failsafe
}

function atfadmin_getMissionIndexFromFileName(%filename)
{
	for (%i = 0; %i < $MissionCount; %i++)
	{
		if ($MissionList[%i, MissionFileName] $= %filename)
			return %i;
	}

	error("couldn't find mission id for" SPC %filename);
	return 0; // failsafe
}

function atfadmin_getMissionTypeIdFromDisplayName(%displayName)
{
	for (%type = 0; %type < $HostTypeCount; %type++)
		if (%displayName $= $HostTypeDisplayName[%type])
			return %type;

	error("couldn't find mission type id for" SPC %displayName);
	return 0; // failsafe
}

function atfadmin_getMissionTypeDisplayNameFromTypeName(%typename)
{
	for (%type = 0; %type < $HostTypeCount; %type++)
		if (%typename $= $HostTypeName[%type])
			return $HostTypeDisplayName[%type];

	error("couldn't find mission type display name for" SPC %typename);
	return $MissionTypeDisplayName; // failsafe
}

function atfadmin_validateMission(%filename, %typename)
{
	for (%i = 0; %i < $MissionCount; %i++)
	{
		if (($MissionList[%i, MissionFileName] $= %filename) &&
			($MissionList[%i, TypeName] $= %typename))
			return %i;
	}

	error("couldn't validate mission" SPC %filename SPC "(" @ %typename @ ")");
	return "";
}

function atfadmin_getMissionDisplayNameFromFileName(%filename)
{
	for (%i = 0; %i < $MissionCount; %i++)
	{
		if ($MissionList[%i, MissionFileName] $= %filename)
			return $MissionList[%i, MissionDisplayName];
	}

	return %filename;
}

function atfadmin_hasAdmin(%client)
{
	return (%client.isAdmin || %client.isSuperAdmin || %client.wasAdmin || %client.wasSuperAdmin);
}

function atfadmin_getPlayerCount(%team)
{
	if (%team $= "")
		%team = 0;

	%count = 0;
	for (%i = 0; %i < ClientGroup.getCount(); %i++)
	{
		%client = ClientGroup.getObject(%i);
		if (%client.team == 0)
			%count++;
	}
}


};
// END package atfadmin_support
