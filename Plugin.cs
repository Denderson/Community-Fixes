using BepInEx;
using Menu.Remix.MixedUI;
using RWCustom;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.CompilerServices.RuntimeHelpers;
using Menu;
using System.Globalization;
using System.Text.RegularExpressions;
using HUD;
using JollyCoop;
using JollyCoop.JollyMenu;
using MoreSlugcats;
using Music;
using System.Xml.Schema;
using System.IO;
using Expedition;
using UnityEngine.Assertions.Must;
using System.Runtime.CompilerServices;
using System.Runtime;

namespace Community_Fixes
{
    [BepInPlugin("praisethepit.commfixes", "Community Fixes", "0.1.0")]
    public class Plugin : BaseUnityPlugin
    {
        private OptionsMenu optionsMenuInstance;
        private bool initialized;
        public void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            if (this.initialized)
            {
                return;
            }
            this.initialized = true;

            //Loading custom assets put png in folder next to mod  and import with line: Futile.atlasManager.LoadImage("atlases/your_png");
            Futile.atlasManager.LoadImage("atlases/testImage");

            optionsMenuInstance = new OptionsMenu(this);
            try
            {
                MachineConnector.SetRegisteredOI("praisethepit.commfixes", optionsMenuInstance);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.Log($"Community Fixes: Hook_OnModsInit options failed init error {optionsMenuInstance}{ex}");
                Logger.LogError(ex);
                Logger.LogMessage("WHOOPS");
            }
        }
        public void OnEnable()
        {
            On.RainWorld.OnModsInit += RainWorld_OnModsInit;
            On.Player.ctor += Player_ctor;
            On.Player.Update += Player_Update; 
            On.Player.AerobicIncrease += Player_AerobicIncrease;
            On.Player.ClassMechanicsSaint += Player_ClassMechanicsSaint;
            On.Menu.SlugcatSelectMenu.UpdateStartButtonText += SlugcatSelectMenu_UpdateStartButtonText;
            On.Menu.SlugcatSelectMenu.ContinueStartedGame += SlugcatSelectMenu_ContinueStartedGame;
            On.RainWorldGame.GoToRedsGameOver += RainWorldGame_GoToRedsGameOver;
            On.RainWorldGame.GameOver += RainWorldGame_GameOver;
            On.SlugcatStats.PearlsGivePassageProgress += SlugcatStats_PearlsGivePassageProgress;
            On.Vulture.Violence += Vulture_Violence;
            On.RainCycle.ctor += RainCycle_ctor;
            On.DeathPersistentSaveData.ctor += DeathPersistentSaveData_ctor; 
            
            On.Spear.HitSomething += Spear_HitSomething;
            On.SaveState.GetStoryDenPosition += SaveState_GetStoryDenPosition;
            On.GhostWorldPresence.SpawnGhost += GhostWorldPresence_SpawnGhost; 
            On.KarmaFlower.BitByPlayer += KarmaFlower_BitByPlayer;
            

            //On.SlugcatStats.SlugcatStoryRegions += SlugcatStats_SlugcatStoryRegions; // Causes a crash
            On.RainCycle.GetDesiredCycleLength += RainCycle_GetDesiredCycleLength; // Causes a crash

            //On.Player.Die += Player_Die; //Leaving just in case I dont have a better alternative 
        }

