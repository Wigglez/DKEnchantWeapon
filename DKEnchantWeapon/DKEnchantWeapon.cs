﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Profiles;
using Styx.Helpers;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Action = Styx.TreeSharp.Action;

namespace Styx.Bot.Quest_Behaviors {
    [CustomBehaviorFileName(@"Misc\DKEnchantWeapon")]
    public class DKEnchantWeapon : CustomForcedBehavior {

        // ===========================================================
        // Constants
        // ===========================================================

        public const int TradeSkillID = 53428;

        public const int DeathgateId = 50977;

        // ===========================================================
        // Fields
        // ===========================================================

        private static readonly LocalPlayer Me = StyxWoW.Me;

        // ===========================================================
        // Constructors
        // ===========================================================

        public DKEnchantWeapon(Dictionary<string, string> args)
            : base(args) {
            try {
                SpellID = GetAttributeAsNullable("SpellID", true, ConstrainAs.SpellId, null) ?? 0;
            } catch(Exception except) {
                // Maintenance problems occur for a number of reasons.  The primary two are...
                // * Changes were made to the behavior, and boundary conditions weren't properly tested.
                // * The Honorbuddy core was changed, and the behavior wasn't adjusted for the new changes.
                // In any case, we pinpoint the source of the problem area here, and hopefully it
                // can be quickly resolved.
                LogMessage("error", "BEHAVIOR MAINTENANCE PROBLEM: " + except.Message + "\nFROM HERE:\n" + except.StackTrace + "\n");
                IsAttributeProblem = true;
            }
        }

        // ===========================================================
        // Getter & Setter
        // ===========================================================

        // Attributes
        public static int SpellID { get; set; }

        // Overrides
        public static Composite Root { get; set; }

        public static bool IsDisposed { get; set; }

        public static bool IsBehaviorDone { get; set; }

        // My shit
        public static int EnchantID { get; set; }

        public static bool RunOnceMainhand { get; set; }
        public static bool RunOnceOffhand { get; set; }
        public static bool TookTeleporter { get; set; }

        public static WoWObject Runeforge { get; set; }
        public static WoWObject Teleporter { get; set; }
        public static WoWObject Deathgate { get; set; }

        public static int ItemMainHandID { get; set; }
        public static int ItemOffHandID { get; set; }
        public static bool HasMainHand { get; set; }
        public static bool HasOffHand { get; set; }
        public static int WeaponMainHandEnchantID { get; set; }
        public static int WeaponOffHandEnchantID { get; set; }

        // ===========================================================
        // Methods for/from SuperClass/Interfaces
        // ===========================================================

        public override void OnStart() {
            // This reports problems, and stops BT processing if there was a problem with attributes...
            // We had to defer this action, as the 'profile line number' is not available during the element's
            // constructor call.
            OnStart_HandleAttributeProblem();

            IsDisposed = false;

            BotEvents.OnBotStopped += BotEvents_OnBotStopped;
        }

        public override void OnFinished() {
            if(!IsDisposed) {
                BotEvents.OnBotStopped -= BotEvents_OnBotStopped;

                SpellID = 0;

                IsBehaviorDone = false;
                EnchantID = 0;

                RunOnceMainhand = false;
                RunOnceOffhand = false;
                TookTeleporter = false;

                Runeforge = null;
                Teleporter = null;
                Deathgate = null;

                ItemMainHandID = 0;
                ItemOffHandID = 0;

                HasMainHand = false;
                HasOffHand = false;

                WeaponMainHandEnchantID = 0;
                WeaponOffHandEnchantID = 0;

                if(Lua.GetReturnVal<bool>("return TradeSkillFrame:IsVisible()", 0)) {
                    Lua.DoString("CloseTradeSkill()");
                }

                GC.SuppressFinalize(this);

                base.OnFinished();
            }

            IsDisposed = true;
        }

        public override bool IsDone {
            get {
                return IsBehaviorDone;
            }
        }

        protected override Composite CreateBehavior() {
            return Root ?? (Root =
                new Decorator(ret => !IsBehaviorDone,
                    new Action(ret => CheckWeaponStatus())
                )
            );
        }

