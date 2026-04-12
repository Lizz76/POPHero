using UnityEditor;
using UnityEngine;

namespace POPHero.Editor
{
    [CustomEditor(typeof(PopHeroPrototypeConfig))]
    public sealed class PopHeroPrototypeConfigEditor : UnityEditor.Editor
    {
        bool showArena = true;
        bool showBall = true;
        bool showAim = true;
        bool showPlayer = true;
        bool showBoard = true;
        bool showBlockRewards = true;
        bool showStickers = true;
        bool showMods = true;
        bool showShop = true;
        bool showIntermission = true;
        bool showEnemies = true;
        bool showDebug = true;
        bool boardVisualsMigratedThisFrame;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            boardVisualsMigratedThisFrame = false;

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Script", MonoScript.FromScriptableObject((PopHeroPrototypeConfig)target), typeof(PopHeroPrototypeConfig), false);
            }

            EditorGUILayout.Space(8f);

            DrawArenaSection(serializedObject.FindProperty("arena"));
            DrawDefaultSection("Ball", serializedObject.FindProperty("ball"), ref showBall);
            DrawDefaultSection("Aim", serializedObject.FindProperty("aim"), ref showAim);
            DrawDefaultSection("Player", serializedObject.FindProperty("player"), ref showPlayer);
            DrawBoardSection(serializedObject.FindProperty("board"));
            DrawDefaultSection("Block Rewards", serializedObject.FindProperty("blockRewards"), ref showBlockRewards);
            DrawDefaultSection("Stickers", serializedObject.FindProperty("stickers"), ref showStickers);
            DrawDefaultSection("Mods", serializedObject.FindProperty("mods"), ref showMods);
            DrawDefaultSection("Shop", serializedObject.FindProperty("shop"), ref showShop);
            DrawDefaultSection("Intermission", serializedObject.FindProperty("intermission"), ref showIntermission);
            DrawEnemiesSection(serializedObject.FindProperty("enemies"));
            DrawDefaultSection("Debug", serializedObject.FindProperty("debug"), ref showDebug);

