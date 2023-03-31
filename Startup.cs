using HarmonyLib;
using System;
using System.IO;
using System.Reflection;
using UnityModManagerNet;

namespace DontSayAnythingGuys
{
    public static class Startup
    {
        public static void Load(UnityModManager.ModEntry modEntry)
        {
            foreach (string path in Directory.GetFiles(Path.Combine(modEntry.Path, "dlls"), "*.dll"))
                Assembly.LoadFrom(path);
            AccessTools.Method($"{typeof(Startup).Namespace}.Main:Setup").Invoke(null, new object[] { modEntry });
        }
    }
}
