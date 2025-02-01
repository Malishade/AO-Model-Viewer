using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu]
public class SettingsManager : ScriptableSingleton<SettingsManager>
{
    public static string SaveDirectory = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\AOModelViewer";
    public static string SettingsFileName = "Settings.json";


    public Settings Settings;

    protected override void OnInitialize()
    {
        Load();
    }

    public void Load()
    {
        try
        {
            Settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText($"{SaveDirectory}\\{SettingsFileName}"));
        }
        catch
        {
            Debug.Log($"No settings file found.");
            Debug.Log($"Using default settings");
            Settings = new();
        }
    }

    public void Save()
    {
        if (!Directory.Exists(SaveDirectory))
            Directory.CreateDirectory(SaveDirectory);

        File.WriteAllText($"{SaveDirectory}\\{SettingsFileName}", JsonConvert.SerializeObject(Settings, Formatting.Indented));
    }
}

public class Settings
{
    public string AODirectory = null;
}