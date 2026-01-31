using BepInEx;
using BepInEx.Hacknet;
using BepInEx.Logging;
using Hacknet.Extensions;
using HacknetFontReplace.Core;
using HacknetFontReplace.Core.Actions;
using Pathfinder.Action;
using Pathfinder.Meta.Load;
using System;
using System.Reflection;

namespace HacknetFontReplace
{
    [BepInPlugin(ModGUID, ModName, ModVer)]
    [IgnorePlugin]
    public class HacknetFontReplacePlugin : HacknetPlugin
    {
        public const string ModGUID = "HacknetFontReplace-WEDF156WFWEF1WEFV15";
        public const string ModName = "HacknetFontReplace";
        public const string ModVer = "1.0.1";
        public static HacknetFontReplacePlugin Instance { get; private set; }
        public static ManualLogSource Logger => Instance.Log;
        public override bool Load()
        {
            Instance = this;
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            HarmonyInstance.PatchAll(Instance.GetType().Assembly);
            GameFontReplace.Init();
            GameSaveAttach.Init();
            ActionManager.RegisterAction<ChangeActiveFontGroup>(nameof(ChangeActiveFontGroup));
            return true;
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (ExtensionLoader.ActiveExtensionInfo == null)
            {
                return null;
            }

            var assemblyName = new AssemblyName(args.Name).Name;
            var dllName = assemblyName.Replace(".", "_");
            if (dllName.StartsWith(nameof(HacknetFontReplace)))
            {
                return null;
            }

            Logger.LogInfo($"try load {assemblyName} from memory resource ");
            var dllBytes = Properties.Resource.ResourceManager.GetObject(dllName);
            if (dllBytes is byte[] bytes)
            {
                return Assembly.Load(bytes);
            }

            Logger.LogError($"error to load {assemblyName}, not find.");
            return null;
        }
    }
}
