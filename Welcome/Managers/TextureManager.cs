using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using UnityEngine;

namespace Welcome.Managers;

public static class TextureManager
{
    private static readonly string FolderPath = Paths.ConfigPath + Path.DirectorySeparatorChar + "Welcome";

    public static Sprite? CustomBackground;

    public static void InitCustomBackground()
    {
        if (WelcomePlugin._PluginEnabled.Value is WelcomePlugin.Toggle.Off) return;
        if (WelcomePlugin._UseCustomBackground.Value is WelcomePlugin.Toggle.Off) return;
        if (WelcomePlugin._CustomBackgroundName.Value.IsNullOrWhiteSpace()) return;

        if (!Directory.Exists(FolderPath)) return;

        string filePath = FolderPath + Path.DirectorySeparatorChar + WelcomePlugin._CustomBackgroundName.Value;
        if (!File.Exists(filePath)) return;

        Sprite? image = RegisterCustomSprite(filePath);
        if (image == null) return;
        CustomBackground = image;
    }

    private static Sprite? RegisterCustomSprite(string fileName)
    {
        if (!File.Exists(fileName)) return null;

        byte[] fileData = File.ReadAllBytes(fileName);
        Texture2D texture = new Texture2D(2, 2);
        if (texture.LoadImage(fileData))
        {
            texture.name = fileName;
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
        }
        return null;
    }
}