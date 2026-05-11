using UnityEngine;

namespace KNLVN.Game
{
    /// <summary>
    /// Generates procedural sprites at runtime.
    /// All sprites are white; colouring is applied via SpriteRenderer.color.
    /// </summary>
    public static class SpriteFactory
    {
        // ─── Cell backgrounds (pivot = bottom-left, fills a 1-unit cell) ─────

        public static Sprite CreateSquare()
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        public static Sprite CreateWallBrick(int res = 64)
        {
            var tex = MakeTex(res);
            int bH = res / 4, bW = res / 2;
            for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                int row = y / bH;
                int off = (row % 2 == 0) ? 0 : bW / 2;
                bool mortar = (y % bH < 2) || ((x + off) % bW < 2);
                tex.SetPixel(x, y, mortar ? new Color(0.55f, 0.55f, 0.55f) : Color.white);
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
        }

        // ─── Centred sprites (pivot = 0.5, 0.5) ──────────────────────────────

        public static Sprite CreateRoundedRect(int res = 64, float cornerRatio = 0.22f)
        {
            var tex = MakeTex(res);
            float r = cornerRatio * res, cx = res * .5f;
            for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                float qx = Mathf.Max(0, Mathf.Abs(x + .5f - cx) - (cx - r));
                float qy = Mathf.Max(0, Mathf.Abs(y + .5f - cx) - (cx - r));
                tex.SetPixel(x, y, qx * qx + qy * qy < r * r ? Color.white : Color.clear);
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, res, res), Vector2.one * .5f, res);
        }

        public static Sprite CreateStar(int res = 64, int pts = 5, float outer = .44f, float inner = .18f)
        {
            var tex = MakeTex(res);
            float cx = res * .5f;
            float sector = Mathf.PI * 2f / pts;
            for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                float dx = (x + .5f - cx) / cx;
                float dy = (y + .5f - cx) / cx;
                float ang = Mathf.Atan2(dy, dx) + Mathf.PI * .5f;
                float r   = Mathf.Sqrt(dx * dx + dy * dy);
                float a   = ((ang % sector) + sector) % sector;
                float t   = Mathf.Abs(a / (sector * .5f) - 1f);
                tex.SetPixel(x, y, r < Mathf.Lerp(inner, outer, t) ? Color.white : Color.clear);
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, res, res), Vector2.one * .5f, res);
        }

        public static Sprite CreateCircle(int res = 64)
        {
            var tex = MakeTex(res);
            float cx = res * .5f, r2 = (cx - .5f) * (cx - .5f);
            for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                float dx = x + .5f - cx, dy = y + .5f - cx;
                tex.SetPixel(x, y, dx * dx + dy * dy < r2 ? Color.white : Color.clear);
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, res, res), Vector2.one * .5f, res);
        }

        public static Sprite CreateDoor(int res = 64)
        {
            var tex = MakeTex(res);
            float cx = res * .5f;
            float archTopY = res * .5f, archR = res * .32f, doorHW = res * .22f;
            for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                float dx = x + .5f - cx, dy = y + .5f - archTopY;
                bool opening = y < archTopY
                    ? (dx * dx + dy * dy < archR * archR && Mathf.Abs(dx) < doorHW)
                    : Mathf.Abs(dx) < doorHW;
                tex.SetPixel(x, y, opening ? Color.clear : Color.white);
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
        }

        // ─── Facing-cell target marker (4 corner brackets) ───────────────────

        public static Sprite CreateTargetMarker(int res = 64)
        {
            var tex = MakeTex(res);
            int thick  = Mathf.Max(2, res / 22);   // line thickness
            int arm    = res / 4;                   // bracket arm length
            int margin = Mathf.Max(1, res / 12);    // inset from texture edge

            // Corners: (x0,y0) = anchor corner; (dx,dy) = direction into texture
            var corners = new (int x, int y, int dx, int dy)[]
            {
                (margin,       margin,       +1, +1),   // bottom-left
                (res-1-margin, margin,       -1, +1),   // bottom-right
                (margin,       res-1-margin, +1, -1),   // top-left
                (res-1-margin, res-1-margin, -1, -1),   // top-right
            };

            foreach (var (cx, cy, dx, dy) in corners)
            {
                // horizontal arm
                for (int i = 0; i < arm; i++)
                for (int t = 0; t < thick; t++)
                {
                    int px = cx + dx * i, py = cy + dy * t;
                    if (px >= 0 && px < res && py >= 0 && py < res)
                        tex.SetPixel(px, py, Color.white);
                }
                // vertical arm
                for (int i = 0; i < arm; i++)
                for (int t = 0; t < thick; t++)
                {
                    int px = cx + dx * t, py = cy + dy * i;
                    if (px >= 0 && px < res && py >= 0 && py < res)
                        tex.SetPixel(px, py, Color.white);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
        }



        /// <summary>
        /// Creates a warm wood-plank tile sprite designed for <see cref="SpriteDrawMode.Tiled"/>.
        /// pixelsPerUnit = <paramref name="res"/> so that one tile exactly fills one grid cell
        /// (assuming cellSize = 1 world unit).
        /// The texture uses <see cref="TextureWrapMode.Repeat"/> for seamless tiling.
        /// </summary>
        public static Sprite CreateFloorTile(int res = 64)
        {
            var tex = new Texture2D(res, res, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode   = TextureWrapMode.Repeat,
            };

            // ── Palette ────────────────────────────────────────────────────────
            // Two alternating plank columns for a staggered wood-floor look.
            var plankA  = new Color(0.78f, 0.63f, 0.46f);   // warm oak
            var plankB  = new Color(0.71f, 0.57f, 0.41f);   // slightly darker oak
            var grout   = new Color(0.52f, 0.43f, 0.32f);   // grout / seam line
            var grain   = new Color(0.82f, 0.68f, 0.50f);   // highlight grain stripe

            int plankW  = res / 2;   // two planks per tile width
            int groutPx = Mathf.Max(1, res / 32);  // 1-2 px seam

            for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                // Which plank column?
                int col    = x / plankW;
                bool isA   = (col % 2 == 0);

                // Vertical seam between planks
                int localX = x % plankW;
                bool vSeam = (localX < groutPx);

                // Horizontal seam at top of tile (staggered: col A seam at y=0, col B at y=res/2)
                int seamY  = isA ? 0 : res / 2;
                bool hSeam = (y % res == seamY || y % res == (seamY + groutPx - 1));
                hSeam = hSeam || (y < groutPx && isA) || (Mathf.Abs(y - res / 2) < groutPx && !isA);

                if (vSeam || hSeam)
                {
                    tex.SetPixel(x, y, grout);
                    continue;
                }

                // Base plank colour
                Color c = isA ? plankA : plankB;

                // Subtle vertical grain stripe
                float grainT = Mathf.Sin(localX * Mathf.PI / plankW);
                c = Color.Lerp(c, grain, grainT * 0.12f);

                // Tiny noise-like horizontal variation
                float wave = Mathf.Sin(y * 0.55f + col * 1.3f) * 0.04f;
                c = new Color(c.r + wave, c.g + wave * 0.8f, c.b + wave * 0.5f, 1f);

                tex.SetPixel(x, y, c);
            }

            tex.Apply();

            // pivot = centre, pixelsPerUnit = res → 1 world unit = 1 tile
            return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
        }

        // ─── Helper ───────────────────────────────────────────────────────────

        private static Texture2D MakeTex(int res)
        {
            var tex = new Texture2D(res, res, TextureFormat.RGBA32, false)
                { filterMode = FilterMode.Bilinear };
            tex.SetPixels32(new Color32[res * res]); // clear
            return tex;
        }
    }
}