            var changed = serializedObject.ApplyModifiedProperties();
            if (boardVisualsMigratedThisFrame || changed)
                EditorUtility.SetDirty(target);
            if (boardVisualsMigratedThisFrame)
                AssetDatabase.SaveAssetIfDirty(target);
        }

        void DrawArenaSection(SerializedProperty arena)
        {
            showArena = EditorGUILayout.BeginFoldoutHeaderGroup(showArena, "Arena");
            if (showArena)
            {
                EditorGUILayout.PropertyField(arena.FindPropertyRelative("boardCenter"));
                EditorGUILayout.PropertyField(arena.FindPropertyRelative("boardSize"));
                EditorGUILayout.PropertyField(arena.FindPropertyRelative("wallThickness"));
                EditorGUILayout.PropertyField(arena.FindPropertyRelative("wallStoneUnitLength"));
                EditorGUILayout.PropertyField(arena.FindPropertyRelative("wallPointSubdivisions"));
                EditorGUILayout.PropertyField(arena.FindPropertyRelative("wallStoneVisualGap"));
                EditorGUILayout.PropertyField(arena.FindPropertyRelative("wallStoneColliderOverlap"));
                EditorGUILayout.PropertyField(arena.FindPropertyRelative("wallStoneColorVariance"));
                EditorGUILayout.PropertyField(arena.FindPropertyRelative("topPanelHeight"));
                EditorGUILayout.PropertyField(arena.FindPropertyRelative("launchLineOffset"));
                EditorGUILayout.PropertyField(arena.FindPropertyRelative("bottomTriggerHeight"));
                EditorGUILayout.PropertyField(arena.FindPropertyRelative("cameraSize"));
                EditorGUILayout.PropertyField(arena.FindPropertyRelative("backgroundColor"));
                EditorGUILayout.PropertyField(arena.FindPropertyRelative("boardBackgroundColor"));
                EditorGUILayout.PropertyField(arena.FindPropertyRelative("boardFrameColor"));
                EditorGUILayout.PropertyField(arena.FindPropertyRelative("wallColor"));
                EditorGUILayout.PropertyField(arena.FindPropertyRelative("launchGuideColor"));
                EditorGUILayout.PropertyField(arena.FindPropertyRelative("safeZoneColor"));
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(4f);
        }

        void DrawBoardSection(SerializedProperty board)
        {
            showBoard = EditorGUILayout.BeginFoldoutHeaderGroup(showBoard, "Board");
            if (showBoard)
            {
                EditorGUILayout.PropertyField(board.FindPropertyRelative("blockSize"));
                EditorGUILayout.PropertyField(board.FindPropertyRelative("attackAddCount"));
                EditorGUILayout.PropertyField(board.FindPropertyRelative("attackMultiplyCount"));
                EditorGUILayout.PropertyField(board.FindPropertyRelative("shieldCount"));
                EditorGUILayout.PropertyField(board.FindPropertyRelative("startingVisibleBlockCount"));
                EditorGUILayout.PropertyField(board.FindPropertyRelative("visibleBlockIncreasePerRound"));
                EditorGUILayout.PropertyField(board.FindPropertyRelative("minRotationAngle"));
                EditorGUILayout.PropertyField(board.FindPropertyRelative("maxRotationAngle"));
                EditorGUILayout.PropertyField(board.FindPropertyRelative("rotationStep"));
                EditorGUILayout.PropertyField(board.FindPropertyRelative("keepLabelUpright"));
                EditorGUILayout.PropertyField(board.FindPropertyRelative("attackAddValues"), true);
                EditorGUILayout.PropertyField(board.FindPropertyRelative("attackMultiplyValues"), true);
                EditorGUILayout.PropertyField(board.FindPropertyRelative("shieldValues"), true);
                EditorGUILayout.PropertyField(board.FindPropertyRelative("sidePadding"));
                EditorGUILayout.PropertyField(board.FindPropertyRelative("topPadding"));
                EditorGUILayout.PropertyField(board.FindPropertyRelative("bottomPadding"));
                EditorGUILayout.PropertyField(board.FindPropertyRelative("launchSafeWidth"));
                EditorGUILayout.PropertyField(board.FindPropertyRelative("launchSafeHeight"));
                EditorGUILayout.PropertyField(board.FindPropertyRelative("perBlockPlacementTries"));
                EditorGUILayout.PropertyField(board.FindPropertyRelative("shuffleRetryCount"));

                EditorGUILayout.Space(6f);
                EditorGUILayout.HelpBox("Block art is configured as rarity backgrounds plus rarity-specific type icons. Each runtime block uses its rarity background together with the matching type icon for that rarity.", MessageType.Info);

                EditorGUILayout.LabelField("Block View Prefabs", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(board.FindPropertyRelative("worldBlockViewPrefab"), new GUIContent("World Block View Prefab"));
                EditorGUILayout.PropertyField(board.FindPropertyRelative("blockCellViewPrefab"), new GUIContent("Block Cell View Prefab"));

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Block Art Sprites", EditorStyles.boldLabel);
                DrawBoardVisuals(board.FindPropertyRelative("visuals"));
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(4f);
        }

        void DrawBoardVisuals(SerializedProperty visuals)
        {
            if (visuals == null)
                return;

            if (MigrateLegacyBoardVisuals(visuals))
            {
                boardVisualsMigratedThisFrame = true;
                EditorGUILayout.HelpBox("Legacy single-sprite block art was copied into the new rarity-based fields. You can now replace each rarity slot independently.", MessageType.Info);
            }

            DrawSpriteGroup("Rarity Backgrounds", visuals,
                ("White Background", "whiteBackgroundSprite"),
                ("Blue Background", "blueBackgroundSprite"),
                ("Purple Background", "purpleBackgroundSprite"),
                ("Gold Background", "goldBackgroundSprite"));

            DrawSpriteGroup("Attack Icons", visuals,
                ("White Attack Icon", "whiteAttackIconSprite"),
                ("Blue Attack Icon", "blueAttackIconSprite"),
                ("Purple Attack Icon", "purpleAttackIconSprite"),
                ("Gold Attack Icon", "goldAttackIconSprite"));

            DrawSpriteGroup("Shield Icons", visuals,
                ("White Shield Icon", "whiteShieldIconSprite"),
                ("Blue Shield Icon", "blueShieldIconSprite"),
                ("Purple Shield Icon", "purpleShieldIconSprite"),
                ("Gold Shield Icon", "goldShieldIconSprite"));

            DrawSpriteGroup("Multiplier Icons", visuals,
                ("White Multiplier Icon", "whiteMultiplierIconSprite"),
                ("Blue Multiplier Icon", "blueMultiplierIconSprite"),
                ("Purple Multiplier Icon", "purpleMultiplierIconSprite"),
                ("Gold Multiplier Icon", "goldMultiplierIconSprite"));
        }

        static void DrawSpriteGroup(string title, SerializedProperty root, params (string label, string path)[] fields)
        {
            EditorGUILayout.LabelField(title, EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;
            foreach (var field in fields)
            {
                var property = root.FindPropertyRelative(field.path);
                if (property != null)
                    EditorGUILayout.PropertyField(property, new GUIContent(field.label));
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(2f);
        }

        static bool MigrateLegacyBoardVisuals(SerializedProperty visuals)
        {
            var migratedFlag = visuals.FindPropertyRelative("legacyVisualsMigrated");
            if (migratedFlag != null && migratedFlag.boolValue)
                return false;

            var migrated = false;

            migrated |= CopyLegacySprite(visuals, "whiteBackgroundSprite", "backgroundSprite");
            migrated |= CopyLegacySprite(visuals, "blueBackgroundSprite", "backgroundSprite");
            migrated |= CopyLegacySprite(visuals, "purpleBackgroundSprite", "backgroundSprite");
            migrated |= CopyLegacySprite(visuals, "goldBackgroundSprite", "backgroundSprite");

            migrated |= CopyLegacySprite(visuals, "whiteAttackIconSprite", "attackIconSprite");
            migrated |= CopyLegacySprite(visuals, "blueAttackIconSprite", "attackIconSprite");
            migrated |= CopyLegacySprite(visuals, "purpleAttackIconSprite", "attackIconSprite");
            migrated |= CopyLegacySprite(visuals, "goldAttackIconSprite", "attackIconSprite");

            migrated |= CopyLegacySprite(visuals, "whiteShieldIconSprite", "shieldIconSprite");
            migrated |= CopyLegacySprite(visuals, "blueShieldIconSprite", "shieldIconSprite");
            migrated |= CopyLegacySprite(visuals, "purpleShieldIconSprite", "shieldIconSprite");
            migrated |= CopyLegacySprite(visuals, "goldShieldIconSprite", "shieldIconSprite");

            migrated |= CopyLegacySprite(visuals, "whiteMultiplierIconSprite", "multiplierIconSprite");
            migrated |= CopyLegacySprite(visuals, "blueMultiplierIconSprite", "multiplierIconSprite");
            migrated |= CopyLegacySprite(visuals, "purpleMultiplierIconSprite", "multiplierIconSprite");
            migrated |= CopyLegacySprite(visuals, "goldMultiplierIconSprite", "multiplierIconSprite");

            if (migrated && migratedFlag != null)
                migratedFlag.boolValue = true;

            return migrated;
        }

        static bool CopyLegacySprite(SerializedProperty root, string targetPath, string legacyPath)
        {
            var target = root.FindPropertyRelative(targetPath);
            var legacy = root.FindPropertyRelative(legacyPath);
            if (target == null || legacy == null)
                return false;
            if (target.objectReferenceValue != null || legacy.objectReferenceValue == null)
                return false;

            target.objectReferenceValue = legacy.objectReferenceValue;
            return true;
        }

        void DrawEnemiesSection(SerializedProperty enemies)
        {
            showEnemies = EditorGUILayout.BeginFoldoutHeaderGroup(showEnemies, "Enemies");
            if (showEnemies)
            {
                var templates = enemies.FindPropertyRelative("templates");
                EditorGUILayout.LabelField("Templates", EditorStyles.boldLabel);
                if (GUILayout.Button("Add Enemy Template"))
                    templates.InsertArrayElementAtIndex(templates.arraySize);

                EditorGUI.indentLevel++;
                for (var index = 0; index < templates.arraySize; index++)
                {
                    var element = templates.GetArrayElementAtIndex(index);
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Enemy {index + 1}", EditorStyles.boldLabel);
                    if (GUILayout.Button("Remove", GUILayout.Width(72f)))
                    {
                        templates.DeleteArrayElementAtIndex(index);
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        break;
                    }

                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("displayName"));
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("maxHp"));
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("rewardGold"));
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("rewardHeal"));
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("attackDamage"));
                    EditorGUILayout.EndVertical();
                }

                EditorGUI.indentLevel--;
                EditorGUILayout.HelpBox("Enemy accent colors are no longer the visual source of truth. Enemy-specific prefabs can take over later without changing this config layout again.", MessageType.None);
                EditorGUILayout.PropertyField(enemies.FindPropertyRelative("endlessHpGrowth"));
                EditorGUILayout.PropertyField(enemies.FindPropertyRelative("endlessGoldGrowth"));
                EditorGUILayout.PropertyField(enemies.FindPropertyRelative("endlessHealGrowth"));
                EditorGUILayout.PropertyField(enemies.FindPropertyRelative("endlessAttackGrowth"));
                EditorGUILayout.PropertyField(enemies.FindPropertyRelative("maxLaunchesPerEnemy"));
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(4f);
        }

        void DrawDefaultSection(string title, SerializedProperty property, ref bool expanded)
        {
            expanded = EditorGUILayout.BeginFoldoutHeaderGroup(expanded, title);
            if (expanded)
                EditorGUILayout.PropertyField(property, true);

            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(4f);
        }
    }
}
