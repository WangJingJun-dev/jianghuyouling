using System;
using UnityEngine;

namespace JianghuYouling
{
    internal static class AISettingsSave
    {
        internal static bool Checked(bool saved, string field)
        {
            if (saved)
            {
                Debug.Log("[JHYL_AI_SETTINGS] save field=" + field + " ok=true");
                return true;
            }
            Debug.LogError("[JHYL_AI_SETTINGS] save field=" + field + " ok=false");
            AISettingsHost.QueueRefresh();
            return false;
        }

        internal static void Require(bool saved, string field)
        {
            if (!Checked(saved, field)) throw new InvalidOperationException(field + "_save_failed");
        }
    }
}
