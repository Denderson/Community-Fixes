using System;
using BepInEx;
using UnityEngine;
using SlugBase.Features;
using static SlugBase.Features.FeatureTypes;
using MoreSlugcats;
using static MonoMod.InlineRT.MonoModRule;

namespace gladiator
{
    [BepInPlugin(MOD_ID, "gladiator", "0.1.0")]
    class Plugin : BaseUnityPlugin
    {
        private const string MOD_ID = "praisethepit.gladiator";

        public static readonly PlayerFeature<float> SuperJump = PlayerFloat("gladiator/super_jump");
        public static readonly PlayerFeature<bool> Adrenaline = PlayerBool("gladiator/adrenaline");
        public static readonly GameFeature<float> MeanLizards = GameFloat("gladiator/mean_lizards");

        // Add hooks
        public void OnEnable()
        {
            On.RainWorld.OnModsInit += Extras.WrapInit(LoadResources);

            // Put your custom hooks here!
            On.Player.Jump += Player_Jump;
            On.Lizard.ctor += Lizard_ctor;
            On.Player.ThrownSpear += Player_ThrownSpear;
        }
        
        // Load any resources, such as sprites or sounds
        private void LoadResources(RainWorld rainWorld)
        {
        }

        // Implement MeanLizards
        private void Lizard_ctor(On.Lizard.orig_ctor orig, Lizard self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);

            if(MeanLizards.TryGet(world.game, out float meanness))
            {
                self.spawnDataEvil = Mathf.Min(self.spawnDataEvil, meanness);
            }
        }


        // Implement SuperJump
        private void Player_Jump(On.Player.orig_Jump orig, Player self)
        {
            orig(self);

            if (SuperJump.TryGet(self, out var power))
            {
                self.jumpBoost *= 1f + power;
            }
        }

        private void Player_ThrownSpear(On.Player.orig_ThrownSpear orig, Player self, Spear spear)
        {
            if ((Plugin.Adrenaline.TryGet(self, out bool flag) && flag) && self.Adrenaline > 0f)
            {
                spear.spearDamageBonus = 3f;
                if (self.canJump != 0)
                {
                    self.animation = Player.AnimationIndex.Roll;
                }
                else
                {
                    self.animation = Player.AnimationIndex.Flip;
                }
                if ((self.room != null && self.room.gravity == 0f) || Mathf.Abs(spear.firstChunk.vel.x) < 1f)
                {
                    self.firstChunk.vel += spear.firstChunk.vel.normalized * 9f;
                }
                else
                {
                    self.rollDirection = (int)Mathf.Sign(spear.firstChunk.vel.x);
                    self.rollCounter = 0;
                    BodyChunk firstChunk3 = self.firstChunk;
                    firstChunk3.vel.x = firstChunk3.vel.x + Mathf.Sign(spear.firstChunk.vel.x) * 9f;
                }
                self.gourmandAttackNegateTime = 80;
            }
        }
    }
}