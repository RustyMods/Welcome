using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using HarmonyLib;
using YamlDotNet.Serialization;

namespace Welcome.Introductions;

public static class Intro
{
    private static readonly string FolderPath = Paths.ConfigPath + Path.DirectorySeparatorChar + "Welcome";
    private static readonly string WelcomeFilePath = FolderPath + Path.DirectorySeparatorChar + "CustomIntro.yml";

    private static List<string> WelcomeText = new();
    
    public static void InitCustomIntro()
    {
        if (!Directory.Exists(FolderPath)) Directory.CreateDirectory(FolderPath);
        if (!File.Exists(WelcomeFilePath))
        {
            File.WriteAllLines(WelcomeFilePath, GetDefaultIntro());
        }
        
        WelcomeText = File.ReadAllLines(WelcomeFilePath).ToList();
    }

    private static void SendToClients(string data, ZNetPeer peer)
    {
        ZPackage zPackage = new ZPackage();
        zPackage.Write(data);
        peer.m_rpc.Invoke(nameof(RPC_Receive_Welcome),zPackage);
    }

    private static void RPC_Receive_Welcome(ZRpc rpc, ZPackage pkg)
    {
        WelcomePlugin.WelcomeLogger.LogDebug("Client: Received server custom welcome");
        string data = pkg.ReadString();
        IDeserializer deserializer = new DeserializerBuilder().Build();
        List<string> dataList = deserializer.Deserialize<List<string>>(data);
        WelcomeText = dataList;
    }

    [HarmonyPatch(typeof(ZNet), nameof(ZNet.OnNewConnection))]
    private static class OnNewConnectionPatch
    {
        private static void Postfix(ZNet __instance, ZNetPeer peer)
        {
            if (!__instance) return;
            peer.m_rpc.Register<ZPackage>(nameof(RPC_Receive_Welcome),RPC_Receive_Welcome);
            if (__instance.IsServer())
            {
                WelcomePlugin.WelcomeLogger.LogDebug("Server: New connection, sending custom welcome");
                ISerializer serializer = new SerializerBuilder().Build();
                string data = serializer.Serialize(WelcomeText);
                SendToClients(data, peer);
            }
        }
    }

    private static List<string> GetDefaultIntro()
    {
        return new List<string>
        {
            "<color=orange>Welcome</color>",
            "",
            "He watches the great ships swinging",
            "Like birds on the tide's vast flow,",
            "And out of the past swift winging",
            "Come visions that grip and glow",
            "Fierce fights of forgotten rover,",
            "Adventurous deeds and bold",
            "Of ancestors who sailed over",
            "Grim seas with some Viking old;",
            "",
            "And stirred by an old, old longing,",
            "An urge that dead ages fling,",
            "He thrills to memories thronging",
            "Of some long gone old sea king,",
            "And dreams with a deep emotion",
            "Of wonderful days to be",
            "When he sails over the ocean",
            "A thrall to its mystery."
        };
    }

    public static string ConvertWelcomeList()
    {
        StringBuilder builder = new StringBuilder();
        foreach (string line in WelcomeText)
        {
            builder.Append("\n");
            builder.Append(line);
        }

        return builder.ToString();
    }
}