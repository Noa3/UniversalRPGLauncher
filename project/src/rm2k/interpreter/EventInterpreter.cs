using System;
using System.Collections.Generic;
using UniversalRPG.Rm2k.Simulation;
using UniversalRPG.Rm2k.Presentation;

namespace UniversalRPG.Rm2k.Interpreter;

/// <summary>
/// Event command interpreter for RM2K/2003 games.
/// Executes commands deterministically against a GameSimulationState.
/// No JavaScript, DLL, or native execution.
///
/// Command codes and parameter layouts are verified against the generated
/// liblcf table (src/generated/lcf/rpg/eventcommand.h) and EasyRPG Player's
/// interpreter implementation (game_interpreter.cpp, game_interpreter_map.cpp).
/// </summary>
/// <summary>
/// Flashes a named character, for command 11320.
/// </summary>
/// <param name="pEventId">The character the command named.</param>
/// <param name="pRed">The red channel, 0 to 31.</param>
/// <param name="pGreen">The green channel, 0 to 31.</param>
/// <param name="pBlue">The blue channel, 0 to 31.</param>
/// <param name="pStrength">The strength, 0 to 31.</param>
/// <param name="pFrames">How long the flash runs, already in frames.</param>
/// <param name="pWait">Whether the page is held by it.</param>
/// <returns>Whether a character carried that id.</returns>
/// <remarks>
/// <strong>Seven parameters is one too many for <see cref="Func{T, TResult}"/>,
/// and the reference's own <c>Flash</c> takes them all</strong> — so this is a
/// named delegate rather than a squeezed tuple. **A tuple would have made the
/// call site unreadable and the mistake at the hook hard to see.**
/// </remarks>
public delegate bool Rm2kSpriteFlash(
	int pEventId,
	int pRed,
	int pGreen,
	int pBlue,
	int pStrength,
	int pFrames,
	bool pWait);

public sealed class EventInterpreter
{
	public const int MaxScriptRecursion = 256;
	public const int MaxWaitFrames = 600; // 10 seconds at 60fps
	public const int MaxGold = 999999;
	public const int MaxItemId = 50000;
	public const int MaxItemCount = 999999;
	public const int GoldOpAdd = 0;
	public const int GoldOpSubtract = 1;
	public const int ItemOpAdd = 0;
	public const int ItemOpSubtract = 1;
	public const int ItemIdConstant = 0;
	public const int ItemIdVariable = 1;
	public const int PartyOpAdd = 0;
	public const int PartyOpRemove = 1;
	public const int ActorIdConstant = 0;
	public const int ActorIdVariable = 1;

	// Verified RM2K/2003 event command codes (liblcf lcf::rpg::Cmd).
	public const int End = 10;
	/// <summary>10120, Message Options, <c>CmdSetup</c> width 4.</summary>
	public const int MessageOptions = 10120;

	/// <summary>
	/// 10130, Change Face Graphic, from <c>Code::ChangeFaceGraphic</c>
	/// and <c>CommandChangeFaceGraphic</c>, whose <c>CmdSetup</c> gives
	/// it a minimum width of 3.
	/// </summary>
	public const int ChangeFaceGraphic = 10130;

	/// <summary>10230, Timer Operation, <c>CmdSetup</c> width 5.</summary>
	public const int TimerOperation = 10230;

	public const int ShowMessage = 10110;
	public const int ShowChoice = 10140;
	public const int InputNumber = 10150;
	public const int ChangeGold = 10310;
	public const int ChangeItems = 10320;
	public const int ChangePartyMembers = 10330;
	public const int ChangeExp = 10410;
	/// <summary>10430, Change Parameters, <c>CmdSetup</c> width 6.</summary>
	public const int ChangeParameters = 10430;

	/// <summary>10460, Change HP, from <c>CommandChangeHP</c>, width 6.</summary>
	public const int ChangeHP = 10460;

	/// <summary>10470, Change SP, from <c>CommandChangeSP</c>, width 5.</summary>
	public const int ChangeSP = 10470;

	public const int ChangeLevel = 10420;
	public const int ControlSwitches = 10210;
	public const int ControlVars = 10220;
	public const int Teleport = 10810;
	public const int Wait = 11410;
	public const int ConditionalBranch = 12010;
	public const int Loop = 12210;
	public const int BreakLoop = 12220;
	public const int Comment = 12410;
	public const int ChangeHeroName = 10610;
	/// <summary>
	/// 11110, Show Picture, from liblcf <c>Code::ShowPicture</c> and EasyRPG's
	/// <c>CommandShowPicture</c>, whose <c>CmdSetup</c> gives it a minimum
	/// width of 14.
	/// </summary>
	public const int ShowPicture = 11110;

	/// <summary>11130, Erase Picture, from the same pair.</summary>
	public const int ErasePicture = 11130;

	/// <summary>11120, Move Picture.</summary>
	public const int MovePicture = 11120;

	/// <summary>
	/// 11910, Open Save Menu, from liblcf <c>Code::OpenSaveMenu</c> and
	/// EasyRPG's <c>CommandOpenSaveMenu</c>.
	/// </summary>
	/// <remarks>
	/// <strong>Width 0, from the reference's own dispatch line</strong> — the
	/// command takes no parameters, and a reader that required one would refuse
	/// every game's save menu.
	/// </remarks>
	public const int OpenSaveMenu = 11910;

	/// <summary>
	/// 11950, Open Main Menu, from liblcf <c>Code::OpenMainMenu</c> and
	/// EasyRPG's <c>CommandOpenMainMenu</c>.
	/// </summary>
	/// <remarks>
	/// <strong>Width 0, from the same line</strong> and for the same reason.
	/// </remarks>
	public const int OpenMainMenu = 11950;

	/// <summary>11510, Play Background Music, from liblcf <c>Code::PlayBGM</c>.</summary>
	public const int PlayBGM = 11510;

	/// <summary>11520, Fade Out Background Music, from <c>Code::FadeOutBGM</c>.</summary>
	public const int FadeOutBGM = 11520;

	/// <summary>11530, Memorize Background Music.</summary>
	public const int MemorizeBGM = 11530;

	/// <summary>11540, Play Memorized Background Music.</summary>
	public const int PlayMemorizedBGM = 11540;

	/// <summary>11550, Play Sound Effect, from liblcf <c>Code::PlaySound</c>.</summary>
	public const int PlaySound = 11550;

	/// <summary>
	/// 12110, Label, from liblcf <c>Code::Label</c>.
	/// </summary>
	/// <remarks>
	/// <strong>The reference's case for it is <c>return true</c> and nothing
	/// else</strong> — it has no method, no parameters read, and no effect. A
	/// label is a name, not an instruction, and a reader that gave it one would
	/// invent a semantic the format does not have.
	/// </remarks>
	public const int Label = 12110;

	/// <summary>
	/// 12120, Jump to Label, from liblcf <c>Code::JumpToLabel</c> and EasyRPG's
	/// <c>CommandJumpToLabel</c>, whose <c>CmdSetup</c> gives it a minimum
	/// width of 1.
	/// </summary>
	public const int JumpToLabel = 12120;

	/// <summary>11010, Erase Screen, from liblcf <c>Code::EraseScreen</c>.</summary>
	public const int EraseScreen = 11010;

	/// <summary>11020, Show Screen.</summary>
	public const int ShowScreen = 11020;

	/// <summary>
	/// 11030, Tint Screen, from <c>Code::TintScreen</c> and EasyRPG
	/// <c>CommandTintScreen</c>, whose <c>CmdSetup</c> gives it a
	/// minimum width of 6.
	/// </summary>
	/// <summary>11310, Player Visibility, from <c>Code::PlayerVisibility</c>.</summary>
	public const int PlayerVisibility = 11310;

	/// <summary>
	/// 11330, Move Event, from <c>Code::MoveEvent</c> and EasyRPG's
	/// <c>CommandMoveEvent</c>, whose <c>CmdSetup</c> gives it a
	/// minimum width of 4.
	/// </summary>
	/// <summary>
	/// 10820, Memorize Location, from <c>Code::MemorizeLocation</c> and
	/// EasyRPG's <c>CommandMemorizeLocation</c>, whose <c>CmdSetup</c>
	/// gives it a minimum width of 3.
	/// </summary>
	/// <summary>
	/// 11840, 11930 and 11960, the three access commands: Change Escape
	/// Access, Change Save Access and Change Main Menu Access.
	/// </summary>
	/// <remarks>
	/// The reference has three one-line methods with the same shape, so one
	/// handler and three constants is the honest reading and not a saving.
	/// </remarks>
	/// <summary>10920, Store Event ID, from <c>CommandStoreEventID</c>, width 4.</summary>
	public const int StoreEventID = 10920;

	/// <summary>11810, Teleport Targets, from <c>CommandTeleportTargets</c>, width 6.</summary>
	public const int TeleportTargets = 11810;

	/// <summary>
	/// 12420 and 12510, Game Over and Return to Title Screen. Both take no
	/// parameters, which is why the reference can ignore the command
	/// entirely.
	/// </summary>
	public const int GameOver = 12420;
	public const int ReturnToTitleScreen = 12510;
	/// <summary>11710, Change Map Tileset, <c>CmdSetup</c> width 2.</summary>
	/// <summary>
	/// 20140 and 20141, the two halves of an RM2K3 choice branch list.
	/// </summary>
	/// <remarks>
	/// **Not a second choice window.** A game writes one 20140 per branch and
	/// ends the list with 20141, and the engine uses the indent to tell which
	/// branches belong together.
	/// </remarks>
	public const int ShowChoiceOption = 20140;
	public const int ChoiceEnd = 20141;
	public const int ChangeMapTileset = 11710;

	/// <summary>11720, Change PBG, <c>CmdSetup</c> width 8.</summary>
	public const int ChangePBG = 11720;

	/// <summary>11740, Change Encounter Steps, <c>CmdSetup</c> width 1.</summary>
	public const int ChangeEncounterSteps = 11740;

	/// <summary>11750, Tile Substitution, <c>CmdSetup</c> width 3.</summary>
	public const int TileSubstitution = 11750;

	/// <summary>10620, Change Hero Title, <c>CmdSetup</c> width 4.</summary>
	/// <summary>10500, Simulated Attack, <c>CmdSetup</c> width 8.</summary>
	public const int SimulatedAttack = 10500;

	/// <summary>
	/// 10710, Enemy Encounter, from liblcf's <c>Code::EnemyEncounter</c> and
	/// EasyRPG's <c>Game_Interpreter_Map::CommandEnemyEncounter</c>.
	/// </summary>
	/// <remarks>
	/// <strong>Six parameters, or ten, and the count depends on the mode.</strong>
	/// The reference's dispatch has two lines: one with a width of 6 for the
	/// plain form and one with 10 for the RPG2K3 form. **A reader that required
	/// ten would refuse every 2K game's encounter, and one that required six
	/// would refuse a 2003 game's background form.**
	/// </remarks>
	public const int EnemyEncounter = 10710;

	public const int ChangeHeroTitle = 10620;

	/// <summary>10630, Change Sprite Association, <c>CmdSetup</c> width 5.</summary>
	public const int ChangeSpriteAssociation = 10630;

	/// <summary>10640, Change Actor Face, <c>CmdSetup</c> width 4.</summary>
	public const int ChangeActorFace = 10640;

	/// <summary>10650, Change Vehicle Graphic, <c>CmdSetup</c> width 2.</summary>
	public const int ChangeVehicleGraphic = 10650;

	/// <summary>10850, Set Vehicle Location, <c>CmdSetup</c> width 5.</summary>
	public const int SetVehicleLocation = 10850;

	/// <summary>
	/// 11060, Pan Screen, from liblcf's <c>Code::PanScreen</c> and EasyRPG's
	/// <c>Game_Interpreter_Map::CommandPanScreen</c>.
	/// </summary>
	/// <remarks>
	/// <strong>Width 5, from the reference's own dispatch line</strong> and not
	/// the two the board once listed for it. A reader that required fewer would
	/// have read a wait flag that is not there.
	/// </remarks>

	/// <summary>
	/// 13110, Change Monster HP, from liblcf's <c>Code::ChangeMonsterHP</c> and
	/// EasyRPG's <c>CommandChangeMonsterHP</c>.
	/// </summary>
	/// <remarks>
	/// <strong>Width 5, and three change modes of which the third is a share.</strong>
	/// The reference's <c>CmdSetup&lt;..., 5&gt;</c> says five, the mode switch is
	/// 0 a constant, 1 a variable and 2 a percentage of the monster's own
	/// maximum, and the fifth is a lethal flag the reference hands to
	/// <c>ChangeHp</c>. <strong>A reader that read mode 2 as another constant
	/// would have healed a wounded boss for one hit point</strong> where the
	/// game asked for a tenth of his life.
	/// </remarks>
	/// <summary>
	/// 10720, Open Shop, from liblcf's <c>Code::OpenShop</c> and EasyRPG's
	/// <c>Game_Interpreter_Map::CommandOpenShop</c>.
	/// </summary>
	/// <remarks>
	/// <strong>Width 4, and the second parameter is a price and not a
	/// stock count.</strong> The reference's <c>CmdSetup&lt;..., 4&gt;</c> says
	/// four and its own <c>AddItem</c> takes the id and the price — <strong>so a
	/// reader that read the second parameter as "how many" would have shown
	/// one item where the shopkeeper keeps fifty</strong>, and a game's shop
	/// would have sold its wares once and then been empty.
	/// </remarks>
	public const int OpenShop = 10720;

	/// <summary>
	/// 10730, Show Inn, from liblcf's <c>Code::ShowInn</c> and EasyRPG's
	/// <c>Game_Interpreter_Map::CommandShowInn</c>.
	/// </summary>
	/// <remarks>
	/// <strong>Width 3, and the price is the third parameter and not the
	/// second.</strong> The three are the item, whether the price is shown, and
	/// the price — <strong>and a reader that read the price as the second
	/// parameter would have taken the "show it" flag for the amount</strong>,
	/// charging a party one gold for a night's rest, or nothing at all.
	/// </remarks>
	public const int ShowInn = 10730;

	/// <summary>
	/// 20710, Victory Handler, from liblcf's <c>Code::VictoryHandler</c> and
	/// EasyRPG's <c>CommandVictoryHandler</c>.
	/// </summary>
	/// <remarks>
	/// <strong>Width 0, and the whole command is one option.</strong> The
	/// reference returns <c>CommandOptionGeneric(com,
	/// eOptionEnemyEncounterVictory, {...})</c> and reads no parameter at all —
	/// a handler is a name for a block, not an instruction.
	/// </remarks>
	public const int VictoryHandler = 20710;

	/// <summary>20711, Escape Handler. Width 0, as its siblings.</summary>
	public const int EscapeHandler = 20711;

	/// <summary>20712, Defeat Handler. Width 0, as its siblings.</summary>
	public const int DefeatHandler = 20712;

	/// <summary>20713, End Battle. Width 0, as its siblings.</summary>
	public const int EndBattle = 20713;

	/// <summary>20720, Transaction. Width 0, as its siblings.</summary>
	public const int Transaction = 20720;

	/// <summary>20721, No Transaction. Width 0, as its siblings.</summary>
	public const int NoTransaction = 20721;

	/// <summary>20722, End Shop. Width 0, as its siblings.</summary>
	public const int EndShop = 20722;

	/// <summary>20730, Stay. Width 0, as its siblings.</summary>
	public const int Stay = 20730;

	/// <summary>20731, No Stay. Width 0, as its siblings.</summary>
	public const int NoStay = 20731;

	/// <summary>20732, End Inn. Width 0, as its siblings.</summary>
	public const int EndInn = 20732;

	/// <summary>
	/// 10440, Change Skills, from liblcf's <c>Code::ChangeSkills</c> and
	/// EasyRPG's <c>Game_Interpreter::CommandChangeSkills</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Width 5, and the flag is the third parameter and it means
	/// remove.</strong> The reference reads
	/// <c>bool remove = com.parameters[2] != 0</c> and then branches on it —
	/// <strong>so a truth in the third parameter unlearns</strong>, and a
	/// reader that read it as "add" would have taught a skill to a hero whose
	/// command meant to take it away.
	/// </para>
	/// <para>
	/// <strong>And the skill id is a value or a variable</strong>, read
	/// through the reference's own <c>ValueOrVariable(parameters[3],
	/// parameters[4])</c> — so a game whose skill came out of a variable
	/// teaches that skill and not the number the file happens to carry.
	/// </para>
	/// <para>
	/// <strong>And the command ends in <c>CheckGameOver()</c>.</strong>
	/// </para>
	/// </remarks>
	public const int ChangeSkills = 10440;

	/// <summary>
	/// 10450, Change Equipment, from liblcf's <c>Code::ChangeEquipment</c>
	/// and EasyRPG's <c>Game_Interpreter::CommandChangeEquipment</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Width 5, and the slot comes from the item's own type in the
	/// first mode and from the parameter in the second.</strong> The
	/// reference's switch on <c>parameters[2]</c> has two cases: 0 reads the
	/// item and takes <c>slot = item-&gt;type</c> across five types, and 1
	/// reads <c>slot = com.parameters[3] + 1</c> directly. <strong>A reader
	/// that took the slot from the parameter in both modes would have put a
	/// helmet where a sword goes.</strong>
	/// </para>
	/// <para>
	/// <strong>A third mode returns false and does nothing</strong> — it is
	/// the only one of the three that refuses rather than repairing.
	/// </para>
	/// </remarks>
	public const int ChangeEquipment = 10450;

	/// <summary>
	/// 10480, Change Condition, from liblcf's <c>Code::ChangeCondition</c>
	/// and EasyRPG's <c>Game_Interpreter::CommandChangeCondition</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Width 4, and the remove flag is the third parameter.</strong>
	/// The reference reads <c>bool remove = com.parameters[2] != 0</c> and the
	/// state id from the fourth.
	/// </para>
	/// <para>
	/// <strong>And the reference's own comment records two RPG_RT quirks
	/// it reproduces.</strong> On the map it removes a state even when the
	/// actor has it from equipment — <c>RemoveState(id, !IsBattleRunning())</c>
	/// — and it always adds a state from an event command, even a battle
	/// state. Both are written down in the source as RPG_RT behaviour, and
	/// both are cases where the "right" implementation is the wrong one.
	/// </para>
	/// </remarks>
	public const int ChangeCondition = 10480;

	public const int ChangeMonsterHp = 13110;

	/// <summary>
	/// 13120, Change Monster MP, from liblcf's <c>Code::ChangeMonsterMP</c> and
	/// EasyRPG's <c>CommandChangeMonsterMP</c>.
	/// </summary>
	/// <remarks>
	/// <strong>Width 5, and two change modes and not three.</strong> The
	/// reference's switch is 0 a constant and 1 a variable — <strong>and a third
	/// case is absent</strong>, so a mode of 2 changes nothing at all. The
	/// spirit points of a monster are read with the change's own switch and not
	/// clamped by the reader.
	/// </remarks>
	public const int ChangeMonsterMp = 13120;

	/// <summary>
	/// 13130, Change Monster Condition, from liblcf's
	/// <c>Code::ChangeMonsterCondition</c> and EasyRPG's
	/// <c>CommandChangeMonsterCondition</c>.
	/// </summary>
	/// <remarks>
	/// <strong>Width 3: enemy, remove-or-add, and the condition id.</strong>
	/// There is no fourth parameter and no percentage, because the condition
	/// is named and not scaled.
	/// </remarks>
	public const int ChangeMonsterCondition = 13130;

	/// <summary>
	/// 13150, Show Hidden Monster, from liblcf's
	/// <c>Code::ShowHiddenMonster</c> and EasyRPG's
	/// <c>CommandShowHiddenMonster</c>.
	/// </summary>
	/// <remarks>
	/// <strong>Width 1, and the whole command is one flag being cleared.</strong>
	/// The reference writes <c>enemy-&gt;SetHidden(false)</c> and returns, and
	/// <strong>there is no arm that hides a monster</strong> — a monster is
	/// hidden in its database row and this is the only command that shows it.
	/// </remarks>
	public const int ShowHiddenMonster = 13150;

	/// <summary>
	/// 13210, Change Battle BG, from liblcf's <c>Code::ChangeBattleBG</c> and
	/// EasyRPG's <c>CommandChangeBattleBG</c>.
	/// </summary>
	/// <remarks>
	/// <strong>Width 1, and the file name is in the command's text and not in
	/// its parameters.</strong> The reference writes
	/// <c>Game_Battle::ChangeBackground(ToString(com.string))</c>, and a reader
	/// that looked in <c>parameters</c> would have found a single zero and
	/// changed nothing.
	/// </remarks>
	/// <summary>
	/// 11210, Show Battle Animation, from liblcf's
	/// <c>Code::ShowBattleAnimation</c> and EasyRPG's
	/// <c>Game_Interpreter_Battle::CommandShowBattleAnimation</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Width 3, or 4, and the fourth exists only in a 2003
	/// game.</strong> The reference's dispatch has two lines for this one
	/// command — one with a width of 3 and the map form with 4 — and the
	/// fourth parameter is read only <c>if (Player::IsRPG2k3() &amp;&amp;
	/// com.parameters.size() &gt; 3)</c>. <strong>A reader that required four
	/// would have refused every 2K game, and one that read the fourth
	/// unconditionally would have shown a 2K game's "aim at the party" as
	/// "aim at the enemies".</strong>
	/// </para>
	/// <para>
	/// <strong>And a negative target is the whole side.</strong> The
	/// reference reads <c>target &lt; 0</c> and then collects either the party
	/// or the enemy party, and the flag says which — so a target of -1 with
	/// the flag at zero is every enemy, and a reader that treated -1 as
	/// "no target" would have played nothing.
	/// </para>
	/// </remarks>
	public const int ShowBattleAnimation = 11210;

	/// <summary>
	/// 13260, Show Battle Animation (the battle form), from liblcf's
	/// <c>Code::ShowBattleAnimation_B</c>.
	/// </summary>
	/// <remarks>
	/// <strong>Width 3, and the reference hands it to the very same
	/// method</strong> — <c>CmdSetup&lt;&amp;CommandShowBattleAnimation,
	/// 3&gt;</c>, with no second implementation. So the two codes differ in
	/// nothing but their number, and a reader that gave them different
	/// behaviour would have invented a difference the format does not have.
	/// </remarks>
	/// <summary>
	/// 13310, Conditional Branch (the battle form), from liblcf's
	/// <c>Code::ConditionalBranch_B</c> and EasyRPG's
	/// <c>Game_Interpreter_Battle::CommandConditionalBranchBattle</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Width 5, six modes, and two of them are 2003-only.</strong> The
	/// reference's switch has cases 0 to 5 — switch, variable, hero can act,
	/// monster can act, monster is the current target, and hero uses the
	/// command. <strong>The last two are guarded by
	/// <c>Player::IsRPG2k3Commands()</c> inside the case</strong>, so a 2K
	/// game's fourth and fifth modes are always false and a reader that
	/// evaluated them anyway would have taken a 2K game's branch with an
	/// enemy's number in a file that never carried one.
	/// </para>
	/// <para>
	/// <strong>And the switch comparison is a boolean equality, not an
	/// inversion.</strong> The reference writes
	/// <c>Get(parameters[1]) == (parameters[2] == 0)</c> — and <c>0 == 0</c> is
	/// <c>true</c>, <strong>so a third parameter of zero asks whether the
	/// switch is <em>on</em></strong>, and a third parameter of one asks
	/// whether it is off. A reader that read the parameter as a bare "is it
	/// off" would have had every switch in every game the wrong way round.
	/// </para>
	/// </remarks>
	public const int ConditionalBranchBattle = 13310;

	/// <summary>
	/// 13410, Terminate Battle, from liblcf's
	/// <c>Code::TerminateBattle</c> and EasyRPG's
	/// <c>Game_Interpreter_Battle::CommandTerminateBattle</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Width 0, and it is an abort and not a defeat.</strong> The
	/// reference's whole command is
	/// <c>AsyncOp::MakeTerminateBattle(static_cast&lt;int&gt;(BattleResult::Abort))</c>
	/// — <strong>a fourth outcome beside victory, escape and defeat</strong>,
	/// and no handler is named for it. A reader that wrote "defeat" here would
	/// have had a game that deliberately abandons a fight reach the game over
	/// screen.
	/// </para>
	/// <para>
	/// <strong>And it returns false</strong> — the frame stops and the result
	/// arrives later, so a reader that advanced to the next command would have
	/// run a game's victory rewards after it abandoned the fight.
	/// </para>
	/// </remarks>
	public const int TerminateBattle = 13410;

	/// <summary>
	/// 23310, Else Branch (the battle form), from liblcf's
	/// <c>Code::ElseBranch_B</c>.
	/// </summary>
	/// <remarks>
	/// <strong>The same option mechanism as every other else branch,</strong>
	/// and the reference's list is the end branch alone — so a battle branch
	/// with an else stops there and one without does not.
	/// </remarks>
	public const int ElseBranchBattle = 23310;

	/// <summary>
	/// 23311, End Branch (the battle form), from liblcf's
	/// <c>Code::EndBranch_B</c>.
	/// </summary>
	/// <remarks>
	/// <strong>Width 0 and the whole command is <c>return true;</c>**, which is
	/// the same as 22011 End Branch and for the same reason: a branch is a
	/// structure, and this only says the structure is over.
	/// </remarks>
	public const int EndBranchBattle = 23311;

	public const int ShowBattleAnimationBattle = 13260;

	public const int ChangeBattleBg = 13210;
	public const int PanScreen = 11060;

	/// <summary>
	/// 11340, Proceed With Movement, from liblcf's
	/// <c>Code::ProceedWithMovement</c> and EasyRPG's
	/// <c>CommandProceedWithMovement</c>.
	/// </summary>
	/// <remarks>
	/// <strong>Width 0, and the whole command is one flag.</strong> The
	/// reference writes <c>_state.wait_movement = true;</c> and nothing else —
	/// so a reader that expected parameters to read would be reading past the
	/// end of a list that is not there.
	/// </remarks>
	/// <summary>
	/// 11320, Flash Sprite, from liblcf's <c>Code::FlashSprite</c> and EasyRPG's
	/// <c>Game_Interpreter_Map::CommandFlashSprite</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Width 7, and the seventh parameter is a mode byte, not a
	/// value.</strong> The reference reads every channel through
	/// <c>ValueOrVariableBitfield(com, 7, shift, val_idx)</c> — <strong>and
	/// without the Maniac patch that helper returns
	/// <c>com.parameters[val_idx]</c> and nothing else.</strong> A reader that
	/// always applied the bitfield would have shifted a game's red channel by
	/// one for a colour the file wrote plainly.
	/// </para>
	/// <para>
	/// <strong>And the duration is in tenths, with a zero that waits one
	/// frame.</strong> The reference's own <c>SetupWait</c> writes
	/// <c>duration * DEFAULT_FPS / 10</c> and has a separate arm for zero that
	/// waits a single frame — so a game's "flash for no time" still holds its
	/// page for a frame.
	/// </para>
	/// </remarks>
	/// <summary>
	/// 10830, Recall to Location, from liblcf's <c>Code::RecallToLocation</c> and
	/// EasyRPG's <c>Game_Interpreter_Map::CommandRecallToLocation</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Width 3, and all three are variable ids and not values.</strong>
	/// The reference reads
	/// <c>Get(parameters[0])</c>, <c>Get(parameters[1])</c> and
	/// <c>Get(parameters[2])</c> and then teleports there. <strong>A reader that
	/// read them as coordinates would have sent a game to the map whose number
	/// the editor happened to write</strong> — and a 2K game's "recall" would
	/// have gone to map 1, tile 1, which is a real place on every map.
	/// </para>
	/// <para>
	/// <strong>And the facing is minus one, not the player's.</strong> The
	/// reference's <c>ReserveTeleport(map_id, x, y, -1, tt)</c> writes a
	/// facing of -1, which is its own "keep the direction the hero had" — and
	/// a reader that copied <c>10810</c>'s default of the current facing would
	/// have made a game's recall turn the hero.
	/// </para>
	/// </remarks>
	/// <summary>
	/// 10740, Enter Hero Name, from liblcf's <c>Code::EnterHeroName</c> and
	/// EasyRPG's <c>Game_Interpreter_Map::CommandEnterHeroName</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Width 3, and the third is a flag and not a text.</strong> The
	/// reference reads <c>actor_id</c>, <c>charset</c> and
	/// <c>use_default_name</c> and hands all three to its name scene.
	/// <strong>A reader that treated the third as part of a name would have
	/// shown every hero a name ending in a digit.</strong>
	/// </para>
	/// <para>
	/// <strong>And an actor that does not resolve is a warning and not a
	/// refusal</strong> — the reference's own
	/// <c>Output::Warning("EnterHeroName: Invalid actor ID {}")</c> and
	/// <c>return true</c>, which is the same shape <c>11320</c> and
	/// <c>11330</c> have.
	/// </para>
	/// </remarks>
	public const int EnterHeroName = 10740;

	/// <summary>
	/// 11560, Play Movie, from liblcf's <c>Code::PlayMovie</c> and EasyRPG's
	/// <c>Game_Interpreter_Map::CommandPlayMovie</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Width 5, and the file name is a string and not a
	/// parameter.</strong> The reference reads <c>ToString(com.string)</c> for
	/// the file and <c>parameters[0..4]</c> for everything else. <strong>A
	/// reader that looked for the name in the first parameter would have read
	/// the mode byte as a file name</strong> — and that byte is 0 or 1, so
	/// every movie would have been one file called "0".
	/// </para>
	/// <para>
	/// <strong>And the first parameter is the mode for both positions,</strong>
	/// exactly as in <c>10910</c>:
	/// <c>ValueOrVariable(com.parameters[0], com.parameters[1])</c> for x and
	/// the same mode with <c>parameters[2]</c> for y. The fourth and fifth are
	/// the width and the height, read plainly.
	/// </para>
	/// <para>
	/// <strong>And the command advances even though the reference cannot play
	/// the movie.</strong> Its own body says so: <c>Output::Warning("Couldn't
	/// play movie: {}. Movie playback is not implemented (yet).", filename)</c>
	/// and then <c>return true</c>. <strong>A reader that refused the command
	/// would have stalled a game's event</strong> on a cutscene it cannot show,
	/// and one that pretended to play it would have claimed a capability the
	/// reference does not have. This stores the request and says plainly that
	/// nothing is playing it.
	/// </para>
	/// </remarks>
	public const int PlayMovie = 11560;

	public const int RecallToLocation = 10830;

	/// <summary>
	/// 10870, Trade Event Locations, from liblcf's
	/// <c>Code::TradeEventLocations</c> and EasyRPG's
	/// <c>Game_Interpreter::CommandTradeEventLocations</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Width 2, and the two parameters are the two figures.</strong> The
	/// reference reads both through the same patch-guarded helper
	/// <c>11320</c> uses, and then <em>swaps</em> their three coordinates.
	/// <strong>A reader that moved one onto the other would have collapsed two
	/// events onto one tile</strong>, and a game whose "the guard takes your
	/// place" cutscene would have had both guards stand still.
	/// </para>
	/// <para>
	/// <strong>And a figure that does not resolve swaps nothing</strong> — the
	/// reference's <c>if (event1 != nullptr &amp;&amp; event2 != nullptr)</c>
	/// guards the whole exchange.
	/// </para>
	/// </remarks>
	public const int TradeEventLocations = 10870;

	/// <summary>
	/// 10910, Store Terrain ID, from liblcf's <c>Code::StoreTerrainID</c> and
	/// EasyRPG's <c>Game_Interpreter::CommandStoreTerrainID</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Width 4, and the first parameter is the mode for the first two
	/// coordinates and not for the third.</strong> The reference writes
	/// <c>ValueOrVariable(parameters[0], parameters[1])</c> for x and
	/// <c>ValueOrVariable(parameters[0], parameters[2])</c> for y — <strong>so
	/// the same mode byte governs both</strong> — and the fourth is the
	/// variable to write. A reader that gave each coordinate its own mode would
	/// have read a game's y from a constant while its x came from a variable.
	/// </para>
	/// <para>
	/// <strong>And the reference's own comment says <c>code 10820</c>.</strong>
	/// The dispatch line and liblcf both say <c>10910</c>, so the comment is
	/// stale and the number in the file is the one that counts — <strong>a
	/// reader that believed the comment would have implemented 10820</strong>,
	/// which is a different command with different parameters.
	/// </para>
	/// </remarks>
	public const int StoreTerrainId = 10910;

	public const int FlashSprite = 11320;

	public const int ProceedWithMovement = 11340;

	/// <summary>
	/// 11350, Halt All Movement, from liblcf's <c>Code::HaltAllMovement</c> and
	/// EasyRPG's <c>CommandHaltAllMovement</c>.
	/// </summary>
	/// <remarks>
	/// <strong>Width 0, and the whole command is one call.</strong> The
	/// reference writes <c>Game_Map::RemoveAllPendingMoves();</c> — every
	/// pending move on the map, and not the player's own.
	/// </remarks>
	public const int HaltAllMovement = 11350;

	/// <summary>
	/// 10490, Full Heal, from liblcf's <c>Code::FullHeal</c> and EasyRPG's
	/// <c>CommandFullHeal</c>.
	/// </summary>
	/// <remarks>
	/// <strong>Width 2, from the reference's <c>CmdSetup</c> line</strong> — the
	/// actor to heal and whether to include the skill points. A reader that
	/// required a third would refuse every game's heal.
	/// </remarks>
	public const int FullHeal = 10490;

	/// <summary>
	/// 10840, Get On/Off Vehicle, from liblcf's <c>Code::GetOnOffVehicle</c> and
	/// EasyRPG's <c>Game_Player::GetOnOffVehicle</c>.
	/// </summary>
	/// <remarks>
	/// <strong>Width 0, and that is the shape of the command.</strong> The
	/// vehicle is not a parameter — it is whatever is under the player or in
	/// front of it, in the order the reference checks them, and a reader that
	/// expected a parameter number would be reading a list that is not there.
	/// </remarks>
	public const int GetOnOffVehicle = 10840;

	/// <summary>10660, Change System BGM, <c>CmdSetup</c> width 7.</summary>
	public const int ChangeSystemBGM = 10660;

	/// <summary>10670, Change System SFX, <c>CmdSetup</c> width 6.</summary>
	public const int ChangeSystemSFX = 10670;

	/// <summary>10680, Change System Graphics, <c>CmdSetup</c> width 4.</summary>
	public const int ChangeSystemGraphics = 10680;

	/// <summary>10690, Change Screen Transitions, <c>CmdSetup</c> width 2.</summary>
	public const int ChangeScreenTransitions = 10690;

	/// <summary>11820 and 11830, Change Teleport Access and Escape Target.</summary>
	/// <remarks>
	/// <c>11820</c> is the fourth of the four one-line access commands and was
	/// missing from the board entirely; <c>11830</c> is the same shape as the
	/// teleport point, with the same fourth-parameter meaning.
	/// </remarks>
	public const int ChangeTeleportAccess = 11820;
	public const int EscapeTarget = 11830;

	/// <summary>11840, 11930 and 11960, the three other access commands.</summary>
	public const int ChangeEscapeAccess = 11840;
	public const int ChangeSaveAccess = 11930;
	public const int ChangeMainMenuAccess = 11960;
	public const int MemorizeLocation = 10820;

	public const int MoveEvent = 11330;

	public const int TintScreen = 11030;

	public const int FlashScreen = 11040;
	public const int ShakeScreen = 11050;
	public const int WeatherEffects = 11070;
	public const int EndEventProcessing = 12310;
	public const int CallEvent = 12330;
	public const int EraseEvent = 12320;
	public const int ChangeEventLocation = 10860;

	// CallEvent target kinds (EasyRPG CommandCallEvent).
	public const int CallTargetCommonEvent = 0;
	public const int CallTargetMapEvent = 1;
	public const int ShowMessage2 = 20110; // message continuation line

