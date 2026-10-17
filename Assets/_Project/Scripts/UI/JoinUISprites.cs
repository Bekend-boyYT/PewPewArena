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

        // 4-corner Chamfered Selector / Button Box (48x48, chamfer=8, border=1.8)
        public static Sprite ChamferBoxFill => Build("chamfer_box_fill", 48, 48,
            (x, y) => Chamfer4(x, y, 48f, 8f, 0f) ? 1f : 0f, new Vector4(12, 12, 12, 12));

        public static Sprite ChamferBoxFrame => Build("chamfer_box_frame", 48, 48,
            (x, y) => Chamfer4(x, y, 48f, 8f, 0f) && !Chamfer4(x, y, 48f, 8f, 1.8f) ? 1f : 0f,
            new Vector4(12, 12, 12, 12));

        public static Sprite ChamferGlowFrame => Build("chamfer_glow_frame", 64, 64,
            (x, y) => ChamferGlow(x, y, 64f, 9f, 2.2f, 7f), new Vector4(20, 20, 20, 20));

        // Room Code Box Tech Brackets (64x64, chamfer=14, double-line side brackets)
        public static Sprite RoomCodeBrackets => Build("room_code_brackets", 64, 64, (x, y) =>
        {
            bool outer = Chamfer4(x, y, 64f, 14f, 0f) && !Chamfer4(x, y, 64f, 14f, 2.2f);
            bool onEnds = x < 18f || x > 46f;
            if (outer && onEnds) return 1f;

            bool innerBracket = (x < 13f || x > 51f) && Chamfer4(x, y, 64f, 14f, 4.2f) && !Chamfer4(x, y, 64f, 14f, 5.6f);
            if (innerBracket) return 0.75f;
            return 0f;
        }, new Vector4(20, 20, 20, 20));

        // 4-corner Chamfered Icon Badge (40x40, chamfer=7, border=1.8)
        public static Sprite IconBadgeFill => Build("icon_badge_fill", 40, 40,
            (x, y) => Chamfer4(x, y, 40f, 7f, 0f) ? 1f : 0f, new Vector4(10, 10, 10, 10));

        public static Sprite IconBadgeFrame => Build("icon_badge_frame", 40, 40,
            (x, y) => Chamfer4(x, y, 40f, 7f, 0f) && !Chamfer4(x, y, 40f, 7f, 1.8f) ? 1f : 0f,
            new Vector4(10, 10, 10, 10));

        // Location Pin Icon (48x48)
        public static Sprite MapPinIcon => Build("map_pin_icon", 48, 48, (x, y) =>
        {
            float u = (x / 48f - 0.5f) * 2f;
            float v = (y / 48f - 0.5f) * 2f;

            // Inner circular cutout at top center
            float dHole = Mathf.Sqrt(u * u + (v - 0.22f) * (v - 0.22f));
            if (dHole < 0.18f) return 0f;

            // Top bulb circle
            if (dHole <= 0.46f) return 1f;

            // Tapered cone from bottom tip (0, -0.76) up to tangent of bulb (v = 0.06)
            if (v >= -0.76f && v <= 0.08f)
            {
                float t = Mathf.Clamp01((v + 0.76f) / (0.08f + 0.76f));
                float halfW = Mathf.Lerp(0.02f, 0.432f, t);
                if (Mathf.Abs(u) <= halfW) return 1f;
            }
            return 0f;
        }, Vector4.zero);

        // 8-Tooth Mechanical Gear / Cog Icon (48x48)
        public static Sprite GearIcon => Build("gear_icon", 48, 48, (x, y) =>
        {
            float u = (x / 48f - 0.5f) * 2f;
            float v = (y / 48f - 0.5f) * 2f;
            float r = Mathf.Sqrt(u * u + v * v);
            if (r < 0.25f) return 0f; // center hole

            float angle = Mathf.Atan2(v, u);
            float wave = Mathf.Cos(angle * 8f);
            float tooth = Mathf.Clamp01((wave - 0.15f) / 0.32f);
            float outerR = Mathf.Lerp(0.54f, 0.76f, tooth);
            return r <= outerR ? 1f : 0f;
        }, Vector4.zero);

        // Left & Right Chevrons (< and >) (32x32)
        public static Sprite ChevronLeft => Build("chevron_left", 32, 32, (x, y) =>
        {
            float u = x / 32f;
            float v = y / 32f;
            float d1 = DistToSegment(u, v, 0.64f, 0.80f, 0.32f, 0.50f);
            float d2 = DistToSegment(u, v, 0.32f, 0.50f, 0.64f, 0.20f);
            return Mathf.Min(d1, d2) <= 0.068f ? 1f : 0f;
        }, Vector4.zero);

        public static Sprite ChevronRight => Build("chevron_right", 32, 32, (x, y) =>
        {
            float u = x / 32f;
            float v = y / 32f;
            float d1 = DistToSegment(u, v, 0.36f, 0.80f, 0.68f, 0.50f);
            float d2 = DistToSegment(u, v, 0.68f, 0.50f, 0.36f, 0.20f);
            return Mathf.Min(d1, d2) <= 0.068f ? 1f : 0f;
        }, Vector4.zero);

        // Glowing Status Dot (32x32)
        public static Sprite StatusDot => Build("status_dot", 32, 32, (x, y) =>
        {
            float u = (x / 32f - 0.5f) * 2f;
            float v = (y / 32f - 0.5f) * 2f;
            float r = Mathf.Sqrt(u * u + v * v);
            if (r <= 0.45f) return 1f;
            float glow = Mathf.Clamp01(1f - (r - 0.45f) / 0.55f);
            return glow * glow * 0.60f;
        }, Vector4.zero);

        private static float DistToSegment(float px, float py, float ax, float ay, float bx, float by)
        {
            float vx = bx - ax, vy = by - ay;
            float wx = px - ax, wy = py - ay;
            float c1 = wx * vx + wy * vy;
            if (c1 <= 0f) return Mathf.Sqrt(wx * wx + wy * wy);
            float c2 = vx * vx + vy * vy;
            if (c2 <= c1) return Mathf.Sqrt((px - bx) * (px - bx) + (py - by) * (py - by));
            float b = c1 / c2;
            float pbx = ax + b * vx, pby = ay + b * vy;
            return Mathf.Sqrt((px - pbx) * (px - pbx) + (py - pby) * (py - pby));
        }

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

        private static float ChamferGlow(float x, float y, float size, float chamfer, float border, float glow)
        {
            float pad = glow;
            float innerSize = size - 2f * pad;
            float rx = x - pad;
            float ry = y - pad;
            if (rx >= 0f && rx <= innerSize && ry >= 0f && ry <= innerSize)
            {
                bool inOuter = Chamfer4(rx, ry, innerSize, chamfer, 0f);
                bool inInner = Chamfer4(rx, ry, innerSize, chamfer, border);
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
