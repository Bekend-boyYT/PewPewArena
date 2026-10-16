using System;
using System.Collections.Generic;
using UnityEngine;

namespace PewPewArena.UI
{
    /// <summary>
    /// Procedurally generated (anti-aliased, white, tintable) sprites for the Loadout screen:
    /// chamfered card frames, bay corner brackets, stat icons, button icons and the title brush stroke.
    /// Generated once at runtime and cached, so no extra texture assets are required.
    /// </summary>
    public static class LoadoutUISprites
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        // ---------- Frames ----------
        private const int CardTex = 64, CardChamfer = 18, CardBorder = 3;
        private const int BtnTex = 32, BtnChamfer = 8, BtnBorder = 2;

        /// <summary>Plain white sprite (required so Image.Type.Filled actually fills).</summary>
        public static Sprite White => Build("white", 8, 8, (x, y) => 1f, Vector4.zero, 1);

        /// <summary>Soft radial falloff used for the hover aura behind the weapon.</summary>
        public static Sprite RadialSoft => Build("radial_soft", 128, 64, (x, y) =>
        {
            float u = (x / 128f - 0.5f) * 2f, v = (y / 64f - 0.5f) * 2f;
            float d = Mathf.Clamp01(Mathf.Sqrt(u * u + v * v));
            float a = 1f - d;
            return a * a;
        }, Vector4.zero, 1);

        public static Sprite CardFill => Build("card_fill", CardTex, CardTex,
            (x, y) => Chamfer(x, y, CardTex, CardChamfer, 0f) ? 1f : 0f, new Vector4(22, 22, 22, 22));

        public static Sprite CardFrame => Build("card_frame", CardTex, CardTex,
            (x, y) => Chamfer(x, y, CardTex, CardChamfer, 0f) && !Chamfer(x, y, CardTex, CardChamfer, CardBorder) ? 1f : 0f,
            new Vector4(22, 22, 22, 22));

        public static Sprite ButtonFill => Build("btn_fill", BtnTex, BtnTex,
            (x, y) => Chamfer(x, y, BtnTex, BtnChamfer, 0f) ? 1f : 0f, new Vector4(11, 11, 11, 11));

        public static Sprite ButtonFrame => Build("btn_frame", BtnTex, BtnTex,
            (x, y) => Chamfer(x, y, BtnTex, BtnChamfer, 0f) && !Chamfer(x, y, BtnTex, BtnChamfer, BtnBorder) ? 1f : 0f,
            new Vector4(11, 11, 11, 11));

        /// <summary>Right triangle filling the top-left half (legs on top + left edges).</summary>
        public static Sprite CornerTriangle => Build("corner_tri", 32, 32, (x, y) => y >= x ? 1f : 0f, Vector4.zero);

        /// <summary>L-shaped corner brackets on all 4 corners (9-sliced, transparent centre).</summary>
        public static Sprite BayBrackets => Build("bay_brackets", 64, 64, (x, y) =>
        {
            const float t = 3f, len = 22f, s = 64f;
            bool nearX = x < len || x > s - len;
            bool nearY = y < len || y > s - len;
            bool vertical = (x < t || x > s - t) && nearY;
            bool horizontal = (y < t || y > s - t) && nearX;
            return vertical || horizontal ? 1f : 0f;
        }, new Vector4(24, 24, 24, 24));

        // ---------- Icons ----------
        public static Sprite IconDamage => Build("icon_damage", 64, 64, (x, y) =>
        {
            Vector2 p = new Vector2(x / 64f, y / 64f);
            return InPoly(p, Star(new Vector2(0.5f, 0.5f), 8, 0.48f, 0.22f)) ? 1f : 0f;
        }, Vector4.zero);

        public static Sprite IconFireRate => Build("icon_firerate", 64, 64, (x, y) =>
        {
            Vector2 p = new Vector2(x / 64f, y / 64f);
            Vector2 c = new Vector2(0.5f, 0.40f);
            bool ring = InArc(p, c, 0.31f, 0.45f, -25f, 205f);
            Vector2 tip = c + 0.32f * new Vector2(Mathf.Cos(55f * Mathf.Deg2Rad), Mathf.Sin(55f * Mathf.Deg2Rad));
            bool needle = InCapsule(p, c, tip, 0.055f);
            bool hub = (p - c).sqrMagnitude <= 0.095f * 0.095f;
            return ring || needle || hub ? 1f : 0f;
        }, Vector4.zero);

        public static Sprite IconCapacity => Build("icon_capacity", 64, 64, (x, y) =>
        {
            Vector2 p = new Vector2(x / 64f, y / 64f);
            for (int i = 0; i < 3; i++)
            {
                float cx = 0.19f + i * 0.31f;
                const float hw = 0.105f;
                var bullet = new[]
                {
                    new Vector2(cx - hw, 0.08f), new Vector2(cx + hw, 0.08f),
                    new Vector2(cx + hw, 0.62f), new Vector2(cx, 0.92f), new Vector2(cx - hw, 0.62f)
                };
                if (InPoly(p, bullet)) return 1f;
            }
            return 0f;
        }, Vector4.zero);

        public static Sprite IconRecoil => Build("icon_recoil", 64, 64, (x, y) =>
        {
            Vector2 p = new Vector2(x / 64f, y / 64f);
            bool arc = InArc(p, new Vector2(0.5f, 0.30f), 0.27f, 0.41f, 0f, 180f);
            var arrow = new[] { new Vector2(0.03f, 0.32f), new Vector2(0.28f, 0.32f), new Vector2(0.155f, 0.08f) };
            var arrow2 = new[] { new Vector2(0.72f, 0.32f), new Vector2(0.97f, 0.32f), new Vector2(0.845f, 0.08f) };
            return arc || InPoly(p, arrow) || InPoly(p, arrow2) ? 1f : 0f;
        }, Vector4.zero);