        // ===========================================================
        // Methods
        // ===========================================================

        public static void CustomNormalLog(string message, params object[] args) {
            Logging.Write(Colors.DeepSkyBlue, "[DKEnchantWeapon]: " + message, args);
        }

        public static void CustomDiagnosticLog(string message, params object[] args) {
            Logging.WriteDiagnostic(Colors.DeepSkyBlue, "[DKEnchantWeapon]: " + message, args);
        }

        // ===========================================================
        // Inner and Anonymous Classes
        // ===========================================================

        private void BotEvents_OnBotStopped(EventArgs args) {
            OnFinished();
        }

        private static void SetIDs() {
            if(EnchantID == 0) {
                // Get the enchant ID that we need on the weapons
                AssignEnchantIDsToSpellIDs();
            }

            if(Me.Inventory.Equipped.MainHand != null) {
                var linkMainHand = GetInventoryItemLink("player", 16);
                GetIDsFromString(linkMainHand, "Main Hand");
                HasMainHand = true;
            }

            if(Me.Inventory.Equipped.OffHand == null) {
                return;
            }

            var linkOffHand = GetInventoryItemLink("player", 17);
            GetIDsFromString(linkOffHand, "Off Hand");
            HasOffHand = true;
        }

        private static void AssignEnchantIDsToSpellIDs() {
            switch(SpellID) {
                case 53341: // Rune of Cinderglacier
                    EnchantID = 3369;
                    break;
                case 53331: // Rune of Lichbane
                    EnchantID = 3366;
                    break;
                case 53343: // Rune of Razorice
                    EnchantID = 3370;
                    break;
                case 54447: // Rune of Spellbreaking
                    EnchantID = 3595;
                    break;
                case 53342: // Rune of Spellshattering
                    EnchantID = 3367;
                    break;
                case 54446: // Rune of Swordbreaking
                    EnchantID = 3594;
                    break;
                case 53323: // Rune of Swordshattering
                    EnchantID = 3365;
                    break;
                case 53344: // Rune of the Fallen Crusader
                    EnchantID = 3368;
                    break;
                case 70164: // Rune of the Nerubian Carapace
                    EnchantID = 3883;
                    break;
                case 62158: // Rune of the Stoneskin Gargoyle
                    EnchantID = 3847;
                    break;
            }
        }

        private static void GetIDsFromString(string strSource, string slotName) {
            var first = strSource.IndexOf(":", 0, StringComparison.Ordinal);
            var firstString = strSource.Substring(first);

            var itemId = firstString.IndexOf(":", 0, StringComparison.Ordinal);
            var itemIdString = firstString.Substring(itemId + 1);

            var enchantId = itemIdString.IndexOf(":", 0, StringComparison.Ordinal);
            var enchantIdString = itemIdString.Substring(enchantId + 1);

            var rest = enchantIdString.IndexOf(":", 0, StringComparison.Ordinal);
            var restString = enchantIdString.Substring(rest);

            var desiredItemIDString = itemIdString.Substring(0, (itemIdString.Length - enchantIdString.Length - 1));

            var desiredEnchantIDString = enchantIdString.Substring(0, (enchantIdString.Length - restString.Length));

            switch(slotName) {
                case "Main Hand":
                    ItemMainHandID = desiredItemIDString.ToInt32();
                    WeaponMainHandEnchantID = desiredEnchantIDString.ToInt32();
                    break;
                case "Off Hand":
                    ItemOffHandID = desiredItemIDString.ToInt32();
                    WeaponOffHandEnchantID = desiredEnchantIDString.ToInt32();
                    break;
            }
        }

