using HarmonyLib;
using BepInEx;
using UnityEngine;
using System.Reflection;
using System.Reflection.Emit;

public static class PluginInfo {
    public const string GUID = "dev.librelandcommunity.client";
    public const string NAME = "LibreLand-Client";
    public const string VERSION = "0.0.4";
}

[BepInPlugin(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
public class LibreLandClient : BaseUnityPlugin
{
    private readonly Harmony harmony = new Harmony(PluginInfo.GUID);

    public static string baseUrl = "http://127.0.0.1:8000";
    public static string thingDefinitionUrl = "http://127.0.0.1:8001";
    public static string thingDefinitionAreaBundleUrl = "http://127.0.0.1:8002";
    public static string steamScreenshotPrefixHTTP = "http://127.0.0.1:8003";
    public static string steamScreenshotPrefixHTTPS = "http://127.0.0.1:8003";

    private void Awake()
    {
        Logger.LogInfo($"Plugin {PluginInfo.GUID} is loaded!");
        harmony.PatchAll();
    }

    [HarmonyPatch(typeof(ServerManager))]
    class ServerManagerPatch {
        [HarmonyPatch(nameof(ServerManager.Startup))]
        [HarmonyPrefix]
        static bool ChangeBaseUrl(ServerManager __instance, string ___serverBaseUrl){
            Debug.Log($"Changing RemoteServerBaseUrl to {baseUrl}");

            try {
                MethodInfo setterMethod = typeof(ServerManager).GetProperty("status", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetSetMethod(true);
                setterMethod.Invoke(__instance, new object[] { ManagerStatus.Initializing }); 
                ___serverBaseUrl = __instance.RemoteServerBaseUrl = baseUrl;
                __instance.StartAuthentication();
                return true;
            } catch (Exception ex) {
                Debug.LogError("It's fucked!");
                Debug.LogError(ex);
                return false;
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
                    instructionsList[i] = new CodeInstruction(OpCodes.Ldstr, thingDefinitionUrl);
                }
            }

            // Return the modified instructions
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
                    instructionsList[i] = new CodeInstruction(OpCodes.Ldstr, thingDefinitionAreaBundleUrl);
                }
            }

            return instructionsList.AsEnumerable();
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
                __result = __result.Replace("http://steamuserimages-a.akamaihd.net/ugc/", steamScreenshotPrefixHTTP);
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
                __result = __result.Replace("http://steamuserimages-a.akamaihd.net/ugc/", steamScreenshotPrefixHTTP);
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
                    instructionsList[i] = new CodeInstruction(OpCodes.Ldstr, steamScreenshotPrefixHTTP.Replace("http://", ""));
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
                    instructionsList[i] = new CodeInstruction(OpCodes.Ldstr, steamScreenshotPrefixHTTPS);
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
                    instructionsList[i] = new CodeInstruction(OpCodes.Ldstr, steamScreenshotPrefixHTTPS);
                }
            }
            
            return instructionsList;
        }
    }
}