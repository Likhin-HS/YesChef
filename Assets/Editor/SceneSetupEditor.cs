using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using YesChef.Core;
using YesChef.Managers;
using YesChef.Player;
using YesChef.Stations;
using YesChef.UI;

namespace YesChef.Editor
{
    /// <summary>
    /// Editor tools for setting up the Yes Chef! scene.
    /// Run each menu item once, then save the scene.
    /// </summary>
    public static class SceneSetupEditor
    {
        // ---------------------------------------------------------------
        // Wire all [SerializeField] references on 3D scene objects
        // ---------------------------------------------------------------
        [MenuItem("Tools/YesChef/Wire Scene References")]
        public static void WireSceneReferences()
        {
            // ----- Player/Chef -----
            var player = Object.FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                var so = new SerializedObject(player);
                var held = player.transform.Find("Held");
                if (held != null)
                {
                    SetRef(so, "_heldVisual", held.GetComponent<Renderer>());
                    SetRef(so, "_heldAnchor", held);
                }
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(player);
            }

            // ----- ChoppingTable -----
            var table = Object.FindFirstObjectByType<ChoppingTable>();
            if (table != null)
            {
                var so = new SerializedObject(table);
                var chopItem = table.transform.Find("ChopItem");
                if (chopItem != null)
                    SetRef(so, "_itemVisual", chopItem.GetComponent<Renderer>());
                var progressBar = table.transform.Find("ProgressBar");
                if (progressBar != null)
                {
                    SetRef(so, "_progressBar", progressBar);
                    var fill = progressBar.Find("Fill");
                    if (fill != null)
                        SetRef(so, "_progressFill", fill);
                }
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(table);
            }

            // ----- Stove -----
            var stove = Object.FindFirstObjectByType<Stove>();
            if (stove != null)
            {
                var so = new SerializedObject(stove);
                var visuals = new Renderer[GameConstants.StoveSlotCount];
                var bars = new Transform[GameConstants.StoveSlotCount];
                var fills = new Transform[GameConstants.StoveSlotCount];
                for (int i = 0; i < GameConstants.StoveSlotCount; i++)
                {
                    var meat = stove.transform.Find($"Meat{i}");
                    if (meat != null) visuals[i] = meat.GetComponent<Renderer>();
                    var bar = stove.transform.Find($"Bar{i}");
                    if (bar != null)
                    {
                        bars[i] = bar;
                        var fill = bar.Find("Fill");
                        if (fill != null) fills[i] = fill;
                    }
                }
                SetArrayRef(so, "_slotVisuals", visuals);
                SetArrayRef(so, "_slotBars", bars);
                SetArrayRef(so, "_slotFills", fills);
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(stove);
            }

            // ----- Customer Windows -----
            var windows = Object.FindObjectsByType<CustomerWindow>(FindObjectsSortMode.None);
            foreach (var window in windows)
            {
                var so = new SerializedObject(window);
                var status = window.transform.Find("Status");
                if (status != null)
                    SetRef(so, "_statusVisual", status.GetComponent<Renderer>());
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(window);
            }

            // ----- GameManager -----
            var manager = Object.FindFirstObjectByType<GameManager>();
            if (manager != null)
            {
                var so = new SerializedObject(manager);
                var windowsProp = so.FindProperty("_customerWindows");
                windowsProp.arraySize = windows.Length;
                // Sort by window index
                System.Array.Sort(windows, (a, b) => a.WindowIndex.CompareTo(b.WindowIndex));
                for (int i = 0; i < windows.Length; i++)
                    windowsProp.GetArrayElementAtIndex(i).objectReferenceValue = windows[i];

                var tables = Object.FindObjectsByType<ChoppingTable>(FindObjectsSortMode.None);
                SetArrayRef(so, "_tables", tables);

                var stoves = Object.FindObjectsByType<Stove>(FindObjectsSortMode.None);
                SetArrayRef(so, "_stoves", stoves);

                SetRef(so, "_player", player);

                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(manager);
            }

            Debug.Log("All scene references wired. Save the scene (Ctrl+S).");
        }

