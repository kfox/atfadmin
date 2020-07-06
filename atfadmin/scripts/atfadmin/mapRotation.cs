// START package atfadmin_mapRotation
package atfadmin_mapRotation
{


function atfadmin_getNextMissionID(%missionID)
{
	if (%missionID $= "")
		return 0; // default to the first mission

	%i = %missionID;
	%found = false;
	%failsafe = -1;
	%numPlayers = ClientGroup.getCount();

	// loop until we find a suitable map
	while (!%found && %failsafe < $MissionCount)
	{
		if ($atfadmin_customMapRotation)
		{
			// go forward for custom rotation
			%i++;

			// wrap around if needed
			if (%i >= $MissionCount)
				%i = 0;
		}
		else
		{
			// if we aren't using a custom rotation...
			// go BACKWARD, because the missions are normally
			// in reverse alphabetical order
			if (%i == 0)
				%i = $MissionCount - 1;
			else
				%i--;
		}

		%nextMissionID = %i;

		if ($atfadmin::MixGametypes || ($MissionList[%missionID, TypeIndex] $= $MissionList[%nextMissionID, TypeIndex]))
		{
			// proceed
		}
		else
		{
			// no match, keep looking
			%failsafe++;
			continue;
		}

		// get the player count limits for this mission
		%nextMissionFileName = $MissionList[%nextMissionID, MissionFileName];
		%nextMissionTypeName = $MissionList[%nextMissionID, TypeName];
		%limits = $Host::MapPlayerLimits[%nextMissionFileName, %nextMissionTypeName];

		if (%limits !$= "")
		{
			%minPlayers = getWord(%limits, 0);
			%maxPlayers = getWord(%limits, 1);

			if ((%minPlayers < 0 || %numPlayers >= %minPlayers) && (%maxPlayers < 0 || %numPlayers <= %maxPlayers))
			{
				// proceed
			}
			else
			{
				// no match, keep looking
				%failsafe++;
				continue;
			}
		}

		// if the server has bots but there is no
		// bot support in the selected map, then keep trying...
		// otherwise, we're done
		if ($HostGameBotCount > 0 && !$MissionList[%nextMissionID, BotEnabled])
			%found = false;
		else
			%found = true;

		%failsafe++;
	}

	// if we didn't find a suitable mission,
	// just return the original
	if (!%found) %nextMissionID = %missionID;

	return %nextMissionID;
}

function buildMissionList()
{
	if ($atfadmin::MapRotationFile !$= "none" && isFile("prefs/" @ $atfadmin::MapRotationFile))
	{
		$atfadmin_customMapRotation = true;
		atfadmin_mapReset();
		exec("prefs/" @ $atfadmin::MapRotationFile);
		atfadmin_mapFinish();
	}
	else
	{
		$atfadmin_customMapRotation = false;

		%search = "missions/*.mis";
		%ct = 0;
		$HostTypeCount = 0;
		$HostMissionCount = 0;
		$MissionCount = 0;
		%fobject = new FileObject();

		for (%file = findFirstFile(%search); %file !$= ""; %file = findNextFile(%search))
		{
			%name = fileBase(%file); // get the name

			// if skip list file exists, make sure this mission isn't in skip list
			// if mission is on the skip list then remove it from rotation
			if (isFile("prefs/MissionSkip.cs"))
			{
				%found = false;
				for (%i = 0; $SkipMission::name[%i] !$= ""; %i++)
				{
					if ($SkipMission::name[%i] $= %name)
					{
						%found = true;
						break;
					}
				}

				if (%found) continue;
			}

			%idx = $HostMissionCount;
			$HostMissionCount++;
			$HostMissionFile[%idx] = %name;
			$HostMissionName[%idx] = %name;

			if (!%fobject.openForRead(%file))
			{
				error(%file SPC "is unreadable");
				continue;
			}

			%typeList = "None";
			while (!%fobject.isEOF())
			{
				%line = %fobject.readLine();

				if (getSubStr(%line, 0, 17) $= "// DisplayName = ")
				{
					// override the mission name
					$HostMissionName[%idx] = getSubStr(%line, 17, 1000);
				}
				else if (getSubStr(%line, 0, 18) $= "// MissionTypes = ")
				{
					%typeList = getSubStr(%line, 18, 1000);
					break;
				}
			}
			%fobject.close();

			// don't include single player missions
			if (strstr(%typeList, "SinglePlayer") != -1)
				continue;

			// test to see if the mission is bot-enabled
			%navFile = "terrains/" @ %name @ ".nav";
			$BotEnabled[%idx] = isFile(%navFile);

			for (%word = 0; (%misType = getWord(%typeList, %word)) !$= ""; %word++)
			{
				if (%misType $= "CTF")
					%typeList = rtrim(%typeList) @ " PracticeCTF SCtF";

				if (isFile("prefs/GameTypeSkip.cs"))
				{
					%found = false;
					for (%i = 0; $SkipType::name[%i] !$= ""; %i++)
					{
						if ($SkipType::name[%i] $= %misType)
						{
							%found = true;
							break;
						}
					}

					if (%found) continue;
				}

				// don't include TR2 missions if TR2 is turned off
				if (("TR2" $= %misType) && (!$Host::ClassicLoadTR2Gametype))
					continue;

				for (%i = 0; %i < $HostTypeCount; %i++)
					if ($HostTypeName[%i] $= %misType)
						break;

				if (%i == $HostTypeCount)
				{
					$HostTypeCount++;
					$HostTypeName[%i] = %misType;
					$HostMissionCount[%i] = 0;
				}

				// add the mission to the type
				%ct = $HostMissionCount[%i];
				$HostMission[%i, $HostMissionCount[%i]] = %idx;
				$HostMissionCount[%i]++;

				// add the mission to the master mission list
				$MissionList[$MissionCount, MissionFileName] = $HostMissionFile[%idx];
				$MissionList[$MissionCount, MissionDisplayName] = $HostMissionName[%idx];
				$MissionList[$MissionCount, TypeIndex] = %i;
				$MissionList[$MissionCount, TypeName] = $HostTypeName[%i];
				$MissionList[$MissionCount, TypeDisplayName] = $HostTypeDisplayName[%i];
				$MissionCount++;
			}
		}

		atfadmin_mapFinish();
		%fobject.delete();
	}
}

function atfadmin_mapReset()
{
	deleteVariables("$MissionList*");
	deleteVariables("$HostMission*");
	deleteVariables("$HostType*");
	deleteVariables("$BotEnabled*");
	deleteVariables("$IsClientSide*");
	deleteVariables("$IsVoteableOnly*");
	$HostMissionCount = 0;
	$HostTypeCount = 0;
	$MissionCount = 0;
}

function atfadmin_mapFinish()
{
	getMissionTypeDisplayNames();

	for (%i = 0; %i < $MissionCount; %i++)
		$MissionList[%i, TypeDisplayName] = $HostTypeDisplayName[$MissionList[%i, TypeIndex]];
}

function atfadmin_mapAdd(%file, %type, %min, %max, %clientSide)
{
	// Either get mission type id or create a new one
	for (%id = 0; %id < $HostTypeCount; %id++)
		if ($HostTypeName[%id] $= %type)
			break;

	if (%id == $HostTypeCount)
	{
		$HostTypeCount++;
		$HostTypeName[%id] = %type;
		$HostMissionCount[%id] = 0;
	}

	%fobject = new FileObject();

	// Either get the file number or create a new one
	for (%n = 0; %n < $HostMissionCount; %n++)
		if ($HostMissionFile[%n] $= %file)
			break;

	if (%n == $HostMissionCount)
	{
		$HostMissionCount++;
		$HostMissionFile[%n] = %file;
		$HostMissionName[%n] = %file;
		$HostMissionType[%n] = %id;

		if (%fobject.openForRead("missions/"@%file@".mis"))
		{
			while (!%fobject.isEOF())
			{
				%line = %fobject.readLine();
				if (getSubStr(%line, 0, 17) $= "// DisplayName = ")
				{
					// override the mission name
					$HostMissionName[%n] = getSubStr(%line, 17, 1000);
				}
			}
			%fobject.close();
		}

		%navFile = "terrains/"@%file@".nav";
		$BotEnabled[%n] = isFile(%navFile);

	}

	%m = $HostMissionCount[%id];
	$HostMission[%id, %m] = %n;
	$HostMissionCount[%id]++;

	// begin the master missionlist setup
	$MissionList[$MissionCount, MissionFileName] = %file;
	$MissionList[$MissionCount, TypeIndex] = %id;
	$MissionList[$MissionCount, TypeName] = $HostTypeName[%id];
	$MissionList[$MissionCount, TypeDisplayName] = $HostTypeDisplayName[%id];
	$MissionList[$MissionCount, BotEnabled] = $BotEnabled[%n];
	$MissionList[$MissionCount, MissionDisplayName] = $HostMissionName[%n];
	$MissionList[$MissionCount, MinPlayers] = (%min !$= "") ? %min : "";
	$MissionList[$MissionCount, MaxPlayers] = (%max !$= "") ? %max : "";

	if (%min !$= "" && %max !$= "")
		$Host::MapPlayerLimits[%file, %type] = %min SPC %max;
	else
		$Host::MapPlayerLimits[%file, %type] = "";

	if (%min > $Host::MaxPlayers)
		$IsVoteableOnly[%n] = true;
	else
		$IsVoteableOnly[%n] = false;

	$MissionList[$MissionCount, VoteableOnly] = $IsVoteableOnly[%n];

	if (%clientSide)
		$IsClientSide[%n] = true;
	else
		$IsClientSide[%n] = false;

	$MissionList[$MissionCount, ClientSide] = $IsClientSide[%n];

	%fobject.delete();
	$MissionCount++;
}

function atfadmin_assignHostMissionTypes()
{
	for (%type = 0; %type < $HostTypeCount; %type++)
		for (%index = 0; %index < $HostMissionCount[%type]; %index++)
			$HostMissionType[$HostMission[%type, %index]] = %type;
}

function atfadmin_CreateMapRotationFile()
{
	%fobject = new FileObject();
	%search = "missions/*.mis";
	%typecount = 0;

	for(%file = findFirstFile(%search); %file !$= ""; %file = findNextFile(%search))
	{
		%name = fileBase(%file); // get the name

		if (!%fobject.openForRead(%file)) continue;

		%typeList = "None";

		while (!%fobject.isEOF())
		{
			%line = %fobject.readLine();
			if (getSubStr(%line, 0, 18) $= "// MissionTypes = ")
			{
				%typeList = getSubStr(%line, 18, 1000);
				break;
			}
		}
		%fobject.close();

		// Don't include single player missions:
		if (strstr(%typeList, "SinglePlayer") != -1) continue;

		// convert tabs to spaces
		%typeList = strreplace(%typeList, "" TAB "", " ");

		for(%word = 0; (%type = getWord(%typeList, %word)) !$= ""; %word++)
		{
			for (%i = 0; %i < %typecount; %i++)
			if (%typename[%i] $= %type)
				break;
			if (%i == %typecount)
			{
				%typecount++;
				%typename[%i] = %type;
				%missioncount[%i] = 0;
			}

			%m = %missioncount[%i];
			%mission[%i, %m] = %name;
			%missioncount[%i]++;
		}
	}

	echo("Creating atfadmin Map Rotation file: prefs/" @ $atfadmin::MapRotationFile);

	%fobject.openForWrite("prefs/"@$atfadmin::MapRotationFile);

	%fobject.writeLine("// ----------------------------");
	%fobject.writeLine("// atfadmin custom Map Rotation");
	%fobject.writeLine("// ----------------------------");
	%fobject.writeLine("//");
	%fobject.writeLine("// Use this file to change the map rotation Tribes 2 uses, when a map ends this");
	%fobject.writeLine("// list is used to decide the next map.  To add maps or edit maps on this list");
	%fobject.writeLine("// use the following command:");
	%fobject.writeLine("//");
	%fobject.writeLine("//   atfadmin_mapAdd(%file, %type, %min, %max, %clientSide);");
	%fobject.writeLine("//     %file - Filename of the map.");
	%fobject.writeLine("//     %type - Game type.");
	%fobject.writeLine("//     %min  - min num of players required, -1 for unrestricted (optional)");
	%fobject.writeLine("//     %min  - max num of players required, -1 for unrestricted (optional)");
	%fobject.writeLine("//     %clientSide - true if the map is client-side,");
	%fobject.writeLine("//                   false (or unset) if the map is server-side");
	%fobject.writeLine("//");
	%fobject.writeLine("// eg: atfadmin_mapAdd(\"Sanctuary\", CTF, -1, 32);");
	%fobject.writeLine("//     atfadmin_mapAdd(\"Fracas\", DM);");
	%fobject.writeLine("//");
	%fobject.writeLine("// If you add custom maps to the server you can either add them in by hand or");
	%fobject.writeLine("// use the following function from the server console to rebuild the custom map");
	%fobject.writeLine("// rotation file:");
	%fobject.writeLine("//");
	%fobject.writeLine("//   atfadmin_CreateMapRotationFile();");
	%fobject.writeLine("//     WARNING: This file will be overwritten so make sure you aren't losing any");
	%fobject.writeLine("//     changes.");
	%fobject.writeLine("//");
	%fobject.writeLine("// Notes:");
	%fobject.writeLine("// 1) Any maps not on this list will not be in the rotation or the server menus.");
	%fobject.writeLine("//    You can add custom maps to this list that do not come with Tribes2.");
	%fobject.writeLine("// 2) By default Tribes 2 rotates through the maps of the same type, so when a");
	%fobject.writeLine("//    CTF map ends the next map will be CTF also. There is an option to enable");
	%fobject.writeLine("//    rotation of all maps on the list.");
	%fobject.writeLine("// 3) Min/max number of players is optional and if not included the map will");
	%fobject.writeLine("//    always be in the rotation.");
	%fobject.writeLine("// 4) You need to add the map for each of the types you need to it played for.");
	%fobject.writeLine("//    For instance SunDried is a DM, Hunters, Bounty and Rabbit map.");
	%fobject.writeLine("// 5) If the map name has spaces in it you must include it in quotes, eg:");
	%fobject.writeLine("//    atfadmin_mapAdd(\"Custom Map\", CTF);");
	%fobject.writeLine("// 6) Set the min/max players to 65 and 65 if you want to be sure a map is");
	%fobject.writeLine("//    voteable but not in regular rotation.");
	%fobject.writeLine("//");

	for (%i = 0; %i < %typecount; %i++)
	{
		%type = %typename[%i];
		%fobject.writeLine("");
		%fobject.writeLine("//");
		%fobject.writeLine("// --- " @ %type @ " ---");
		%fobject.writeLine("//");
		%fobject.writeLine("");

		for (%j = %missioncount[%i] - 1; %j >= 0; %j--)
		{
			%file = %mission[%i, %j];
			%min = getWord($Host::MapPlayerLimits[%file, %type], 0);
			%max = getWord($Host::MapPlayerLimits[%file, %type], 1);

			if (%min $= "") %min = "-1";
			if (%max $= "") %max = "-1";

			%text = "atfadmin_mapAdd(\"" @ %file @ "\", " @ %type @ ", " @ %min @ ", " @ %max @ ", false);";

			%fobject.writeLine(%text);
		}

		if (%type $= "CTF" && $atfadmin_parentMod $= "classic")
		{
			%fobject.writeLine("");
			%fobject.writeLine("//");
			%fobject.writeLine("// --- PracticeCTF ---");
			%fobject.writeLine("//");
			%fobject.writeLine("");

			for (%j = %missioncount[%i] - 1; %j >= 0; %j--)
			{
				%file = %mission[%i, %j];
				%min = getWord($Host::MapPlayerLimits[%file, %type], 0);
				%max = getWord($Host::MapPlayerLimits[%file, %type], 1);

				if (%min $= "") %min = "-1";
				if (%max $= "") %max = "-1";

				%text = "atfadmin_mapAdd(\"" @ %file @ "\", PracticeCTF, " @ %min @ ", " @ %max @ ", false);";

				%fobject.writeLine(%text);
			}

			%fobject.writeLine("");
			%fobject.writeLine("//");
			%fobject.writeLine("// --- SCtF ---");
			%fobject.writeLine("//");
			%fobject.writeLine("");

			for (%j = %missioncount[%i] - 1; %j >= 0; %j--)
			{
				%file = %mission[%i, %j];
				%min = getWord($Host::MapPlayerLimits[%file, %type], 0);
				%max = getWord($Host::MapPlayerLimits[%file, %type], 1);

				if (%min $= "") %min = "-1";
				if (%max $= "") %max = "-1";

				%text = "atfadmin_mapAdd(\"" @ %file @ "\", SCtF, " @ %min @ ", " @ %max @ ", false);";

				%fobject.writeLine(%text);
			}
		}
	}

	%fobject.close();
	%fobject.delete();
}


};
// END package atfadmin_mapRotation
