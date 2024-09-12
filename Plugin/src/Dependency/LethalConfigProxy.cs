using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using BepInEx.Configuration;
using LethalConfig;
using LethalConfig.ConfigItems;
using LethalConfig.ConfigItems.Options;

namespace RuntimeIcons.Dependency
{
    public static class LethalConfigProxy
    {
        private static bool? _enabled;

        public static bool Enabled
        {
            get
            {
                _enabled ??= BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("ainavt.lc.lethalconfig");
                return _enabled.Value;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void AddConfig(ConfigEntry<string> entry, bool requiresRestart = false)
        {
            LethalConfigManager.AddConfigItem(new TextInputFieldConfigItem(entry, new TextInputFieldOptions()
            {
                RequiresRestart = requiresRestart,
                Name = GetPrettyConfigName(entry)
            }));
        }
        
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void AddConfig(ConfigEntry<bool> entry, bool requiresRestart = false)
        {
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(entry, new BoolCheckBoxOptions()
            {
                RequiresRestart = requiresRestart,
                Name = GetPrettyConfigName(entry)
            }));
        }
        
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void AddConfig(ConfigEntry<float> entry, bool requiresRestart = false)
        {
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(entry, new FloatInputFieldOptions()
            {
                RequiresRestart = requiresRestart,
                Name = GetPrettyConfigName(entry)
            }));
        }
        
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void AddConfig(ConfigEntry<int> entry, bool requiresRestart = false)
        {
            LethalConfigManager.AddConfigItem(new IntInputFieldConfigItem(entry, new IntInputFieldOptions()
            {
                RequiresRestart = requiresRestart,
                Name = GetPrettyConfigName(entry)
            }));
        }
        
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void AddButton(string section, string name, string description, string buttonText, Action callback)
        {
            LethalConfigManager.AddConfigItem(new GenericButtonConfigItem(
                section, name, description, buttonText, () =>callback?.Invoke()));
        }
		
		
        private static string GetPrettyConfigName<T>(ConfigEntry<T> entry)
        {
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(entry.Definition.Key.Replace("_", " "));
        }
        
    }
}