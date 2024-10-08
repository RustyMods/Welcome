﻿using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Welcome.Managers;

namespace Welcome.Introductions;

public static class Patches
{
    private static bool m_firstSpawn = true;
    [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
    private static class ModifyIntroText
    {
        private static void Prefix(Player __instance)
        {
            if (!__instance || __instance != Player.m_localPlayer || !m_firstSpawn) return;
            if (WelcomePlugin._PluginEnabled.Value is WelcomePlugin.Toggle.Off) return;
            if (WelcomePlugin._AlwaysShowIntro.Value is WelcomePlugin.Toggle.Off) return;
            TextViewer.instance.ShowText(TextViewer.Style.Intro, "CustomIntro", Intro.ConvertWelcomeList(), true);
            m_firstSpawn = false;
        }
    }

    [HarmonyPatch(typeof(Game), nameof(Game.Logout))]
    private static class Game_Logout_Patch
    {
        private static void Postfix() => m_firstSpawn = true;
    }
    
    [HarmonyPatch(typeof(Game), nameof(Game.ShowIntro))]
    private static class ValkyrieTextOverride
    {
        private static void Prefix(Game __instance)
        {
            if (!__instance) return;
            if (WelcomePlugin._PluginEnabled.Value is WelcomePlugin.Toggle.Off) return;
            __instance.m_introTopic = "CustomIntro";
            __instance.m_introText = Intro.ConvertWelcomeList();
        }
    }

    [HarmonyPatch(typeof(TextViewer), nameof(TextViewer.ShowText))]
    private static class TextViewerShowTextPatch
    {
        private static void Postfix(TextViewer __instance, TextViewer.Style style, string topic)
        {
            if (!__instance) return;
            if (style is not TextViewer.Style.Intro || topic != "CustomIntro") return;
            if (WelcomePlugin._UseCustomBackground.Value is WelcomePlugin.Toggle.Off) return;
            if (WelcomePlugin._CustomBackgroundName.Value.IsNullOrWhiteSpace()) return;
            
            GameObject screen = __instance.m_animatorIntro.gameObject;
            // bkg blob Crawl dark_lower_border
            
            Transform bkg = screen.transform.Find("bkg");
            if (bkg)
            {
                if (bkg.TryGetComponent(out Image OverlayImage))
                {
                    OverlayImage.enabled = WelcomePlugin._UseBackgroundOverlay.Value is WelcomePlugin.Toggle.On;
                }
            }

            Transform blob = screen.transform.Find("blob");
            if (!blob) return;
            if (!blob.TryGetComponent(out Image BlobImage)) return;
            if (TextureManager.CustomBackground == null)
            {
                WelcomePlugin.WelcomeLogger.LogDebug("No custom background found");
                return;
            }
            BlobImage.sprite = TextureManager.CustomBackground;
        }
    }
}