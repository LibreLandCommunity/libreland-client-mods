using HarmonyLib;
using BepInEx;
using UnityEngine;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Collections;
using System.Reflection.Emit;

public static class PluginInfo {
    public const string GUID = "dev.librelandcommunity.client";
    public const string NAME = "LibreLand-Client";
    public const string VERSION = "0.0.1";
}

[BepInPlugin(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
public class LibreLandClient : BaseUnityPlugin
{
    private readonly Harmony harmony = new Harmony(PluginInfo.GUID);

    public static string baseUrl = "http://127.0.0.1:8000";
    public static string thingDefinitionUrl = "http://127.0.0.1:8001";
    public static string thingDefinitionAreaBundleUrl = "http://127.0.0.1:8002";
    //public static string thingDefinitionAreaBundleUrl = "http://127.0.0.1:8002";

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
        public static IEnumerable<CodeInstruction> Transpile1(IEnumerable<CodeInstruction> instructions)
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
        public static IEnumerable<CodeInstruction> Transpiler2(IEnumerable<CodeInstruction> instructions)
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
}