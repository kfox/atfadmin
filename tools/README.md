# Tribes 2 Command-Line Tools

A collection of scripts to manage the development and packaging of various VL2 files, i.e. mods, maps, and skins.

These are meant to run on Linux, using GNU utilities and the Linux port of Tribes 2 by Loki Games. Paths would need to be changed for use on a Windows platform.

## Scripts

Brief summaries below, but definitely check the scripts themselves to fully understand what they do.

### dfpcheck

Checks a user-created map VL2 for the presence of client-side
assets used by the Dynamix Final Pack collection of maps.

Including those assets in a VL2 would require clients to have
that map pack downloaded ahead of time and would (sadly) kick them from
the server if they didn't.

### makemap

Creates a VL2 map archive from a set of source files.
Needs to be run from the parent of the map's `missions` directory.

### makemod

Creates a VL2 mod archive from a set of source files.

### makeskins

Creates a VL2 skins archive from a set of source files.
Needs to be run from the parent of the skins' `textures` directory.

### mapwipe

Used to delete files from an extracted map VL2.
Needs to be run from the parent of the map's `missions` directory.

### rmdso

Removes shared objects compiled by the Torque Engine after Tribes 2 is launched.

Shared object files (*.dso files) are cached and not recompiled until they're deleted and Tribes 2 is subsequently relaunched, making iterative development and debugging of custom scripts a challenge unless this script is used between map builds.

### sf

Remove extraneous/modified fields in mission files (ending in *.mis).

Extra fields are automatically added by Tribes 2 when the mission is saved from the map editor. These fields aren't necessary in the packaged map VL2 archives, and can result in unwanted resource states.

This script is just a wrapper to invoke `sed` on the provided files using the commands in `stripfields.sed`.

### skinwipe

Used to delete files from an extracted skins VL2.
Needs to be run from the parent of the skins' `textures` directory.

### t2hangcheck

A script to check if the Linux version of the Tribes 2 server had crashed. It would restart the server if needed.
