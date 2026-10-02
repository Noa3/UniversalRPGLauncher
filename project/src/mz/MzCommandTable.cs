using System;
using System.Collections.Generic;

namespace UniversalRPG.Web;

/// <summary>
/// What a number in an MZ event command list is, and which command owns it.
/// </summary>
/// <remarks>
/// <para>
/// A command list holds two kinds of number and telling them apart is the whole
/// of reading one. A <b>command</b> is dispatched to a method of its own, and
/// <see cref="MzCommandName"/> names every one of them as the engine does. A
/// <b>data</b> number carries what a command above it works on: a line of text
/// under a message, a purchase under a shop, a line of script under a script
/// block. The engine never dispatches those to a method, it reads them inside the
/// command above, and a reader that treated them as commands would run a line of
/// dialogue as if it were an instruction.
/// </para>
/// <para>
/// <b>The owner of each data number was read out of the engine, not derived.</b>
/// Four of the five are three hundred above their command — 401 under 101, 405
/// under 105, 408 under 108, 655 under 355 — and one is not: <b>605 belongs to
/// 302, and three hundred above it is 305, which is a different command
/// entirely.</b> A reader that used the rule rather than the measurement would
/// attach a shop's purchase list to the wrong command, and the rule is not a
/// rule, it is a coincidence in four cases out of five.
/// </para>
/// </remarks>
public static class MzCommandTable
{
    public const int Loop = 112;
    public const int BreakLoop = 113;
    public const int ExitEventProcessing = 115;
    public const int Label = 118;
    public const int JumpToLabel = 119;
    public const int ControlSwitches = 121;
    public const int ControlVariables = 122;
    public const int EndBranch = 412;
    /// <summary>The end of a branch, which the editor writes and which
    /// the engine steps over because it has no method for it.</summary>
    public const int Else = 411;
    public const int RepeatAbove = 413;

    /// <summary>Calls a common event by the index it has in
    /// <c>CommonEvents.json</c>. The engine makes a child interpreter for it,
    /// and the list that called it waits until the child has finished.</summary>
    public const int CommonEvent = 117;

    /// <summary>Puts a picture on the screen. The engine makes a new picture
    /// and puts it in the slot, so anything the old one had is gone with it.</summary>
    /// <summary>Scroll Text. Its choices follow in 405 entries.</summary>
    /// <remarks>
    /// <strong>And its rules are measured at
    /// <c>Game_Interpreter.prototype.command105</c>:</strong> <c>if
    /// ($gameMessage.isBusy()) return false;
    /// $gameMessage.setScroll(params[0], params[1]); while
    /// (this.nextEventCode() === 405) { this._index++;
    /// $gameMessage.add(this.currentCommand().parameters[0]); }
    /// this.setWaitMode("message"); return true;</c> — <strong>and the
    /// <c>while</c> is the engine's own, so the interpreter steps once
    /// over its own 405 lines and not again.</strong>
    /// </remarks>
    public const int ScrollText = 105;

    /// <summary>Screen Shake, and the fourth parameter waits for it.</summary>
    /// <remarks>
    /// <strong>And its rules are measured at
    /// <c>command225</c>:</strong> <c>$gameScreen.startShake(params[0],
    /// params[1], params[2]); if (params[3]) this.wait(params[2]);</c> —
    /// <strong>and <c>startShake</c> stores power, speed and duration, and
    /// <c>updateShake</c> moves by <c>(power * speed * direction) / 10</c>
    /// and reverses at <c>±power * 2</c>.</strong>
    /// </remarks>
    public const int ScreenShake = 225;

    /// <summary>Flash the screen. Its fourth parameter waits for it.</summary>
    /// <remarks>
    /// <strong>And measured at <c>command224</c>:</strong>
    /// <c>$gameScreen.flashWhite(params[0], params[1]); if (params[2])
    /// this.wait(params[1]);</c>
    /// </remarks>
    public const int ScreenFlash = 224;

    /// <summary>Recover all of an actor, or of the whole party.</summary>
    /// <remarks>
    /// <strong>And its rules are measured at
    /// <c>Game_Interpreter.prototype.command314</c>:</strong> <c>this
    /// .iterateActorEx(params[0], params[1], actor =&gt; { actor.recoverAll();
    /// });</c> — <strong>and <c>iterateActorEx</c> walks the party when
    /// <c>params[1]</c> is true and one actor when it is false.</strong>
    /// </remarks>
    public const int RecoverAll = 314;

    public const int ShowPicture = 231;

    /// <summary>Moves a picture to a new place over a number of frames. It
    /// sets a target, so a move of zero frames changes nothing at all.</summary>
    public const int MovePicture = 232;

    /// <summary>Removes a picture from the screen.</summary>
    public const int ErasePicture = 235;

    /// <summary>
    /// A dialogue, and <b>the lines under it are eaten by this command rather
    /// than dispatched</b>: <c>while (this.nextEventCode() === 401) {
    /// this._index++; $gameMessage.add(…); }</c>. There is no
    /// <c>command401</c> for them to be dispatched to, because this one does
    /// it.
    /// </summary>
    /// <remarks>
    /// **And it refuses a second dialogue while one is up**, and takes one
    /// of 102, 103 and 104 — but only the one directly after its last line.
    /// </remarks>
    public const int ShowDialogue = 101;

    /// <summary>
    /// A block of the author's own notes. **It runs nothing.**
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And its continuation lines are <c>408</c>, not
    /// <c>0</c>.</strong> That is the measurement, and it is why the command
    /// table has an owner for <c>408</c> and not for <c>0</c>:
    /// <c>0</c> is a comment, and a comment is <c>0</c> in MZ and MV and
    /// <c>108</c> elsewhere.
    /// </para>
    /// <para>
    /// <strong>And <c>108</c> was not in this table until a finished game's
    /// command list asked for it.</strong> Measured at
    /// <c>D:/Itch/sister/www</c>: 6750 <c>108</c> and 1908 <c>408</c>, **and
    /// those are the two commonest numbers in the whole game after
    /// <c>355</c>.</strong>
    /// </para>
    /// </remarks>
    public const int Comment = 108;

