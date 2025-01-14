using HarmonyLib;
using BepInEx;
using UnityEngine;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Configuration;

public static class PluginInfo {
    public const string GUID = "dev.librelandcommunity.client";
    public const string NAME = "Libreland-Client";
    public const string VERSION = "1.0.0";
}

[BepInPlugin(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
public class LibrelandClient : BaseUnityPlugin
{
    private readonly Harmony harmony = new Harmony(PluginInfo.GUID);

    public static string baseUrl = "http://127.0.0.1:8000";
    public static string baseThingDefinitionUrl = "http://127.0.0.1:8001";
    public static string baseThingDefinitionAreaBundleUrl = "http://127.0.0.1:8002";
    public static string baseSteamScreenshotPrefixHTTP = "http://127.0.0.1:8003";
    public static string baseSteamScreenshotPrefixHTTPS = "http://127.0.0.1:8003";

    ConfigFile urls;
    ConfigFile user;

    private static ConfigEntry<string> urlConfig;
    private static ConfigEntry<string> thingDefinitionUrlConfig;
    private static ConfigEntry<string> thingDefinitionAreaBundleUrlConfig;
    private static ConfigEntry<string> steamScreenshotPrefixHTTPConfig;
    private static ConfigEntry<string> steamScreenshotPrefixHTTPSConfig;
    private static ConfigEntry<string> punAppIDConfig;
    private static ConfigEntry<string> punChatIDConfig;
    private static ConfigEntry<string> punVoiceIDConfig;

    private static ConfigEntry<string> usernameConfig;
    private static ConfigEntry<string> passwordConfig;

    private void Awake()
    {
        urls = new ConfigFile(Path.Combine(Paths.ConfigPath, "Libreland-Server-Urls.cfg"), true);
        user = new ConfigFile(Path.Combine(Paths.ConfigPath, "Libreland-Server-User.cfg"), true);

        urlConfig = urls.Bind("URL", "BaseURL", baseUrl, "Replacement url that used to point to 'anyland.com'");
        thingDefinitionUrlConfig = urls.Bind("URL", "ThingDefinitionURL", baseThingDefinitionUrl, "Replacement url that used to points to things");
        thingDefinitionAreaBundleUrlConfig = urls.Bind("URL", "ThingDefinitionAreaBundleUrl", baseThingDefinitionAreaBundleUrl, "Replacement url that used to points to areas");
        steamScreenshotPrefixHTTPConfig = urls.Bind("URL", "SteamScreenshotPrefixHTTP", baseSteamScreenshotPrefixHTTP, "Replacement url that used to points to HTTP steam screenshots");
        steamScreenshotPrefixHTTPSConfig = urls.Bind("URL", "SteamScreenshotPrefixHTTPS", baseSteamScreenshotPrefixHTTPS, "Replacement url that used to points to HTTPS steam screenshots");
        punAppIDConfig = urls.Bind("URL", "PUNAppID", PhotonNetwork.PhotonServerSettings.AppID, "Replacement app ID for pun servers");
        punChatIDConfig = urls.Bind("URL", "PUNChatID", PhotonNetwork.PhotonServerSettings.ChatAppID, "Replacement chat ID for pun servers");
        punVoiceIDConfig = urls.Bind("URL", "PUNVoiceID", PhotonNetwork.PhotonServerSettings.VoiceAppID, "Replacement voice ID for pun servers");
        
        usernameConfig = user.Bind("USER", "Username", System.Environment.MachineName, "");
        passwordConfig = user.Bind("USER", "Password", "REPLACEME", "");

        Logger.LogInfo($"Plugin {PluginInfo.GUID} is loaded!");
        harmony.PatchAll();
    }

    [HarmonyPatch(typeof(ServerManager))]
    class ServerManagerPatch {
        [HarmonyPatch(nameof(ServerManager.Startup))]
        [HarmonyPrefix]
        static bool ChangeBaseUrl(ServerManager __instance, string ___serverBaseUrl){
            Debug.Log($"Changing RemoteServerBaseUrl to {urlConfig.Value}");
            __instance.RemoteServerBaseUrl = urlConfig.Value;
            return true;
        }

        [HarmonyPatch(nameof(ServerManager.Startup))]
        [HarmonyPostfix]
        static void Check(ServerManager __instance, string ___serverBaseUrl){
            Debug.Log($"Final RemoteServerBaseUrl value: {__instance.RemoteServerBaseUrl}");
            Debug.Log($"Final ServerBaseUrl value: {___serverBaseUrl}");
        }

        // Stupid transpiler shit, I hate it because the game is such an old unity version the other methods break
        // that's my theory idk tbh anymore
        [HarmonyPatch("GetThingDefinition")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpile1(IEnumerable<CodeInstruction> instructions)
        {
            var instructionsList = instructions.ToList();

            for (int i = 0; i < instructionsList.Count; i++)
            {
                var instruction = instructionsList[i];

                if (instruction.opcode.Equals(OpCodes.Ldsfld) &&
                    instruction.operand is FieldInfo fieldInfo &&
                    fieldInfo.Name.Equals("GetThingDefinition_CDN") &&
                    fieldInfo.DeclaringType.Equals(typeof(ServerURLs)))
                {
                    instructionsList[i] = new CodeInstruction(OpCodes.Ldstr, thingDefinitionUrlConfig.Value);
                }
            }

            return instructionsList.AsEnumerable();
        }

        [HarmonyPatch("GetThingDefinitionAreaBundle")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler2(IEnumerable<CodeInstruction> instructions)
        {
            var instructionsList = instructions.ToList();

            for (int i = 0; i < instructionsList.Count; i++)
            {
                var instruction = instructionsList[i];

                if (instruction.opcode.Equals(OpCodes.Ldsfld) &&
                    instruction.operand is FieldInfo fieldInfo &&
                    fieldInfo.Name.Equals("GetThingDefinitionAreaBundle_CDN") &&
                    fieldInfo.DeclaringType.Equals(typeof(ServerURLs)))
                {
                    instructionsList[i] = new CodeInstruction(OpCodes.Ldstr, thingDefinitionAreaBundleUrlConfig.Value);
                }
            }

            return instructionsList.AsEnumerable();
        }
    }

    [HarmonyPatch(typeof(BroadcastNetworkManager), "OverridePhotonAppId")]
    class BroadcastNetworkManagerPatch {
        [HarmonyPostfix]
        static void Postfix() {
            Debug.Log($"Changing PUN AppID to {punAppIDConfig.Value}");
            PhotonNetwork.PhotonServerSettings.AppID = punAppIDConfig.Value;
            Debug.Log($"Changing PUN ChatID to {punChatIDConfig.Value}");
            PhotonNetwork.PhotonServerSettings.ChatAppID = punChatIDConfig.Value;
            Debug.Log($"Changing PUN VoidID to {punVoiceIDConfig.Value}");
            PhotonNetwork.PhotonServerSettings.VoiceAppID = punVoiceIDConfig.Value;
        }
    }

    [HarmonyPatch(typeof(SteamManager), nameof(SteamManager.GetAuthSessionTicket))]
    class SteamManagerPatch {
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            Debug.Log("Chaning return method of GetAuthSessionTicket");
            var instructionsList = new List<CodeInstruction>();
            instructionsList.Add(new CodeInstruction(OpCodes.Ldstr, $"{usernameConfig.Value}|{passwordConfig.Value}"));
            instructionsList.Add(new CodeInstruction(OpCodes.Ret));
            return instructionsList;
        }
    }

    // This is the ugc stuff I don't quite know how it works but this should all theoretically work
    [HarmonyPatch(typeof(ThingPart), nameof(ThingPart.GetFullImageUrl))]
    class ThingPartPatch {
        [HarmonyPostfix]
        public static void Postfix(ref string __result)
        {
            if (__result.Contains("http://steamuserimages-a.akamaihd.net/ugc/"))
            {
                __result = __result.Replace("http://steamuserimages-a.akamaihd.net/ugc/", steamScreenshotPrefixHTTPConfig.Value);
            }
        }
    }

    [HarmonyPatch(typeof(TextLink))]
    class TextLinkPatch {
        [HarmonyPatch(nameof(TextLink.GetFullUrl))]
        [HarmonyPostfix]
        static void Postfix(ref string __result)
        {
            if (__result.Contains("https://steamuserimages-a.akamaihd.net/ugc/"))
            {
                __result = __result.Replace("http://steamuserimages-a.akamaihd.net/ugc/", steamScreenshotPrefixHTTPSConfig.Value);
            }
        }

        [HarmonyPatch(nameof(TextLink.TryParseURL))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {          
            var instructionsList = instructions.ToList();
            
            for (int i = 0; i < instructionsList.Count; i++)
            {
                var instruction = instructionsList[i];
                
                if (instruction.opcode.Equals(OpCodes.Ldstr) && instruction.operand is string str && str.Equals("steamuserimages-a.akamaihd.net/"))
                {
                    instructionsList[i] = new CodeInstruction(OpCodes.Ldstr, steamScreenshotPrefixHTTPConfig.Value.Replace("http://", ""));
                }
            }
            
            return instructionsList;
        }
    }

    [HarmonyPatch(typeof(ReferenceImageQuad), "Update")]
    class ReferenceImageQuadPatch {
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {          
            var instructionsList = instructions.ToList();
            
            for (int i = 0; i < instructionsList.Count; i++)
            {
                var instruction = instructionsList[i];
                
                if (instruction.opcode.Equals(OpCodes.Ldstr) && instruction.operand is string str && str.Equals("https://steamuserimages-a.akamaihd.net/ugc/"))
                {
                    instructionsList[i] = new CodeInstruction(OpCodes.Ldstr, steamScreenshotPrefixHTTPSConfig.Value);
                }
            }
            
            return instructionsList;
        }
    }

    [HarmonyPatch(typeof(ThingPartCopyPasteDialog), "PasteImageIfValidates")]
    class ThingPartCopyPasteDialogPatch {
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {          
            var instructionsList = instructions.ToList();
            
            for (int i = 0; i < instructionsList.Count; i++)
            {
                var instruction = instructionsList[i];
                
                if (instruction.opcode.Equals(OpCodes.Ldstr) && instruction.operand is string str && str.Equals("https://steamuserimages-a.akamaihd.net/ugc/"))
                {
                    instructionsList[i] = new CodeInstruction(OpCodes.Ldstr, steamScreenshotPrefixHTTPSConfig.Value);
                }
            }
            
            return instructionsList;
        }
    }

    [HarmonyPatch(typeof(Dialog), "ShowDesktopHelp")]
    class DialogPatch {
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var instructionsList = instructions.ToList();
             for (int i = 0; i < instructionsList.Count; i++)
            {
                var instruction = instructionsList[i];
                
                if (instruction.opcode.Equals(OpCodes.Ldstr) && instruction.operand is string str)
                {
                    if (str.Equals("and explore via PC + keyboard")) {
                        instructionsList[i] = new CodeInstruction(OpCodes.Ldstr, "and explore via PC + keyboard \nArchive by Zetaphor, LeCloutPanda \nand the Anyland community for sharing \ntheir cache with Libreland.");
                    }
                }
            }

            return instructionsList.AsEnumerable();
        }
    }

    [HarmonyPatch(typeof(VideoDialog), "Start")]
    class VideoDialogPatch {
        [HarmonyPostfix]
        static void RemoveVideoPanelBecauseItsBroken(VideoDialog __instance) {
            __instance.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
        }
    }
}