	/// <summary>
	/// 1009, from liblcf <c>Code::ChangeBattleCommands</c> and EasyRPG
	/// <c>CommandChangeBattleCommands</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>This was misread as a second message continuation line and the
	/// mistake was pushed.</strong> A card counted 20 occurrences of a bare
	/// <c>1009</c> with a string after a <c>10110</c>, compared them to MZ's
	/// <c>401</c> following a <c>101</c>, and concluded the two engines share a
	/// convention. They do not.
	/// </para>
	/// <para>
	/// The fixture settles it: <c>1009</c> carries <strong>four integers and an
	/// empty text</strong> — <c>[1,1,1,1]</c>, <c>[1,1,2,1]</c>,
	/// <c>[1,3,8,1]</c>, <c>[1,4,10,1]</c> — which are exactly
	/// <c>parameters[0..3]</c> of <c>CommandChangeBattleCommands</c>: actor,
	/// class, battle command id, and whether to add. <strong>A message line
	/// carries text and no integers, and these carry neither.</strong>
	/// </para>
	/// <para>
	/// <strong>Two engines having the same number for different things is not a
	/// coincidence worth acting on</strong> — it is the normal state of a
	/// twenty year old command table, and a pattern that fits two readings is
	/// not evidence for either.
	/// </para>
	/// </remarks>
	/// <summary>
	/// 11610, Key Input Proc, from liblcf <c>Code::KeyInputProc</c> and EasyRPG
	/// <c>Game_Interpreter::CommandKeyInputProc</c>.
	/// </summary>
	/// <remarks>
	/// It waits for a key and writes a <em>code</em> into a variable, not the
	/// key's own value — a digit answers 11 to 20, an operator 21 to 25, the
	/// confirm key is 5. It is not the number input command with a different
	/// name, and <c>Rm2kKeyInput</c> holds the whole table.
	/// </remarks>
	public const int KeyInputProc = 11610;

	/// <summary>
	/// 1008, Change Class, from liblcf's <c>Code::ChangeClass</c> and
	/// EasyRPG's <c>Game_Interpreter::CommandChangeClass</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Width 7, and the first two are the actor selection.</strong> The
	/// reference reads <c>class_id</c> from <c>parameters[2]</c>, the level
	/// flag from <c>[3]</c>, the skill mode from <c>[4]</c>, the parameter
	/// mode from <c>[5]</c> and the message flag from <c>[6]</c>, then walks
	/// <c>GetActors(parameters[0], parameters[1])</c>. <strong>And the whole
	/// body is 2003-only:</strong> the first line is
	/// <c>if (!Player::IsRPG2k3Commands()) return true;</c> — <strong>a reader
	/// that ran it in a 2K game would have changed a hero's class in a file
	/// format that has no such command.</strong>
	/// </para>
	/// <para>
	/// <strong>Class 0 is "no class" and is not an error.</strong> The
	/// reference reads it as a real value, checks <c>class_id != 0</c> before
	/// warning about an invalid class, and falls back to the actor's own
	/// database settings for the flags. A reader that refused zero would have
	/// refused the one class a 2K3 game can remove a hero from.
	/// </para>
	/// </remarks>
	public const int ChangeClass = 1008;

	public const int ChangeBattleCommands = 1009;

	/// <summary>5001, from liblcf <c>Code::OpenLoadMenu</c>.</summary>
	public const int OpenLoadMenu = 5001;

	/// <summary>5002, from liblcf <c>Code::ExitGame</c>.</summary>
	public const int ExitGame = 5002;

	/// <summary>5003, from liblcf <c>Code::ToggleAtbMode</c>.</summary>
	public const int ToggleAtbMode = 5003;

	/// <summary>5004, from liblcf <c>Code::ToggleFullscreen</c>.</summary>
	public const int ToggleFullscreen = 5004;

	/// <summary>5005, from liblcf <c>Code::OpenVideoOptions</c>.</summary>
	public const int OpenVideoOptions = 5005;

	/// <summary>
	/// The RPG2K3 E commands, which is the only engine version that runs any of
	/// the five menu commands. EasyRPG's
	/// <c>Player::IsRPG2k3ECommands</c> gate.
	/// </summary>
	/// <remarks>
	/// **All five return true — a silent no-op — on a game that is not
	/// RPG2K3 E commands**, and a silent no-op in an interpreter is the worst
	/// possible answer: the page carries on as though the game had asked for
	/// nothing, and a player who pressed the button to open the load menu
	/// watches the game do nothing at all.
	/// </remarks>
	public static bool IsMenuCommand(int pCode)
	{
		return pCode >= OpenLoadMenu && pCode <= OpenVideoOptions;
	}
	public const int ElseBranch = 22010;
	public const int EndBranch = 22011;
	public const int EndLoop = 22210;
	public const int Comment2 = 22410; // comment continuation line

	// ControlSwitches mode values (EasyRPG CommandControlSwitches).
	public const int SwitchModeOn = 0;
	public const int SwitchModeOff = 1;
	public const int SwitchModeFlip = 2;

	// ControlVars operation values (EasyRPG CommandControlVariables).
	public const int VarOpSet = 0;
	public const int VarOpAdd = 1;
	public const int VarOpSub = 2;
	public const int VarOpMul = 3;
	public const int VarOpDiv = 4;
	public const int VarOpMod = 5;

	// ControlVars operand types (subset implemented so far).
	public const int VarOperandConstant = 0;
	public const int VarOperandVariable = 1;

	// TargetEvalMode (EasyRPG Game_Interpreter_Shared): lvalue form stored in
	// ControlSwitches/ControlVariables parameters[0]. Indirect and expression
	// modes are patch-only and stay fail-closed.
	public const int TargetEvalSingle = 0;
	public const int TargetEvalRange = 1;
	public const int TargetEvalIndirectSingle = 2;
	public const int TargetEvalIndirectRange = 3;
	public const int TargetEvalExpression = 4;

	// ValueEvalMode (EasyRPG Game_Interpreter_Shared): rvalue form stored in
	// ControlVariables parameters[4].
	public const int VarOperandVariableIndirect = 2;

	// GetActors modes (EasyRPG Game_Interpreter::GetActors).
	public const int ActorSelectParty = 0;
	public const int ActorSelectHero = 1;
	public const int ActorSelectVariableHero = 2;

	// OperateValue operations used by ChangeExp/ChangeLevel.
	public const int ActorValueAdd = 0;
	public const int ActorValueSubtract = 1;

	// Screen effect subcommands (RPG2K3 extension of FlashScreen/ShakeScreen).
	public const int FlashSubOnce = 0;
	public const int FlashSubBegin = 1;
	public const int FlashSubEnd = 2;
	public const int ShakeSubOnce = 0;
	public const int ShakeSubBegin = 1;
	public const int ShakeSubEnd = 2;
	public const int EventInterpreterMaxTenths = MaxWaitFrames / 6;

	// ConditionalBranch condition types (EasyRPG CommandConditionalBranch).
	public const int ConditionSwitch = 0;
	public const int ConditionVariable = 1;

	// ConditionalBranch comparison operators (EasyRPG CheckOperator).
	public const int BranchOpEqual = 0;
	public const int BranchOpGreaterOrEqual = 1;
	public const int BranchOpLessOrEqual = 2;
	public const int BranchOpGreater = 3;
	public const int BranchOpLess = 4;
	public const int BranchOpNotEqual = 5;

	private readonly GameSimulationState _state;
	private readonly int _eventId;
	private IReadOnlyList<Rm2kMap.EventCommand> _commands;
	private readonly Stack<int> _loopStack = new();
	private readonly Stack<CallFrame> _callStack = new();
	private readonly Func<int, int, IReadOnlyList<Rm2kMap.EventCommand>?>? _eventCommandResolver;
	private readonly Func<int, int, int, int, bool>? _eventLocationSetter;
	private readonly Func<int, bool>? _eventDeactivator;

	/// <summary>
	/// The id of the event standing at a tile, for <c>10920</c>.
	/// </summary>
	/// <remarks>
	/// **A resolver and not a map reference, and for the same reason the other
	/// three are resolvers**: the interpreter must not know how a map is held,
	/// or a test could not drive it. 0 means nothing is there, which is what the
	/// reference stores when the tile is empty.
	/// </remarks>
	private readonly Func<int, int, int>? _eventIdAtTile;
	/// <summary>
	/// Starts a move route on a character, for command 11330.
	/// </summary>
	/// <remarks>
	/// The event id, the move frequency, the commands, whether the
	/// route repeats and whether the page is held by it. **A null
	/// means the character this interpreter cannot name**, which is
	/// not the same as a character that refused the route.
	/// </remarks>
	private readonly Func<int, int, IReadOnlyList<Rm2kMap.MoveCommand>,
		bool, bool, bool>? _moveRouteStarter;

	/// <summary>
	/// Flashes a character, for command 11320.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The character id, the four colour channels, the strength, the frames,
	/// and whether the page is held by it. <strong>A null means the character
	/// this interpreter cannot name</strong> — the same distinction
	/// <c>11330</c> makes, and the reason this is a hook and not a field: the
	/// interpreter names a figure, and whoever owns the figures decides
	/// whether that name resolves.
	/// </para>
	/// </remarks>
	private readonly Rm2kSpriteFlash? _spriteFlasher;

	/// <summary>
	/// Reads a figure's place, for command 10870.
	/// </summary>
	/// <param name="pEventId">The figure the command named.</param>
	/// <param name="pMapId">Its map.</param>
	/// <param name="pX">Its tile column.</param>
	/// <param name="pY">Its tile row.</param>
	/// <returns>Whether a figure carried that id.</returns>
	/// <remarks>
	/// <strong>Read and move are two delegates and not one.</strong> The
	/// reference's trade takes all six coordinates before it writes any, and a
	/// single "swap" hook would have let a half-finished swap stand — <strong>
	/// a reader that moved the first figure onto the second's place and then
	/// found the second unreachable would have collapsed two guards onto one
	/// tile.</strong>
	/// </remarks>
	public delegate bool Rm2kEventPlaceReader(
		int pEventId,
		out int pMapId,
		out int pX,
		out int pY);

	/// <summary>Moves a figure to a map and a tile, for command 10870.</summary>
	public delegate void Rm2kEventPlaceMover(int pEventId, int pMapId, int pX, int pY);

	/// <summary>
	/// Shows the name screen for a hero, for command 10740.
	/// </summary>
	/// <param name="pActorId">The hero the command named.</param>
	/// <param name="pCharsetIndex">The face index to show while the name is typed.</param>
	/// <param name="pUseDefaultName">Whether to offer the database's name.</param>
	/// <remarks>
	/// <strong>A hook and not a field, and the interpreter does not own the
	/// name.</strong> The reference builds a whole scene for this, and
	/// <strong>a reader that wrote the name straight into the state would have
	/// had a game's "name your hero" prompt rename the hero with no prompt
	/// at all</strong> — which is a different game, and a worse one.
	///
	/// <para>
	/// <strong>The face travels as an index and not as a file name.</strong>
	/// The reference's <c>Scene_Name(*actor, charset, use_default_name)</c>
	/// takes an <c>int</c>, because a face is an index into the actor's own
	/// face set — <strong>and a reader that read it as a file name would have
	/// looked for a charset file whose name happens to be a number.</strong>
	/// </para>
	/// </remarks>
	public delegate void Rm2kHeroNameEntry(
		int pActorId,
		int pCharsetIndex,
		bool pUseDefaultName);

	private readonly Rm2kHeroNameEntry? _heroNameEntry;

	private readonly Rm2kEventPlaceReader? _eventPlaceReader;

	private readonly Rm2kEventPlaceMover? _eventPlaceMover;

	/// <summary>
	/// Boards or leaves the vehicle under the player, for command 10840.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Whether it happened, and not whether it could.</strong> The
	/// reference's <c>GetOnOffVehicle</c> does nothing at all when there is
	/// nothing to board onto or nothing to step off onto — **and the
	/// difference between "did it" and "could it" is the whole of the command's
	/// observable behaviour.** A hook that returned the ability would have a
	/// game whose cutscene ran a "you cannot board here" line the reference
	/// never shows.
	/// </para>
	/// <para>
	/// <strong>The fifteen boarding rules stay on their own class.</strong>
	/// <c>Rm2kVehicleBoarding</c> already has them, tested, and the interpreter
	/// must not learn what a boat is — the same reason the route hook exists.
	/// </para>
	/// </remarks>
	private readonly Func<bool>? _vehicleBoardToggle;
	private readonly PresentationState? _presentation;

	/// <summary>
	/// The generator <c>10500</c> draws its variance from.
	/// </summary>
	/// <remarks>
	/// <strong>Its own and not MZ's</strong>, because the two engines do not
	/// share a stream: a game that uses both should not have one command's
	/// draws move the other's.
	/// </remarks>
	private readonly Rm2kDamageRandom _damageRandom = new();
	private int _commandIndex;

	/// <summary>
	/// The branch number of the option list being run, from the LCF indent.
	/// </summary>
	/// <remarks>
	/// <strong>This is the <c>20140</c>/<c>20141</c> state</strong> and it
	/// lives on the interpreter rather than in the simulation, because it
	/// belongs to one event and not to the game.
	/// </remarks>
	private int _subIndex;


	/// <summary>
	/// The branch number currently live, for a test that has to choose one.
	/// </summary>
	/// <remarks>
	/// <strong>Readable and settable on purpose.</strong> The number comes from
	/// the LCF indent, so a test cannot invent it any other way, and a reader
	/// that only ever cleared it could not be tested at all.
	/// </remarks>
	public int SubIndex
	{
		get => _subIndex;
		set => _subIndex = value;
	}

	/// <summary>
	/// The value that means "no branch is live any more".
	/// </summary>
	/// <remarks>
	/// <strong>A branch number and not a flag.</strong> The reference sets the
	/// sub command index to a sentinel value once a branch has run, and a
	/// reader that used a boolean would have to decide what to do with a
	/// second option list on the same page.
	/// </remarks>
	private const int SubCommandSentinel = -1;
	private int _waitFramesRemaining;

	/// <summary>Suspended caller state for a bounded nested CallEvent.</summary>
	private sealed class CallFrame
	{
		public CallFrame(IReadOnlyList<Rm2kMap.EventCommand> pCommands, int pReturnIndex, int pLoopDepth)
		{
			Commands = pCommands;
			ReturnIndex = pReturnIndex;
			LoopDepth = pLoopDepth;
		}

		public IReadOnlyList<Rm2kMap.EventCommand> Commands { get; }
		public int ReturnIndex { get; }
		public int LoopDepth { get; }
	}

	public EventInterpreter(GameSimulationState state, int eventId,
		IReadOnlyList<Rm2kMap.EventCommand> commands, PresentationState? presentation = null,
		Func<int, int, IReadOnlyList<Rm2kMap.EventCommand>?>? eventCommandResolver = null,
		Func<int, int, int, int, bool>? eventLocationSetter = null,
		Func<int, bool>? eventDeactivator = null,
		Func<int, int, int>? eventIdAtTile = null,
		Func<int, int, IReadOnlyList<Rm2kMap.MoveCommand>, bool, bool, bool>?
			moveRouteStarter = null,
		Func<bool>? vehicleBoardToggle = null,
		Rm2kSpriteFlash? spriteFlasher = null,
		Rm2kEventPlaceReader? eventPlaceReader = null,
		Rm2kEventPlaceMover? eventPlaceMover = null,
		Rm2kHeroNameEntry? heroNameEntry = null)
			{
		_state = state ?? throw new ArgumentNullException(nameof(state));
		_eventId = eventId;
		_commands = commands ?? throw new ArgumentNullException(nameof(commands));
		_presentation = presentation;
		_eventCommandResolver = eventCommandResolver;
		_eventLocationSetter = eventLocationSetter;
		_eventDeactivator = eventDeactivator;
		_eventIdAtTile = eventIdAtTile;
		_moveRouteStarter = moveRouteStarter;
		_vehicleBoardToggle = vehicleBoardToggle;
		_spriteFlasher = spriteFlasher;
		_eventPlaceReader = eventPlaceReader;
		_eventPlaceMover = eventPlaceMover;
		_heroNameEntry = heroNameEntry;
		_commandIndex = 0;
	}

	public GameSimulationState State => _state;

	public int EventId => _eventId;
	public int CurrentCommandIndex => _commandIndex;
	public int WaitFramesRemaining => _waitFramesRemaining;
	public int CallDepth => _callStack.Count;
	public bool IsRunning { get; private set; } = true;

	/// <summary>
	/// Execute one frame of this event's commands.
	/// Returns true if the event should continue running.
	/// Active waits consume frames before the next command executes.
	/// </summary>
	public bool ExecuteFrame()
	{
		if (!IsRunning)
		{
			return false;
		}

		if (_waitFramesRemaining > 0)
		{
			_waitFramesRemaining--;
			return true;
		}

		if (_commandIndex >= _commands.Count)
		{
			return FinishFrame();
		}

		var cmd = _commands[_commandIndex];

		switch (cmd.Code)
		{
			case End:
			case EndEventProcessing:
				// liblcf END terminates the current frame; a nested CallEvent
				// returns to its caller, the base frame stops the event.
				return FinishFrame();

			case MessageOptions:
				ExecuteMessageOptions(cmd);
				return Advance();

			case ChangeFaceGraphic:
				ExecuteChangeFaceGraphic(cmd);
				return Advance();

			case TimerOperation:
				ExecuteTimerOperation(cmd);
				return Advance();

			case ShowMessage:
			case Comment:
				ExecuteMessageOrComment(cmd);
				return Advance();

			case ShowChoice:
				if (!ExecuteShowChoice(cmd)) return true;
				return Advance();

			case InputNumber:
				if (!ExecuteInputNumber(cmd)) return true;
				return Advance();

			case ShowMessage2:
			case Comment2:
				// Continuation line without a preceding ShowMessage/Comment: skip.
				return Advance();

			case Wait:
				ExecuteWait(cmd);
				return Advance();

			case ControlSwitches:
				ExecuteControlSwitches(cmd);
				return Advance();

			case ChangeGold:
				ExecuteChangeGold(cmd);
				return Advance();

			case ChangeItems:
				ExecuteChangeItems(cmd);
				return Advance();

			case ChangePartyMembers:
				ExecuteChangePartyMembers(cmd);
				return Advance();

			case ControlVars:
				ExecuteControlVars(cmd);
				return Advance();

			case ChangeParameters:
				ExecuteChangeParameters(cmd);
				return Advance();

			case ChangeHP:
				ExecuteChangeHpOrSp(cmd, pIsHp: true);
				return Advance();

			case ChangeSP:
				ExecuteChangeHpOrSp(cmd, pIsHp: false);
				return Advance();

			case FullHeal:
				// **The sixth actor command, and the first one that needs no
				// value of its own** — it restores the current counts to the
				// bases rather than changing a base. The other five all take an
				// amount; this one takes nothing but a flag.
				ExecuteFullHeal(cmd);
				return Advance();

			case ChangeLevel:
				ExecuteChangeLevelOrExp(cmd, pIsLevel: true);
				return Advance();

			case ChangeExp:
				ExecuteChangeLevelOrExp(cmd, pIsLevel: false);
				return Advance();

			case ChangeHeroName:
				ExecuteChangeHeroName(cmd);
				return Advance();

			case ShowPicture:
				ExecuteShowPicture(cmd);
				return Advance();

			case ErasePicture:
				ExecuteErasePicture(cmd);
				return Advance();

			case MovePicture:
				// **The three picture commands are one family and only two of
				// them were reachable.** ShowPicture and ErasePicture ran; this
				// one was named in the constant list and had no case, so a game
				// that slid a title card across the screen had it jump to its
				// destination the moment the command ran -- or, before that,
				// fall into the default arm and be reported as an unsupported
				// command. A reader that looked at the constant list and not at
				// the dispatch would not have seen the gap.
				ExecuteMovePicture(cmd);
				return Advance();

			case StoreEventID:
				ExecuteStoreEventId(cmd);
				return Advance();

			case TeleportTargets:
				ExecuteTeleportTargets(cmd);
				return Advance();

			case GameOver:
				ExecuteGameOver();
				// **The reference returns false, so the page waits and the game over
				// screen is the only way past it.** Advancing would run the rest of the
				// event behind a screen the player is still looking at.
				// **A held page, and the way this dispatcher spells a wait:**
				// the index does not move, so the next frame runs this case
				// again until the screen is gone.
				return false;

			case ReturnToTitleScreen:
				ExecuteReturnToTitle();
				// **Also a wait, and also for a different reason**: the reference makes
				// this an async operation, so the page holds until the title is up.
				// **A held page, and the way this dispatcher spells a wait:**
				// the index does not move, so the next frame runs this case
				// again until the screen is gone.
				return false;

			case ShowChoiceOption:
				ExecuteShowChoiceOption(cmd);
				return Advance();

			case ChoiceEnd:
				ExecuteShowChoiceEnd();
				return Advance();

			case ChangeMapTileset:
				ExecuteChangeMapTileset(cmd);
				return Advance();

			case ChangePBG:
				ExecuteChangePbg(cmd);
				return Advance();

			case ChangeEncounterSteps:
				ExecuteChangeEncounterSteps(cmd);
				return Advance();

			case TileSubstitution:
				ExecuteTileSubstitution(cmd);
				return Advance();

			case ProceedWithMovement:
				// **The two movement commands, and both are one line in the
				// reference.** The board listed them as "liblcf names them and
				// EasyRPG dispatches them nowhere"; they are dispatched, with a
				// width of 0 and one statement each.
				_state.ProceedWithMovement = true;
				return Advance();

			case HaltAllMovement:
				// **Every pending move on the map, and not the player's own.**
				// The reference calls `Game_Map::RemoveAllPendingMoves()`,
				// which is a map-wide call and not a player one.
				_state.HaltAllMovement();
				return Advance();

			case OpenShop:
				ExecuteOpenShop(cmd);
				return Advance();

			case ShowInn:
				ExecuteShowInn(cmd);
				return Advance();

			case VictoryHandler:
				return ExecuteBattleHandler(
					GameSimulationState.BattleOutcome.Victory);

			case EscapeHandler:
				return ExecuteBattleHandler(
					GameSimulationState.BattleOutcome.Escape);

			case DefeatHandler:
				return ExecuteBattleHandler(
					GameSimulationState.BattleOutcome.Defeat);

			case EndBattle:
				return ExecuteEndBattle();

			case Transaction:
				return ExecuteShopHandler(GameSimulationState.ShopOption.Transaction);

			case NoTransaction:
				return ExecuteShopHandler(
					GameSimulationState.ShopOption.NoTransaction);

			case EndShop:
				return ExecuteEndShop();

			case Stay:
				return ExecuteInnHandler(GameSimulationState.ShopOption.Stay);

			case NoStay:
				return ExecuteInnHandler(GameSimulationState.ShopOption.NoStay);

			case EndInn:
				return ExecuteEndInn();

			case ChangeSkills:
				ExecuteChangeSkills(cmd);
				return Advance();

			case ChangeEquipment:
				ExecuteChangeEquipment(cmd);
				return Advance();

			case ChangeCondition:
				ExecuteChangeCondition(cmd);
				return Advance();

			case ChangeMonsterHp:
				ExecuteChangeMonsterHp(cmd);
				return Advance();

			case ChangeMonsterMp:
				ExecuteChangeMonsterMp(cmd);
				return Advance();

			case ChangeMonsterCondition:
				ExecuteChangeMonsterCondition(cmd);
				return Advance();

			case ShowHiddenMonster:
				ExecuteShowHiddenMonster(cmd);
				return Advance();

			case PlayMovie:
				ExecutePlayMovie(cmd);
				return Advance();

			case EnterHeroName:
				ExecuteEnterHeroName(cmd);
				return Advance();

			case TradeEventLocations:
				ExecuteTradeEventLocations(cmd);
				return Advance();

			case RecallToLocation:
				ExecuteRecallToLocation(cmd);
				return Advance();

			case StoreTerrainId:
				ExecuteStoreTerrainId(cmd);
				return Advance();

			case FlashSprite:
				return ExecuteFlashSprite(cmd);

			case TerminateBattle:
				// **Der vierte Ausgang, und der Befehl haelt den Frame
				// an** -- die Referenz gibt false zurueck, weil das Ergebnis
				// erst spaeter kommt. Ein Leser, der weitergewaert haette,
				// wuerde die Siegesbelohnung eines Spiels ausgefuehrt haben,
				// das den Kampf absichtlich abgebrochen hat.
				_state.Result = GameSimulationState.BattleResult.Abort;
				_state.AddDiagnostic(
					$"[Event {_eventId}] Terminate battle: the battle is "
					+ "aborted, which is a fourth result and not a defeat");
				return true;

			case ConditionalBranchBattle:
				return ExecuteConditionalBranchBattle(cmd);

			case ElseBranchBattle:
				// **Nur laufen, wenn der Sub-Index wirklich auf diesen
				// Zweig zeigt** -- die Referenz ruft auch hier
				// CommandOptionGeneric, und ein Leser, der den Index
				// gesetzt haette, ohne ihn hier zu pruefen, wuerde den
				// else-Zweig auch dann laufen lassen, wenn die Bedingung
				// wahr war und der Zweig gar nicht zu diesem Block gehoert.
				if (_state.IsSubcommandChosen(SubIdxBranchBattleElse))
				{
					_state.AddDiagnostic(
						$"[Event {_eventId}] Battle else branch: this is the "
						+ "chosen option, so its block runs");
					return Advance();
				}

				var uebersprungen2 = SkipToOneOf(
					new[] { EndBranchBattle }, "battle else branch");
				_state.AddDiagnostic(
					$"[Event {_eventId}] Battle else branch: not the chosen "
					+ $"option, so skipped {uebersprungen2} commands");
				return Advance();

			case EndBranchBattle:
				_state.AddDiagnostic(
					$"[Event {_eventId}] End battle branch: the branch is over");
				return Advance();

			case ShowBattleAnimation:
			case ShowBattleAnimationBattle:
				// **The reference hands both codes to one method**, and this
				// reader does the same — the two differ in their number and in
				// nothing else.
				ExecuteShowBattleAnimation(cmd);
				return Advance();

			case ChangeBattleBg:
				ExecuteChangeBattleBg(cmd);
				return Advance();

			case PanScreen:
				ExecutePanScreen(cmd);
				// **Through Advance(), exactly like the Wait case above** — the
				// wait is not taken from the return value but from the frame
				// budget `ExecuteFrame` checks first, so a pan that was asked
				// to wait holds its page on the next call and not on this one.
				return Advance();

			case SimulatedAttack:
				ExecuteSimulatedAttack(cmd);
				return Advance();

			case EnemyEncounter:
				// **The battle commands were five fields in the state and no
				// command that set them.** `IsBattleActive`, `ActiveTroopId`,
				// `BattleTurn`, `BattlePhase` and `TroopMembers` existed and
				// nothing reached them — so a game's encounter command fell
				// into the default arm and no battle ever started, while the
				// state carried a battle phase of its own.
				ExecuteEnemyEncounter(cmd);
				// **And the page holds**, because the reference makes the
				// battle an asynchronous operation: the arms after this
				// command run when the battle ends, and not before.
				return _state.WaitingFor != GameSimulationState.WaitReason.None;

			case ChangeHeroTitle:
				ExecuteChangeHeroTitle(cmd);
				return Advance();

			case ChangeSpriteAssociation:
				ExecuteChangeSpriteAssociation(cmd);
				return Advance();

			case ChangeActorFace:
				ExecuteChangeActorFace(cmd);
				return Advance();

			case ChangeVehicleGraphic:
				ExecuteChangeVehicleGraphic(cmd);
				return Advance();

			case SetVehicleLocation:
				ExecuteSetVehicleLocation(cmd);
				return Advance();

			case GetOnOffVehicle:
				// **The third vehicle command and the only one with no
				// parameters.** `10650` sets a graphic, `10850` sets a
				// position, and this one reads neither: the vehicle is whatever
				// is under the player or in front of it, in the order the
				// reference checks them.
				ExecuteGetOnOffVehicle();
				return Advance();

			case ChangeSystemBGM:
				ExecuteChangeSystemBgm(cmd);
				return Advance();

			case ChangeSystemSFX:
				ExecuteChangeSystemSfx(cmd);
				return Advance();

			case ChangeSystemGraphics:
				ExecuteChangeSystemGraphics(cmd);
				return Advance();

			case ChangeScreenTransitions:
				ExecuteChangeScreenTransitions(cmd);
				return Advance();

			case ChangeTeleportAccess:
				ExecuteAccessChange(cmd, pWhich: AccessFlag.Teleport);
				return Advance();

			case EscapeTarget:
				ExecuteEscapeTarget(cmd);
				return Advance();

			case ChangeEscapeAccess:
				ExecuteAccessChange(cmd, pWhich: AccessFlag.Escape);
				return Advance();

			case ChangeSaveAccess:
				ExecuteAccessChange(cmd, pWhich: AccessFlag.Save);
				return Advance();

			case ChangeMainMenuAccess:
				ExecuteAccessChange(cmd, pWhich: AccessFlag.Menu);
				return Advance();

			case OpenSaveMenu:
				// **The menu openers are not access changes.** `11930` says
				// whether the player *may* save and takes a parameter; `11910`
				// opens the menu and takes none. A reader that treated the
				// second as the first would have a game whose save menu opened
				// every time a cutscene unlocked saving.
				ExecuteOpenMenu(cmd, pIsSave: true);
				// **A held page, and this dispatcher's spelling of a wait is
				// `true` with the index left alone** -- the same as the game
				// over screen, so the next frame runs this case again until
				// the menu is gone.
				return _state.WaitingFor != GameSimulationState.WaitReason.None;

			case OpenMainMenu:
				ExecuteOpenMenu(cmd, pIsSave: false);
				return _state.WaitingFor != GameSimulationState.WaitReason.None;

			case MemorizeLocation:
				ExecuteMemorizeLocation(cmd);
				return Advance();

			case PlayerVisibility:
				ExecutePlayerVisibility(cmd);
				return Advance();

			case MoveEvent:
				ExecuteMoveEvent(cmd);
				return Advance();

			case TintScreen:
				// **The wait is the same conditional the reference uses**, and the
				// index moves only when the command did not set one. A first
				// draft returned a bare true here, so a tint that asked to wait
				// still advanced the page and the wait never happened.
				var waitBefore = _waitFramesRemaining;
				ExecuteTintScreen(cmd);
				return _waitFramesRemaining == waitBefore ? Advance() : IsRunning;

			case EraseScreen:
			case ShowScreen:
				ExecuteScreenTransition(cmd);
				return true;

			case FlashScreen:
				ExecuteFlashScreen(cmd);
				return Advance();

			case ShakeScreen:
				ExecuteShakeScreen(cmd);
				return Advance();

			case WeatherEffects:
				ExecuteWeatherEffects(cmd);
				return Advance();

			case CallEvent:
				return ExecuteCallEvent(cmd);

			case ChangeEventLocation:
				ExecuteChangeEventLocation(cmd);
				return Advance();

			case EraseEvent:
				return ExecuteEraseEvent(cmd);

			case Teleport:
				ExecuteTeleport(cmd);
				return Advance();

			case ConditionalBranch:
				ExecuteConditionalBranch();
				return Advance();

			case ElseBranch:
				ExecuteElseBranch();
				return Advance();

			case EndBranch:
				ExecuteEndBranch();
				return Advance();

			case PlayBGM:
			case PlaySound:
				ExecutePlayTrack(cmd);
				return Advance();

			case FadeOutBGM:
				ExecuteFadeOutBGM(cmd);
				return Advance();

			case MemorizeBGM:
				_state.AddDiagnostic(
					_state.Audio.MemorizeBgm()
						? $"[Event {_eventId}] Memorized the current BGM"
						: $"[Event {_eventId}] Memorize BGM: there was none, and"
							+ " the reference copies the pointer and leaves it empty");
				return Advance();

			case PlayMemorizedBGM:
				_state.AddDiagnostic(
					_state.Audio.PlayMemorizedBgm()
						? $"[Event {_eventId}] Played the memorised BGM"
						: $"[Event {_eventId}] Play memorised BGM: nothing was"
							+ " memorised");
				return Advance();

			case Label:
				// `case Cmd::Label: return true;` in the reference. A label is a
				// name, not an instruction, and giving it an effect would invent
				// a semantic the format does not have.
				return Advance();

			case JumpToLabel:
			{
				// **The engine's own rule, in full.** EasyRPG increments the
				// index only when the command left it alone:
				//
				//     if (index_before_exec == frame->current_command) {
				//         frame->current_command++;
				//     }
				//
				// A jump that finds its label moves the index, so it does not
				// advance — the page lands *on* the label, and the label is a
				// no-op that costs a frame of its own. A jump that finds
				// nothing leaves the index where it was, so the rule increments
				// it and the page carries on.
				//
				// **Two drafts got this wrong in opposite directions and both
				// were silent**: one returned a bare true and left the page on
				// the jump forever, which looks like a hang; the other advanced
				// unconditionally and skipped the no-op the format puts there
				// on purpose. Only the engine's conditional form is both.
				var before = _commandIndex;
				ExecuteJumpToLabel(cmd);
				return _commandIndex == before ? Advance() : IsRunning;
			}

			case Loop:
				_loopStack.Push(_commandIndex);
				return Advance();

			case BreakLoop:
				ExecuteBreakLoop();
				return true; // index already moved past the matching EndLoop

			case EndLoop:
				ExecuteEndLoop();
				return true; // index points at the matching Loop command

			case KeyInputProc:
				ExecuteKeyInputProc(cmd);
				return true;

			case ChangeClass:
				ExecuteChangeClass(cmd);
				return Advance();

			case ChangeBattleCommands:
				ExecuteChangeBattleCommands(cmd);
				return Advance();

			case OpenLoadMenu:
			case ExitGame:
			case ToggleAtbMode:
			case ToggleFullscreen:
			case OpenVideoOptions:
				return ExecuteMenuCommand(cmd);

			default:
				_state.AddDiagnostic($"[Event {_eventId}] Unsupported RM2K command {cmd.Code} skipped");
				return Advance();
		}
	}