    /// <summary>One line of a comment block, under <see cref="Comment"/>.</summary>
    public const int CommentLine = 408;

    /// <summary>
    /// The party's money, by whatever route.
    /// </summary>
    /// <remarks>
    /// <strong>And <c>command125</c> is three lines:</strong>
    /// <code>
    /// const value = this.operateValue(params[0], params[1], params[2]);
    /// $gameParty.gainGold(value);
    /// </code>
    /// <strong>and the operand is the same reader <c>122</c> uses</strong>,
    /// because the engine has one <c>operateValue</c> and not three.
    /// </remarks>
    public const int ChangeGold = 125;

    /// <summary>
    /// A film from the <c>movies</c> folder, and the run waits for it.
    /// </summary>
    /// <remarks>
    /// <strong>And <c>command261</c> returns <c>false</c> in every
    /// frame</strong>, <strong>so it is called again and again until the
    /// video has ended, and <c>this._index++</c> is inside the
    /// <c>if</c> -- which means the wait is only armed when a name is
    /// there.</strong>
    /// </remarks>
    public const int PlayMovie = 261;

    /// <summary>
    /// A call into a plugin, and one string carries the whole of it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this is not <see cref="PluginCommand"/> under another
    /// number.</strong> <c>357</c> is MZ's numbered form with a plugin file
    /// name, a command and its arguments as separate parameters;
    /// <strong><c>356</c> is MV's and it is one string that the engine splits
    /// itself:</strong>
    /// </para>
    /// <code>
    /// command356() {
    ///     const args = this._params[0].split(" ");
    ///     const command = args.shift();
    ///     this.pluginCommand(command, args);
    ///     return true;
    /// }
    /// </code>
    /// <para>
    /// <strong>And measured at <c>D:/Itch/sister/www</c>: five thousand four
    /// hundred and seventy-two <c>356</c>, every one with exactly one
    /// parameter, and not one <c>357</c> in the whole game.</strong>
    /// </para>
    /// </remarks>
    public const int PluginCommandCall = 356;
    /// <summary>
    /// The colour the screen is washed in, and how long that takes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the engine's line is
    /// <c>$gameScreen.startTint(this._params[0], this._params[1])</c>, and
    /// then <c>if (this._params[2]) { this.wait(this._params[1]); }</c>
    /// -- so the third parameter is not a colour and not a duration but
    /// <strong>whether the page waits for the tint</strong>.</strong>
    /// <strong>And a reader that read it as a number would wait when the
    /// game said not to and not wait when it said yes.</strong>
    /// </para>
    /// </remarks>
    /// <summary>
    /// A state goes onto an actor or comes off.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <code>
    /// command313() {
    ///     this.iterateActorEx(this._params[0], this._params[1], actor => {
    ///         const alreadyDead = actor.isDead();
    ///         if (this._params[2] === 0) {
    ///             actor.addState(this._params[3]);
    ///         } else {
    ///             actor.removeState(this._params[3]);
    ///         }
    ///         if (actor.isDead() &amp;&amp; !alreadyDead) {
    ///             actor.performCollapse();
    ///         }
    ///         actor.clearResult();
    ///     });
    ///     return true;
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// <strong>And the third parameter is the direction and not a
    /// number:</strong> <strong>zero adds the state and anything else takes
    /// it away.</strong> <strong>And this game writes
    /// <c>[0, 2, 0, 25]</c>, <c>[0, 2, 0, 26]</c>, <c>[0, 2, 0, 28]</c>
    /// three hundred and nineteen times</strong> -- <strong>which are
    /// poisons, sleep and illnesses, and every one of them is
    /// added.</strong>
    /// </para>
    /// <para>
    /// <strong>And <c>alreadyDead</c> is read before the change</strong>,
    /// <strong>so only an actor who was alive and is now dead
    /// collapses</strong> -- <strong>and that is the difference between
    /// "he just died" and "he was dead already and a state keeps
    /// him dead".</strong>
    /// </para>
    /// </remarks>
    /// <summary>
    /// <c>command311</c> -- <c>iterateActorEx</c> plus
    /// <c>changeHp</c>.
    /// </summary>
    /// <summary>
    /// <c>command312</c> -- <c>operateValue(params[2], params[3],
    /// params[4])</c> then <c>actor.gainMp(value)</c>.
    /// </summary>
    public const int ChangeMp = 312;

    /// <summary>
    /// <c>command315</c> -- <c>actor.changeExp(actor.currentExp() +
    /// value, params[5])</c>.
    /// </summary>
    public const int ChangeExp = 315;

    /// <summary>
    /// <c>command316</c> -- <c>actor.changeLevel(actor.level + value,
    /// params[5])</c>.
    /// </summary>
    public const int ChangeLevel = 316;

    /// <summary>
    /// <c>command317</c> -- <c>actor.addParam(params[2], value)</c>.
    /// </summary>
    public const int ChangeParameter = 317;

    /// <summary>
    /// <c>command233</c> -- <c>$gameScreen.rotatePicture(params[0],
    /// params[1])</c>.
    /// </summary>
    public const int RotatePicture = 233;

    /// <summary>
    /// <c>command236</c> -- <c>$gameScreen.changeWeather(params[0],
    /// params[1], params[2])</c>, and the fourth says whether to wait.
    /// </summary>
    public const int SetWeatherEffect = 236;

    /// <summary>
    /// <c>command283</c> -- <c>$gameMap.changeBattleback(params[0],
    /// params[1])</c>.
    /// </summary>
    public const int ChangeBattleback = 283;

    /// <summary>
    /// <c>command303</c> -- the name editor, and it checks
    /// <c>$dataActors[params[0]]</c> first.
    /// </summary>
    public const int ChangeActorName = 303;

    /// <summary>
    /// <c>command320</c> -- <c>actor.setName(this._params[1])</c>.
    /// </summary>
    public const int ChangeName = 320;

    /// <summary>
    /// <c>command331</c> -- $gameTroop.members()[n].setHp(value, this._params[0] < 0).
    /// </summary>
    public const int ChangeEnemyHp = 331;

