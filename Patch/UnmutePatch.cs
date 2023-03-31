using HarmonyLib;

namespace DontSayAnythingGuys.Patch
{
    public static class UnmutePatch
    {
        [HarmonyPatch(typeof(scrController), "Fail2Action")]
        public static class Patch_scrController_Fail2Action
        {
            public static void Postfix()
            {
                if (Main.Settings.unmuteOnEnd)
                    Main.UnmuteUser();
            }
        }

        [HarmonyPatch(typeof(scrController), "OnLandOnPortal")]
        public static class Patch_scrController_OnLandOnPortal
        {
            public static void Postfix()
            {
                if (Main.Settings.unmuteOnEnd)
                    Main.UnmuteUser();
            }
        }

        [HarmonyPatch(typeof(scnEditor), "SwitchToEditMode")]
        public static class Patch_scnEditor_SwitchToEditMode
        {
            public static void Postfix()
            {
                if (Main.Settings.unmuteOnEnd)
                    Main.UnmuteUser();
            }
        }
    }
}