	/// <summary>
	/// The five RPG2K3 menu commands, and the rule that decides whether they do
	/// anything at all.
	/// </summary>
	/// <remarks>
	/// <para>
	/// **The version gate is the whole command.** EasyRPG's
	/// <c>Player::IsRPG2k3ECommands()</c> guards all five, and every one of them
	/// <c>return true</c> — a silent no-op — on a game that is not RPG2K3 E
	/// commands. That is the worst answer an interpreter can give, because a
	/// player who pressed a button to open the load menu watches the game do
	/// nothing, and nothing in the log says why.
	/// </para>
	/// <para>
	/// <strong>So this reader does not copy the no-op.</strong> It refuses
	/// visibly: a diagnostic names the command and says the game is not an
	/// E-command game. <em>A command that silently does nothing is a bug that
	/// survives every test, because nothing changed.</em>
	/// </para>
	/// <para>
	/// <strong>On an E-command game the menu opens and the interpreter waits
	/// for it</strong>, which is the second half of the rule: EasyRPG returns
	/// <c>false</c> from <c>CommandOpenVideoOptions</c> after pushing a scene,
	/// so the page does not advance until the scene closes. A reader that
	/// advanced immediately would run the rest of the page behind a menu nobody
	/// had opened yet.
	/// </para>
	/// </remarks>
	private bool ExecuteMenuCommand(Rm2kMap.EventCommand pCommand)
	{
		if (!_state.SupportsRpg2k3ECommands)
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] {DescribeMenuCommand(pCommand.Code)} is an"
				+ " RPG2K3 E command and this game does not declare them;"
				+ " skipped, and nothing opened");
			return Advance();
		}

		switch (pCommand.Code)
		{
			case OpenLoadMenu:
				return PushScene("Load");
			case OpenVideoOptions:
				return PushScene("Settings");
			case ExitGame:
				_state.ExitRequested = true;
				return Advance();
			case ToggleAtbMode:
				_state.AtbWaitMode = !_state.AtbWaitMode;
				return Advance();
			case ToggleFullscreen:
				_state.FullscreenRequested = !_state.FullscreenRequested;
				return Advance();
			default:
				_state.AddDiagnostic(
					$"[Event {_eventId}] {DescribeMenuCommand(pCommand.Code)} is"
					+ " not one of the five and was refused rather than guessed at");
				return Advance();
		}
	}

	/// <summary>
	/// Pushes a scene and holds the page, from the source's <c>return false</c>.
	/// </summary>
	private bool PushScene(string pScene)
	{
		if (_state.CurrentScene != pScene)
		{
			_state.SceneStack.Add(pScene);
			_state.CurrentScene = pScene;
		}
		// The page does not advance. It advances when the scene is popped, which
		// is the caller's next decision and not this command's.
		return true;
	}

	/// <summary>
	/// The liblcf names of the five, for a diagnostic a reader can act on.
	/// </summary>
	private static string DescribeMenuCommand(int pCode)
	{
		return pCode switch
		{
			OpenLoadMenu => "Open Load Menu (5001)",
			ExitGame => "Exit Game (5002)",
			ToggleAtbMode => "Toggle ATB Mode (5003)",
			ToggleFullscreen => "Toggle Fullscreen (5004)",
			OpenVideoOptions => "Open Video Options (5005)",
			_ => $"Command {pCode}",
		};
	}

	private bool Advance()
	{
		_commandIndex++;
		return IsRunning;
	}

	/// <summary>
	/// Completes the current command frame. A nested CallEvent resumes its
	/// caller; the base frame stops the interpreter.
	/// </summary>
	private bool FinishFrame()
	{
		if (_callStack.Count == 0)
		{
			IsRunning = false;
			return false;
		}
		var frame = _callStack.Pop();
		_commands = frame.Commands;
		_commandIndex = frame.ReturnIndex;
		TrimLoopStack(frame.LoopDepth);
		return IsRunning;
	}

	private void TrimLoopStack(int pDepth)
	{
		while (_loopStack.Count > pDepth)
		{
			_loopStack.Pop();
		}
	}

	/// <summary>
	/// EasyRPG CommandCallEvent pushes a nested frame. Map events are resolved
	/// through the injected resolver; common events stay diagnostic-only because
	/// the LDB common-event section is not decoded yet.
	/// </summary>
	private bool ExecuteCallEvent(Rm2kMap.EventCommand pCmd)
	{
		// Verified layout: [targetKind, eventId, pageIndex] with minimum width 3.
		if (pCmd.Parameters.Count < 3)
		{
			Malformed("Call event");
			return Advance();
		}
		var targetKind = pCmd.Parameters[0];
		if (targetKind != CallTargetMapEvent)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Call event: target kind {targetKind} is not supported yet");
			return Advance();
		}
		var calledEventId = pCmd.Parameters[1];
		var pageIndex = pCmd.Parameters[2];
		if (calledEventId < 1 || calledEventId > GameSimulationState.MaxActorId)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Call event: invalid event id {calledEventId} skipped");
			return Advance();
		}
		if (pageIndex < 0 || pageIndex > PresentationState.MaxPictures * 1000)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Call event: invalid page index {pageIndex} skipped");
			return Advance();
		}
		if (_eventCommandResolver == null)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Call event: no event command resolver available");
			return Advance();
		}
		if (_callStack.Count >= MaxScriptRecursion)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Call event: recursion limit {MaxScriptRecursion} reached");
			return Advance();
		}
		var called = _eventCommandResolver(calledEventId, pageIndex);
		if (called == null || called.Count == 0)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Call event: event {calledEventId} page {pageIndex} has no commands");
			return Advance();
		}

		_callStack.Push(new CallFrame(_commands, _commandIndex + 1, _loopStack.Count));
		_commands = called;
		_commandIndex = 0;
		_state.AddDiagnostic($"[Event {_eventId}] Call event: running event {calledEventId} page {pageIndex}");
		return true;
	}

	private bool ExecuteShowChoice(Rm2kMap.EventCommand pCmd)
	{
		if (_presentation == null)
		{
			Malformed("Show choice: presentation state unavailable");
			return true;
		}
		if (_presentation.ActiveChoice == null)
		{
			var options = pCmd.Text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
			if (!_presentation.ShowChoices(options))
			{
				Malformed("Show choice: invalid options");
				return true;
			}
			return false;
		}
		if (_presentation.ActiveChoice.SelectedIndex < 0)
		{
			return false;
		}
		_state.AddDiagnostic($"[Event {_eventId}] Choice selected: {_presentation.ActiveChoice.SelectedIndex}");
		_presentation.ClearChoice();
		return true;
	}

	private bool ExecuteInputNumber(Rm2kMap.EventCommand pCmd)
	{
		if (_presentation == null || pCmd.Parameters.Count < 1 || pCmd.Parameters[0] < 1 || pCmd.Parameters[0] > GameSimulationState.MaxVariables)
		{
			Malformed("Input number");
			return true;
		}
		var variableId = pCmd.Parameters[0];
		if (_presentation.PendingInputVariableId != null && _presentation.PendingInputVariableId != variableId)
		{
			_state.AddDiagnostic($"[Event {_eventId}] InputNumber for variable {variableId} paused: pending input for different variable {_presentation.PendingInputVariableId}");
			return false;
		}
		if (_presentation.PendingInputVariableId == null && !_presentation.BeginInput(variableId))
		{
			Malformed("Input number");
			return true;
		}
		if (!_presentation.TryConsumeInput(out _, out var value))
		{
			return false;
		}
		while (_state.Variables.Count < variableId) _state.Variables.Add(0);
		_state.Variables[variableId - 1] = value;
		_state.AddDiagnostic($"[Event {_eventId}] Input number -> variable {variableId}");
		return true;
	}

	/// <summary>
	/// 10120, Message Options, from EasyRPG's
	/// <c>CommandMessageOptions</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Four independent flags and not one "style".</strong> The
	/// reference sets four fields on the game system and a reader that
	/// collapsed them would make a transparent bottom message and a
	/// top-positioned one the same request.
	/// </para>
	/// <para>
	/// <strong>Parameters[2] is inverted</strong> — <c>== 0</c> means the
	/// window holds its position while the map scrolls. A reader that
	/// mapped a non-zero to fixed would scroll every window a game had
	/// pinned, and the difference is visible only while the map moves.
	/// </para>
	/// <remarks>
	/// <strong>Parameters[1] has three values and not two</strong>: top,
	/// middle, bottom. A reader that stored a boolean would put the
	/// middle where the top belongs.
	/// </para>
	/// </remarks>
	private void ExecuteMessageOptions(Rm2kMap.EventCommand pCmd)
	{
		if (_presentation == null)
		{
			Malformed("Message options");
			return;
		}
		// CmdSetup minimum width 4.
		if (pCmd.Parameters.Count < 4)
		{
			Malformed("Message options");
			return;
		}
		if (!_presentation.SetMessageOptions(
			Param(pCmd, 0) != 0,
			Param(pCmd, 1),
		// **Inverted, and the comment is the whole reason.** The reference
		// writes SetMessagePositionFixed(com.parameters[2] == 0), so a
		// zero is the fixed case and not the moving one.
		Param(pCmd, 2) == 0,
			Param(pCmd, 3) != 0))
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Message options refused: position"
				+ $" {Param(pCmd, 1)} is not 0, 1 or 2, and nothing was changed");
			return;
		}
		_state.AddDiagnostic(
			$"[Event {_eventId}] Message options: transparent is"
			+ $" {Param(pCmd, 0) != 0}, position {Param(pCmd, 1)},"
			+ $" fixed is {Param(pCmd, 2) == 0},"
			+ $" continuing events is {Param(pCmd, 3) != 0}");
	}

	/// <summary>
	/// 10130, Change Face Graphic, from EasyRPG's <c>CommandChangeFaceGraphic</c>.
	/// </summary>
	/// <remarks>
	/// The name is in the command string field and the index in
	/// <c>parameters[0]</c>, and <c>parameters[1]</c> and <c>parameters[2]</c>
	/// are the right-side and flipped flags. This is <strong>a request and
	/// not a drawn portrait</strong> — nothing loads a file, and a face that
	/// is set with no renderer behind it is what this reader can say.
	/// </remarks>
	private void ExecuteChangeFaceGraphic(Rm2kMap.EventCommand pCmd)
	{
		if (_presentation == null)
		{
			Malformed("Change face graphic");
			return;
		}
		// CmdSetup minimum width 3.
		if (pCmd.Parameters.Count < 3)
		{
			Malformed("Change face graphic");
			return;
		}
		if (string.IsNullOrEmpty(pCmd.Text))
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Change face graphic: no file name, and the"
				+ " string field is where the name lives; nothing was set");
			return;
		}
		if (!_presentation.SetFace(
			pCmd.Text, Param(pCmd, 0), Param(pCmd, 1) != 0, Param(pCmd, 2) != 0))
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Change face graphic refused: the index"
				+ " is outside the file four slots, or the name was empty or too long");
			return;
		}
		_state.AddDiagnostic(
			$"[Event {_eventId}] Face set to slot {Param(pCmd, 0)},"
			+ $" on the right is {Param(pCmd, 1) != 0},"
			+ $" flipped is {Param(pCmd, 2) != 0}");
	}

	/// <summary>
	/// 10230, Timer Operation, from EasyRPG's <c>CommandTimerOperation</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>One command, three operations, chosen by
	/// <c>parameters[0]</c></strong>: 0 sets the seconds, 1 starts the timer
	/// with the visible and battle flags, 2 stops it. Anything else is
	/// <c>return false</c> in the reference, which holds the page.
	/// </para>
	/// <para>
	/// <strong>The seconds can come from a variable</strong> — this is
	/// <c>ValueOrVariable(parameters[1], parameters[2])</c> and not a plain
	/// read, so a game can set a timer from a counter it already keeps.
	/// </para>
	/// <para>
	/// <strong>A sixth parameter names the timer</strong>, and the reference
	/// reads it <em>only</em> when the command carries more than five and
	/// the game is RPG2K3. Without it, every timer command means timer one —
	/// which is why a 2K game has only one timer and a 2003 game has two.
	/// </para>
	/// </remarks>
	private void ExecuteTimerOperation(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 5.
		if (pCmd.Parameters.Count < 5)
		{
			Malformed("Timer operation");
			return;
		}
		// **The sixth parameter names the timer, and only when the command
		// carries it and the game is RPG2K3.** A 2K game has one timer and a
		// 2003 game has two, and that is the whole difference.
		var timerId = pCmd.Parameters.Count > 5 && _state.SupportsRpg2k3ECommands
			? pCmd.Parameters[5] : 1;
		switch (pCmd.Parameters[0])
		{
			case 0:
			{
				var seconds = ValueOrVariable(Param(pCmd, 1), Param(pCmd, 2));
				if (!_state.SetTimer(timerId, seconds))
				{
					_state.AddDiagnostic(
						$"[Event {_eventId}] Timer {timerId}: {seconds} seconds is"
						+ " outside 0 to 86400, and nothing was set");
					return;
				}
				_state.AddDiagnostic(
					$"[Event {_eventId}] Timer {timerId} set to {seconds} seconds,"
					+ " and **not started**, because the reference has a separate"
					+ " start operation and a reader that started on set would start a"
					+ " countdown the game meant to arm");
				return;
			}
			case 1:
			{
				if (!_state.StartTimer(
					timerId, Param(pCmd, 3) != 0, Param(pCmd, 4) != 0))
				{
					_state.AddDiagnostic(
						$"[Event {_eventId}] Timer {timerId} cannot be started, because"
						+ " a game has two and that is not one of them");
					return;
				}
				_state.AddDiagnostic(
					$"[Event {_eventId}] Timer {timerId} started, visible is"
					+ $" {Param(pCmd, 3) != 0}, in battle is {Param(pCmd, 4) != 0}");
				return;
			}
			case 2:
			{
				if (!_state.StopTimer(timerId))
				{
					_state.AddDiagnostic(
						$"[Event {_eventId}] Timer {timerId} cannot be stopped, because"
						+ " a game has two and that is not one of them");
					return;
				}
				_state.AddDiagnostic(
					$"[Event {_eventId}] Timer {timerId} stopped, and its seconds"
					+ " are kept, because the reference does not reset them and a game"
					+ " that shows a count and restarts it expects it to still be there");
				return;
			}
			default:
				_state.AddDiagnostic(
					$"[Event {_eventId}] Timer operation {pCmd.Parameters[0]} is not"
					+ " set, start or stop, and the reference holds the page for it");
				return;
		}
	}

	private void ExecuteMessageOrComment(Rm2kMap.EventCommand pCmd)
	{
		var kind = pCmd.Code == ShowMessage ? "Show message" : "Comment";
		var text = pCmd.Text;
		// Consume continuation lines (ShowMessage_2 / Comment_2).
		while (_commandIndex + 1 < _commands.Count
			&& (_commands[_commandIndex + 1].Code == (pCmd.Code == ShowMessage ? ShowMessage2 : Comment2)))
			{
			_commandIndex++;
			text += "\n" + _commands[_commandIndex].Text;
		}
		if (pCmd.Code == ShowMessage && _presentation != null)
		{
			if (!_presentation.ShowMessage(text))
			{
				_state.AddDiagnostic($"[Event {_eventId}] Presentation rejected message: exceeds bounds");
			}
		}
		_state.AddDiagnostic($"[Event {_eventId}] {kind}: {Truncate(text)}");
	}

	private static string Truncate(string pText)
	{
		var singleLine = pText.Replace("\n", "\\n");
		return singleLine.Length <= 80 ? singleLine : singleLine[..80];
	}

	private void ExecuteWait(Rm2kMap.EventCommand pCmd)
	{
		// params[0] is a duration in tenths of a second (EasyRPG SetupWait);
		// 0.0 seconds still waits exactly one frame.
		var tenths = Param(pCmd, 0);
		WaitForFrames(tenths == 0 ? 1 : TenthsToFrames(tenths));
		_state.AddDiagnostic($"[Event {_eventId}] Wait {_waitFramesRemaining} frames");
	}

	/// <summary>EasyRPG converts tenths of a second at 60 simulation frames per second.</summary>
	private static int TenthsToFrames(int pTenths) => Math.Min(checked(pTenths * 6), MaxWaitFrames);

	private void WaitForFrames(int pFrames)
	{
		_waitFramesRemaining = Math.Clamp(pFrames, 1, MaxWaitFrames);
	}

	/// <summary>
	/// 1009, Change Battle Commands, from EasyRPG's
	/// <c>CommandChangeBattleCommands</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Parameters: <c>[actorMode, actorId, commandId, add]</c>, which is what
	/// <c>CmdSetup&lt;..., 4&gt;</c> says — a minimum width of four. The
	/// reference reads <c>parameters[0..1]</c> through <c>GetActors</c>,
	/// <c>parameters[2]</c> as the command id and <c>parameters[3] != 0</c> as
	/// "add".
	/// </para>
	/// <para>
	/// <strong>Three actor modes, and they are not the same set of people.</strong>
	/// Mode 0 is the party, 1 is one hero by id, 2 is the hero named by a
	/// variable. An invalid hero id is <em>a warning and an empty list</em>, not
	/// an error — the command runs and touches nobody, and a reader that
	/// refused the whole page would drop the rest of an event because one hero
	/// id was wrong.
	/// </para>
	/// </remarks>
	/// <summary>
	/// 11610, Key Input Proc, from EasyRPG's
	/// <c>CommandKeyInputProc</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The reference resets the key state and returns <c>false</c>, so the page
	/// holds until a key arrives. <strong>And while waiting it sets the variable
	/// to zero every frame</strong> — the reference's own comment says so, and
	/// a reader that only wrote on arrival would leave whatever the game had put
	/// there a moment ago.
	/// </para>
	/// <para>
	/// <strong>The engine version is a parameter of the read, not an
	/// assumption.</strong> Parameters 5 to 9 mean shift/down/left/right/up on
	/// RM2K and numbers/operators/time-variable/timed on RM2K3, and reading the
	/// wrong column produces a command that waits for the wrong keys rather than
	/// an error.
	/// </para>
	/// </remarks>
	private void ExecuteKeyInputProc(Rm2kMap.EventCommand pCmd)
	{
		if (_presentation == null)
		{
			Malformed("Key input proc");
			return;
		}
		// The version is read from the save data, and a game that has not said
		// which it is is treated as 2K, which is the reading the command's own
		// legacy branch expects.
		var request = Rm2kKeyInput.Read(
			pCmd.Parameters,
			pIsRpg2k3: _state.SupportsRpg2k3ECommands,
			pIsMajorUpdated: _state.SupportsRpg2k3ECommands);
		if (request == null)
		{
			Malformed("Key input proc");
			return;
		}
		if (_presentation.PendingKeyInputVariableId == request.VariableId
			&& _presentation.PendingKeyInputKeys.Count > 0)
			{
			// Still open, and no key has arrived. The variable is zeroed every
			// frame the reference waits, so a game reading it sees 0 rather
			// than whatever it last held.
			if (request.Wait && request.VariableId <= GameSimulationState.MaxVariables)
			{
				WriteVariable(request.VariableId, 0);
			}
			return;
		}
		if (!_presentation.BeginKeyInput(request))
		{
			Malformed("Key input proc");
			return;
		}
		if (request.Wait && request.VariableId <= GameSimulationState.MaxVariables)
		{
			WriteVariable(request.VariableId, 0);
		}
		_state.AddDiagnostic(
			$"[Event {_eventId}] Key input proc: waiting for one of"
			+ $" {request.AllowedKeys.Count} keys -> variable {request.VariableId}");
	}

	/// <summary>
	/// Delivers a key press to an open 11610 prompt and writes the answer.
	/// </summary>
	/// <remarks>
	/// This is the frame-step side of the command, and it is separate from the
	/// dispatch on purpose: <c>ExecuteFrame</c> is one interpreter step, and a
	/// key arrives on an input frame, which is not the same thing. A reader that
	/// put the wait inside the dispatch would re-arm the prompt on every frame
	/// it stayed open, and the reference resets its key state on every call.
	/// </remarks>
	public void PressKeys(IReadOnlyList<string> pPressed)
	{
		if (_presentation == null)
		{
			return;
		}
		if (!_presentation.TryConsumeKeyInput(pPressed, out var value))
		{
			return;
		}
		if (!_presentation.TryConsumeKeyInput(
			out var variableId, out _, out _, out _))
			{
			return;
		}
		WriteVariable(variableId, value);
		_state.AddDiagnostic(
			$"[Event {_eventId}] Key input proc: value {value} -> variable {variableId}");
	}

	private void WriteVariable(int pVariableId, int pValue)
	{
		while (_state.Variables.Count < pVariableId)
		{
			_state.Variables.Add(0);
		}
		_state.Variables[pVariableId - 1] = pValue;
	}

	/// <summary>
	/// 12120, Jump to Label, from EasyRPG's <c>CommandJumpToLabel</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The search is <strong>from the beginning of the page, not from here</strong>
	/// — <c>for (int idx = 0; idx &lt; list.size(); idx++)</c> — so a jump can
	/// go backwards, and a backward jump is how an author writes a loop
	/// without a loop command. A reader that searched forwards from the current
	/// index would turn every backward jump into a fall-through to the end of
	/// the page.
	/// </para>
	/// <para>
	/// <strong>The index lands on the label, not after it.</strong>
	/// <c>index = idx</c> and the label itself does nothing, so the next step
	/// runs the label and then the instruction after it. Pointing past the label
	/// would work the same way — which is why this looks like a detail and is
	/// not: <em>the loop the search runs is what makes a missing label
	/// distinguishable from a label that is simply the next command.</em>
	/// </para>
	/// <para>
	/// <strong>A label that is not there leaves the page where it was</strong>,
	/// because the loop finds nothing and the assignment never runs. The
	/// reference is silent about it, and a reader that reported a failure would
	/// turn a page that merely falls through into a page that stops with an
	/// error — so this one says so and carries on.
	/// </para>
	/// </remarks>
	/// <summary>
	/// 11510 and 11550, from EasyRPG's <c>CommandPlayBGM</c> and
	/// <c>CommandPlaySound</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The parameters are <c>[fadeIn, volume, tempo, balance]</c> and the
	/// file name is in the command's string field, and <strong>the first
	/// parameter is the fade, not the name</strong> — the name arrives through
	/// the Maniac branch of <c>CommandStringOrVariableBitfield</c>, which reads
	/// the string field first and only falls back to a variable when the patch
	/// is active.
	/// </para>
	/// <para>
	/// <strong>Balance is 0 to 100 with 50 in the middle</strong>, and not
	/// -100 to 100: the editor writes 0 to 100 and the engine stores it as
	/// written, so a reader that treated the middle as 0 would call every
	/// centred track hard left.
	/// </para>
	/// <para>
	/// <strong>This is data and not sound.</strong> Nothing here has a player
	/// behind it, and a reader that reported a track as playing would be
	/// claiming a sound nobody can hear. The diagnostic says what was asked for
	/// and not that it was heard.
	/// </para>
	/// </remarks>
	private void ExecutePlayTrack(Rm2kMap.EventCommand pCmd)
	{
		var istBgm = pCmd.Code == PlayBGM;
		var label = istBgm ? "Play BGM" : "Play sound";
		// **CmdSetup gives the music a width of 4 and the effect a width of 3**,
		// and a first draft wrote 5 and 4. The counts are not the same as the
		// number of values: the music's four are fade, volume, tempo and
		// balance, and the effect's three are volume, tempo and balance. The
		// mode slot only appears when the Maniac patch is active, and the
		// function's own `assert(mode_idx != val_idx)` would not tolerate a
		// command that always carried it.
		var minWidth = istBgm ? 4 : 3;
		if (pCmd.Parameters.Count < minWidth)
		{
			Malformed(label);
			return;
		}
		if (string.IsNullOrEmpty(pCmd.Text))
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] {label}: no file name, and the string"
				+ " field is where the name lives; nothing was played");
			return;
		}
		var channel = istBgm
			? Rm2kChannel.BackgroundMusic
			: Rm2kChannel.SoundEffect;
		//
		// **The four numbers are `parameters[0..3]` and nothing else.** The
		// reference reads them through
		// `ValueOrVariableBitfield(com, 4, 0..3, 1..4)` and that function's
		// first line is `if (!IsPatchManiac()) return com.parameters[val_idx]`
		// — so *without the Maniac patch each value is simply its own
		// parameter*, and the fifth parameter exists only to hold four
		// two-bit mode fields when the patch is active.
		//
		// **A first draft read `parameters[1]` as the mode for the other three
		// and then refused every command whose values were non-zero** — which is
		// every music command a real game writes. Worse, the refusal was
		// dressed as caution: "this reader does not decode a bitfield yet" reads
		// as care, and it was actually a wrong reading of the source that had
		// not been read to the end. **Refusing loudly is not a substitute for
		// knowing.**
		//
		// A game that *does* carry the patch is refused here, and the reason
		// names the patch rather than the format — because the patch's values
		// are the same numbers read through four two-bit mode fields, and
		// reading them as plain numbers would produce a plausible volume that
		// is not the volume.
		if (_state.SupportsManiacPatch)
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] {label}: this game carries the Maniac patch,"
				+ " which packs the four values into mode fields this reader does"
				+ " not decode; nothing was played");
			return;
		}
		//
		// **The fade is parameters[0] on the music and there is none on the
		// effect**, so the two commands' lists do not line up: the music is
		// [fade, volume, tempo, balance] and the effect is [volume, tempo,
		// balance]. A first draft read both from the same offsets, which put
		// the effect's volume where its balance belongs.
		var fadeInTenths = istBgm ? Param(pCmd, 0) : 0;
		var volume = Param(pCmd, istBgm ? 1 : 0);
		var tempo = Param(pCmd, istBgm ? 2 : 1);
		var balance = Param(pCmd, istBgm ? 3 : 2);
		var cancelledFade = _state.Audio.Play(
			channel, pCmd.Text, volume, tempo, balance, fadeInTenths);
			// `Play` returns whether it *cancelled a fade*, so the track is read
			// back rather than assumed. **Reading the channel back is what tells a
			// refusal from a success** — the name is unique per play, so a channel
			// that does not hold this name is a channel nothing was written to.
			var track = istBgm ? _state.Audio.Bgm : _state.Audio.SoundEffect;
			if (track == null
				|| !string.Equals(track.Name, pCmd.Text, StringComparison.Ordinal))
				{
				_state.AddDiagnostic(
					$"[Event {_eventId}] {label}: refused \"{Truncate(pCmd.Text)}\" because"
					+ " one of the numbers was out of range");
				return;
			}
			_state.AddDiagnostic(
			$"[Event {_eventId}] {label}: {track}"
			+ (cancelledFade
				? ", and it cancelled a fade that was still running"
				: string.Empty));
	}

	/// <summary>
	/// 11520, Fade Out Background Music, from EasyRPG's
	/// <c>CommandFadeOutBGM</c>.
	/// </summary>
	/// <remarks>
	/// One parameter, the fade in tenths of a second, and it is passed straight
	/// to the audio system. <strong>Fading a channel that is silent is a
	/// no-op, not an error and not an empty track</strong> — the reference hands
	/// the number over and the audio system has nothing to fade, and a reader
	/// that invented a track to fade would be claiming a sound that was never
	/// asked for.
	/// </remarks>
	private void ExecuteFadeOutBGM(Rm2kMap.EventCommand pCmd)
	{
		if (pCmd.Parameters.Count < 1)
		{
			Malformed("Fade out BGM");
			return;
		}
		var tenths = ValueOrVariable(EventInterpreter.VarOperandConstant, pCmd.Parameters[0]);
		if (!_state.Audio.FadeOut(Rm2kChannel.BackgroundMusic, tenths))
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Fade out BGM: nothing to fade, and the"
				+ " reference hands the number to a channel that has no track; the"
				+ $" fade of {tenths} was not started");
			return;
		}
		_state.AddDiagnostic(
			$"[Event {_eventId}] Fade out BGM over {tenths} tenths");
	}

	private void ExecuteJumpToLabel(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 1.
		if (pCmd.Parameters.Count < 1)
		{
			Malformed("Jump to label");
			return;
		}
		var labelId = pCmd.Parameters[0];
		var found = -1;
		// **From the start of the page, not from here.** A backward jump is a
		// loop, and the reference's own loop begins at zero.
		for (var idx = 0; idx < _commands.Count; idx++)
		{
			if (_commands[idx].Code != Label)
			{
				continue;
			}
			if (_commands[idx].Parameters.Count == 0
				|| _commands[idx].Parameters[0] != labelId)
				{
				continue;
			}
			found = idx;
			break;
		}
		if (found < 0)
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Jump to label {labelId}: no such label, and"
				+ " the reference leaves the page where it was");
			return;
		}
		// On the label itself, and **not** one past it. The engine's own rule
		// is `if (index_before_exec == frame->current_command) { ++; }`, so a
		// command that moved the index is not incremented again — and the label
		// is a no-op that costs a frame of its own before the instruction
		// behind it.
		//
		// A first draft pointed past the label and got a green suite, because
		// a no-op that is skipped and a no-op that runs differ only in a frame
		// nobody counts.
		_commandIndex = found;
		_state.AddDiagnostic(
			$"[Event {_eventId}] Jump to label {labelId} -> index {found}");
	}

	/// <summary>
	/// Runs 1008, Change Class, from liblcf's <c>Code::ChangeClass</c> and
	/// EasyRPG's <c>Game_Interpreter::CommandChangeClass</c> and
	/// <c>Game_Actor::ChangeClass</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Seven parameters and a 2003 gate.</strong> The reference's
	/// first line is <c>if (!Player::IsRPG2k3Commands()) return true;</c> —
	/// <strong>a 2K game carries no such command</strong>, and a reader that
	/// ran it anyway would have changed a hero's class in a file that has no
	/// field for it.
	/// </para>
	/// <para>
	/// <strong>And it removes the whole equipment first, always.</strong> The
	/// reference's own comment says it: <c>// RPG_RT always removes all
	/// equipment on level change.</c> <strong>A reader that kept the
	/// equipment would have left a hero wearing the previous class's armour
	/// with the new class's statistics</strong> — and RPG_RT does not allow
	/// that, so a game that checks a hero's equipment afterwards would branch
	/// on a state the original never produced.
	/// </para>
	/// <para>
	/// <strong>And it resets the experience even when the level did not
	/// change</strong> — <c>// RPG_RT always resets EXP when class is changed,
	/// even if level unchanged.</c> <strong>A reader that only reset the
	/// experience when the level moved would have left a hero carrying the
	/// progress of a class they no longer have.</strong>
	/// </para>
	/// <para>
	/// <strong>Class 0 is "no class", not an error.</strong> The reference
	/// guards its warning with <c>class_id != 0</c> and takes the actor's own
	/// database settings for the flags when there is no class.
	/// </para>
	/// </remarks>
	private void ExecuteChangeClass(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 7.
		if (pCmd.Parameters.Count < 7)
		{
			Malformed("Change class");
			return;
		}

		// **Die ganze Sache ist 2003-only**, und das ist die erste Zeile der
		// Referenz -- nicht eine Warnung, sondern ein Rueckkehr mit nichts
		// getan.
		if (!_state.SupportsRpg2k3ECommands)
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Change class: a 2K game has no class"
				+ " command, and the reference's own guard returns before"
				+ " anything changes");
			return;
		}

		var actors = ResolveActors(
			pCmd.Parameters[0], pCmd.Parameters[1], "Change class");
		if (actors == null)
		{
			return;
		}

		var classId = pCmd.Parameters[2];
		var level1 = pCmd.Parameters[3] > 0;
		var skillMode = pCmd.Parameters[4];
		var paramMode = pCmd.Parameters[5];

		if (classId < 0 || classId > GameSimulationState.MaxClassId)
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Change class: class {classId} is outside"
				+ " 0 to 5000, and the reference warns and touches nobody");
			return;
		}

		foreach (var actorId in actors)
		{
			_state.ChangeActorClass(actorId, classId, level1, skillMode, paramMode);
			_state.AddDiagnostic(
				$"[Event {_eventId}] Change class: actor {actorId} is now in"
				+ $" class {classId}"
				+ (level1 ? " at level 1" : " at the level it had")
				+ $", skill mode {skillMode}, parameter mode {paramMode}"
				+ ", and the whole equipment went with it");
		}
	}

	private void ExecuteChangeBattleCommands(Rm2kMap.EventCommand pCmd)
	{
		// EasyRPG: CmdSetup minimum width 4.
		if (pCmd.Parameters.Count < 4)
		{
			Malformed("Change battle commands");
			return;
		}
		var actors = ResolveActors(
			pCmd.Parameters[0], pCmd.Parameters[1], "Change battle commands");
		if (actors == null)
		{
			return;
		}
		var commandId = pCmd.Parameters[2];
		var add = pCmd.Parameters[3] != 0;
		foreach (var actorId in actors)
		{
			if (!_state.ChangeActorBattleCommand(actorId, commandId, add))
			{
				// **Nothing changed, and this reader says so.** The reference is
				// silent here, but a command that was asked to do something and
				// did not is exactly the case a diagnostic exists for — and the
				// two directions mean opposite things: "add what it already
				// has" is a game author's habit, "remove what it does not have"
				// is usually a mistake worth naming.
				_state.AddDiagnostic(
					$"[Event {_eventId}] Change battle commands: actor {actorId}"
					+ $" already {(add ? "has" : "lacks")} command {commandId},"
					+ $" so nothing changed");
				continue;
			}
			_state.AddDiagnostic(
				$"[Event {_eventId}] Change battle commands: actor {actorId}"
				+ $" {(add ? "gained" : "lost")} command {commandId}");
		}
	}

	/// <summary>
	/// Runs 10490, Full Heal, from liblcf's <c>Code::FullHeal</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Width 2, and the second parameter is the skill points and not a
	/// third actor.</strong> The reference's <c>CmdSetup</c> gives the command
	/// two parameters and nothing more, and a reader that expected six like the
	/// HP and SP commands would refuse every heal a game wrote.
	/// </para>
	/// <para>
	/// <strong>It restores the current counts to the bases, and does not touch
	/// a base.</strong> Every other actor command in this family changes a base
	/// value or a current count; this one puts the counts back to what the bases
	/// say. **A reader that healed by adding the base would double a hero's
	/// hit points** — and a game that heals after a battle would leave the hero
	/// stronger every time.
	/// </para>
	/// <para>
	/// <strong>Two parameters, and both of them are the actor selection.</strong>
	/// The first is the mode — party, one hero, or a hero named by a variable —
	/// and the second is the actor number. <strong>There is no third
	/// parameter and no skill-point flag</strong>, and I wrote one before
	/// reading the layout: a reader that took the second parameter as "also
	/// heal SP" would heal the skill points of every actor a game healed, and
	/// a game that heals only hit points between fights would have a party
	/// that never runs out of magic.
	/// </para>
	/// </remarks>
	private void ExecuteFullHeal(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 2.
		if (pCmd.Parameters.Count < 2)
		{
			Malformed("Full heal");
			return;
		}
		var actors = ResolveActors(pCmd.Parameters[0], pCmd.Parameters[1], "Full heal");
		if (actors == null)
		{
			return;
		}
		// **There is no skill-point flag, and I wrote one before reading the
		// reference's parameter layout.** `CommandFullHeal`'s two parameters
		// are the selection mode and the actor number — the same first two the
		// HP and SP commands have — so a reader that took the second as "also
		// heal the skill points" would have healed the skill points of every
		// actor a game healed, and a game that heals only hit points between
		// fights would have a party that never runs out of magic.
		//
		// **The skill points go with it, always.** The command is called Full
		// Heal and the reference's body sets both counts; a game that wants
		// only the hit points has 10460 for that.
		const bool withSp = true;
		foreach (var actorId in actors)
		{
			var values = _state.GetOrCreateActorValues(actorId);
			// **Assignment and not addition.** The current count becomes the
			// base; a reader that added the base would heal a hero to twice
			// their maximum, and a game that heals between every fight would
			// have a party that grew without bound.
			_state.CurrentHp[actorId] = values.BaseMaxHp;
			if (withSp)
			{
				_state.CurrentSp[actorId] = values.BaseMaxSp;
			}
			_state.AddDiagnostic(
				$"[Event {_eventId}] Full heal: actor {actorId} is at"
				+ $" {values.BaseMaxHp} hit points"
				+ (withSp ? $" and {values.BaseMaxSp} skill points" : ", and the"
					+ " skill points were not asked for"));
		}
	}

	/// <summary>
	/// 10430, Change Parameters, from EasyRPG's
	/// <c>CommandChangeParameters</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>It changes the <em>base</em>, not the current value</strong> —
	/// <c>SetBaseMaxHp</c> and <c>SetMaxHp</c> are different calls in the
	/// reference and this command is the first. <em>That is the whole
	/// difference</em>: the base survives a level change and a save, a buff
	/// does not, and a reader that wrote the current value would let a
	/// saved game keep a buff that ended three maps ago.
	/// </para>
	/// <para>
	/// The value goes through <c>OperateValue</c>, so the operation is in
	/// <c>parameters[2]</c>, the operand mode in <c>parameters[4]</c> and
	/// the operand itself in <c>parameters[5]</c>.
	/// </para>
	/// </remarks>
	private void ExecuteChangeParameters(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 6.
		if (pCmd.Parameters.Count < 6)
		{
			Malformed("Change parameters");
			return;
		}
		var actors = ResolveActors(pCmd.Parameters[0], pCmd.Parameters[1], "Change parameters");
		if (actors == null)
		{
			return;
		}
		var operation = pCmd.Parameters[2];
		if (operation != VarOpSet && operation != VarOpAdd && operation != VarOpSub)
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Change parameters: operation {operation}"
				+ " is not set, add or subtract, and the reference treats it as add");
			operation = VarOpAdd;
		}
		int operand;
		switch (pCmd.Parameters[4])
		{
			case VarOperandConstant:
				operand = pCmd.Parameters[5];
				break;
			case VarOperandVariable:
				operand = GetVariable(pCmd.Parameters[5]);
				break;
			case VarOperandVariableIndirect:
				if (pCmd.Parameters[5] < 1 || pCmd.Parameters[5] > GameSimulationState.MaxVariables)
			{
					_state.AddDiagnostic(
						$"[Event {_eventId}] Change parameters: invalid indirect variable"
						+ $" {pCmd.Parameters[5]} skipped");
					return;
			}
				operand = GetVariable(GetVariable(pCmd.Parameters[5]));
				break;
			default:
				_state.AddDiagnostic(
					$"[Event {_eventId}] Change parameters: operand mode"
					+ $" {pCmd.Parameters[4]} is not one of the three, and the constant"
					+ $" {pCmd.Parameters[5]} was used");
				operand = pCmd.Parameters[5];
				break;
		}
		// **OperateValue negates for the subtract operation**, and that is the
		// reference own helper and not a reader side switch.
		if (operation == VarOpSub)
		{
			operand = -operand;
		}
		var parameter = pCmd.Parameters[3];
		foreach (var actorId in actors)
		{
			var values = _state.GetOrCreateActorValues(actorId);
			var vorher = values.GetParameter(parameter);
			if (!values.AddToParameter(parameter, operand))
			{
				_state.AddDiagnostic(
					$"[Event {_eventId}] Change parameters: {parameter} is not one"
					+ " of the six, and nothing was changed");
				continue;
			}
			_state.AddDiagnostic(
				$"[Event {_eventId}] Change parameters: actor {actorId}"
				+ $" parameter {parameter} {vorher} -> {values.GetParameter(parameter)}"
				+ (vorher + operand != values.GetParameter(parameter)
					? ", and the difference is a clamp"
					: string.Empty));
		}
	}

	/// <summary>
	/// 10460 and 10470, Change HP and Change SP, from
	/// EasyRPG's <c>CommandChangeHP</c> and <c>CommandChangeSP</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Parameters[2] is a "remove" flag and not an operation</strong>,
	/// and the reference negates the amount when it is set. A reader that
	/// read it as a sign would subtract when the game meant to add.
	/// </para>
	/// <para>
	/// <strong>HP and SP clamp differently and that is not an accident.</strong>
	/// HP has a lethal flag and a floor of one when it is not set, because
	/// a game can protect a hero from a hit. <c>SP</c> has neither: the
	/// reference writes <c>if (sp &lt; 0) sp = 0;</c> and nothing else, and a
	/// reader that gave SP the same floor as HP would leave a hero unable to
	/// cast anything.
	/// </para>
	/// <para>
	/// <strong>The ceiling is the current maximum, not the base</strong>, and
	/// that is the reason the base lives in its own place: a hero with a base
	/// of 40 and equipment worth 10 cannot be healed past 50.
	/// </para>
	/// </remarks>
	private void ExecuteChangeHpOrSp(Rm2kMap.EventCommand pCmd, bool pIsHp)
	{
		var label = pIsHp ? "Change HP" : "Change SP";
		// **HP needs six parameters and SP five**: the sixth is the lethal flag
		// and SP has none, so the two commands do not line up.
		var minWidth = pIsHp ? 6 : 5;
		if (pCmd.Parameters.Count < minWidth)
		{
			Malformed(label);
			return;
		}
		var actors = ResolveActors(pCmd.Parameters[0], pCmd.Parameters[1], label);
		if (actors == null)
		{
			return;
		}
		// **A remove flag and not a sign.** The reference writes
		// `bool remove = com.parameters[2] != 0; if (remove) amount = -amount;`.
		var remove = pCmd.Parameters[2] != 0;
		var amount = ValueOrVariable(Param(pCmd, 3), Param(pCmd, 4));
		if (remove)
		{
			amount = -amount;
		}
		var lethal = pIsHp && pCmd.Parameters[5] != 0;
		foreach (var actorId in actors)
		{
			if (pIsHp)
			{
				var vorher = _state.GetActorCurrentHp(actorId);
				var nachher = Rm2kActorValues.ChangeHp(
					vorher, amount, _state.GetOrCreateActorValues(actorId).BaseMaxHp, lethal);
				_state.CurrentHp[actorId] = nachher;
				_state.AddDiagnostic(
					$"[Event {_eventId}] {label}: actor {actorId}"
					+ $" {vorher} -> {nachher} by {amount},"
					+ (nachher == 1 && !lethal && vorher + amount < 1
						? ", and it stopped at one because the change was not lethal"
						: string.Empty));
			}
			else
			{
				var vorher = _state.GetActorCurrentSp(actorId);
				var nachher = Rm2kActorValues.ChangeSp(
					vorher, amount, _state.GetOrCreateActorValues(actorId).BaseMaxSp);
				_state.CurrentSp[actorId] = nachher;
				_state.AddDiagnostic(
					$"[Event {_eventId}] {label}: actor {actorId}"
					+ $" {vorher} -> {nachher} by {amount}");
			}
		}
	}

	private void ExecuteChangeLevelOrExp(Rm2kMap.EventCommand pCmd, bool pIsLevel)
	{
		var label = pIsLevel ? "Change level" : "Change exp";
		// EasyRPG: [actorMode, actorId, operation, operandMode, operand, showMessage]
		// with CmdSetup minimum width 6.
		if (pCmd.Parameters.Count < 6)
		{
			Malformed(label);
			return;
		}
		var actors = ResolveActors(pCmd.Parameters[0], pCmd.Parameters[1], label);
		if (actors == null)
		{
			return;
		}
		var operation = pCmd.Parameters[2];
		if (operation != ActorValueAdd && operation != ActorValueSubtract)
		{
			_state.AddDiagnostic($"[Event {_eventId}] {label}: unsupported operation {operation} skipped");
			return;
		}
		int operand;
		switch (pCmd.Parameters[3])
		{
			case VarOperandConstant:
				operand = pCmd.Parameters[4];
				break;
			case VarOperandVariable:
				operand = GetVariable(pCmd.Parameters[4]);
				break;
			case VarOperandVariableIndirect:
				if (pCmd.Parameters[4] < 1 || pCmd.Parameters[4] > GameSimulationState.MaxVariables)
				{
					_state.AddDiagnostic($"[Event {_eventId}] {label}: invalid indirect variable {pCmd.Parameters[4]} skipped");
					return;
				}
				operand = GetVariable(GetVariable(pCmd.Parameters[4]));
				break;
			default:
				_state.AddDiagnostic($"[Event {_eventId}] {label}: unsupported operand mode {pCmd.Parameters[3]} skipped");
				return;
		}
		// OperateValue negates the operand for the subtract operation.
		if (operation == ActorValueSubtract)
		{
			operand = -operand;
		}

		foreach (var actorId in actors)
		{
			if (pIsLevel)
			{
				var level = Math.Clamp(
					_state.GetActorLevel(actorId) + operand,
					GameSimulationState.MinActorLevel,
					GameSimulationState.MaxActorLevel);
				_state.SetActorLevel(actorId, level);
				_state.AddDiagnostic($"[Event {_eventId}] Change level: actor {actorId} -> level {level}");
			}
			else
			{
				var exp = Math.Clamp(
					_state.GetActorExp(actorId) + operand,
					0,
					GameSimulationState.MaxActorExp);
				_state.SetActorExp(actorId, exp);
				_state.AddDiagnostic($"[Event {_eventId}] Change exp: actor {actorId} -> exp {exp}");
			}
		}
	}

	/// <summary>
	/// EasyRPG GetActors(): mode 0 selects the party, mode 1 a single actor id,
	/// mode 2 the actor id stored in a variable. Returns null when the request is
	/// invalid, so the caller can fail closed.
	/// </summary>
	private List<int>? ResolveActors(int pActorMode, int pActorId, string pLabel)
	{
		switch (pActorMode)
		{
			case ActorSelectParty:
				return new List<int>(_state.PartyMemberIds);
			case ActorSelectHero:
				if (pActorId < 1 || pActorId > GameSimulationState.MaxActorId)
				{
					_state.AddDiagnostic($"[Event {_eventId}] {pLabel}: invalid actor id {pActorId} skipped");
					return null;
				}
				return new List<int> { pActorId };
			case ActorSelectVariableHero:
				if (pActorId < 1 || pActorId > GameSimulationState.MaxVariables)
				{
					_state.AddDiagnostic($"[Event {_eventId}] {pLabel}: invalid variable id {pActorId} skipped");
					return null;
				}
				var actorId = GetVariable(pActorId);
				if (actorId < 1 || actorId > GameSimulationState.MaxActorId)
				{
					_state.AddDiagnostic($"[Event {_eventId}] {pLabel}: variable {pActorId} holds invalid actor id {actorId}");
					return null;
				}
				return new List<int> { actorId };
			default:
				_state.AddDiagnostic($"[Event {_eventId}] {pLabel}: unsupported actor mode {pActorMode} skipped");
				return null;
		}
	}

	private void ExecuteChangeHeroName(Rm2kMap.EventCommand pCmd)
	{
		// EasyRPG CmdSetup minimum width 1; the actor id is the first parameter
		// and the new name is the command string.
		if (pCmd.Parameters.Count < 1)
		{
			Malformed("Change hero name");
			return;
		}
		var actorId = pCmd.Parameters[0];
		if (actorId < 1 || actorId > GameSimulationState.MaxActorId)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Change hero name: invalid actor id {actorId} skipped");
			return;
		}
		_state.SetActorName(actorId, pCmd.Text);
		_state.AddDiagnostic($"[Event {_eventId}] Change hero name: actor {actorId} renamed");
	}

	/// <summary>
	/// EasyRPG CommandChangeEventLocation: [eventId, operandMode, x, y] with an
	/// optional RPG2K3 direction in parameters[4].
	/// </summary>
	private void ExecuteChangeEventLocation(Rm2kMap.EventCommand pCmd)
	{
		if (pCmd.Parameters.Count < 4)
		{
			Malformed("Change event location");
			return;
		}
		if (_eventLocationSetter == null)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Change event location: no event location hook available");
			return;
		}
		var targetEventId = pCmd.Parameters[0];
		if (targetEventId < 1 || targetEventId > GameSimulationState.MaxActorId)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Change event location: invalid event id {targetEventId} skipped");
			return;
		}
		var x = ResolveEventCoordinate(pCmd.Parameters[1], pCmd.Parameters[2]);
		var y = ResolveEventCoordinate(pCmd.Parameters[1], pCmd.Parameters[3]);
		if (x < 0 || y < 0)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Change event location: invalid coordinates ({x},{y}) skipped");
			return;
		}
		var direction = pCmd.Parameters.Count > 4 ? pCmd.Parameters[4] - 1 : -1;
		if (direction is not (-1 or 0 or 1 or 2 or 3))
		{
			_state.AddDiagnostic($"[Event {_eventId}] Change event location: invalid direction {pCmd.Parameters[4]} skipped");
			return;
		}
		if (!_eventLocationSetter(targetEventId, x, y, direction))
		{
			_state.AddDiagnostic($"[Event {_eventId}] Change event location: event {targetEventId} not found");
			return;
		}
		_state.AddDiagnostic($"[Event {_eventId}] Change event location: event {targetEventId} -> ({x},{y})");
	}

	private int ResolveEventCoordinate(int pOperandMode, int pValue)
	{
		switch (pOperandMode)
		{
			case VarOperandConstant:
				return pValue;
			case VarOperandVariable:
				return pValue >= 1 && pValue <= GameSimulationState.MaxVariables ? GetVariable(pValue) : -1;
			case VarOperandVariableIndirect:
				if (pValue < 1 || pValue > GameSimulationState.MaxVariables)
				{
					return -1;
				}
				return GetVariable(GetVariable(pValue));
			default:
				return -1;
		}
	}

	/// <summary>
	/// EasyRPG CommandEraseEvent: the vanilla command carries no parameters and
	/// deactivates the event that owns the running command list.
	/// </summary>
	private bool ExecuteEraseEvent(Rm2kMap.EventCommand pCmd)
	{
		if (pCmd.Parameters.Count > 0)
		{
			// Patch-provided event ids are not modeled.
			_state.AddDiagnostic($"[Event {_eventId}] Erase event: parameterized form is not supported yet");
			return Advance();
		}
		if (_eventDeactivator == null)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Erase event: no event deactivation hook available");
			return Advance();
		}
		if (!_eventDeactivator(_eventId))
		{
			_state.AddDiagnostic($"[Event {_eventId}] Erase event: event {_eventId} not found");
			return Advance();
		}
		_state.AddDiagnostic($"[Event {_eventId}] Erase event: event {_eventId} deactivated");
		return FinishFrame();
	}

	/// <summary>
	/// 11110, Show Picture, from EasyRPG's <c>CommandShowPicture</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The parameters are
	/// <c>[id, positionMode, x, y, fixedToMap, magnify, topTransparency,
	/// useTransparent, red, green, blue, saturation, effectMode, effectPower,
	/// bottomTransparency]</c>, and the last one is **optional**: the reference
	/// only reads it when the command carries more than 14 parameters.
	/// </para>
	/// <para>
	/// <strong>Position and the picture number can both be variables</strong>,
	/// and <c>parameters[1]</c> is not a mode number in the usual sense: it is
	/// the value's mode <em>and</em> the Maniac patch packs the X and Y origin
	/// into its upper bits, which the reference masks off with
	/// <c>ManiacBitmask(com.parameters[1], 0xFF)</c>. A reader that treated the
	/// whole number as a mode would read mode 257 where a game meant mode 1.
	/// </para>
	/// <para>
	/// <strong>The magnitude is one value, not two.</strong> The reference sets
	/// <c>magnify_height = magnify_width</c>, so the picture is square in the
	/// magnification and <c>parameters[6]</c> is the top colour, not a height.
	/// </para>
	/// </remarks>
	private void ExecuteShowPicture(Rm2kMap.EventCommand pCmd)
	{
		if (_presentation == null)
		{
			Malformed("Show picture");
			return;
		}
		// CmdSetup minimum width 14. A command that is shorter is a truncated
		// file and not a picture with defaults.
		if (pCmd.Parameters.Count < 14)
		{
			Malformed("Show picture");
			return;
		}
		var pictureId = ValueOrVariable(Param(pCmd, 1), pCmd.Parameters[0]);
		// The mode lives in the low byte; the Maniac patch packs the origin
		// into the rest.
		var positionMode = pCmd.Parameters[1] & 0xFF;
		var x = ValueOrVariable(positionMode, pCmd.Parameters[2]);
		var y = ValueOrVariable(positionMode, pCmd.Parameters[3]);
		//
		// **The Maniac bitmask applies to the bottom transparency, and only to
		// the bottom one.** The reference masks parameters[14] with 0xFF because
		// the patch puts flags above the value, and it does *not* mask
		// parameters[6] — the top transparency is read whole. A first draft
		// masked both, which turned a top transparency of 100 into 100 and a
		// bottom of 100 into 100 by luck and any value above 255 into garbage.
		var topTransparency = Param(pCmd, 6);
		int? bottom = pCmd.Parameters.Count > 14
			? (pCmd.Parameters[14] & 0xFF)
			: null;

		var ok = _presentation.ShowPicture(
			pictureId, pCmd.Text, x, y,
			Param(pCmd, 4) > 0,
			Param(pCmd, 5),
			topTransparency,
			Param(pCmd, 7) > 0,
			Param(pCmd, 8), Param(pCmd, 9), Param(pCmd, 10),
			Param(pCmd, 11), Param(pCmd, 12), Param(pCmd, 13),
			bottom);
		if (!ok)
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Show picture {pictureId} \"{Truncate(pCmd.Text)}\""
				+ " refused: a bound was out of range");
			return;
		}
		_state.AddDiagnostic(
			$"[Event {_eventId}] Show picture {pictureId} \"{Truncate(pCmd.Text)}\""
			+ $" at {x},{y}");
	}

	/// <summary>
	/// Runs command 11120, Move Picture, from liblcf's <c>Code::MovePicture</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Sixteen parameters, and the width is a minimum and not a
	/// count.</strong> The reference's dispatch line is
	/// <c>case Cmd::MovePicture: return CmdSetup&lt;&amp;Game_Interpreter::CommandMovePicture, 16&gt;(com);</c>
	/// — **and I first wrote eight, from the five this reads plus a guess.** A
	/// reader with eight would have rejected every real game's move picture as
	/// a truncated file. The rest is the editor's own packing and is not read
	/// here.
	/// </para>
	/// <para>
	/// <strong>The id is resolved through the mode and the target is
	/// resolved through the same mode</strong> — the reference reads the
	/// picture number, the position mode, the X and the Y with the same helper
	/// the show command uses, so a game can move a picture to a position it
	/// computed. A reader that read the target as a constant would only ever be
	/// able to move a picture to a number the author typed.
	/// </para>
	/// <para>
	/// <strong>Zero frames is a placement.</strong> An editor field the author
	/// never touched reads as zero, and the reference sets the position and
	/// returns; refusing it would stop the event, and a game whose title card
	/// is placed by a zero-frame move would lose the card.
	/// </para>
	/// </remarks>
	private void ExecuteMovePicture(Rm2kMap.EventCommand pCmd)
	{
		if (_presentation == null)
		{
			Malformed("Move picture");
			return;
		}
		// **The reference's own minimum, 16** -- and not the five this method
		// reads. A command that is shorter is a truncated file.
		if (pCmd.Parameters.Count < 16)
		{
			Malformed("Move picture");
			return;
		}
		var mode = pCmd.Parameters[1] & 0xFF;
		var pictureId = ValueOrVariable(mode, pCmd.Parameters[0]);
		var x = ValueOrVariable(mode, pCmd.Parameters[2]);
		var y = ValueOrVariable(mode, pCmd.Parameters[3]);
		// **The duration is the last of the four and not the fifth.** The
		// reference reads parameters[4] and stops; a reader that went on to the
		// patch's own parameters would read a number that means something
		// else entirely in a game that carries the patch.
		var frames = pCmd.Parameters[4];
		// **The state, not the adapter.** The interpreter holds the state
		// itself, and the adapter is the runtime's way in; a command that went
		// through the adapter would have to know which of the two it was
		// talking to, and the answer would be "whichever the caller gave it".
		if (!_presentation.MovePicture(pictureId, x, y, frames))
		{
			_state.AddDiagnostic(
				$"Move picture {pictureId} was refused: no such picture is on the"
				+ " screen, so there is nothing to move.");
		}
	}

	/// <summary>
	/// 11130, Erase Picture, from EasyRPG's <c>CommandErasePicture</c>.
	/// </summary>
	/// <remarks>
	/// The id can be a variable, and <strong>erasing a picture that is not there
	/// is not an error</strong> — the reference returns true regardless. A
	/// reader that reported a refusal would make a game that legitimately erases
	/// twice look broken, so this one says which of the two happened.
	/// </remarks>
		private void ExecuteErasePicture(Rm2kMap.EventCommand pCmd)
		{
		if (_presentation == null)
		{
			Malformed("Erase picture");
			return;
		}
		// CmdSetup minimum width 1 -- a bare id, with no mode at all, is the
		// old form and the common one.
		if (pCmd.Parameters.Count < 1)
		{
			Malformed("Erase picture");
			return;
		}
		// **The id comes first and the mode second.** A first draft read
		// parameters[0] as the mode and parameters[1] as the id, which is the
		// order the *show* command uses for its position mode and not the
		// order this one does -- so a one parameter command, the form the
		// editor writes most, erased the picture numbered by nothing at all.
		var mode = pCmd.Parameters.Count > 1 ? pCmd.Parameters[1] : 0;
		var first = pCmd.Parameters[0];
		int last;
		switch (mode)
		{
			case 0:
			case 1:
				last = ValueOrVariable(mode, first);
				break;
			case 2:
			case 3:
				// A range, and the end is parameters[2].
				last = pCmd.Parameters.Count > 2
					? ValueOrVariable(mode, pCmd.Parameters[2])
					: first;
				break;
			default:
				_state.AddDiagnostic(
					$"[Event {_eventId}] Erase picture: unsupported mode {mode},"
					+ $" erasing only {first}");
				last = ValueOrVariable(0, first);
				break;
		}
		// A range's start is always a plain number -- the reference reads
		// parameters[0] straight out -- and only the end goes through the
		// mode. Modes 2 and 3 therefore resolve both ends as constants, and a
		// reader that ran the start through the mode would look up a variable
		// the game never named.
		var start = mode is 2 or 3 ? first : ValueOrVariable(mode, first);
		var low = Math.Min(start, last);
		var high = Math.Max(start, last);
		var erased = 0;
		var missing = 0;
		for (var id = low; id <= high; id++)
		{
			if (_presentation.ErasePicture(id, out var hatte))
			{
				erased++;
				if (!hatte)
				{
					missing++;
				}
			}
		}
		if (erased == 0)
		{
			// **Nothing was in range at all, which is not the same as the id
			// being out of bounds.** `ErasePicture` returns false for an id of
			// zero or past the limit, and true for an id in range that holds no
			// picture -- so a run of ids that were all in range and all empty
			// is a success with nothing in it. A first draft reported both as
			// a refusal, which told a game author their command was out of
			// bounds when the truth was that they had already erased it.
			var ausserhalb = low < 1 || high > PresentationState.MaxPictures;
			_state.AddDiagnostic(
				$"[Event {_eventId}] Erase picture {low}"
				+ (low == high ? string.Empty : $"..{high}")
				+ (ausserhalb
					? " refused: the id is out of bounds"
					: ": there was none, and the reference treats that as success"));
			return;
		}
		// **Erasing a picture that is not there is success.** The reference
		// returns true regardless, and a reader that reported a refusal would
		// make a game that legitimately erases twice look broken -- and games
		// do erase twice, because a page runs and then runs again.
		_state.AddDiagnostic(
			$"[Event {_eventId}] Erase picture {low}"
			+ (low == high ? string.Empty : $"..{high}")
			+ $": {erased} removed"
			+ (missing > 0
				? $", {missing} of them were not there, and the reference"
					+ " treats that as success"
				: string.Empty));
	}

	/// <summary>
	/// Reads a parameter that is a constant or a variable, from
	/// <c>ValueOrVariable</c> and this project's own <c>TargetEval</c> modes.
	/// </summary>
	/// <remarks>
	/// <strong>The mode is the same three the branch command uses</strong>:
	/// constant, variable, and — in the branch command — the indirect forms. The
	/// picture command reads a variable directly, not an expression, so a mode
	/// it does not know is a diagnostic rather than a guess.
	/// </remarks>
	private int ValueOrVariable(int pMode, int pValue)
	{
		switch (pMode)
		{
			case TargetEvalSingle:
				return pValue;
			case VarOperandVariable:
				return GetVariable(pValue);
			default:
				_state.AddDiagnostic(
					$"[Event {_eventId}] Picture: unsupported value mode {pMode},"
					+ $" using the constant {pValue}");
				return pValue;
		}
	}

	/// <summary>
	/// 11030, Tint Screen, from EasyRPG's <c>CommandTintScreen</c>.
	/// </summary>
	/// <remarks>
	/// Parameters are <c>[red, green, blue, saturation, tenths, wait]</c>,
	/// and <strong>the duration is in tenths of a second</strong> — the
	/// reference converts it itself with <c>tenths * DEFAULT_FPS / 10</c>,
	/// so a reader that stored tenths would report a number the engine
	/// never had.
	/// <para>
	/// <strong>Saturation is a percentage where 100 means untinted.</strong> A
	/// reader that treated 0 as "no tint" would have tinted the screen to
	/// grey at the one value that means "leave it alone" — and 0 is what a
	/// game writes when it wants no tint.
	/// </para>
	/// <para>
	/// <strong>The wait is conditional and it is a wait.</strong> The
	/// reference calls <c>SetupWait(tenths)</c> only when the last parameter
	/// is non-zero, which holds the page for as long as the tint runs.
	/// </para>
	/// </remarks>
	/// <summary>
	/// 11310, Player Visibility, from EasyRPG's
	/// <c>CommandPlayerVisibility</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// **The parameter is inverted, and that is the whole command:**
	/// <c>parameters[0] == 0</c> means <em>hidden</em>. A reader that
	/// mapped a non-zero to visible got the right answer for a command that
	/// hides and the wrong one for a command that shows — and a game whose
	/// only use of this command is to hide a sprite would work until the
	/// first time it showed one again.
	/// </para>
	/// <para>
	/// <strong>It also resets the through-position</strong>, which the
	/// reference does with its own comment "RPG_RT does this here" — so
	/// a player who has walked through a wall and is then hidden does not
	/// stay standing in the wall.
	/// </para>
	/// </remarks>
	/// <summary>
	/// 10820, Memorize Location, from EasyRPG's <c>CommandMemorizeLocation</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The three parameters are <strong>the variables to write into</strong>,
	/// not the values: <c>parameters[0]</c> gets the map id,
	/// <c>parameters[1]</c> the player x and <c>parameters[2]</c> the y. A
	/// reader that read them as the position to store would write the
	/// player's tile into three variables and store nothing at all.
	/// </para>
	/// <para>
	/// <strong>There is no matching recall here.</strong> <c>10830</c> is in
	/// liblcf and EasyRPG has no method for it in this build, so a game that
	/// memorizes and then recalls would have the first half and not the
	/// second. That asymmetry is the reference's, and it is worth recording
	/// rather than implementing a half from imagination.
	/// </para>
	/// </remarks>
	/// <summary>Which of the four player access rights a command addresses.</summary>
	private enum AccessFlag
	{
		Escape,
		Save,
		Menu,
	/// <summary>The teleport command, from <c>11820</c>.</summary>
	Teleport,
	}

	/// <summary>
	/// 11820, 11840, 11930 and 11960, the four one-line access commands.
	/// <c>11820</c> is <c>SetAllowTeleport</c> and was missing from the board.
	/// </summary>
	/// <remarks>
	/// <para>
	/// All three are one line in the reference —
	/// <c>SetAllowEscape(com.parameters[0] != 0)</c> and its two siblings — and
	/// <strong>that is the whole command</strong>. A zero is therefore not "no
	/// change" but a removal: a cutscene that locks the menu and a cutscene that
	/// unlocks it again write the same field, and **a reader that only ever set
	/// it to true could never give a player their menu back.**
	/// </para>
	/// <para>
	/// One parameter is the width, and the commands carry no more.
	/// </para>
	/// </remarks>
	/// <summary>
	/// 10920, Store Event ID, from EasyRPG's <c>CommandStoreEventID</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Both coordinates go through <c>ValueOrVariable</c></strong> with
	/// the same mode in <c>parameters[0]</c>, so a game can look up the tile it
	/// last walked over. A reader that read them as constants could only ever
	/// ask about one tile, and a game that follows a variable would store the
	/// wrong event or none.
	/// </para>
	/// <para>
	/// <strong>An empty tile stores 0 and does not hold the page</strong>,
	/// because that is the reference: <c>ev ? ev->GetId() : 0</c>. A game that
	/// asks about a tile with nothing on it gets a zero it can test, and a
	/// reader that held the page would leave the variable holding whatever it
	/// held before.
	/// </para>
	/// </remarks>
	private void ExecuteStoreEventId(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 4.
		if (pCmd.Parameters.Count < 4)
		{
			Malformed("Store event id");
			return;
		}
		if (_eventIdAtTile == null)
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Store event id: no event lookup is wired, so"
				+ " the tile cannot be asked; nothing was stored");
			return;
		}
		var x = ValueOrVariable(Param(pCmd, 0), Param(pCmd, 1));
		var y = ValueOrVariable(Param(pCmd, 0), Param(pCmd, 2));
		var varId = pCmd.Parameters[3];
		if (varId < 1 || varId > GameSimulationState.MaxVariables)
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Store event id: variable {varId} is outside"
				+ $" 1 to {GameSimulationState.MaxVariables}, and nothing was stored");
			return;
		}
		// **A tile outside the map is not a tile.** The reference would look it
		// up and get nothing back, so this stores nothing and says which
		// coordinate was wrong, rather than a zero that reads like "no event".
		if (x < 0 || y < 0 || x >= _state.MapWidth || y >= _state.MapHeight)
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Store event id: tile {x},{y} is outside the"
				+ $" map of {_state.MapWidth} by {_state.MapHeight}, and nothing"
				+ " was stored");
			return;
		}
		var eventId = _eventIdAtTile(x, y);
		_state.Variables[varId - 1] = eventId;
		_state.AddDiagnostic(
			$"[Event {_eventId}] Store event id: tile {x},{y} holds event"
			+ $" {eventId}, stored in variable {varId}");
	}

	/// <summary>
	/// 11810, Teleport Targets, from EasyRPG's
	/// <c>CommandTeleportTargets</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Parameters[0] is a mode and not a target id</strong>: a non-zero
	/// <em>removes</em> every target on that map and stops there. A reader that
	/// read it as the first target's index would add a point where the game
	/// meant to clear them.
	/// </para>
	/// <para>
	/// <strong>Parameters[4] says the switch must be on, not that there is
	/// one.</strong> A reader that read it as "use a switch" would make every
	/// conditional warp unconditional, and a secret entrance would open at the
	/// start of the game.
	/// </para>
	/// </remarks>
	private void ExecuteTeleportTargets(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 6.
		if (pCmd.Parameters.Count < 6)
		{
			Malformed("Teleport targets");
			return;
		}
		var mapId = pCmd.Parameters[1];
		if (pCmd.Parameters[0] != 0)
		{
			// **The removal takes every point on the map, not one.** A second
			// call on a map with nothing on it is not an error: the end state is
			// the one the game asked for either way.
			_state.TeleportTargets.Remove(mapId);
			_state.AddDiagnostic(
				$"[Event {_eventId}] Teleport targets: every target on map"
				+ $" {mapId} was removed, because the mode is not zero");
			return;
		}
		var x = pCmd.Parameters[2];
		var y = pCmd.Parameters[3];
		var requiresSwitchOn = pCmd.Parameters[4] != 0;
		var switchId = pCmd.Parameters[5];
		if (requiresSwitchOn && (switchId < 1
			|| switchId > GameSimulationState.MaxSwitches))
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Teleport targets: the point needs switch"
				+ $" {switchId}, which is outside 1 to"
				+ $" {GameSimulationState.MaxSwitches}, and the point was refused");
			return;
		}
		// **A point outside the map is refused, and the reference stores it
		// unchecked.** A warp to a tile that is not there is a warp into
		// nothing, and a game that declares one has a bug this reader can name.
		var usable = x >= 0 && y >= 0
			&& (mapId != _state.MapId
				|| (x < _state.MapWidth && y < _state.MapHeight));
		if (!usable)
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Teleport targets: tile {x},{y} on map"
				+ $" {mapId} is outside the map, and the point was refused");
			return;
		}
		if (!_state.TeleportTargets.TryGetValue(mapId, out var list))
		{
			list = [];
			_state.TeleportTargets[mapId] = list;
		}
		// **A second point on the same tile replaces the first.** The reference
		// appends, and a game that re-declares a point would otherwise have
		// two warps on one tile with no way to say which one is meant.
		list.RemoveAll(p => p.X == x && p.Y == y);
		list.Add(new GameSimulationState.TeleportTarget
		{
			MapId = mapId,
			X = x,
			Y = y,
			RequiresSwitchOn = requiresSwitchOn,
			SwitchId = requiresSwitchOn ? switchId : 0,
			IsUsable = true,
		});
		_state.AddDiagnostic(
			$"[Event {_eventId}] Teleport targets: {x},{y} on map {mapId} added,"
			+ (requiresSwitchOn
				? $" and it needs switch {switchId} on"
				: " and it is unconditional"));
	}

	/// <summary>
	/// 12420, Game Over, from EasyRPG's <c>CommandGameOver</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The reference takes <strong>no parameters at all</strong> — the command
	/// is written <c>const&amp; com</c> and never read — so this one does not
	/// check a width.
	/// </para>
	/// <para>
	/// <strong>It waits for a message to close first.</strong> That is the
	/// reference's first two lines: <c>if (Game_Message::IsMessageActive())
	/// return false;</c>. A hero who says their last line and dies should die
	/// after the line is read, and a reader that showed the game over screen on
	/// top of the text would bury the line the game wrote for that moment.
	/// </para>
	/// </remarks>
	/// <summary>
	/// Runs 11910 and 11950, the two commands that open a menu.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Width 0, and that is the whole shape of both.</strong> The
	/// reference's dispatch lines give the save and the main menu a width of
	/// zero, so a reader that required a parameter would have refused every
	/// game's menu and one that read <c>parameters[0]</c> would be reading past
	/// the end of a list that is not there.
	/// </para>
	/// <para>
	/// <strong>A request and not an open menu.</strong> This reader builds no
	/// menu scene, so the flag says what a command asked for — the same shape as
	/// the game over screen, and a caller that draws the menu from it is the
	/// runtime's business rather than the simulation's.
	/// </para>
	/// <para>
	/// <strong>Two flags and not one.</strong> A reader that stored "a menu" in
	/// a single field would have the save command clear the main menu's
	/// request, and a game that opened the main menu and then saved would find
	/// neither of them up.
	/// </para>
	/// <para>
	/// <strong>An open message first, and the menu waits for it</strong> — the
	/// same rule as the game over screen and the title request. A hero who says
	/// "here, take this menu" and has the menu cover the line is a game that hid
	/// a line the author wrote for that moment.
	/// </para>
	/// </remarks>
	private void ExecuteOpenMenu(Rm2kMap.EventCommand pCmd, bool pIsSave)
	{
		// **The command is not read, and that is the point.** Width zero means
		// there is nothing in it; the parameter exists so that one method can
		// serve both commands, and a reader that invented a use for it would be
		// inventing a semantic the format does not have.
		_ = pCmd;
		if (_presentation != null && _presentation.MessageVisible)
		{
			_state.WaitingFor = GameSimulationState.WaitReason.MessageOpen;
			_state.AddDiagnostic(
				$"[Event {_eventId}] {(pIsSave ? "Save" : "Main")} menu waits: a"
				+ " message is open, because the reference shows the menu after"
				+ " the line is read");
			return;
		}
		if (pIsSave)
		{
			_state.IsSaveMenuActive = true;
			_state.WaitingFor = GameSimulationState.WaitReason.SaveMenuOpen;
		}
		else
		{
			_state.IsMainMenuActive = true;
			_state.WaitingFor = GameSimulationState.WaitReason.MainMenuOpen;
		}
		_state.AddDiagnostic(
			$"[Event {_eventId}] {(pIsSave ? "Save" : "Main")} menu is requested,"
			+ " and the page holds until it is closed");
	}

	private void ExecuteGameOver()
	{
		if (_presentation != null && _presentation.MessageVisible)
		{
			_state.WaitingFor = GameSimulationState.WaitReason.MessageOpen;
			_state.AddDiagnostic(
				$"[Event {_eventId}] Game over waits: a message is open, because the"
				+ " reference shows the screen after the line is read");
			return;
		}
		_state.WaitingFor = GameSimulationState.WaitReason.GameOver;
		_state.IsGameOverActive = true;
		_state.AddDiagnostic(
			$"[Event {_eventId}] Game over is up, and the page holds here");
	}

	/// <summary>
	/// 12510, Return to Title Screen, from EasyRPG's
	/// <c>CommandReturnToTitleScreen</c>.
	/// </summary>
	/// <remarks>
	/// The reference makes this an <strong>asynchronous operation</strong>, so
	/// the page holds until the title screen is actually up. It also takes no
	/// parameters, and it waits for a message for the same reason
	/// <c>12420</c> does.
	/// </remarks>
	private void ExecuteReturnToTitle()
	{
		if (_presentation != null && _presentation.MessageVisible)
		{
			_state.WaitingFor = GameSimulationState.WaitReason.MessageOpen;
			_state.AddDiagnostic(
				$"[Event {_eventId}] Return to title waits: a message is open");
			return;
		}
		_state.WaitingFor = GameSimulationState.WaitReason.TitleRequested;
		_state.IsTitleRequested = true;
		_state.AddDiagnostic(
			$"[Event {_eventId}] Return to the title screen is requested, and the"
			+ " page holds until it is up");
	}

	/// <summary>
	/// 11830, Escape Target, from EasyRPG's <c>CommandEscapeTarget</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>The fourth parameter means the switch must be on</strong>, exactly
	/// as in <c>11810</c> — the reference reads
	/// <c>bool switch_on = com.parameters[3]</c> and pairs it with the switch id.
	/// A reader that read it as "use a switch" would make a locked escape point
	/// available from the first minute of the game, and a game that hides its
	/// exit behind a switch would be walkable straight out of it.
	/// </para>
	/// <para>
	/// <strong>There is exactly one and this replaces it.</strong> A map has
	/// many warp points and one place Escape goes to; a reader that kept a list
	/// would have to invent a rule for which one wins, and the game never wrote
	/// that rule.
	/// </para>
	/// </remarks>
	private void ExecuteEscapeTarget(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 5.
		if (pCmd.Parameters.Count < 5)
		{
			Malformed("Escape target");
			return;
		}
		var mapId = pCmd.Parameters[0];
		var x = pCmd.Parameters[1];
		var y = pCmd.Parameters[2];
		var requiresSwitchOn = pCmd.Parameters[3] != 0;
		var switchId = pCmd.Parameters[4];
		if (requiresSwitchOn && (switchId < 1
			|| switchId > GameSimulationState.MaxSwitches))
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Escape target needs switch {switchId}, which"
				+ $" is outside 1 to {GameSimulationState.MaxSwitches}, and the"
				+ " target was refused");
			return;
		}
		// **A point outside the map is refused, and the escape target is the
		// last thing that should be wrong**: a player who presses Escape and
		// lands nowhere is stuck, and a zero coordinate would send them to the
		// top left corner of the map instead.
		if (x < 0 || y < 0
			|| (mapId == _state.MapId
				&& (x >= _state.MapWidth || y >= _state.MapHeight)))
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Escape target {x},{y} on map {mapId} is"
				+ " outside the map, and the target was refused");
			return;
		}
		_state.EscapeTarget = new GameSimulationState.TeleportTarget
		{
			MapId = mapId,
			X = x,
			Y = y,
			RequiresSwitchOn = requiresSwitchOn,
			SwitchId = requiresSwitchOn ? switchId : 0,
			IsUsable = true,
		};
		_state.AddDiagnostic(
			$"[Event {_eventId}] Escape target is now {x},{y} on map {mapId},"
			+ (requiresSwitchOn
				? $" and it needs switch {switchId} on"
				: " and it is unconditional"));
	}

	/// <summary>
	/// 10660, Change System BGM, from EasyRPG's <c>CommandChangeSystemBGM</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Parameter 0 is a context, and there are seven of them</strong>:
	/// battle, victory, inn, boat, ship, airship, game over. A reader that
	/// offered one slot would let a game replace its battle theme and its inn
	/// theme at the same time, and a reader that offered twelve would offer
	/// five music slots that do not exist.
	/// </para>
	/// <para>
	/// <strong>Music has a fade-in and sounds do not</strong>, because a sound
	/// effect with a fade is a sound effect the player waited for. That is why
	/// this reads <c>parameters[1]</c> and <c>10670</c> does not.
	/// </para>
	/// </remarks>
	private void ExecuteChangeSystemBgm(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 7.
		if (pCmd.Parameters.Count < 7)
		{
			Malformed("Change system BGM");
			return;
		}
		var context = pCmd.Parameters[0];
		// **The enum is zero based — BGM_Battle is 0 — so the guard is 0 to 6, and
		// a reader that started at one would refuse the battle theme**, which is
		// the one a game changes most.
		if (context < 0 || context >= GameSimulationState.MaxSystemBgmContext)
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Change system BGM: context {context} is outside"
				+ $" 0 to {GameSimulationState.MaxSystemBgmContext - 1}, and the"
				+ " reference changes nothing for an unknown one");
			return;
		}
		var name = CommandStringOrVariable(pCmd, 5, 0, 6);
		var slot = new GameSimulationState.SystemBgm
		{
			Name = name ?? "",
			FadeIn = SystemBitfield(pCmd, 1),
			Volume = ClampPercent(SystemBitfield(pCmd, 2)),
			Tempo = ClampPercent(SystemBitfield(pCmd, 3)),
			Balance = ClampPercent(SystemBitfield(pCmd, 4)),
		};
		_state.SystemBgmSlots[context] = slot;
		_state.AddDiagnostic(
			$"[Event {_eventId}] System BGM slot {context} is now"
			+ $" \"{slot.Name}\", fade in {slot.FadeIn} ms, volume {slot.Volume},"
			+ $" tempo {slot.Tempo}, balance {slot.Balance}");
	}

	/// <summary>
	/// 10670, Change System SFX, from EasyRPG's <c>CommandChangeSystemSFX</c>.
	/// </summary>
	/// <remarks>
	/// <strong>Twelve contexts and not seven:</strong> the cursor, the decision,
	/// the cancel, the buzzer, the battle, the escape, and then one per battle
	/// event — enemy attack, enemy damage, ally damage, evasion, enemy death,
	/// item use. A reader that offered only the menu four would leave a game
	/// with a silent battle, and the six battle ones are the ones a game
	/// notices.
	/// </remarks>
	private void ExecuteChangeSystemSfx(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 6.
		if (pCmd.Parameters.Count < 6)
		{
			Malformed("Change system SFX");
			return;
		}
		var context = pCmd.Parameters[0];
		// **Zero based as well, and SFX_Cursor is 0.**
		if (context < 0 || context >= GameSimulationState.MaxSystemSfxContext)
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Change system SFX: context {context} is outside"
				+ $" 0 to {GameSimulationState.MaxSystemSfxContext - 1}, and the"
				+ " reference changes nothing for an unknown one");
			return;
		}
		var name = CommandStringOrVariable(pCmd, 4, 0, 5);
		var slot = new GameSimulationState.SystemSfx
		{
			Name = name ?? "",
			Volume = ClampPercent(SystemBitfield(pCmd, 1)),
			Tempo = ClampPercent(SystemBitfield(pCmd, 2)),
			Balance = ClampPercent(SystemBitfield(pCmd, 3)),
		};
		_state.SystemSfxSlots[context] = slot;
		_state.AddDiagnostic(
			$"[Event {_eventId}] System SFX slot {context} is now"
			+ $" \"{slot.Name}\", volume {slot.Volume}, tempo {slot.Tempo},"
			+ $" balance {slot.Balance}");
	}

	/// <summary>
	/// 10680, Change System Graphics, from EasyRPG's
	/// <c>CommandChangeSystemGraphics</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The name comes from the string field, <c>parameters[0]</c> is a stretch
	/// mode and <c>parameters[1]</c> a font. <strong>The reference casts both
	/// straight from the command without checking</strong> — the last line of its
	/// method is a bare <c>static_cast</c> pair — so this reader clamps them,
	/// because a cast that produces an out-of-range enum is a crash and not a
	/// diagnostic.
	/// </para>
	/// <para>
	/// <strong>An empty name is a request to go back to the database</strong>,
	/// not a file that does not exist: the reference has a
	/// <c>ResetSystemGraphic</c> and this is the command that reaches it.
	/// </para>
	/// </remarks>
	private void ExecuteChangeSystemGraphics(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 4.
		if (pCmd.Parameters.Count < 4)
		{
			Malformed("Change system graphics");
			return;
		}
		var name = CommandStringOrVariable(pCmd, 2, 0, 3) ?? "";
		_state.SystemGraphicName = name;
		_state.SystemGraphicStretch = Math.Clamp(pCmd.Parameters[0], 0, MaxSystemStretch);
		_state.SystemGraphicFont = Math.Clamp(pCmd.Parameters[1], 0, MaxSystemFont);
		_state.AddDiagnostic(
			$"[Event {_eventId}] System graphic is now"
			+ $" {(name.Length == 0 ? "the database default" : $"\"{name}\"")},"
			+ $" stretch {_state.SystemGraphicStretch},"
			+ $" font {_state.SystemGraphicFont}");
	}

	/// <summary>
	/// 10690, Change Screen Transitions, from EasyRPG's
	/// <c>CommandChangeScreenTransitions</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Setting a transition to the value the database already holds stores
	/// -1, and that is the whole command.</strong> The reference writes
	/// <c>return t != db ? t : -1;</c>: a game that sets a transition to the
	/// value it already has is asking to <em>stop overriding it</em>.
	/// </para>
	/// <para>
	/// <strong>A reader that stored the value would pin the transition
	/// forever</strong>, and a game that later changed its database row would be
	/// overridden by a command that meant "let go".
	/// </para>
	/// <para>
	/// <strong>Six transitions in three pairs</strong> — teleport in and out,
	/// battle start in and out, battle end in and out. A reader that offered one
	/// "transition" would make a game that fades out on teleport also fade out
	/// on entering a battle, and those are separate choices.
	/// </para>
	/// </remarks>
	private void ExecuteChangeScreenTransitions(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 2.
		if (pCmd.Parameters.Count < 2)
		{
			Malformed("Change screen transitions");
			return;
		}
		var which = pCmd.Parameters[0];
		if (which < 0 || which >= GameSimulationState.MaxTransition)
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Change screen transitions: {which} is not one"
				+ " of the six, and the reference asserts on it");
			return;
		}
		_state.SystemTransitions[which] = pCmd.Parameters[1];
		_state.AddDiagnostic(
			$"[Event {_eventId}] Transition {which} is now {pCmd.Parameters[1]},"
			+ " and -1 is the one that means the database default again");
	}

	/// <summary>
	/// The name a system audio or graphic command carries.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>The string field, and that is the whole thing without the Maniac
	/// patch.</strong> The reference reads
	/// <c>CommandStringOrVariableBitfield(com, 5, 0, 6)</c> and its first two
	/// lines are <c>if (!Player::IsPatchManiac()) return com.string;</c>. The
	/// patched path reads a game-string mirror this runtime does not have, and
	/// a game without the patch never reaches it.
	/// </para>
	/// <para>
	/// <strong>A game that does use the patch gets the plain name and a
	/// diagnostic</strong>, rather than a silently wrong one.
	/// </para>
	/// </remarks>
	private string CommandStringOrVariable(
		Rm2kMap.EventCommand pCmd, int pModeIndex, int pShift, int pValueIndex)
	{
		if (_state.SupportsManiacPatch)
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Command {pCmd.Code} carries a Maniac string"
				+ $" reference in parameters[{pModeIndex}]; this runtime has no"
				+ " game-string mirror, so the plain name from the string field was used");
		}
		return pCmd.Text;
	}
	/// Clamps a volume, tempo or balance to 0..100, from the liblcf bounds.
	/// </summary>
	/// <remarks>
	/// **The reference does not clamp these and the format does**, so a command
	/// that asked for 400 would write a number no player could hear. Clamping is
	/// the difference between "too loud" and "no sound at all".
	/// </remarks>
	private static int ClampPercent(int pValue)
	{
		return Math.Clamp(pValue, 0, 100);
	}

	/// <summary>The highest stretch mode the reference's enum has.</summary>
	private const int MaxSystemStretch = 1;

	/// <summary>The highest font the reference's enum has.</summary>
	private const int MaxSystemFont = 3;

	/// <summary>
	/// One audio or system number from a command, honouring the Maniac bitfield only
	/// when the game uses the patch.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Without the patch the parameter is the value.</strong> The
	/// reference reads <c>ValueOrVariableBitfield(com, 5, 1, 1)</c> and that
	/// helper returns <c>parameters[val_idx]</c> directly when the game is not
	/// a Maniac one — so the value index equals the mode index and the two are
	/// the same number.
	/// </para>
	/// <para>
	/// <strong>With the patch the mode index carries a bitfield</strong> and the
	/// value index is a different number. This reader does not implement the
	/// game-string mirror that mode needs, so it says so and uses the plain
	/// value rather than reading a packed bitfield out of a mode that means
	/// something else here.
	/// </para>
	/// </remarks>
	/// <summary>
	/// 10620, Change Hero Title, from EasyRPG's <c>CommandChangeHeroTitle</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>An invalid hero is a warning and not a refusal</strong> — the
	/// reference writes <c>Output::Warning</c> and <c>return true</c>. A reader
	/// that held the page would leave a cutscene waiting for a hero the database
	/// never had, and a game that changes a title for an actor slot it did not
	/// fill would hang on that line forever.
	/// </para>
	/// <para>
	/// <strong>The title is its own field and not the name.</strong> That is
	/// what <c>SetTitle</c> does, and a game that gives a hero a title keeps the
	/// database name for the party window.
	/// </para>
	/// </remarks>
	/// <summary>
	/// The offset between a command vehicle id and the liblcf vehicle type.
	/// </summary>
	/// <remarks>
	/// <strong>One, and not zero.</strong> The reference writes
	/// <c>(Game_Vehicle::Type)(com.parameters[0] + 1)</c> and its enum is
	/// <c>None = 0, Boat = 1, Ship = 2, Airship = 3</c> — those numbers are in
	/// the save format, so they are not an internal detail. Parameter 0 is the
	/// boat; a reader that used it directly would address vehicle 0, and
	/// vehicle 0 is the party and not a vehicle.
	/// </remarks>
	private const int FirstVehicleIdOffset = 1;
	/// <summary>
	/// 11710, Change Map Tileset, from EasyRPG's
	/// <c>CommandChangeMapTileset</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Chipset 0 is a real chipset and not "none".</strong> The
	/// reference compares against <c>Game_Map::GetChipset()</c> and returns
	/// early when they match, so a game that sets the chipset it already has
	/// pays nothing. A reader that treated zero as unset would redraw every
	/// time a game ran the command, and would refuse the first chipset in the
	/// database.
	/// </para>
	/// <para>
	/// The redraw itself has no equivalent here, so it is a diagnostic:
	/// <strong>a chipset that changed and nothing redrew looks exactly like a
	/// chipset that did not change</strong>, and the diagnostic is the only
	/// thing that tells them apart.
	/// </para>
	/// </remarks>
	private void ExecuteChangeMapTileset(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 2.
		if (pCmd.Parameters.Count < 2)
		{
			Malformed("Change map tileset");
			return;
		}
		var chipsetId = SystemBitfield(pCmd, 0);
		if (chipsetId == _state.ChipsetId)
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Chipset {chipsetId} is already the one this"
				+ " map uses, and the reference returns before touching anything");
			return;
		}
		if (!_state.SetChipset(chipsetId))
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Chipset {chipsetId} is outside 0 to"
				+ $" {GameSimulationState.MaxChipsetId}, and nothing was changed");
			return;
		}
		_state.AddDiagnostic(
			$"[Event {_eventId}] Chipset is now {chipsetId}, and the map has to"
			+ " be redrawn — nothing redrew it here, which is not the same as it"
			+ " having stayed the same");
	}

	/// <summary>
	/// 11720, Change PBG, from EasyRPG's <c>CommandChangePBG</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Six flags and two speeds, and they come from different
	/// parameters.</strong> The flags are 0, 1, 2 and 4; the horizontal speed is
	/// 3 and the vertical is 5. <em>A reader that read them in order would take
	/// the speed out of a flag</em> — and the fourth flag and the horizontal
	/// speed are adjacent, so the mistake is easy to make and hard to see.
	/// </para>
	/// <para>
	/// <strong>An empty name means the database panorama</strong> and not a file
	/// that does not exist: that is what the reference does with
	/// <c>if (!params.name.empty())</c> before it asks for the file.
	/// </para>
	/// <para>
	/// The reference also makes the interpreter <strong>wait</strong> for the
	/// panorama file, through an async yield. This reader has no file system
	/// here, so the wait is a diagnostic and the parameters are stored — a
	/// reader that waited forever would hang a game whose panorama is simply
	/// missing.
	/// </para>
	/// </remarks>
	private void ExecuteChangePbg(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 8.
		if (pCmd.Parameters.Count < 8)
		{
			Malformed("Change PBG");
			return;
		}
		var name = CommandStringOrVariable(pCmd, 6, 0, 7);
		var parallax = new GameSimulationState.Parallax
		{
			Name = name,
			ScrollHorizontally = pCmd.Parameters[0] != 0,
			ScrollVertically = pCmd.Parameters[1] != 0,
			ScrollHorizontallyAutomatic = pCmd.Parameters[2] != 0,
			// **Parameter 3 and not parameter 4.** The reference writes
			// scroll_horz_speed from ValueOrVariableBitfield(com, 6, 4, 3):
			// shift 4 in the mode, index 3 for the value.
			HorizontalSpeed = SystemBitfield(pCmd, 3),
			ScrollVerticallyAutomatic = pCmd.Parameters[4] != 0,
			// And parameter 5, with the same shape.
			VerticalSpeed = SystemBitfield(pCmd, 5),
		};
		_state.SetParallax(parallax);
		_state.AddDiagnostic(
			$"[Event {_eventId}] Panorama is now"
			+ $" {(name.Length == 0 ? "the database one" : $"\"{name}\"")},"
			+ $" scroll is {parallax.ScrollHorizontally} /"
			+ $" {parallax.ScrollVertically}, automatic is"
			+ $" {parallax.ScrollHorizontallyAutomatic} at"
			+ $" {parallax.HorizontalSpeed} and"
			+ $" {parallax.ScrollVerticallyAutomatic} at"
			+ $" {parallax.VerticalSpeed}");
		if (name.Length > 0)
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] The reference waits for the panorama file"
				+ " before it continues; this reader has no file system here, so"
				+ " the parameters are stored and the wait is not taken");
		}
	}

	/// <summary>
	/// 11740, Change Encounter Steps, from EasyRPG's
	/// <c>CommandChangeEncounterSteps</c>.
	/// </summary>
	/// <remarks>
	/// <strong>Zero is a real value and it is the one that turns random
	/// encounters off.</strong> A reader that treated zero as unset could never
	/// turn them off, and a game that does so would keep fighting every few
	/// steps for the rest of the map. The reference writes the value straight
	/// through with no check, so the bound is this reader's and the diagnostic
	/// says which values are allowed.
	/// </remarks>
	private void ExecuteChangeEncounterSteps(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 1.
		if (pCmd.Parameters.Count < 1)
		{
			Malformed("Change encounter steps");
			return;
		}
		var steps = pCmd.Parameters[0];
		if (!_state.SetEncounterSteps(steps))
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Encounter steps {steps} is outside 0 to"
				+ $" {GameSimulationState.MaxEncounterSteps}, and nothing was"
				+ " changed");
			return;
		}
		_state.AddDiagnostic(
			$"[Event {_eventId}] An encounter now comes every {steps} steps,"
			+ (steps == 0
				? " and zero means never — the reference writes it through"
				: " and a game set this to zero to stop fighting"));
	}

	// The reference's own subcommand indices, in the order its option enum
	// declares them. They are the numbers `CommandOptionGeneric` compares
	// against, and they are not the command codes.
	private const int SubIdxShopTransaction = 0;
	private const int SubIdxShopNoTransaction = 1;
	private const int SubIdxInnStay = 2;
	private const int SubIdxInnNoStay = 3;
	/// <summary>
	/// And the subcommand index of a victory, and it is a number and
	/// not a command code.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>And these are the reference's own subcommand indices, in
	/// the order its option enum declares them</strong>, -- <strong>and
	/// they are the numbers <c>CommandOptionGeneric</c> compares
	/// against</strong>, -- <strong>and they are not the command
	/// codes</strong>, -- <strong>and a reader that used
	/// <c>20710</c> here would compare a handler against a
	/// position.</strong>
	/// </para>
	/// <para>
	/// <strong>And they are public because the runtime writes the
	/// outcome</strong>, -- <strong>and it writes
	/// <c>BattleSubcommand</c>, which nothing in this repository read
	/// before.</strong> -- <strong>And that is why the field existed
	/// and no test failed: it was written once and never
	/// read.</strong>
	/// </para>
	/// </remarks>
	public const int SubIdxVictory = 4;

	/// <summary>And the subcommand index of an escape, which is five.</summary>
	public const int SubIdxEscape = 5;

	/// <summary>
	/// And the subcommand index of a defeat, which is six and not the
	/// same as an escape.
	/// </summary>
	public const int SubIdxDefeat = 6;
	private const int SubIdxBranchBattleElse = 7;

	/// <summary>
	/// Runs 10720, Open Shop, from liblcf's <c>Code::OpenShop</c> and
	/// EasyRPG's <c>Game_Interpreter_Map::CommandOpenShop</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Width 4, and the second parameter is a price and not a stock
	/// count.</strong> The reference's own <c>AddItem</c> takes the id and the
	/// price, so a reader that read the second parameter as "how many" would
	/// have shown one item where the shopkeeper keeps fifty.
	/// </para>
	/// <para>
	/// <strong>And the first parameter is a value or a variable</strong> — the
	/// reference runs it through its own <c>ValueOrVariable</c> helper, so a
	/// shop whose id came out of a variable opens with that shop and not with
	/// the number the file happens to carry.
	/// </para>
	/// </remarks>
		private void ExecuteOpenShop(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 4.
		if (pCmd.Parameters.Count < 4)
		{
			Malformed("Open shop");
			return;
		}

		// **The first parameter is a mode and not a value**, and the
		// reference's switch on it has three cases and a default that does
		// nothing: 0 buys and sells, 1 buys only, 2 sells only.
		var kaufen = pCmd.Parameters[0] == 0 || pCmd.Parameters[0] == 1;
		var verkaufen = pCmd.Parameters[0] == 0 || pCmd.Parameters[0] == 2;
		if (pCmd.Parameters[0] > 2)
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Open shop: mode {pCmd.Parameters[0]} is not "
				+ "0, 1 or 2, and the reference's switch leaves buying and "
				+ "selling both off");
		}

		var art = pCmd.Parameters[1];
		// **Parameter 2 is a flag the reference reads and does not use** — it
		// keeps it in the file for the reader's own documentation, and its
		// comment says so. **A reader that used it as the item count would
		// have filled a shop with the number of its own handlers.**
		var hatHandler = pCmd.Parameters[2] != 0;

		// **And the goods start at parameter 4, not at 3** — everything from
		// there on is copied into the shop's list, so a width of 4 means a
		// shop with no goods at all.
		_state.OpenShop(art, kaufen, verkaufen, hatHandler);
		for (var i = 4; i < pCmd.Parameters.Count; i++)
		{
			// **The loop variable and the value it carries are not the same
			// thing** — a reader that passed `i` would have stocked a shop
			// with 4, 5, 6 instead of the ids the game wrote.
			_state.AddShopGood(pCmd.Parameters[i]);
		}

		_state.ActiveShopOption = GameSimulationState.ShopOption.ShopTransaction;
		_state.AddDiagnostic(
			$"[Event {_eventId}] Open shop: type {art}, "
			+ $"{(kaufen ? "buys and sells" : verkaufen ? "sells only" : "neither")}, "
			+ $"{_state.ShopItemIds.Count} goods");
	}

	/// <summary>
	/// Runs 10730, Show Inn, from liblcf's <c>Code::ShowInn</c> and EasyRPG's
	/// <c>Game_Interpreter_Map::CommandShowInn</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>The price is the second parameter, and the first is the inn's
	/// type.</strong> The reference writes <c>int inn_price =
	/// com.parameters[1]</c> in the first two lines of the command, and the
	/// third parameter is the handler flag it does not use. <strong>A reader
	/// that took the type for the price would have charged a party the
	/// inn's kind for a night's rest</strong> — and a game's inn for a coin
	/// would have been free.
	/// </para>
	/// <para>
	/// <strong>A price of zero skips the prompt.</strong> The reference writes
	/// its own "Skip prompt" branch for a zero price, so a game's free inn
	/// never opens a window at all.
	/// </para>
	/// </remarks>