    /// <summary>
    /// <c>command332</c> -- enemy.gainMp(value).
    /// </summary>
    public const int ChangeEnemyMp = 332;

    /// <summary>
    /// <c>command333</c> -- enemy.gainTp(value).
    /// </summary>
    public const int ChangeEnemyState = 333;

    /// <summary>
    /// <c>command334</c> -- <c>enemy.recoverAll()</c> and nothing else:
    /// no index, no state, no value.
    /// </summary>
    public const int EnemyRecoverAll = 334;

    /// <summary>
    /// <c>command335</c> -- <c>enemy.appear();</c> and then
    /// <c>$gameTroop.makeUniqueNames();</c>, and that is two calls.
    /// </summary>
    public const int EnemyAppear = 335;




    /// <summary>
    /// <c>command337</c> -- enemy.transform(params[1]).
    /// </summary>
    public const int ShowBattleAnimation = 337;

    /// <summary>
    /// <c>command339</c> -- <c>iterateBattler</c>, and it asks
    /// <c>isDeathStateAffected()</c> before forcing an action.
    /// </summary>
    public const int ForceAction = 339;

    /// <summary>
    /// <c>command340</c> -- <c>BattleManager.abort();</c> and nothing
    /// else, and it is not a damage command.
    /// </summary>
    public const int AbortBattle = 340;



    /// <summary>
    /// <c>command601</c> -- _branch !== 0.
    /// </summary>
    public const int BattleWin = 601;

    /// <summary>
    /// <c>command602</c> -- _branch !== 1.
    /// </summary>
    public const int BattleEscape = 602;

    /// <summary>
    /// <c>command603</c> -- _branch !== 2.
    /// </summary>
    public const int BattleLose = 603;

    /// <summary>
    /// <c>command103</c> -- <c>$gameMessage.setNumberInput(params[0],
    /// params[1])</c>, and it waits for the answer.
    /// </summary>
    public const int InputNumber = 103;

    /// <summary>
    /// <c>command281</c> -- <c>if (this._params[0] === 0) {
    /// $gameMap.enableNameDisplay(); } else {
    /// $gameMap.disableNameDisplay(); }</c>
    /// </summary>
    public const int ChangeMapNameDisplay = 281;

    /// <summary>
    /// <c>command282</c> -- <c>$gameMap.changeTileset(this._params[0])</c>,
    /// and it waits for the tileset images.
    /// </summary>
    public const int ChangeTileset = 282;

    /// <summary>
    /// <c>command284</c> -- <c>$gameMap.changeParallax(params[0], params[1],
    /// params[2], params[3], params[4])</c>.
    /// </summary>
    public const int ChangeParallax = 284;

    /// <summary>
    /// The goods line under a <c>302</c>, and not a command.
    /// </summary>
    /// <remarks>
    /// <strong>And <c>605</c> has no <c>command605</c> in the
    /// engine</strong> -- <strong>and <c>MzCommandSet.NoMethodCodes</c>
    /// says so</strong> -- <strong>and <c>command302</c> reads it
    /// itself</strong>: <c>while (this.nextEventCode() === 605) {
    /// this._index++; goods.push(this.currentCommand().parameters); }</c>.
    /// </remarks>
    public const int GoodsLine = 605;

    /// <summary>
    /// <c>command204</c> -- <c>if ($gameMap.isScrolling()) {
    /// this.setWaitMode('scroll'); return false; } $gameMap.startScroll(
    /// params[0], params[1], params[2]);</c>
    /// </summary>
    public const int ScrollMap = 204;

    /// <summary>
    /// <c>command234</c> -- <c>$gameScreen.tintPicture(params[0],
    /// params[1], params[2])</c>, and the fourth says whether to wait.
    /// </summary>
    public const int TintPicture = 234;

    /// <summary>
    /// <c>command302</c> -- the shop, and it reads the <c>605</c> lines
    /// that follow it.
    /// </summary>
    public const int ShopProcessing = 302;

    /// <summary>
    /// <c>command124</c> -- <c>if (this._params[0] === 0) {
    /// $gameTimer.start(this._params[1] * 60); } else { $gameTimer.stop(); }</c>
    /// </summary>
    public const int ControlTimer = 124;

    /// <summary>
    /// <c>command132</c> -- <c>$gameSystem._battleBgm properties and
    /// $gameSystem.saveBgm()</c>.
    /// </summary>
    public const int ChangeBattleBgm = 132;

    /// <summary>
    /// <c>command134</c> -- <c>if (this._params[0] === 0) {
    /// $gameSystem.disableSave(); } else { $gameSystem.enableSave(); }</c>
    /// </summary>
    public const int ChangeSaveAccess = 134;

    /// <summary>
    /// <c>command135</c> -- <c>if (this._params[0] === 0) {
    /// $gameSystem.disableMenu(); } else { $gameSystem.enableMenu(); }</c>
    /// </summary>
    public const int ChangeMenuAccess = 135;

    /// <summary>
    /// <c>command138</c> -- <c>$gameSystem.setWindowTone(this._params[0])</c>
    /// </summary>
    public const int ChangeWindowColor = 138;

    /// <summary>
    /// <c>command352</c> -- <c>if (!$gameParty.inBattle()) {
    /// SceneManager.push(Scene_Save); } return true;</c>
    /// </summary>
    public const int SaveGame = 352;

    /// <summary>
    /// <c>command354</c> -- <c>SceneManager.goto(Scene_Title); return
    /// true;</c>, and a <c>goto</c> and not a <c>push</c>.
    /// </summary>
    public const int ReturnToTitle = 354;

    /// <summary>
    /// <c>command353</c> -- <c>SceneManager.goto(Scene_Gameover);
    /// return true;</c>
    /// </summary>
    public const int GameOver = 353;

    /// <summary>
    /// <c>command321</c> -- <c>const actor = $gameActors.actor(params[0]);
    /// if (actor &amp;&amp; $dataClasses[params[1]]) { actor.changeClass(
    /// params[1], params[2]); } return true;</c>
    /// </summary>
    public const int ChangeClass = 321;

