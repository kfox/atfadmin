package atfadmin_lightning
{


function atfadmin_lightningStrike(%client)
{
	// return if client is dead or gone
	if(!%client.player)
		return;

	// number of simultaneous lightning bolts
	%bolts = 1;

	// get player position
	%pos = %client.player.position;
	%x = getword(%pos, 0);
	%y = getword(%pos, 1);
	%z = getword(%pos, 2) + 100;
	%startpos = %x SPC %y SPC %z;

	// create the lightning bolts
	for (%i = 0; %i < %bolts; %i++)
	{
		// randomize the color for each bolt
		%color = getrandom() SPC getrandom() SPC getrandom() SPC 1;

		// create a new object
		%lightning[%i] = new Lightning()
		{
			position = %startpos;
			rotation = "1 1 0 0";
			scale = "15 15 300";
			dataBlock = "DefaultStorm";
			strikesPerMinute = "300";
			strikeWidth = 1 + getrandom() * 3;
			chanceToHitTarget = 99;
			strikeRadius = (getrandom() * 50) + 20;
			boltStartRadius = getrandom() * 15;
			color = %color;
			fadeColor = getword(%color, 0)/3 SPC getword(%color, 1)/3 SPC getword(%color, 2)/3 SPC 0.2;
			useFog = "0";
		};

		// remove the lightning
		schedule(1000, 0, atfadmin_endLightningStrike, %lightning[%i]);
	}
}

function atfadmin_endLightningStrike(%obj)
{
	%obj.delete();
}


};
