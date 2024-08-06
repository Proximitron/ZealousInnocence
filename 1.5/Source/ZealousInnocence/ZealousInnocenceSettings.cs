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
        }

        public Listing_Standard row;
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
            row = new Listing_Standard();
            row.ColumnWidth = (canvas.width - 40f) * 0.34f;
            row.Begin(canvas);
            row.GapLine(gabSize);

            row.CheckboxLabeled("SettingReduceAge".Translate(), ref reduceAge, "SettingReduceAgeHelp".Translate());

            row.GapLine(gabSize);
            row.Label("SettingRitualAgeResult".Translate() + ": " + targetChronoAge, tooltip: "SettingRitualAgeResultHelp".Translate());
            targetChronoAge = (float)System.Math.Round(row.Slider(targetChronoAge, 3, 13));

            if (reduceAge)
            {
                row.GapLine(gabSize);
                row.TextEntry("SettingNotWithForeverYoung".Translate());
                row.CheckboxLabeled("SettingExtraGrowthMoments", ref formerAdultsGetGrowthMoments, "SettingExtraGrowthMomentsHelp".Translate());

                if (ModsConfig.IdeologyActive)
                {
                    row.CheckboxLabeled("SettingIdeologyRoles".Translate(), ref formerAdultsCanHaveIdeoRoles, "SettingIdeologyRolesHelp".Translate());
                }
                row.CheckboxLabeled("SettingLearningNeed".Translate(), ref formerAdultsNeedLearning, "SettingLearningNeedHelp".Translate());
            }

            this.row.NewColumn();
            this.row.ColumnWidth = (canvas.width - 40f) * 0.33f;
            row.GapLine(gabSize);

            row.CheckboxLabeled("SettingDynamicGenetics".Translate(), ref dynamicGenetics, "SettingDynamicGeneticsHelp".Translate());
            if (dynamicGenetics)
            {
                row.GapLine(gabSize);
                row.Label("SettingAdultBedwetterChance".Translate() + $": {Math.Round(adultBedwetters * 100)}%", tooltip: "SettingAdultBedwetterChanceHelp".Translate(NamedArgumentUtility.Named("5", "CHANCE")));
                adultBedwetters = row.Slider(adultBedwetters, 0f, 1f);
            }

            this.row.NewColumn();
            this.row.ColumnWidth = (canvas.width - 40f) * 0.33f;
            row.GapLine(gabSize);

            row.Label("SettingBladderControlFactor".Translate() + $": {Math.Round(generalBladderControlFactor * 100)}%", tooltip: "SettingBladderControlFactor".Translate(NamedArgumentUtility.Named("100", "CHANCE")));
            generalBladderControlFactor = row.Slider(generalBladderControlFactor, 0f, 2f);
            row.Label("SettingNighttimeControlFactor".Translate() + $": {Math.Round(generalNighttimeControlFactor * 100)}%", tooltip: "SettingNighttimeControlFactorHelp".Translate(NamedArgumentUtility.Named("100", "CHANCE")));
            generalNighttimeControlFactor = row.Slider(generalNighttimeControlFactor, 0f, 2f);
            row.GapLine(gabSize);
            row.Label("SettingNeedDiapersOnBladderControl".Translate() + $": {Math.Round(needDiapers * 100)}%", tooltip: "SettingNeedDiapersOnBladderControlHelp".Translate(NamedArgumentUtility.Named("45", "CHANCE")));
            needDiapers = row.Slider(needDiapers, 0f, 1f);
            row.Label("SettingNeedPullupsOnBladderControl".Translate() + $": {Math.Round(needPullUp * 100)}%", tooltip: "SettingNeedPullupsOnBladderControlHelp".Translate(NamedArgumentUtility.Named("60", "CHANCE")));
            needPullUp = row.Slider(needPullUp, 0f, 1f);
            row.GapLine(gabSize);
            row.CheckboxLabeled("SettingUseBedPan".Translate(), ref useBedPan, "SettingUseBedPanHelp".Translate());
            if (useBedPan)
            {
                row.CheckboxLabeled("SettingUseBedPanIfDiaperEquipped".Translate(), ref useBedPanIfDiaperEquipped, "SettingUseBedPanIfDiaperEquippedHelp".Translate());
            }
            row.GapLine(gabSize);
            row.CheckboxLabeled("SettingActivateFaeces".Translate(), ref faecesActive, "SettingActivateFaecesHelp".Translate());

            row.End();

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
        public void DoExtraTab(Rect canvas)
        {
            row = new Listing_Standard();
            row.ColumnWidth = (canvas.width - 40f) * 0.99f;
            row.Begin(canvas);
            row.GapLine(gabSize);
            row.TextEntry("Coming soon!");
            this.row.End();
        }

        public void DebuggingTab(Rect canvas)
        {
            this.row = new Listing_Standard();
            this.row.ColumnWidth = (canvas.width - 40f) * 0.34f;
            this.row.Begin(canvas);
            row.GapLine(gabSize);

            row.CheckboxLabeled("SettingDebugMode".Translate(), ref debugging, "".Translate());

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
            this.row.NewColumn();
            this.row.ColumnWidth = (canvas.width - 40f) * 0.33f;
            row.GapLine(gabSize);

            if (debugging)
            {
                row.CheckboxLabeled("DEBUG Cloth", ref debuggingCloth, "Generates debugging related to cloth.");
                row.CheckboxLabeled("DEBUG Jobs", ref debuggingJobs, "Generates debugging related to jobs.");
                row.CheckboxLabeled("DEBUG Capacities", ref debuggingCapacities, "Generates debugging related to capacities like bladder control.");
                row.CheckboxLabeled("DEBUG Genes", ref debuggingGenes, "Generates debugging related to genes and creation of gene related conditions and changes.");
                row.CheckboxLabeled("DEBUG Bedwetting", ref debuggingBedwetting, "Generates debugging related to all bedwetting related functions and calls.");
                row.CheckboxLabeled("DEBUG Apparel", ref debuggingApparelGenerator, "Generates debugging related the apparel generator, used on generating new pawns.");

            }
            this.row.NewColumn();
            this.row.ColumnWidth = (canvas.width - 40f) * 0.33f;
            row.GapLine(gabSize);
            if (debugging && row.ButtonText("Check ForeverYoung active"))
            {
                if (ModChecker.ForeverYoungActive())
                {
                    row.GapLine(gabSize);
                    row.TextEntry("'ForeverYoung' IS active!");
                }
                else
                {
                    row.GapLine(gabSize);
                    row.TextEntry("'ForeverYoung' NOT active!");
                }
            }
            this.row.End();
        }
    }
}
