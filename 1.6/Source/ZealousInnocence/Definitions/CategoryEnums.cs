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
        NonAdult
    }
}