/// <summary>
	/// Runs 20710, 20711 and 20712, the three battle outcome handlers, from
	/// EasyRPG's <c>CommandVictoryHandler</c>, <c>CommandEscapeHandler</c> and
	/// <c>CommandDefeatHandler</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>One shape for three commands, and the reference writes it
	/// three times.</strong> Each is a
	/// <c>CommandOptionGeneric(com, option, {...})</c> that names the outcome
	/// it answers and the list of commands that follow it — <strong>and the
	/// whole of a handler is that call</strong>, with no parameter read and no
	/// effect of its own.
	/// </para>
	/// <para>
	/// <strong>The handler list is the commands up to the matching closer</strong>
	/// — for the victory handler the reference's own list is
	/// <c>{Cmd::EscapeHandler, Cmd::DefeatHandler, Cmd::EndBattle}</c>, so
	/// what runs after a victory is chosen by the game and not by this reader.
	/// </para>
	/// </remarks>
		private bool ExecuteBattleHandler(GameSimulationState.BattleOutcome pOutcome)
	{
		var subIdx = pOutcome switch
		{
			GameSimulationState.BattleOutcome.Victory => SubIdxVictory,
			GameSimulationState.BattleOutcome.Escape => SubIdxEscape,
			_ => SubIdxDefeat,
		};
		return ExecuteOptionHandler(subIdx, "battle handler", pOutcome);
	}

