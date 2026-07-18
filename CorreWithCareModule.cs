using Celeste.Mod.Helpers;
using CorreWithCare.Utils;

namespace CorreWithCare;

public class CorreWithCareModule : EverestModule
{
    public const string Name = "CorreWithCare";

    public static CorreWithCareModule Instance;
    public override Type SettingsType => typeof(CorreWithCareSettings);
    public static CorreWithCareSettings Settings => (CorreWithCareSettings)Instance._Settings;
    public override Type SessionType => typeof(CorreWithCareSession);
    public static CorreWithCareSession Session => (CorreWithCareSession)Instance._Session;
    public override Type SaveDataType => typeof(CorreWithCareSaveData);
    public static CorreWithCareSaveData SaveData => (CorreWithCareSaveData)Instance._SaveData;

    public string ModDirectory
    {
        get => Path.Combine(Path.GetDirectoryName(FakeAssembly.GetFakeEntryAssembly().Location), $"Mods\\{Name}");
    }

    public CorreWithCareModule()
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
        Print.Info("Corre is standing by");
        Instance = this;
    }

    public override void Unload()
    {
    }
}