    /// <summary>
    /// <c>command319</c> -- <c>const actor = $gameActors.actor(params[0]);
    /// if (actor) { actor.changeEquipById(params[1], params[2]); } return
    /// true;</c>
    /// </summary>
    public const int ChangeEquipment = 319;

    public const int ChangeHp = 311;
    /// <summary>
    /// <c>command111</c> -- the branch itself, and the engine calls it `Conditional Branch`.
    /// </summary>
    public const int ConditionalBranch = 111;

    /// <summary>
    /// <c>command221</c> -- $gameScreen.startFadeOut(this.fadeSpeed()).
    /// </summary>
    public const int FadeoutScreen = 221;

    /// <summary>
    /// <c>command222</c> -- $gameScreen.startFadeIn(this.fadeSpeed()).
    /// </summary>
    public const int FadeinScreen = 222;



    /// <summary>
    /// <c>command336</c> -- enemy.transform(params[1]).
    /// </summary>
    public const int EnemyTransform = 336;




    /// <summary>
    /// <c>command133</c> -- $gameSystem.setVictoryMe(params[0]), and that is the song after a won fight and not the battle song.
    /// </summary>
    public const int ChangeVictoryMe = 133;

    /// <summary>
    /// <c>command136</c> -- disableEncounter / enableEncounter, and then $gamePlayer.makeEncounterCount(), and that third line is the whole difference from command137.
    /// </summary>
    public const int ChangeEncounter = 136;
    /// <summary>
    /// <c>command137</c> -- <c>disableFormation()</c> when the parameter
    /// is zero and <c>enableFormation()</c> otherwise, and there is no
    /// third line, which is the whole difference from
    /// <see cref="ChangeEncounter"/>.
    /// </summary>
    public const int ChangeFormationAccess = 137;



    /// <summary>
    /// <c>command139</c> -- $gameSystem.setDefeatMe(params[0]).
    /// </summary>
    public const int ChangeDefeatMe = 139;

    /// <summary>
    /// <c>command140</c> -- vehicle.setBgm(params[1]).
    /// </summary>
    public const int ChangeVehicleBgm = 140;

    /// <summary>
    /// <c>command202</c> -- vehicle.setLocation(mapId, x, y), and all three numbers can come out of variables.
    /// </summary>
    public const int SetVehicleLocation = 202;

    /// <summary>
    /// <c>command206</c> -- $gamePlayer.getOnOffVehicle(), and it takes no parameter at all.
    /// </summary>
    public const int GetOnOffVehicle = 206;

    /// <summary>
    /// <c>command285</c> -- terrainTag / eventIdXy / tileId / regionId into a variable.
    /// </summary>
    public const int GetLocationInfo = 285;

    /// <summary>
    /// <c>command323</c> -- vehicle.setImage(params[1], params[2]).
    /// </summary>
    public const int ChangeVehicleImage = 323;

    /// <summary>
    /// <c>command324</c> -- actor.setNickname(params[1]), and that is not the name.
    /// </summary>
    public const int ChangeNickname = 324;

    /// <summary>
    /// <c>command325</c> -- actor.setProfile(params[1]).
    /// </summary>
    public const int ChangeProfile = 325;

    /// <summary>
    /// <c>command326</c> -- actor.gainTp(operateValue(2, 3, 4)).
    /// </summary>
    public const int ChangeTp = 326;

    /// <summary>
    /// <c>command342</c> -- enemy.gainTp(operateValue(1, 2, 3)), and the minus of command340 is nowhere in it.
    /// </summary>
    public const int ChangeEnemyTp = 342;


    public const int ChangeActorState = 313;

    /// <summary>The end of a loop's body, and it is not <c>412</c>.</summary>
    /// <remarks>
    /// <para>
    /// <code>
    /// command403() {
    ///     if (this._branch[this._indent] &gt;= 0) {
    ///         this.skipBranch();
    ///     }
    ///     return true;
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// <strong>And the guard is <c>&gt;= 0</c> and not
    /// <c>== -1</c></strong>: <strong>a branch the interpreter has not
    /// decided is <c>0</c>, one it decided true is <c>1</c>, and only one
    /// it decided false is <c>-1</c>.</strong> <strong>So a <c>403</c> whose
    /// branch is still <c>0</c> skips, and one whose branch is <c>-1</c>
    /// does nothing.</strong>
    /// </para>
    /// <para>
    /// <strong>And <see cref="EndBranch"/> is <c>412</c>, which is the end
    /// of a branch and has no method of its own.</strong>
    /// </para>
    /// </remarks>
    public const int EndLoop = 403;

    /// <summary>Let the player hand over an item.</summary>
    /// <remarks>
    /// <para>
    /// <code>
    /// command104() {
    ///     if (!$gameMessage.isBusy()) {
    ///         this.setupItemChoice(this._params);
    ///         this._index++;
    ///         this.setWaitMode('message');
    ///     }
    ///     return false;
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// <strong>And it returns <c>false</c> in every frame</strong>, <strong>
    /// so it is asked again until the message is not busy any more</strong>,
    /// <strong>and the <c>this._index++</c> sits inside the
    /// <c>if</c></strong>.
    /// </para>
    /// <para>
    /// <strong>And <c>setupItemChoice(params)</c> is
    /// <c>$gameMessage.setItemChoice(params[0], params[1] || 2)</c>
    /// -- an item id and a category, not a column count</strong>, <strong>
    /// and the default is <c>2</c> and not <c>0</c></strong>, <strong>
    /// because <c>0 || 2</c> is <c>2</c> in JavaScript.</strong>
    /// </para>
    /// </remarks>
    public const int ShowItemChoice = 104;

    /// <summary>A skill goes onto an actor or comes off.</summary>
    /// <remarks>
    /// <para>
    /// <code>
    /// command318() {
    ///     this.iterateActorEx(this._params[0], this._params[1], actor => {
    ///         if (this._params[2] === 0) {
    ///             actor.learnSkill(this._params[3]);
    ///         } else {
    ///             actor.forgetSkill(this._params[3]);
    ///         }
    ///     });
    ///     return true;
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// <strong>And this is <c>313</c> with a skill for a state</strong>,
    /// <strong>and the form is the same in every slot.</strong>
    /// </para>
    /// </remarks>
    public const int ChangeActorSkill = 318;

