using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ZealousInnocence
{    /*public class Graphic_HpBased : Graphic_Single
    {
        public override Material MatSingle
        {
            get
            {
                LogStackTrace();

                var apparel = this.currentlyRenderedThing as Apparel;
                if (apparel != null)
                {
                    Log.Message($"Changing render.");
                    var hpPercent = (float)apparel.HitPoints / apparel.MaxHitPoints;
                    if (hpPercent < 0.5f)
                    {
                        return MaterialPool.MatFrom(this.path + "_Dirty", ShaderDatabase.Cutout);
                    }
                }
                else
                {
                    if(currentlyRenderedThing != null)
                    {
                        Log.Message($"Is reported {currentlyRenderedThing.LabelShort}.");
                    }
                    else
                    {
                        Log.Message($"Is reported null.");
                    }
                    
                }
                return base.MatSingle;
            }
        }
        public override Material MatAt(Rot4 rot, Thing thing = null)
        {
            if(thing != null) this.currentlyRenderedThing = thing;

            return MatSingle;
        }
        public override void Print(SectionLayer layer, Thing thing, float extraRotation)
        {
            if(thing != null) this.currentlyRenderedThing = thing;
            this.currentlyRenderedThing = thing;
            base.Print(layer, thing, extraRotation);
        }
        public override Material MatSingleFor(Thing thing)
        {
            if (thing != null) this.currentlyRenderedThing = thing;
            return base.MatSingleFor(thing);
        }
        // Token: 0x06002312 RID: 8978 RVA: 0x000D42A9 File Offset: 0x000D24A9
        public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
		{
            this.currentlyRenderedThing = thing;
            Log.Message($"Call draw.");
            base.DrawWorker(loc, rot, thingDef, thing, extraRotation);
		}
        private void LogStackTrace()
        {
            StackTrace stackTrace = new StackTrace();
            Log.Message(stackTrace.ToString());
        }

        private Thing currentlyRenderedThing;
    }*/
}
