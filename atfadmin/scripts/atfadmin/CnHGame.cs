// START package atfadmin_CnHGame
package atfadmin_CnHGame
{


function CnHGame::initGameVars(%game)
{
	// set the defaults
	Parent::initGameVars(%game);

	// override the defaults

	// default of 1200 points per tower required to win @ 1 pt per %game.TIME_REQ_TEAM_HOLD_BONUS milliseconds
	%game.SCORE_LIMIT_PER_TOWER = ($atfadmin::CnHTowerValue > 0) ? $atfadmin::CnHTowerValue : 1200;

	// player must hold a switch 12 seconds to get a point for it
	%game.TIME_REQ_PLYR_CAP_BONUS =	($atfadmin::CnHPlayerPointTime > 0) ? $atfadmin::CnHPlayerPointTime : 12;
	%game.TIME_REQ_PLYR_CAP_BONUS *= 1000;

	// time after touching it takes for team to get a point
	%game.TIME_REQ_TEAM_CAP_BONUS = ($atfadmin::CnHTeamPointTime > 0) ? $atfadmin::CnHTeamPointTime : 12;
	%game.TIME_REQ_TEAM_CAP_BONUS *= 1000;
}


};
// END package atfadmin_CnHGame
