using UnityEngine;
#if Storage_yg
using YG;
#endif

#if Storage_yg
namespace YG
{
    // Our blobs inside the PluginYG2 save file (Yandex cloud saves).
    public partial class SavesYG
    {
        public string meta = "";
        public string retention = "";
    }
}
#endif

namespace TowerDefense
{
    // Where persistent progress lives. Every system keeps its state as one JSON blob under a key.
    // With the PluginYG2 Storage module installed (define Storage_yg) the blobs go to
    // YG2.saves and Yandex cloud saves; without it, to PlayerPrefs (IndexedDB in WebGL).
    public static class SaveStorage
    {
        public const string MetaKey = "meta_v1";
        public const string RetentionKey = "retention_v1";

#if Storage_yg
        // Cloud data arrives asynchronously; anything read before that came from defaults
        // and must not be written over the cloud save.
        public static bool IsReady => YG2.isSDKEnabled;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            YG2.onGetSDKData += Reload;
            if (YG2.isSDKEnabled) Reload();
        }

        private static void Reload()
        {
            MetaProgress.Reload();
            RetentionSystem.Reload();
        }

        public static string Get(string key)
        {
            string json = key == MetaKey ? YG2.saves.meta : YG2.saves.retention;
            // Progress made before cloud saves were enabled was kept in PlayerPrefs.
            return string.IsNullOrEmpty(json) ? PlayerPrefs.GetString(key, "") : json;
        }

        public static void Set(string key, string json)
        {
            if (!IsReady) return;
            if (key == MetaKey) YG2.saves.meta = json;
            else YG2.saves.retention = json;
            YG2.SaveProgress();
        }
#else
        public static bool IsReady => true;

        public static string Get(string key) => PlayerPrefs.GetString(key, "");

        public static void Set(string key, string json)
        {
            PlayerPrefs.SetString(key, json);
            PlayerPrefs.Save();
        }
#endif
    }
}
