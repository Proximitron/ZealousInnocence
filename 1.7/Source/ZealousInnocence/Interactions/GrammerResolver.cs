using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.Grammar;

namespace ZealousInnocence.Interactions
{
    public class SpeakUp_DialogManager
    {
        public static readonly System.Type StatementType;
        public static readonly System.Type TalkType;
        public static readonly FieldInfo Statement_Emitter;
        public static readonly FieldInfo Statement_Receiver;
        public static readonly FieldInfo Statement_IntDef;
        public static readonly FieldInfo Statement_Talk;

        static SpeakUp_DialogManager()
        {
            // Look for the types in SpeakUp.dll
            StatementType = AccessTools.TypeByName("SpeakUp.Statement");
            TalkType = AccessTools.TypeByName("SpeakUp.Talk");

            if (StatementType == null)
            {
                Log.Warning("[ZI] Could not reflect SpeakUp.Statement – SpeakUp integration disabled.");
                return; // ⚠️ IMPORTANT: don't try to GetField on a null type
            }

            if (TalkType == null)
                Log.Warning("[ZI] Could not reflect SpeakUp.Talk – SpeakUp integration may be limited.");

            Statement_Emitter = StatementType.GetField("Emitter", BindingFlags.Public | BindingFlags.Instance);
            Statement_Receiver = StatementType.GetField("Reciever", BindingFlags.Public | BindingFlags.Instance);
            Statement_IntDef = StatementType.GetField("IntDef", BindingFlags.Public | BindingFlags.Instance);
            Statement_Talk = StatementType.GetField("Talk", BindingFlags.Public | BindingFlags.Instance);

            if (Statement_Emitter == null || Statement_Receiver == null || Statement_IntDef == null)
            {
                Log.Warning("[ZI] SpeakUp.Statement fields not as expected – integration may be limited.");
            }
        }

        public static void FireStatement_Postfix(object statement)
        {
            try
            {
                // If SpeakUp is not present or type missing → abort safely
                if (StatementType == null)
                    return;

                if (statement == null || !StatementType.IsInstanceOfType(statement))
                    return;

                var emitterObj = Statement_Emitter?.GetValue(statement);
                var receiverObj = Statement_Receiver?.GetValue(statement);
                var intDefObj = Statement_IntDef?.GetValue(statement);
                //var talkObj   = Statement_Talk?.GetValue(statement);

                var emitter = emitterObj as Pawn;
                var receiver = receiverObj as Pawn;
                var intDef = intDefObj as InteractionDef;

                if (emitter == null || receiver == null || intDef == null)
                    return;

                Log.Warning($"[ZI] Message with defName {intDef.defName}");
            }
            catch (System.Exception e)
            {
                Log.Error($"[ZI] Exception in FireStatement_Postfix: {e}");
                // swallow to avoid breaking SpeakUp / base game
            }
        }
    }

}
