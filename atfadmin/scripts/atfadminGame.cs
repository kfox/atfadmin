// DisplayName = atfadmin

//----------------------------------------------------------------------
// functions
//----------------------------------------------------------------------

function atfadmin_initialize()
{
	// atfadmin version
	$atfadmin_Version = "2.3.3";

	// force verbose logging
	$logEchoEnabled = true;
	setlogmode(1);

	// parent mod (e.g., classic or base)
	$atfadmin_parentMod = getSubStr(getModPaths(), 0, strStr(getModPaths(), ";"));

	// compile and load (but don't activate) the packaged scripts
	%search = "scripts/atfadmin/*.cs";
	for (%file = findFirstFile(%search); %file !$= ""; %file = findNextFile(%search))
		exec("scripts/atfadmin/" @ fileBase(%file) @ ".cs");

	// load defaults
	exec("scripts/atfadminDefaults.cs");

	// load the prefs if they exist...
	// and find out if the admin wants atfadmin enabled
	// otherwise, run atfadmin to create the prefs file
	if (isFile("prefs/atfadmin.cs"))
		exec("prefs/atfadmin.cs");

	if (!isFile("prefs/atfadmin.cs") || atfadmin_upgraded())
		ATFAdminGame::ExportPrefs();

	ATFAdminGame::Enable();
	atfadmin_overrideServerDefaults();
}

function atfadmin_upgraded()
{
	// upgrade from older versions of atfadmin
	// returns true if upgrade performed, false otherwise
	%upgraded = false;

	if ($atfadmin::AllowAdminTournamentMode !$= "")
	{
		// changed in 2.3.0
		$atfadmin::AllowAdminChangeMode = $atfadmin::AllowAdminTournamentMode;
		$atfadmin::AllowAdminTournamentMode = "";
		%upgraded = true;
	}

	if ($atfadmin::AllowPlayerVoteTournamentMode !$= "")
	{
		// changed in 2.3.0
		$atfadmin::AllowPlayerVoteChangeMode = $atfadmin::AllowPlayerVoteTournamentMode;
		$atfadmin::AllowPlayerVoteTournamentMode = "";
		%upgraded = true;
	}

	if ($atfadmin::RotateAllMaps !$= "")
	{
		// removed in 2.3.0
		$atfadmin::RotateAllMaps = "";
		%upgraded = true;
	}

	return %upgraded;
}

function ATFAdminGame::activatePackages(%game)
{
	$atfadminPackages = "";
	%search = "scripts/atfadmin/*.cs";

	for (%file = findFirstFile(%search); %file !$= ""; %file = findNextFile(%search))
	{
		%basename = fileBase(%file);
		%packageName = "atfadmin_" @ %basename;
		if (isPackage(%packageName) && !isActivePackage(%packageName))
		{
			if (%packageName $= "atfadmin_stats" && !$atfadmin::EnableStats)
				continue;

			warn("activating" SPC %packageName SPC "package...");
			activatePackage(%packageName);
			$atfadminPackages = (getFieldCount($atfadminPackages) > 0) ? $atfadminPackages TAB %packageName : %packageName;
		}
	}
}

function ATFAdminGame::deactivatePackages(%game)
{
	for (%i = 0; %i < getFieldCount($atfadminPackages); %i++)
	{
		%packageName = getField($atfadminPackages, %i);
		if (isActivePackage(%packageName))
		{
			warn("deactivating" SPC %packageName SPC "package...");
			deactivatePackage(%packageName);
		}
	}
	$atfadminPackages = "";
}

function ATFAdminGame::Enable()
{
	// activate the packages
	ATFAdminGame::activatePackages();

	// create a map rotation file if none exists
	// and it is desired
	if ($atfadmin::MapRotationFile !$= "" && $atfadmin::MapRotationFile !$= "none" && !isFile("prefs/"@$atfadmin::MapRotationFile))
		atfadmin_CreateMapRotationFile();

	// build the mission list
	buildMissionList();

	// count our admins and set pw if necessary
	AdminAutoPWCheck(true);

	// turn on asset tracking
	if ($atfadmin::AssetTracking > 0)
		$atfadmin_assetTrack = true;
}

function ATFAdminGame::ExportPrefs()
{
	warn("exporting atfadmin prefs...");
	if (isFile("prefs/atfadmin.cs.dso"))
		deleteFile("prefs/atfadmin.cs.dso");
	export("$atfadmin::*", "prefs/atfadmin.cs", false);
}


//----------------------------------------------------------------------
// code to run the first time this file is exec'ed
//----------------------------------------------------------------------

schedule(0, 0, "atfadmin_initialize");
