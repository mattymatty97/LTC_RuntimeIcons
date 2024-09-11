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
                RequiresRestart = requiresRestart
            }));
        }
        
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void AddConfig(ConfigEntry<bool> entry, bool requiresRestart = false)
        {
            LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(entry, new BoolCheckBoxOptions()
            {
                RequiresRestart = requiresRestart
            }));
        }
        
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void AddConfig(ConfigEntry<float> entry, bool requiresRestart = false)
        {
            LethalConfigManager.AddConfigItem(new FloatInputFieldConfigItem(entry, new FloatInputFieldOptions()
            {
                RequiresRestart = requiresRestart
            }));
        }
        
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void AddConfig(ConfigEntry<int> entry, bool requiresRestart = false)
        {
            LethalConfigManager.AddConfigItem(new IntInputFieldConfigItem(entry, new IntInputFieldOptions()
            {
                RequiresRestart = requiresRestart
            }));
        }
        
    }
}
