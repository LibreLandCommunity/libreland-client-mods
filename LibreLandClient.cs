using HarmonyLib;
using BepInEx;
using UnityEngine;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Configuration;
using System.Data;

public static class PluginInfo {
    public const string GUID = "dev.librelandcommunity.client";
    public const string NAME = "LibreLand-Client";
    public const string VERSION = "0.0.8";
}

[BepInPlugin(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
public class LibreLandClient : BaseUnityPlugin
{
    private readonly Harmony harmony = new Harmony(PluginInfo.GUID);

    public static string baseUrl = "http://127.0.0.1:8000";
    public static string baseThingDefinitionUrl = "http://127.0.0.1:8001";
    public static string baseThingDefinitionAreaBundleUrl = "http://127.0.0.1:8002";
    public static string baseSteamScreenshotPrefixHTTP = "http://127.0.0.1:8003";
    public static string baseSteamScreenshotPrefixHTTPS = "http://127.0.0.1:8003";

    private static ConfigEntry<string> urlConfig;
    private static ConfigEntry<string> thingDefinitionUrlConfig;
    private static ConfigEntry<string> thingDefinitionAreaBundleUrlConfig;
    private static ConfigEntry<string> steamScreenshotPrefixHTTPConfig;
    private static ConfigEntry<string> steamScreenshotPrefixHTTPSConfig;

    private static ConfigEntry<string> usernameConfig;
    private static ConfigEntry<string> passwordConfig;

    private void Awake()
    {
        urlConfig = Config.Bind("URL", "BaseURL", baseUrl, "Replacement url that used to point to 'anyland.com'");
        thingDefinitionUrlConfig = Config.Bind("URL", "ThingDefinitionURL", baseThingDefinitionUrl, "Replacement url that used to points to things");
        thingDefinitionAreaBundleUrlConfig = Config.Bind("URL", "ThingDefinitionAreaBundleUrl", baseThingDefinitionAreaBundleUrl, "Replacement url that used to points to areas");
        steamScreenshotPrefixHTTPConfig = Config.Bind("URL", "SteamScreenshotPrefixHTTP", baseSteamScreenshotPrefixHTTP, "Replacement url that used to points to HTTP steam screenshots");
        steamScreenshotPrefixHTTPSConfig = Config.Bind("URL", "SteamScreenshotPrefixHTTPS", baseSteamScreenshotPrefixHTTPS, "Replacement url that used to points to HTTPS steam screenshots");
        
        usernameConfig = Config.Bind("USER", "Username", System.Environment.MachineName, "");
        passwordConfig = Config.Bind("USER", "Password", "REPLACEME", "");

        Logger.LogInfo($"Plugin {PluginInfo.GUID} is loaded!");
        harmony.PatchAll();
    }

    [HarmonyPatch(typeof(ServerManager))]
    class ServerManagerPatch {
        [HarmonyPatch(nameof(ServerManager.Startup))]
        [HarmonyPrefix]
        static bool ChangeBaseUrl(ServerManager __instance, string ___serverBaseUrl){
            Debug.Log($"Changing RemoteServerBaseUrl to {urlConfig.Value}");

            try {
                MethodInfo setterMethod = typeof(ServerManager).GetProperty("status", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetSetMethod(true);
                setterMethod.Invoke(__instance, new object[] { ManagerStatus.Initializing }); 
                __instance.RemoteServerBaseUrl = urlConfig.Value;
                ___serverBaseUrl = __instance.RemoteServerBaseUrl;
                __instance.StartAuthentication();
                return false;
            } catch (Exception ex) {
                Debug.LogError("It's fucked!");
                Debug.LogError(ex);
                return true;
            }
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
}