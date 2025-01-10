namespace LibreLandClient;
using HarmonyLib;
using BepInEx;
using UnityEngine;

public static class PluginInfo {
    public const string GUID = "dev.librelandcommunity.client";
    public const string NAME = "LibreLand-Client";
    public const string VERSION = "0.0.1";
}

[BepInPlugin(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
public class LibreLandClient : BaseUnityPlugin
{
    private void Awake()
    {
        Logger.LogInfo($"Plugin {PluginInfo.GUID} is loaded!");

        Harmony harmony = new Harmony(PluginInfo.GUID);
        harmony.PatchAll();
    }

    [HarmonyPatch(typeof(ServerManager), nameof(ServerManager.RemoteServerBaseUrl), MethodType.Getter)]
    private static bool PatchRemoteServerBaseUrl(string __result){
        __result = "Deez";
        return false;
    }

    // Test patch to see if changing stuff worked
    [HarmonyPatch(typeof(ServerManager), nameof("Setup"))]
    private static bool ServerManagerPatch() {
        return false;
    }
}
