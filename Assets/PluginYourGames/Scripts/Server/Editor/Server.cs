using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace YG.EditorScr
{
    [InitializeOnLoad]
    public static class Server
    {
        public const string LOAD_COMPLETE_KEY = "PluginYG_LoadServerComplete";
        private const string URL_KEY = "PluginYG_URLCloudInfo";
        private const string STANDART_URL = "https://max-games.ru/public/pluginYG2/data.json";
        private const string TEST_URL = "";
        private const int MAX_REDIRECTS = 3;

        private static string testUrl = string.Empty;

        public static bool loadComplete
        {
            get { return SessionState.GetBool(LOAD_COMPLETE_KEY, false); }
        }

        private static int loadCount;

        static Server()
        {
            EditorApplication.delayCall += () =>
            {
                if (PluginPrefs.GetInt(InfoYG.FIRST_STARTUP_KEY) != 0 &&
                    SessionState.GetBool(LOAD_COMPLETE_KEY, false) == false)
                {
                    LoadServerInfo();
                }
            };
        }

        public static async void LoadServerInfo(bool core = false)
        {
            bool hasServerInfoForNotifications = HasServerInfoForNotifications();

            if (!core)
            {
                loadCount = 0;
                SessionState.SetBool(LOAD_COMPLETE_KEY, false);
            }

            try
            {
                loadCount++;
                if (loadCount < 4)
                {
                    string fileContent = null;
                    string fetchFailure = null;
                    async Task<string> Fetch(string url)
                    {
                        try { return await ReadFileFromURL(url); }
                        catch (HttpRequestException ex) { fetchFailure = ex.Message; }
                        catch (TaskCanceledException) { fetchFailure = "Request timed out"; }
                        return null;
                    }
                    bool useTestUrl = !string.IsNullOrWhiteSpace(TEST_URL);

                    if (useTestUrl)
                    {
                        fileContent = await Fetch(TEST_URL);
                    }
                    else
                    {
                        string currentUrl = PluginPrefs.GetString(URL_KEY, STANDART_URL);

                        for (int redirectStep = 0; redirectStep <= MAX_REDIRECTS; redirectStep++)
                        {
                            fileContent = await Fetch(currentUrl);

                            if (string.IsNullOrEmpty(fileContent))
                            {
                                if (currentUrl != STANDART_URL)
                                {
                                    currentUrl = STANDART_URL;
                                    PluginPrefs.SetString(URL_KEY, STANDART_URL);
                                    continue;
                                }

                                break;
                            }

                            if (!TryParseServerJson(fileContent, out ServerJson cloud))
                            {
                                if (currentUrl != STANDART_URL)
                                {
                                    currentUrl = STANDART_URL;
                                    PluginPrefs.SetString(URL_KEY, STANDART_URL);
                                    continue;
                                }

#if RU_YG2
                                Debug.LogError($"Сервер вернул невалидный JSON для данных плагина. URL: {currentUrl}");
#else
                                Debug.LogError($"Server returned invalid JSON for plugin data. URL: {currentUrl}");
#endif
                                fileContent = null;
                                break;
                            }

                            string redirectUrl = string.IsNullOrWhiteSpace(cloud.redirection) ? string.Empty : cloud.redirection.Trim();

                            if (!string.IsNullOrWhiteSpace(redirectUrl) && redirectUrl != currentUrl)
                            {
                                currentUrl = redirectUrl;
                                PluginPrefs.SetString(URL_KEY, redirectUrl);

                                if (redirectStep >= MAX_REDIRECTS)
                                    Debug.LogError($"Too many redirects while loading server info. Last URL: {redirectUrl}");

                                continue;
                            }

                            break;
                        }
                    }

                    if (!string.IsNullOrEmpty(fileContent))
                    {
                        if (!File.Exists(InfoYG.FILE_SERVER_INFO) || File.ReadAllText(InfoYG.FILE_SERVER_INFO) != fileContent)
                            FileYG.WriteAllText(InfoYG.FILE_SERVER_INFO, fileContent);

                        ServerInfo.Read();
                        hasServerInfoForNotifications = HasServerInfoForNotifications();
                    }
                    else
                    {
                        ServerInfo.Read();
                        hasServerInfoForNotifications = HasServerInfoForNotifications();
                        string reason = fetchFailure ?? "No usable catalog returned";
                        if (hasServerInfoForNotifications)
                            Debug.LogWarning($"PluginYG2: using the local module catalog. Online update check failed ({reason}).");
                        else
                            Debug.LogError($"PluginYG2: no local module catalog is available and the online update check failed ({reason}).");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading server info: {ex.Message}");
            }
            finally
            {
                await Task.Delay(100);
                SessionState.SetBool(LOAD_COMPLETE_KEY, true);
                ServerInfo.DoActionLoadServerInfo();

                if (hasServerInfoForNotifications)
                    NotificationUpdateWindow.OpenWindowIfExistUpdate();
            }
        }

        private static bool HasServerInfoForNotifications()
        {
            ServerJson info = ServerInfo.saveInfo;
            return info != null && info.modules != null && info.modules.Length > 0;
        }

        private static async Task<string> ReadFileFromURL(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                using (HttpResponseMessage response = await client.GetAsync(url))
                {
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsStringAsync();
                }
            }
        }
        private static bool TryParseServerJson(string json, out ServerJson cloud)
        {
            cloud = null;

            if (string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                cloud = JsonUtility.FromJson<ServerJson>(json);
                return cloud != null;
            }
            catch
            {
                return false;
            }
        }

        public static void DeletePrefs()
        {
            PluginPrefs.DeleteAll();
            SessionState.SetBool(LOAD_COMPLETE_KEY, false);
        }
    }
}