/// <summary>
	/// Runs 20713, End Battle, from EasyRPG's <c>CommandEndBattle</c>.
	/// </summary>
	/// <remarks>
	/// <strong>Width 0, and it is the one handler that ends something.</strong>
	/// The reference clears the battle state and returns true — a reader that
	/// made it a plain option would have left a game's battle running with no
	/// way out of it, and the reward screen it was written to reach would never
	/// have opened.
	/// </remarks>
	private bool ExecuteEndBattle()
	{
		_state.IsBattleActive = false;
		_state.ActiveBattleOption = GameSimulationState.BattleOutcome.None;
		_state.AddDiagnostic("[Event " + _eventId + "] End battle");
		return Advance();
	}

	/// <summary>
	/// Runs 20720 and 20721, the two shop handlers, from EasyRPG's
	/// <c>CommandTransaction</c> and <c>CommandNoTransaction</c>.
	/// </summary>
	/// <remarks>
	/// <strong>The two are not the same with a flag.</strong> The reference's
	/// lists are <c>{Cmd::NoTransaction, Cmd::EndShop}</c> and
	/// <c>{Cmd::EndShop}</c> — <strong>so a shop with two handlers can run
	/// one and then the other, and a shop with one runs it and
	/// ends.</strong> The trading itself is the shop's, and the handler list is
	/// what the game wrote between the opener and the closer.
	/// </remarks>
		private bool ExecuteShopHandler(GameSimulationState.ShopOption pOption)
	{
		var subIdx = pOption == GameSimulationState.ShopOption.Transaction
			? SubIdxShopTransaction
			: SubIdxShopNoTransaction;
		return ExecuteOptionHandler(subIdx, "shop handler", pOption);
	}

/// <summary>
	/// Runs 20722, End Shop, from EasyRPG's <c>CommandEndShop</c>.
	/// </summary>
	/// <remarks>
	/// <strong>Width 0, and it closes rather than opens.</strong> The
	/// reference calls <c>Game_Shop::SetMode(Game_Shop::Close)</c> and clears
	/// the option, and a reader that only set the option would have left a
	/// shop on screen with the command meant to shut it gone.
	/// </remarks>
		private bool ExecuteEndShop()
	{
		// **The reference's whole command is `return true;` and it changes
		// nothing** — the shop's own scene closed when the player left it,
		// and this command only tells the interpreter to carry on. **A reader
		// that cleared the shop state here would have had a game's shop
		// close the moment its own block ended**, which is a different event.
		_state.AddDiagnostic("[Event " + _eventId + "] End shop");
		return Advance();
	}

/// <summary>
	/// Runs 20730 and 20731, the two inn handlers, from EasyRPG's
	/// <c>CommandStay</c> and <c>CommandNoStay</c>.
	/// </summary>
	/// <remarks>
	/// <strong>Same shape as the shop's two, with the same two-arm list.</strong>
	/// The reference's lists are <c>{Cmd::NoStay, Cmd::EndInn}</c> and
	/// <c>{Cmd::EndInn}</c> — <strong>and the party is not healed here</strong>:
	/// the reference's <c>CommandStay</c> is the handler, and what a stay does
	/// to the party's hit points is the inn's own business, which is the same
	/// split the reference makes for a shop's trading.
	/// </remarks>
		private bool ExecuteInnHandler(GameSimulationState.ShopOption pOption)
	{
		var subIdx = pOption == GameSimulationState.ShopOption.Stay
			? SubIdxInnStay
			: SubIdxInnNoStay;
		return ExecuteOptionHandler(subIdx, "inn handler", pOption);
	}

