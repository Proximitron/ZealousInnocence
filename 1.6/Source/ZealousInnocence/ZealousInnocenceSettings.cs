using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ZealousInnocence
{
    public class ZealousInnocenceSettings : ModSettings
    {
        
        public bool reduceAge = false;
        public float targetChronoAge = 10f;
        public bool formerAdultsNeedLearning = true;
        public bool formerAdultsCanHaveIdeoRoles = true;
        public bool formerAdultsGetGrowthMoments = false;


        public bool dynamicGenetics = true;
        public float adultBedwetters = 0.05f;

        public float generalBladderControlFactor = 1.00f;
        public float generalNighttimeControlFactor = 1.00f;
        public bool useBedPan = false;
        public bool useBedPanIfDiaperEquipped = true;

        public bool faecesActive = false;
        
        public float needDiapers = 0.45f;
        public float needPullUp = 0.6f;
        
        public bool debugging = false;
        public bool debuggingCloth = false;
        public bool debuggingJobs = false;
        public bool debuggingCapacities = false;
        public bool debuggingGenes = false;
        public bool debuggingBedwetting = false;
        public bool debuggingApparelGenerator = false;
        public bool debuggingRects = false; // draw layout boxes

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



        public float gabSize = 12f;
        public override void ExposeData()
        {
            base.ExposeData();
            
            Scribe_Values.Look(ref reduceAge, "reduceAge", false);
            Scribe_Values.Look(ref targetChronoAge, "targetChronoAge", 10f);
            Scribe_Values.Look(ref formerAdultsNeedLearning, "formerAdultsNeedLearning", true);
            Scribe_Values.Look(ref formerAdultsCanHaveIdeoRoles, "formerAdultsCanHaveIdeoRoles", true);
            Scribe_Values.Look(ref formerAdultsGetGrowthMoments, "formerAdultsGetGrowthMoments", false);


            Scribe_Values.Look(ref dynamicGenetics, "dynamicGenetics", true);
            Scribe_Values.Look(ref adultBedwetters, "adultBedwetters", 0.05f);

            Scribe_Values.Look(ref generalBladderControlFactor, "generalBladderControlFactor", 1.0f);
            Scribe_Values.Look(ref generalNighttimeControlFactor, "generalNighttimeControlFactor", 1.0f);
            Scribe_Values.Look(ref needDiapers, "needDiapers", 0.45f);
            Scribe_Values.Look(ref needPullUp, "needPullUp", 0.6f);

            Scribe_Values.Look(ref useBedPan, "useBedPan", false);
            Scribe_Values.Look(ref useBedPanIfDiaperEquipped, "useBedPanIfDiaperEquipped", false);

            Scribe_Values.Look(ref faecesActive, "faecesActive", false);

            Scribe_Values.Look(ref debugging, "debugging", false);
            Scribe_Values.Look(ref debuggingCloth, "debuggingCloth", false);
            Scribe_Values.Look(ref debuggingJobs, "debuggingJobs", false);
            Scribe_Values.Look(ref debuggingCapacities, "debuggingCapacities", false);
            Scribe_Values.Look(ref debuggingGenes, "debuggingGenes", false);
            Scribe_Values.Look(ref debuggingBedwetting, "debuggingBedwetting", false);
            Scribe_Values.Look(ref debuggingApparelGenerator, "debuggingApparelGenerator", false);

            Scribe_Values.Look(ref debuggingRects, "debuggingRects", false);
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
                new TabRecord("dbh.ExtraFeatures".Translate(), delegate()
                {
                    this.tab = 1;
                    base.Write();
                }, this.tab == 1),
                new TabRecord("Debugging", delegate()
                {
                    this.tab = 2;
                    base.Write();
                }, this.tab == 2),
            };
            TabDrawer.DrawTabs<TabRecord>(baseRect, tabs, 200f);
            if (this.tab == 0)
            {
                this.DoMainTab(canvas.ContractedBy(10f));
            }
            if (this.tab == 1)
            {
                this.DoExtraTab(canvas.ContractedBy(10f));
            }
            if (this.tab == 2)
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

            list.GapLine(gabSize);
            list.Label("SettingRitualAgeResult".Translate() + ": " + targetChronoAge, tooltip: "SettingRitualAgeResultHelp".Translate());
            targetChronoAge = (float)System.Math.Round(list.Slider(targetChronoAge, 3, 13));

            if (reduceAge)
            {
                list.GapLine(gabSize);
                list.TextEntry("SettingNotWithForeverYoung".Translate());
                list.CheckboxLabeled("SettingExtraGrowthMoments", ref formerAdultsGetGrowthMoments, "SettingExtraGrowthMomentsHelp".Translate());

                if (ModsConfig.IdeologyActive)
                {
                    list.CheckboxLabeled("SettingIdeologyRoles".Translate(), ref formerAdultsCanHaveIdeoRoles, "SettingIdeologyRolesHelp".Translate());
                }
                list.CheckboxLabeled("SettingLearningNeed".Translate(), ref formerAdultsNeedLearning, "SettingLearningNeedHelp".Translate());
            }

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

            list.End();

            /*
            this.row.GapLine(12f);
            this.row.DoubleTrouble("DisableNeeds".Translate(), ref this.BladderNeed, ref this.HygieneNeed, "DisableNeedsTip".Translate());
            this.row.DoubleTrouble("PrisonersGetNeeds".Translate(), ref this.PrisonersGetBladder, ref this.PrisonersGetHygiene, "PrisonersGetNeedsTip".Translate());
            this.row.DoubleTrouble("HospitalityGuestsGetNeeds".Translate(), ref this.GuestsGetBladder, ref this.GuestsGetHygiene, "HospitalityGuestsGetNeedsTip".Translate());
            this.row.CheckboxLabeled("AllowNonHuman".Translate(), ref this.AllowNonHuman, "AllowNonHumanTip".Translate(), 0f, 1f);
            this.row.GapLine(12f);
            this.row.CheckboxLabeled("PetsGetBladder".Translate(), ref this.PetsGetBladder, "PetsGetBladderTip".Translate(), 0f, 1f);
            this.row.CheckboxLabeled("WildAnimalsGetBladder".Translate(), ref this.AnimalsGetBladder, "WildAnimalsGetBladderTip".Translate(), 0f, 1f);
            this.row.GapLine(12f);
            */
        }
        public void DoExtraTab(Rect inRect)
        {
            list = new Listing_Standard();
            list.ColumnWidth = (inRect.width - 40f) * 0.5f;
            list.Begin(inRect);
            list.GapLine(gabSize);
            // toggles
            list.CheckboxLabeled("Enable two-up layout", ref enableTwoUp, "Render two rows side-by-side when there’s enough width.");
            list.CheckboxLabeled("Snap to UI scale", ref snapToUIScale, "Snap rectangles like vanilla Label() to avoid blurry text at fractional UI scales.");
            list.CheckboxLabeled("Use Tiny font fallback", ref tinyFontFallback, "Allow switching the right label to Tiny font when it reduces height.");
            list.CheckboxLabeled("Allow two columns (right text)", ref enableTwoColRight, "Split long right text into two narrow columns.");
            list.GapLine(gabSize);

            // thresholds
            SliderPx(list, "Min two-up cell width", ref minTwoUpCellWidth, 200f, 400f, 260f, "Minimum width of each half-cell to attempt two-up.");
            SliderPx(list, "Full-width height gate", ref fullWidthHeightGate, 32f, 96f, 48f, "If a half-cell would be taller than this, render full width instead.");
            SliderPx(list, "Two-column trigger height", ref twoColTriggerH, 24f, 80f, 36f, "If the right text wraps taller than this, use two internal columns.");
            SliderPx(list, "Two-column min width", ref twoColMinWidth, 120f, 260f, 160f, "Require at least this width for the right area to split in two.");

            this.list.NewColumn();
            list.ColumnWidth = (inRect.width - 40f) * 0.5f;
            // spacing
            SliderPx(list, "Pair gap (between cells)", ref pairGap, 4f, 32f, 12f);
            SliderPx(list, "Padding X (inside cell)", ref paddingX, 8f, 28f, 17f);
            SliderPx(list, "Gap inside (left ↔ right)", ref gapInside, 6f, 24f, 10f);
            SliderPx(list, "Two-column gap", ref twoColGap, 3f, 18f, 6f);
            SliderPx(list, "Row min height", ref rowMinHeight, 18f, 32f, 20f);
            SliderPx(list, "Extra row spacing", ref extraRowSpacing, 0f, 12f, 0f, "Add vertical spacing after each completed row (or pair).");

            // proportions
            SliderPct(list, "Max left width fraction", ref maxLeftFrac, 0.40f, 0.80f, 0.58f, "Upper bound for the left label width as a fraction of the inner content area.");

            if (list.ButtonText("Reset to defaults")) ResetUiToDefaults();

            list.End();
        }

        public void DebuggingTab(Rect canvas)
        {
            this.list = new Listing_Standard();
            this.list.ColumnWidth = (canvas.width - 40f) * 0.34f;
            this.list.Begin(canvas);
            list.GapLine(gabSize);

            list.CheckboxLabeled("SettingDebugMode".Translate(), ref debugging, "".Translate());

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
            this.list.NewColumn();
            this.list.ColumnWidth = (canvas.width - 40f) * 0.33f;
            list.GapLine(gabSize);

            if (debugging)
            {
                list.CheckboxLabeled("DEBUG Cloth", ref debuggingCloth, "Generates debugging related to cloth.");
                list.CheckboxLabeled("DEBUG Jobs", ref debuggingJobs, "Generates debugging related to jobs.");
                list.CheckboxLabeled("DEBUG Capacities", ref debuggingCapacities, "Generates debugging related to capacities like bladder control.");
                list.CheckboxLabeled("DEBUG Genes", ref debuggingGenes, "Generates debugging related to genes and creation of gene related conditions and changes.");
                list.CheckboxLabeled("DEBUG Bedwetting", ref debuggingBedwetting, "Generates debugging related to all bedwetting related functions and calls.");
                list.CheckboxLabeled("DEBUG Apparel", ref debuggingApparelGenerator, "Generates debugging related the apparel generator, used on generating new pawns.");

            }
            this.list.NewColumn();
            this.list.ColumnWidth = (canvas.width - 40f) * 0.33f;
            list.GapLine(gabSize);
            if (debugging && list.ButtonText("Check ForeverYoung active"))
            {
                if (ModChecker.ForeverYoungActive())
                {
                    list.GapLine(gabSize);
                    list.TextEntry("'ForeverYoung' IS active!");
                }
                else
                {
                    list.GapLine(gabSize);
                    list.TextEntry("'ForeverYoung' NOT active!");
                }
            }
            this.list.End();
        }
        private static void SliderPx(Listing_Standard list, string label, ref float val, float min, float max, float def, string tooltip = null)
        {
            val = Mathf.Round(list.SliderLabeled($"{label}: {val:F0}px", val, min, max, tooltip: tooltip));
            if (list.ButtonTextLabeled($"↺ {label}", "Reset")) val = def;
        }
        private static void SliderPct(Listing_Standard list, string label, ref float val, float min, float max, float def, string tooltip = null)
        {
            val = Mathf.Round(list.SliderLabeled($"{label}: {(val * 100f):F0}%", val, min, max, tooltip: tooltip) * 100f) / 100f;
            if (list.ButtonTextLabeled($"↺ {label}", "Reset")) val = def;
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
    }
}
