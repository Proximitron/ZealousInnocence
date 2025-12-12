using DubsBadHygiene;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ZealousInnocence
{
    public class ZealousInnocenceSettings : ModSettings
    {
        private const int CurrentSettingsVersion = 163;
        public int settingVersion = -1;
        public bool reduceAge = true;
        //public float targetChronoAge = 10f;
        //public bool formerAdultsNeedLearning = true;
        //public bool formerAdultsCanHaveIdeoRoles = true;
        public bool bladderForRaidCaravanVisitors = true;

        public bool dynamicGenetics = true;
        public float adultBedwetters = 0.05f;

        public float generalBladderControlFactor = 1.00f;
        public float generalNighttimeControlFactor = 1.00f;
        public bool useBedPan = false;
        public bool useBedPanIfDiaperEquipped = true;

        public bool faecesActive = false;

        public float bladderRelieveAmount = 0.2f;

        public float needDiapers = 0.45f;
        public float needPullUp = 0.6f;

        public Color solingColor = new Color(0.6f, 0.3f, 0.0f);
        public Color wettingColor = Color.yellow;

        public bool debugging = false;
        public bool debuggingCloth = false;
        public bool debuggingJobs = false;
        public bool debuggingCapacities = false;
        public bool debuggingGenes = false;
        public bool debuggingBedwetting = false;
        public bool debuggingRegression = false;
        public bool debuggingApparelGenerator = false;
        public bool debuggingRects = false; // draw layout boxes
        public bool debuggingHarmonyPatching = false;
        public bool debuggingNeeds = false;

        // --- behavior toggles ---
        public bool enableTwoUp = true;  // pair rows side-by-side when wide enough
        public bool snapToUIScale = true;  // snap draw rects like vanilla Label()
        public bool tinyFontFallback = true;  // try Tiny for tight right label
        public bool enableTwoColRight = true;  // allow two columns inside right area

        // --- size thresholds (base pixels; multiplied by UI scale automatically) ---
        public float minTwoUpCellWidth = 260f;  // min half-cell width to even try two-up
        public float fullWidthHeightGate = 48f;   // if half-cell predicted taller than this, draw full width
        public float twoColTriggerH = 36f;   // when right text wraps taller than this, go 2 internal cols
        public float twoColMinWidth = 160f;  // only allow internal 2-col if right area at least this wide

        // --- spacing/padding (base pixels; scaled with UI) ---
        public float pairGap = 12f;   // space between left/right cells in a pair
        public float paddingX = 17f;   // left/right padding inside a cell
        public float gapInside = 10f;   // space between left label and right label
        public float twoColGap = 6f;    // gap between internal columns (when split)
        public float rowMinHeight = 20f;   // baseline row height
        public float extraRowSpacing = 0f;    // extra vertical spacing after each final row

        // --- proportions ---
        public float maxLeftFrac = 0.58f; // cap for left label width as fraction of content area

        /// <summary>
        /// Base amount of regression severity healed per in-game day under neutral conditions.
        /// </summary>
        public float Regression_BaseRecoveryPerDay = 1.00f;

        /// <summary>
        /// Multiplier applied when pawn is in bed (resting bonus).
        /// </summary>
        public float Regression_RestingMultiplier = 1.0f;

        /// <summary>
        /// Multiplier applied when pawn is not yet adult (child bonus).
        /// </summary>
        public float Regression_ChildMultiplier = 1.0f;

        /// <summary>
        /// Multiplier applied to animals (shorter lifecycle).
        /// </summary>
        public float Regression_AnimalMultiplier = 2.0f;


        public float Regression_RessurectChance = 0.5f;

        // ========== Learning ==========

        public int Regression_PullupChange = 6;
        public int Regression_DiaperChange = 9;
        public int Regression_DiaperChangeOthers = 13;

        // ========== Skill Masking ==========

        /// <summary>
        /// The amount of the skill levels that are hidden while max severity
        /// </summary>
        public float Regression_LevelMaskBySeverity = 0.7f;


        // Ageing complications caused by regress being reversed
        /*public bool AgingComplications_Enabled = true;
        public float AgingComplications_RiskPerYear = 0.15f; // chance per crossed birthday
        public int AgingComplications_MaxNew = 1;      // cap per aging event
        */

        public float gabSize = 12f;
        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref settingVersion, "settingVersion", -1);
            Scribe_Values.Look(ref reduceAge, "reduceAge", true);
            /*Scribe_Values.Look(ref targetChronoAge, "targetChronoAge", 10f);
            Scribe_Values.Look(ref formerAdultsNeedLearning, "formerAdultsNeedLearning", true);
            Scribe_Values.Look(ref formerAdultsCanHaveIdeoRoles, "formerAdultsCanHaveIdeoRoles", true);*/
            Scribe_Values.Look(ref bladderForRaidCaravanVisitors, "bladderForRaidCaravanVisitors", true);
            

            Scribe_Values.Look(ref dynamicGenetics, "dynamicGenetics", true);
            Scribe_Values.Look(ref adultBedwetters, "adultBedwetters", 0.05f);

            Scribe_Values.Look(ref generalBladderControlFactor, "generalBladderControlFactor", 1.0f);
            Scribe_Values.Look(ref generalNighttimeControlFactor, "generalNighttimeControlFactor", 1.0f);
            Scribe_Values.Look(ref needDiapers, "needDiapers", 0.45f);
            Scribe_Values.Look(ref needPullUp, "needPullUp", 0.6f);

            Scribe_Values.Look(ref useBedPan, "useBedPan", false);
            Scribe_Values.Look(ref useBedPanIfDiaperEquipped, "useBedPanIfDiaperEquipped", false);

            Scribe_Values.Look(ref faecesActive, "faecesActive", false);
            
            Scribe_Values.Look(ref bladderRelieveAmount, "bladderRelieveAmount", 0.2f);

            Scribe_Values.Look(ref debugging, "debugging", false);
            Scribe_Values.Look(ref debuggingCloth, "debuggingCloth", false);
            Scribe_Values.Look(ref debuggingJobs, "debuggingJobs", false);
            Scribe_Values.Look(ref debuggingCapacities, "debuggingCapacities", false);
            Scribe_Values.Look(ref debuggingGenes, "debuggingGenes", false);
            Scribe_Values.Look(ref debuggingBedwetting, "debuggingBedwetting", false);
            Scribe_Values.Look(ref debuggingRegression, "debuggingRegression", false);
            Scribe_Values.Look(ref debuggingApparelGenerator, "debuggingApparelGenerator", false);
            Scribe_Values.Look(ref debuggingRects, "debuggingRects", false);
            Scribe_Values.Look(ref debuggingHarmonyPatching, "debuggingHarmonyPatching", false);
            Scribe_Values.Look(ref debuggingNeeds, "debuggingNeeds", false);
            

            Scribe_Values.Look(ref enableTwoUp, "enableTwoUp", true);
            Scribe_Values.Look(ref snapToUIScale, "snapToUIScale", true);
            Scribe_Values.Look(ref tinyFontFallback, "tinyFontFallback", true);
            Scribe_Values.Look(ref enableTwoColRight, "enableTwoColRight", true);

            Scribe_Values.Look(ref minTwoUpCellWidth, "minTwoUpCellWidth", 260f);
            Scribe_Values.Look(ref fullWidthHeightGate, "fullWidthHeightGate", 48f);
            Scribe_Values.Look(ref twoColTriggerH, "twoColTriggerH", 36f);
            Scribe_Values.Look(ref twoColMinWidth, "twoColMinWidth", 160f);

            Scribe_Values.Look(ref pairGap, "pairGap", 12f);
            Scribe_Values.Look(ref paddingX, "paddingX", 17f);
            Scribe_Values.Look(ref gapInside, "gapInside", 10f);
            Scribe_Values.Look(ref twoColGap, "twoColGap", 6f);
            Scribe_Values.Look(ref rowMinHeight, "rowMinHeight", 22f);
            Scribe_Values.Look(ref extraRowSpacing, "extraRowSpacing", 0f);

            Scribe_Values.Look(ref maxLeftFrac, "maxLeftFrac", 0.58f);

            Scribe_Values.Look(ref Regression_BaseRecoveryPerDay, "Regression_BaseRecoveryPerDay", 1.00f);
            Scribe_Values.Look(ref Regression_RestingMultiplier, "Regression_RestingMultiplier", 1.0f);
            Scribe_Values.Look(ref Regression_ChildMultiplier, "Regression_ChildMultiplier", 1.0f);
            Scribe_Values.Look(ref Regression_AnimalMultiplier, "Regression_AnimalMultiplier", 2.0f);

            Scribe_Values.Look(ref Regression_PullupChange, "Regression_PullupChange", 6);
            Scribe_Values.Look(ref Regression_DiaperChange, "Regression_DiaperChange", 9);
            Scribe_Values.Look(ref Regression_DiaperChangeOthers, "Regression_DiaperChangeOthers", 13);

            Scribe_Values.Look(ref Regression_LevelMaskBySeverity, "Regression_LevelMaskBySeverity", 0.7f);

            if (Scribe.mode == LoadSaveMode.LoadingVars && settingVersion != CurrentSettingsVersion)
            {
                Log.Message($"[ZI] Resetting all settings because of major version change or first start {settingVersion} => {CurrentSettingsVersion}");
                var oldPoop = faecesActive;
                ResetAllToDefaults();
                settingVersion = CurrentSettingsVersion;
                faecesActive = oldPoop;
            }
        }
        private void ResetAllToDefaults()
        {
            // Create a fresh default instance to copy values from
            var defaults = new ZealousInnocenceSettings();

            var flags = BindingFlags.Instance | BindingFlags.Public;
            foreach (var field in typeof(ZealousInnocenceSettings)
                     .GetFields(flags)
                     .Where(f => !f.IsStatic))
            {
                field.SetValue(this, field.GetValue(defaults));
            }
        }

        public Listing_Standard list;
        public int tab;
        public void DoWindowContents(Rect canvas)
        {
            Rect baseRect = canvas;
            baseRect.y += 35f;
            canvas.height -= 35f;
            canvas.y += 35f;
            Widgets.DrawMenuSection(canvas);
            List<TabRecord> tabs = new List<TabRecord>
            {
                new TabRecord("MainFeatures".Translate(), delegate()
                {
                    this.tab = 0;
                    base.Write();
                }, this.tab == 0),
                new TabRecord("SettingRegressionFeatures".Translate(), delegate()
                {
                    this.tab = 1;
                    base.Write();
                }, this.tab == 1),
                new TabRecord("dbh.ExtraFeatures".Translate(), delegate()
                {
                    this.tab = 2;
                    base.Write();
                }, this.tab == 2),
                new TabRecord("Debugging", delegate()
                {
                    this.tab = 3;
                    base.Write();
                }, this.tab == 3),
            };
            TabDrawer.DrawTabs<TabRecord>(baseRect, tabs, 200f);
            if (this.tab == 0)
            {
                this.DoMainTab(canvas.ContractedBy(10f));
            }
            else if (this.tab == 1)
            {
                this.DoRegression(canvas.ContractedBy(10f));
            }
            else if (this.tab == 2)
            {
                this.DoExtraTab(canvas.ContractedBy(10f));
            }
            else if (this.tab == 3)
            {
                this.DebuggingTab(canvas.ContractedBy(10f));
            }
        }
        public void DoMainTab(Rect canvas)
        {
            list = new Listing_Standard();
            list.ColumnWidth = (canvas.width - 40f) * 0.34f;
            list.Begin(canvas);
            list.GapLine(gabSize);

            list.CheckboxLabeled("SettingReduceAge".Translate(), ref reduceAge, "SettingReduceAgeHelp".Translate());

            /*list.GapLine(gabSize);
            list.Label("SettingRitualAgeResult".Translate() + ": " + targetChronoAge, tooltip: "SettingRitualAgeResultHelp".Translate());
            targetChronoAge = (float)System.Math.Round(list.Slider(targetChronoAge, 3, 13));

            if (reduceAge)
            {
                list.GapLine(gabSize);
                list.TextEntry("SettingNotWithForeverYoung".Translate());

                if (ModsConfig.IdeologyActive)
                {
                    list.CheckboxLabeled("SettingIdeologyRoles".Translate(), ref formerAdultsCanHaveIdeoRoles, "SettingIdeologyRolesHelp".Translate());
                }
                list.CheckboxLabeled("SettingLearningNeed".Translate(), ref formerAdultsNeedLearning, "SettingLearningNeedHelp".Translate());
            }*/
            list.GapLine(gabSize);
            list.TextEntry("SettingRequiresRestart".Translate());
            list.CheckboxLabeled("SettingEnableBladderForRaidVisitorCaravans".Translate(), ref bladderForRaidCaravanVisitors, "SettingEnableBladderForRaidVisitorCaravansHelp".Translate());

            this.list.NewColumn();
            this.list.ColumnWidth = (canvas.width - 40f) * 0.33f;
            list.GapLine(gabSize);

            list.CheckboxLabeled("SettingDynamicGenetics".Translate(), ref dynamicGenetics, "SettingDynamicGeneticsHelp".Translate());
            if (dynamicGenetics)
            {
                list.GapLine(gabSize);
                list.Label("SettingAdultBedwetterChance".Translate() + $": {Math.Round(adultBedwetters * 100)}%", tooltip: "SettingAdultBedwetterChanceHelp".Translate(NamedArgumentUtility.Named("5", "CHANCE")));
                adultBedwetters = list.Slider(adultBedwetters, 0f, 1f);
            }

            this.list.NewColumn();
            this.list.ColumnWidth = (canvas.width - 40f) * 0.33f;
            list.GapLine(gabSize);

            list.Label("SettingBladderControlFactor".Translate() + $": {Math.Round(generalBladderControlFactor * 100)}%", tooltip: "SettingBladderControlFactor".Translate(NamedArgumentUtility.Named("100", "CHANCE")));
            generalBladderControlFactor = list.Slider(generalBladderControlFactor, 0f, 2f);
            list.Label("SettingNighttimeControlFactor".Translate() + $": {Math.Round(generalNighttimeControlFactor * 100)}%", tooltip: "SettingNighttimeControlFactorHelp".Translate(NamedArgumentUtility.Named("100", "CHANCE")));
            generalNighttimeControlFactor = list.Slider(generalNighttimeControlFactor, 0f, 2f);
            list.GapLine(gabSize);
            list.Label("SettingNeedDiapersOnBladderControl".Translate() + $": {Math.Round(needDiapers * 100)}%", tooltip: "SettingNeedDiapersOnBladderControlHelp".Translate(NamedArgumentUtility.Named("45", "CHANCE")));
            needDiapers = list.Slider(needDiapers, 0f, 1f);
            list.Label("SettingNeedPullupsOnBladderControl".Translate() + $": {Math.Round(needPullUp * 100)}%", tooltip: "SettingNeedPullupsOnBladderControlHelp".Translate(NamedArgumentUtility.Named("60", "CHANCE")));
            needPullUp = list.Slider(needPullUp, 0f, 1f);
            list.GapLine(gabSize);
            list.CheckboxLabeled("SettingUseBedPan".Translate(), ref useBedPan, "SettingUseBedPanHelp".Translate());
            if (useBedPan)
            {
                list.CheckboxLabeled("SettingUseBedPanIfDiaperEquipped".Translate(), ref useBedPanIfDiaperEquipped, "SettingUseBedPanIfDiaperEquippedHelp".Translate());
            }
            list.GapLine(gabSize);
            list.CheckboxLabeled("SettingActivateFaeces".Translate(), ref faecesActive, "SettingActivateFaecesHelp".Translate());
            list.GapLine(gabSize);
            list.Label("SettingBladderRelieveAmount".Translate() + $": {Math.Round(bladderRelieveAmount * 100)}%", tooltip: "SettingBladderRelieveAmountHelp".Translate(NamedArgumentUtility.Named("20", "CHANCE")));
            bladderRelieveAmount = list.Slider(bladderRelieveAmount, 0.05f, 1f);
            list.End();
        }
        public void DoRegression(Rect inRect)
        {
            list = new Listing_Standard();
            list.ColumnWidth = (inRect.width - 40f) * 0.5f;
            list.Begin(inRect);
            list.GapLine(gabSize);

            // --- Regression Healing / Decay ---
            list.Label("SettingRegressionHealing".Translate(), -1f, tooltip: "SettingRegressionHealingHelp".Translate());

            SliderPct(list, "SettingBaseRecovery".Translate(), ref Regression_BaseRecoveryPerDay,
                0f, Prefs.DevMode ? 10.0f : 3.00f, 1.00f,
                "SettingBaseRecoveryHelp".Translate());

            SliderPct(list, "SettingRestingMultiplier".Translate(), ref Regression_RestingMultiplier,
                0.25f, 3f, 1.0f,
                "SettingRestingMultiplierHelp".Translate());

            SliderPct(list, "SettingChildMultiplier".Translate(), ref Regression_ChildMultiplier,
                0.25f, 3f, 1.0f,
                "SettingChildMultiplierHelp".Translate());

            SliderPct(list, "SettingAnimalMultiplier".Translate(), ref Regression_AnimalMultiplier,
                0.5f, 6f, 2.0f,
                "SettingAnimalMultiplierHelp".Translate());

            SliderPct(list, "SettingRessurectChance".Translate(), ref Regression_RessurectChance,
                0f, 1f, 0.5f,
                "SettingRessurectChanceHelp".Translate());

            list.NewColumn();
            list.ColumnWidth = (inRect.width - 40f) * 0.5f;

            // --- Regression Healing / Decay ---
            list.Label("SettingRegressionLearning".Translate(), -1f, tooltip: "SettingRegressionLearningHelp".Translate());

            SliderInt(list, "SettingLearnChangePullups".Translate(), ref Regression_PullupChange,
                3, 20, 6,
                "SettingLearnChangePullupsHelp".Translate());

            SliderInt(list, "SettingLearnChangeDiapers".Translate(), ref Regression_DiaperChange,
                3, 20, 9,
                "SettingLearnChangeDiapersHelp".Translate());

            SliderInt(list, "SettingLearnChangeDiapers".Translate(), ref Regression_DiaperChangeOthers,
                3, 20, 13,
                "SettingLearnChangeDiapersHelp".Translate());

            list.GapLine(gabSize);

            // --- Skill Masking ---
            list.Label("SettingSkillMasking".Translate(), -1f, tooltip: "SettingSkillMaskingHelp".Translate());

            SliderPct(list, "SettingLevelMaskBySeverity".Translate(), ref Regression_LevelMaskBySeverity,
                0f, 1f, 0.7f,
                "SettingLevelMaskBySeverityHelp".Translate());

            list.GapLine(gabSize);
            if (list.ButtonText("SettingResetDefaults".Translate()))
                ResetRegressionToDefaults();

            list.End();
        }
        public void DoExtraTab(Rect inRect)
        {
            list = new Listing_Standard();
            list.ColumnWidth = (inRect.width - 40f) * 0.5f;
            list.Begin(inRect);
            list.GapLine(gabSize);

            // --- Toggles ---
            list.CheckboxLabeled("SettingEnableTwoUp".Translate(), ref enableTwoUp, "SettingEnableTwoUpHelp".Translate());
            list.CheckboxLabeled("SettingSnapToUiScale".Translate(), ref snapToUIScale, "SettingSnapToUiScaleHelp".Translate());
            list.CheckboxLabeled("SettingTinyFontFallback".Translate(), ref tinyFontFallback, "SettingTinyFontFallbackHelp".Translate());
            list.CheckboxLabeled("SettingEnableTwoColRight".Translate(), ref enableTwoColRight, "SettingEnableTwoColRightHelp".Translate());
            list.GapLine(gabSize);

            // --- Thresholds ---
            SliderPx(list, "SettingMinTwoUpCellWidth".Translate(), ref minTwoUpCellWidth, 200f, 400f, 260f,
                "SettingMinTwoUpCellWidthHelp".Translate());
            SliderPx(list, "SettingFullWidthHeightGate".Translate(), ref fullWidthHeightGate, 32f, 96f, 48f,
                "SettingFullWidthHeightGateHelp".Translate());
            SliderPx(list, "SettingTwoColTriggerHeight".Translate(), ref twoColTriggerH, 24f, 80f, 36f,
                "SettingTwoColTriggerHeightHelp".Translate());
            SliderPx(list, "SettingTwoColMinWidth".Translate(), ref twoColMinWidth, 120f, 260f, 160f,
                "SettingTwoColMinWidthHelp".Translate());

            list.NewColumn();
            list.ColumnWidth = (inRect.width - 40f) * 0.5f;

            // --- Spacing ---
            SliderPx(list, "SettingPairGap".Translate(), ref pairGap, 4f, 32f, 12f, "SettingPairGapHelp".Translate());
            SliderPx(list, "SettingPaddingX".Translate(), ref paddingX, 8f, 28f, 17f, "SettingPaddingXHelp".Translate());
            SliderPx(list, "SettingGapInside".Translate(), ref gapInside, 6f, 24f, 10f, "SettingGapInsideHelp".Translate());
            SliderPx(list, "SettingTwoColGap".Translate(), ref twoColGap, 3f, 18f, 6f, "SettingTwoColGapHelp".Translate());
            SliderPx(list, "SettingRowMinHeight".Translate(), ref rowMinHeight, 18f, 32f, 20f, "SettingRowMinHeightHelp".Translate());
            SliderPx(list, "SettingExtraRowSpacing".Translate(), ref extraRowSpacing, 0f, 12f, 0f, "SettingExtraRowSpacingHelp".Translate());

            // --- Proportions ---
            SliderPct(list, "SettingMaxLeftWidthFrac".Translate(), ref maxLeftFrac, 0.40f, 0.80f, 0.58f,
                "SettingMaxLeftWidthFracHelp".Translate());

            list.GapLine(gabSize);
            if (list.ButtonText("SettingResetDefaults".Translate()))
                ResetUiToDefaults();

            list.End();
        }
        /*
this.DoLinks(canvas);
this.row.GapLine(12f);

if (Current.ProgramState == ProgramState.Playing)
{
    GUI.color = Color.red;
    this.row.Label("QuitToMenuToChange".Translate(), -1f, null);
    GUI.color = Color.white;

    this.< DoConfigurationTab > g__joe | 49_0("PassiveWaterCoolersLink".Translate(), ref this.PassiveWaterCoolers, "PassiveWaterCoolersLinkTip".Translate());
    this.< DoConfigurationTab > g__joe | 49_0("fixtureQuality".Translate(), ref this.QualityComps, "fixtureQualityTip".Translate());
}
else
{

    this.row.CheckboxLabeledWithAction("PassiveWaterCoolersLink".Translate(), ref this.PassiveWaterCoolers, "PassiveWaterCoolersLinkTip".Translate(), new Action(Settings.< DoConfigurationTab > g__resolver | 49_4));
    bool qualityComps = this.QualityComps;
    this.row.CheckboxLabeledWithAction("fixtureQuality".Translate(), ref this.QualityComps, "fixtureQualityTip".Translate(), new Action(Settings.< DoConfigurationTab > g__resolver | 49_4));
    if (!qualityComps && this.QualityComps)
    {
        base.Write();
        TaggedString taggedString3 = "dbh.restartconfirm".Translate();
        TaggedString taggedString4 = "GoBack".Translate();
        Find.WindowStack.Add(new Dialog_MessageBox("fixtureQualityRequiresRestart".Translate(), taggedString4, new Action(this.< DoConfigurationTab > g__coooom | 49_5), taggedString3, new Action(GenCommandLine.Restart), null, true, null, null, WindowLayer.Dialog));
    }
}
this.row.GapLine(12f);

this.row.CheckboxLabeled("PrivacyChecks".Translate(), ref this.PrivacyChecks, "PrivacyChecksTip".Translate(), 0f, 1f);

this.row.CheckboxLabeled("dbh.PriorityIndoorCleaning".Translate(), ref Settings.PriorityIndoorCleaning, "dbh.PriorityIndoorCleaningTip".Translate(), 0f, 1f);
*/
        private bool fyChecked;
        private bool fyActive;
        public void DebuggingTab(Rect canvas)
        {
            list = new Listing_Standard();
            list.ColumnWidth = (canvas.width - 40f) * 0.34f;
            list.Begin(canvas);
            list.GapLine(gabSize);

            list.CheckboxLabeled("SettingDebugMode".Translate(), ref debugging, "SettingDebugModeHelp".Translate());

            list.NewColumn();
            list.ColumnWidth = (canvas.width - 40f) * 0.33f;
            list.GapLine(gabSize);

            if (debugging)
            {
                list.CheckboxLabeled("SettingDebugCloth".Translate(), ref debuggingCloth, "SettingDebugClothHelp".Translate());
                list.CheckboxLabeled("SettingDebugJobs".Translate(), ref debuggingJobs, "SettingDebugJobsHelp".Translate());
                list.CheckboxLabeled("SettingDebugCapacities".Translate(), ref debuggingCapacities, "SettingDebugCapacitiesHelp".Translate());
                list.CheckboxLabeled("SettingDebugGenes".Translate(), ref debuggingGenes, "SettingDebugGenesHelp".Translate());
                list.CheckboxLabeled("SettingDebugBedwetting".Translate(), ref debuggingBedwetting, "SettingDebugBedwettingHelp".Translate());
                list.CheckboxLabeled("SettingDebugRegression".Translate(), ref debuggingRegression, "SettingDebugRegressionHelp".Translate());
                list.CheckboxLabeled("SettingDebugApparel".Translate(), ref debuggingApparelGenerator, "SettingDebugApparelHelp".Translate());
                list.CheckboxLabeled("SettingDebugRects".Translate(), ref debuggingRects, "SettingDebugRectsHelp".Translate());
                list.CheckboxLabeled("SettingDebugHarmony".Translate(), ref debuggingHarmonyPatching, "SettingDebugHarmonyHelp".Translate());
                list.CheckboxLabeled("SettingDebugNeeds".Translate(), ref debuggingNeeds, "SettingDebugNeedsHelp".Translate());
            }
            else
            {
                debuggingCloth = false;
                debuggingJobs = false;
                debuggingCapacities = false;
                debuggingGenes = false;
                debuggingBedwetting = false;
                debuggingRegression = false;
                debuggingApparelGenerator = false;
                debuggingRects = false;
                debuggingHarmonyPatching = false;
                debuggingNeeds = false;
            }

            list.NewColumn();
            list.ColumnWidth = (canvas.width - 40f) * 0.33f;
            list.GapLine(gabSize);

            if (debugging)
            {
                if (list.ButtonText("SettingCheckForeverYoung".Translate()))
                {
                    fyActive = ModChecker.ForeverYoungActive();
                    fyChecked = true;
                }

                if (fyChecked)
                {
                    list.GapLine(gabSize);
                    list.Label(fyActive
                        ? "SettingForeverYoungActive".Translate()
                        : "SettingForeverYoungNotActive".Translate());
                }
            }

            list.End();
        }
        private static void SliderPx(Listing_Standard list, string label, ref float val, float min, float max, float def, string tooltip = null)
        {
            val = Mathf.Round(list.SliderLabeled($"{label}: {val:F0}px", val, min, max, tooltip: tooltip));
            if (list.ButtonTextLabeled($"↺ {label}", "SettingResetSingle".Translate())) val = def;
        }
        private static void SliderPct(Listing_Standard list, string label, ref float val, float min, float max, float def, string tooltip = null)
        {
            val = Mathf.Round(list.SliderLabeled($"{label}: {(val * 100f):F0}%", val, min, max, tooltip: tooltip) * 100f) / 100f;
            if (list.ButtonTextLabeled($"↺ {label}", "SettingResetSingle".Translate())) val = def;
        }
        private void SliderInt(Listing_Standard list, string label, ref int val, int min, int max, int def, string tooltip = null)
        {
            float f = val;
            f = list.SliderLabeled($"{label}: {val}", f, min, max, tooltip: tooltip);
            if (list.ButtonTextLabeled($"↺ {label}", "SettingResetSingle".Translate())) val = def;
            val = Mathf.RoundToInt(f);
        }
        private void ResetUiToDefaults()
        {
            enableTwoUp = true;
            snapToUIScale = true;
            tinyFontFallback = true;
            enableTwoColRight = true;

            minTwoUpCellWidth = 260f;
            fullWidthHeightGate = 48f;
            twoColTriggerH = 36f;
            twoColMinWidth = 160f;

            pairGap = 12f;
            paddingX = 17f;
            gapInside = 10f;
            twoColGap = 6f;
            rowMinHeight = 20f;
            extraRowSpacing = 0f;

            maxLeftFrac = 0.58f;
        }

        private void ResetRegressionToDefaults()
        {
            Regression_BaseRecoveryPerDay = 1.0f;
            Regression_RestingMultiplier = 1.0f;
            Regression_ChildMultiplier = 1.0f;
            Regression_AnimalMultiplier = 2.0f;
            Regression_RessurectChance = 0.5f;

            Regression_PullupChange = 6;
            Regression_DiaperChange = 9;
            Regression_DiaperChangeOthers = 13;

            Regression_LevelMaskBySeverity = 0.7f;
            
        }

    }
}
