# Tribes 2 Command-Line Tools

A collection of scripts to manage the development and packaging of various VL2 files, i.e. mods, maps, and skins.

These scripts were ones I might run in a Linux terminal
with the Linux client and server port of Tribes 2 by Loki Games.
Paths and some commands need to be changed for use on a Windows platform.

## Scripts

Brief summaries below, but definitely check the scripts themselves to fully understand what they do.

### [dfpcheck](dfpcheck)

I would use this to check a map prior to adding it to my server.
The script checks a user-created map VL2 for the presence of client-side
assets used by the "Dynamix Final Pack" (DFP) collection of maps.

Why was this helpful? Because including those assets (or any custom client-side
assets) in a VL2 would require clients to have that map pack downloaded ahead
of time and would (sadly) kick them from the server if they didn't have them
pre-installed.

### [makemap](makemap)

Creates a VL2 map archive from a set of source files.
Needs to be run from the parent of the map's `missions` directory.

### [makemod](makemod)

Creates a VL2 mod archive from a set of source files.

### [makeskins](makeskins)

Creates a VL2 skins archive from a set of source files.
Needs to be run from the parent of the skins' `textures` directory.

### [mapwipe](mapwipe)

Used to delete files from an extracted map VL2.
Needs to be run from the parent of the map's `missions` directory.

### [rmdso](rmdso)

Removes shared objects compiled by the Torque Engine after Tribes 2 is launched.

Shared object files (*.dso files) are cached and are not recompiled until
they're deleted and Tribes 2 is subsequently relaunched, making iterative
development and debugging of custom scripts a challenge unless this script is
used between map/script (re-)builds.

### [sf](sf)

Remove extraneous/modified fields in mission files (ending in *.mis).

Extraneous fields are automatically added by Tribes 2 when the mission file is saved via the map editor. These fields aren't necessary in the packaged map VL2 archives and can result in unwanted/problematic resource states, so they should be removed.

This script is just a wrapper to invoke `sed` on the provided files using the commands in [`stripfields.sed`](stripfields.sed).

### [skinwipe](skinwipe)

Used to delete files from an extracted skins VL2.
Needs to be run from the parent of the skins' `textures` directory.

### [t2hangcheck](t2hangcheck)

A script to check if the Linux version of the Tribes 2 server had crashed.

The Loki Tribes 2 server would sometimes hang on Linux. I used to run this via
cron every 5 minutes or so to automatically restart the server if it had stopped
logging, which was usually an indicator that the server had hung and needed
to be restarted.