        // ---------------------------------------------------------------
        // Build the HUD Canvas and wire GameHUD references
        // ---------------------------------------------------------------
        [MenuItem("Tools/YesChef/Build HUD Canvas")]
        public static void BuildHUD()
        {
            var hud = Object.FindFirstObjectByType<GameHUD>();
            if (hud == null)
            {
                Debug.LogError("No GameHUD component found in the scene.");
                return;
            }

            // Delete any existing canvas children so we can rebuild cleanly
            for (int i = hud.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(hud.transform.GetChild(i).gameObject);

            // Ensure EventSystem exists
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
                Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
            }

            var canvasGo = new GameObject("HUDCanvas");
            canvasGo.transform.SetParent(hud.transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();
            Transform root = canvasGo.transform;

            // === Top Bar ===
            var top = CreatePanel(root, "TopBar", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -30f), new Vector2(760f, 60f), new Color(1f, 1f, 1f, 0.85f));
            var scoreText = CreateLabel(top.transform, "ScoreText", 20, TextAnchor.MiddleLeft,
                new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(400f, 50f));
            var timerText = CreateLabel(top.transform, "TimerText", 22, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200f, 50f));
            var heldText = CreateLabel(top.transform, "HeldText", 20, TextAnchor.MiddleRight,
                new Vector2(1f, 0.5f), new Vector2(-10f, 0f), new Vector2(300f, 50f));

            // === Order Cards ===
            var ordersRoot = new GameObject("Orders");
            ordersRoot.transform.SetParent(root, false);
            var ordersRt = ordersRoot.AddComponent<RectTransform>();
            ordersRt.anchorMin = new Vector2(0f, 0.5f);
            ordersRt.anchorMax = new Vector2(0f, 0.5f);
            ordersRt.pivot = new Vector2(0f, 0.5f);
            ordersRt.anchoredPosition = new Vector2(10f, 20f);
            ordersRt.sizeDelta = new Vector2(300f, 560f);

            var orderTitles = new Text[GameConstants.MaxActiveOrders];
            var orderNeeds = new Text[GameConstants.MaxActiveOrders];
            var orderTimers = new Text[GameConstants.MaxActiveOrders];
            var orderPopups = new Text[GameConstants.MaxActiveOrders];

            for (int i = 0; i < GameConstants.MaxActiveOrders; i++)
            {
                var card = CreatePanel(ordersRoot.transform, $"Order{i + 1}",
                    new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(0f, -70f - i * 135f), new Vector2(290f, 125f),
                    new Color(1f, 1f, 1f, 0.9f));

                orderTitles[i] = CreateLabel(card.transform, "Title", 18, TextAnchor.UpperLeft,
                    new Vector2(0f, 1f), new Vector2(10f, -6f), new Vector2(270f, 26f));
                orderTitles[i].text = $"Window {i + 1}";

                orderNeeds[i] = CreateLabel(card.transform, "Needs", 17, TextAnchor.UpperLeft,
                    new Vector2(0f, 1f), new Vector2(10f, -34f), new Vector2(270f, 52f));
                orderNeeds[i].text = "\u2014";

                orderTimers[i] = CreateLabel(card.transform, "Age", 15, TextAnchor.LowerLeft,
                    new Vector2(0f, 0f), new Vector2(10f, 6f), new Vector2(270f, 24f));

                orderPopups[i] = CreateLabel(card.transform, "Popup", 26, TextAnchor.MiddleRight,
                    new Vector2(1f, 0.5f), new Vector2(-8f, 0f), new Vector2(120f, 40f));
                var pc = orderPopups[i].color;
                pc.a = 0f;
                orderPopups[i].color = pc;
                orderPopups[i].fontStyle = FontStyle.Bold;
            }

