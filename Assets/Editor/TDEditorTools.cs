using System.IO;
using TowerDefense;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace TowerDefense.EditorTools
{
    // One-click content generation + scene wiring so nobody has to hand-drag
    // 20 references in the Inspector. Run the three menu items in order,
    // from the game_scene tab, then press Play.
    public static class TDEditorTools
    {
        private const string GeneratedRoot = "Assets/Generated";
        private const string PrefabsFolder = GeneratedRoot + "/Prefabs";
        private const string DataFolder = GeneratedRoot + "/Data";

        private const string EnemySpriteDir = "Assets/Skins_enemy/Foozle_2DC0028_Spire_EnemyPack_2_Ground/Ground/Spritesheets/";
        private const string TowerBaseDir = "Assets/Skins_tower/Foozle_2DS0018_Spire_TowerPack_2/Towers bases/PNGs/";
        private const string Tower03WeaponDir = "Assets/Skins_tower/Foozle_2DS0018_Spire_TowerPack_2/Towers Weapons/Tower 03/Spritesheets/";
        private const string Tower04WeaponDir = "Assets/Skins_tower/Foozle_2DS0018_Spire_TowerPack_2/Towers Weapons/Tower 04/PNGs/";

        // ------------------------------------------------------------
        // 1. Generate ScriptableObject data + prefabs from the art packs
        // Balance is always rewritten; sprites only when found, and existing prefabs are
        // kept: the designer animates them by hand now (see Assets/Textures/Sprites).
        // ------------------------------------------------------------
        [MenuItem("Tower Defense/1. Generate Game Content", false, 10)]
        public static void GenerateContent()
        {
            EnsureFolder(GeneratedRoot);
            EnsureFolder(PrefabsFolder);
            EnsureFolder(DataFolder);

            EnemyData leafbug = CreateEnemyData("Leafbug", 15f, 3.2f, 3, EnemySpriteDir + "Leafbug.png");
            EnemyData firebug = CreateEnemyData("Firebug", 25f, 2.4f, 5, EnemySpriteDir + "Firebug.png");
            EnemyData scorpion = CreateEnemyData("Scorpion", 45f, 1.8f, 8, EnemySpriteDir + "Scorpion.png");
            EnemyData magmaCrab = CreateEnemyData("Magma Crab", 80f, 1.2f, 12, EnemySpriteDir + "Magma Crab.png");

            CreateEnemyPrefab();

            GameObject arrowProjectile = CreateProjectilePrefab("ArrowProjectile", Tower03WeaponDir + "Tower 03 - Level 01 - Projectile.png");
            GameObject cannonProjectile = CreateProjectilePrefab("CannonProjectile", Tower04WeaponDir + "Tower 04 - Level 01 - Projectile.png");

            CreateTowerData("Arrow Tower", 50, 6f, 3.5f, 1.5f, 6f,
                TowerBaseDir + "Tower 03.png", Tower03WeaponDir + "Tower 03 - Level 01 - Weapon.png", arrowProjectile);
            CreateTowerData("Cannon Tower", 90, 14f, 3f, 0.7f, 5f,
                TowerBaseDir + "Tower 04.png", Tower04WeaponDir + "Tower 04 - Level 01 - Weapon.png", cannonProjectile);

            CreateTowerPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Tower Defense] Content generated under " + GeneratedRoot);
        }

        // ------------------------------------------------------------
        // Role enemies for the campaign (roadmap item 5). Runtime systems load them
        // from Resources/Enemies; art reuses the base pack sprites.
        // ------------------------------------------------------------
        [MenuItem("Tower Defense/1.5 Generate Role Enemies", false, 11)]
        public static void GenerateRoleEnemies()
        {
            const string roleFolder = "Assets/Resources/Enemies";
            EnsureFolder("Assets/Resources");
            EnsureFolder(roleFolder);

            EnemyData leafbug = AssetDatabase.LoadAssetAtPath<EnemyData>($"{DataFolder}/Enemy_Leafbug.asset");
            EnemyData scorpion = AssetDatabase.LoadAssetAtPath<EnemyData>($"{DataFolder}/Enemy_Scorpion.asset");
            EnemyData magmaCrab = AssetDatabase.LoadAssetAtPath<EnemyData>($"{DataFolder}/Enemy_MagmaCrab.asset");

            EnemyData armored = CreateRoleEnemy(roleFolder, "Enemy_ArmoredBug", "Бронежук", 60f, 1.6f, 9, leafbug);
            armored.arrowDamageMultiplier = 0.35f;

            EnemyData swift = CreateRoleEnemy(roleFolder, "Enemy_Swift", "Стремительный жук", 14f, 4.6f, 4, leafbug);
            swift.damageToBase = 1;

            EnemyData healer = CreateRoleEnemy(roleFolder, "Enemy_Healer", "Жук-целитель", 40f, 1.6f, 10, scorpion);
            healer.healPerSecond = 4f;
            healer.healRadius = 2.2f;

            EnemyData splitter = CreateRoleEnemy(roleFolder, "Enemy_Splitter", "Делящийся жук", 35f, 2.2f, 6, scorpion);
            splitter.splitCount = 3;
            splitter.splitInto = leafbug;

            EnemyData cleanse = CreateRoleEnemy(roleFolder, "Enemy_Cleanse", "Магический жук", 45f, 1.9f, 10, magmaCrab);
            cleanse.cleanseInterval = 4f;

            EnemyData flyer = CreateRoleEnemy(roleFolder, "Enemy_Flyer", "Летающий жук", 25f, 3.4f, 6, leafbug);
            flyer.isFlying = true;

            EnemyData boss = CreateRoleEnemy(roleFolder, "Enemy_Boss", "Королева жуков", 900f, 1.1f, 150, magmaCrab);
            boss.isBoss = true;
            boss.damageToBase = 5;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Tower Defense] Role enemies generated under " + roleFolder);
        }

        private static EnemyData CreateRoleEnemy(string folder, string fileName, string displayName,
            float hp, float speed, int reward, EnemyData spriteSource)
        {
            string assetPath = $"{folder}/{fileName}.asset";
            EnemyData data = AssetDatabase.LoadAssetAtPath<EnemyData>(assetPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<EnemyData>();
                AssetDatabase.CreateAsset(data, assetPath);
            }
            data.displayName = displayName;
            data.maxHealth = hp;
            data.moveSpeed = speed;
            data.currencyReward = reward;
            data.damageToBase = 1;
            if (spriteSource != null)
            {
                data.sprite = spriteSource.sprite;
                data.animatorController = spriteSource.animatorController;
            }
            EditorUtility.SetDirty(data);
            return data;
        }

        private static EnemyData CreateEnemyData(string name, float hp, float speed, int reward, string spritePath)
        {
            string assetPath = $"{DataFolder}/Enemy_{name.Replace(" ", "")}.asset";
            EnemyData data = AssetDatabase.LoadAssetAtPath<EnemyData>(assetPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<EnemyData>();
                AssetDatabase.CreateAsset(data, assetPath);
            }
            data.displayName = name;
            data.maxHealth = hp;
            data.moveSpeed = speed;
            data.currencyReward = reward;
            data.damageToBase = 1;
            data.sprite = LoadSprite(spritePath) ?? data.sprite;
            EditorUtility.SetDirty(data);
            return data;
        }

        private static TowerData CreateTowerData(string name, int cost, float damage, float range, float fireRate,
            float projectileSpeed, string baseSpritePath, string weaponSpritePath, GameObject projectilePrefab)
        {
            string assetPath = $"{DataFolder}/Tower_{name.Replace(" ", "")}.asset";
            TowerData data = AssetDatabase.LoadAssetAtPath<TowerData>(assetPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<TowerData>();
                AssetDatabase.CreateAsset(data, assetPath);
            }
            data.displayName = name;
            data.cost = cost;
            data.damage = damage;
            data.range = range;
            data.fireRate = fireRate;
            data.projectileSpeed = projectileSpeed;
            data.baseSprite = LoadSprite(baseSpritePath) ?? data.baseSprite;
            data.weaponSprite = LoadSprite(weaponSpritePath) ?? data.weaponSprite;
            data.projectilePrefab = projectilePrefab;
            EditorUtility.SetDirty(data);
            return data;
        }

        private static GameObject CreateEnemyPrefab()
        {
            string path = $"{PrefabsFolder}/EnemyPrefab.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            GameObject root = new GameObject("Enemy");
            root.AddComponent<SpriteRenderer>();

            GameObject barBg = new GameObject("HealthBarBG");
            barBg.transform.SetParent(root.transform);
            barBg.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            SpriteRenderer bgSr = barBg.AddComponent<SpriteRenderer>();
            bgSr.sprite = GetOrCreateWhiteSprite();
            bgSr.color = Color.black;
            bgSr.sortingOrder = 10;
            barBg.transform.localScale = new Vector3(0.6f, 0.1f, 1f);

            GameObject barFill = new GameObject("HealthBarFill");
            barFill.transform.SetParent(root.transform);
            barFill.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            SpriteRenderer fillSr = barFill.AddComponent<SpriteRenderer>();
            fillSr.sprite = GetOrCreateWhiteSprite();
            fillSr.color = Color.green;
            fillSr.sortingOrder = 11;
            barFill.transform.localScale = new Vector3(0.58f, 0.08f, 1f);

            Enemy enemyComp = root.AddComponent<Enemy>();
            SerializedObject so = new SerializedObject(enemyComp);
            so.FindProperty("healthBarFill").objectReferenceValue = barFill.transform;
            so.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateProjectilePrefab(string name, string spritePath)
        {
            string path = $"{PrefabsFolder}/{name}.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            GameObject root = new GameObject(name);
            SpriteRenderer sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite(spritePath);
            sr.sortingOrder = 5;
            root.AddComponent<Projectile>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateTowerPrefab()
        {
            string path = $"{PrefabsFolder}/TowerPrefab.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            GameObject root = new GameObject("Tower");

            GameObject baseGO = new GameObject("Base");
            baseGO.transform.SetParent(root.transform);
            baseGO.transform.localPosition = Vector3.zero;
            SpriteRenderer baseSr = baseGO.AddComponent<SpriteRenderer>();
            baseSr.sortingOrder = 1;

            GameObject pivot = new GameObject("WeaponPivot");
            pivot.transform.SetParent(root.transform);
            pivot.transform.localPosition = Vector3.zero;

            GameObject weaponGO = new GameObject("Weapon");
            weaponGO.transform.SetParent(pivot.transform);
            weaponGO.transform.localPosition = Vector3.zero;
            SpriteRenderer weaponSr = weaponGO.AddComponent<SpriteRenderer>();
            weaponSr.sortingOrder = 2;

            Tower towerComp = root.AddComponent<Tower>();
            SerializedObject so = new SerializedObject(towerComp);
            so.FindProperty("baseRenderer").objectReferenceValue = baseSr;
            so.FindProperty("weaponRenderer").objectReferenceValue = weaponSr;
            so.FindProperty("weaponPivot").objectReferenceValue = pivot.transform;
            so.ApplyModifiedProperties();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // ------------------------------------------------------------
        // 2. Build HUD canvas + GameManager/WaveManager in the open scene
        // ------------------------------------------------------------
        [MenuItem("Tower Defense/2. Build Scene Setup (HUD + Managers)", false, 20)]
        public static void BuildSceneSetup()
        {
            GameObject waypointsGO = GameObject.Find("Waypoints");
            if (waypointsGO == null)
            {
                Debug.LogError("[Tower Defense] No 'Waypoints' GameObject found. Open game_scene first.");
                return;
            }
            PathHolder pathHolder = waypointsGO.GetComponent<PathHolder>();
            if (pathHolder == null) pathHolder = Undo.AddComponent<PathHolder>(waypointsGO);

            GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsFolder}/EnemyPrefab.prefab");
            EnemyData leafbug = AssetDatabase.LoadAssetAtPath<EnemyData>($"{DataFolder}/Enemy_Leafbug.asset");
            EnemyData firebug = AssetDatabase.LoadAssetAtPath<EnemyData>($"{DataFolder}/Enemy_Firebug.asset");
            EnemyData scorpion = AssetDatabase.LoadAssetAtPath<EnemyData>($"{DataFolder}/Enemy_Scorpion.asset");
            EnemyData magmaCrab = AssetDatabase.LoadAssetAtPath<EnemyData>($"{DataFolder}/Enemy_MagmaCrab.asset");

            if (enemyPrefab == null || leafbug == null)
            {
                Debug.LogError("[Tower Defense] Generated content not found. Run 'Tower Defense/1. Generate Game Content' first.");
                return;
            }

            GameObject gmGO = GameObject.Find("GameManager") ?? new GameObject("GameManager");
            GameManager gameManager = gmGO.GetComponent<GameManager>() ?? gmGO.AddComponent<GameManager>();

            GameObject wmGO = GameObject.Find("WaveManager") ?? new GameObject("WaveManager");
            WaveManager waveManager = wmGO.GetComponent<WaveManager>() ?? wmGO.AddComponent<WaveManager>();

            SerializedObject wmSO = new SerializedObject(waveManager);
            wmSO.FindProperty("path").objectReferenceValue = pathHolder;
            wmSO.FindProperty("enemyPrefab").objectReferenceValue = enemyPrefab;

            SerializedProperty wavesProp = wmSO.FindProperty("waves");
            wavesProp.ClearArray();
            AddWave(wavesProp, 0, 3f, (leafbug, 6, 0.6f));
            AddWave(wavesProp, 1, 6f, (leafbug, 8, 0.5f), (firebug, 4, 0.7f));
            AddWave(wavesProp, 2, 6f, (firebug, 6, 0.6f), (scorpion, 4, 1f));
            AddWave(wavesProp, 3, 6f, (scorpion, 6, 0.8f), (magmaCrab, 3, 1.2f));
            AddWave(wavesProp, 4, 6f, (leafbug, 10, 0.3f), (firebug, 6, 0.5f), (scorpion, 4, 0.7f), (magmaCrab, 4, 1f));
            wmSO.ApplyModifiedProperties();

            // Canvas
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            GameObject canvasGO;
            if (canvas == null)
            {
                canvasGO = new GameObject("HUDCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasGO.GetComponent<Canvas>();
            }
            else
            {
                canvasGO = canvas.gameObject;
            }
            ForceOverlayCanvas(canvas);

            // This project's Active Input Handling is "Input System Package (New)" only,
            // so the EventSystem needs InputSystemUIInputModule, not the legacy StandaloneInputModule.
            if (Object.FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            Text currencyText = CreateText(canvasGO.transform, "CurrencyText", "Gold: 0", new Vector2(0f, 1f), new Vector2(20, -20), TextAnchor.UpperLeft);
            Text livesText = CreateText(canvasGO.transform, "LivesText", "Lives: 0", new Vector2(0f, 1f), new Vector2(20, -55), TextAnchor.UpperLeft);
            Text waveText = CreateText(canvasGO.transform, "WaveText", "Wave: 0/0", new Vector2(1f, 1f), new Vector2(-20, -20), TextAnchor.UpperRight);

            Button startWaveButton = CreateButton(canvasGO.transform, "StartWaveButton", "Start Wave", new Vector2(1f, 0f), new Vector2(-120, 60), new Vector2(180, 60));

            GameObject gameOverPanel = CreatePanel(canvasGO.transform, "GameOverPanel", new Vector2(500, 300));
            Text gameOverText = CreateText(gameOverPanel.transform, "ResultText", "Victory!", new Vector2(0.5f, 0.5f), new Vector2(0, 70), TextAnchor.MiddleCenter);
            Button restartButton = CreateButton(gameOverPanel.transform, "RestartButton", "Restart", new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(180, 50));
            Button mainMenuButton = CreateButton(gameOverPanel.transform, "MainMenuButton", "Main Menu", new Vector2(0.5f, 0.5f), new Vector2(0, -70), new Vector2(180, 50));
            gameOverPanel.SetActive(false);

            // Build menu popup (positioned at runtime next to the clicked slot)
            GameObject buildMenuPanel = CreatePanel(canvasGO.transform, "BuildMenuPanel", new Vector2(260, 90));
            RectTransform buildMenuRect = buildMenuPanel.GetComponent<RectTransform>();
            HorizontalLayoutGroup layout = buildMenuPanel.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 8;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            TowerData arrowTowerData = AssetDatabase.LoadAssetAtPath<TowerData>($"{DataFolder}/Tower_ArrowTower.asset");
            TowerData cannonTowerData = AssetDatabase.LoadAssetAtPath<TowerData>($"{DataFolder}/Tower_CannonTower.asset");
            GameObject towerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsFolder}/TowerPrefab.prefab");

            Button arrowBtn = CreateButton(buildMenuPanel.transform, "BuildArrowButton", "Arrow\n50g", Vector2.zero, Vector2.zero, new Vector2(110, 74));
            Button cannonBtn = CreateButton(buildMenuPanel.transform, "BuildCannonButton", "Cannon\n90g", Vector2.zero, Vector2.zero, new Vector2(110, 74));

            SetupBuildOption(arrowBtn, arrowTowerData, towerPrefab);
            SetupBuildOption(cannonBtn, cannonTowerData, towerPrefab);

            buildMenuPanel.SetActive(false);

            GameObject buildMenuManagerGO = GameObject.Find("BuildMenuUI") ?? new GameObject("BuildMenuUI");
            BuildMenuUI buildMenuUI = buildMenuManagerGO.GetComponent<BuildMenuUI>() ?? buildMenuManagerGO.AddComponent<BuildMenuUI>();
            SerializedObject bmSO = new SerializedObject(buildMenuUI);
            bmSO.FindProperty("panel").objectReferenceValue = buildMenuRect;
            bmSO.ApplyModifiedProperties();

            GameObject hudGO = GameObject.Find("HUDController") ?? new GameObject("HUDController");
            HUDController hud = hudGO.GetComponent<HUDController>() ?? hudGO.AddComponent<HUDController>();

            SerializedObject hudSO = new SerializedObject(hud);
            hudSO.FindProperty("gameManager").objectReferenceValue = gameManager;
            hudSO.FindProperty("waveManager").objectReferenceValue = waveManager;
            hudSO.FindProperty("currencyText").objectReferenceValue = currencyText;
            hudSO.FindProperty("livesText").objectReferenceValue = livesText;
            hudSO.FindProperty("waveText").objectReferenceValue = waveText;
            hudSO.FindProperty("startWaveButton").objectReferenceValue = startWaveButton;
            hudSO.FindProperty("gameOverPanel").objectReferenceValue = gameOverPanel;
            hudSO.FindProperty("gameOverText").objectReferenceValue = gameOverText;
            hudSO.ApplyModifiedProperties();

            UnityEventTools.AddPersistentListener(restartButton.onClick, hud.OnRestartClicked);
            UnityEventTools.AddPersistentListener(mainMenuButton.onClick, hud.OnMainMenuClicked);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[Tower Defense] HUD and managers built. Save the scene (Ctrl+S).");
        }

        // Forces any Canvas we reuse into Screen Space - Overlay with sane scaling.
        // A pre-existing Canvas left in World Space (or pointed at a missing
        // camera) renders nothing visible and is easy to miss by eye.
        private static void ForceOverlayCanvas(Canvas canvas)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            if (canvas.GetComponent<GraphicRaycaster>() == null)
                canvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        private static void SetupBuildOption(Button button, TowerData data, GameObject prefab)
        {
            BuildOptionButton option = button.gameObject.AddComponent<BuildOptionButton>();
            SerializedObject so = new SerializedObject(option);
            so.FindProperty("towerData").objectReferenceValue = data;
            so.FindProperty("towerPrefab").objectReferenceValue = prefab;
            so.FindProperty("label").objectReferenceValue = button.GetComponentInChildren<Text>();
            so.ApplyModifiedProperties();
        }

        private static void AddWave(SerializedProperty wavesProp, int index, float delay,
            params (EnemyData enemy, int count, float interval)[] entries)
        {
            wavesProp.InsertArrayElementAtIndex(index);
            SerializedProperty waveElement = wavesProp.GetArrayElementAtIndex(index);
            waveElement.FindPropertyRelative("name").stringValue = $"Wave {index + 1}";
            waveElement.FindPropertyRelative("delayBeforeWave").floatValue = delay;

            SerializedProperty entriesProp = waveElement.FindPropertyRelative("entries");
            entriesProp.ClearArray();
            for (int i = 0; i < entries.Length; i++)
            {
                entriesProp.InsertArrayElementAtIndex(i);
                SerializedProperty entryProp = entriesProp.GetArrayElementAtIndex(i);
                entryProp.FindPropertyRelative("enemy").objectReferenceValue = entries[i].enemy;
                entryProp.FindPropertyRelative("count").intValue = entries[i].count;
                entryProp.FindPropertyRelative("interval").floatValue = entries[i].interval;
            }
        }

        // ------------------------------------------------------------
        // 4. Register MainMenu + game_scene in Build Settings
        // ------------------------------------------------------------
        [MenuItem("Tower Defense/4. Add Scenes To Build Settings", false, 40)]
        public static void AddScenesToBuildSettings()
        {
            const string mainMenuPath = "Assets/Scenes/MainMenu.unity";
            const string gamePath = "Assets/Scenes/game_scene.unity";

            var existing = EditorBuildSettings.scenes;
            bool hasMainMenu = false;
            bool hasGameScene = false;
            foreach (var s in existing)
            {
                if (s.path == mainMenuPath) hasMainMenu = true;
                if (s.path == gamePath) hasGameScene = true;
            }

            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>();
            if (!hasMainMenu) list.Add(new EditorBuildSettingsScene(mainMenuPath, true));
            if (!hasGameScene) list.Add(new EditorBuildSettingsScene(gamePath, true));
            list.AddRange(existing);

            EditorBuildSettings.scenes = list.ToArray();
            Debug.Log("[Tower Defense] Build Settings scenes: " + string.Join(", ", System.Array.ConvertAll(EditorBuildSettings.scenes, s => s.path)));
        }

        // ------------------------------------------------------------
        // 5. Build the Main Menu UI (title + Play/Quit) - run with MainMenu.unity open
        // ------------------------------------------------------------
        [MenuItem("Tower Defense/5. Build Main Menu UI", false, 50)]
        public static void BuildMainMenuUI()
        {
            MainMenuController controller = Object.FindFirstObjectByType<MainMenuController>(FindObjectsInactive.Include);
            if (controller == null)
            {
                string activeScene = EditorSceneManager.GetActiveScene().name;
                Debug.LogError($"[Tower Defense] No MainMenuController found in the active scene ('{activeScene}'). " +
                    "Double-click Assets/Scenes/MainMenu.unity to open it as the active scene, then run this again.");
                return;
            }

            Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                Debug.LogError("[Tower Defense] No Canvas found in the open scene.");
                return;
            }
            ForceOverlayCanvas(canvas);

            if (Object.FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            if (GameObject.Find("TitleText") == null)
            {
                Text title = CreateText(canvas.transform, "TitleText", "Tower Defense", new Vector2(0.5f, 0.75f), Vector2.zero, TextAnchor.MiddleCenter);
                RectTransform titleRt = title.GetComponent<RectTransform>();
                titleRt.sizeDelta = new Vector2(700, 100);
                title.fontSize = 56;
                title.fontStyle = FontStyle.Bold;
            }

            if (GameObject.Find("PlayButton") == null)
            {
                Button playButton = CreateButton(canvas.transform, "PlayButton", "Play", new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(220, 70));
                UnityEventTools.AddPersistentListener(playButton.onClick, controller.OnPlayClicked);
            }

            if (GameObject.Find("QuitButton") == null)
            {
                Button quitButton = CreateButton(canvas.transform, "QuitButton", "Quit", new Vector2(0.5f, 0.5f), new Vector2(0, -70), new Vector2(220, 70));
                UnityEventTools.AddPersistentListener(quitButton.onClick, controller.OnQuitClicked);
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[Tower Defense] Main menu UI built. Save the scene (Ctrl+S).");
        }

        // Only place a slot every Nth path segment, alternating sides, instead
        // of both sides of every single segment - keeps the count sane on a
        // path with a lot of waypoints.
        private const int SlotSegmentStep = 2;
        private const float SlotOffsetDistance = 1.5f;

        // ------------------------------------------------------------
        // Clear existing tower slots (run before re-generating)
        // ------------------------------------------------------------
        [MenuItem("Tower Defense/3a. Clear Tower Slots", false, 29)]
        public static void ClearTowerSlots()
        {
            GameObject slotsRoot = GameObject.Find("TowerSlots");
            if (slotsRoot == null)
            {
                Debug.Log("[Tower Defense] No TowerSlots object found, nothing to clear.");
                return;
            }

            int count = slotsRoot.transform.childCount;
            for (int i = count - 1; i >= 0; i--)
                Object.DestroyImmediate(slotsRoot.transform.GetChild(i).gameObject);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"[Tower Defense] Cleared {count} tower slots.");
        }

        // ------------------------------------------------------------
        // 3. Auto-place fixed tower build slots along the path
        // ------------------------------------------------------------
        [MenuItem("Tower Defense/3. Auto-Place Tower Slots", false, 30)]
        public static void AutoPlaceTowerSlots()
        {
            GameObject waypointsGO = GameObject.Find("Waypoints");
            if (waypointsGO == null)
            {
                Debug.LogError("[Tower Defense] No 'Waypoints' GameObject found in the open scene.");
                return;
            }

            int childCount = waypointsGO.transform.childCount;
            if (childCount < 2)
            {
                Debug.LogError("[Tower Defense] Waypoints needs at least 2 children to compute a path direction.");
                return;
            }

            GameObject slotsRoot = GameObject.Find("TowerSlots") ?? new GameObject("TowerSlots");
            Sprite ringSprite = GetOrCreateRingSprite();
            int startingCount = slotsRoot.transform.childCount;
            int slotIndex = startingCount;
            bool sideFlip = false;

            for (int i = 0; i < childCount - 1; i += SlotSegmentStep)
            {
                Vector3 a = waypointsGO.transform.GetChild(i).position;
                Vector3 b = waypointsGO.transform.GetChild(i + 1).position;
                Vector3 mid = (a + b) * 0.5f;
                Vector3 dir = (b - a).normalized;
                Vector3 normal = new Vector3(-dir.y, dir.x, 0f);
                Vector3 offset = (sideFlip ? -normal : normal) * SlotOffsetDistance;

                CreateSlot(slotsRoot.transform, ref slotIndex, mid + offset, ringSprite);
                sideFlip = !sideFlip;
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            int created = slotIndex - startingCount;
            Debug.Log($"[Tower Defense] Created {created} new tower slots ({slotIndex} total under TowerSlots). Run '3a. Clear Tower Slots' first if you want to start over. Reposition any that overlap the path, then save the scene.");
        }

        private static void CreateSlot(Transform parent, ref int index, Vector3 position, Sprite ringSprite)
        {
            GameObject slotGO = new GameObject($"Slot_{index}");
            slotGO.transform.SetParent(parent);
            slotGO.transform.position = position;

            SpriteRenderer sr = slotGO.AddComponent<SpriteRenderer>();
            sr.sprite = ringSprite;
            sr.color = new Color(1f, 1f, 1f, 0.6f);
            sr.sortingOrder = 1;

            CircleCollider2D col = slotGO.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;

            TowerSlot slot = slotGO.AddComponent<TowerSlot>();
            SerializedObject so = new SerializedObject(slot);
            so.FindProperty("indicator").objectReferenceValue = sr;
            so.ApplyModifiedProperties();

            index++;
        }

        // ------------------------------------------------------------
        // 6. Create the poison tower content and add it to the game scene
        // ------------------------------------------------------------
        private const string PoisonTowerArtDir = "Assets/Textures/Sprites/Skins_tower/poison_tower/";

        [MenuItem("Tower Defense/6. Integrate Poison Tower", false, 60)]
        public static void IntegratePoisonTower()
        {
            EnsureFolder(PrefabsFolder);
            EnsureFolder(DataFolder);

            Sprite[] levelSprites = new Sprite[8];
            for (int i = 0; i < levelSprites.Length; i++)
                levelSprites[i] = LoadSprite($"{PoisonTowerArtDir}PoisonTower_Level{i + 1}.png");

            GameObject impactPrefab = CreatePoisonImpactPrefab();
            GameObject projectilePrefab = CreatePoisonProjectilePrefab();

            string dataPath = $"{DataFolder}/Tower_PoisonTower.asset";
            TowerData data = AssetDatabase.LoadAssetAtPath<TowerData>(dataPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<TowerData>();
                AssetDatabase.CreateAsset(data, dataPath);
            }

            data.displayName = "Poison Tower";
            data.cost = 70;
            data.damage = 3f;
            data.range = 3.3f;
            data.fireRate = 0.9f;
            data.projectileSpeed = 5f;
            data.damageOverTimePerSecond = 5f;
            data.damageOverTimeDuration = 3f;
            data.preferUnpoisonedTarget = true;
            data.baseSprite = levelSprites[0];
            data.levelSprites = levelSprites;
            data.projectilePrefab = projectilePrefab;
            data.impactPrefab = impactPrefab;
            EditorUtility.SetDirty(data);

            AddPoisonTowerToBuildMenu(data);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Tower Defense] Poison Tower integrated: data, projectile, impact effect, and build menu option.");
        }

        private static GameObject CreatePoisonProjectilePrefab()
        {
            string path = $"{PrefabsFolder}/PoisonProjectile.prefab";
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root == null)
            {
                root = new GameObject("PoisonProjectile");
                root.AddComponent<SpriteRenderer>();
                root.AddComponent<Projectile>();
            }

            SpriteRenderer sr = root.GetComponent<SpriteRenderer>();
            sr.sprite = LoadSprite(PoisonTowerArtDir + "PoisonTower_Projectile.png");
            sr.sortingOrder = 5;
            root.transform.localScale = new Vector3(3f, 3f, 3f);
            if (AssetDatabase.Contains(root))
            {
                PrefabUtility.SavePrefabAsset(root);
            }
            else
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Object.DestroyImmediate(root);
                root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            return root;
        }

        private static GameObject CreatePoisonImpactPrefab()
        {
            string path = $"{PrefabsFolder}/PoisonImpact.prefab";
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root == null)
            {
                root = new GameObject("PoisonImpact");
                root.AddComponent<SpriteRenderer>();
                root.AddComponent<ImpactEffect>();
            }

            SpriteRenderer sr = root.GetComponent<SpriteRenderer>();
            sr.sprite = LoadSprite(PoisonTowerArtDir + "PoisonTower_Impact.png");
            sr.sortingOrder = 6;
            root.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);

            ImpactEffect effect = root.GetComponent<ImpactEffect>();
            SerializedObject so = new SerializedObject(effect);
            so.FindProperty("lifetime").floatValue = 0.45f;
            so.FindProperty("scaleGrowth").floatValue = 0.5f;
            so.ApplyModifiedProperties();
            if (AssetDatabase.Contains(root))
            {
                PrefabUtility.SavePrefabAsset(root);
            }
            else
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Object.DestroyImmediate(root);
                root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            return root;
        }

        private static void AddPoisonTowerToBuildMenu(TowerData data)
        {
            const string scenePath = "Assets/Scenes/game_scene.unity";
            if (EditorSceneManager.GetActiveScene().path != scenePath)
                EditorSceneManager.OpenScene(scenePath);

            GameObject panel = null;
            foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go.name == "BuildMenuPanel" && go.scene.IsValid())
                {
                    panel = go;
                    break;
                }
            }
            if (panel == null)
            {
                Debug.LogError("[Tower Defense] BuildMenuPanel not found. Run 'Tower Defense/2. Build Scene Setup' in game_scene first.");
                return;
            }

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(830f, 90f);

            GameObject towerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsFolder}/TowerPrefab.prefab");
            if (towerPrefab == null)
            {
                Debug.LogError("[Tower Defense] TowerPrefab not found.");
                return;
            }

            Transform existing = panel.transform.Find("BuildPoisonButton");
            Button button = existing != null ? existing.GetComponent<Button>() : null;
            if (button == null)
                button = CreateButton(panel.transform, "BuildPoisonButton", "Poison\n70g", Vector2.zero, Vector2.zero, new Vector2(110f, 74f));
            SetupBuildOption(button, data, towerPrefab);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        // ------------------------------------------------------------
        // 7. Create the ballista content and add it to the game scene
        // ------------------------------------------------------------
        private const string BallistaArtDir = "Assets/Textures/Sprites/Skins_tower/ballista/";

        [MenuItem("Tower Defense/7. Integrate Ballista", false, 61)]
        public static void IntegrateBallista()
        {
            EnsureFolder(PrefabsFolder);
            EnsureFolder(DataFolder);

            Sprite[] levelSprites = new Sprite[8];
            for (int i = 0; i < levelSprites.Length; i++)
                levelSprites[i] = LoadSprite($"{BallistaArtDir}Ballista_Level{i + 1}.png");

            GameObject impactPrefab = CreateBallistaImpactPrefab();
            GameObject projectilePrefab = CreateBallistaProjectilePrefab();

            string dataPath = $"{DataFolder}/Tower_Ballista.asset";
            TowerData data = AssetDatabase.LoadAssetAtPath<TowerData>(dataPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<TowerData>();
                AssetDatabase.CreateAsset(data, dataPath);
            }

            data.displayName = "Ballista";
            data.cost = 120;
            data.damage = 26f;
            data.range = 4f;
            data.fireRate = 0.45f;
            data.projectileSpeed = 7f;
            data.splashRadius = 0f;
            data.pierceCount = 2;
            data.baseSprite = levelSprites[0];
            data.levelSprites = levelSprites;
            data.projectilePrefab = projectilePrefab;
            data.impactPrefab = impactPrefab;
            EditorUtility.SetDirty(data);

            AddBallistaToBuildMenu(data);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Tower Defense] Ballista integrated: data, piercing bolt, impact effect, and build menu option.");
        }

        private static GameObject CreateBallistaProjectilePrefab()
        {
            string path = $"{PrefabsFolder}/BallistaProjectile.prefab";
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root == null)
            {
                root = new GameObject("BallistaProjectile");
                root.AddComponent<SpriteRenderer>();
                root.AddComponent<Projectile>();
            }

            SpriteRenderer sr = root.GetComponent<SpriteRenderer>();
            sr.sprite = LoadSprite(BallistaArtDir + "Ballista_Bolt.png");
            sr.sortingOrder = 5;
            root.transform.localScale = new Vector3(2f, 2f, 2f);
            if (AssetDatabase.Contains(root))
            {
                PrefabUtility.SavePrefabAsset(root);
            }
            else
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Object.DestroyImmediate(root);
                root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            return root;
        }

        private static GameObject CreateBallistaImpactPrefab()
        {
            string path = $"{PrefabsFolder}/BallistaImpact.prefab";
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root == null)
            {
                root = new GameObject("BallistaImpact");
                root.AddComponent<SpriteRenderer>();
                root.AddComponent<ImpactEffect>();
            }

            SpriteRenderer sr = root.GetComponent<SpriteRenderer>();
            sr.sprite = LoadSprite(BallistaArtDir + "Ballista_Impact.png");
            sr.sortingOrder = 6;
            root.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);

            ImpactEffect effect = root.GetComponent<ImpactEffect>();
            SerializedObject so = new SerializedObject(effect);
            so.FindProperty("lifetime").floatValue = 0.4f;
            so.FindProperty("scaleGrowth").floatValue = 0.35f;
            so.ApplyModifiedProperties();
            if (AssetDatabase.Contains(root))
            {
                PrefabUtility.SavePrefabAsset(root);
            }
            else
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Object.DestroyImmediate(root);
                root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            return root;
        }

        private static void AddBallistaToBuildMenu(TowerData data)
        {
            const string scenePath = "Assets/Scenes/game_scene.unity";
            if (EditorSceneManager.GetActiveScene().path != scenePath)
                EditorSceneManager.OpenScene(scenePath);

            GameObject panel = null;
            foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go.name == "BuildMenuPanel" && go.scene.IsValid())
                {
                    panel = go;
                    break;
                }
            }
            if (panel == null)
            {
                Debug.LogError("[Tower Defense] BuildMenuPanel not found. Run 'Tower Defense/2. Build Scene Setup' in game_scene first.");
                return;
            }

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(830f, 90f);

            GameObject towerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsFolder}/TowerPrefab.prefab");
            if (towerPrefab == null)
            {
                Debug.LogError("[Tower Defense] TowerPrefab not found.");
                return;
            }

            Transform existing = panel.transform.Find("BuildBallistaButton");
            Button button = existing != null ? existing.GetComponent<Button>() : null;
            if (button == null)
                button = CreateButton(panel.transform, "BuildBallistaButton", "Ballista\n120g", Vector2.zero, Vector2.zero, new Vector2(110f, 74f));
            SetupBuildOption(button, data, towerPrefab);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        // ------------------------------------------------------------
        // 8. Create the lightning tower content and add it to the game scene
        // ------------------------------------------------------------
        private const string LightningArtDir = "Assets/Textures/Sprites/Skins_tower/lightning_tower/";

        [MenuItem("Tower Defense/8. Integrate Lightning Tower", false, 62)]
        public static void IntegrateLightningTower()
        {
            EnsureFolder(PrefabsFolder);
            EnsureFolder(DataFolder);

            Sprite[] levelSprites = new Sprite[8];
            for (int i = 0; i < levelSprites.Length; i++)
                levelSprites[i] = LoadSprite($"{LightningArtDir}LightningTower_Level{i + 1}.png");

            GameObject impactPrefab = CreateLightningImpactPrefab();
            GameObject projectilePrefab = CreateLightningProjectilePrefab();
            GameObject arcPrefab = CreateLightningArcPrefab();

            string dataPath = $"{DataFolder}/Tower_LightningTower.asset";
            TowerData data = AssetDatabase.LoadAssetAtPath<TowerData>(dataPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<TowerData>();
                AssetDatabase.CreateAsset(data, dataPath);
            }

            data.displayName = "Lightning Tower";
            data.cost = 100;
            data.damage = 12f;
            data.range = 3.6f;
            data.fireRate = 1.15f;
            data.projectileSpeed = 9f;
            data.splashRadius = 0f;
            data.pierceCount = 0;
            data.chainCount = 2;
            data.chainRadius = 2.2f;
            data.chainDamageMultiplier = 0.75f;
            data.baseSprite = levelSprites[0];
            data.levelSprites = levelSprites;
            data.projectilePrefab = projectilePrefab;
            data.impactPrefab = impactPrefab;
            data.chainArcPrefab = arcPrefab;
            EditorUtility.SetDirty(data);

            AddLightningTowerToBuildMenu(data);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Tower Defense] Lightning Tower integrated: data, chain lightning, arc effect, and build menu option.");
        }

        private static GameObject CreateLightningProjectilePrefab()
        {
            string path = $"{PrefabsFolder}/LightningProjectile.prefab";
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root == null)
            {
                root = new GameObject("LightningProjectile");
                root.AddComponent<SpriteRenderer>();
                root.AddComponent<Projectile>();
            }

            SpriteRenderer sr = root.GetComponent<SpriteRenderer>();
            sr.sprite = LoadSprite(LightningArtDir + "LightningTower_Bolt.png");
            sr.sortingOrder = 5;
            root.transform.localScale = new Vector3(2f, 2f, 2f);
            if (AssetDatabase.Contains(root))
            {
                PrefabUtility.SavePrefabAsset(root);
            }
            else
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Object.DestroyImmediate(root);
                root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            return root;
        }

        private static GameObject CreateLightningImpactPrefab()
        {
            string path = $"{PrefabsFolder}/LightningImpact.prefab";
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root == null)
            {
                root = new GameObject("LightningImpact");
                root.AddComponent<SpriteRenderer>();
                root.AddComponent<ImpactEffect>();
            }

            SpriteRenderer sr = root.GetComponent<SpriteRenderer>();
            sr.sprite = LoadSprite(LightningArtDir + "LightningTower_Impact.png");
            sr.sortingOrder = 6;
            root.transform.localScale = new Vector3(1f, 1f, 1f);

            ImpactEffect effect = root.GetComponent<ImpactEffect>();
            SerializedObject so = new SerializedObject(effect);
            so.FindProperty("lifetime").floatValue = 0.35f;
            so.FindProperty("scaleGrowth").floatValue = 0.45f;
            so.ApplyModifiedProperties();
            if (AssetDatabase.Contains(root))
            {
                PrefabUtility.SavePrefabAsset(root);
            }
            else
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Object.DestroyImmediate(root);
                root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            return root;
        }

        private static GameObject CreateLightningArcPrefab()
        {
            string path = $"{PrefabsFolder}/LightningArc.prefab";
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root == null)
            {
                root = new GameObject("LightningArc");
                root.AddComponent<SpriteRenderer>();
                root.AddComponent<LightningArcEffect>();
            }

            SpriteRenderer sr = root.GetComponent<SpriteRenderer>();
            sr.sprite = LoadSprite(LightningArtDir + "LightningTower_Arc.png");
            sr.sortingOrder = 7;
            root.transform.localScale = Vector3.one;

            LightningArcEffect effect = root.GetComponent<LightningArcEffect>();
            SerializedObject so = new SerializedObject(effect);
            so.FindProperty("lifetime").floatValue = 0.18f;
            so.ApplyModifiedProperties();
            if (AssetDatabase.Contains(root))
            {
                PrefabUtility.SavePrefabAsset(root);
            }
            else
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Object.DestroyImmediate(root);
                root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            return root;
        }

        private static void AddLightningTowerToBuildMenu(TowerData data)
        {
            const string scenePath = "Assets/Scenes/game_scene.unity";
            if (EditorSceneManager.GetActiveScene().path != scenePath)
                EditorSceneManager.OpenScene(scenePath);

            GameObject panel = null;
            foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go.name == "BuildMenuPanel" && go.scene.IsValid())
                {
                    panel = go;
                    break;
                }
            }
            if (panel == null)
            {
                Debug.LogError("[Tower Defense] BuildMenuPanel not found. Run 'Tower Defense/2. Build Scene Setup' in game_scene first.");
                return;
            }

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(830f, 90f);

            GameObject towerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsFolder}/TowerPrefab.prefab");
            if (towerPrefab == null)
            {
                Debug.LogError("[Tower Defense] TowerPrefab not found.");
                return;
            }

            Transform existing = panel.transform.Find("BuildLightningButton");
            Button button = existing != null ? existing.GetComponent<Button>() : null;
            if (button == null)
                button = CreateButton(panel.transform, "BuildLightningButton", "Lightning\n100g", Vector2.zero, Vector2.zero, new Vector2(110f, 74f));
            SetupBuildOption(button, data, towerPrefab);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        // ------------------------------------------------------------
        // 9. Create the ice tower content and add it to the game scene
        // ------------------------------------------------------------
        private const string IceTowerArtDir = "Assets/Textures/Sprites/Skins_tower/ice_tower/";

        [MenuItem("Tower Defense/9. Integrate Ice Tower", false, 63)]
        public static void IntegrateIceTower()
        {
            EnsureFolder(PrefabsFolder);
            EnsureFolder(DataFolder);

            Sprite[] levelSprites = new Sprite[8];
            for (int i = 0; i < levelSprites.Length; i++)
                levelSprites[i] = LoadSprite($"{IceTowerArtDir}IceTower_Level{i + 1}.png");

            GameObject impactPrefab = CreateIceImpactPrefab();
            GameObject projectilePrefab = CreateIceProjectilePrefab();

            string dataPath = $"{DataFolder}/Tower_IceTower.asset";
            TowerData data = AssetDatabase.LoadAssetAtPath<TowerData>(dataPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<TowerData>();
                AssetDatabase.CreateAsset(data, dataPath);
            }

            data.displayName = "Ice Tower";
            data.cost = 80;
            data.damage = 6f;
            data.range = 3.5f;
            data.fireRate = 1f;
            data.projectileSpeed = 6.5f;
            data.splashRadius = 0f;
            data.pierceCount = 0;
            data.chainCount = 0;
            data.slowDuration = 2.5f;
            data.slowMultiplier = 0.55f;
            data.baseSprite = levelSprites[0];
            data.levelSprites = levelSprites;
            data.projectilePrefab = projectilePrefab;
            data.impactPrefab = impactPrefab;
            EditorUtility.SetDirty(data);

            AddIceTowerToBuildMenu(data);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Tower Defense] Ice Tower integrated: data, slowing shard, impact effect, and build menu option.");
        }

        private static GameObject CreateIceProjectilePrefab()
        {
            string path = $"{PrefabsFolder}/IceProjectile.prefab";
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root == null)
            {
                root = new GameObject("IceProjectile");
                root.AddComponent<SpriteRenderer>();
                root.AddComponent<Projectile>();
            }

            SpriteRenderer sr = root.GetComponent<SpriteRenderer>();
            sr.sprite = LoadSprite(IceTowerArtDir + "IceTower_Shard.png");
            sr.sortingOrder = 5;
            root.transform.localScale = new Vector3(2f, 2f, 2f);
            if (AssetDatabase.Contains(root))
            {
                PrefabUtility.SavePrefabAsset(root);
            }
            else
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Object.DestroyImmediate(root);
                root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            return root;
        }

        private static GameObject CreateIceImpactPrefab()
        {
            string path = $"{PrefabsFolder}/IceImpact.prefab";
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root == null)
            {
                root = new GameObject("IceImpact");
                root.AddComponent<SpriteRenderer>();
                root.AddComponent<ImpactEffect>();
            }

            SpriteRenderer sr = root.GetComponent<SpriteRenderer>();
            sr.sprite = LoadSprite(IceTowerArtDir + "IceTower_Impact.png");
            sr.sortingOrder = 6;
            root.transform.localScale = Vector3.one;

            ImpactEffect effect = root.GetComponent<ImpactEffect>();
            SerializedObject so = new SerializedObject(effect);
            so.FindProperty("lifetime").floatValue = 0.35f;
            so.FindProperty("scaleGrowth").floatValue = 0.4f;
            so.ApplyModifiedProperties();
            if (AssetDatabase.Contains(root))
            {
                PrefabUtility.SavePrefabAsset(root);
            }
            else
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Object.DestroyImmediate(root);
                root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            return root;
        }

        private static void AddIceTowerToBuildMenu(TowerData data)
        {
            const string scenePath = "Assets/Scenes/game_scene.unity";
            if (EditorSceneManager.GetActiveScene().path != scenePath)
                EditorSceneManager.OpenScene(scenePath);

            GameObject panel = null;
            foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go.name == "BuildMenuPanel" && go.scene.IsValid())
                {
                    panel = go;
                    break;
                }
            }
            if (panel == null)
            {
                Debug.LogError("[Tower Defense] BuildMenuPanel not found. Run 'Tower Defense/2. Build Scene Setup' in game_scene first.");
                return;
            }

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(830f, 90f);

            GameObject towerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsFolder}/TowerPrefab.prefab");
            if (towerPrefab == null)
            {
                Debug.LogError("[Tower Defense] TowerPrefab not found.");
                return;
            }

            Transform existing = panel.transform.Find("BuildIceButton");
            Button button = existing != null ? existing.GetComponent<Button>() : null;
            if (button == null)
                button = CreateButton(panel.transform, "BuildIceButton", "Ice\n80g", Vector2.zero, Vector2.zero, new Vector2(110f, 74f));
            SetupBuildOption(button, data, towerPrefab);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        // ------------------------------------------------------------
        // 10. Create the fire mage content and add it to the game scene
        // ------------------------------------------------------------
        private const string FireMageArtDir = "Assets/Textures/Sprites/Skins_tower/fire_mage_tower/";

        [MenuItem("Tower Defense/10. Integrate Fire Mage", false, 64)]
        public static void IntegrateFireMage()
        {
            EnsureFolder(PrefabsFolder);
            EnsureFolder(DataFolder);

            Sprite[] levelSprites = new Sprite[8];
            for (int i = 0; i < levelSprites.Length; i++)
                levelSprites[i] = LoadSprite($"{FireMageArtDir}FireMageTower_Level{i + 1}.png");

            GameObject impactPrefab = CreateFireImpactPrefab();
            GameObject projectilePrefab = CreateFireProjectilePrefab();

            string dataPath = $"{DataFolder}/Tower_Flamethrower.asset";
            TowerData data = AssetDatabase.LoadAssetAtPath<TowerData>(dataPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<TowerData>();
                AssetDatabase.CreateAsset(data, dataPath);
            }

            data.displayName = "Fire Mage";
            data.cost = 110;
            data.damage = 8f;
            data.range = 3.4f;
            data.fireRate = 0.8f;
            data.projectileSpeed = 6f;
            data.splashRadius = 1f;
            data.splashDamageMultiplier = 0.65f;
            data.pierceCount = 0;
            data.chainCount = 0;
            data.slowDuration = 0f;
            data.burnDamagePerSecond = 6f;
            data.burnDuration = 2.5f;
            data.baseSprite = levelSprites[0];
            data.levelSprites = levelSprites;
            data.projectilePrefab = projectilePrefab;
            data.impactPrefab = impactPrefab;
            EditorUtility.SetDirty(data);

            AddFireMageToBuildMenu(data);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Tower Defense] Fire Mage integrated: data, splash fireball, burn effect, and build menu option.");
        }

        private static GameObject CreateFireProjectilePrefab()
        {
            string path = $"{PrefabsFolder}/FireProjectile.prefab";
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root == null)
            {
                root = new GameObject("FireProjectile");
                root.AddComponent<SpriteRenderer>();
                root.AddComponent<Projectile>();
            }

            SpriteRenderer sr = root.GetComponent<SpriteRenderer>();
            sr.sprite = LoadSprite(FireMageArtDir + "FireMage_Fireball.png");
            sr.sortingOrder = 5;
            root.transform.localScale = new Vector3(2f, 2f, 2f);
            if (AssetDatabase.Contains(root))
            {
                PrefabUtility.SavePrefabAsset(root);
            }
            else
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Object.DestroyImmediate(root);
                root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            return root;
        }

        private static GameObject CreateFireImpactPrefab()
        {
            string path = $"{PrefabsFolder}/FireImpact.prefab";
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root == null)
            {
                root = new GameObject("FireImpact");
                root.AddComponent<SpriteRenderer>();
                root.AddComponent<ImpactEffect>();
            }

            SpriteRenderer sr = root.GetComponent<SpriteRenderer>();
            sr.sprite = LoadSprite(FireMageArtDir + "FireMage_Impact.png");
            sr.sortingOrder = 6;
            root.transform.localScale = Vector3.one;

            ImpactEffect effect = root.GetComponent<ImpactEffect>();
            SerializedObject so = new SerializedObject(effect);
            so.FindProperty("lifetime").floatValue = 0.35f;
            so.FindProperty("scaleGrowth").floatValue = 0.45f;
            so.ApplyModifiedProperties();
            if (AssetDatabase.Contains(root))
            {
                PrefabUtility.SavePrefabAsset(root);
            }
            else
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Object.DestroyImmediate(root);
                root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            return root;
        }

        private static void AddFireMageToBuildMenu(TowerData data)
        {
            const string scenePath = "Assets/Scenes/game_scene.unity";
            if (EditorSceneManager.GetActiveScene().path != scenePath)
                EditorSceneManager.OpenScene(scenePath);

            GameObject panel = null;
            foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go.name == "BuildMenuPanel" && go.scene.IsValid())
                {
                    panel = go;
                    break;
                }
            }
            if (panel == null)
            {
                Debug.LogError("[Tower Defense] BuildMenuPanel not found. Run 'Tower Defense/2. Build Scene Setup' in game_scene first.");
                return;
            }

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(830f, 90f);

            GameObject towerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsFolder}/TowerPrefab.prefab");
            if (towerPrefab == null)
            {
                Debug.LogError("[Tower Defense] TowerPrefab not found.");
                return;
            }

            Transform existing = panel.transform.Find("BuildFireButton");
            Button button = existing != null ? existing.GetComponent<Button>() : null;
            if (button == null)
                button = CreateButton(panel.transform, "BuildFireButton", "Fire\n110g", Vector2.zero, Vector2.zero, new Vector2(110f, 74f));
            SetupBuildOption(button, data, towerPrefab);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        // ------------------------------------------------------------
        // Shared helpers
        // ------------------------------------------------------------
        private static Sprite LoadSprite(string path, string suffix = "_0")
        {
            Object[] reps = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
            foreach (Object o in reps)
            {
                if (o is Sprite sprite && sprite.name.EndsWith(suffix))
                    return sprite;
            }
            if (reps.Length > 0 && reps[0] is Sprite first) return first;
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite GetOrCreateWhiteSprite()
        {
            return GetOrCreateGeneratedSprite("WhiteSquare.png", 4, (x, y, size) => Color.white);
        }

        private static Sprite GetOrCreateRingSprite()
        {
            return GetOrCreateGeneratedSprite("SlotMarker.png", 32, (x, y, size) =>
            {
                Vector2 center = new Vector2(size / 2f, size / 2f);
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                float outer = size / 2f - 1f;
                float inner = outer - 3f;
                return (dist <= outer && dist >= inner) ? Color.white : Color.clear;
            });
        }

        private delegate Color PixelFunc(int x, int y, int size);

        private static Sprite GetOrCreateGeneratedSprite(string fileName, int size, PixelFunc pixelFunc)
        {
            string path = $"{GeneratedRoot}/{fileName}";
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            EnsureFolder(GeneratedRoot);

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, pixelFunc(x, y, size));
            tex.Apply();

            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            File.WriteAllBytes(path, png);
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = size;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(string.IsNullOrEmpty(parent) ? "Assets" : parent, folderName);
        }

        private static Text CreateText(Transform parent, string name, string content, Vector2 anchor, Vector2 anchoredPos, TextAnchor alignment)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(400, 40);

            Text text = go.AddComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 28;
            text.alignment = alignment;
            text.color = Color.white;

            Outline outline = go.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchor, Vector2 anchoredPos, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f, 0.9f);

            GameObject textGO = new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(go.transform, false);
            RectTransform textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;

            Text text = textGO.AddComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 18;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

            return go.GetComponent<Button>();
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.8f);
            return go;
        }
    }
}
