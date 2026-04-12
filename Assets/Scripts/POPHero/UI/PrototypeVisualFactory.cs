using UnityEngine;
using TMPro;
using UnityEngine.TextCore.LowLevel;

namespace POPHero
{
    public static class PrototypeVisualFactory
    {
        static Sprite squareSprite;
        static Sprite circleSprite;
        static Sprite attackIconSprite;
        static Sprite shieldIconSprite;
        static Sprite multiplierIconSprite;
        static Font cachedCjkFont;
        static TMP_FontAsset cachedCjkTmpFont;

        public static Sprite SquareSprite => squareSprite ??= CreateSolidSprite(false);
        public static Sprite CircleSprite => circleSprite ??= CreateSolidSprite(true);
        public static Sprite AttackIconSprite => attackIconSprite ??= CreateBlockIconSprite(BlockIconShape.Attack);
        public static Sprite ShieldIconSprite => shieldIconSprite ??= CreateBlockIconSprite(BlockIconShape.Shield);
        public static Sprite MultiplierIconSprite => multiplierIconSprite ??= CreateBlockIconSprite(BlockIconShape.Multiplier);

        public static GameObject CreateSpriteObject(string objectName, Transform parent, Sprite sprite, Color color, int sortingOrder, Vector2 scale)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            return go;
        }

        public static TextMesh CreateTextObject(string objectName, Transform parent, string text, Color color, int sortingOrder, float characterSize, FontStyle fontStyle = FontStyle.Bold)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            var mesh = go.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.fontSize = 64;
            mesh.characterSize = characterSize;
            mesh.color = color;
            mesh.fontStyle = fontStyle;
            mesh.font = GetCjkRuntimeFont();
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = sortingOrder;
            if (mesh.font != null && mesh.font.material != null)
                renderer.sharedMaterial = mesh.font.material;
            return mesh;
        }

        public static Font GetCjkRuntimeFont()
        {
            if (cachedCjkFont != null)
                return cachedCjkFont;

            cachedCjkFont = Resources.Load<Font>("Fonts/VonwaonBitmap-16px");
            if (cachedCjkFont != null)
                return cachedCjkFont;

            try
            {
                cachedCjkFont = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "SimSun", "Arial" }, 64);
            }
            catch
            {
                try
                {
                    cachedCjkFont = Font.CreateDynamicFontFromOSFont("Arial", 64);
                }
                catch
                {
                    cachedCjkFont = null;
                }
            }

            return cachedCjkFont;
        }

        public static TMP_FontAsset GetCjkTmpFontAsset()
        {
            if (cachedCjkTmpFont != null)
                return cachedCjkTmpFont;

            var bundledAsset = Resources.Load<TMP_FontAsset>("Fonts/POPHero CJK SDF");
            if (IsUsableTmpFont(bundledAsset))
            {
                cachedCjkTmpFont = bundledAsset;
                return cachedCjkTmpFont;
            }

            var runtimeFont = GetCjkRuntimeFont();
            if (runtimeFont != null)
            {
                try
                {
                    cachedCjkTmpFont = TMP_FontAsset.CreateFontAsset(
                        runtimeFont,
                        64,
                        8,
                        GlyphRenderMode.SDFAA,
                        2048,
                        2048,
                        AtlasPopulationMode.Dynamic,
                        true);
                    cachedCjkTmpFont.name = "POPHero CJK Runtime";
                    cachedCjkTmpFont.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                    cachedCjkTmpFont.isMultiAtlasTexturesEnabled = true;
                    return cachedCjkTmpFont;
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[POPHero] Failed to create runtime CJK TMP font asset: {ex.Message}");
                }
            }

            cachedCjkTmpFont = TMP_Settings.defaultFontAsset;

            return cachedCjkTmpFont;
        }

        static bool IsUsableTmpFont(TMP_FontAsset fontAsset)
        {
            if (fontAsset == null)
                return false;

            var atlasTextures = fontAsset.atlasTextures;
            return atlasTextures != null && atlasTextures.Length > 0 && atlasTextures[0] != null;
        }

        static Sprite CreateSolidSprite(bool circle)
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = circle ? "POPHero_Circle" : "POPHero_Square"
            };

            var pixels = new Color[size * size];
            var radius = size * 0.46f;
            var center = (size - 1) * 0.5f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var index = y * size + x;
                    if (!circle)
                    {
                        pixels[index] = Color.white;
                        continue;
                    }

                    var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    pixels[index] = distance <= radius ? Color.white : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        static Sprite CreateBlockIconSprite(BlockIconShape shape)
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = $"POPHero_{shape}_Icon"
            };

            var pixels = new Color[size * size];
            var polygon = BuildPolygon(shape, size);
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var index = y * size + x;
                    var point = new Vector2(x + 0.5f, y + 0.5f);
                    pixels[index] = PointInPolygon(point, polygon) ? Color.white : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        static Vector2[] BuildPolygon(BlockIconShape shape, int size)
        {
            var max = size - 1f;
            return shape switch
            {
                BlockIconShape.Attack => new[]
                {
                    new Vector2(max * 0.5f, max * 0.08f),
                    new Vector2(max * 0.87f, max * 0.86f),
                    new Vector2(max * 0.13f, max * 0.86f)
                },
                BlockIconShape.Shield => new[]
                {
                    new Vector2(max * 0.5f, max * 0.06f),
                    new Vector2(max * 0.82f, max * 0.2f),
                    new Vector2(max * 0.76f, max * 0.62f),
                    new Vector2(max * 0.5f, max * 0.92f),
                    new Vector2(max * 0.24f, max * 0.62f),
                    new Vector2(max * 0.18f, max * 0.2f)
                },
                _ => new[]
                {
                    new Vector2(max * 0.5f, max * 0.06f),
                    new Vector2(max * 0.94f, max * 0.5f),
                    new Vector2(max * 0.5f, max * 0.94f),
                    new Vector2(max * 0.06f, max * 0.5f)
                }
            };
        }

        static bool PointInPolygon(Vector2 point, Vector2[] polygon)
        {
            var inside = false;
            for (var i = 0; i < polygon.Length; i++)
            {
                var j = (i + polygon.Length - 1) % polygon.Length;
                var current = polygon[i];
                var previous = polygon[j];
                var intersects = ((current.y > point.y) != (previous.y > point.y)) &&
                    (point.x < (previous.x - current.x) * (point.y - current.y) / Mathf.Max(0.0001f, previous.y - current.y) + current.x);
                if (intersects)
                    inside = !inside;
            }

            return inside;
        }

        enum BlockIconShape
        {
            Attack,
            Shield,
            Multiplier
        }
    }
}
