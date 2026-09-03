using Celeste.Mod.Helpers;
using CorreWithCare.Core;
using CorreWithCare.Utils;

namespace CorreWithCare;

public class CWCModule : EverestModule
{
    public const string Name = "CorreWithCare";

    public static CWCModule Instance;
    public override Type SettingsType => typeof(CWCSettings);
    public static CWCSettings Settings => (CWCSettings)Instance._Settings;
    public override Type SessionType => typeof(CWCSession);
    public static CWCSession Session => (CWCSession)Instance._Session;
    public override Type SaveDataType => typeof(CWCSaveData);
    public static CWCSaveData SaveData => (CWCSaveData)Instance._SaveData;

    public string ModDirectory
    {
        get => Path.Combine(Path.GetDirectoryName(FakeAssembly.GetFakeEntryAssembly().Location), $"Mods\\{Name}");
    }

    public CWCModule()
    {
        Instance = this;
    }

    public static bool CheckDependency(string modName, string minimumVersion)
    {
        EverestModuleMetadata meta = new()
        {
            Name = modName,
            Version = new Version(minimumVersion)
        };

        return Everest.Loader.DependencyLoaded(meta);
    }

    public static bool CheckDependency(string modName, string minimumVersion,
        out EverestModule module)
    {
        EverestModuleMetadata meta = new()
        {
            Name = modName,
            Version = new Version(minimumVersion)
        };

        return Everest.Loader.TryGetDependency(meta, out module);
    }

    public override void Load()
    {
        Log.Info("Corre is standing by");
        Instance = this;

        LoadingManager.Load();
    }

    public override void Unload()
    {
        LoadingManager.Unload();
    }
}