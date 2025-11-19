using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZealousInnocence
{
    public enum DiaperSituationCategory : byte
    {
        Trashed,
        Spent,
        Used,
        Clean,
    }

    public enum DiaperLikeCategory : byte
    {
        Neutral,
        Liked,
        Disliked,
        Child,
        Toddler
    }

    [System.Flags]
    public enum AgeStage
    {
        None = 0,
        Baby = 1 << 0,
        Toddler = 1 << 1,
        Child = 1 << 2,
        Teen = 1 << 3,
        Adult = 1 << 4,
        Old = 1 << 5,
    }
}