            // === Bottom Bar ===
            var bottom = CreatePanel(root, "BottomBar", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 55f), new Vector2(900f, 100f), new Color(1f, 1f, 1f, 0.85f));
            var promptText = CreateLabel(bottom.transform, "PromptText", 19, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(880f, 40f));
            var fridgeText = CreateLabel(bottom.transform, "FridgeText", 16, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(-220f, 14f), new Vector2(300f, 28f));

            var vegBtn = CreateButton(bottom.transform, "Veg", new Vector2(-70f, -62f));
            var cheeseBtn = CreateButton(bottom.transform, "Cheese", new Vector2(30f, -62f));
            var meatBtn = CreateButton(bottom.transform, "Meat", new Vector2(130f, -62f));
            var pauseBtn = CreateButton(bottom.transform, "Pause", new Vector2(280f, -62f));
            var quitBtn = CreateButton(bottom.transform, "Quit", new Vector2(380f, -62f));
            pauseBtn.gameObject.SetActive(false);

            // === Modals ===
            string startMsg = "YES CHEF!\n\nServe as many orders as you can in 3 minutes.\n\n" +
                "WASD / Arrows \u2014 move\nE \u2014 interact / take / serve\n" +
                "Q \u2014 pick up chopped veg / cooked meat\n" +
                "1 / 2 / 3 \u2014 choose fridge ingredient\nP or Esc \u2014 pause\n\n" +
                "Veg: chop 2s on Table (20)  \u2022  Cheese: ready now (10)  \u2022  Meat: cook 6s on Stove (30)\n" +
                "Score = ingredients \u2212 seconds open (can go negative!). Trash clears your hand.";

            var startPanel = CreateModal(root, "StartPanel", startMsg, "Start Game");
            var pausePanel = CreateModal(root, "PausePanel", "Paused", "Resume");
            var gameOverPanel = CreateModal(root, "GameOverPanel", "Time!", "Play Again");
            var gameOverText = gameOverPanel.transform.Find("Box/Message")?.GetComponent<Text>();

            pausePanel.SetActive(false);
            gameOverPanel.SetActive(false);

            // === Wire SerializeField references on GameHUD ===
            var so = new SerializedObject(hud);

            SetRef(so, "_scoreText", scoreText);
            SetRef(so, "_timerText", timerText);
            SetRef(so, "_heldText", heldText);
            SetRef(so, "_promptText", promptText);
            SetRef(so, "_fridgeText", fridgeText);

            SetRef(so, "_startPanel", startPanel);
            SetRef(so, "_pausePanel", pausePanel);
            SetRef(so, "_gameOverPanel", gameOverPanel);
            SetRef(so, "_gameOverText", gameOverText);

            SetRef(so, "_pauseButton", pauseBtn);
            SetRef(so, "_quitButton", quitBtn);
            SetRef(so, "_vegButton", vegBtn);
            SetRef(so, "_cheeseButton", cheeseBtn);
            SetRef(so, "_meatButton", meatBtn);

            SetArrayRef(so, "_orderTitles", orderTitles);
            SetArrayRef(so, "_orderNeeds", orderNeeds);
            SetArrayRef(so, "_orderTimers", orderTimers);
            SetArrayRef(so, "_orderPopups", orderPopups);

            // Wire scene references on GameHUD
            var manager = Object.FindFirstObjectByType<GameManager>();
            var player = Object.FindFirstObjectByType<PlayerController>();
            var interactor = Object.FindFirstObjectByType<PlayerInteractor>();
            var fridge = Object.FindFirstObjectByType<Refrigerator>();
            var windows = Object.FindObjectsByType<CustomerWindow>(FindObjectsSortMode.None);

            SetRef(so, "_manager", manager);
            SetRef(so, "_player", player);
            SetRef(so, "_interactor", interactor);
            SetRef(so, "_fridge", fridge);

            var windowsProp = so.FindProperty("_windows");
            System.Array.Sort(windows, (a, b) => a.WindowIndex.CompareTo(b.WindowIndex));
            windowsProp.arraySize = windows.Length;
            for (int i = 0; i < windows.Length; i++)
                windowsProp.GetArrayElementAtIndex(i).objectReferenceValue = windows[i];

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(hud);

            Undo.RegisterCreatedObjectUndo(canvasGo, "Build HUD Canvas");
            Debug.Log("HUD Canvas built and wired. Save the scene (Ctrl+S).");
        }

        // ---------------------------------------------------------------
        // Fix layout — reposition 3D objects to match the blueprint
        // ---------------------------------------------------------------
        [MenuItem("Tools/YesChef/Fix Layout")]
        public static void FixLayout()
        {
            // Blueprint layout (top-down, X = left/right, Z = up/down):
            // Kitchen: 18 wide x 11 deep, centered at origin
            // Left wall (West, x=-9): 4 customer windows evenly spaced
            // Center-right area: Table (upper), Stove (lower)
            // Right side: Refrigerator (upper-right), Trash (lower-right)
            // Player starts left-center

            float halfW = GameConstants.KitchenWidth / 2f;  // 9
            float halfD = GameConstants.KitchenDepth / 2f;   // 5.5

            // Windows — along the west wall, evenly spaced vertically
            // The wall is at x=-9, windows should poke through it slightly
            // 4 windows spread across the depth: z = 3.3, 1.1, -1.1, -3.3
            float windowX = -halfW + 0.35f; // Slightly inside the west wall
            float[] windowZ = { 3.3f, 1.1f, -1.1f, -3.3f };
            var windows = Object.FindObjectsByType<CustomerWindow>(FindObjectsSortMode.None);
            System.Array.Sort(windows, (a, b) => a.WindowIndex.CompareTo(b.WindowIndex));

            for (int i = 0; i < windows.Length && i < windowZ.Length; i++)
            {
                var t = windows[i].transform;
                t.position = new Vector3(windowX, 0.6f, windowZ[i]);
                t.localScale = new Vector3(1.2f, 1.2f, 1.6f);
                EditorUtility.SetDirty(windows[i]);
            }

            // Table — center-upper area
            var table = Object.FindFirstObjectByType<ChoppingTable>();
            if (table != null)
            {
                table.transform.position = new Vector3(1.5f, 0.5f, 2f);
                table.transform.localScale = new Vector3(3f, 1f, 1.6f);
                EditorUtility.SetDirty(table);
            }

            // Stove — center-lower area
            var stove = Object.FindFirstObjectByType<Stove>();
            if (stove != null)
            {
                stove.transform.position = new Vector3(1.5f, 0.5f, -1.5f);
                stove.transform.localScale = new Vector3(3f, 1f, 1.6f);
                EditorUtility.SetDirty(stove);
            }

            // Refrigerator — upper-right corner
            var fridge = Object.FindFirstObjectByType<Refrigerator>();
            if (fridge != null)
            {
                fridge.transform.position = new Vector3(6.5f, 0.9f, 3.5f);
                fridge.transform.localScale = new Vector3(2f, 1.8f, 2f);
                EditorUtility.SetDirty(fridge);
            }

            // Trash — lower-right corner
            var trash = Object.FindFirstObjectByType<Trash>();
            if (trash != null)
            {
                trash.transform.position = new Vector3(6.5f, 0.5f, -3.5f);
                trash.transform.localScale = new Vector3(1.6f, 1f, 1.6f);
                EditorUtility.SetDirty(trash);
            }

            // Player — left-center, between windows and stations
            var player = Object.FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                player.transform.position = new Vector3(-4f, 0f, 0f);
                EditorUtility.SetDirty(player);
            }

            // Camera — centered top-down, slightly forward for good framing
            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0f, 18f, -1f);
                cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                cam.orthographic = true;
                cam.orthographicSize = 7f;
                EditorUtility.SetDirty(cam);
            }

            Debug.Log("Layout fixed to match blueprint. Save the scene (Ctrl+S).");
        }

        // ---------------------------------------------------------------
        // Run all setup steps in sequence
        // ---------------------------------------------------------------
        [MenuItem("Tools/YesChef/Setup Everything")]
        public static void SetupEverything()
        {
            FixLayout();
            WireSceneReferences();
            BuildHUD();
            Debug.Log("=== All setup complete. Save the scene (Ctrl+S). ===");
        }

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------

        private static GameObject CreatePanel(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            go.GetComponent<Image>().color = color;
            return go;
        }

        private static Text CreateLabel(Transform parent, string name, int size,
            TextAnchor align, Vector2 anchor, Vector2 pos, Vector2 box)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = box;
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = align;
            text.color = Color.black;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Button CreateButton(Transform parent, string label, Vector2 pos)
        {
            var go = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(92f, 34f);
            go.GetComponent<Image>().color = Color.white;

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            var text = textGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.black;
            text.text = label;

            return go.GetComponent<Button>();
        }

        private static GameObject CreateModal(Transform parent, string name,
            string message, string buttonLabel)
        {
            var overlay = new GameObject(name, typeof(RectTransform), typeof(Image));
            overlay.transform.SetParent(parent, false);
            var rt = overlay.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            overlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            var box = CreatePanel(overlay.transform, "Box",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(620f, 480f), Color.white);

            var msg = CreateLabel(box.transform, "Message", 19, TextAnchor.UpperCenter,
                new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(580f, 380f));
            msg.text = message;

            var button = CreateButton(box.transform, buttonLabel, new Vector2(0f, -190f));
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(200f, 48f);

            return overlay;
        }

        private static void SetRef(SerializedObject so, string field, Object value)
        {
            var prop = so.FindProperty(field);
            if (prop != null)
                prop.objectReferenceValue = value;
            else
                Debug.LogWarning($"Field '{field}' not found on {so.targetObject.GetType().Name}");
        }

        private static void SetArrayRef<T>(SerializedObject so, string field, T[] values) where T : Object
        {
            var prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogWarning($"Field '{field}' not found on {so.targetObject.GetType().Name}");
                return;
            }
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
