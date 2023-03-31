using HarmonyLib;

namespace DontSayAnythingGuys.Patch
{
    public static class MutePatch
    {
        [HarmonyPatch(typeof(scrController), "Hit")]
        public static class Patch_scrController_Hit
        {
            public static void Postfix(bool __result)
            {
                if (__result && scrController.instance.currentSeqID == Main.Settings.tile)
                {
                    Main.MuteUser();
                }
            }
        }

        [HarmonyPatch(typeof(scrController), "Start_Rewind")]
        public static class Patch_scrController_Start_Rewind
        {
            public static void Postfix(int _currentSeqID)
            {
                if (_currentSeqID >= Main.Settings.tile)
                {
                    Main.MuteUser();
                }
            }
        }
    }
}