    /// <summary>Put the background music aside so a later one can come
    /// back.</summary>
    /// <remarks>
    /// <para>
    /// <code>command243() { $gameSystem.saveBgm(); return true; }</code>
    /// </para>
    /// <para>
    /// <strong>And it carries no parameters at all</strong> -- <strong>and
    /// it changes no number a reader can check</strong>, <strong>and what
    /// it changes is that there is a remembered track.</strong>
    /// </para>
    /// </remarks>
    public const int SaveBgm = 243;

    /// <summary>Put the remembered music back, and it is <c>244</c>.</summary>
    /// <remarks>
    /// <strong>And <c>243</c> and <c>244</c> are a pair with no numbers in
    /// either</strong> -- <strong>and a reader that ran one and not the
    /// other leaves a track that the game can put back twice.</strong>
    /// </remarks>
    public const int RestoreBgm = 244;

    /// <summary>How many of a weapon the party has, and it is <c>128</c>
    /// with a weapon.</summary>
    /// <remarks>
    /// <para>
    /// <code>
    /// command127() {
    ///     const value = this.operateValue(this._params[1],
    ///                                  this._params[2],
    ///                                  this._params[3]);
    ///     $gameParty.gainItem($dataWeapons[this._params[0]],
    ///                        value, this._params[4]);
    ///     return true;
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// <strong>And <c>operateValue</c> starts at <c>params[1]</c>
    /// again</strong> -- <strong>the third command in this row where the
    /// first slot is something else</strong> -- <strong>and the container
    /// is <c>$dataWeapons</c>, which is neither <c>$dataItems</c> nor
    /// <c>$dataArmors</c>.</strong>
    /// </para>
    /// </remarks>
    public const int ChangeWeapon = 127;

    /// <summary>The player walks through walls and off the map.</summary>
    /// <remarks>
    /// <para>
    /// <code>
    /// command211() {
    ///     $gamePlayer.setTransparent(this._params[0] === 0);
    ///     return true;
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// <strong>And the parameter is inverted against its own
    /// name</strong>: <strong>zero makes the player transparent and
    /// anything else makes it solid</strong>, <strong>because the editor's
    /// checkbox says "Through Walls" and the engine reads the
    /// opposite.</strong>
    /// </para>
    /// </remarks>
    public const int PlayerTransparency = 211;

    /// <summary>Show the followers or take them off the screen.</summary>
    /// <remarks>
    /// <para>
    /// <code>
    /// command216() {
    ///     if (this._params[0] === 0) {
    ///         $gamePlayer.showFollowers();
    ///     } else {
    ///         $gamePlayer.hideFollowers();
    ///     }
    ///     $gamePlayer.refresh();
    ///     return true;
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// <strong>And this one is not inverted</strong> -- <strong>zero shows
    /// and anything else hides</strong> -- <strong>and it is the command
    /// right next to <see cref="PlayerTransparency"/>, which is
    /// inverted.</strong> <strong>Two neighbours and two directions is the
    /// pair a reader gets wrong.</strong>
    /// </para>
    /// </remarks>
    public const int ShowFollowers = 216;

    /// <summary>Walk the followers up to the player.</summary>
    /// <remarks>
    /// <para>
    /// <code>
    /// command217() {
    ///     if (!$gameParty.inBattle()) {
    ///         $gamePlayer.gatherFollowers();
    ///         this.setWaitMode('gather');
    ///     }
    ///     return true;
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// <strong>And it carries no parameters</strong>, <strong>it does
    /// nothing in a battle</strong>, <strong>and it waits for the walk with
    /// the engine's own <c>'gather'</c> and not with a frame count.</strong>
    /// </para>
    /// </remarks>
    public const int GatherFollowers = 217;

    /// <summary>
    /// An animation over a character, and the other one: <c>221</c> is over
    /// an event, this is over whatever character the page names.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <code>
    /// command212() {
    ///     this._character = this.character(this._params[0]);
    ///     if (this._character) {
    ///         this._character.requestAnimation(this._params[1]);
    ///         if (this._params[2]) {
    ///             this.setWaitMode('animation');
    ///         }
    ///     }
    ///     return true;
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// <strong>And the first parameter is a character and not an actor
    /// id.</strong> Measured: <c>[0, 157, false]</c>, <c>[0, 182,
    /// false]</c>, <c>[-1, 182, false]</c> -- <strong>and minus one is the
    /// player, exactly as for <see cref="ShowBalloonIcon"/>.</strong>
    /// </para>
    /// <para>
    /// <strong>And <c>requestAnimation</c> takes a number and no frame
    /// count</strong> -- <strong>the length is in the project's own
    /// <c>Animations.json</c> and is not invented here.</strong>
    /// </para>
    /// <para>
    /// <strong>And when the character is not there nothing happens and it
    /// is not an error</strong> -- <strong>the engine guards with <c>if
    /// (this._character)</c> and goes on.</strong>
    /// </para>
    /// </remarks>
    /// <summary>
    /// How many of a piece of armour the party has.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <code>
    /// command128() {
    ///     const value = this.operateValue(this._params[1],
    ///                                  this._params[2],
    ///                                  this._params[3]);
    ///     $gameParty.gainItem($dataArmors[this._params[0]],
    ///                        value, this._params[4]);
    ///     return true;
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// <strong>And <c>operateValue</c> starts at <c>params[1]</c> here and
    /// at <c>params[0]</c> in <c>125</c></strong>, <strong>because the
    /// first slot here is the armour and not the
    /// operation.</strong> <strong>And one reader that always started at
    /// zero would take the armour for the operation</strong> -- <strong>and
    /// measured at <c>D:/Itch/sister/www</c>: <c>[150, 0, 0, 1, false]</c>,
    /// <c>[27, 0, 0, 1, false]</c>, <c>[100, 0, 0, 1, false]</c>.</strong>
    /// </para>
    /// <para>
    /// <strong>And the container is <c>$dataArmors</c> and not
    /// <c>$dataItems</c></strong>, <strong>so the count belongs beside the
    /// item count and not in it.</strong>
    /// </para>
    /// </remarks>
    public const int ChangeArmor = 128;