        /// <summary>
        ///     Returns an item link for an item in the unit's inventory. The player's inventory is actually extended to include items in the bank, 
        ///     items in the player's containers and the player's key ring in addition to the items the player has equipped. The appropriate inventoryID 
        ///     can be found by calling the appropriate function.
        /// </summary>
        /// <param name="pUnit">A unit to query; only valid for 'player' or the unit currently being inspected (string, unitID)</param>
        /// <param name="pSlot">An inventory slot number, as can be obtained from GetInventorySlotInfo. (number, inventoryID)</param>
        /// <returns>
        ///     <para>link = An item link for the given item (string, hyperlink)</para>
        /// </returns>
        /// <remarks>
        ///     <para>-- Inventory slots</para>
        ///     <para>INVSLOT_AMMO            = 0;</para>
        ///     <para>INVSLOT_HEAD            = 1; INVSLOT_FIRST_EQUIPPED = INVSLOT_HEAD;</para>
        ///     <para>INVSLOT_NECK            = 2;</para>
        ///     <para>INVSLOT_SHOULDER        = 3;</para>
        ///     <para>INVSLOT_BODY            = 4;</para>
        ///     <para>INVSLOT_CHEST           = 5;</para>
        ///     <para>INVSLOT_WAIST           = 6;</para>
        ///     <para>INVSLOT_LEGS            = 7;</para>
        ///     <para>INVSLOT_FEET            = 8;</para>
        ///     <para>INVSLOT_WRIST           = 9;</para>
        ///     <para>INVSLOT_HAND            = 10;</para>
        ///     <para>INVSLOT_FINGER1         = 11;</para>
        ///     <para>INVSLOT_FINGER2         = 12;</para>
        ///     <para>INVSLOT_TRINKET1        = 13;</para>
        ///     <para>INVSLOT_TRINKET2        = 14;</para>
        ///     <para>INVSLOT_BACK            = 15;</para>
        ///     <para>INVSLOT_MAINHAND        = 16;</para>
        ///     <para>INVSLOT_OFFHAND         = 17;</para>
        ///     <para>INVSLOT_RANGED          = 18;</para>
        ///     <para>INVSLOT_TABARD          = 19;</para>
        ///     <para>INVSLOT_LAST_EQUIPPED   = INVSLOT_TABARD;</para>
        ///     <para> </para>
        ///     <para>http://wowprogramming.com/docs/api/GetInventoryItemLink</para>
        /// </remarks>
        private static string GetInventoryItemLink(string pUnit, int pSlot) {
            return Lua.GetReturnVal<string>(string.Format("return GetInventoryItemLink('{0}', {1})", pUnit, pSlot), 0);
        }

        private static void CheckWeaponStatus() {
            SetIDs();

            if(HasMainHand) {
                if(EnchantID != WeaponMainHandEnchantID) {
                    HandleEnchanting("main");
                    return;
                }

                if(HasOffHand && EnchantID != WeaponOffHandEnchantID) {
                    HandleEnchanting("off");
                    return;
                }

                // Leave
                if(Me.ZoneId == 139) {
                    CastDeathgate();
                    TakeDeathgate();
                    return;
                }

                IsBehaviorDone = true;
            } else {
                CustomNormalLog("You have no main hand weapon equipped.");
                IsBehaviorDone = true;
            }
        }

        private static void HandleEnchanting(string hand) {
            // Go to
            if(Me.ZoneId != 139) {
                CastDeathgate();
                TakeDeathgate();
                return;
            }

            if(!TookTeleporter) {
                NavigateToTeleporter();
                return;
            }

            NavigateToRuneforge();

            if(!Runeforge.WithinInteractRange) {
                return;
            }

            if(Lua.GetReturnVal<bool>("return StaticPopup1:IsVisible()", 0)) {
                Lua.DoString("StaticPopup1Button1:Click()");
                return;
            }

            if(!Lua.GetReturnVal<bool>("return TradeSkillFrame:IsVisible()", 0)) {
                if(!Me.IsMoving) {
                    WoWSpell.FromId(TradeSkillID).Cast();
                    return;
                }
            } else {
                switch(hand) {
                    case "main":
                        if(RunOnceMainhand) {
                            return;
                        }

                        Lua.DoString("DoTradeSkill(" + GetTradeSkillIndex() + ", 1)");

                        var mainHand = StyxWoW.Me.CarriedItems.FirstOrDefault(i => i.Entry == ItemMainHandID);

                        if(mainHand != null) {
                            mainHand.Use();
                        }

                        RunOnceMainhand = true;

                        break;
                    case "off":
                        if(RunOnceOffhand) {
                            return;
                        }

                        Lua.DoString("DoTradeSkill(" + GetTradeSkillIndex() + ", 1)");

                        var offHand = StyxWoW.Me.CarriedItems.FirstOrDefault(i => i.Entry == ItemOffHandID);

                        if(offHand != null) {
                            offHand.Use();
                        }

                        RunOnceOffhand = true;

                        break;
                }

                return;
            }

            if(!IsViable(Me)) {
                return;
            }

            CastDeathgate();
            TakeDeathgate();
        }