/// <summary>
	/// Runs 20732, End Inn, from EasyRPG's <c>CommandEndInn</c>.
	/// </summary>
	 /// <remarks>
	/// <strong>Width 0, and it is the inn's counterpart of 20722.</strong>
	/// </remarks>
	private bool ExecuteEndInn()
	{
		_state.CloseInn();
		_state.AddDiagnostic("[Event " + _eventId + "] End inn");
		return Advance();
	}

	/// <summary>
	/// Runs one handler, the reference's <c>CommandOptionGeneric</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>A handler runs its block only when it is the option that was
	/// chosen, and skips it when it is not.</strong> That is the reference's
	/// whole method: one comparison of the sub-index against the option, and
	/// then either the sentinel write or the skip. <strong>A reader that always
	/// skipped would run a shop's "you bought nothing" branch beside its "you
	/// bought something" branch</strong>, and a reader that always ran would
	/// run both.
	/// </para>
	/// <para>
	/// <strong>And the index is not moved when the handler ran</strong> — the
	/// reference's sentinel is what stops the second handler, and the commands
	/// after the handler are the ones the reference returns to.
	/// </para>
	/// </remarks>
	private bool ExecuteOptionHandler(
		int pSubIdx,
		string pName,
		object pOption)
	{
		if (_state.IsSubcommandChosen(pSubIdx))
		{
			_state.ActiveShopOption = pOption is GameSimulationState.ShopOption s
				? s
				: _state.ActiveShopOption;
			_state.AddDiagnostic(
				$"[Event {_eventId}] {pName}: this is the chosen option, so "
				+ "its block runs");
			return Advance();
		}

		// **The other arm: skip to the next handler in the list.** The
		// reference's `SkipToNextConditional` and this reader's walk are the
		// same walk, and the bound is checked before the step.
		var schliesser = SchliesserOf(pSubIdx);
		var uebersprungen = SkipToOneOf(schliesser, pName);
		_state.AddDiagnostic(
			$"[Event {_eventId}] {pName}: not the chosen option, so skipped "
			+ $"{uebersprungen} commands");
		return Advance();
	}

	/// <summary>
	/// The commands that end a handler's block, from the sub-index.
	/// </summary>
	/// <remarks>
	/// <strong>These are the reference's own lists, and their lengths
	/// differ.</strong> A shop transaction ends at the no-transaction or the
	/// end-shop command; a no-transaction ends at the end shop alone. A
	/// victory ends at the escape, defeat or end-battle command; a defeat at
	/// the end battle alone.
	/// </remarks>
	private static int[] SchliesserOf(int pSubIdx)
	{
		return pSubIdx switch
		{
			SubIdxShopTransaction => new[]
			{
				NoTransaction, EndShop,
			},
			SubIdxShopNoTransaction => new[] { EndShop },
			SubIdxInnStay => new[] { NoStay, EndInn },
			SubIdxInnNoStay => new[] { EndInn },
			SubIdxVictory => new[]
			{
				EscapeHandler, DefeatHandler, EndBattle,
			},
			SubIdxEscape => new[] { DefeatHandler, EndBattle },
			_ => new[] { EndBattle },
		};
	}

	/// <summary>
	/// Skips the commands a handler owns, up to the closer that matches it.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Each handler has its own closer, and the closer is the
	/// command that follows the block.</strong> A victory handler's block ends
	/// at the escape, defeat or end-battle command; a shop transaction's ends
	/// at the no-transaction or end-shop command; an inn's at the no-stay or
	/// end-inn command. <strong>A reader that skipped to one global "end" would
	/// have taken a game's second handler with its first.</strong>
	/// </para>
	/// <para>
	/// The bound is checked before the step, exactly as
	/// <see cref="SkipToNextChoiceEnd"/> does — a block with no closer is a
	/// game bug, and the index is left on the last command so the page finishes
	/// instead of walking past the list.
	/// </para>
	/// </remarks>
	private int SkipToHandlerEnd(GameSimulationState.BattleOutcome pOutcome)
	{
		var schliesser = pOutcome switch
		{
			GameSimulationState.BattleOutcome.Victory => new[]
			{
				EscapeHandler, DefeatHandler, EndBattle,
			},
			GameSimulationState.BattleOutcome.Escape => new[]
			{
				DefeatHandler, EndBattle,
			},
			_ => new[] { EndBattle },
		};
		return SkipToOneOf(schliesser, "battle handler");
	}

	/// <summary>
	/// Skips the commands a shop or inn handler owns, up to its closer.
	/// </summary>
	private int SkipToHandlerEnd(GameSimulationState.ShopOption pOption)
	{
		int[] schliesser = pOption switch
		{
			GameSimulationState.ShopOption.ShopTransaction => new[]
			{
				NoTransaction, EndShop,
			},
			GameSimulationState.ShopOption.Transaction => new[]
			{
				NoTransaction, EndShop,
			},
			GameSimulationState.ShopOption.NoTransaction => new[]
			{
				EndShop,
			},
			GameSimulationState.ShopOption.InnStay => new[]
			{
				NoStay, EndInn,
			},
			GameSimulationState.ShopOption.Stay => new[]
			{
				NoStay, EndInn,
			},
			_ => new[] { EndInn },
		};
		return SkipToOneOf(schliesser, "shop handler");
	}

	/// <summary>
	/// Skips forward to the first of a set of closing codes, or to the end of
	/// the list when there is none.
	/// </summary>
	/// <param name="pClosers">The codes that end the block.</param>
	/// <param name="pName">What the block is, for the diagnostic.</param>
	/// <returns>How many commands were skipped.</returns>
	private int SkipToOneOf(int[] pClosers, string pName)
	{
		var start = _commandIndex;
		while (_commandIndex + 1 < _commands.Count)
		{
			_commandIndex++;
			var code = _commands[_commandIndex].Code;
			foreach (var closer in pClosers)
			{
				if (code == closer)
				{
					return _commandIndex - start;
				}
			}
		}

		_commandIndex = _commands.Count - 1;
		_state.AddDiagnostic(
			$"[Event {_eventId}] {pName} has no closer in the list, so the "
			+ "commands after it ran as ordinary ones");
		return _commandIndex - start;
	}

	/// <summary>
	/// Runs 10440, Change Skills, from liblcf's <c>Code::ChangeSkills</c> and
	/// EasyRPG's <c>Game_Interpreter::CommandChangeSkills</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Actors, then a flag that means remove, then a value or a
	/// variable.</strong> The first two parameters are the reference's own
	/// <c>GetActors(mode, id)</c>, and its three modes are 0 the party, 1 one
	/// hero by id and 2 the hero a variable names.
	/// </para>
	/// <para>
	/// <strong>And the command ends in <c>CheckGameOver()</c></strong> — so a
	/// game whose last hero is killed reaches the game-over screen from a
	/// skill command, which is odd and is what the reference does.
	/// </para>
	/// </remarks>
	private void ExecuteChangeSkills(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 5.
		if (pCmd.Parameters.Count < 5)
		{
			Malformed("Change skills");
			return;
		}

		var actoren = ResolveActors(
			pCmd.Parameters[0], pCmd.Parameters[1], "Change skills");
		if (actoren is null)
		{
			return;
		}

		// **The reference's own read: a truth removes.** A reader that read
		// it as "add" would have taught a skill to a hero whose command meant
		// to take it away.
		var entfernen = pCmd.Parameters[2] != 0;
		var skillId = ValueOrVariable(pCmd.Parameters[3], pCmd.Parameters[4]);

		foreach (var actorId in actoren)
		{
			var skills = _state.SkillsOf(actorId);
			if (entfernen)
			{
				skills.Remove(skillId);
			}
			else
			{
				skills.Add(skillId);
			}
		}

		// **And this is the reference's last line.**
		_state.CheckGameOver();
		_state.AddDiagnostic(
			$"[Event {_eventId}] Change skills: {(entfernen ? "unlearn" : "learn")}"
			+ $" skill {skillId} on {actoren.Count} actors");
	}

	/// <summary>
	/// Runs 10450, Change Equipment, from liblcf's
	/// <c>Code::ChangeEquipment</c> and EasyRPG's
	/// <c>Game_Interpreter::CommandChangeEquipment</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Two modes, and the slot means something different in
	/// each.</strong> Mode 0 reads the item and takes its slot from the
	/// item's <em>type</em> across five types; mode 1 reads
	/// <c>parameters[3] + 1</c> directly. <strong>A reader that took the slot
	/// from the parameter in both modes would have put a helmet where a sword
	/// goes</strong>, and it is the item's type that says which.
	/// </para>
	/// <para>
	/// <strong>And the sixth slot is not a slot.</strong> The reference checks
	/// <c>slot == 6</c> before any of the five and empties the whole actor — so
	/// a reader that treated the sixth value as a sixth slot would have had a
	/// game's "unequip everything" write a hidden entry and leave every real
	/// slot on.
	/// </para>
	/// <para>
	/// <strong>And a third mode returns false and does nothing</strong> — the
	/// only one of the three that refuses rather than repairing.
	/// </para>
	/// </remarks>
	private void ExecuteChangeEquipment(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 5.
		if (pCmd.Parameters.Count < 5)
		{
			Malformed("Change equipment");
			return;
		}

		var itemId = 0;
		GameSimulationState.EquipmentSlot slot;
		switch (pCmd.Parameters[2])
		{
			case 0:
				{
					itemId = ValueOrVariable(pCmd.Parameters[3], pCmd.Parameters[4]);
					var art = EquipmentKindOf(itemId);
					if (art == GameSimulationState.EquipmentKind.None)
					{
						// **The reference's default arm returns true and
						// touches nothing** — an item that is not equipment
						// is a game bug, and it is not a refusal.
						_state.AddDiagnostic(
							$"[Event {_eventId}] Change equipment: item {itemId} "
							+ "is not equipment, and the reference's own "
							+ "switch leaves it alone");
						return;
					}

					slot = SlotOf(art);
					break;
				}

			case 1:
				{
					// **The direct slot is the parameter plus one**, because
					// the reference's slot numbers start at one and its
					// enum at zero.
					itemId = 0;
					slot = SlotOfNumber(pCmd.Parameters[3] + 1);
					break;
				}

			default:
				_state.AddDiagnostic(
					$"[Event {_eventId}] Change equipment: mode {pCmd.Parameters[2]} "
					+ "is not 0 or 1, and the reference's switch returns false "
					+ "and does nothing");
				return;
		}

		var actoren = ResolveActors(
			pCmd.Parameters[0], pCmd.Parameters[1], "Change equipment");
		if (actoren is null)
		{
			return;
		}

		foreach (var actorId in actoren)
		{
			if (slot == GameSimulationState.EquipmentSlot.All)
			{
				// **The reference's own branch: everything goes at once.**
				_state.RemoveWholeEquipment(actorId);
				continue;
			}

			// **A two-weapon actor with a shield in hand keeps it** — the
			// reference skips the assignment outright, and a reader that only
			// wrote into the shield slot would have given him a shield and
			// then a second shield in the same slot.
			if (_state.HasTwoWeapons(actorId)
				&& slot == GameSimulationState.EquipmentSlot.Shield
				&& itemId != 0)
			{
				continue;
			}

			// **And the second weapon: the reference puts a one-handed
			// weapon into the second slot when the first is empty and
			// neither weapon is two-handed.** A reader that only wrote into
			// the weapon slot would have given a two-handed swordsman a
			// second sword in his shield hand.
			if (_state.HasTwoWeapons(actorId)
				&& slot == GameSimulationState.EquipmentSlot.Weapon
				&& itemId != 0
				&& _state.GetEquipment(
					actorId, GameSimulationState.EquipmentSlot.Weapon) == 0
				&& !_state.TwoHandedWeapons.Contains(itemId))
			{
				_state.ChangeEquipment(
					actorId, GameSimulationState.EquipmentSlot.Shield, itemId);
				continue;
			}

			_state.ChangeEquipment(actorId, slot, itemId);
		}

		// **And this is the reference's last line.**
		_state.CheckGameOver();
		_state.AddDiagnostic(
			$"[Event {_eventId}] Change equipment: {slot} on "
			+ $"{actoren.Count} actors, item {itemId}");
	}

	/// <summary>
	/// Runs 10480, Change Condition, from liblcf's <c>Code::ChangeCondition</c>
	/// and EasyRPG's <c>Game_Interpreter::CommandChangeCondition</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Width 4, and the reference's own comment records two RPG_RT
	/// quirks it reproduces.</strong> On the map it removes a state even when
	/// the actor has it from equipment — <c>RemoveState(id,
	/// !IsBattleRunning())</c> — and it always adds a state from an event
	/// command, even a battle state. <strong>Both are cases where the obvious
	/// implementation is the wrong one</strong>: a reader that respected the
	/// equipment's own state would have left a hero permanently poisoned by a
	/// ring he never took off, in a game the reference lets him walk out of.
	/// </para>
	/// <para>
	/// <strong>And the flag is the third parameter and it means remove.</strong>
	/// </para>
	/// </remarks>
	private void ExecuteChangeCondition(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 4.
		if (pCmd.Parameters.Count < 4)
		{
			Malformed("Change condition");
			return;
		}

		var actoren = ResolveActors(
			pCmd.Parameters[0], pCmd.Parameters[1], "Change condition");
		if (actoren is null)
		{
			return;
		}

		var entfernen = pCmd.Parameters[2] != 0;
		var zustand = pCmd.Parameters[3];

		foreach (var actorId in actoren)
		{
			var zustaende = _state.ConditionsOf(actorId);
			if (entfernen)
			{
				// **The reference removes it and does not ask where it came
				// from** -- its own comment says so in as many words.
				zustaende.Remove(zustand);
			}
			else
			{
				zustaende.Add(zustand);
			}
		}

		// **And this is the reference's last line**, for the third command
		// that carries it.
		_state.CheckGameOver();
		_state.AddDiagnostic(
			$"[Event {_eventId}] Change condition: {(entfernen ? "remove" : "add")}"
			+ $" condition {zustand} on {actoren.Count} actors");
	}

	/// <summary>
	/// What kind of equipment an item is, for 10450's first mode.
	/// </summary>
	/// <remarks>
	/// <strong>Five kinds, and the sixth — "everything" — is not one of
	/// them.</strong> A reader that mapped "all" onto an item kind would have
	/// had a game's "unequip everything" command equip a sixth slot.
	/// </remarks>
	private GameSimulationState.EquipmentKind EquipmentKindOf(int pItemId)
	{
		return _state.EquipmentKindOf(pItemId);
	}

	/// <summary>
	/// The slot a kind of equipment goes in, from the reference's own
	/// <c>slot = item-&gt;type</c>.
	/// </summary>
	private static GameSimulationState.EquipmentSlot SlotOf(
		GameSimulationState.EquipmentKind pKind)
	{
		return pKind switch
		{
			GameSimulationState.EquipmentKind.Weapon =>
				GameSimulationState.EquipmentSlot.Weapon,
			GameSimulationState.EquipmentKind.Shield =>
				GameSimulationState.EquipmentSlot.Shield,
			GameSimulationState.EquipmentKind.Armor =>
				GameSimulationState.EquipmentSlot.Armor,
			GameSimulationState.EquipmentKind.Helmet =>
				GameSimulationState.EquipmentSlot.Helmet,
			GameSimulationState.EquipmentKind.Accessory =>
				GameSimulationState.EquipmentSlot.Accessory,
			_ => GameSimulationState.EquipmentSlot.All,
		};
	}

	/// <summary>
	/// A slot from the reference's one-based number, including its sixth
	/// "everything".
	/// </summary>
	/// <remarks>
	/// <strong>Six is not out of range.</strong> The reference's own numbers
	/// run one to six with six meaning "remove everything", and a reader that
	/// treated a six as an invalid slot would have refused the one number
	/// that means something.
	/// </remarks>
	private static GameSimulationState.EquipmentSlot SlotOfNumber(int pNumber)
	{
		return pNumber switch
		{
			1 => GameSimulationState.EquipmentSlot.Weapon,
			2 => GameSimulationState.EquipmentSlot.Shield,
			3 => GameSimulationState.EquipmentSlot.Armor,
			4 => GameSimulationState.EquipmentSlot.Helmet,
			5 => GameSimulationState.EquipmentSlot.Accessory,
			6 => GameSimulationState.EquipmentSlot.All,
			_ => GameSimulationState.EquipmentSlot.All,
		};
	}

	/// <summary>
	/// Runs 11560, Play Movie, from liblcf's <c>Code::PlayMovie</c> and
	/// EasyRPG's <c>Game_Interpreter_Map::CommandPlayMovie</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Five parameters and a string.</strong> The reference reads
	/// <c>ToString(com.string)</c> for the file name and
	/// <c>parameters[0..4]</c> for the rest. <strong>The name is not a
	/// parameter</strong>, and the first parameter is the mode that governs both
	/// positions — the same shape <c>10910</c> has.
	/// </para>
	/// <para>
	/// <strong>This stores the request and does not claim to play it.</strong>
	/// The reference's own body is explicit:
	/// <c>Output::Warning("Couldn't play movie: {}. Movie playback is not
	/// implemented (yet).", filename)</c> — and then it stores the request and
	/// returns true, which advances the page. <strong>A reader that refused the
	/// command would have stalled a game's event</strong> on a cutscene it
	/// cannot show, and a reader that reported success would have claimed a
	/// capability the reference does not have.
	/// </para>
	/// </remarks>
	private void ExecutePlayMovie(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 5.
		if (pCmd.Parameters.Count < 5)
		{
			Malformed("Play movie");
			return;
		}

		// **Der Name steht im String und nicht in den Parametern.**
		var file = pCmd.Text ?? "";

		// **Derselbe Modus fuer beide Positionen.**
		var posX = ValueOrVariable(pCmd.Parameters[0], pCmd.Parameters[1]);
		var posY = ValueOrVariable(pCmd.Parameters[0], pCmd.Parameters[2]);
		var resX = pCmd.Parameters[3];
		var resY = pCmd.Parameters[4];

		_state.MovieFileName = file;
		_state.MoviePosX = posX;
		_state.MoviePosY = posY;
		_state.MovieResX = resX;
		_state.MovieResY = resY;
		_state.IsMoviePending = true;
		_state.AddDiagnostic(
			$"[Event {_eventId}] Play movie: \"{file}\" at ({posX}, {posY}) "
			+ $"sized {resX} by {resY} is requested and nothing is playing it, "
			+ "which is what the reference does too — its own warning says "
			+ "movie playback is not implemented yet");
	}

	/// <summary>
	/// Runs 10740, Enter Hero Name, from liblcf's <c>Code::EnterHeroName</c> and
	/// EasyRPG's <c>Game_Interpreter_Map::CommandEnterHeroName</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Three parameters, and the third is a flag.</strong> The
	/// reference reads <c>actor_id</c>, <c>charset</c> and
	/// <c>use_default_name</c> and builds a name scene from all three.
	/// <strong>A reader that treated the third as text would have shown every
	/// hero a name ending in a digit.</strong>
	/// </para>
	/// <para>
	/// <strong>The command itself writes nothing.</strong> The reference hands
	/// the work to a scene and the scene writes the name when the player is
	/// done — <strong>so a reader that stored the name here would have renamed
	/// a hero with no prompt at all</strong>, which is a different game and a
	/// worse one. What this command does is name the hero, say who it is, and
	/// leave the name to whoever owns the screen.
	/// </para>
	/// </remarks>
	private void ExecuteEnterHeroName(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 3.
		if (pCmd.Parameters.Count < 3)
		{
			Malformed("Enter hero name");
			return;
		}

		var actorId = pCmd.Parameters[0];
		var charset = pCmd.Parameters[1];
		var useDefaultName = pCmd.Parameters[2] != 0;

		if (_heroNameEntry == null)
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Enter hero name: no name screen is "
				+ $"attached, so hero {actorId} keeps the name it has");
			return;
		}

		// **Die Referenz warnt und laeuft weiter** -- und nicht umgekehrt.
		// **Und sie schaut nur nach: `GetActor` gibt null fuer einen Helden,
		// den es nicht gibt, und legt keinen an.** Ein Leser, der den Eintrag
		// erzeugt haette, wuerde einem Spiel, das Held 99 benennt, Held 99
		// erschaffen -- **und der Held waere danach fuer jeden Befehl da,
		// in der Parteifenster und in der Speicherdatei.**
		if (_state.FindActorValues(actorId) == null)
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Enter hero name: the reference warns on an "
				+ $"invalid actor ID {actorId} and moves on, and there is no "
				+ "hero by that name; nothing was changed");
			return;
		}

		_heroNameEntry(actorId, charset, useDefaultName);
		_state.AddDiagnostic(
			$"[Event {_eventId}] Enter hero name: the name screen is up for "
			+ $"hero {actorId} with the face {charset} and "
			+ (useDefaultName
				? "the database name offered"
				: "the database name withheld"));
	}

	/// <summary>
	/// Runs 10870, Trade Event Locations, from liblcf's
	/// <c>Code::TradeEventLocations</c> and EasyRPG's
	/// <c>Game_Interpreter::CommandTradeEventLocations</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Width 2, and the two parameters are the two figures.</strong> The
	/// reference reads both through the same patch-guarded helper
	/// <c>11320</c> uses, and then <em>swaps</em> their three coordinates.
	/// <strong>A reader that moved one onto the other would have collapsed two
	/// events onto one tile</strong>, and a game whose "the guard takes your
	/// place" cutscene would have had both guards stand still.
	/// </para>
	/// <para>
	/// <strong>And all six coordinates are read before any is written.</strong>
	/// The reference copies m1, x1, y1 and then m2, x2, y2, and only then
	/// calls the two <c>MoveTo</c> — <strong>so a figure that does not resolve
	/// leaves the other where it stood</strong>, because the whole exchange is
	/// behind one <c>if</c>.
	/// </para>
	/// </remarks>
	private void ExecuteTradeEventLocations(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 2.
		if (pCmd.Parameters.Count < 2)
		{
			Malformed("Trade event locations");
			return;
		}

		if (_eventPlaceReader == null || _eventPlaceMover == null)
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Trade event locations: this interpreter "
				+ "cannot reach a figure, so nothing was traded");
			return;
		}

		// **Ohne den Patch sind die beiden Parameter die Figuren selbst.**
		var id1 = pCmd.Parameters[0];
		var id2 = pCmd.Parameters[1];

		// **Erst lesen, dann schreiben** -- alle vier Koordinaten, bevor
		// irgendetwas bewegt wird.
		if (!_eventPlaceReader(id1, out var m1, out var x1, out var y1))
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Trade event locations: no figure carries "
				+ $"the id {id1}, and the reference swaps nothing");
			return;
		}

		if (!_eventPlaceReader(id2, out var m2, out var x2, out var y2))
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Trade event locations: no figure carries "
				+ $"the id {id2}, and the reference swaps nothing — not even "
				+ "the first figure's half of the trade");
			return;
		}

		_eventPlaceMover(id1, m2, x2, y2);
		_eventPlaceMover(id2, m1, x1, y1);
		_state.AddDiagnostic(
			$"[Event {_eventId}] Trade event locations: {id1} -> map {m2} at "
			+ $"({x2}, {y2}) and {id2} -> map {m1} at ({x1}, {y1})");
	}

	/// <summary>
	/// Runs 10830, Recall to Location, from liblcf's
	/// <c>Code::RecallToLocation</c> and EasyRPG's
	/// <c>Game_Interpreter_Map::CommandRecallToLocation</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Three parameters, and all three are variable ids and not
	/// values.</strong> The reference reads all three through
	/// <c>game_variables-&gt;Get()</c> and teleports to what they hold. <strong>A
	/// reader that read them as coordinates would have sent a game to the map
	/// whose number the editor happened to write</strong> — and a 2K game's
	/// "recall" would have gone to map 1, tile 1, which is a real place on
	/// every map.
	/// </para>
	/// <para>
	/// <strong>And the facing is minus one.</strong> The reference writes
	/// <c>ReserveTeleport(map_id, x, y, -1, tt)</c>, and that -1 is its own
	/// "keep the direction the hero had" — <strong>a reader that copied
	/// <c>10810</c>'s default of the current facing would have made a game's
	/// recall turn the hero</strong>, and 10810's default is the facing the
	/// player has, which the recall has no reason to know.
	/// </para>
	/// </remarks>
	private void ExecuteRecallToLocation(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 3.
		if (pCmd.Parameters.Count < 3)
		{
			Malformed("Recall to location");
			return;
		}

		// **Alle drei sind Variablen-Referenzen.**
		var mapId = GetVariable(pCmd.Parameters[0]);
		var x = GetVariable(pCmd.Parameters[1]);
		var y = GetVariable(pCmd.Parameters[2]);

		if (mapId < 1 || mapId > GameSimulationState.MaxMapId || x < 0 || y < 0)
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Recall to location: the variables hold "
				+ $"({mapId}, {x}, {y}), and that is not a place");
			return;
		}

		// **Die Blickrichtung bleibt, weil die Referenz -1 schreibt.**
		_state.PendingMapId = mapId;
		_state.PendingX = x;
		_state.PendingY = y;
		_state.PendingFacing = -1;
		_state.IsTransferPending = true;
		_state.AddDiagnostic(
			$"[Event {_eventId}] Recall pending -> map {mapId} at ({x}, "
			+ $"{y}) from the variables {pCmd.Parameters[0]}, "
			+ $"{pCmd.Parameters[1]} and {pCmd.Parameters[2]}");
	}

	/// <summary>
	/// Runs 10910, Store Terrain ID, from liblcf's
	/// <c>Code::StoreTerrainID</c> and EasyRPG's
	/// <c>Game_Interpreter::CommandStoreTerrainID</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Width 4, and the first parameter is the mode for the first two
	/// coordinates and not for the third.</strong> The reference writes
	/// <c>ValueOrVariable(parameters[0], parameters[1])</c> for x and
	/// <c>ValueOrVariable(parameters[0], parameters[2])</c> for y — <strong>so
	/// the same mode byte governs both</strong> — and the fourth is the
	/// variable to write into. A reader that gave each coordinate its own mode
	/// would have read a game's y from a constant while its x came from a
	/// variable.
	/// </para>
	/// <para>
	/// <strong>And the reference's own comment says <c>code 10820</c>.</strong>
	/// The dispatch line and liblcf both say <c>10910</c> — <strong>a reader
	/// that believed the comment would have implemented 10820</strong>, which is
	/// a different command with different parameters.
	/// </para>
	/// </remarks>
	private void ExecuteStoreTerrainId(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 4.
		if (pCmd.Parameters.Count < 4)
		{
			Malformed("Store terrain ID");
			return;
		}

		// **Derselbe Modus fuer beide Koordinaten.**
		var x = ValueOrVariable(pCmd.Parameters[0], pCmd.Parameters[1]);
		var y = ValueOrVariable(pCmd.Parameters[0], pCmd.Parameters[2]);
		var varId = pCmd.Parameters[3];

		if (varId < 1 || varId > GameSimulationState.MaxVariables)
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Store terrain ID: variable {varId} is "
				+ "out of range, and the reference writes nothing");
			return;
		}

		_state.Variables[varId - 1] = _state.TerrainTagAt(x, y);
		_state.AddDiagnostic(
			$"[Event {_eventId}] Store terrain ID: tile ({x}, {y}) has terrain "
			+ $"{_state.Variables[varId - 1]} and it is now in variable {varId}");
	}

	/// <summary>
	/// Runs 11320, Flash Sprite, from liblcf's <c>Code::FlashSprite</c> and
	/// EasyRPG's <c>Game_Interpreter_Map::CommandFlashSprite</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Seven parameters, and the seventh is a mode byte and not a
	/// value.</strong> The reference reads every channel through
	/// <c>ValueOrVariableBitfield(com, 7, shift, val_idx)</c> — <strong>and
	/// without the Maniac patch that helper returns
	/// <c>parameters[val_idx]</c> and nothing else.</strong> <strong>A reader
	/// that always applied the bitfield would have shifted a game's red
	/// channel by one for a colour the file wrote plainly</strong>, and
	/// RPG_Maker 2000 games have no mode byte at all.
	/// </para>
	/// <para>
	/// <strong>And a duration of zero still waits one frame.</strong> The
	/// reference's own <c>SetupWait</c> has a separate arm for zero — so a
	/// game's "flash for no time" holds its page for a frame, and a reader
	/// that passed zero to the frame budget would have advanced immediately.
	/// </para>
	/// <para>
	/// <strong>And a character that does not resolve is a warning, not a
	/// refusal.</strong> The reference's <c>GetCharacter</c> returns null, the
	/// whole flash is skipped, and the command still returns true — so the
	/// page moves on.
	/// </para>
	/// </remarks>
	private bool ExecuteFlashSprite(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 7.
		if (pCmd.Parameters.Count < 7)
		{
			Malformed("Flash sprite");
			return Advance();
		}

		// **Ohne den Maniac-Patch liest die Referenz den ersten Parameter
		// direkt** -- ihr `ValueOrVariableBitfield` gibt in diesem Fall
		// `com.parameters[val_idx]` zurueck und sonst nichts. **Eine Maskierung
		/// waere hier eine Erfindung**, und eine Figur 99 waere als 3
		// angekommen.
		var eventId = pCmd.Parameters[0];
		var r = pCmd.Parameters[1];
		var g = pCmd.Parameters[2];
		var b = pCmd.Parameters[3];
		var staerke = pCmd.Parameters[4];
		var zehntel = pCmd.Parameters[5];
		var warten = pCmd.Parameters[6] > 0;

		if (_spriteFlasher == null)
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Flash sprite: this interpreter cannot reach"
				+ $" a character, so nothing flashed on {eventId}");
			return Advance();
		}

		// **Die Dauer wird selbst umgerechnet, und der Hook bekommt Frames.**
		var frames = TenthsToFrames(zehntel);
		if (!_spriteFlasher(eventId, r, g, b, staerke, frames, false))
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Flash sprite: no character carries the id "
				+ $"{eventId}, and the reference skips the whole flash and "
				+ "still returns true");
			return Advance();
		}

		if (warten)
		{
			// **Und eine Dauer von null wartet ein Frame** -- die
			// SetupWait-Regel des Referenzcodes, nicht die Frame-Budget-
			// Klammer, denn die wuerde hier eine abweichende Zahl geben.
			WaitForFrames(frames == 0 ? 1 : frames);
		}

		_state.AddDiagnostic(
			$"[Event {_eventId}] Flash sprite on {eventId}: rgba({r}, {g}, "
			+ $"{b}, {staerke}) for {frames} frames"
			+ (warten ? ", and the page waits" : ""));
		return Advance();
	}

	/// <summary>
	/// Runs 13310, Conditional Branch (the battle form), from liblcf's
	/// <c>Code::ConditionalBranch_B</c> and EasyRPG's
	/// <c>Game_Interpreter_Battle::CommandConditionalBranchBattle</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Width 5, six modes, and the switch comparison is
	/// inverted.</strong> The reference writes
	/// <c>Get(parameters[1]) == (parameters[2] == 0)</c>, so the third
	/// parameter asks "is it off" — <strong>and a reader that read it as "is it
	/// on" would have run a game's "if the switch is off" branch when the
	/// switch was on.</strong>
	/// </para>
	/// <para>
	/// <strong>And the last two modes are 2003-only, guarded inside their
	/// case.</strong> The reference's fourth mode reads
	/// <c>IsRPG2k3Commands() &amp;&amp; targets_single_enemy &amp;&amp;
	/// target_enemy_index == parameters[1]</c> and its fifth
	/// <c>IsRPG2k3Commands() &amp;&amp; current_actor_id ==
	/// parameters[1]</c> — <strong>so a 2K game's fourth and fifth modes are
	/// always false</strong>, and a reader that evaluated them anyway would
	/// have taken a branch with an enemy's number in a file that never
	/// carried one.
	/// </para>
	/// <para>
	/// <strong>An id that is not there is a warning and a false</strong> — the
	/// reference leaves <c>result</c> at the false it was initialised to, so
	/// the else branch runs.
	/// </para>
	/// </remarks>
	private bool ExecuteConditionalBranchBattle(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 5.
		if (pCmd.Parameters.Count < 5)
		{
			Malformed("Conditional branch, battle");
			return true;
		}

		var ergebnis = false;
		switch (pCmd.Parameters[0])
		{
			case 0:
				// **Die Formel ist eine Gleichheit mit einem Boolean, und
				// der dritte Parameter 0 bedeutet "der Schalter ist AN".**
				// Die Referenz schreibt `Get(id) == (parameters[2] == 0)` --
				// `0 == 0` ist `true`, also vergleicht sie den Schalter mit
				// `true`. **Ein Leser, der den Parameter als "ist er aus"
				// gelesen haette, haette jeden Schalter umgedreht.**
				ergebnis = GetSwitch(pCmd.Parameters[1])
					== (pCmd.Parameters[2] == 0);
				break;
			case 1:
				{
					var wert1 = GetVariable(pCmd.Parameters[1]);
					var wert2 = pCmd.Parameters[2] == 0
						? pCmd.Parameters[3]
						: GetVariable(pCmd.Parameters[3]);
					// **Sechs Vergleichsarten, und die Referenz bricht nach dem
					// sechsten aus dem Switch heraus -- ein siebter Modus
					// laesst das Ergebnis falsch.**
					switch (pCmd.Parameters[4])
					{
						case 0:
							ergebnis = wert1 == wert2;
							break;
						case 1:
							ergebnis = wert1 >= wert2;
							break;
						case 2:
							ergebnis = wert1 <= wert2;
							break;
						case 3:
							ergebnis = wert1 > wert2;
							break;
						case 4:
							ergebnis = wert1 < wert2;
							break;
						case 5:
							ergebnis = wert1 != wert2;
							break;
					}

					break;
				}

			case 2:
				{
					var held = pCmd.Parameters[1];
					if (held < 1 || held > GameSimulationState.MaxActorId)
					{
						_state.AddDiagnostic(
							$"[Event {_eventId}] Conditional branch, battle: "
							+ $"invalid actor ID {held}, and the reference "
							+ "warns and leaves the result false");
					}
					else
					{
						ergebnis = _state.CanHeroAct(held);
					}

					break;
				}

			case 3:
				{
					var monster = pCmd.Parameters[1];
					if (monster < 0
						|| monster >= _state.TroopMembers.Count)
					{
						_state.AddDiagnostic(
							$"[Event {_eventId}] Conditional branch, battle: "
							+ $"invalid enemy ID {monster}, and the reference "
							+ "warns and leaves the result false");
					}
					else
					{
						ergebnis = _state.CanMonsterAct(monster);
					}

					break;
				}

			case 4:
				// **Der Zielvergleich ist zweifach: erst der 2003-Schalter,
				// dann derEinzelflag-Vergleich.** Ein Leser, der nur den
				// Index verglich, haette einen Kampf mit mehreren Zielen
				// getroffen, in dem gar keines das einzelne ist.
				ergebnis = _state.TargetsSingleEnemy
					&& _state.CurrentTargetIndex == pCmd.Parameters[1];
				break;
			case 5:
				// **Der Held, der gerade handelt, und seine letzte
				// Befehlsnummer** -- die Referenz vergleicht das Ergebnis
				// seines Kampf Befehls, und ein Spiel, das es nicht gesetzt
				// hat, vergleicht gegen nichts.
				ergebnis = _state.CurrentActorId == pCmd.Parameters[1]
					&& _state.LastBattleAction == pCmd.Parameters[2];
				break;
			default:
				_state.AddDiagnostic(
					$"[Event {_eventId}] Conditional branch, battle: mode "
					+ $"{pCmd.Parameters[0]} is not 0 to 5, and the reference's "
					+ "switch leaves the result false");
				return true;
		}

		_state.LastBattleBranch = ergebnis;
		_state.AddDiagnostic(
			$"[Event {_eventId}] Conditional branch, battle: mode "
			+ $"{pCmd.Parameters[0]} is {(ergebnis ? "true" : "false")}");

		if (ergebnis)
		{
			return Advance();
		}

		// **Index setzen UND springen** -- das sind zwei Schritte, und die
		// Referenz macht beide: `SetSubcommandIndex` und dann
		// `SkipToNextConditional({ElseBranch_B, EndBranch_B})`.
		//
		// **Ein Leser, der nur den Index schriebe, wuerde den then-Block
		// ausfuehren** -- genau den Block, den der Zweig ueberspringen soll.
		// Und einer, der nur springe, wuerde den else-Zweig nicht nehmen,
		// weil sein Befehl den Index nicht findet.
		_state.SubcommandIndex = SubIdxBranchBattleElse;
		var uebersprungen3 = SkipToOneOf(
			new[] { ElseBranchBattle, EndBranchBattle }, "battle branch");
		_state.AddDiagnostic(
			$"[Event {_eventId}] Conditional branch, battle: false, so the "
			+ $"then block was skipped ({uebersprungen3} commands)");
		return Advance();
	}

	/// <summary>
	/// Runs 11210 and 13260, Show Battle Animation, from liblcf's
	/// <c>Code::ShowBattleAnimation</c> and
	/// <c>Code::ShowBattleAnimation_B</c> and EasyRPG's
	/// <c>Game_Interpreter_Battle::CommandShowBattleAnimation</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Three parameters, or four, and the fourth is a 2003 form.</strong>
	/// The reference reads it only
	/// <c>if (Player::IsRPG2k3() &amp;&amp; com.parameters.size() &gt; 3)</c>, so
	/// this takes three and reads the fourth only when there is one.
	/// </para>
	/// <para>
	/// <strong>Allies count from one and enemies from zero.</strong> The
	/// reference subtracts one from a party target and not from a monster
	/// target — so a target of zero is the first enemy and the zeroth ally,
	/// which does not exist. <strong>A reader that used one numbering for both
	/// would have played a game's first hero's animation on its second
	/// hero.</strong>
	/// </para>
	/// <para>
	/// <strong>And a negative target is the whole side, with the flag
	/// deciding which.</strong> The reference collects the party or the enemy
	/// party accordingly, so a target of -1 without the flag is every
	/// <em>enemy</em>.
	/// </para>
	/// <para>
	/// <strong>And the wait is the animation's own length.</strong> The
	/// reference writes <c>_state.wait_time = frames</c> and the frames come
	/// from the animation's timing rows, so a game that wrote "wait" waits
	/// as long as its animation and not for a constant.
	/// </para>
	/// </remarks>
	private void ExecuteShowBattleAnimation(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 3.
		if (pCmd.Parameters.Count < 3)
		{
			Malformed("Show battle animation");
			return;
		}

		var animationId = pCmd.Parameters[0];
		var ziel = pCmd.Parameters[1];
		var warten = pCmd.Parameters[2] != 0;
		// **The fourth parameter is read only when there is one**, and the
		// reference guards it with the engine generation as well — so a 2K
		// game with a fourth value would have it read here, and a 2003 game
		// without one would not. The generation is not visible to this
		// reader, so the count is what decides.
		var aufVerbündete = pCmd.Parameters.Count > 3
			&& pCmd.Parameters[3] != 0;

		var frames = _state.BattleAnimationFrameCount(animationId);
		if (frames == 0)
		{
			// **The reference's GetElement returns nothing, it warns, and it
			// returns zero frames** — so a mistyped animation id plays
			// nothing and waits for nothing.
			_state.AddDiagnostic(
				$"[Event {_eventId}] Show battle animation: animation "
				+ $"{animationId} is not in the table, and the reference's "
				+ "GetElement returns zero frames for it");
			return;
		}

		_state.BattleAnimationId = animationId;
		_state.BattleAnimationOnAllies = aufVerbündete;
		_state.BattleAnimationOnAllTargets = ziel < 0;
		// **A party target is one-based and an enemy target is not**, and the
		// subtraction happens before the range check.
		_state.BattleAnimationTarget = aufVerbündete ? ziel - 1 : ziel;
		_state.BattleAnimationFrames = frames;
		_state.BattleAnimationWaitRequested = warten;

		if (warten)
		{
			// **The wait is the animation's own length**, through the
			// interpreter's frame budget and not the state.
			WaitForFrames(frames);
		}

		_state.AddDiagnostic(
			$"[Event {_eventId}] Show battle animation {animationId}: "
			+ (ziel < 0
				? aufVerbündete ? "the whole party" : "the whole enemy party"
				: aufVerbündete
					? $"ally {_state.BattleAnimationTarget}"
					: $"enemy {_state.BattleAnimationTarget}")
			+ $", {frames} frames"
			+ (warten ? ", and the page waits" : ""));
	}

	/// <summary>
	/// Runs 13110, Change Monster HP, from liblcf's
	/// <c>Code::ChangeMonsterHP</c> and EasyRPG's
	/// <c>Game_Interpreter_Battle::CommandChangeMonsterHP</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Three change modes, and the third is a share of the monster's
	/// own maximum.</strong> The reference's switch is 0 a constant, 1 a
	/// variable and 2 a percentage — so mode 2 on a monster with 500 of 1000
	/// hit points takes 250, and not 2. <strong>A reader that read mode 2 as
	/// another constant would have healed a wounded boss for one hit
	/// point</strong> where the game asked for a tenth of his life.
	/// </para>
	/// <para>
	/// <strong>The sign is a flag and not the value's own sign.</strong> The
	/// reference reads <c>bool lose = com.parameters[1] &gt; 0</c> and then
	/// writes <c>change = -change</c> — so a game that wrote a negative number
	/// with the flag at zero still heals, and a game that wrote a positive one
	/// with the flag at one still hurts. The value's own sign is read never.
	/// </para>
	/// <para>
	/// <strong>And the death is a timer and not a removal.</strong> A monster
	/// at zero hit points gets the system's enemy-kill sound and a death
	/// timer — a reader that dropped him from the troop at once would have had
	/// him vanish in the middle of the frame, and the animation the timer
	/// exists for would have had nothing to play on.
	/// </para>
	/// </remarks>
	private void ExecuteChangeMonsterHp(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 5.
		if (pCmd.Parameters.Count < 5)
		{
			Malformed("Change monster HP");
			return;
		}

		var index = pCmd.Parameters[0];
		if (index < 0 || index >= _state.TroopMembers.Count)
		{
			_state.AddDiagnostic(
				$"[Battle] Change monster HP: invalid enemy ID {index}, and the "
				+ "reference warns and returns");
			return;
		}

		var verlieren = pCmd.Parameters[1] > 0;
		var wert = 0;
		switch (pCmd.Parameters[2])
		{
			case 0:
				wert = pCmd.Parameters[3];
				break;
			case 1:
				wert = GetVariable(pCmd.Parameters[3]);
				break;
			case 2:
				wert = pCmd.Parameters[3] * _state.MonsterMaxHp(index) / 100;
				break;
			default:
				_state.AddDiagnostic(
					$"[Battle] Change monster HP: mode {pCmd.Parameters[2]} is "
					+ "not 0, 1 or 2, and the reference's switch changes nothing");
				return;
		}

		if (verlieren)
		{
			wert = -wert;
		}

		_state.SetMonsterHp(index, _state.MonsterHp(index) + wert);
		if (_state.IsMonsterDead(index))
		{
			// **The death is timed, and this is where the reference plays the
			// system's enemy-kill sound.**
			_state.SetMonsterExit(
				index, GameSimulationState.MonsterExit.Timed);
		}

		_state.AddDiagnostic(
			$"[Battle] Change monster HP: enemy {index} now at "
			+ $"{_state.MonsterHp(index)} of {_state.MonsterMaxHp(index)}");
	}

	/// <summary>
	/// Runs 13120, Change Monster MP, from liblcf's
	/// <c>Code::ChangeMonsterMP</c> and EasyRPG's
	/// <c>Game_Interpreter_Battle::CommandChangeMonsterMP</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Two change modes, and there is no third.</strong> The
	/// reference's switch is 0 a constant and 1 a variable — so a mode of 2
	/// changes nothing, and this command has no percentage mode where
	/// <c>13110</c> has one. A reader that reused the hit-point command's
	/// three modes would have healed a monster's mana for a share of a
	/// maximum that is never read here.
	/// </para>
	/// <para>
	/// <strong>The sign is a flag here too,</strong> and the reference's own
	/// <c>sp += change; SetSp(sp)</c> is where a game that drains more than a
	/// monster has ends up with a negative number — so this reader clamps at
	/// zero and the difference is recorded rather than copied.
	/// </para>
	/// </remarks>
	private void ExecuteChangeMonsterMp(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 5.
		if (pCmd.Parameters.Count < 5)
		{
			Malformed("Change monster MP");
			return;
		}

		var index = pCmd.Parameters[0];
		if (index < 0 || index >= _state.TroopMembers.Count)
		{
			_state.AddDiagnostic(
				$"[Battle] Change monster MP: invalid enemy ID {index}, and the "
				+ "reference warns and returns");
			return;
		}

		var verlieren = pCmd.Parameters[1] > 0;
		var wert = 0;
		switch (pCmd.Parameters[2])
		{
			case 0:
				wert = pCmd.Parameters[3];
				break;
			case 1:
				wert = GetVariable(pCmd.Parameters[3]);
				break;
			default:
				_state.AddDiagnostic(
					$"[Battle] Change monster MP: mode {pCmd.Parameters[2]} is "
					+ "not 0 or 1, and the reference's switch changes nothing");
				return;
		}

		if (verlieren)
		{
			wert = -wert;
		}

		_state.SetMonsterSp(index, _state.MonsterSp(index) + wert);
		_state.AddDiagnostic(
			$"[Battle] Change monster MP: enemy {index} now at "
			+ $"{_state.MonsterSp(index)}");
	}

	/// <summary>
	/// Runs 13130, Change Monster Condition, from liblcf's
	/// <c>Code::ChangeMonsterCondition</c> and EasyRPG's
	/// <c>Game_Interpreter_Battle::CommandChangeMonsterCondition</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Removing the death condition is a death, and it is
	/// immediate.</strong> The reference calls
	/// <c>RemoveState(state_id, false)</c> and its own comment says the monster
	/// disappears and does not animate death — written down as an RPG_RT bug
	/// that it reproduces. <strong>So the two death paths differ:</strong> a
	/// monster whose hit points reach zero gets a death timer, and one whose
	/// death condition is removed vanishes at once.
	/// </para>
	/// <para>
	/// <strong>And the flag is remove-or-add and not add-or-remove.</strong> The
	/// reference reads <c>bool remove = com.parameters[1] &gt; 0</c> — so the
	/// second parameter's truth means removal, and a reader that read it as
	/// "add" would have healed a poisoned monster with the command meant to
	/// cure him.
	/// </para>
	/// </remarks>
	private void ExecuteChangeMonsterCondition(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 3.
		if (pCmd.Parameters.Count < 3)
		{
			Malformed("Change monster condition");
			return;
		}

		var index = pCmd.Parameters[0];
		if (index < 0 || index >= _state.TroopMembers.Count)
		{
			_state.AddDiagnostic(
				$"[Battle] Change monster condition: invalid enemy ID "
				+ $"{index}, and the reference warns and returns");
			return;
		}

		var entfernen = pCmd.Parameters[1] > 0;
		var zustand = pCmd.Parameters[2];
		if (entfernen)
		{
			_state.RemoveMonsterCondition(index, zustand);
		}
		else
		{
			_state.AddMonsterCondition(index, zustand);
		}

		_state.AddDiagnostic(
			$"[Battle] Change monster condition: enemy {index} "
			+ (entfernen ? "loses" : "gains") + $" condition {zustand}");
	}

	/// <summary>
	/// Runs 13150, Show Hidden Monster, from liblcf's
	/// <c>Code::ShowHiddenMonster</c> and EasyRPG's
	/// <c>Game_Interpreter_Battle::CommandShowHiddenMonster</c>.
	/// </summary>
	/// <remarks>
	/// <strong>One parameter and no second arm.</strong> The reference's whole
	/// command is <c>enemy-&gt;SetHidden(false)</c> — <strong>so a monster is
	/// hidden in its database row and this is the only command that shows
	/// it</strong>, and a reader that added a "hide" direction would have given
	/// a game a command the reference does not have.
	/// </remarks>
	private void ExecuteShowHiddenMonster(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 1.
		if (pCmd.Parameters.Count < 1)
		{
			Malformed("Show hidden monster");
			return;
		}

		var index = pCmd.Parameters[0];
		if (index < 0 || index >= _state.TroopMembers.Count)
		{
			_state.AddDiagnostic(
				$"[Battle] Show hidden monster: invalid enemy ID {index}, and "
				+ "the reference warns and returns");
			return;
		}

		_state.ShowMonster(index);
		_state.AddDiagnostic($"[Battle] Show hidden monster: enemy {index}");
	}

	/// <summary>
	/// Runs 13210, Change Battle BG, from liblcf's
	/// <c>Code::ChangeBattleBG</c> and EasyRPG's
	/// <c>Game_Interpreter_Battle::CommandChangeBattleBG</c>.
	/// </summary>
	/// <remarks>
	/// <strong>The file name is in the command's text, and the one parameter
	/// is not it.</strong> The reference writes
	/// <c>Game_Battle::ChangeBackground(ToString(com.string))</c> — and a
	/// reader that looked in <c>parameters</c> would have found a single zero
	/// and left every battle with the background the troop file named.
	/// </remarks>
	private void ExecuteChangeBattleBg(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 1.
		if (pCmd.Parameters.Count < 1)
		{
			Malformed("Change battle BG");
			return;
		}

		_state.BattleBackground = pCmd.Text ?? "";
		_state.AddDiagnostic(
			$"[Battle] Change battle BG: '{_state.BattleBackground}'");
	}

	/// <summary>
	/// Runs 11060, Pan Screen, from liblcf's <c>Code::PanScreen</c> and
	/// EasyRPG's <c>Game_Interpreter_Map::CommandPanScreen</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Four modes, and only the middle two move anything.</strong> The
	/// reference's switch is 0 lock, 1 unlock, 2 pan and 3 reset — **and a value
	/// it does not know falls through all four and does nothing at all**, with
	/// no diagnostic and no refusal. A reader that defaulted to the pan would
	/// have a game's mistyped mode scrolling the screen instead of doing
	/// nothing, and a game that uses lock to hold the camera during a cutscene
	/// would have the camera move instead of holding.
	/// </para>
	/// <para>
	/// <strong>The speed is clamped to 1 to 6 and not refused.</strong> The
	/// reference writes <c>Utils::Clamp&lt;int&gt;(com.parameters[3], 1, 6)</c> —
	/// so a game that wrote a zero or a nine gets the nearest speed and the pan
	/// still runs, and a reader that refused would have stopped the event on a
	/// number the engine repairs.
	/// </para>
	/// <para>
	/// <strong>The wait is the rounded distance over the speed.</strong> The
	/// reference's <c>GetPanWait</c> is
	/// <c>distance / speed + (distance % speed != 0)</c> — rounded up, so a pan
	/// of five tiles at speed 3 waits two frames and not one, and a pan that
	/// would wait zero never ends the frame it started in.
	/// </para>
	/// <para>
	/// <strong>And the reference's own comment says the wait takes the maximum
	/// over all pending pans, not this one.</strong> That is a statement about
	/// the engine's behaviour that a reader with one pan has nothing to
	/// disagree with, and it is recorded here because the alternative — a
	/// reader that waited for its own pan only — is the shape the code has.
	/// </para>
	/// </remarks>
	private void ExecutePanScreen(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 5.
		if (pCmd.Parameters.Count < 5)
		{
			Malformed("Pan screen");
			return;
		}
		var mode = pCmd.Parameters[0];
		// **The speed, clamped the reference's way** — and read for the two
		// modes that use it, because lock and unlock have none.
		var speed = Math.Clamp(pCmd.Parameters[3], 1, 6);
		var waiting = pCmd.Parameters[4] != 0;

		switch (mode)
		{
			case 0:
				_state.IsPanLocked = true;
				_state.PanLastMode = GameSimulationState.PanMode.Lock;
				// **A lock does not stop a pan that is already running** — the
				// reference calls `LockPan()` and nothing else, and a reader
				// that halted the pan would freeze a camera mid-scroll.
				break;
			case 1:
				_state.IsPanLocked = false;
				_state.PanLastMode = GameSimulationState.PanMode.Unlock;
				break;
			case 2:
				{
					var direction = DirectionOf(pCmd.Parameters[1]);
					var distance = pCmd.Parameters[2];
					_state.PanLastMode = GameSimulationState.PanMode.Pan;
					_state.PanLastDirection = direction;
					_state.PanLastDistance = distance;
					_state.PanSpeed = speed;
					_state.PanTargetX = distance * StepOf(direction, 0);
					_state.PanTargetY = distance * StepOf(direction, 1);
					_state.PanFramesLeft = FramesFor(distance, speed);
					_state.IsPanActive = true;
					break;
				}
			case 3:
				_state.PanLastMode = GameSimulationState.PanMode.Reset;
				_state.PanSpeed = speed;
				_state.PanFramesLeft = FramesFor(
					Math.Max(Math.Abs(_state.PanTargetX), Math.Abs(_state.PanTargetY)),
					speed);
				_state.PanTargetX = 0;
				_state.PanTargetY = 0;
				_state.IsPanActive = true;
				break;
			default:
				// **An unknown mode does nothing at all, and says so.** The
				// reference falls through its switch with no default arm, so a
				// game's value of 4 is a no-op — and a reader that refused
				// would have stopped an event the engine walks past.
				_state.AddDiagnostic(
					$"[Event {_eventId}] Pan screen: mode {mode} is not 0, 1, 2"
					+ " or 3, and the reference's switch does nothing for it");
				return;
		}

		if (waiting && _state.PanFramesLeft > 0)
		{
			// **The wait is this interpreter's own frame budget**, which is the
			// shape the reference uses for every other timed command — and it
			// goes through `WaitForFrames`, which clamps to at least one frame
			// and to the interpreter's maximum. A pan that was asked to wait
			// and does not would have a cutscene run its next lines while the
			// camera is still sliding, and an unclamped zero would have made
			// `11340`'s "wait for the movement" end in the same frame it was
			// asked for.
			WaitForFrames(_state.PanFramesLeft);
		}
		_state.AddDiagnostic(
			$"[Event {_eventId}] Pan screen: {_state.PanLastMode}"
			+ (_state.PanLastMode == GameSimulationState.PanMode.Pan
				? $" {_state.PanLastDirection} by {_state.PanLastDistance}"
					+ $" at speed {_state.PanSpeed}"
				: string.Empty)
			+ (waiting ? ", and the page waits" : string.Empty));
	}

	/// <summary>
	/// The frames a pan of a distance at a speed takes, the reference's way.
	/// </summary>
	/// <remarks>
	/// <strong>Distance over speed, rounded up.</strong> The reference writes
	/// <c>distance / speed + (distance % speed != 0)</c> in <c>GetPanWait</c> —
	/// and the rounding is what stops a pan that divides evenly from being one
	/// frame short, and what stops one that does not from ending the frame it
	/// started in.
	/// </remarks>
	private static int FramesFor(int pDistance, int pSpeed)
	{
		var speed = Math.Max(1, pSpeed);
		return pDistance / speed + (pDistance % speed != 0 ? 1 : 0);
	}

	/// <summary>
	/// The step a pan direction takes on each axis, in tiles.
	/// </summary>
	/// <remarks>
	/// <strong>A diagonal is one on both axes and not two.</strong> A reader that
	/// used a compass distance would have a diagonal pan covering twice the
	/// ground a cardinal one does over the same number, and a game's cutscene
	/// that pans to a corner would end past the corner.
	/// </remarks>
	private static int StepOf(GameSimulationState.PanDirection pDirection, int pAxis)
	{
		var x = pDirection switch
		{
			GameSimulationState.PanDirection.Left => -1,
			GameSimulationState.PanDirection.Right => 1,
			GameSimulationState.PanDirection.UpLeft => -1,
			GameSimulationState.PanDirection.UpRight => 1,
			GameSimulationState.PanDirection.DownLeft => -1,
			GameSimulationState.PanDirection.DownRight => 1,
			_ => 0,
		};
		var y = pDirection switch
		{
			GameSimulationState.PanDirection.Up => -1,
			GameSimulationState.PanDirection.UpLeft => -1,
			GameSimulationState.PanDirection.UpRight => -1,
			GameSimulationState.PanDirection.Down => 1,
			GameSimulationState.PanDirection.DownLeft => 1,
			GameSimulationState.PanDirection.DownRight => 1,
			_ => 0,
		};
		return pAxis == 0 ? x : y;
	}

	/// <summary>
	/// A pan direction number, from <c>11060</c>'s second parameter.
	/// </summary>
	/// <remarks>
	/// <strong>The reference's own order, and the eight values are a
	/// compass order after all.</strong> The editor's pan directions are up 0,
	/// right 1, down 2, left 3 and the four diagonals 4 to 7 — so a reader that
	/// used the eight-direction passability bit order would scroll the wrong way
	/// for every diagonal.
	/// </remarks>
	private static GameSimulationState.PanDirection DirectionOf(int pNumber)
	{
		return pNumber switch
		{
			0 => GameSimulationState.PanDirection.Up,
			1 => GameSimulationState.PanDirection.Right,
			2 => GameSimulationState.PanDirection.Down,
			3 => GameSimulationState.PanDirection.Left,
			4 => GameSimulationState.PanDirection.UpLeft,
			5 => GameSimulationState.PanDirection.UpRight,
			6 => GameSimulationState.PanDirection.DownLeft,
			7 => GameSimulationState.PanDirection.DownRight,
			_ => GameSimulationState.PanDirection.Up,
		};
	}

	/// <summary>
	/// 11750, Tile Substitution, from EasyRPG's <c>CommandTileSubstitution</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Parameter 0 is a boolean and not a layer number.</strong> The
	/// reference calls <c>SubstituteUp</c> or <c>SubstituteDown</c> on it, and a
	/// reader that read it as "0 means lower, 1 means upper" would be right by
	/// accident for two values and wrong for every other.
	/// </para>
	/// <para>
	/// <strong>This is the command the class was missing a writer for.</strong>
	/// The two 144-entry tables and their two readers existed, so the command
	/// could be parsed and never run.
	/// </para>
	/// </remarks>
	private void ExecuteTileSubstitution(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 3.
		if (pCmd.Parameters.Count < 3)
		{
			Malformed("Tile substitution");
			return;
		}
		var upper = pCmd.Parameters[0] != 0;
		var oldId = pCmd.Parameters[1];
		var newId = pCmd.Parameters[2];
		_state.TileSubstitution ??= new Rm2kTileSubstitution(null, null);
		if (!_state.TileSubstitution.SubstituteTile(upper, oldId, newId))
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Tile substitution: index {oldId} is outside"
				+ $" the table of {Rm2kChipset.NumUpperTiles}, and nothing was"
				+ " changed");
			return;
		}
		_state.AddDiagnostic(
			$"[Event {_eventId}] Tile {(upper ? "upper" : "lower")} {oldId}"
			+ $" now draws chip {newId}");
	}

	/// <summary>
	/// 20140, Show Choice Option, from EasyRPG's
	/// <c>CommandShowChoiceOption</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This is the RM2K3 form of a choice branch, and it is <strong>not a
	/// second choice window</strong> — it is one branch of a list that the
	/// player already answered. The reference passes it to
	/// <c>CommandOptionGeneric</c>, which does two things:
	/// </para>
	/// <list type="bullet">
	/// <item>If this command's <see cref="_subIndex"/> matches the branch
	/// number in <c>parameters[0]</c>, it clears the sub index so the other
	/// branches are skipped.</item>
	/// <item>Otherwise it <strong>skips to the next conditional</strong> — which
	/// is <c>20141 Show Choice End</c> — and runs nothing of this branch.</item>
	/// </list>
	/// <para>
	/// <strong>Both halves matter and a reader with only one gets a game that
	/// runs every branch</strong> — a hero who asks a question would walk away,
	/// fight the guard, buy the sword and leave, all in one frame.
	/// </para>
	/// <para>
	/// <strong>This is also why <see cref="Rm2kMap.EventCommand.Indent"/> exists.</strong>
	/// The sub index comes from the LCF indent chunk, and a decoder that read
	/// that chunk and then dropped the number produced events that parse
	/// completely and behave wrongly.
	/// </para>
	/// </remarks>
	private void ExecuteShowChoiceOption(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 1.
		if (pCmd.Parameters.Count < 1)
		{
			Malformed("Show choice option");
			return;
		}
		var branch = pCmd.Parameters[0];
		if (_subIndex == branch)
		{
			// **The chosen branch clears the sub index**, so the interpreter
			// stops treating the following 20140s as live branches.
			_subIndex = SubCommandSentinel;
			_state.AddDiagnostic(
				$"[Event {_eventId}] Choice branch {branch} runs, and the"
				+ " following branches are skipped");
			return;
		}
		// **Every other branch jumps to the next 20141 and runs nothing of
		// this one.** That is the whole difference between a branch list and a
		// plain sequence of commands.
		var skipped = SkipToNextChoiceEnd();
		_state.AddDiagnostic(
			$"[Event {_eventId}] Choice branch {branch} is not the chosen one,"
			+ $" so {skipped} commands were skipped");
	}

	/// <summary>
	/// 20141, Show Choice End, from EasyRPG's <c>CommandShowChoiceEnd</c>.
	/// </summary>
	/// <remarks>
	/// <strong>It does nothing, and that is the whole command.</strong> The
	/// reference writes <c>return true;</c> and never reads the command. It
	/// exists so that a branch list has an end, and a reader that treated it
	/// as "the end of the choice" would close a window the player is still
	/// looking at.
	/// </remarks>
	private void ExecuteShowChoiceEnd()
	{
		_state.AddDiagnostic(
			$"[Event {_eventId}] Choice branch list ends here, and the command"
			+ " itself does nothing — that is what the reference does with it");
	}

	/// <summary>
	/// Jumps to the next <c>20141</c> and returns how many commands were
	/// skipped.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>A branch with no end stops at the end of the list</strong>, and
	/// says so. The reference's <c>SkipToNextConditional</c> walks to the next
	/// command from its set; a branch list whose last option has no end is a
	/// game bug, and a reader that ran off the end of the command array would
	/// read past the list.
	/// </para>
	/// <para>
	/// Only <c>20141</c> is in the set here, because the reference passes
	/// <c>{Cmd::ShowChoiceOption, Cmd::ShowChoiceEnd}</c> and a nested option
	/// list ends at its own end.
	/// </para>
	/// </remarks>
	private int SkipToNextChoiceEnd()
	{
		var start = _commandIndex;
		// **The bound is checked before the read, and not after the step.** A
		// branch list whose last option has no end is a game bug, and a reader
		// that stepped first and checked after would read one past the array
		// and throw — so the check is the first thing in the loop.
		while (_commandIndex + 1 < _commands.Count)
		{
			_commandIndex++;
			if (_commands[_commandIndex].Code != ChoiceEnd)
			{
				continue;
			}
			return _commandIndex - start;
		}
		// The index is left on the last command, so the page finishes instead
		// of walking past the list.
		_commandIndex = _commands.Count - 1;
		_state.AddDiagnostic(
			$"[Event {_eventId}] Choice branch list has no {ChoiceEnd} after the"
			+ $" skipped commands, so the branch ran to the end of the list");
		return _commandIndex - start;
	}

	/// <summary>
	/// Runs 10710, Enemy Encounter, from liblcf's <c>Code::EnemyEncounter</c>
	/// and EasyRPG's <c>Game_Interpreter_Map::CommandEnemyEncounter</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Six parameters, or ten, and the count is the mode's own.</strong>
	/// The reference's dispatch has two lines for this one command — one with a
	/// width of 6 and one with 10 for the RPG2K3 form. <strong>A reader that
	/// required ten would refuse every 2K game's encounter, and one that
	/// required six would refuse a 2003 game's background form.</strong> This
	/// takes six, and reads the tenth only in the one mode that has it.
	/// </para>
	/// <para>
	/// <strong>An open message first, and the battle waits for it</strong> — the
	/// reference's first two lines are
	/// <c>if (Game_Message::IsMessageActive()) return false;</c>, the same as
	/// the game over screen, the title request and the two menu openers.
	/// </para>
	/// <para>
	/// <strong>The troop is resolved through the mode and the value, like every
	/// other number a command takes</strong> — the reference writes
	/// <c>ValueOrVariable(com.parameters[0], com.parameters[1])</c>, so a game can
	/// pick the encounter group by variable.
	/// </para>
	/// <para>
	/// <strong>Three terrain modes and a fourth that is refused.</strong> The
	/// reference's switch has cases 0, 1 and 2 and a <c>default: return false</c>
	/// — so a mode of 3 does not start a battle at all. <strong>A reader that
	/// defaulted to the first would have fought a battle the file did not ask
	/// for</strong>, and a game's test battle with a mistyped mode would have
	/// been a real one.
	/// </para>
	/// <para>
	/// <strong>The escape mode is three values and not a boolean.</strong> The
	/// reference reads <c>escape_mode = com.parameters[3]</c> with 0 for "not
	/// at all", 1 for "end the event processing" and 2 for the game's own
	/// handler — and the middle one sets <c>abort_on_escape</c>, which
	/// <em>ends the event</em> when the party escapes. <strong>A reader that
	/// read it as a boolean would have a game whose escape returned to the
	/// event's next line where the reference ends the event dead.</strong>
	/// </para>
	/// <para>
	/// <strong>Defeat is a game over unless the command says otherwise, and the
	/// reference pushes the game over screen itself</strong> — so a defeat mode
	/// of 1 is a game that has written its own defeat handling.
	/// </para>
	/// </remarks>
	private void ExecuteEnemyEncounter(Rm2kMap.EventCommand pCmd)
	{
		if (_presentation != null && _presentation.MessageVisible)
		{
			_state.WaitingFor = GameSimulationState.WaitReason.MessageOpen;
			_state.AddDiagnostic(
				$"[Event {_eventId}] Encounter waits: a message is open, because"
				+ " the reference starts the battle after the line is read");
			return;
		}
		// **Six, and the tenth is read only in the one mode that has it.**
		if (pCmd.Parameters.Count < 6)
		{
			Malformed("Enemy encounter");
			return;
		}
		// **The troop through the mode and the value**, the reference's own
		// ValueOrVariable call.
		var troopId = ValueOrVariable(pCmd.Parameters[0], pCmd.Parameters[1]);

		GameSimulationState.BattleTerrainMode terrainMode;
		switch (pCmd.Parameters[2])
		{
			case 0:
				terrainMode = GameSimulationState.BattleTerrainMode.System;
				break;
			case 1:
				terrainMode = GameSimulationState.BattleTerrainMode.Background;
				break;
			case 2:
				terrainMode = GameSimulationState.BattleTerrainMode.TerrainId;
				break;
			default:
				// **A fourth mode is refused and not defaulted.** The
				// reference's `default: return false` starts no battle, and a
				// reader that fell through to the first case would have fought
				// one the file did not ask for.
				_state.AddDiagnostic(
					$"[Event {_eventId}] Encounter: terrain mode"
					+ $" {pCmd.Parameters[2]} is not 0, 1 or 2, and the reference"
					+ " starts no battle for it");
				return;
		}

		GameSimulationState.BattleEscapeMode escape;
		switch (pCmd.Parameters[3])
		{
			case 0:
				escape = GameSimulationState.BattleEscapeMode.Disallow;
				break;
			case 1:
				escape = GameSimulationState.BattleEscapeMode.EndEvent;
				break;
			case 2:
				escape = GameSimulationState.BattleEscapeMode.CustomHandler;
				break;
			default:
				// **The reference does not switch on this one at all** — it
				// writes `allow_escape = (escape_mode != 0)`. So a fourth
				// value is "escapable, and the custom handler runs", and
				// saying so is better than refusing a number the reference
				// accepts.
				escape = GameSimulationState.BattleEscapeMode.CustomHandler;
				break;
		}
		// **Defeat is a boolean in the reference, and a game over unless it
		// says otherwise.**
		var defeat = pCmd.Parameters[4] == 0
			? GameSimulationState.BattleDefeatMode.GameOver
			: GameSimulationState.BattleDefeatMode.CustomHandler;

		_state.ActiveTroopId = troopId;
		_state.IsBattleActive = true;
		_state.BattleTurn = 0;
		// **The first turn is the player's, and that is phase 1 in this
		// reader's own scale** — 0 is initial, 1 player, 2 enemy. The battle
		// has started, so the initial phase is behind it.
		_state.BattlePhase = 1;
		_state.BattleEscape = escape;
		_state.BattleDefeat = defeat;
		_state.BattleFirstStrike = pCmd.Parameters[5] != 0;
		_state.BattleTerrain = terrainMode;
		// **No outcome yet, and -1 says so** — a reader that wrote 0 would
		// have a game whose victory arm ran before the battle was fought.
		_state.BattleSubcommand = -1;
		_state.WaitingFor = GameSimulationState.WaitReason.BattleRunning;
		_state.AddDiagnostic(
			$"[Event {_eventId}] Encounter: troop {troopId}, terrain"
			+ $" {terrainMode}, escape {escape}, defeat {defeat}"
			+ (_state.BattleFirstStrike ? ", the party strikes first" : string.Empty)
			+ ", and the page holds until the battle ends");
	}

	/// <summary>
	/// 10500, Simulated Attack, from EasyRPG's <c>CommandSimulatedAttack</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>It is not a battle.</strong> There is no turn order, no troop and
	/// no target selection: the command picks heroes by the usual actor
	/// parameters, computes one number from their defence and spirit, and
	/// subtracts it from their hit points. A game uses it for a trap that bites,
	/// a poison that hurts, a script that stings — <em>damage without a
	/// battle</em>, which is why this slice did not have to build a combat
	/// system first.
	/// </para>
	/// <para>
	/// <strong>Defence is divided by 400 and spirit by 800.</strong> So 800
	/// points of spirit block twice as much as 400 points of defence. A reader
	/// that used one divisor for both would make spirit twice as strong as the
	/// game meant it.
	/// </para>
	/// <para>
	/// <strong>Parameters 6 and 7 are an optional result variable.</strong> A
	/// non-zero sixth stores the damage that was dealt into the seventh
	/// parameter's variable, and a game that shows "you took 12" reads it from
	/// there. The reference does it per actor, so a command against a whole
	/// party leaves the last actor's damage in the variable — <em>which is what
	/// the reference does and not what a reader would guess</em>.
	/// </para>
	/// </remarks>
	private void ExecuteSimulatedAttack(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 8.
		if (pCmd.Parameters.Count < 8)
		{
			Malformed("Simulated attack");
			return;
		}
		var actors = ResolveActors(
			pCmd.Parameters[0], pCmd.Parameters[1], "Simulated attack");
		if (actors == null)
		{
			return;
		}
		var storeResult = pCmd.Parameters[6] != 0;
		var resultVariable = pCmd.Parameters[7];
		if (storeResult && (resultVariable < 1
			|| resultVariable > GameSimulationState.MaxVariables))
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Simulated attack: the result goes into"
				+ $" variable {resultVariable}, which is outside 1 to"
				+ $" {GameSimulationState.MaxVariables}, and nothing was changed");
			return;
		}
		var lastDamage = 0;
		foreach (var actorId in actors)
		{
			var values = _state.GetOrCreateActorValues(actorId);
			var damage = Rm2kSimulatedAttack.Compute(
				pCmd.Parameters[2],
				values.BaseDefense, pCmd.Parameters[3],
				values.BaseSpirit, pCmd.Parameters[4],
				pCmd.Parameters[5], _damageRandom);
			// **The damage is a loss, and the current hit points are the
			// reference's own field** — a command that added it would heal the
			// hero it was aimed at.
			var vorher = _state.GetActorCurrentHp(actorId);
			var nachher = Rm2kActorValues.ChangeHp(
				vorher, -damage, values.BaseMaxHp, pLethal: true);
			_state.CurrentHp[actorId] = nachher;
			lastDamage = damage;
			_state.AddDiagnostic(
				$"[Event {_eventId}] Simulated attack on hero {actorId}:"
				+ $" {vorher} -> {nachher} by {damage}");
		}
		// **The last actor's damage, and not the total.** The reference writes
		// the variable inside the loop, so a command against a party leaves the
		// last one in it — and a reader that summed would make a game that shows
		// "you took N" show a number the game never produced.
		if (storeResult)
		{
			_state.Variables[resultVariable - 1] = lastDamage;
			_state.AddDiagnostic(
				$"[Event {_eventId}] Simulated attack stored {lastDamage} in"
				+ $" variable {resultVariable}, and that is the last actor's"
				+ " damage and not the sum");
		}
	}

	private void ExecuteChangeHeroTitle(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 4.
		if (pCmd.Parameters.Count < 4)
		{
			Malformed("Change hero title");
			return;
		}
		var actorId = SystemBitfield(pCmd, 0);
		var values = FindActorValues(actorId, "Change hero title");
		if (values == null)
		{
			return;
		}
		var title = CommandStringOrVariable(pCmd, 1, 1, 2);
		values.Title = title;
		_state.AddDiagnostic(
			$"[Event {_eventId}] Hero {actorId} is now called \"{title}\"");
	}

	/// <summary>
	/// 10630, Change Sprite Association, from EasyRPG's
	/// <c>CommandChangeSpriteAssociation</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>The index is a walk-cycle offset and not a character
	/// number.</strong> A costume is the same file with a different index, and a
	/// reader that read the index as a character number would put a hero in
	/// somebody else's costume.
	/// </para>
	/// <para>
	/// <strong>The transparency flag is <c>parameters[2]</c> and not part of
	/// the bitfield</strong>, which is what the reference does: it reads the
	/// mode index for the other two values and this one directly. A reader that
	/// took all three from the bitfield would make a costume transparent
	/// whenever a Maniac game packed a different value there.
	/// </para>
	/// <para>
	/// The reference also calls <c>ResetGraphic</c> afterwards. <strong>Without
	/// that the hero keeps the old sprite until the next redraw</strong>, which
	/// in a turn-based engine is at the end of the move — so a costume change
	/// would show up one step late.
	/// </para>
	/// </remarks>
	private void ExecuteChangeSpriteAssociation(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 5.
		if (pCmd.Parameters.Count < 5)
		{
			Malformed("Change sprite association");
			return;
		}
		var actorId = SystemBitfield(pCmd, 0);
		var values = FindActorValues(actorId, "Change sprite association");
		if (values == null)
		{
			return;
		}
		var name = CommandStringOrVariable(pCmd, 3, 1, 4);
		values.SpriteName = name;
		values.SpriteIndex = SystemBitfield(pCmd, 1);
		// **parameters[2] and not the bitfield, which is the reference's own
		// split and the reason a Maniac game does not change the transparency
		// through this command.**
		values.SpriteTransparent = pCmd.Parameters[2] != 0;
		_state.AddDiagnostic(
			$"[Event {_eventId}] Hero {actorId} now wears \"{name}\" index"
			+ $" {values.SpriteIndex}, transparent is {values.SpriteTransparent}");
	}

	/// <summary>
	/// 10640, Change Actor Face, from EasyRPG's <c>CommandChangeActorFace</c>.
	/// </summary>
	/// <remarks>
	/// <strong>Two parameters and not three</strong> — the name and the index —
	/// and the index is a plain value. A reader that read a transparency flag
	/// here would shift the index by one and put a hero in the wrong face.
	/// </remarks>
	private void ExecuteChangeActorFace(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 4.
		if (pCmd.Parameters.Count < 4)
		{
			Malformed("Change actor face");
			return;
		}
		var actorId = SystemBitfield(pCmd, 0);
		var values = FindActorValues(actorId, "Change actor face");
		if (values == null)
		{
			return;
		}
		var name = CommandStringOrVariable(pCmd, 2, 1, 3);
		var index = SystemBitfield(pCmd, 1);
		if (index < 0 || index > Rm2kActorValues.MaxFaceIndex)
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Change actor face: index {index} is outside the"
				+ $" file's four slots for hero {actorId}, and nothing was set");
			return;
		}
		values.FaceName = name;
		values.FaceIndex = index;
		_state.AddDiagnostic(
			$"[Event {_eventId}] Hero {actorId} now shows \"{name}\" slot {index}");
	}

	/// <summary>
	/// 10650, Change Vehicle Graphic, from EasyRPG's
	/// <c>CommandChangeVehicleGraphic</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Parameter 0 plus one.</strong> The reference writes
	/// <c>(Game_Vehicle::Type)(com.parameters[0] + 1)</c>, because the enum
	/// starts at one for the party-less case. A reader that used the parameter
	/// directly would address vehicle 0 where the game meant vehicle 1 — and
	/// vehicle 0 is not a vehicle at all.
	/// </para>
	/// <para>
	/// The reference sets <strong>two</strong> fields: the current sprite and
	/// the original one. The original is what the vehicle goes back to when a
	/// board is left, so <strong>setting only the current one would leave a
	/// vehicle in its costume after the party got out.</strong>
	/// </para>
	/// </remarks>
	private void ExecuteChangeVehicleGraphic(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 2.
		if (pCmd.Parameters.Count < 2)
		{
			Malformed("Change vehicle graphic");
			return;
		}
		var vehicle = FindVehicle(pCmd.Parameters[0], "Change vehicle graphic");
		if (vehicle == null)
		{
			return;
		}
		vehicle.CharacterName = pCmd.Text;
		vehicle.SpriteIndex = pCmd.Parameters[1];
		// The original is the same value today, and **a reader that set only the
		// current sprite would leave a vehicle in its costume after a board**.
		vehicle.OriginalCharacterName = pCmd.Text;
		vehicle.OriginalSpriteIndex = pCmd.Parameters[1];
		_state.AddDiagnostic(
			$"[Event {_eventId}] Vehicle {pCmd.Parameters[0]} is now \"{pCmd.Text}\""
			+ $" index {pCmd.Parameters[1]}, and that is also what it returns to");
	}

	/// <summary>
	/// Runs 10840, Get On/Off Vehicle.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Whether it happened, and not whether it could.</strong> The
	/// reference's <c>GetOnOffVehicle</c> does nothing at all when there is
	/// nothing to board onto and nothing to step off onto — and the difference
	/// is the whole of the command's observable behaviour. A hook that returned
	/// the ability instead of the act would have a game run a "you cannot board
	/// here" branch the reference never takes.
	/// </para>
	/// <para>
	/// <strong>A missing hook is a refusal with a name, and not a silent
	/// skip.</strong> The fifteen boarding rules live on
	/// <c>Rm2kVehicleBoarding</c> and are called by whoever holds the world; a
	/// caller that gave the interpreter no hook has not said "no vehicle here",
	/// it has said "this reader cannot board", and those are different.
	/// </para>
	/// <para>
	/// <strong>The command does not wait, and that is the reference's
	/// behaviour.</strong> EasyRPG's issue thread on this command records that
	/// RPG_RT's own <c>GetOnOffVehicle</c> does not wait for the boarding
	/// animation, and this reader follows the reference rather than the bug
	/// report's observations of both.
	/// </para>
	/// </remarks>
	private void ExecuteGetOnOffVehicle()
	{
		if (_vehicleBoardToggle == null)
		{
			Malformed("Get on/off vehicle");
			return;
		}
		var boarded = _vehicleBoardToggle();
		_state.AddDiagnostic(
			$"[Event {_eventId}] Get on/off vehicle: "
			+ (boarded ? "boarded or left a vehicle" : "nothing to board or leave")
			+ ", which is the reference's silence and not a failure");
	}

	/// <summary>
	/// 10850, Set Vehicle Location, from EasyRPG's
	/// <c>CommandSetVehicleLocation</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <strong>Vehicle id -1 moves the party and not a vehicle.</strong> The
	/// reference has a comment on it: in RPG_RT a party in no vehicle has the
	/// id -1, and passing -1 moves the party on its own. A reader that refused
	/// it as an invalid id would make every "teleport the hero" command in a
	/// game do nothing — and that is a very common command.
	/// </para>
	/// <para>
	/// <strong>When the party is in that vehicle, both move together</strong>,
	/// and the reference returns right after. A reader that moved only the
	/// vehicle would leave the hero standing in the old map.
	/// </para>
	/// <para>
	/// <strong>All three coordinates go through <c>ValueOrVariable</c></strong>
	/// with the mode in <c>parameters[1]</c>, so a game can follow a variable.
	/// </para>
	/// </remarks>
	private void ExecuteSetVehicleLocation(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 5.
		if (pCmd.Parameters.Count < 5)
		{
			Malformed("Set vehicle location");
			return;
		}
		// **-1 is the party, and a reader that read it as an invalid id would
		// make every "move the hero" command in a game do nothing.**
		var rawId = pCmd.Parameters[0];
		var vehicle = rawId < 0 ? null : FindVehicle(rawId, "Set vehicle location");
		if (rawId >= 0 && vehicle == null)
		{
			return;
		}
		var mode = pCmd.Parameters[1];
		var mapId = ValueOrVariable(mode, pCmd.Parameters[2]);
		var x = ValueOrVariable(mode, pCmd.Parameters[3]);
		var y = ValueOrVariable(mode, pCmd.Parameters[4]);
		// **IsBoarding and not a vehicle id.** The reference compares the
		// player Vehicle() with the vehicle it is moving, and this reader has
		// the same two pieces: whether the player is aboard, and which type.
		// **Boarding is nullable and that is a design, not a gap.** A game that
		// never touches a vehicle never allocates one, so a reader that
		// dereferenced it would throw on every 10850 in a game with no ship —
		// and the common case is exactly that game.
		var partyRides = _state.Boarding != null
			&& _state.Boarding.IsBoarding
			&& _state.Boarding.VehicleType == rawId + FirstVehicleIdOffset;
		if (partyRides)
		{
			// **The party and the vehicle move as one, and the reference returns
			// right after.** Moving only the vehicle would leave the hero
			// standing in the map they left.
			if (vehicle != null)
			{
				vehicle.MapId = mapId;
				vehicle.X = x;
				vehicle.Y = y;
			}
			_state.MapId = mapId;
			_state.MapX = x;
			_state.MapY = y;
			_state.AddDiagnostic(
				$"[Event {_eventId}] Vehicle {rawId} and the party inside it are"
				+ $" now on map {mapId} at {x},{y}");
			return;
		}
		if (vehicle == null)
		{
			// **No vehicle and nobody in one: this is the party on its own.**
			_state.MapId = mapId;
			_state.MapX = x;
			_state.MapY = y;
			_state.AddDiagnostic(
				$"[Event {_eventId}] The party is now on map {mapId} at {x},{y},"
				+ " because id -1 means the party and not a vehicle");
			return;
		}
		vehicle.MapId = mapId;
		vehicle.X = x;
		vehicle.Y = y;
		_state.AddDiagnostic(
			$"[Event {_eventId}] Vehicle {rawId} is now on map {mapId} at {x},{y},"
			+ " and the party stays where it is");
	}

	/// <summary>
	/// The base values of a hero, or null with a diagnostic when the database
	/// has no such hero.
	/// </summary>
	/// <remarks>
	/// <strong>A missing hero is a warning and not an exception.</strong> The
	/// reference calls <c>GetActor</c>, checks the result and writes a warning;
	/// a reader that threw would take a game down over an actor slot it chose
	/// not to fill.
	/// </remarks>
	private Rm2kActorValues? FindActorValues(int pActorId, string pCommand)
	{
		if (pActorId < 1 || pActorId > GameSimulationState.MaxActorId)
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] {pCommand}: hero {pActorId} is outside 1 to"
				+ $" {GameSimulationState.MaxActorId}, and the reference warns"
				+ " and moves on; nothing was changed");
			return null;
		}
		return _state.GetOrCreateActorValues(pActorId);
	}

	/// <summary>
	/// A vehicle, or null with a diagnostic.
	/// </summary>
	/// <remarks>
	/// <strong>The parameter plus one</strong>, because the reference's enum
	/// starts at one. A reader that used the parameter directly would address
	/// vehicle 0 where the game meant vehicle 1.
	/// </remarks>
	private Rm2kVehicleState? FindVehicle(int pRawId, string pCommand)
	{
		// **The plus one is the reference's own line** and the liblcf enum is
		// None 0, Boat 1, Ship 2, Airship 3 — so parameter 0 is the boat.
		var vehicleId = pRawId + FirstVehicleIdOffset;
		if (vehicleId <= 0)
			{
			_state.AddDiagnostic(
				$"[Event {_eventId}] {pCommand}: vehicle {pRawId} would be type 0,"
				+ " which is the party and not a vehicle; the reference only allows"
				+ " that through 10850");
			return null;
			}
		foreach (var vehicle in _state.Vehicles)
		{
			if (vehicle.VehicleType == vehicleId)
			{
				return vehicle;
			}
		}
		_state.AddDiagnostic(
			$"[Event {_eventId}] {pCommand}: vehicle {pRawId} is not on this map,"
			+ " and the reference warns and moves on; nothing was changed");
		return null;
	}

	private int SystemBitfield(Rm2kMap.EventCommand pCmd, int pIndex)
	{
		if (_state.SupportsManiacPatch)
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Command {pCmd.Code} parameter {pIndex} is a"
				+ " Maniac bitfield in a game that uses the patch; this runtime has no"
				+ $" game-string mirror, so the plain value {pCmd.Parameters[pIndex]} was used");
		}
		return pCmd.Parameters[pIndex];
	}
	private void ExecuteAccessChange(Rm2kMap.EventCommand pCmd, AccessFlag pWhich)
	{
	if (pCmd.Parameters.Count < 1)
	{
		Malformed("Change access");
		return;
	}
	var allowed = pCmd.Parameters[0] != 0;
	// **One call, and the two other flags come along unchanged.** A reader
	// that only ever wrote the flag its command named could never give a
	// player their menu back, and one that wrote all three from defaults
	// would unlock a cutscene the game had just locked.
	switch (pWhich)
	{
		case AccessFlag.Escape:
			_state.SetAccess(allowed, _state.AllowSave, _state.AllowMenu,
				_state.AllowTeleport);
			break;
		case AccessFlag.Save:
			_state.SetAccess(_state.AllowEscape, allowed, _state.AllowMenu,
				_state.AllowTeleport);
			break;
		case AccessFlag.Menu:
			_state.SetAccess(_state.AllowEscape, _state.AllowSave, allowed,
				_state.AllowTeleport);
			break;
		case AccessFlag.Teleport:
			_state.SetAccess(_state.AllowEscape, _state.AllowSave,
				_state.AllowMenu, pTeleport: allowed);
			break;
		default:
			_state.AddDiagnostic(
				$"[Event {_eventId}] Change access: the flag is not one of the four");
			return;
	}
	_state.AddDiagnostic(
		$"[Event {_eventId}] Change access: {pWhich} is now {allowed}, from a"
		+ $" {pCmd.Parameters[0]}");
	}

	private void ExecuteMemorizeLocation(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 3.
		if (pCmd.Parameters.Count < 3)
		{
			Malformed("Memorize location");
			return;
		}
		var varMap = pCmd.Parameters[0];
		var varX = pCmd.Parameters[1];
		var varY = pCmd.Parameters[2];
		// **All three must be writable**, and a variable id of 0 is not one —
		// a first draft would have written to index -1 and thrown, and a
		// game whose editor left a field empty would have stopped its event.
		foreach (var id in new int[] { varMap, varX, varY })
		{
			if (id < 1 || id > GameSimulationState.MaxVariables)
			{
				_state.AddDiagnostic(
					$"[Event {_eventId}] Memorize location: variable {id} is"
					+ " outside 1 to 50000, and nothing was stored");
				return;
			}
		}
		WriteVariable(varMap, _state.MapId);
		WriteVariable(varX, _state.MapX);
		WriteVariable(varY, _state.MapY);
		_state.AddDiagnostic(
			$"[Event {_eventId}] Memorized map {_state.MapId}"
			+ $" at {_state.MapX},{_state.MapY} into variables"
			+ $" {varMap}, {varX} and {varY}");
	}

	private void ExecutePlayerVisibility(Rm2kMap.EventCommand pCmd)
	{
		if (pCmd.Parameters.Count < 1)
		{
			Malformed("Player visibility");
			return;
	}
		var hidden = pCmd.Parameters[0] == 0;
		_state.PlayerIsHidden = hidden;
		if (hidden)
		{
			// The reference calls ResetThrough here as well, and a hidden
			// player who has walked through a wall must not stay in it.
			_state.PlayerIsThrough = false;
		}
		_state.AddDiagnostic(
			$"[Event {_eventId}] Player is now"
			+ $" {(hidden ? "hidden" : "visible")}, because parameters[0] == 0"
			+ " is the hide case");
	}

	/// <summary>
	/// 11330, Move Event, from EasyRPG's <c>CommandMoveEvent</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Parameters are <c>[eventId, moveFreq, idBitfield, skippable,
	/// route...]</c>, and <strong>the route is the rest of the list</strong>
	/// — the reference walks it from index four to the end. A reader that
	/// read a fixed count would silently drop a long route, and the game
	/// would run a shortened version of what the author wrote.
	/// </para>
	/// <para>
	/// <strong>A move frequency outside 1 to 8 becomes 6</strong>, and that
	/// is the reference own default rather than a refusal: <c>if (move_freq
	/// &lt;= 0 || move_freq &gt; 8) move_freq = 6;</c>. A reader that
	/// refused would stop a route RPG_RT happily runs.
	/// </para>
	/// <para>
	/// <strong>The id mode and the repeat flag share one word.</strong> The
	/// mode is the low two bits of the third parameter and the repeat is
	/// its low bit, so a reader that took the whole number as the mode
	/// would read mode 3 where a game meant mode 1 and a repeat.
	/// </para>
	/// </remarks>
	private void ExecuteMoveEvent(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 4.
		if (pCmd.Parameters.Count < 4)
		{
			Malformed("Move event");
			return;
		}
		if (_moveRouteStarter == null)
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Move event: this interpreter cannot reach a"
				+ " character, so no route was started; the steps are still in the"
				+ " file");
			return;
	}
		var idBitfield = pCmd.Parameters[2];
		var eventId = ValueOrVariable(idBitfield & 0x3, pCmd.Parameters[0]);
		var repeat = (idBitfield & 0x1) != 0;
		var moveFreq = pCmd.Parameters[1];
		if (moveFreq <= 0 || moveFreq > 8)
		{
			// The reference own fallback, not a refusal.
			moveFreq = 6;
	}
		var skippable = pCmd.Parameters[3] != 0;
		var route = new List<Rm2kMap.MoveCommand>();
		for (var i = 4; i < pCmd.Parameters.Count; i++)
		{
			route.Add(new Rm2kMap.MoveCommand(pCmd.Parameters[i]));
	}
		var started = _moveRouteStarter(
			eventId, moveFreq, route, repeat, skippable);
		if (!started)
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Move event {eventId}: no character carries"
				+ " that id, and the reference logs a warning and touches nobody;"
				+ $" the route had {route.Count} steps");
			return;
	}
		_state.AddDiagnostic(
			$"[Event {_eventId}] Move event {eventId}: {route.Count} steps"
			+ $" at frequency {moveFreq}, repeating is {repeat},"
			+ $" skippable is {skippable}");
	}

	private void ExecuteTintScreen(Rm2kMap.EventCommand pCmd)
	{
		if (_presentation == null)
		{
			Malformed("Tint screen");
			return;
	}
		// CmdSetup minimum width 6.
		if (pCmd.Parameters.Count < 6)
		{
			Malformed("Tint screen");
			return;
	}
		var tenths = pCmd.Parameters[4];
		if (tenths < 0 || tenths > EventInterpreterMaxTenths)
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Tint screen: {tenths} tenths is outside"
				+ " the format's 0 to 100; nothing was tinted");
			return;
	}
		// The reference's own conversion. **The multiplier is the frame rate
		// and not a literal**, and a reader that hardcoded one would disagree
		// with a game that declared another.
		var frames = tenths * 60 / 10;
		if (!_presentation.TintScreen(
			Param(pCmd, 0), Param(pCmd, 1), Param(pCmd, 2), Param(pCmd, 3), frames))
			{
			_state.AddDiagnostic(
				$"[Event {_eventId}] Tint screen refused: a channel was outside"
				+ " 0 to 255 or the saturation outside 0 to 100");
			return;
	}
		_state.AddDiagnostic(
			$"[Event {_eventId}] Tint screen r{Param(pCmd, 0)}"
			+ $" g{Param(pCmd, 1)} b{Param(pCmd, 2)}"
			+ $" saturation {Param(pCmd, 3)} over {tenths} tenths"
			+ $", which is {frames} frames");
		if (Param(pCmd, 5) != 0)
		{
			_waitFramesRemaining = tenths * 6;
	}
	}

	/// <summary>
	/// 11010 and 11020, Erase and Show Screen, from EasyRPG's
	/// <c>CommandEraseScreen</c> and <c>CommandShowScreen</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// One parameter, the transition kind. <strong>Parameter -1 is not a
	/// kind</strong> — it means "the game's own teleport transition", which
	/// comes from the editor's settings and is not in the command at all.
	/// This reader has not read those settings, so it says so rather than
	/// falling through to none, because <strong>a silent fall-through is
	/// what the reference does for a number it does not know, and it makes
	/// every teleport in a game lose its transition without a word.</strong>
	/// </para>
	/// <para>
	/// <strong>A message blocks the transition</strong> — both commands
	/// return false while one is open — and so does a requested battle or
	/// game-over scene. This reader holds the page for the message, because
	/// the reference does; the pending scene is not modelled here yet.
	/// </para>
	/// </remarks>
	private void ExecuteScreenTransition(Rm2kMap.EventCommand pCmd)
	{
		var istShow = pCmd.Code == ShowScreen;
		var label = istShow ? "Show screen" : "Erase screen";
		if (_presentation == null)
		{
			Malformed(label);
			return;
	}
		if (pCmd.Parameters.Count < 1)
		{
			Malformed(label);
			return;
	}
		if (_presentation.MessageVisible)
		{
			_state.AddDiagnostic(
				$"[Event {_eventId}] {label}: a message is open, and the"
				+ " reference holds the transition until it is not; nothing happened");
			_waitFramesRemaining = 1;
			return;
	}
		var richtung = istShow
			? Rm2kTransitionDirection.Show
			: Rm2kTransitionDirection.Erase;
		var ergebnis = Rm2kTransitionKind.FromParameter(pCmd.Parameters[0], richtung);
		switch (ergebnis.Kind)
		{
			case Rm2kTransitionRequestResultKind.Named:
				if (!_presentation.RequestTransition(richtung, ergebnis.Transition))
				{
					_state.AddDiagnostic(
						$"[Event {_eventId}] {label}: refused a transition this"
						+ " presentation does not define");
					return;
	}
				_state.AddDiagnostic(
					$"[Event {_eventId}] {label}: {ergebnis.Transition}");
				return;
			case Rm2kTransitionRequestResultKind.GameWideTransition:
				_state.AddDiagnostic(
					$"[Event {_eventId}] {label}: parameter -1 is the game's"
					+ " own teleport transition from the editor's settings, and this"
					+ " reader has not read those; no transition was chosen");
				return;
			default:
				_state.AddDiagnostic(
					$"[Event {_eventId}] {label}: parameter {pCmd.Parameters[0]}"
					+ " names no transition in either table, and the reference falls"
					+ " through to none without saying so; none was chosen");
				return;
	}
	}

	private void ExecuteFlashScreen(Rm2kMap.EventCommand pCmd)
	{
		// EasyRPG CommandFlashScreen: [red, green, blue, alpha, tenths, wait]
		// with CmdSetup minimum width 6.
		if (pCmd.Parameters.Count < 6)
		{
			Malformed("Flash screen");
			return;
		}
		if (_presentation == null)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Flash screen: presentation state unavailable");
			return;
		}
		var tenths = pCmd.Parameters[4];
		if (tenths < 0 || tenths > EventInterpreterMaxTenths)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Flash screen: invalid duration {tenths} skipped");
			return;
		}
		var subcommand = pCmd.Parameters.Count > 6 ? pCmd.Parameters[6] : FlashSubOnce;
		if (subcommand is not (FlashSubOnce or FlashSubBegin or FlashSubEnd))
		{
			_state.AddDiagnostic($"[Event {_eventId}] Flash screen: unsupported subcommand {subcommand} skipped");
			return;
		}
		if (subcommand == FlashSubEnd)
		{
			_presentation.FlashEnd();
			_state.AddDiagnostic($"[Event {_eventId}] Flash screen: ended");
			return;
		}
		var frames = TenthsToFrames(tenths);
		if (!_presentation.FlashOnce(pCmd.Parameters[0], pCmd.Parameters[1], pCmd.Parameters[2], pCmd.Parameters[3], frames))
		{
			_state.AddDiagnostic($"[Event {_eventId}] Flash screen: parameters outside bounds skipped");
			return;
		}
		_state.AddDiagnostic($"[Event {_eventId}] Flash screen: {frames} frames");
		// SetupWait treats a zero duration as a single frame.
		if (pCmd.Parameters[5] != 0)
		{
			WaitForFrames(tenths <= 0 ? 1 : frames);
		}
	}

	private void ExecuteShakeScreen(Rm2kMap.EventCommand pCmd)
	{
		// EasyRPG CommandShakeScreen: [strength, speed, tenths, wait]
		// with CmdSetup minimum width 4.
		if (pCmd.Parameters.Count < 4)
		{
			Malformed("Shake screen");
			return;
		}
		if (_presentation == null)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Shake screen: presentation state unavailable");
			return;
		}
		var tenths = pCmd.Parameters[2];
		if (tenths < 0 || tenths > EventInterpreterMaxTenths)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Shake screen: invalid duration {tenths} skipped");
			return;
		}
		var subcommand = pCmd.Parameters.Count > 4 ? pCmd.Parameters[4] : ShakeSubOnce;
		if (subcommand is not (ShakeSubOnce or ShakeSubBegin or ShakeSubEnd))
		{
			_state.AddDiagnostic($"[Event {_eventId}] Shake screen: unsupported subcommand {subcommand} skipped");
			return;
		}
		if (subcommand == ShakeSubEnd || tenths == 0)
		{
			// EasyRPG treats a zero duration as ending the shake.
			_presentation.ShakeEnd();
			_state.AddDiagnostic($"[Event {_eventId}] Shake screen: ended");
			return;
		}
		var frames = TenthsToFrames(tenths);
		if (!_presentation.ShakeOnce(pCmd.Parameters[0], pCmd.Parameters[1], frames))
		{
			_state.AddDiagnostic($"[Event {_eventId}] Shake screen: parameters outside bounds skipped");
			return;
		}
		_state.AddDiagnostic($"[Event {_eventId}] Shake screen: {frames} frames");
		if (pCmd.Parameters[3] != 0)
		{
			WaitForFrames(frames);
		}
	}

	private void ExecuteWeatherEffects(Rm2kMap.EventCommand pCmd)
	{
		// EasyRPG CommandWeatherEffects: [type, strength] with minimum width 2.
		if (pCmd.Parameters.Count < 2)
		{
			Malformed("Weather effects");
			return;
		}
		if (_presentation == null)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Weather effects: presentation state unavailable");
			return;
		}
		// EasyRPG clamps the strength to 2 and folds unknown RM2K types to 0.
		var strength = Math.Min(pCmd.Parameters[1], PresentationState.MaxWeatherStrength);
		var type = pCmd.Parameters[0] > PresentationState.MaxWeatherType ? 0 : pCmd.Parameters[0];
		if (type < 0 || !_presentation.SetWeather(type, strength))
		{
			_state.AddDiagnostic($"[Event {_eventId}] Weather effects: type {pCmd.Parameters[0]} rejected");
			return;
		}
		_state.AddDiagnostic($"[Event {_eventId}] Weather effects: type {type} strength {strength}");
	}

	private void ExecuteControlSwitches(Rm2kMap.EventCommand pCmd)
	{
		if (pCmd.Parameters.Count < 4)
		{
			Malformed("Control switches");
			return;
		}
		// EasyRPG Game_Interpreter_Shared::TargetEvalMode: parameters[0] selects
		// the lvalue form, [1] is the first id and [2] the range end.
		var targetMode = pCmd.Parameters[0];
		var startId = pCmd.Parameters[1];
		var endId = targetMode == TargetEvalRange ? pCmd.Parameters[2] : startId;
		var mode = pCmd.Parameters[3];
		if (targetMode != TargetEvalSingle && targetMode != TargetEvalRange)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Control switches: unsupported target mode {targetMode} skipped");
			return;
		}
		if (startId < 1 || endId < startId || endId > GameSimulationState.MaxSwitches)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Control switches: invalid range {startId}-{endId} skipped");
			return;
		}
		if (mode is not (SwitchModeOn or SwitchModeOff or SwitchModeFlip))
		{
			_state.AddDiagnostic($"[Event {_eventId}] Control switches: unknown mode {mode} skipped");
			return;
		}
		for (var id = startId; id <= endId; id++)
		{
			while (_state.Switches.Count < id)
			{
				_state.Switches.Add(false);
			}
			switch (mode)
			{
				case SwitchModeOn:
					_state.Switches[id - 1] = true;
					break;
				case SwitchModeOff:
					_state.Switches[id - 1] = false;
					break;
				default:
					_state.Switches[id - 1] = !_state.Switches[id - 1];
					break;
			}
		}
		var effect = mode switch
		{
			SwitchModeOn => "ON",
			SwitchModeOff => "OFF",
			_ => "FLIP",
		};
		_state.AddDiagnostic($"[Event {_eventId}] Switches {startId}-{endId} -> {effect}");
	}

	private void ExecuteChangeGold(Rm2kMap.EventCommand pCmd)
	{
		if (pCmd.Parameters.Count < 3)
		{
			Malformed("Change gold");
			return;
		}
		var operation = pCmd.Parameters[0];
		var operandType = pCmd.Parameters[1];
		var operandValue = pCmd.Parameters[2];
		if (operation is not (0 or 1))
		{
			_state.AddDiagnostic($"[Event {_eventId}] Change gold: unsupported operation {operation} skipped");
			return;
		}

		int operand;
		switch (operandType)
		{
			case VarOperandConstant:
				operand = operandValue;
				break;
			case VarOperandVariable:
				if (operandValue < 1 || operandValue > GameSimulationState.MaxVariables)
				{
					_state.AddDiagnostic($"[Event {_eventId}] Change gold: invalid variable operand {operandValue} skipped");
					return;
				}
				operand = GetVariable(operandValue);
				break;
			default:
				_state.AddDiagnostic($"[Event {_eventId}] Change gold: unsupported operand type {operandType} skipped");
				return;
		}

		var current = (long)_state.Gold;
		var result = operation switch
		{
			GoldOpAdd => current + operand,
			GoldOpSubtract => current - operand,
			_ => current,
		};
		_state.Gold = (int)Math.Clamp(result, 0L, (long)MaxGold);
		_state.AddDiagnostic($"[Event {_eventId}] Gold <- op {operation} {operand}: {_state.Gold}");
	}

	private void ExecuteChangeItems(Rm2kMap.EventCommand pCmd)
	{
		if (pCmd.Parameters.Count < 5)
		{
			Malformed("Change items");
			return;
		}

		var operation = pCmd.Parameters[0];
		var itemMode = pCmd.Parameters[1];
		var itemValue = pCmd.Parameters[2];
		var operandType = pCmd.Parameters[3];
		var operandValue = pCmd.Parameters[4];
		if (operation is not (ItemOpAdd or ItemOpSubtract))
		{
			_state.AddDiagnostic($"[Event {_eventId}] Change items: unsupported operation {operation} skipped");
			return;
		}

		var itemId = itemMode switch
		{
			ItemIdConstant => itemValue,
			ItemIdVariable => GetVariable(itemValue),
			_ => -1,
		};
		if (itemMode is not (ItemIdConstant or ItemIdVariable) || itemId < 1 || itemId > MaxItemId)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Change items: invalid item id {itemId} skipped");
			return;
		}
		if (itemMode == ItemIdVariable && (itemValue < 1 || itemValue > GameSimulationState.MaxVariables))
		{
			_state.AddDiagnostic($"[Event {_eventId}] Change items: invalid item variable {itemValue} skipped");
			return;
		}
		if (operandType == VarOperandVariable && (operandValue < 1 || operandValue > GameSimulationState.MaxVariables))
		{
			_state.AddDiagnostic($"[Event {_eventId}] Change items: invalid amount variable {operandValue} skipped");
			return;
		}
		if (operandType is not (VarOperandConstant or VarOperandVariable))
		{
			_state.AddDiagnostic($"[Event {_eventId}] Change items: invalid operand type {operandType} skipped");
			return;
		}
		var amount = operandType == VarOperandVariable ? GetVariable(operandValue) : operandValue;
		if (amount < 0)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Change items: negative amount {amount} skipped");
			return;
		}

		var current = _state.ItemCounts.TryGetValue(itemId, out var count) ? count : 0;
		var delta = operation == ItemOpSubtract ? -(long)amount : amount;
		var result = Math.Clamp((long)current + delta, 0L, (long)MaxItemCount);
		_state.ItemCounts[itemId] = (int)result;
		_state.AddDiagnostic($"[Event {_eventId}] Item {itemId} count <- op {operation} {amount}: {result}");
	}

	private void ExecuteChangePartyMembers(Rm2kMap.EventCommand pCmd)
	{
		if (pCmd.Parameters.Count < 3)
		{
			Malformed("Change party members");
			return;
		}

		var operation = pCmd.Parameters[0];
		var actorMode = pCmd.Parameters[1];
		var actorValue = pCmd.Parameters[2];
		if (operation is not (PartyOpAdd or PartyOpRemove))
		{
			_state.AddDiagnostic($"[Event {_eventId}] Change party members: unsupported operation {operation} skipped");
			return;
		}

		int actorId;
		switch (actorMode)
		{
			case ActorIdConstant:
				actorId = actorValue;
				break;
			case ActorIdVariable:
				if (actorValue < 1 || actorValue > GameSimulationState.MaxVariables)
				{
					_state.AddDiagnostic($"[Event {_eventId}] Change party members: invalid actor variable {actorValue} skipped");
					return;
				}
				actorId = GetVariable(actorValue);
				break;
			default:
				_state.AddDiagnostic($"[Event {_eventId}] Change party members: unsupported actor mode {actorMode} skipped");
				return;
		}

		if (actorId < 1 || actorId > GameSimulationState.MaxActorId)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Change party members: invalid actor id {actorId} skipped");
			return;
		}

		var existingIndex = -1;
		for (var index = 0; index < _state.PartyMemberIds.Count; index++)
		{
			if (_state.PartyMemberIds[index] == actorId)
			{
				existingIndex = index;
				break;
			}
		}

		if (operation == PartyOpAdd)
		{
			if (existingIndex >= 0)
			{
				_state.AddDiagnostic($"[Event {_eventId}] Change party members: actor {actorId} already in party");
				return;
			}
			if (_state.PartyMemberIds.Count >= GameSimulationState.MaxPartyMembers)
			{
				_state.AddDiagnostic($"[Event {_eventId}] Change party members: party full, actor {actorId} skipped");
				return;
			}
			_state.PartyMemberIds.Add(actorId);
		}
		else
		{
			if (existingIndex < 0)
			{
				_state.AddDiagnostic($"[Event {_eventId}] Change party members: actor {actorId} not in party");
				return;
			}
			_state.PartyMemberIds.RemoveAt(existingIndex);
			if (_state.ActiveActorIndex >= _state.PartyMemberIds.Count)
			{
				_state.ActiveActorIndex = Math.Max(0, _state.PartyMemberIds.Count - 1);
			}
		}

		_state.AddDiagnostic($"[Event {_eventId}] Party <- op {operation} actor {actorId}");
	}

	private void ExecuteControlVars(Rm2kMap.EventCommand pCmd)
	{
		if (pCmd.Parameters.Count < 6)
		{
			Malformed("Control variables");
			return;
		}
		var targetMode = pCmd.Parameters[0];
		var startId = pCmd.Parameters[1];
		var endId = targetMode == TargetEvalRange ? pCmd.Parameters[2] : startId;
		var op = pCmd.Parameters[3];
		var operandType = pCmd.Parameters[4];
		var operandValue = pCmd.Parameters[5];

		if (targetMode != TargetEvalSingle && targetMode != TargetEvalRange)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Control variables: unsupported target mode {targetMode} skipped");
			return;
		}
		if (startId < 1 || endId < startId || endId > GameSimulationState.MaxVariables)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Control variables: invalid range {startId}-{endId} skipped");
			return;
		}
		if (op < VarOpSet || op > VarOpMod)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Control variables: unsupported operation {op} skipped");
			return;
		}
		int operand;
		switch (operandType)
		{
			case VarOperandConstant:
				operand = operandValue;
				break;
			case VarOperandVariable:
				operand = GetVariable(operandValue);
				break;
			case VarOperandVariableIndirect:
				// EasyRPG ValueOrVariable mode 2: v[v[x]].
				if (operandValue < 1 || operandValue > GameSimulationState.MaxVariables)
				{
					_state.AddDiagnostic($"[Event {_eventId}] Control variables: invalid indirect variable {operandValue} skipped");
					return;
				}
				operand = GetVariable(GetVariable(operandValue));
				break;
			default:
				_state.AddDiagnostic($"[Event {_eventId}] Control variables: unsupported operand type {operandType} skipped");
				return;
		}
		if ((op == VarOpDiv || op == VarOpMod) && operand == 0)
		{
			_state.AddDiagnostic($"[Event {_eventId}] Control variables: division by zero skipped");
			return;
		}
		for (var id = startId; id <= endId; id++)
		{
			while (_state.Variables.Count < id)
			{
				_state.Variables.Add(0);
			}
			var index = id - 1;
			var current = _state.Variables[index];
			_state.Variables[index] = op switch
			{
				VarOpSet => operand,
				VarOpAdd => current + operand,
				VarOpSub => current - operand,
				VarOpMul => current * operand,
				VarOpDiv => current / operand,
				_ => current % operand,
			};
		}
		_state.AddDiagnostic($"[Event {_eventId}] Variables {startId}-{endId} <- op {op} {operand}");
	}

	private void ExecuteTeleport(Rm2kMap.EventCommand pCmd)
	{
		// Code 10810 "Place Hero": [0]=map id, [1]=x, [2]=y, optional [3]=facing (2k3).
		if (pCmd.Parameters.Count < 3)
		{
			Malformed("Transfer player");
			return;
		}
		var mapId = pCmd.Parameters[0];
		var x = pCmd.Parameters[1];
		var y = pCmd.Parameters[2];
		var facing = pCmd.Parameters.Count >= 4 ? pCmd.Parameters[3] : (int)_state.FacingDirection;
		if (mapId < 1 || mapId > GameSimulationState.MaxMapId || x < 0 || y < 0 || facing is not (2 or 4 or 6 or 8))
		{
			_state.AddDiagnostic($"[Event {_eventId}] Transfer player: invalid target ({mapId}, {x}, {y}) skipped");
			return;
		}
		_state.FacingDirection = (byte)facing;
		_state.PendingMapId = mapId;
		_state.PendingX = x;
		_state.PendingY = y;
		_state.IsTransferPending = true;
		_state.AddDiagnostic($"[Event {_eventId}] Transfer pending -> map {mapId} at ({x}, {y})");
	}

	private void ExecuteConditionalBranch()
	{
		if (!EvaluateCondition())
		{
			var elseIndex = FindConditionalBoundary(_commandIndex, true);
			var target = elseIndex ?? FindConditionalBoundary(_commandIndex, false);
			// Jump onto the Else/End marker; the frame loop advances past it.
			_commandIndex = target ?? _commands.Count - 1;
		}
	}

	private bool EvaluateCondition()
	{
		var cmd = _commands[_commandIndex];
		switch (Param(cmd, 0))
		{
			case ConditionSwitch:
				if (cmd.Parameters.Count < 3)
				{
					Malformed("ConditionalBranch");
					return false;
				}
				return GetSwitch(Param(cmd, 1)) == (Param(cmd, 2) == 0);
			case ConditionVariable:
				if (cmd.Parameters.Count < 5)
				{
					Malformed("ConditionalBranch");
					return false;
				}
				var left = GetVariable(Param(cmd, 1));
				var right = Param(cmd, 2) == VarOperandConstant
					? Param(cmd, 3)
					: GetVariable(Param(cmd, 3));
				return CompareValues(left, right, Param(cmd, 4));
			default:
				// Timer/gold/item/actor conditions need party/timer state that
				// the deterministic core does not model yet; EasyRPG treats an
				// unevaluable branch as false (else path).
				_state.AddDiagnostic($"[Event {_eventId}] ConditionalBranch type {Param(cmd, 0)} not supported yet; taking else branch");
				return false;
		}
	}

	private bool CompareValues(int pLeft, int pRight, int pOperator)
	{
		switch (pOperator)
		{
			case BranchOpEqual: return pLeft == pRight;
			case BranchOpGreaterOrEqual: return pLeft >= pRight;
			case BranchOpLessOrEqual: return pLeft <= pRight;
			case BranchOpGreater: return pLeft > pRight;
			case BranchOpLess: return pLeft < pRight;
			case BranchOpNotEqual: return pLeft != pRight;
			default:
				Malformed("ConditionalBranch");
				return false;
		}
	}

	/// <summary>
	/// Finds the ElseBranch or EndBranch belonging to this ConditionalBranch,
	/// skipping nested branch blocks by depth counting.
	/// </summary>
	private int? FindConditionalBoundary(int pFrom, bool pSearchElse)
	{
		var depth = 0;
		for (var i = pFrom + 1; i < _commands.Count; i++)
		{
			var code = _commands[i].Code;
			if (code == ConditionalBranch)
			{
				depth++;
			}
			else if (code == EndBranch)
			{
				if (depth == 0 && !pSearchElse)
				{
					return i;
				}
				depth--;
			}
			else if (code == ElseBranch && depth == 0 && pSearchElse)
			{
				return i;
			}
		}
		return null;
	}

	private void ExecuteElseBranch()
	{
		// Reached only when the condition was true and the then-body ran to
		// its end: skip the else block by jumping onto the matching EndBranch.
		_commandIndex = FindConditionalBoundary(_commandIndex, false) ?? _commandIndex;
	}

	private void ExecuteEndBranch()
	{
		// Structured block end; the frame loop advances past it.
	}

	private void ExecuteBreakLoop()
	{
		_loopStack.Clear();
		var endLoop = FindMatchingBranch(_commandIndex, EndLoop);
		if (endLoop.HasValue)
		{
			_commandIndex = endLoop.Value + 1;
		}
		else
		{
			// No matching EndLoop (RPG_RT tolerates this): run to the end.
			_commandIndex = _commands.Count;
		}
	}

	private void ExecuteEndLoop()
	{
		if (_loopStack.Count > 0)
		{
			// Jump to the first body command; the Loop entry stays on the
			// stack so nesting depth stays bounded without re-pushing.
			_commandIndex = _loopStack.Peek() + 1;
		}
		else
		{
			// End without a matching Loop: skip safely.
			_commandIndex++;
		}
	}

	private int? FindMatchingBranch(int pFrom, int pCode)
	{
		for (var i = pFrom + 1; i < _commands.Count; i++)
		{
			if (_commands[i].Code == pCode)
			{
				return i;
			}
		}
		return null;
	}

	private int GetVariable(int pId)
	{
		if (pId < 1 || pId > _state.Variables.Count)
		{
			return 0;
		}
		return _state.Variables[pId - 1];
	}

	private bool GetSwitch(int pId)
	{
		return pId >= 1 && pId <= _state.Switches.Count && _state.Switches[pId - 1];
	}

	private static int Param(Rm2kMap.EventCommand pCmd, int pIndex)
	{
		return pIndex < pCmd.Parameters.Count ? pCmd.Parameters[pIndex] : 0;
	}

	private void Malformed(string pCommand)
	{
		_state.AddDiagnostic($"[Event {_eventId}] {pCommand}: malformed parameters skipped");
	}


private void ExecuteShowInn(Rm2kMap.EventCommand pCmd)
	{
		// CmdSetup minimum width 3.
		if (pCmd.Parameters.Count < 3)
		{
			Malformed("Show inn");
			return;
		}

		var art = pCmd.Parameters[0];
		// **The price is the second parameter, and the reference reads it in
		// the command's first two lines.**
		var preis = pCmd.Parameters[1];
		// **And a price of zero skips the prompt** — the reference has its
		// own branch for it, so a game's free inn never opens a window.
		var zeigtPreis = preis != 0;
		_state.OpenInn(art, preis);
		_state.ShouldShowInnPrice = zeigtPreis;
		_state.ActiveShopOption = GameSimulationState.ShopOption.InnStay;
		_state.AddDiagnostic(
			$"[Event {_eventId}] Show inn: type {art}, {preis} gold"
			+ (zeigtPreis ? string.Empty : ", and the prompt is skipped"));
	}
}