    public const int ShowAnimation = 212;


    public const int ScreenTint = 223;

    /// <summary>
    /// Remove the event this page belongs to.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <code>
    /// command214() {
    ///     if (this.isOnCurrentMap() &amp;&amp; this._eventId &gt; 0) {
    ///         $gameMap.eraseEvent(this._eventId);
    ///     }
    ///     return true;
    /// }
    /// </code>
    /// </para>
    /// <para>
    /// <strong>And it carries no parameters at all</strong>, <strong>and it
    /// is the second most frequent command in the MV game this repository
    /// reads: one hundred and sixty-five uses.</strong> <strong>And it is
    /// the one that takes a chest or a sign or a door away after the player
    /// has had what was behind it.</strong>
    /// </para>
    /// <para>
    /// <strong>And <c>222</c> is a different command with a similar
    /// name</strong> -- <strong>that one removes the running event
    /// temporarily and until the party leaves the map</strong>, <strong>and
    /// this one removes it from the map's own list.</strong>
    /// </para>
    /// </remarks>
    public const int EraseEventFromMap = 214;

    /// <summary>
    /// One line of text. **It has no <c>command401</c> method** — the engine
    /// reads it by position, as the text of a 101's line, and
    /// <c>command401</c> does not exist.
    /// </summary>
    public const int ShowTextLine = 401;

    /// <summary>
    /// The choices under a line, again with no method of its own.
    /// </summary>
    /// <summary>
    /// The 102 under a dialogue: <b>the event command that asks the
    /// player</b>. <c>command102</c> calls <c>setupChoices</c> and steps the
    /// index over itself, so a 101 takes it — <b>there is nothing left of it
    /// to dispatch</b>.
    /// </summary>
    /// <remarks>
    /// **And this is the fifth name in five cards that had to be measured
    /// rather than remembered.** `ShowChoices` has been in this table since
    /// K-132, and it was 405 — the 405 that carries the choice list <b>as
    /// data</b>, not the 102 that shows it. A first draft of K-133 compared
    /// the follower against <c>ShowChoices</c>, so not one of this game's
    /// eight choices was ever found: 1352 commands instead of 1360, and a
    /// dialogue that ended on a choice the engine would have taken.
    /// <b>One constant, two meanings, and the tests did not notice for four
    /// runs** — because the ones that failed were the ones checking the sum.
    /// </remarks>
    public const int ShowChoiceList = 102;

    /// <summary>
    /// The 405: <b>a line of a 102's choices, and a line of a 105's
    /// scroll text</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And it has two jobs, and that is measured.</strong> Under a
    /// <c>102 Show Choice</c> it is one option, and it has no
    /// <c>command405</c> method, because the 102 that shows them reads it
    /// by position.
    /// </para>
    /// <para>
    /// <strong>And under a <c>105 Scroll Text</c> it is one line of the
    /// text that scrolls.</strong> Measured at <c>command105</c>: <c>while
    /// (this.nextEventCode() === 405) { this._index++;
    /// $gameMessage.add(this.currentCommand().parameters[0]); }</c>.
    /// </para>
    /// <para>
    /// <strong>And this project's four 105 commands carry exactly that</strong>
    /// — <strong>Map003 event 9 has one at index 201 followed by eight
    /// lines of narrative, and Map006 event 7 has one at index 5 with an
    /// empty 405 among its lines, which is a paragraph break.</strong>
    /// </para>
    /// </remarks>
    public const int ShowChoices = 405;

    /// <summary>
    /// The 402, which is two commands and not one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And the same number is two different commands.</strong>
    /// Inside a <c>401 Show Text</c> block it is *carries on with the
    /// next line* — the counterpart of a <c>401</c>. Inside a
    /// <c>102 Show Choice List</c> block it is <strong>one option of
    /// the choice</strong>, carrying its branch index and its text:
    /// measured <c>[0, "Yes"]</c> and <c>[1, "No"]</c>.
    /// </para>
    /// <para>
    /// <strong>And a reader that named it once had one of the two.</strong>
    /// Naming it <em>Continue Text</em> made a choice of six options look
    /// like six lines of dialogue; <strong>naming it <em>Choice
    /// Option</em> made a four-line dialogue look like a choice.</strong>
    /// <strong>Both names are here, and the code that uses each one says
    /// which block it is in.</strong>
    /// </para>
    /// </remarks>
    public const int ContinueText = 402;

    /// <summary>
    /// The same 402, read as one option of a <c>102</c>.
    /// </summary>
    public const int ChoicesOption = 402;

    /// <summary>
    /// Forces a move route onto a character. <c>command205</c> is
    /// <c>$gameMap.refreshIfNeeded(); this._characterId = params[0]; const
    /// character = this.character(params[0]); if (character) { character
    /// .forceMoveRoute(params[1]); if (params[1].wait) setWaitMode("route"); }
    /// return true;</c> — **it returns true even when there is no such
    /// character**, so a route for a character this reader has not got is a
    /// route that goes nowhere and is not an error.
    /// </summary>
    public const int MoveRoute = 205;

    /// <summary>Sends the player to another place. The engine's
    /// <c>command201</c> returns <b>false</b> in a battle or with a message on
    /// the screen, and otherwise only <i>reserves</i> the transfer and sets
    /// <c>setWaitMode("transfer")</c>.</summary>
    public const int TransferPlayer = 201;

    /// <summary>Opens the menu. The engine's own
    /// <c>command351</c> is <c>if (!$gameParty.inBattle()) {
    /// SceneManager.push(Scene_Menu); }</c> — no number changes, and a menu is
    /// a thing the player sees rather than a thing the game computes.</summary>
    public const int OpenMenu = 351;

    /// <summary>Runs a plugin's own code. The engine's <c>command357</c> is
    /// <c>PluginManager.callCommand(this, pluginName, params[1], params[3])
    /// </c>, which is somebody else's JavaScript and is never run here.</summary>
    public const int PluginCommand = 357;

