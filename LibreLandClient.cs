using HarmonyLib;
using BepInEx;
using UnityEngine;
using System.Runtime.CompilerServices;
using System.Reflection;

public static class PluginInfo {
    public const string GUID = "dev.librelandcommunity.client";
    public const string NAME = "LibreLand-Client";
    public const string VERSION = "0.0.1";
}

[BepInPlugin(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
public class LibreLandClient : BaseUnityPlugin
{
    private readonly Harmony harmony = new Harmony(PluginInfo.GUID);

    public static string baseUrl = "Geoff";
    public static string thingDefinitionUrl = "Deez";
    public static string thingDefinitionAreaBundleUrl = "Nuts";

    private void Awake()
    {
        Logger.LogInfo($"Plugin {PluginInfo.GUID} is loaded!");
        harmony.PatchAll();
    }

    // Test patch to see if changing stuff worked
    // CURRENTLY BROKEN BUT COMPILES DON'T USE YET
    [HarmonyPatch(typeof(ServerManager), nameof(ServerManager.Startup))]
    class ServerManagerPatch {
        [HarmonyPrefix]
        static bool ChangeBaseUrl(ServerManager __instance, string ___serverBaseUrl){
            Debug.Log($"Chaing RemoteServerBaseUrl to {baseUrl}");

            // I gotta remember how I did this... I don't have a backup of my old mod :(
            FieldInfo info = AccessTools.Field(typeof(ServerManager), "status");
            try {
                info.SetValue(__instance, ManagerStatus.Initializing);
            } catch {
                Debug.LogError("It's fucked!");
            }
            ___serverBaseUrl = __instance.RemoteServerBaseUrl = baseUrl;
            __instance.StartAuthentication();

            return false;
        }

        [HarmonyPostfix]
        static void ValidateChangeBaseUrl(ServerManager __instance) {
            Debug.Log($"Newly Modified RemoteServerBaseUrl: {__instance.RemoteServerBaseUrl}");
            Debug.Log($"Newly Modified ManagerStatus: {__instance.status}");
            Debug.Log($"Newly Modified GetThingDefinition_CDN: {ServerURLs.GetThingDefinition_CDN}");
            Debug.Log($"Newly Modified GetThingDefinitionAreaBundle_CDN: {ServerURLs.GetThingDefinitionAreaBundle_CDN}");
        }
    }

    // Temp not working will figure out later
    //[HarmonyPatch(typeof(ServerURLs), "GetThingDefinition_CDN", MethodType.Getter)]
    //static class ServerURLsPatch {
    //    [HarmonyPostfix]
    //    static void ChangeThingDefinitionCDN(ref string __result) {
    //        Debug.Log("Patching GetThingDefinition_CDN");
    //        __result = thingDefinitionUrl;
    //    }
    //    //[HarmonyPatch(typeof(ServerURLs))]
    //    //[HarmonyPatch("GetThingDefinitionAreaBundle_CDN", MethodType.Getter)]
    //    //[HarmonyPostfix]
    //    //static void ChangeThingDefinitionAreaBundleCDN(ref string __result) {
    //    //    Debug.Log("Patching GetThingDefinitionAreaBundle_CDN");
    //    //    __result = thingDefinitionAreaBundleUrl;
    //    //}
    //}
}
