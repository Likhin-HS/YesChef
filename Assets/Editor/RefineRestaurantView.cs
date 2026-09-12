using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using YesChef;

namespace YesChef.Editor
{
    public static class RefineRestaurantView
    {
        [MenuItem("Tools/Save HighRes Game Screenshot")]
        public static void SaveScreenshot()
        {
            var cam = Camera.main;
            if (cam == null) return;
            var rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
            var prevRt = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            tex.Apply();
            cam.targetTexture = prevRt;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);
            byte[] bytes = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            string path = @"C:\Users\likhi\.gemini\antigravity-ide\brain\7ba94137-cd6a-44a3-b96d-739f417f4ff7\final_slanted_walls_gameview.png";
            System.IO.File.WriteAllBytes(path, bytes);
            Debug.Log("Saved screenshot to " + path);

            // Also capture a nice 3/4 closeup of the customer windows
            var closeGo = new GameObject("TempCloseupCam");
            var closeCam = closeGo.AddComponent<Camera>();
            closeCam.orthographic = false;
            closeCam.fieldOfView = 50f;
            closeGo.transform.position = new Vector3(-6.2f, 2.8f, -2.8f);
            closeGo.transform.rotation = Quaternion.Euler(15f, -50f, 0f);

            var closeRt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
            closeCam.targetTexture = closeRt;
            closeCam.Render();
            RenderTexture.active = closeRt;
            var closeTex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            closeTex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            closeTex.Apply();
            closeCam.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(closeRt);
            byte[] closeBytes = closeTex.EncodeToPNG();
            Object.DestroyImmediate(closeTex);
            Object.DestroyImmediate(closeGo);

            string closePath = @"C:\Users\likhi\.gemini\antigravity-ide\brain\7ba94137-cd6a-44a3-b96d-739f417f4ff7\customer_windows_closeup.png";
            System.IO.File.WriteAllBytes(closePath, closeBytes);
            Debug.Log("Saved closeup screenshot to " + closePath);
        }

        [MenuItem("Tools/Start Service In PlayMode")]
        public static void StartService()
        {
            if (YesChef.Managers.GameManager.Instance != null)
            {
                YesChef.Managers.GameManager.Instance.StartGame();
            }
        }