    /// <summary>Runs a line of the author's own JavaScript. The engine's
    /// <c>command355</c> ends in <c>eval(script)</c> and is not run here.</summary>
    /// <summary>
    /// A line of the author's own JavaScript, and its 655s follow it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>And this repository does not run it, and it is reported
    /// and not executed.</strong> Measured at <c>command355</c>:
    /// <c>let script = this.currentCommand().parameters[0] + "\n"; while
    /// (this.nextEventCode() === 655) { this._index++; script +=
    /// this.currentCommand().parameters[0] + "\n"; } eval(script);</c>
    /// </para>
    /// <para>
    /// <strong>And the <c>while</c> is the engine's own, and it steps the
    /// index once per line</strong> — <strong>the same rule as
    /// <c>101</c> over its <c>401</c>s and <c>105</c> over its
    /// <c>405</c>s, and the reader must not step it again.</strong>
    /// </para>
    /// <para>
    /// <strong>And measured on this repository's own fixture, Map002
    /// event 6 holds 83 commands of which 62 are 655s, and without
    /// this branch the reader walked past the block one line at a time
    /// and the run froze at 100000 commands.</strong>
    /// </para>
    /// </remarks>
    public const int Script = 355;

    /// <summary>A line of the author's own JavaScript, under a 355.</summary>
    /// <remarks>
    /// <strong>And it has no <c>command655</c> method, and that is
    /// measured</strong> — <strong>the engine reads its lines inside
    /// <c>command355</c>'s own <c>while</c>, and the dispatcher never
    /// sees one.</strong> <strong>And a reader that has no branch for it
    /// treats it as a command with no effect and walks to the end of the
    /// list in single steps.</strong>
    /// </remarks>
    public const int ScriptLine = 655;

    /// <summary>A line of a plugin command's arguments.</summary>
    /// <remarks>
    /// <strong>And it has no <c>command657</c> method either, and that is
    /// measured</strong> — <strong>and <c>command357</c> takes its three
    /// parameters out of the list itself</strong>:
    /// <c>PluginManager.callCommand(this, pluginName, params[1],
    /// params[3]);</c> — <strong>and <c>params[2]</c> is the number of
    /// <c>657</c> lines that follow.</strong>
    /// </remarks>
    public const int ScriptLine2 = 657;

    /// <summary>Changes how many of an item the party has. The engine's
    /// <c>gainItem</c> clamps the count to <c>maxItems</c>, which is
    /// ninety-nine, and deletes the entry when it lands on zero.</summary>
    public const int ChangeItems = 126;

    /// <summary>Waits a number of frames. The engine's
    /// <c>updateWaitCount</c> counts them down one per frame, and a run that
    /// has no frames to count is waiting, not finished.</summary>
    public const int Wait = 230;

    /// <summary>Plays a background music file, from
    /// <c>command241</c>.</summary>
    /// <remarks>
    /// <strong>And the parameter is an object, and not four numbers.</strong>
    /// Measured on a finished project:
    /// <c>241 [{"name":"Scene8","volume":40,"pitch":80,"pan":0}]</c> —
    /// <strong>and XP writes the same command as 11510 with four bit
    /// fields, and a reader that read one of the two forms into the
    /// other produced a track at volume 0 named
    /// <c>{"name"</c>.</strong>
    /// </remarks>
    public const int PlayBgm = 241;

    /// <summary>Fades the background music out, from
    /// <c>command242</c>.</summary>
    public const int FadeOutBgm = 242;

    /// <summary>Plays a background sound, from <c>command245</c>.</summary>
    public const int PlayBgs = 245;

    /// <summary>Fades the background sound out, from
    /// <c>command246</c>.</summary>
    public const int FadeOutBgs = 246;

    /// <summary>Plays a music that plays alone, from
    /// <c>command249</c>.</summary>
    public const int PlayMe = 249;

    /// <summary>Plays a sound effect, from <c>command250</c>.</summary>
    public const int PlaySe = 250;

    /// <summary>Stops the sound effect, from <c>command251</c>.</summary>
    public const int StopSe = 251;

    /// <summary>Controls a self switch, from <c>command123</c>.</summary>
    /// <remarks>
    /// <strong>And the first parameter is a letter and not a number.</strong>
    /// Measured on a finished project: <c>123 ["A", 0]</c> — **and
    /// four letters for four switches, A through D**, **which the
    /// official help names as such**: *Self Switch — Specify the target
    /// self switch (A through D).*
    /// </remarks>
    public const int ControlSelfSwitch = 123;

    /// <summary>Changes the party, from <c>command129</c>.</summary>
    public const int ChangePartyMember = 129;

    /// <summary>Shows a balloon icon over a character, from
    /// <c>command213</c>.</summary>
    /// <remarks>
    /// <strong>And the first parameter is a character, and not an
    /// actor id.</strong> Measured: <c>213 [-1, 2, false]</c> — **and
    /// minus one is the player**, **and the official help says the
    /// position is *based on the position of the player or event*.**
    /// A reader that read the first parameter as an actor id looked up
    /// actor minus one and found nobody.
    /// </remarks>
    public const int ShowBalloonIcon = 213;



    /// <summary>Moves an event to a tile, from <c>command203</c>.</summary>
    /// <remarks>
    /// <para>
    /// <strong>And this has no page in the official help under that
    /// name, and the help has the page under
    /// <em>Set Event Location</em> instead</strong>, **and the number
    /// in the file is 203.**
    /// </para>
    /// <para>
    /// <strong>And the measured form is
    /// <c>[Event, Place, X, Y, Direction]</c></strong> — ten times in a
    /// finished project, <strong>and <em>Place</em> is 0 in every one of
    /// them</strong>, **which is *Direct Designation*.** The help:
    /// *To move an event to a specific location, select [Direct
    /// Designation], then click [...].*
    /// </para>
    /// </remarks>
    public const int SetEventLocation = 203;

