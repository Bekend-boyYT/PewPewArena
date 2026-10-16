using System;
using System.Collections.Generic;
using UnityEngine;

namespace PewPewArena.UI
{
    /// <summary>
    /// Procedurally generated (anti-aliased, white, tintable) sprites for the Join Game screen:
    /// - 4-corner chamfered card container frame and fill.
    /// - Rounded-chamfer input box frame and fill.
    /// - Glowing cyan button frame and fill.
    /// - Title horizontal laser beam accent with smooth gradient fade.
    /// - Ethereal ambient atmosphere radial glow for left and right edges.
    /// Generated once at runtime and cached.
    /// </summary>
    public static class JoinUISprites
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite White => Build("white", 8, 8, (x, y) => 1f, Vector4.zero, 1);

        // 4-corner chamfered card (64x64, chamfer=16, border=2.5)
        public static Sprite CardFill => Build("card_fill", 64, 64,
            (x, y) => Chamfer4(x, y, 64f, 16f, 0f) ? 1f : 0f, new Vector4(20, 20, 20, 20));

        public static Sprite CardFrame => Build("card_frame", 64, 64,
            (x, y) => Chamfer4(x, y, 64f, 16f, 0f) && !Chamfer4(x, y, 64f, 16f, 2.5f) ? 1f : 0f,
            new Vector4(20, 20, 20, 20));

        // Rounded chamfer for Input Field (48x48, radius=10, border=2)
        public static Sprite InputFill => Build("input_fill", 48, 48,
            (x, y) => RoundedRect(x, y, 48f, 10f, 0f) ? 1f : 0f, new Vector4(14, 14, 14, 14));

        public static Sprite InputFrame => Build("input_frame", 48, 48,
            (x, y) => RoundedRect(x, y, 48f, 10f, 0f) && !RoundedRect(x, y, 48f, 10f, 2f) ? 1f : 0f,
            new Vector4(14, 14, 14, 14));

        // Rounded Button Frame & Glow Frame
        public static Sprite ButtonFill => Build("btn_fill", 48, 48,
            (x, y) => RoundedRect(x, y, 48f, 10f, 0f) ? 1f : 0f, new Vector4(14, 14, 14, 14));

        public static Sprite ButtonFrame => Build("btn_frame", 48, 48,
            (x, y) => RoundedRect(x, y, 48f, 10f, 0f) && !RoundedRect(x, y, 48f, 10f, 2f) ? 1f : 0f,
            new Vector4(14, 14, 14, 14));

        public static Sprite ButtonGlowFrame => Build("btn_glow_frame", 64, 64,
            (x, y) => GlowFrame(x, y, 64f, 12f, 2.5f, 6f), new Vector4(20, 20, 20, 20));

        // Horizontal Laser Beam (128x16, fades outward towards u=0, bright core at u=1)
        public static Sprite LaserLine => Build("laser_line", 128, 16, (x, y) =>
        {
            float u = x / 128f;
            float v = y / 16f;
            float dv = Mathf.Abs(v - 0.5f) * 2f;
            float verticalCore = Mathf.Clamp01(1f - dv * dv);
            float horizontalFade = Mathf.SmoothStep(0f, 1f, u);
            return verticalCore * horizontalFade;
        }, Vector4.zero);

        // Soft Radial Atmosphere Glow (128x128)
        public static Sprite AtmosphereGlow => Build("atmosphere_glow", 128, 128, (x, y) =>
        {
            float u = (x / 128f - 0.5f) * 2f;
            float v = (y / 128f - 0.5f) * 2f;
            float d = Mathf.Clamp01(Mathf.Sqrt(u * u + v * v));
            float a = Mathf.Clamp01(1f - d);
            return a * a * a; // cubic falloff
        }, Vector4.zero);

        private static Sprite Build(string key, int w, int h, Func<float, float, float> coverage, Vector4 border, int ss = 4)
        {
            if (Cache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = "JoinUI_" + key,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };

            var px = new Color32[w * h];
            float inv = 1f / ss;
            float norm = 1f / (ss * ss);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float sum = 0f;
                    for (int sy = 0; sy < ss; sy++)
                        for (int sx = 0; sx < ss; sx++)
                            sum += coverage(x + (sx + 0.5f) * inv, y + (sy + 0.5f) * inv);
                    px[y * w + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(sum * norm) * 255f));
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, true);

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            sprite.name = key;
            sprite.hideFlags = HideFlags.DontSave;
            Cache[key] = sprite;
            return sprite;
        }

        private static bool Chamfer4(float x, float y, float size, float chamfer, float inset)
        {
            if (x < inset || y < inset || x > size - inset || y > size - inset) return false;
            float diag = chamfer + inset * 1.41421f;
            if (x + y < diag) return false;
            if (x + (size - y) < diag) return false;
            if ((size - x) + y < diag) return false;
            if ((size - x) + (size - y) < diag) return false;
            return true;
        }

        private static bool RoundedRect(float x, float y, float size, float radius, float inset)
        {
            if (x < inset || y < inset || x > size - inset || y > size - inset) return false;
            float r = radius - inset;
            if (r <= 0f) return true;
            if (x < inset + r && y < inset + r) { float dx = x - (inset + r), dy = y - (inset + r); return dx * dx + dy * dy <= r * r; }
            if (x > size - inset - r && y < inset + r) { float dx = x - (size - inset - r), dy = y - (inset + r); return dx * dx + dy * dy <= r * r; }
            if (x < inset + r && y > size - inset - r) { float dx = x - (inset + r), dy = y - (size - inset - r); return dx * dx + dy * dy <= r * r; }
            if (x > size - inset - r && y > size - inset - r) { float dx = x - (size - inset - r), dy = y - (size - inset - r); return dx * dx + dy * dy <= r * r; }
            return true;
        }

        private static float GlowFrame(float x, float y, float size, float radius, float border, float glow)
        {
            float pad = glow;
            float innerSize = size - 2f * pad;
            float rx = x - pad;
            float ry = y - pad;
            if (rx >= 0f && rx <= innerSize && ry >= 0f && ry <= innerSize)
            {
                bool inOuter = RoundedRect(rx, ry, innerSize, radius, 0f);
                bool inInner = RoundedRect(rx, ry, innerSize, radius, border);
                if (inInner) return 0f;
                if (inOuter) return 1f;
            }
            float dx = 0f;
            if (rx < 0f) dx = -rx;
            else if (rx > innerSize) dx = rx - innerSize;
            float dy = 0f;
            if (ry < 0f) dy = -ry;
            else if (ry > innerSize) dy = ry - innerSize;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            float g = Mathf.Clamp01(1f - dist / glow);
            return g * g;
        }
    }
}