        private static bool IsViable(WoWObject pWoWObject) {
            return pWoWObject != null && pWoWObject.IsValid && StyxWoW.IsInGame;
        }

        private static void CastDeathgate() {
            if(Me.IsCasting || Me.IsChanneling || Me.IsMoving) {
                return;
            }

            var deathGateRemainingCooldown = WoWSpell.FromId(DeathgateId).CooldownTimeLeft.Seconds;

            if(!WoWSpell.FromId(DeathgateId).Cooldown) {
                if(!SpellManager.CanCast(DeathgateId)) {
                    return;
                }

                SpellManager.Cast(DeathgateId);
            } else {
                if(deathGateRemainingCooldown > 0 && deathGateRemainingCooldown < 45) {
                    CustomNormalLog("Waiting for Death Gate to get off cooldown. {0} seconds remaining.", deathGateRemainingCooldown);
                }
            }
        }

        private static void FindDeathgate() {
            Deathgate = ObjectManager.GetObjectsOfTypeFast<WoWObject>().FirstOrDefault(gate => gate.IsValid && gate.Entry == 190942);
        }

        private static void TakeDeathgate() {
            FindDeathgate();

            if(Deathgate != null) {
                Deathgate.Interact();
            }
        }

        private static void NavigateToTeleporter() {
            // Upper teleporter - 207580
            // Lower teleporter - 207581
            Teleporter = ObjectManager.GetObjectsOfTypeFast<WoWObject>().FirstOrDefault(teleportPad => teleportPad.IsValid && teleportPad.Entry == 207580);

            if(Teleporter == null) {
                CustomNormalLog("No teleporter found.");
                return;
            }

            if(Teleporter != null && Teleporter.Location.Distance(Me.Location) <= 3) {
                TookTeleporter = true;
                return;
            }

            Navigator.MoveTo(Teleporter.Location);
        }

        private static void NavigateToRuneforge() {
            if(Runeforge == null) {
                Runeforge = ObjectManager.GetObjectsOfTypeFast<WoWObject>().FirstOrDefault(runeforge => runeforge.IsValid && runeforge.Entry == GetRandomRuneforge());
            }

            if(Runeforge == null || Runeforge.WithinInteractRange) {
                return;
            }

            var runeforgeLocation = WoWMovement.CalculatePointFrom(Runeforge.Location, 7f);

            Navigator.MoveTo(runeforgeLocation);
        }

        private static int GetRandomRuneforge() {
            var rand = new Random();

            switch(rand.Next(0, 3)) {
                case 0:
                    return 207577; // right runeforge
                case 1:
                    return 207579; // back runeforge
                case 2:
                    return 207578; // left runeforge
            }

            return 0;
        }

        private static int GetTradeSkillIndex() {
            var numTradeSkills = Lua.GetReturnVal<int>("return GetNumTradeSkills()", 0);

            for(var i = 1; i <= numTradeSkills; i++) {
                var link = Lua.GetReturnVal<string>(string.Format("return GetTradeSkillItemLink({0})", i), 0);

                // Make sure it's not a category!
                if(string.IsNullOrEmpty(link)) {
                    continue;
                }

                link = link.Remove(0, link.IndexOf(':') + 1);

                link = link.Remove(link.IndexOf(':') != -1 ? link.IndexOf(':') : link.IndexOf('|'));

                var id = int.Parse(link);

                if(id == SpellID) {
                    return i;
                }
            }

            return 0;
        }
    }
}