        public static Sprite IconCrosshair => Build("icon_crosshair", 64, 64, (x, y) =>
        {
            Vector2 p = new Vector2(x / 64f, y / 64f);
            Vector2 c = new Vector2(0.5f, 0.5f);
            float d = (p - c).magnitude;
            bool ring = d >= 0.26f && d <= 0.36f;
            bool vTick = Mathf.Abs(p.x - 0.5f) < 0.05f && (p.y < 0.26f || p.y > 0.74f);
            bool hTick = Mathf.Abs(p.y - 0.5f) < 0.05f && (p.x < 0.26f || p.x > 0.74f);
            bool dot = d <= 0.07f;
            return ring || vTick || hTick || dot ? 1f : 0f;
        }, Vector4.zero);

        public static Sprite IconSquare => Build("icon_square", 64, 64, (x, y) =>
        {
            float u = x / 64f, v = y / 64f;
            bool outer = u > 0.14f && u < 0.86f && v > 0.14f && v < 0.86f;
            bool inner = u > 0.28f && u < 0.72f && v > 0.28f && v < 0.72f;
            return outer && !inner ? 1f : 0f;
        }, Vector4.zero);

        /// <summary>Three slanted bars ("///") used as the weapon class tag glyph.</summary>
        public static Sprite TagSlashes => Build("tag_slashes", 96, 36, (x, y) =>
        {
            Vector2 p = new Vector2(x / 96f, y / 36f);
            for (int i = 0; i < 3; i++)
            {
                float x0 = 0.02f + i * 0.27f;
                var bar = new[]
                {
                    new Vector2(x0, 0.05f), new Vector2(x0 + 0.17f, 0.05f),
                    new Vector2(x0 + 0.36f, 0.95f), new Vector2(x0 + 0.19f, 0.95f)
                };
                if (InPoly(p, bar)) return 1f;
            }
            return 0f;
        }, Vector4.zero);

        /// <summary>Ragged dark ink brush stroke used behind the LOADOUT title.</summary>
        public static Sprite TitleBrush => Build("title_brush", 512, 128, (x, y) =>
        {
            float u = x / 512f, v = y / 128f;
            u -= (v - 0.5f) * 0.10f; // slight italic skew

            float left = 0.03f + Noise1(v * 7f + 1.3f) * 0.07f;
            float right = 0.97f - Noise1(v * 6f + 8.1f) * 0.10f;
            float top = 0.90f - Noise1(u * 9f + 2.7f) * 0.10f - u * 0.10f;
            float bottom = 0.12f + Noise1(u * 8f + 5.9f) * 0.10f + (1f - u) * 0.04f;
            if (u < left || u > right || v < bottom || v > top) return 0f;

            float a = 0.78f + 0.22f * Noise1(v * 46f + 3.3f);

            // Dry-brush fibre breakup towards the right tail
            float tail = Mathf.InverseLerp(right - 0.16f, right, u);
            if (tail > 0f && Noise1(v * 34f + u * 3f) < tail * 0.85f) return 0f;

            // Softer fibres on the left start
            float head = 1f - Mathf.InverseLerp(left, left + 0.05f, u);
            if (head > 0f && Noise1(v * 28f + 11f) < head * 0.7f) return 0f;

            return a;
        }, Vector4.zero, 2);

        // ---------- Builder ----------
        private static Sprite Build(string key, int w, int h, Func<float, float, float> coverage, Vector4 border, int ss = 4)
        {
            if (Cache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = "Loadout_" + key,
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

        // ---------- Shape helpers ----------
        /// <summary>Square with top-right and bottom-left corners cut at 45°, optionally inset.</summary>
        private static bool Chamfer(float x, float y, float size, float chamfer, float inset)
        {
            if (x < inset || y < inset || x > size - inset || y > size - inset) return false;
            float diag = chamfer + inset * 1.41421f;
            return x + y >= diag && (size - x) + (size - y) >= diag;
        }

        private static bool InPoly(Vector2 p, Vector2[] poly)
        {
            bool c = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if (((poly[i].y > p.y) != (poly[j].y > p.y)) &&
                    (p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x))
                    c = !c;
            }
            return c;
        }

        private static bool InCapsule(Vector2 p, Vector2 a, Vector2 b, float r)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return (p - (a + ab * t)).sqrMagnitude <= r * r;
        }

        private static bool InArc(Vector2 p, Vector2 c, float rIn, float rOut, float degFrom, float degTo)
        {
            Vector2 d = p - c;
            float m = d.magnitude;
            if (m < rIn || m > rOut) return false;
            float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            if (ang < degFrom) ang += 360f;
            return ang >= degFrom && ang <= degTo;
        }

        private static Vector2[] Star(Vector2 c, int points, float rOut, float rIn)
        {
            var pts = new Vector2[points * 2];
            for (int i = 0; i < pts.Length; i++)
            {
                float a = Mathf.PI * 0.5f + i * Mathf.PI / points;
                float r = (i % 2 == 0) ? rOut : rIn;
                pts[i] = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
            }
            return pts;
        }

        private static float Hash(float n)
        {
            float s = Mathf.Sin(n * 127.1f) * 43758.5453f;
            return s - Mathf.Floor(s);
        }

        private static float Noise1(float x)
        {
            float i = Mathf.Floor(x);
            float f = x - i;
            f = f * f * (3f - 2f * f);
            return Mathf.Lerp(Hash(i), Hash(i + 1f), f);
        }
    }
}
