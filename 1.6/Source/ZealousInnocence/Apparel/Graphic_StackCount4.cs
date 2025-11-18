using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ZealousInnocence
{
    // Works like vanilla Graphic_StackCount, but with 4 tiers:
    //  a = 1
    //  b = (2 .. <= 25% of stackLimit)
    //  d = (>25% .. <= 50% of stackLimit)   <-- your new mid-low tier
    //  c = (>50% .. up to stackLimit)
    //
    // Falls back gracefully if not all four are present.
    public class Graphic_StackCount4 : Graphic_Collection
    {
        private Graphic gA, gB, gC, gD;

        public override void Init(GraphicRequest req)
        {
            base.Init(req);

            // Expected single images (no directions): <base>_a.png, _b.png, _c.png, _d.png
            gA = FindBySuffix("_a") ?? SafeGet(0);
            gB = FindBySuffix("_b") ?? SafeGet(1);
            gC = FindBySuffix("_c") ?? SafeGet(2);
            gD = FindBySuffix("_d") ?? SafeGet(3);
#if DEBUG
            // One-time helpful warning if a stage is missing (prevents silent fallback headaches)
            if (gA == BaseContent.BadGraphic || gB == BaseContent.BadGraphic
                || gC == BaseContent.BadGraphic || gD == BaseContent.BadGraphic)
            {
                Log.Warning($"[Graphic_StackCount4] One or more stages missing under '{path}'. " +
                            "Expected *_a, *_b, *_c, *_d as single textures. " +
                            $"SubGraphics={subGraphics?.Length ?? 0}, maskPath={(maskPath ?? "null")}");
            }
#endif
        }

        // ---- Colored clone overloads (both) ----

        public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
        {
            Color fixedColor = new Color(0.96f, 0.96f, 0.96f, 1f);   // light gray
            Color fixedColorTwo = new Color(0.96f, 0.96f, 0.96f, 1f);   // same for secondary
            // Preserve our class, data, shader params, and mask path
            return GraphicDatabase.Get(
                this.GetType(),
                this.path,
                newShader ?? this.Shader,
                this.drawSize,
                fixedColor,
                fixedColorTwo,
                this.data,
                this.data?.shaderParameters,
                this.maskPath
            );
        }

        // ---- Selection helpers ----

        private Graphic FindBySuffix(string suffix)
        {
            if (subGraphics == null) return null;
            for (int i = 0; i < subGraphics.Length; i++)
            {
                var g = subGraphics[i];
                var p = g?.path;
                if (!string.IsNullOrEmpty(p) && p.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    return g;
            }
            return null;
        }

        private Graphic SafeGet(int idx)
        {
            if (subGraphics != null && idx >= 0 && idx < subGraphics.Length && subGraphics[idx] != null)
                return subGraphics[idx];
            return BaseContent.BadGraphic;
        }
        private Graphic PickFor(Thing t)
        {
            if (t == null) return gA ?? SafeGet(0);

            int count = t.stackCount;
            int limit = Mathf.Max(1, t.def?.stackLimit ?? 1);

            // exact 1 → A
            if (count <= 1) return gA ?? SafeGet(0);

            // define proportional thresholds
            int bMax = Mathf.FloorToInt(limit * 0.4f); // 40% of max stack = B range end
            int cMax = Mathf.FloorToInt(limit * 0.9f); // 90% of max stack = C range end

            // ensure thresholds are monotonic
            bMax = Mathf.Clamp(bMax, 2, Math.Max(2, limit - 2));
            cMax = Mathf.Clamp(cMax, bMax + 1, Math.Max(bMax + 1, limit - 1));

            if (count <= bMax) return gB ?? gA ?? SafeGet(0);         // up to ~40% full
            if (count < limit) return gC ?? gB ?? gA ?? SafeGet(0);  // up to ~90% full
            return gD ?? gC ?? gB ?? gA ?? SafeGet(0);                // full stack
        }

        public override Material MatSingleFor(Thing thing)
        {
            var g = PickFor(thing);
            return g?.MatSingleFor(thing) ?? BaseContent.BadMat;
        }

        public override Material MatAt(Rot4 rot, Thing thing = null)
        {
            var g = PickFor(thing);
            return g?.MatAt(rot, thing) ?? BaseContent.BadMat;
        }
    }
}
