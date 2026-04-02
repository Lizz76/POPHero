using UnityEngine;

namespace POPHero
{
    public static class PrototypeVisualFactory
    {
        static Sprite squareSprite;
        static Sprite circleSprite;
        static Font cachedCjkFont;

        public static Sprite SquareSprite => squareSprite ??= CreateSolidSprite(false);
        public static Sprite CircleSprite => circleSprite ??= CreateSolidSprite(true);

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
    }
}