    /// <summary>Starts a fight, from <c>command301</c>.</summary>
    /// <remarks>
    /// <para>
    /// <strong>And the help is short and exact.</strong> *Causes troops
    /// to appear and starts a battle. Troops — Specify the troop against
    /// which the player will fight. Can Escape — When enabled, the
    /// [Escape] command will be enabled during battle. Can Lose — When
    /// enabled, there will not be a game over even if the entire party is
    /// defeated.*
    /// </para>
    /// <para>
    /// <strong>And the measured form is
    /// <c>[0, 7, false, false]</c></strong> — four values, **and the
    /// last two are real JSON booleans**, **which is the same
    /// word-not-number finding that <c>213</c> brought.**
    /// </para>
    /// <para>
    /// <strong>And "Can Lose" is the one whose name lies.</strong> *When
    /// enabled, there will not be a game over* — **so the box says
    /// losing is survivable, and a reader that stored it as "losing is
    /// forbidden" turned a game's escape-from-defeat into a game
    /// over.**
    /// </para>
    /// </remarks>
    public const int BattleProcessing = 301;

    /// <summary>
    /// <c>command322</c> -- `actor.setCharacterImage`, 
    /// `setFaceImage` and `setBattlerImage`, and it takes six
    /// parameters and no vehicle at all.
    /// </summary>
    public const int ChangeActorImages = 322;

    private static readonly Dictionary<int, string> Names = BuildNames();

    /// <summary>The owner of each data number, measured from the engine.</summary>
    /// <summary>
    /// The owner of each data number. Every entry here was measured, from the
    /// engine's own source where it names one and from a real game's own event
    /// lists where it does not, and **not one of them follows a rule**.
    /// </summary>
    /// <summary>
    /// The command that reads each number as its own data.
    /// </summary>
    /// <remarks>
    /// Every entry was measured, from the engine's own source where it
    /// names one and from a real game's own event lists where it does not,
    /// and <b>not one of them follows a rule</b>. Four of the first five are
    /// three hundred above their command, which is how the rule was found,
    /// and the fifth is not: 605 belongs to 302 while three hundred above it
    /// is 305, a different command. The rest are not three hundred above
    /// anything, and 412 belongs to 111 rather than to the 112 that three
    /// hundred above it would name. A reader that used the rule would be
    /// right often enough to look checked and wrong in a place nothing
    /// else would show.
    /// </remarks>
    /// <summary>The command that reads each number as its own data.</summary>
    /// <remarks>
    /// Every entry was measured, from the engine source where it names one
    /// and from a real game own event lists where it does not, and <b>none
    /// of them follows a rule</b>. Some are three hundred above their
    /// command, which is how the rule is found; 605 is not (three hundred
    /// above it is 305, a different command) and 412 is not (three hundred
    /// above it is 112, a loop and not a branch).
    /// </remarks>
    /// <summary>The command that reads each number as its own data.</summary>
    /// <remarks>
    /// A number in this map is one the engine gives <b>no method of its
    /// own</b>, which is how a command and a piece of data are told apart
    /// here. That is a measurement, not a rule, and the rule that is
    /// tempting is wrong in both directions: 605 is three hundred above 605
    /// minus 300 is 305, a different command, and <b>411 and 413 are commands
    /// the engine does dispatch</b> (Else and Repeat Above) while 412 beside
    /// them is not a command at all. A reader that assumed a whole family
    /// was data would refuse a branch and run a structure as an
    /// instruction.
    /// </remarks>
    private static readonly Dictionary<int, int> Owners = new()
    {
        [401] = 101,        // a line of text under Show Text
        [405] = 105,        // a line under Show Scrolling Text
        [408] = 108,        // a line under Comment
        [412] = 111,        // the else of a branch that has one; the engine gives it no method of its own
        [501] = 102,        // one choice under a Show Choices
        [605] = 302,        // a purchase under Shop Processing
        [655] = 355,        // a line of script under Script
        [657] = 355,        // a further line of script under the same block
    };

    /// <summary>How many commands this generation dispatches.</summary>
    public static int Count => Names.Count;

    /// <summary>
    /// The name the engine gives a command, and for a data number the name of the
    /// command that reads it. Null when the number is neither.
    /// </summary>
    public static string? NameOf(int pCode)
    {
        if (Names.TryGetValue(pCode, out var name))
        {
            return name;
        }
        if (Owners.TryGetValue(pCode, out var owner))
        {
            return $"{Names[owner]} ({pCode})";
        }
        return null;
    }

    public static bool IsCommand(int pCode) => Names.ContainsKey(pCode);

    /// <summary>
    /// The command that reads this number as its own data, or 0 when the number
    /// is a command of its own or is nothing this reader knows.
    /// </summary>
    public static int OwnerOf(int pCode) =>
        Owners.TryGetValue(pCode, out var owner) ? owner : 0;

    public static MzCommandKind KindOf(int pCode)
    {
        if (IsCommand(pCode))
        {
            return MzCommandKind.Command;
        }
        if (Owners.ContainsKey(pCode))
        {
            return MzCommandKind.Data;
        }
        return pCode == 0 ? MzCommandKind.Separator : MzCommandKind.Unknown;
    }

    /// <summary>
    /// The name the engine gives a command.
    /// </summary>
    /// <remarks>
    /// A command has exactly one name and it is written once, in the enum
    /// member above. A name in a second table beside it could be changed
    /// there without the enum noticing, and a reader would then hand out a
    /// name no source in this repository carries. There is one place.
    /// </remarks>
    private static Dictionary<int, string> BuildNames()
    {
        // One place a name is written: MzCommandSet. A name in a second
        // table beside the enum drifted from it once already, and the reader
        // handed out the copy that nothing else carried.
        var names = new Dictionary<int, string>();
        foreach (var command in MzCommandSet.Commands)
        {
            names[command.Code] = command.Name;
        }
        return names;
    }
}

/// <summary>What a number in a command list is.</summary>
public enum MzCommandKind
{
    /// <summary>A command the engine dispatches to a method of its own.</summary>
    Command,

    /// <summary>A number another command reads as its own data.</summary>
    Data,

    /// <summary>A command this reader has no name for.</summary>
    Unknown,

    /// <summary>An indent of zero in the editor's own list.</summary>
    Separator,
}