        [MenuItem("Tools/Refine Restaurant View & Windows")]
        public static void Execute()
        {
            var creamMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_WallCream.mat");
            var darkWallMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_WallDark.mat");
            var honeyWoodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_WoodHoney.mat");
            var darkWoodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_WoodDark.mat");
            var stainlessMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Stainless.mat");
            var whiteMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_White.mat");
            var blackMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_Black.mat");
            var skinMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_SkinTone.mat");
            var hairDarkMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_HairDark.mat");
            var hairBlondeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_HairBlonde.mat");
            var blueCustMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_CustomerBlue.mat");
            var redCustMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_CustomerRed.mat");
            var greenCustMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_CustomerGreen.mat");
            var purpleCustMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_CustomerPurple.mat");
            var statusIdleMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_StatusIdle.mat");
            var redTrashMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/M_TrashRed.mat");

            var vegSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Icons/icon_vegetable.png");
            var cheeseSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Icons/icon_cheese.png");
            var meatSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Icons/icon_meat.png");
            var customFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/LiberationSans.ttf");
            if (customFont == null) customFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            // 1. Camera Setup - Clean, subtle perspective matching reference art
            var cam = Camera.main;
            if (cam != null)
            {
                cam.orthographic = false;
                cam.fieldOfView = 48f;
                cam.transform.position = new Vector3(0f, 16.6f, -9.0f);
                cam.transform.rotation = Quaternion.Euler(56f, 0f, 0f);
            }

            // 2. Lighting & Environment
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientSkyColor = new Color(0.74f, 0.72f, 0.70f, 1f);
            var dirLightGo = GameObject.Find("Directional Light");
            if (dirLightGo != null)
            {
                dirLightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                var lightComp = dirLightGo.GetComponent<Light>();
                if (lightComp != null)
                {
                    lightComp.intensity = 1.0f;
                    lightComp.shadowStrength = 0.35f;
                }
            }

            // 3. Corner Posts (4 dark charcoal pillars connecting the room corners)
            var cornerPostsRoot = GameObject.Find("CornerPosts");
            if (cornerPostsRoot == null)
            {
                cornerPostsRoot = new GameObject("CornerPosts");
            }
            while (cornerPostsRoot.transform.childCount > 0)
            {
                Object.DestroyImmediate(cornerPostsRoot.transform.GetChild(0).gameObject);
            }
            CreateCornerPost("Post_NW", new Vector3(-8.85f, 1.25f, 5.55f), cornerPostsRoot.transform, darkWallMat);
            CreateCornerPost("Post_NE", new Vector3(8.85f, 1.25f, 5.55f), cornerPostsRoot.transform, darkWallMat);
            CreateCornerPost("Post_SW", new Vector3(-8.85f, 1.25f, -5.55f), cornerPostsRoot.transform, darkWallMat);
            CreateCornerPost("Post_SE", new Vector3(8.85f, 1.25f, -5.55f), cornerPostsRoot.transform, darkWallMat);

            // 4. North Wall (Straight horizontal back wall)
            var wallNorth = GameObject.Find("WallNorth");
            if (wallNorth != null)
            {
                wallNorth.transform.position = new Vector3(0f, 1.25f, 5.55f);
                wallNorth.transform.rotation = Quaternion.identity;
                wallNorth.transform.localScale = new Vector3(17.70f, 2.50f, 0.40f);
                SetMat(wallNorth, creamMat);

                var cap = wallNorth.transform.Find("CapRail");
                if (cap == null) cap = wallNorth.transform.Find("TopCap");
                if (cap != null)
                {
                    cap.name = "CapRail";
                    cap.localPosition = new Vector3(0f, 0.48f, 0f);
                    cap.localScale = new Vector3(1.0f, 0.08f, 1.25f);
                    SetMat(cap.gameObject, darkWallMat);
                }
            }

            // 5. South Wall (Straight horizontal low threshold in foreground)
            var wallSouth = GameObject.Find("WallSouth");
            if (wallSouth != null)
            {
                wallSouth.transform.position = new Vector3(0f, 0.30f, -5.55f);
                wallSouth.transform.rotation = Quaternion.identity;
                wallSouth.transform.localScale = new Vector3(17.70f, 0.60f, 0.40f);
                SetMat(wallSouth, darkWallMat);
            }

            // 6. East Wall (Straight right wall connecting NE and SE posts)
            var wallEast = GameObject.Find("WallEast");
            if (wallEast != null)
            {
                wallEast.transform.position = new Vector3(8.85f, 1.25f, 0f);
                wallEast.transform.rotation = Quaternion.identity;
                wallEast.transform.localScale = new Vector3(0.40f, 2.50f, 11.10f);
                SetMat(wallEast, creamMat);

                while (wallEast.transform.childCount > 0)
                {
                    Object.DestroyImmediate(wallEast.transform.GetChild(0).gameObject);
                }
                var eastCap = GameObject.CreatePrimitive(PrimitiveType.Cube);
                eastCap.name = "CapRail";
                eastCap.transform.SetParent(wallEast.transform, false);
                eastCap.transform.localPosition = new Vector3(0f, 0.48f, 0f);
                eastCap.transform.localScale = new Vector3(1.25f, 0.08f, 1.0f);
                SetMat(eastCap, darkWallMat);
                Object.DestroyImmediate(eastCap.GetComponent<Collider>());
            }

            // 7. West Sidewalk
            var sidewalk = GameObject.Find("SidewalkWest");
            if (sidewalk != null)
            {
                sidewalk.transform.position = new Vector3(-11.20f, -0.06f, 0f);
                sidewalk.transform.rotation = Quaternion.identity;
                sidewalk.transform.localScale = new Vector3(4.0f, 0.10f, 15.0f);
            }

            // 8. West Wall Root & Openings
            var westWall = GameObject.Find("WestWall");
            if (westWall == null)
            {
                westWall = new GameObject("WestWall");
            }
            westWall.transform.position = new Vector3(-8.85f, 0f, 0f);
            westWall.transform.rotation = Quaternion.identity;
            westWall.transform.localScale = Vector3.one;

            // Retain/find the 4 CustomerWindow game objects
            var windows = new CustomerWindow[4];
            for (int i = 0; i < 4; i++)
            {
                int num = i + 1;
                var winGo = GameObject.Find("CustomerWindow_" + num);
                if (winGo != null)
                {
                    windows[i] = winGo.GetComponent<CustomerWindow>();
                    winGo.transform.SetParent(null, true); // unparent temporarily
                }
            }

            // Clear old architectural children in WestWall
            while (westWall.transform.childCount > 0)
            {
                Object.DestroyImmediate(westWall.transform.GetChild(0).gameObject);
            }

            float wallLength = 11.10f; // from z = -5.55 to +5.55
            float wallThickness = 0.40f;

            // Lower solid wall (from y = 0 to 0.90)
            var lowerWall = CreatePart("LowerWall", westWall.transform,
                new Vector3(0f, 0.45f, 0f), new Vector3(wallThickness, 0.90f, wallLength), creamMat);
            var baseboard = CreatePart("Baseboard", westWall.transform,
                new Vector3(0.02f, 0.08f, 0f), new Vector3(wallThickness + 0.04f, 0.16f, wallLength), darkWallMat);

            // Upper solid header (from y = 2.10 to 2.40)
            var upperHeader = CreatePart("UpperHeader", westWall.transform,
                new Vector3(0f, 2.25f, 0f), new Vector3(wallThickness, 0.30f, wallLength), creamMat);
            var crownCap = CreatePart("CrownCap", westWall.transform,
                new Vector3(0f, 2.42f, 0f), new Vector3(wallThickness + 0.10f, 0.14f, wallLength), darkWallMat);

            // 5 Wall Dividers framing the 4 window openings:
            // Windows at localZ: +3.30, +1.10, -1.10, -3.30 (each 1.50m wide, from -0.75 to +0.75)
            // Window 1: 2.55 to 4.05
            // Window 2: 0.35 to 1.85
            // Window 3: -1.85 to -0.35
            // Window 4: -4.05 to -2.55
            CreatePart("Divider_EndN", westWall.transform,
                new Vector3(0f, 1.50f, 4.80f), new Vector3(wallThickness, 1.20f, 1.50f), creamMat);
            CreatePart("Divider_1_2", westWall.transform,
                new Vector3(0f, 1.50f, 2.20f), new Vector3(wallThickness, 1.20f, 0.70f), creamMat);
            CreatePart("Divider_2_3", westWall.transform,
                new Vector3(0f, 1.50f, 0.00f), new Vector3(wallThickness, 1.20f, 0.70f), creamMat);
            CreatePart("Divider_3_4", westWall.transform,
                new Vector3(0f, 1.50f, -2.20f), new Vector3(wallThickness, 1.20f, 0.70f), creamMat);
            CreatePart("Divider_EndS", westWall.transform,
                new Vector3(0f, 1.50f, -4.80f), new Vector3(wallThickness, 1.20f, 1.50f), creamMat);

            // Setup each of the 4 Customer Windows
            float[] windowZ = { 3.30f, 1.10f, -1.10f, -3.30f };
            Material[] shirtMats = { blueCustMat, redCustMat, greenCustMat, purpleCustMat };
            Material[] hairMats = { hairDarkMat, hairDarkMat, hairBlondeMat, hairDarkMat };
            int[] hairStyles = { 0, 1, 2, 0 }; // 0: slick dark, 1: high bun dark, 2: blonde bob, 0: dark parted

            for (int i = 0; i < 4; i++)
            {
                var win = windows[i];
                if (win == null) continue;

                var winGo = win.gameObject;
                winGo.transform.SetParent(westWall.transform, false);
                winGo.transform.localPosition = new Vector3(0f, 0.45f, windowZ[i]);
                winGo.transform.localRotation = Quaternion.identity;
                winGo.transform.localScale = Vector3.one;

                // Configure interaction collider (accessible from kitchen)
                var boxCol = winGo.GetComponent<BoxCollider>();
                if (boxCol == null) boxCol = winGo.AddComponent<BoxCollider>();
                boxCol.center = new Vector3(0.40f, 0.90f, 0f);
                boxCol.size = new Vector3(1.80f, 1.20f, 1.60f);
                boxCol.isTrigger = false;

                // Clear old children of the window to rebuild cleanly
                while (winGo.transform.childCount > 0)
                {
                    Object.DestroyImmediate(winGo.transform.GetChild(0).gameObject);
                }

                // Window Casement (Warm honey wood frame)
                // Counter Sill
                var sill = CreatePart("CounterSill", winGo.transform,
                    new Vector3(0.10f, 0.45f, 0f), new Vector3(0.70f, 0.08f, 1.55f), honeyWoodMat);
                // Left & Right Jambs
                CreatePart("Jamb_N", winGo.transform,
                    new Vector3(0f, 1.05f, 0.73f), new Vector3(0.46f, 1.20f, 0.08f), honeyWoodMat);
                CreatePart("Jamb_S", winGo.transform,
                    new Vector3(0f, 1.05f, -0.73f), new Vector3(0.46f, 1.20f, 0.08f), honeyWoodMat);
                // Top Lintel
                CreatePart("Lintel", winGo.transform,
                    new Vector3(0f, 1.63f, 0f), new Vector3(0.48f, 0.08f, 1.55f), honeyWoodMat);
                // Back panel
                CreatePart("BackPanel", winGo.transform,
                    new Vector3(-0.35f, 1.05f, 0f), new Vector3(0.04f, 1.20f, 1.55f), darkWoodMat);

                // Service bell on counter sill
                var bell = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bell.name = "ServiceBell";
                bell.transform.SetParent(winGo.transform, false);
                bell.transform.localPosition = new Vector3(0.28f, 0.52f, -0.45f);
                bell.transform.localScale = new Vector3(0.16f, 0.04f, 0.16f);
                SetMat(bell, stainlessMat);
                Object.DestroyImmediate(bell.GetComponent<Collider>());

                // Status Lamp on counter sill
                var lamp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                lamp.name = "Status";
                lamp.transform.SetParent(winGo.transform, false);
                lamp.transform.localPosition = new Vector3(0.28f, 0.50f, 0.45f);
                lamp.transform.localScale = new Vector3(0.12f, 0.03f, 0.12f);
                SetMat(lamp, statusIdleMat);
                Object.DestroyImmediate(lamp.GetComponent<Collider>());
                var statusRenderer = lamp.GetComponent<Renderer>();

                // Build Chibi Customer standing behind counter, facing East (+X) into kitchen
                var custCharGo = new GameObject("CustomerChar");
                custCharGo.transform.SetParent(winGo.transform, false);
                custCharGo.transform.localPosition = new Vector3(-0.04f, 0.38f, 0f);
                custCharGo.transform.localRotation = Quaternion.identity; // Pure East (+X) into kitchen
                custCharGo.transform.localScale = Vector3.one;

                BuildChibiCustomer(custCharGo.transform, shirtMats[i], hairMats[i], hairStyles[i], skinMat, darkWallMat, redCustMat);

                // Build Floating Order Card (Speech Bubble)
                // Positioned right beside the customer over the counter sill
                var bubbleGo = new GameObject("SpeechBubble");
                bubbleGo.transform.SetParent(winGo.transform, false);
                bubbleGo.transform.localPosition = new Vector3(1.48f, 1.15f, 0f);
                bubbleGo.transform.localRotation = Quaternion.Euler(56f, 0f, 0f); // aligns flat with camera view
                bubbleGo.transform.localScale = Vector3.one;

                var cardBacking = CreatePart("Background", bubbleGo.transform,
                    new Vector3(0f, 0f, 0f), new Vector3(1.45f, 0.95f, 0.02f), whiteMat);
                var cardShadow = CreatePart("Shadow", bubbleGo.transform,
                    new Vector3(0.03f, -0.03f, 0.02f), new Vector3(1.48f, 0.98f, 0.02f), darkWallMat);

                // Pointer tail pointing LEFT towards customer's face
                var tail = CreatePart("Tail", bubbleGo.transform,
                    new Vector3(-0.76f, -0.05f, 0f), new Vector3(0.20f, 0.20f, 0.03f), whiteMat);
                tail.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

                // 3 Icon slots for requested ingredients
                var bubbleIcons = new SpriteRenderer[3];
                for (int s = 0; s < 3; s++)
                {
                    var iconGo = new GameObject("Icon_" + s);
                    iconGo.transform.SetParent(bubbleGo.transform, false);
                    iconGo.transform.localPosition = new Vector3((s - 1) * 0.40f, 0.16f, -0.04f);
                    iconGo.transform.localScale = new Vector3(0.42f, 0.42f, 0.42f);
                    var sr = iconGo.AddComponent<SpriteRenderer>();
                    sr.sortingOrder = 5;
                    bubbleIcons[s] = sr;
                }

                // Setup sample preview sprites matching reference image in editor
                if (i == 0) // Blue: Veg, Meat
                {
                    bubbleIcons[0].sprite = vegSprite;
                    bubbleIcons[1].sprite = meatSprite;
                    bubbleIcons[2].gameObject.SetActive(false);
                    bubbleIcons[0].transform.localPosition = new Vector3(-0.24f, 0.16f, -0.04f);
                    bubbleIcons[1].transform.localPosition = new Vector3(0.24f, 0.16f, -0.04f);
                }
                else if (i == 1) // Red: Cheese, Veg
                {
                    bubbleIcons[0].sprite = cheeseSprite;
                    bubbleIcons[1].sprite = vegSprite;
                    bubbleIcons[2].gameObject.SetActive(false);
                    bubbleIcons[0].transform.localPosition = new Vector3(-0.24f, 0.16f, -0.04f);
                    bubbleIcons[1].transform.localPosition = new Vector3(0.24f, 0.16f, -0.04f);
                }
                else if (i == 2) // Green: Meat
                {
                    bubbleIcons[0].sprite = meatSprite;
                    bubbleIcons[1].gameObject.SetActive(false);
                    bubbleIcons[2].gameObject.SetActive(false);
                    bubbleIcons[0].transform.localPosition = new Vector3(0f, 0.16f, -0.04f);
                }
                else // Purple: Veg, Cheese, Meat
                {
                    bubbleIcons[0].sprite = vegSprite;
                    bubbleIcons[1].sprite = cheeseSprite;
                    bubbleIcons[2].sprite = meatSprite;
                    bubbleIcons[0].transform.localPosition = new Vector3(-0.40f, 0.16f, -0.04f);
                    bubbleIcons[1].transform.localPosition = new Vector3(0f, 0.16f, -0.04f);
                    bubbleIcons[2].transform.localPosition = new Vector3(0.40f, 0.16f, -0.04f);
                }

                // Patience Bar
                var barBg = CreatePart("BarBg", bubbleGo.transform,
                    new Vector3(-0.16f, -0.25f, -0.02f), new Vector3(0.85f, 0.14f, 0.02f), darkWallMat);

                var barFillGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                barFillGo.name = "BarFill";
                barFillGo.transform.SetParent(barBg.transform, false);
                barFillGo.transform.localPosition = new Vector3(0f, 0f, -0.01f);
                barFillGo.transform.localScale = new Vector3(1f, 0.85f, 1.2f);
                SetMat(barFillGo, redTrashMat);
                Object.DestroyImmediate(barFillGo.GetComponent<Collider>());

                // Timer text (e.g. "28s", "42s", "19s", "36s")
                string[] sampleTimes = { "28s", "42s", "19s", "36s" };
                var timerTextGo = new GameObject("TimerText", typeof(TextMesh));
                timerTextGo.transform.SetParent(bubbleGo.transform, false);
                timerTextGo.transform.localPosition = new Vector3(0.42f, -0.25f, -0.04f);
                var timerTm = timerTextGo.GetComponent<TextMesh>();
                timerTm.font = customFont;
                var tmRend = timerTextGo.GetComponent<MeshRenderer>();
                if (tmRend != null && customFont != null) tmRend.sharedMaterial = customFont.material;
                timerTm.fontSize = 42;
                timerTm.characterSize = 0.055f;
                timerTm.alignment = TextAlignment.Left;
                timerTm.anchor = TextAnchor.MiddleLeft;
                timerTm.text = sampleTimes[i];
                timerTm.color = new Color(0.2f, 0.2f, 0.25f);

                // Wire up CustomerWindow serialized properties
                var so = new SerializedObject(win);
                so.FindProperty("_windowIndex").intValue = i;
                so.FindProperty("_statusVisual").objectReferenceValue = statusRenderer;
                so.FindProperty("_speechBubble").objectReferenceValue = bubbleGo;
                so.FindProperty("_customerChar").objectReferenceValue = custCharGo.transform;
                so.FindProperty("_barFill").objectReferenceValue = barFillGo.transform;
                so.FindProperty("_bubbleTimerText").objectReferenceValue = timerTm;
                so.FindProperty("_spriteVeg").objectReferenceValue = vegSprite;
                so.FindProperty("_spriteCheese").objectReferenceValue = cheeseSprite;
                so.FindProperty("_spriteMeat").objectReferenceValue = meatSprite;
                so.FindProperty("_customFont").objectReferenceValue = customFont;

                var iconsProp = so.FindProperty("_bubbleIcons");
                iconsProp.arraySize = 3;
                for (int s = 0; s < 3; s++)
                {
                    iconsProp.GetArrayElementAtIndex(s).objectReferenceValue = bubbleIcons[s];
                }
                so.ApplyModifiedProperties();
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            SaveScreenshot();
            Debug.Log("[RefineRestaurantView] Successfully refined restaurant diorama view & customer windows!");
        }

        private static GameObject CreateCornerPost(string name, Vector3 pos, Transform parent, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(0.50f, 2.50f, 0.50f);
            SetMat(go, mat);
            return go;
        }

        private static GameObject CreatePart(string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            SetMat(go, mat);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static void BuildChibiCustomer(Transform root, Material shirtMat, Material hairMat, int hairStyle, Material skinMat, Material darkMat, Material blushMat)
        {
            // Torso (colored shirt)
            var torso = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            torso.name = "Torso";
            torso.transform.SetParent(root, false);
            torso.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            torso.transform.localScale = new Vector3(0.44f, 0.22f, 0.44f);
            SetMat(torso, shirtMat);
            Object.DestroyImmediate(torso.GetComponent<Collider>());

            // Head (smooth skin sphere)
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(root, false);
            head.transform.localPosition = new Vector3(0f, 0.58f, 0f);
            head.transform.localScale = new Vector3(0.42f, 0.40f, 0.42f);
            SetMat(head, skinMat);
            Object.DestroyImmediate(head.GetComponent<Collider>());

            // Eyes (two cute dark spheres on the East-facing +X front of the head)
            CreateEye("Eye_L", head.transform, new Vector3(0.47f, 0.04f, 0.20f), darkMat, new Vector3(0.15f, 0.15f, 0.15f));
            CreateEye("Eye_R", head.transform, new Vector3(0.47f, 0.04f, -0.20f), darkMat, new Vector3(0.15f, 0.15f, 0.15f));

            // Nose dot (facing East +X)
            CreateEye("Nose", head.transform, new Vector3(0.50f, -0.06f, 0f), darkMat, new Vector3(0.08f, 0.08f, 0.08f));

            // Cute rosy blush cheeks
            CreateEye("Blush_L", head.transform, new Vector3(0.41f, -0.10f, 0.30f), blushMat, new Vector3(0.12f, 0.08f, 0.14f));
            CreateEye("Blush_R", head.transform, new Vector3(0.41f, -0.10f, -0.30f), blushMat, new Vector3(0.12f, 0.08f, 0.14f));

            // Hairstyle (kept towards the back and top, leaving face exposed)
            if (hairStyle == 1) // High round bun (Red customer)
            {
                var hairCap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                hairCap.name = "HairCap";
                hairCap.transform.SetParent(head.transform, false);
                hairCap.transform.localPosition = new Vector3(-0.08f, 0.08f, 0f);
                hairCap.transform.localScale = new Vector3(0.96f, 0.96f, 1.02f);
                SetMat(hairCap, hairMat);
                Object.DestroyImmediate(hairCap.GetComponent<Collider>());

                var bun = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bun.name = "Bun";
                bun.transform.SetParent(head.transform, false);
                bun.transform.localPosition = new Vector3(-0.08f, 0.54f, 0f);
                bun.transform.localScale = new Vector3(0.46f, 0.46f, 0.46f);
                SetMat(bun, hairMat);
                Object.DestroyImmediate(bun.GetComponent<Collider>());
            }
            else if (hairStyle == 2) // Blonde bob (Green customer)
            {
                var hairCap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                hairCap.name = "HairBob";
                hairCap.transform.SetParent(head.transform, false);
                hairCap.transform.localPosition = new Vector3(-0.08f, 0.08f, 0f);
                hairCap.transform.localScale = new Vector3(0.96f, 0.96f, 1.02f);
                SetMat(hairCap, hairMat);
                Object.DestroyImmediate(hairCap.GetComponent<Collider>());

                // Side bobs
                CreateEye("Bob_L", head.transform, new Vector3(0.02f, -0.10f, 0.45f), hairMat, new Vector3(0.35f, 0.42f, 0.20f));
                CreateEye("Bob_R", head.transform, new Vector3(0.02f, -0.10f, -0.45f), hairMat, new Vector3(0.35f, 0.42f, 0.20f));
            }
            else // Slick / parted dark hair (Blue and Purple customers)
            {
                var hairCap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                hairCap.name = "HairShort";
                hairCap.transform.SetParent(head.transform, false);
                hairCap.transform.localPosition = new Vector3(-0.10f, 0.10f, 0f);
                hairCap.transform.localScale = new Vector3(0.94f, 0.94f, 1.01f);
                SetMat(hairCap, hairMat);
                Object.DestroyImmediate(hairCap.GetComponent<Collider>());
            }

            // Arms & Hands resting naturally on counter sill facing East
            CreateArm("Arm_L", root, new Vector3(0.08f, 0.18f, 0.16f), new Vector3(-8f, 0f, -112f), shirtMat, skinMat);
            CreateArm("Arm_R", root, new Vector3(0.08f, 0.18f, -0.16f), new Vector3(8f, 0f, -112f), shirtMat, skinMat);
        }

        private static void CreateEye(string name, Transform parent, Vector3 localPos, Material mat, Vector3? scale = null)
        {
            var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.name = name;
            eye.transform.SetParent(parent, false);
            eye.transform.localPosition = localPos;
            eye.transform.localScale = scale ?? new Vector3(0.08f, 0.08f, 0.08f);
            SetMat(eye, mat);
            Object.DestroyImmediate(eye.GetComponent<Collider>());
        }

        private static void CreateArm(string name, Transform root, Vector3 localPos, Vector3 localEuler, Material sleeveMat, Material skinMat)
        {
            var armRoot = new GameObject(name);
            armRoot.transform.SetParent(root, false);
            armRoot.transform.localPosition = localPos;
            armRoot.transform.localRotation = Quaternion.Euler(localEuler);

            var sleeve = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            sleeve.name = "Sleeve";
            sleeve.transform.SetParent(armRoot.transform, false);
            sleeve.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            sleeve.transform.localScale = new Vector3(0.10f, 0.08f, 0.10f);
            SetMat(sleeve, sleeveMat);
            Object.DestroyImmediate(sleeve.GetComponent<Collider>());

            var hand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hand.name = "Hand";
            hand.transform.SetParent(armRoot.transform, false);
            hand.transform.localPosition = new Vector3(0f, 0.17f, 0f);
            hand.transform.localScale = new Vector3(0.11f, 0.10f, 0.11f);
            SetMat(hand, skinMat);
            Object.DestroyImmediate(hand.GetComponent<Collider>());
        }

        private static void SetMat(GameObject go, Material mat)
        {
            if (go == null || mat == null) return;
            var r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = mat;
        }
    }
}