        private int RainCycle_GetDesiredCycleLength(On.RainCycle.orig_GetDesiredCycleLength orig, RainCycle self)
        {
            if (self.world.game.session is StoryGameSession)
            {
                float multiplier = (self.world.game.session as StoryGameSession).saveState.deathPersistentSaveData.howWellIsPlayerDoing;
                if ((self.world.game.session as StoryGameSession).saveState.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Rivulet && self.world.game.GetStorySession.saveState.miscWorldSaveData.pebblesEnergyTaken)
                {
                    return (int)(orig(self) * multiplier * ((self.world.region.name == "VS" || self.world.region.name == "UW" || self.world.region.name == "SH" || self.world.region.name == "SB" || self.world.region.name == "SL") ? 0.5 : 0.3));
                }
                return (int)(orig(self) * multiplier);
            }
            return orig(self);
        }
        private void KarmaFlower_BitByPlayer(On.KarmaFlower.orig_BitByPlayer orig, KarmaFlower self, Creature.Grasp grasp, bool eu)
        {
            orig(self, grasp, eu);
            if (OptionsMenu.noKarmaFlowers.Value)
            {
                ((grasp.grabber as Player).room.game.session as StoryGameSession).saveState.deathPersistentSaveData.reinforcedKarma = false;
            }
        }
        private bool GhostWorldPresence_SpawnGhost(On.GhostWorldPresence.orig_SpawnGhost orig, GhostWorldPresence.GhostID ghostID, int karma, int karmaCap, int ghostPreviouslyEncountered, bool playingAsRed)
        {
            if (OptionsMenu.noGhostVisit.Value)
            {
                playingAsRed = true;
            }
            if (OptionsMenu.noGhostKarma.Value)
            {
                karma = karmaCap;
            }
            return orig (ghostID, karma, karmaCap, ghostPreviouslyEncountered, playingAsRed);
        }
        private void Player_AerobicIncrease(On.Player.orig_AerobicIncrease orig, Player self, float f)
        {
            f /= OptionsMenu.exhaustionGained.Value;
            self.aerobicLevel = Mathf.Min(1f, self.aerobicLevel + f /9f);
        }
        private void Player_ClassMechanicsSaint(On.Player.orig_ClassMechanicsSaint orig, Player self)
        {
            
        }
        private string SaveState_GetStoryDenPosition(On.SaveState.orig_GetStoryDenPosition orig, SlugcatStats.Name slugcat, out bool isVanilla)
        {
            if (OptionsMenu.randomShelter.Value)
            {
                isVanilla = false;
                Dictionary<string, int> dictionary = new Dictionary<string, int>();
                Dictionary<string, List<string>> dictionary2 = new Dictionary<string, List<string>>();
                List<string> list2 = SlugcatStats.SlugcatStoryRegions(slugcat);
                if (File.Exists(AssetManager.ResolveFilePath("randomstarts.txt")))
                {
                    string[] array = File.ReadAllLines(AssetManager.ResolveFilePath("randomstarts.txt"));
                    for (int i = 0; i < array.Length; i++)
                    {
                        if (!array[i].StartsWith("//") && array[i].Length > 0)
                        {
                            string text = Regex.Split(array[i], "_")[0];
                            if (!dictionary2.ContainsKey(text))
                            {
                                dictionary2.Add(text, new List<string>());
                            }
                            if (list2.Contains(text))
                            {
                                dictionary2[text].Add(array[i]);
                            }
                            if (dictionary2[text].Contains(array[i]) && !dictionary.ContainsKey(text))
                            {
                                dictionary.Add(text, 5);
                            }
                        }
                    }
                    System.Random random = new System.Random();
                    int maxValue = dictionary.Values.Sum();
                    int randomIndex = random.Next(0, maxValue);
                    string key = dictionary.First(delegate (KeyValuePair<string, int> x)
                    {
                        randomIndex -= x.Value;
                        return randomIndex < 0;
                    }).Key;
                    int num = (from list in dictionary2.Values select list.Count).Sum();
                    string text2 = dictionary2[key].ElementAt(UnityEngine.Random.Range(0, dictionary2[key].Count - 1));
                    return text2;
                }
                return "SU_S01";
            }
            return orig(slugcat, out isVanilla);
        }
        private List<string> SlugcatStats_SlugcatStoryRegions(On.SlugcatStats.orig_SlugcatStoryRegions orig, SlugcatStats.Name i)
        {
            if (OptionsMenu.biggerPassages.Value)
            {
                string[] source;
                source = new string[] { "OE", "SU", "HI", "DS", "CC", "GW", "SH", "VS", "SL", "MS", "SI", "LF", "UW", "SS", "LC", "SB" };
                if (i == MoreSlugcatsEnums.SlugcatStatsName.Rivulet) { source = new string[] { "OE", "SU", "HI", "DS", "CC", "GW", "SH", "VS", "SL", "MS", "SI", "LF", "UW", "RM", "LC", "SB" }; }
                if (i == MoreSlugcatsEnums.SlugcatStatsName.Artificer) { source = new string[] { "SU", "HI", "DS", "CC", "GW", "SH", "VS", "LM", "SI", "LF", "UW", "SS", "SB", "LC" }; } // TODO: Add HR maybe
                if (i == MoreSlugcatsEnums.SlugcatStatsName.Saint) { source = new string[] { "OE", "SU", "HI", "UG", "CC", "GW", "VS", "CL", "SL", "MS", "SI", "LF", "SB", "HR" }; }
                if (i == MoreSlugcatsEnums.SlugcatStatsName.Spear) { source = new string[] { "OE", "SU", "HI", "DS", "CC", "GW", "SH", "VS", "LM", "SI", "LF", "UW", "SS", "LC", "SB", "DM", }; }
                return source.ToList<string>();
            }
            return orig(i);
        }
        private void Player_Die(On.Player.orig_Die orig, Player self)
        {
            bool wasDead = self.dead;
            orig(self);
            if (!wasDead && self.dead && self.room.game.session is StoryGameSession)
            {
                (self.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.karma -= OptionsMenu.deathKarma.Value; 
            }
        }
        private bool Spear_HitSomething(On.Spear.orig_HitSomething orig, Spear self, SharedPhysics.CollisionResult result, bool eu)
        {
            if (ModManager.MMF && MMF.cfgVulnerableJellyfish.Value && !self.Spear_NeedleCanFeed() && result.obj is JellyFish)
            {
                (result.obj as JellyFish).dead = true;
                self.room.PlaySound(SoundID.Spear_Stick_In_Creature, self.firstChunk);
                self.LodgeInCreature(result, eu, true);
                return true;
            }
            return orig(self, result, eu);
        }
        private void DeathPersistentSaveData_ctor(On.DeathPersistentSaveData.orig_ctor orig, DeathPersistentSaveData self, SlugcatStats.Name slugcat)
        {
            orig(self, slugcat);
            self.karma = OptionsMenu.startingKarma.Value - 1;
            self.karma = Math.Min(self.karma, self.karmaCap);
        }
        private void RainCycle_ctor(On.RainCycle.orig_ctor orig, RainCycle self, World world, float minutes)
        {
            orig(self, world, minutes);
            if ((self.world.game.session is StoryGameSession) && OptionsMenu.forceFlood.Value && self.maxPreTimer > 0)
            {
                world.game.globalRain.drainWorldFlood = 99000f;
            }
            if (self.preTimer > 0)
            {
                self.maxPreTimer = (int)(self.maxPreTimer * OptionsMenu.precycleTimer.Value);
                self.preTimer = self.maxPreTimer;
                self.preCycleRainPulse_WaveA = 0f;
                self.preCycleRainPulse_WaveB = 0f;
                self.preCycleRainPulse_WaveC = 1.5707964f;
                world.game.globalRain.preCycleRainPulse_Scale = 1f;
            }
        }
        public int CyclesInNegatives(global::Player self)
        {
            if (self.room.game.session is StoryGameSession)
            {
                int cycleCount = OptionsMenu.startingCycles.Value - (self.room.game.session as StoryGameSession).saveState.cycleNumber;
                if ((self.room.game.session as StoryGameSession).saveState.miscWorldSaveData.SSaiConversationsHad > 0)
                {
                    cycleCount += OptionsMenu.pebblesCycles.Value;
                }
                if ((self.room.game.session as StoryGameSession).saveState.miscWorldSaveData.EverMetMoon)
                {
                    cycleCount += OptionsMenu.moonCycles.Value;
                }
                return -(cycleCount);
            }
            return -1;
        }
        private void Vulture_Violence(On.Vulture.orig_Violence orig, Vulture self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos onAppendagePos, Creature.DamageType type, float damage, float stunBonus)
        {
            if ((OptionsMenu.monkDemask.Value) && (hitChunk != null && hitChunk.index == 4))
            {
                damage = 1;
            }
            orig(self, source, directionAndMomentum, hitChunk, onAppendagePos, type, damage, stunBonus);
        }
        private bool SlugcatStats_PearlsGivePassageProgress(On.SlugcatStats.orig_PearlsGivePassageProgress orig, StoryGameSession session)
        {
            if (OptionsMenu.allScholar.Value && (session.saveState.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel || session.saveState.saveStateNumber == MoreSlugcatsEnums.SlugcatStatsName.Saint))
            {
                return true;
            }
            return orig(session);
        }
        private void RainWorldGame_GameOver(On.RainWorldGame.orig_GameOver orig, RainWorldGame self, Creature.Grasp dependentOnGrasp)
        {
            if (self.IsStorySession && OptionsMenu.enableIllness.Value)
            {
                int negativeCycles = OptionsMenu.startingCycles.Value - (self.session as StoryGameSession).saveState.cycleNumber;
                if ((self.session as StoryGameSession).saveState.miscWorldSaveData.SSaiConversationsHad > 0)
                {
                    negativeCycles += OptionsMenu.pebblesCycles.Value;
                }
                if ((self.session as StoryGameSession).saveState.miscWorldSaveData.EverMetMoon)
                {
                    negativeCycles += OptionsMenu.moonCycles.Value;
                }
                if (negativeCycles <= 0)
                {
                    self.GoToRedsGameOver();
                    return;
                }
            }
            if (self.IsStorySession && OptionsMenu.karmaPermadeath.Value)
            {
                if (self.GetStorySession.saveState.deathPersistentSaveData.karma < OptionsMenu.deathKarma.Value && !self.GetStorySession.saveState.deathPersistentSaveData.reinforcedKarma)
                {
                    self.GoToRedsGameOver();
                    return;
                }
            }
            (self.session as StoryGameSession).saveState.deathPersistentSaveData.karma -= OptionsMenu.deathKarma.Value;
            orig(self, dependentOnGrasp);
        }
        private void RainWorldGame_GoToRedsGameOver(On.RainWorldGame.orig_GoToRedsGameOver orig, RainWorldGame self)
        {
            if (self.manager.upcomingProcess != null)
            {
                return;
            }
            self.manager.musicPlayer?.FadeOutAllSongs(20f);
            if (self.Players[0].realizedCreature != null && (self.Players[0].realizedCreature as Player).redsIllness != null)
            {
                (self.Players[0].realizedCreature as Player).redsIllness.fadeOutSlow = true;
            }
            if (self.GetStorySession.saveState.saveStateNumber != SlugcatStats.Name.Red)
            {
                self.GetStorySession.saveState.deathPersistentSaveData.altEnding = true;
                if (ModManager.CoopAvailable)
                {
                    int num = 0;
                    using (IEnumerator<Player> enumerator = (from x in self.session.game.Players select x.realizedCreature as Player).GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            Player player = enumerator.Current;
                            self.GetStorySession.saveState.AppendCycleToStatistics(player, self.GetStorySession, true, num);
                            num++;
                        }
                        self.manager.rainWorld.progression.SaveWorldStateAndProgression(false);
                        self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Statistics, 10f);
                    }
                }
                self.GetStorySession.saveState.AppendCycleToStatistics(self.Players[0].realizedCreature as Player, self.GetStorySession, true, 0);
            }
            orig(self);
        }
        private void SlugcatSelectMenu_UpdateStartButtonText(On.Menu.SlugcatSelectMenu.orig_UpdateStartButtonText orig, SlugcatSelectMenu self)
        {
            orig(self);
            if (!self.restartChecked)
            {
                if (OptionsMenu.enableIllness.Value || OptionsMenu.karmaPermadeath.Value)
                {
                    SlugcatStats.Name slugcat = self.slugcatPages[self.slugcatPageIndex].slugcatNumber;
                    if (self.saveGameData[slugcat] != null && (self.saveGameData[slugcat].ascended || self.saveGameData[slugcat].altEnding))
                    {
                        self.startButton.menuLabel.text = self.Translate("STATISTICS");
                    }
                }
                if (OptionsMenu.noPermadeath.Value)
                {
                    self.startButton.menuLabel.text = self.Translate("CONTINUE");
                }
            }
            return;
        }
        private void SlugcatSelectMenu_ContinueStartedGame(On.Menu.SlugcatSelectMenu.orig_ContinueStartedGame orig, SlugcatSelectMenu self, SlugcatStats.Name storyGameCharacter)
        {
            if (!self.restartChecked)
            {
                if (OptionsMenu.noPermadeath.Value)
                {
                    self.manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.Load;
                    self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);
                    return;
                }
                if (OptionsMenu.enableIllness.Value || OptionsMenu.karmaPermadeath.Value)
                {
                    if (self.slugcatPages[self.slugcatPageIndex].slugcatNumber == storyGameCharacter && self.saveGameData[storyGameCharacter] != null && (self.saveGameData[storyGameCharacter].ascended || self.saveGameData[storyGameCharacter].altEnding))
                    {
                        self.redSaveState = self.manager.rainWorld.progression.GetOrInitiateSaveState(storyGameCharacter, null, self.manager.menuSetup, false);
                        self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Statistics);
                        return;
                    }
                }
            }
            orig(self, storyGameCharacter);
        }
        private void Player_Update(On.Player.orig_Update orig, global::Player self, bool eu)
        {
            if (OptionsMenu.enableIllness.Value && (self.slugcatStats.name != global::SlugcatStats.Name.Red && self.redsIllness != null && CyclesInNegatives(self) >= 0))
            {
                self.redsIllness.Update();
            }
            orig(self, eu);
        }
        private void Player_ctor(On.Player.orig_ctor orig, global::Player self, global::AbstractCreature abstractCreature, global::World world)
        {
            orig(self, abstractCreature, world);
            if ((!self.playerState.isGhost && (self.redsIllness == null || self.redsIllness.cycle <= CyclesInNegatives(self)) && CyclesInNegatives(self) >= 0) && OptionsMenu.enableIllness.Value)
            {
                self.redsIllness = new global::RedsIllness(self, CyclesInNegatives(self));
            }
            orig.Invoke(self, abstractCreature, world);
            if (OptionsMenu.randomShelter.Value && world.game.rainWorld.progression.currentSaveState.cycleNumber == 0)
            {
                if (self.SlugCatClass == SlugcatStats.Name.Red)
                {
                    world.game.FirstRealizedPlayer.objectInStomach = new DataPearl.AbstractDataPearl(world, AbstractPhysicalObject.AbstractObjectType.DataPearl, null, abstractCreature.spawnDen, world.game.GetNewID(), -1, -1, null, DataPearl.AbstractDataPearl.DataPearlType.Red_stomach);
                }
                if (self.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Rivulet)
                {
                    world.game.FirstRealizedPlayer.objectInStomach = new DataPearl.AbstractDataPearl(world, AbstractPhysicalObject.AbstractObjectType.DataPearl, null, abstractCreature.spawnDen, world.game.GetNewID(), -1, -1, null, MoreSlugcatsEnums.DataPearlType.Rivulet_stomach);
                }
            }
        }

    }
    public class OptionsMenu : OptionInterface
    {
        public OptionsMenu(Plugin plugin)
        {
            // Page 1
            randomShelter = this.config.Bind<bool>("commfixes_randomShelter", false, new ConfigurableInfo("Start the game in a random shelter on the map."));
            dynamicTimer = this.config.Bind<bool>("commfixes_dynamicTimer", true, new ConfigurableInfo("Slightly modifies rain timer based on your current progress and skill."));
            forceFlood = this.config.Bind<bool>("commfixes_forceFlood", true, new ConfigurableInfo("All shelter failures include room flooding mechanic."));
            allScholar = this.config.Bind<bool>("commfixes_allScholar", true, new ConfigurableInfo("Allows Saint and Inv to get Scholar passage."));
            biggerPassages = this.config.Bind<bool>("commfixes_biggerPassages", true, new ConfigurableInfo("Makes Dragon Slayer, Wanderer, Scholar and Pilgrim passages contain more pips.")); // TODO
            betterPassage = this.config.Bind<bool>("commfixes_betterPassages", false, new ConfigurableInfo("Passages will bring key items from all shelters instead of only the one you are in.")); // TODO
            precycleTimer = this.config.Bind<float>("commfixes_precycleTimer", 1f, new ConfigurableInfo("Multiplies the duration of precycles."));
            exhaustionGained = this.config.Bind<float>("commfixes_exhaustionGained", 1f, new ConfigurableInfo("Multiplies the exhaustion gained"));

            // Page 2
            monkDemask = this.config.Bind<bool>("commfixes_monkDemask", true, new ConfigurableInfo("Reduces the damage requirement for demasking vultures."));
            monkDreams = this.config.Bind<bool>("commfixes_monkDreams", true, new ConfigurableInfo("Gives Monk unique dreams that replace the base Survivor ones.")); //TODO
            lizardKarma = this.config.Bind<bool>("commfixes_lizardKarma", false, new ConfigurableInfo("Lizards can also give Artificer karma.")); //TODO
            invShoreline = this.config.Bind<bool>("commfixes_invShoreline", true, new ConfigurableInfo("Controls are reversed while swimming in Shoreline")); //TODO
            invChimney = this.config.Bind<bool>("commfixes_invChimney", false, new ConfigurableInfo("Replaces Yeeks with Squidcadas in Inv Chimney Canopy.")); //TODO
            invSnails = this.config.Bind<float>("commfixes_invSnails", 1f, new ConfigurableInfo("Multiplies Snail spawn rate in Painage System.")); //TODO
            saintPermakill = this.config.Bind<bool>("commfixes_saintPermakill", false, new ConfigurableInfo("Creatures killed by Saint's 10 karma ability never respawn")); //TODO

            // Page 3
            noPermadeath = this.config.Bind<bool>("commfixes_noPermadeath", false, new ConfigurableInfo("Replaces Statistics button with Continue button."));
            enableIllness = this.config.Bind<bool>("commfixes_enableIllness", false, new ConfigurableInfo("The return of Scorerunify, more buggy than ever!"));
            karmaPermadeath = this.config.Bind<bool>("commfixes_karmaPermadeath", false, new ConfigurableInfo("If you would lose more karma than you currently have, end the game instead."));
            noGhostVisit = this.config.Bind<bool>("commfixes_noGhostVisit", true, new ConfigurableInfo("Allows you to meet echoes without trimming them first."));
            noGhostKarma = this.config.Bind<bool>("commfixes_noGhostKarma", false, new ConfigurableInfo("Allows you to meet echoes with any amount of karma."));
            noKarmaFlowers = this.config.Bind<bool>("commfixes_noKarmaFlowers", false, new ConfigurableInfo("Disables karma flowers from giving karma reinforcement.")); 
            startingCycles = this.config.Bind<int>("commfixes_startingCycles", 19, new ConfigurableInfo("Number of cycles you start with."));
            pebblesCycles = this.config.Bind<int>("commfixes_pebblesCycles", 5, new ConfigurableInfo("Number of cycles given by meeting Five Pebbles."));
            moonCycles = this.config.Bind<int>("commfixes_moonCycles", 0, new ConfigurableInfo("Number of cycles given by meeting Looks To The Moon."));
            startingKarma = this.config.Bind<int>("commfixes_startingKarma", 1, new ConfigurableInfo("Karma each slugcat starts with."));
            deathKarma = this.config.Bind<int>("commfixes_deathKarma", 1, new ConfigurableInfo("Karma lost per each death.")); // TODO
            sleepKarma = this.config.Bind<int>("commfixes_sleepKarma", 1, new ConfigurableInfo("Karma gained per each hibernation.")); //TODO

            misc = this.config.Bind<bool>("commfixes_misc", false);
        }

        public override void Initialize()
        {
            Color unfinishedColor = new Color(0.85f, 0.35f, 0.4f);

            var opTab1 = new OpTab(this, "General fixes");
            var opTab2 = new OpTab(this, "Slugcat specific fixes");
            var opTab3 = new OpTab(this, "Illness and permadeath");
            var opTab4 = new OpTab(this, "Misc changes");
            this.Tabs = new[] { opTab1, opTab2, opTab3, opTab4 };

            // Tab 1
            UIelement[] UIArrayElements = new UIelement[] //create an array of ui elements
            {
                new OpLabel(0, 550, "General changes", true),

                new OpLabel(30, 503, "Random starting shelter"), new OpCheckBox(randomShelter, 0, 500),
                new OpLabel(30, 463, "Dynamic rain timer") {color = unfinishedColor}, new OpCheckBox(dynamicTimer, 0, 460) {colorEdge = unfinishedColor}, // TODO
                new OpLabel(30, 423, "Force precycle flood"), new OpCheckBox(forceFlood, 0, 420),
                new OpLabel(30, 383, "Scholar for everyone"), new OpCheckBox(allScholar, 0, 380),
                new OpLabel(30, 343, "Harder passages"), new OpCheckBox(biggerPassages, 0, 340) {colorEdge = unfinishedColor}, // TODO

                new OpLabel(115, 305, "Precycle duration"), new OpFloatSlider(precycleTimer, new Vector2(0, 300), 100) {max = 2f},
                new OpLabel(115, 265, "Exhaustion multiplier"), new OpFloatSlider(exhaustionGained, new Vector2(0, 260), 100) {max = 2f},

                //new OpLabelLong(new Vector2(100,0),new Vector2(400,100), "Bottom Text", true),
                //new OpLabel(0, 550, "Red colour = unfinished"),
            };
            opTab1.AddItems(UIArrayElements);

            // Tab 2
            UIArrayElements = new UIelement[]
            {
                new OpLabel(0, 550, "Slugcat specific fixes", true),

                //Monk
                new OpLabel(30, 503, "Allow Monk demasks"), new OpCheckBox(monkDemask, 0, 500),
                new OpLabel(30, 463, "Monk custom dreams") { color = unfinishedColor }, new OpCheckBox(monkDreams, 0, 460){colorEdge = unfinishedColor}, // TODO

                //Hunter

                //Gourmand

                //Artificer
                new OpLabel(30, 423, "Lizard karma") { color = unfinishedColor }, new OpCheckBox(lizardKarma, 0, 420){colorEdge = unfinishedColor}, // TODO

                //Rivulet

                //Spearmaster

                //Saint
                new OpLabel(30, 303, "Permanent Saint ascension") { color = unfinishedColor }, new OpCheckBox(saintPermakill, 0, 300) {colorEdge = unfinishedColor},

                //Inv
                new OpLabel(30, 383, "Reverse controls in Inv Shoreline") { color = unfinishedColor }, new OpCheckBox(invShoreline, 0, 380){colorEdge = unfinishedColor}, // TODO
                new OpLabel(30, 343, "Replace Yeeks with Squidcadas in Inv Chimney Canopy") { color = unfinishedColor }, new OpCheckBox(invChimney, 0, 340) {colorEdge = unfinishedColor}, // TODO
                
                new OpLabel(115, 265, "Painage Snail spawn rate") { color = unfinishedColor }, new OpFloatSlider(invSnails, new Vector2(0, 260), 100){max = 2, colorLine = unfinishedColor, colorEdge = unfinishedColor},
            };
            opTab2.AddItems(UIArrayElements);
            UIArrayElements = new UIelement[]
            {
                new OpLabel(0, 550, "Illness and permadeath", true),

                new OpLabel(30, 503, "Continue from statistics screen"), new OpCheckBox(noPermadeath, 0, 500),
                new OpLabel(30, 463, "Enable illness for all slugcats"), new OpCheckBox(enableIllness, 0, 460),
                new OpLabel(30, 423, "Enable permadeath at low karma"), new OpCheckBox(karmaPermadeath, 0, 420),
                new OpLabel(30, 383, "No Echo trimming requirement"), new OpCheckBox(noGhostVisit, 0, 380),
                new OpLabel(30, 343, "No Echo karma requirement"), new OpCheckBox(noGhostKarma, 0, 340),
                new OpLabel(30, 303, "No karma flowers"), new OpCheckBox(noKarmaFlowers, 0, 300),

                new OpLabel(115, 265, "Starting cycles"), new OpSlider(startingCycles, new Vector2(0, 260), 100){min = -20, max = 20},
                new OpLabel(115, 225, "Pebbles cycles"), new OpSlider(pebblesCycles, new Vector2(0, 220), 100){min = 0, max = 5},
                new OpLabel(115, 185, "Moon cycles"), new OpSlider(moonCycles, new Vector2(0, 180), 100){min = 0, max = 5},
                new OpLabel(115, 145, "Starting karma"), new OpSlider(startingKarma, new Vector2(0, 140), 100){min = 1, max = 10},
                new OpLabel(115, 105, "Karma lost per death") { color = unfinishedColor }, new OpSlider(deathKarma, new Vector2(0, 100), 100){min = 0, max = 10, colorLine = unfinishedColor},
                new OpLabel(115, 65, "Karma gained per hibernation") { color = unfinishedColor }, new OpSlider(sleepKarma, new Vector2(0, 60), 100){min = 0, max = 10, colorLine = unfinishedColor, colorEdge = unfinishedColor}, // TODO
            };
            opTab3.AddItems(UIArrayElements);
            UIArrayElements = new UIelement[]
            {
                    new OpLabel(0, 550, "Misc changes", true),

                    new OpCheckBox(misc, 0, 500),
                    new OpLabel(0, 450, "Fixed Vulnerable Jellyfish not working"),
                    new OpLabel(0, 430, "Added miros vultures to Inv Chimney Canopy"),
                    new OpLabel(0, 410, ""){color = unfinishedColor}, // TODO
                    new OpLabel(0, 390, "Removed PlayerPushback in SU_PMPSTATION01 and expanded Facility Roots"){color = unfinishedColor}, // TODO
                    new OpLabel(0, 370, "Added multiple echoes around the world"){color = unfinishedColor}, // TODO
            };
            opTab4.AddItems(UIArrayElements);
        }

        public static Configurable<bool> randomShelter, dynamicTimer, forceFlood, noGhostVisit, noGhostKarma, allScholar, biggerPassages, betterPassage, monkDemask, monkDreams, lizardKarma, invShoreline, invChimney, noPermadeath, enableIllness, karmaPermadeath, misc, noKarmaFlowers, saintPermakill;
        public static Configurable<float> precycleTimer, exhaustionGained, invSnails;
        public static Configurable<int> startingCycles, pebblesCycles, moonCycles, startingKarma, deathKarma, sleepKarma;
    }
}