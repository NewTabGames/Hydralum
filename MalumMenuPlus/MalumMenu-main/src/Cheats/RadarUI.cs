using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;

using System.Reflection;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;

namespace MalumMenu;

// --- Ported from NocturneRadar.cs ---
// On-screen radar/minimap. The map itself is drawn procedurally from the live
// ShipStatus room colliders (plus the embedded navigation graph for corridors),
// NOT from baked map images. This keeps the map and the player dots in the exact
// same coordinate space, so everything lines up, and it removes the previous
// dependency on loading .png resources at runtime (which rendered a blank map).

public sealed class RadarUI : MonoBehaviour
{
    private const float W = 236f, H = 212f, Pad = 2f, Head = 22f;

    private static GUIStyle _title;
    private static Texture2D _dot;
    private static readonly Dictionary<byte, List<(Vector2 w, float t)>> _trail = new Dictionary<byte, List<(Vector2, float)>>();

    // World-space bounds of the current map, used to project world positions onto the radar rect.
    private static Vector2 _min, _max;
    private static int _boundsMap = -999;

    private static float _sc = 1f, _al = 0.93f;
    private static bool _drag;
    private static Vector2 _dragOff;
    private static float _rx, _ry;

    // Exposed so the cursor-teleport can tell when the mouse is over the radar (and skip world-TP there).
    public static Rect WindowRect;
    // Door-button click state (for single vs double-click detection).
    private static float _lastDoorClickTime = -1f;
    private static int _lastDoorRoom = -1;

    public static void DrawGui()
    {
        if (!CheatToggles.radar)
            return;
        if (ShipStatus.Instance == null || PlayerControl.LocalPlayer == null)
            return;
        if (MeetingHud.Instance != null || ExileController.Instance != null)
            return;
        if (!Bounds())
            return;

        float userSc = Mathf.Clamp((CheatToggles.radarSize) / 100f, 0.6f, 1.8f);
        _sc = userSc * Mathf.Clamp(Screen.height / 1080f, 0.85f, 2.2f);
        _al = Mathf.Clamp((CheatToggles.radarOpacity) / 100f, 0.3f, 1f);
        float w = W * _sc, h = H * _sc;

        if (!_drag)
        {
            _rx = CheatToggles.radarX;
            _ry = CheatToggles.radarY;
        }
        _rx = Mathf.Clamp(_rx, 0f, Mathf.Max(0f, Screen.width - w));
        _ry = Mathf.Clamp(_ry, 0f, Mathf.Max(0f, Screen.height - h));

        var box = new Rect(_rx, _ry, w, h);
        float pad = Pad * _sc, head = Head * _sc;
        var inner = new Rect(box.x + pad, box.y + head + 2f * _sc, box.width - 2f * pad, box.height - head - 2f * _sc - pad);
        var lockRect = new Rect(box.xMax - head + 1f * _sc, box.y + 3f * _sc, head - 6f * _sc, head - 6f * _sc);
        bool locked = CheatToggles.radarLocked;
        WindowRect = box;

        Event e = Event.current;
        if (e != null)
        {
            if (e.type == EventType.MouseDown && e.button == 0 && lockRect.Contains(e.mousePosition))
            {
                CheatToggles.radarLocked = !locked;
                _drag = false;
                e.Use();
            }
            else if (e.type == EventType.MouseDown && e.button == 1 && inner.Contains(e.mousePosition))
            {
                // Right-click the map -> teleport to the nearest premade room location.
                TeleportFromRadar(e.mousePosition, inner);
                e.Use();
            }
            else if (e.type == EventType.MouseDown && e.button == 0 && CheatToggles.radarDoors && HandleDoorClick(e.mousePosition, inner))
            {
                // Consumed a door button (close / pin / unpin) so it doesn't start a window drag.
                e.Use();
            }
            else if (e.type == EventType.MouseDown && e.button == 0 && !locked && box.Contains(e.mousePosition))
            {
                _drag = true;
                _dragOff = new Vector2(e.mousePosition.x - box.x, e.mousePosition.y - box.y);
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && _drag)
            {
                _rx = Mathf.Clamp(e.mousePosition.x - _dragOff.x, 0f, Mathf.Max(0f, Screen.width - w));
                _ry = Mathf.Clamp(e.mousePosition.y - _dragOff.y, 0f, Mathf.Max(0f, Screen.height - h));
                e.Use();
            }
            else if (e.type == EventType.MouseUp && _drag)
            {
                _drag = false;
                CheatToggles.radarX = _rx;
                CheatToggles.radarY = _ry;
                e.Use();
            }
        }
        if (e == null || e.type != EventType.Repaint)
            return;

        EnsureStyle();
        NocturnePalette p = NocturneStyle.Current;

        NocturneStyle.FillRounded(box, A(p.Window, 0.93f), 12);
        NocturneStyle.FillRounded(new Rect(box.x, box.y, box.width, head + 6f * _sc), A(p.Accent, 0.10f), 12);
        NocturneStyle.StrokeRounded(box, A(p.Accent, 0.55f), 12, 1);
        NocturneStyle.Fill(new Rect(box.x, box.y + head + 3f * _sc, box.width, 1f), A(p.Accent, 0.35f));
        _title.fontSize = Mathf.Max(9, Mathf.RoundToInt(11f * _sc));
        _title.normal.textColor = A(p.Accent, 1f);
        GUI.Label(new Rect(box.x, box.y + 3f * _sc, box.width, head), "◎  " + MapName(), _title);

        bool lockHover = lockRect.Contains(e.mousePosition);
        Color lockCol = locked ? new Color(0.96f, 0.32f, 0.32f, 1f)
            : (lockHover ? new Color(0.75f, 0.80f, 0.86f, 1f) : new Color(0.5f, 0.55f, 0.62f, 1f));
        NocturneStyle.FillRounded(lockRect, A(new Color(0.10f, 0.10f, 0.13f), locked ? 0.9f : 0.55f), 4);
        NocturneStyle.StrokeRounded(lockRect, A(lockCol, 0.85f), 4, 1);
        DrawLock(lockRect, lockCol);

        NocturneStyle.FillRounded(inner, A(new Color(0.24f, 0.26f, 0.30f), 0.55f), 6);

        RadarNav.Graph g = RadarNav.Current();
        try
        {
            Skeleton(g, inner);
        }
        catch { }

        Players(inner);
        if (CheatToggles.radarBodies)
            Bodies(inner);
        if (CheatToggles.radarDoors)
            Doors(inner);
    }

    private static void DrawLock(Rect r, Color col)
    {
        float bw = r.width * 0.62f;
        float bx = r.center.x - bw * 0.5f;
        float bodyTop = r.y + r.height * 0.46f;
        float bodyH = r.yMax - 2f * _sc - bodyTop;
        if (bodyH < 1f)
            return;
        NocturneStyle.FillRounded(new Rect(bx, bodyTop, bw, bodyH), A(col, 1f), 2);

        float shW = bw * 0.72f;
        float shX = r.center.x - shW * 0.5f;
        float shTop = r.y + 2.5f * _sc;
        float th = Mathf.Max(1f, 1.2f * _sc);
        float shH = bodyTop - shTop;
        if (shH < 1f)
            return;
        NocturneStyle.Fill(new Rect(shX, shTop, th, shH), A(col, 1f));
        NocturneStyle.Fill(new Rect(shX + shW - th, shTop, th, shH), A(col, 1f));
        NocturneStyle.Fill(new Rect(shX, shTop, shW, th), A(col, 1f));
    }

    private static void EnsureStyle()
    {
        if (_title != null)
            return;
        _title = new GUIStyle(GUI.skin.label) { fontSize = 11, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, richText = true };
    }

    internal static int CurrentMapId()
    {
        try
        {
            GameOptionsManager gom = GameOptionsManager.Instance;
            if (gom != null && gom.CurrentGameOptions != null)
                return gom.CurrentGameOptions.MapId;
        }
        catch { }
        try
        {
            if (ShipStatus.Instance != null)
                return (int)ShipStatus.Instance.Type;
        }
        catch { }
        return -1;
    }

    private static string MapName()
    {
        switch (CurrentMapId())
        {
            case 0:
            case 3:
                return "The Skeld";
            case 1:
                return "Mira HQ";
            case 2:
                return "Polus";
            case 4:
                return "Airship";
            case 5:
                return "Fungle";
            default:
                return "Map";
        }
    }

    // Computes the world-space bounding box of the current map from its room colliders
    // (falling back to the nav graph), padded slightly. Returns false when nothing is known yet.
    private static bool Bounds()
    {
        int map = CurrentMapId();
        if (map == _boundsMap && _min.x <= _max.x)
            return true;

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);

        ShipStatus s = ShipStatus.Instance;
        if (s != null && s.AllRooms != null)
        {
            var rooms = s.AllRooms;
            for (int i = 0; i < rooms.Length; i++)
            {
                Collider2D c = rooms[i] != null ? rooms[i].roomArea : null;
                if (c == null)
                    continue;
                Bounds b = c.bounds;
                Grow(ref min, ref max, new Vector2(b.min.x, b.min.y));
                Grow(ref min, ref max, new Vector2(b.max.x, b.max.y));
            }
        }

        if (min.x > max.x)
        {
            RadarNav.Graph g = RadarNav.Current();
            if (g != null && g.Pos.Length > 0)
            {
                foreach (Vector2 v in g.Pos)
                    Grow(ref min, ref max, v);
            }
            else
                return false;
        }

        if (min.x > max.x)
            return false;
        Vector2 m = (max - min) * 0.015f + Vector2.one * 0.3f;
        _min = min - m;
        _max = max + m;
        _boundsMap = map;
        _trail.Clear();
        return true;
    }

    private static void Grow(ref Vector2 min, ref Vector2 max, Vector2 v)
    {
        if (v.x < min.x)
            min.x = v.x;
        if (v.y < min.y)
            min.y = v.y;
        if (v.x > max.x)
            max.x = v.x;
        if (v.y > max.y)
            max.y = v.y;
    }

    // Projects a world position into the radar rect using the shared map bounds.
    private static Vector2 Map(Vector2 w, Rect r)
    {
        float tx = (w.x - _min.x) / Mathf.Max(0.01f, _max.x - _min.x);
        float ty = (w.y - _min.y) / Mathf.Max(0.01f, _max.y - _min.y);
        return new Vector2(r.x + tx * r.width, r.y + (1f - ty) * r.height);
    }

    private static Vector2[] _skel;
    private static Color32[] _skelPx;
    private static Texture2D _skelTex;
    private static GUIStyle _skelStyle;
    private static int _skelMap = -999, _skelW, _skelH;
    private static float _skelAt;

    private static void Skeleton(RadarNav.Graph g, Rect r)
    {
        int w = Mathf.Clamp(Mathf.RoundToInt(r.width), 1, 1024);
        int h = Mathf.Clamp(Mathf.RoundToInt(r.height), 1, 1024);
        int map = CurrentMapId();

        bool dirty = _skelTex == null || map != _skelMap;
        bool resized = w != _skelW || h != _skelH;
        if (dirty || (resized && Time.unscaledTime - _skelAt > 0.1f))
        {
            BuildSkelTex(g, w, h);
            _skelMap = map;
            _skelW = w;
            _skelH = h;
            _skelAt = Time.unscaledTime;
        }
        if (_skelTex == null)
            return;

        if (_skelStyle == null)
            _skelStyle = new GUIStyle();
        _skelStyle.normal.background = _skelTex;
        Color prev = GUI.color;
        GUI.color = A(new Color(0.80f, 0.85f, 0.96f), 0.55f);
        GUI.Box(r, GUIContent.none, _skelStyle);
        GUI.color = prev;
    }

    private static void BuildSkelTex(RadarNav.Graph g, int w, int h)
    {
        if (_skelTex == null || _skelTex.width != w || _skelTex.height != h)
        {
            if (_skelTex != null)
                UnityEngine.Object.Destroy(_skelTex);
            _skelTex = new Texture2D(w, h, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            _skelPx = new Color32[w * h];
        }

        PaintMap(g, w, h);
        if (FitToContent(w, h))
            PaintMap(g, w, h);

        _skelTex.SetPixels32(_skelPx);
        _skelTex.Apply();
    }

    private static void PaintMap(RadarNav.Graph g, int w, int h)
    {
        System.Array.Clear(_skelPx, 0, _skelPx.Length);

        Color32 fillCol = new Color32(255, 255, 255, 120);
        Color32 edgeCol = new Color32(255, 255, 255, 255);
        Color32 corrCol = new Color32(255, 255, 255, 150);
        AddRoomFills(_skelPx, w, h, fillCol, edgeCol);

        // Corridors (optional): connect nav-graph nodes if a graph is available.
        if (g != null && g.Pos != null && g.Pos.Length > 0)
        {
            int n = g.Pos.Length;
            if (_skel == null || _skel.Length < n)
                _skel = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                float tx = (g.Pos[i].x - _min.x) / Mathf.Max(0.01f, _max.x - _min.x);
                float ty = (g.Pos[i].y - _min.y) / Mathf.Max(0.01f, _max.y - _min.y);
                _skel[i] = new Vector2(tx * w, ty * h);
            }

            int thick = Mathf.Max(2, Mathf.RoundToInt(2.4f * _sc));
            for (int i = 0; i < n; i++)
            {
                int[] nbs = g.Adj[i];
                if (nbs == null)
                    continue;
                foreach (int nb in nbs)
                {
                    if (nb <= i || nb >= n)
                        continue;
                    Stamp(_skelPx, w, h, _skel[i], _skel[nb], thick, corrCol);
                }
            }
        }
    }

    // Re-centres the map bounds tightly around whatever was actually painted, so the map
    // fills the radar rect instead of floating in a corner. Returns true if bounds changed.
    private static bool FitToContent(int w, int h)
    {
        int minX = w, minY = h, maxX = -1, maxY = -1;
        for (int y = 0; y < h; y++)
        {
            int row = y * w;
            for (int x = 0; x < w; x++)
            {
                if (_skelPx[row + x].a == 0)
                    continue;
                if (x < minX)
                    minX = x;
                if (x > maxX)
                    maxX = x;
                if (y < minY)
                    minY = y;
                if (y > maxY)
                    maxY = y;
            }
        }
        if (maxX < minX || maxY < minY)
            return false;

        float mL = minX / (float)w, mR = 1f - (maxX + 1) / (float)w;
        float mB = minY / (float)h, mT = 1f - (maxY + 1) / (float)h;
        if (mL < 0.02f && mR < 0.02f && mB < 0.02f && mT < 0.02f)
            return false;

        float rx = Mathf.Max(0.01f, _max.x - _min.x), ry = Mathf.Max(0.01f, _max.y - _min.y);
        Vector2 nmin = new Vector2(_min.x + minX / (float)w * rx, _min.y + minY / (float)h * ry);
        Vector2 nmax = new Vector2(_min.x + (maxX + 1) / (float)w * rx, _min.y + (maxY + 1) / (float)h * ry);
        Vector2 pad = (nmax - nmin) * 0.008f;
        _min = nmin - pad;
        _max = nmax + pad;
        return true;
    }

    private static void Stamp(Color32[] px, int w, int h, Vector2 a, Vector2 b, int thick, Color32 col)
    {
        float dx = b.x - a.x, dy = b.y - a.y;
        float len = Mathf.Sqrt(dx * dx + dy * dy);
        if (len < 0.5f)
            return;
        int steps = Mathf.CeilToInt(len);
        float sx = dx / steps, sy = dy / steps;
        int half = thick / 2;
        for (int s = 0; s <= steps; s++)
        {
            int cx = Mathf.RoundToInt(a.x + sx * s);
            int cy = Mathf.RoundToInt(a.y + sy * s);
            for (int oy = -half; oy <= half; oy++)
            {
                int y = cy + oy;
                if (y < 0 || y >= h)
                    continue;
                int row = y * w;
                for (int ox = -half; ox <= half; ox++)
                {
                    int x = cx + ox;
                    if (x < 0 || x >= w)
                        continue;
                    px[row + x] = col;
                }
            }
        }
    }

    private static readonly List<Vector2> _fpoly = new List<Vector2>(48);

    private static void AddRoomFills(Color32[] px, int w, int h, Color32 fill, Color32 edge)
    {
        ShipStatus s = ShipStatus.Instance;
        if (s == null || s.AllRooms == null)
            return;
        var rooms = s.AllRooms;
        for (int ri = 0; ri < rooms.Length; ri++)
        {
            PlainShipRoom room = rooms[ri];
            Collider2D c = room != null ? room.roomArea : null;
            if (c == null)
                continue;
            try
            {
                FillCollider(c, px, w, h, fill, edge);
            }
            catch { }
        }
    }

    private static void FillCollider(Collider2D c, Color32[] px, int w, int h, Color32 fill, Color32 edge)
    {
        Transform t = c.transform;

        PolygonCollider2D poly = c.TryCast<PolygonCollider2D>();
        if (poly != null)
        {
            Vector2 off = poly.offset;
            var pts = poly.points;
            if (pts == null || pts.Length < 3)
                return;
            _fpoly.Clear();
            for (int i = 0; i < pts.Length; i++)
            {
                Vector3 wp = t.TransformPoint(new Vector3(pts[i].x + off.x, pts[i].y + off.y, 0f));
                _fpoly.Add(WorldToPix(new Vector2(wp.x, wp.y), w, h));
            }
            FillPoly(px, w, h, _fpoly, fill);
            Outline(px, w, h, edge);
            return;
        }

        BoxCollider2D box = c.TryCast<BoxCollider2D>();
        if (box != null)
        {
            Vector2 hs = box.size * 0.5f;
            Vector2 off = box.offset;
            _fpoly.Clear();
            _fpoly.Add(WorldToPix(Corner(t, off.x - hs.x, off.y - hs.y), w, h));
            _fpoly.Add(WorldToPix(Corner(t, off.x + hs.x, off.y - hs.y), w, h));
            _fpoly.Add(WorldToPix(Corner(t, off.x + hs.x, off.y + hs.y), w, h));
            _fpoly.Add(WorldToPix(Corner(t, off.x - hs.x, off.y + hs.y), w, h));
            FillPoly(px, w, h, _fpoly, fill);
            Outline(px, w, h, edge);
        }
    }

    private static void Outline(Color32[] px, int w, int h, Color32 edge)
    {
        int et = Mathf.Max(1, Mathf.RoundToInt(1.3f * _sc));
        for (int i = 0; i < _fpoly.Count; i++)
            Stamp(px, w, h, _fpoly[i], _fpoly[(i + 1) % _fpoly.Count], et, edge);
    }

    private static Vector2 Corner(Transform t, float x, float y)
    {
        Vector3 wp = t.TransformPoint(new Vector3(x, y, 0f));
        return new Vector2(wp.x, wp.y);
    }

    private static Vector2 WorldToPix(Vector2 world, int w, int h)
    {
        float tx = (world.x - _min.x) / Mathf.Max(0.01f, _max.x - _min.x);
        float ty = (world.y - _min.y) / Mathf.Max(0.01f, _max.y - _min.y);
        return new Vector2(tx * w, ty * h);
    }

    private static readonly List<float> _fx = new List<float>(16);

    private static void FillPoly(Color32[] px, int w, int h, List<Vector2> poly, Color32 col)
    {
        int n = poly.Count;
        if (n < 3)
            return;

        float minY = float.MaxValue, maxY = float.MinValue;
        for (int i = 0; i < n; i++)
        {
            if (poly[i].y < minY)
                minY = poly[i].y;
            if (poly[i].y > maxY)
                maxY = poly[i].y;
        }
        int y0 = Mathf.Clamp(Mathf.FloorToInt(minY), 0, h - 1);
        int y1 = Mathf.Clamp(Mathf.CeilToInt(maxY), 0, h - 1);

        for (int y = y0; y <= y1; y++)
        {
            float yc = y + 0.5f;
            _fx.Clear();
            for (int i = 0; i < n; i++)
            {
                Vector2 a = poly[i], b = poly[(i + 1) % n];
                if ((a.y <= yc && b.y > yc) || (b.y <= yc && a.y > yc))
                    _fx.Add(a.x + (yc - a.y) / (b.y - a.y) * (b.x - a.x));
            }
            if (_fx.Count < 2)
                continue;
            _fx.Sort();
            int row = y * w;
            for (int i = 0; i + 1 < _fx.Count; i += 2)
            {
                int xa = Mathf.Clamp(Mathf.RoundToInt(_fx[i]), 0, w - 1);
                int xb = Mathf.Clamp(Mathf.RoundToInt(_fx[i + 1]), 0, w - 1);
                for (int x = xa; x <= xb; x++)
                    px[row + x] = col;
            }
        }
    }

    private static void Players(Rect r)
    {
        try
        {
            PlayerControl me = PlayerControl.LocalPlayer;
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f);
            foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
            {
                if (pc == null || pc.Data == null)
                    continue;
                bool dead = pc.Data.IsDead;
                if (dead && !CheatToggles.radarGhosts && pc != me)
                    continue; // "Ghosts on Radar" disabled: hide other dead players, but keep your own dot
                Vector2 w = pc.GetTruePosition();
                Vector2 sp = Map(w, r);
                Color col = PlayerColor(pc);

                if (!dead)
                {
                    Track(pc.PlayerId, w);
                    Trail(pc.PlayerId, r, col);
                }
                else
                    col.a = 0.4f;

                if (pc == me)
                    DrawDot(sp, (15f + pulse * 5f) * _sc, A(NocturneStyle.Current.Accent, 0.22f));
                float d = (pc == me ? 11f : 8.5f) * _sc;
                DrawDot(sp, d + 3f * _sc, A(Color.black, 0.6f));
                DrawDot(sp, d, A(col, col.a));
            }
        }
        catch { }
    }

    private static void Track(byte id, Vector2 w)
    {
        if (!_trail.TryGetValue(id, out var list))
        {
            list = new List<(Vector2, float)>();
            _trail[id] = list;
        }
        float now = Time.unscaledTime;
        if (list.Count == 0 || now - list[list.Count - 1].t > 0.06f)
            list.Add((w, now));
        while (list.Count > 0 && now - list[0].t > 0.5f)
            list.RemoveAt(0);
    }

    private static void Trail(byte id, Rect r, Color col)
    {
        if (!_trail.TryGetValue(id, out var list) || list.Count < 2)
            return;
        for (int i = 1; i < list.Count; i++)
            Line(Map(list[i - 1].w, r), Map(list[i].w, r), A(col, (float)i / list.Count * 0.45f * col.a), 2f * _sc);
    }

    private static Texture2D DotTex()
    {
        if (_dot != null)
            return _dot;
        int s = 32;
        _dot = new Texture2D(s, s, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var px = new Color32[s * s];
        float rad = s / 2f - 1f, c = s / 2f;
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float dx = x - c + 0.5f, dy = y - c + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                px[y * s + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01((rad - dist) / 1.5f) * 255f));
            }
        _dot.SetPixels32(px);
        _dot.Apply();
        return _dot;
    }

    private static GUIStyle _dotStyle;

    private static void DrawDot(Vector2 c, float size, Color col)
    {
        if (_dotStyle == null)
            _dotStyle = new GUIStyle();
        _dotStyle.normal.background = DotTex();
        Color prev = GUI.color;
        GUI.color = new Color(col.r, col.g, col.b, col.a * prev.a);
        GUI.Box(new Rect(c.x - size / 2f, c.y - size / 2f, size, size), GUIContent.none, _dotStyle);
        GUI.color = prev;
    }

    private static DeadBody[] _bodies;
    private static float _bodiesAt = -99f;

    private static void Bodies(Rect r)
    {
        try
        {
            if (_bodies == null || Time.unscaledTime - _bodiesAt > 0.5f)
            {
                _bodiesAt = Time.unscaledTime;
                _bodies = UnityEngine.Object.FindObjectsOfType<DeadBody>();
            }
            if (_bodies == null)
                return;
            for (int i = 0; i < _bodies.Length; i++)
            {
                DeadBody b = _bodies[i];
                if (b == null)
                    continue;
                Vector2 sp = Map(b.TruePosition, r);
                DrawDot(sp, 11f * _sc, A(new Color(0.6f, 0.1f, 0.1f, 1f), 0.85f));
                DrawDot(sp, 9f * _sc, A(new Color(0.9f, 0.25f, 0.25f, 1f), 0.95f));
                float k = 3f * _sc;
                Line(new Vector2(sp.x - k, sp.y - k), new Vector2(sp.x + k, sp.y + k), A(Color.white, 1f), 1.6f * _sc);
                Line(new Vector2(sp.x - k, sp.y + k), new Vector2(sp.x + k, sp.y - k), A(Color.white, 1f), 1.6f * _sc);
            }
        }
        catch { }
    }

    // Draws a square marker at each door; red when that room is pinned (kept shut).
    private static void Doors(Rect r)
    {
        ShipStatus s = ShipStatus.Instance;
        if (s == null || s.AllDoors == null) return;

        foreach (OpenableDoor d in s.AllDoors)
        {
            if (d == null) continue;
            Vector2 sp = Map(d.transform.position, r);
            bool pinned = NocturneDoors.IsPinned((int)d.Room);
            Color col = pinned ? new Color(0.96f, 0.28f, 0.28f) : new Color(0.87f, 0.80f, 0.50f);
            float half = (pinned ? 4.5f : 4f) * _sc;
            NocturneStyle.Fill(new Rect(sp.x - half - 1f, sp.y - half - 1f, half * 2f + 2f, half * 2f + 2f), A(Color.black, 0.75f));
            NocturneStyle.Fill(new Rect(sp.x - half, sp.y - half, half * 2f, half * 2f), A(col, 1f));
        }
    }

    // Single click = shut once, double click = pin (loops shut via Tick), click while pinned = unpin.
    private static bool HandleDoorClick(Vector2 mouse, Rect r)
    {
        ShipStatus s = ShipStatus.Instance;
        if (s == null || s.AllDoors == null) return false;

        float hitR = 9f * _sc;
        OpenableDoor hit = null;
        float best = hitR * hitR;

        foreach (OpenableDoor d in s.AllDoors)
        {
            if (d == null) continue;
            Vector2 sp = Map(d.transform.position, r);
            float dist = (sp - mouse).sqrMagnitude;
            if (dist < best) { best = dist; hit = d; }
        }

        if (hit == null) return false;

        int room = (int)hit.Room;
        float now = Time.unscaledTime;

        if (NocturneDoors.IsPinned(room))
            NocturneDoors.TogglePin(room); // click while pinned -> unpin
        else if (now - _lastDoorClickTime < 0.35f && _lastDoorRoom == room)
            NocturneDoors.TogglePin(room); // double click -> pin
        else
            NocturneDoors.CloseOne(room);  // single click -> shut once

        _lastDoorClickTime = now;
        _lastDoorRoom = room;
        return true;
    }

    // Right-click the radar: invert the projection to an approximate world position, then teleport to the
    // nearest premade room location so the radar/world misalignment doesn't matter.
    private static void TeleportFromRadar(Vector2 mouse, Rect r)
    {
        if (PlayerControl.LocalPlayer == null) return;

        float tx = Mathf.Clamp01((mouse.x - r.x) / Mathf.Max(1f, r.width));
        float ty = Mathf.Clamp01((mouse.y - r.y) / Mathf.Max(1f, r.height));
        Vector2 world = new Vector2(_min.x + tx * (_max.x - _min.x), _min.y + (1f - ty) * (_max.y - _min.y));

        // Teleport straight to the exact point clicked (like SickoMenu) — hallways and other unnamed
        // spots included — instead of snapping to the nearest named room/location.
        MalumTeleport.TeleportTo(world);
    }

    private static Color PlayerColor(PlayerControl pc)
    {
        try
        {
            int id = pc.Data.DefaultOutfit != null ? pc.Data.DefaultOutfit.ColorId : 0;
            if (Palette.PlayerColors != null && id >= 0 && id < Palette.PlayerColors.Length)
            {
                Color32 c = Palette.PlayerColors[id];
                return new Color(c.r / 255f, c.g / 255f, c.b / 255f, 1f);
            }
        }
        catch { }
        return Color.white;
    }

    private static Color A(Color c, float a) => new Color(c.r, c.g, c.b, a * _al);

    private static void Line(Vector2 a, Vector2 b, Color col, float w)
    {
        float dx = b.x - a.x, dy = b.y - a.y;
        float len = Mathf.Sqrt(dx * dx + dy * dy);
        if (len < 1f)
            return;
        Matrix4x4 m = GUI.matrix;
        GUIUtility.RotateAroundPivot(Mathf.Atan2(dy, dx) * Mathf.Rad2Deg, a);
        NocturneStyle.Fill(new Rect(a.x, a.y - w * 0.5f, len, w), col);
        GUI.matrix = m;
    }
}

// --- Ported from NocturneNav.cs (graph loading only, no pathfinding) ---
// Loads the embedded room-adjacency graph used to draw corridors on the radar.
// Pure managed string parsing of the "*_onx.json" resources; no IL2CPP interop.

internal static class RadarNav
{
    internal sealed class Graph
    {
        public Vector2[] Pos;
        public int[][] Adj;
    }

    private static Graph _g;
    private static int _gMap = -999;

    private static string Res(int map)
    {
        switch (map)
        {
            case 0:
                return "MalumMenu.grid.skeld_onx.json";
            case 1:
                return "MalumMenu.grid.mira_onx.json";
            case 2:
                return "MalumMenu.grid.polus_onx.json";
            case 3:
                return "MalumMenu.grid.skeld_onx.json";
            case 4:
                return "MalumMenu.grid.airship_onx.json";
            case 5:
                return "MalumMenu.grid.fungle_onx.json";
            default:
                return null;
        }
    }

    internal static Graph Current()
    {
        int map = RadarUI.CurrentMapId();
        if (map < 0)
            return null;
        if (_g != null && _gMap == map)
            return _g;

        string res = Res(map);
        if (res == null)
            return null;
        try
        {
            _g = Load(res);
            _gMap = map;
        }
        catch
        {
            _g = null;
        }
        return _g;
    }

    private static Graph Load(string res)
    {
        using Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(res);
        if (s == null)
            return null;
        using StreamReader r = new StreamReader(s);
        return Parse(r.ReadToEnd());
    }

    private static Graph Parse(string json)
    {
        Section(json, "\"hubs\"", out List<Vector2> mp, out List<int[]> ma);
        if (mp == null || mp.Count == 0)
            return null;

        int mainN = mp.Count;
        var pos = new List<Vector2>(mp);
        var adj = new List<List<int>>(mainN);
        for (int i = 0; i < mainN; i++)
            adj.Add(new List<int>(ma[i] ?? Array.Empty<int>()));

        Section(json, "\"spurs\"", out List<Vector2> sp, out List<int[]> sa);
        if (sp != null)
        {
            for (int j = 0; j < sp.Count; j++)
            {
                int g = mainN + j;
                pos.Add(sp[j]);
                var mine = new List<int>();
                foreach (int m in sa[j] ?? Array.Empty<int>())
                {
                    if (m < 0 || m >= mainN)
                        continue;
                    mine.Add(m);
                    adj[m].Add(g);
                }
                adj.Add(mine);
            }
        }

        var g2 = new Graph { Pos = pos.ToArray(), Adj = new int[pos.Count][] };
        for (int i = 0; i < adj.Count; i++)
            g2.Adj[i] = adj[i].ToArray();
        return g2;
    }

    private static void Section(string json, string key, out List<Vector2> pos, out List<int[]> adj)
    {
        pos = null;
        adj = null;
        int k = json.IndexOf(key, StringComparison.Ordinal);
        if (k < 0)
            return;
        int open = json.IndexOf('[', k);
        int close = Bracket(json, open);
        if (open < 0 || close < 0)
            return;

        string sec = json.Substring(open, close - open + 1);
        pos = new List<Vector2>(128);
        adj = new List<int[]>(128);
        int i = 0;
        while (true)
        {
            int os = sec.IndexOf('{', i);
            if (os < 0)
                break;
            int oe = Brace(sec, os);
            if (oe < 0)
                break;
            string obj = sec.Substring(os, oe - os + 1);
            i = oe + 1;
            pos.Add(new Vector2(Flt(obj, "\"px\""), Flt(obj, "\"qy\"")));
            adj.Add(Ints(obj, "\"edg\""));
        }
    }

    private static int Bracket(string s, int open)
    {
        if (open < 0 || open >= s.Length || s[open] != '[')
            return -1;
        int d = 0;
        for (int k = open; k < s.Length; k++)
        {
            if (s[k] == '[')
                d++;
            else if (s[k] == ']' && --d == 0)
                return k;
        }
        return -1;
    }

    private static int Brace(string s, int open)
    {
        if (open < 0 || open >= s.Length || s[open] != '{')
            return -1;
        int d = 0;
        for (int k = open; k < s.Length; k++)
        {
            if (s[k] == '{')
                d++;
            else if (s[k] == '}' && --d == 0)
                return k;
        }
        return -1;
    }

    private static float Flt(string obj, string key)
    {
        int k = obj.IndexOf(key, StringComparison.Ordinal);
        if (k < 0)
            return 0f;
        int c = obj.IndexOf(':', k);
        if (c < 0)
            return 0f;
        int a = c + 1;
        while (a < obj.Length && char.IsWhiteSpace(obj[a]))
            a++;
        int b = a;
        while (b < obj.Length && (char.IsDigit(obj[b]) || "+-.eE".IndexOf(obj[b]) >= 0))
            b++;
        return float.TryParse(obj.Substring(a, b - a), NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : 0f;
    }

    private static int[] Ints(string obj, string key)
    {
        int k = obj.IndexOf(key, StringComparison.Ordinal);
        if (k < 0)
            return Array.Empty<int>();
        int open = obj.IndexOf('[', k);
        int close = Bracket(obj, open);
        if (open < 0 || close < 0)
            return Array.Empty<int>();
        string inner = obj.Substring(open + 1, close - open - 1);
        var list = new List<int>();
        int p = 0;
        while (p < inner.Length)
        {
            while (p < inner.Length && !(char.IsDigit(inner[p]) || inner[p] == '-'))
                p++;
            if (p >= inner.Length)
                break;
            int st = p;
            while (p < inner.Length && (char.IsDigit(inner[p]) || inner[p] == '-'))
                p++;
            if (int.TryParse(inner.Substring(st, p - st), out int v))
                list.Add(v);
        }
        return list.ToArray();
    }
}

// --- Ported from NocturneStyle.cs ---


internal sealed class NocturnePalette
{
    internal NocturnePalette(string id, string name, Color window, Color panel, Color accent, Color accentSoft,
        Color button, Color buttonHover, Color text, Color muted)
    {
        Id = id;
        Name = name;
        Window = window;
        Panel = panel;
        Accent = accent;
        AccentSoft = accentSoft;
        Button = button;
        ButtonHover = buttonHover;
        Text = text;
        Muted = muted;
    }

    internal string Id { get; }
    internal string Name { get; }
    internal Color Window { get; }
    internal Color Panel { get; }
    internal Color Accent { get; }
    internal Color AccentSoft { get; }
    internal Color Button { get; }
    internal Color ButtonHover { get; }
    internal Color Text { get; }
    internal Color Muted { get; }
}

internal static class NocturneStyle
{

    private static Color Soft(Color a) => new Color(a.r * 0.34f, a.g * 0.34f, a.b * 0.34f, 1f);

    private static NocturnePalette Theme(string name, Color accent) =>
        new NocturnePalette(name.ToLowerInvariant(), name, Rgb(13, 13, 13), Rgb(21, 21, 21), accent, Soft(accent),
            Rgb(31, 31, 31), Rgb(46, 46, 46), Rgb(237, 237, 236), Rgb(143, 143, 141));

    private static readonly NocturnePalette[] Themes =
    {
        Theme("Orange", Rgb(255, 143, 36)),
        Theme("Amber", Rgb(255, 176, 40)),
        Theme("Gold", Rgb(240, 205, 60)),
        Theme("Yellow", Rgb(235, 225, 72)),
        Theme("Lime", Rgb(178, 222, 60)),
        Theme("Green", Rgb(74, 201, 110)),
        Theme("Emerald", Rgb(45, 208, 140)),
        Theme("Mint", Rgb(120, 232, 182)),
        Theme("Teal", Rgb(40, 200, 182)),
        Theme("Cyan", Rgb(44, 206, 200)),
        Theme("Aqua", Rgb(60, 220, 236)),
        Theme("Sky", Rgb(82, 182, 255)),
        Theme("Blue", Rgb(74, 150, 255)),
        Theme("Indigo", Rgb(104, 98, 255)),
        Theme("Violet", Rgb(160, 110, 255)),
        Theme("Purple", Rgb(192, 100, 255)),
        Theme("Magenta", Rgb(236, 82, 232)),
        Theme("Pink", Rgb(255, 94, 168)),
        Theme("Rose", Rgb(255, 112, 142)),
        Theme("Red", Rgb(242, 32, 38)),
        Theme("Crimson", Rgb(226, 46, 82)),
        Theme("Coral", Rgb(255, 120, 92)),
        Theme("Slate", Rgb(122, 152, 208)),
        Theme("Silver", Rgb(212, 216, 224)),
    };

    private static Texture2D _white;
    private static GUIStyle _fill;
    private static GUIStyle _texStyle;
    private static readonly Dictionary<int, Texture2D> RoundedTex = new Dictionary<int, Texture2D>();
    private static readonly Dictionary<int, GUIStyle> RoundedStyles = new Dictionary<int, GUIStyle>();
    private static readonly Dictionary<int, GUIStyle> StrokeStyles = new Dictionary<int, GUIStyle>();

    internal static int ThemeCount => Themes.Length;
    internal static NocturnePalette ThemeAt(int i) => Themes[Mathf.Clamp(i, 0, Themes.Length - 1)];

    private static NocturnePalette _resolved;
    private static int _resolvedFrame = -1;

    internal static NocturnePalette Current
    {
        get
        {
            int f = Time.frameCount;
            if (_resolved != null && f == _resolvedFrame)
                return _resolved;

            _resolvedFrame = f;
            _resolved = Resolve();
            return _resolved;
        }
    }

    private static NocturnePalette Resolve()
    {
        return Theme("Custom", Color.HSVToRGB(0.5f, 0.5f, 0.5f));
    }

    private static Texture2D _hueBar;

    internal static Texture2D HueBar
    {
        get
        {
            if (_hueBar != null)
                return _hueBar;

            _hueBar = new Texture2D(256, 1, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            for (int i = 0; i < 256; i++)
                _hueBar.SetPixel(i, 0, Color.HSVToRGB(i / 255f, 0.62f, 0.98f));
            _hueBar.Apply();
            return _hueBar;
        }
    }

    private static GUIStyle _hueStyle;

    internal static void DrawHueBar(Rect r)
    {
        if (_hueStyle == null)
        {
            _hueStyle = new GUIStyle();
            _hueStyle.normal.background = HueBar;
        }

        Color prev = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, prev.a);
        GUI.Box(r, GUIContent.none, _hueStyle);
        GUI.color = prev;
    }

    internal static Texture2D White
    {
        get
        {
            if (_white == null)
                _white = Solid(Color.white);
            return _white;
        }
    }

    internal static Texture2D Solid(Color c)
    {

        var t = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave,
            wrapMode = TextureWrapMode.Clamp
        };
        t.SetPixel(0, 0, c);
        t.Apply();
        return t;
    }

    internal static void ClampWindow(ref Rect win, float sw, float sh, float minW, float minH)
    {
        win.width = Mathf.Clamp(win.width, minW, sw);
        win.height = Mathf.Clamp(win.height, minH, sh);
        win.x = Mathf.Clamp(win.x, 0f, sw - win.width);
        win.y = Mathf.Clamp(win.y, 0f, sh - win.height);
    }

    internal static void Fill(Rect r, Color c)
    {
        if (_fill == null)
        {
            _fill = new GUIStyle();
            _fill.normal.background = White;
        }

        Color prev = GUI.color;
        GUI.color = new Color(c.r, c.g, c.b, c.a * prev.a);
        GUI.Box(r, GUIContent.none, _fill);
        GUI.color = prev;
    }

    internal static void FillRaw(Rect r)
    {
        if (_fill == null)
        {
            _fill = new GUIStyle();
            _fill.normal.background = White;
        }
        GUI.Box(r, GUIContent.none, _fill);
    }

    internal static void FillRounded(Rect r, Color c, int radius)
    {
        GUIStyle st = RoundedStyle(radius);
        Color prev = GUI.color;
        GUI.color = new Color(c.r, c.g, c.b, c.a * prev.a);
        GUI.Box(r, GUIContent.none, st);
        GUI.color = prev;
    }

    internal static GUIStyle RoundedStyle(int radius)
    {
        radius = Mathf.Clamp(radius, 2, 40);
        if (RoundedStyles.TryGetValue(radius, out GUIStyle s) && s != null)
            return s;

        s = new GUIStyle();
        s.normal.background = RoundedTexture(radius);
        s.border = Offset(radius, radius, radius, radius);
        RoundedStyles[radius] = s;
        return s;
    }

    private static Texture2D RoundedTexture(int radius)
    {
        if (RoundedTex.TryGetValue(radius, out Texture2D t) && t != null)
            return t;

        int size = radius * 2 + 2;
        t = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        var px = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float sx = x + 0.5f;
                float sy = y + 0.5f;
                float dx = sx < radius ? radius - sx : (sx > size - radius ? sx - (size - radius) : 0f);
                float dy = sy < radius ? radius - sy : (sy > size - radius ? sy - (size - radius) : 0f);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(radius - dist + 0.5f);
                px[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
        }

        t.SetPixels32(px);
        t.Apply();
        RoundedTex[radius] = t;
        return t;
    }

    internal static void StrokeRounded(Rect r, Color c, int radius, int thickness)
    {
        radius = Mathf.Clamp(radius, 3, 40);
        thickness = Mathf.Clamp(thickness, 1, 6);
        int key = radius * 16 + thickness;
        if (!StrokeStyles.TryGetValue(key, out GUIStyle st) || st == null)
        {
            st = new GUIStyle();
            st.normal.background = BuildStroke(radius, thickness);
            st.border = Offset(radius, radius, radius, radius);
            StrokeStyles[key] = st;
        }

        Color prev = GUI.color;
        GUI.color = new Color(c.r, c.g, c.b, c.a * prev.a);
        GUI.Box(r, GUIContent.none, st);
        GUI.color = prev;
    }

    private static Texture2D BuildStroke(int radius, int thickness)
    {
        int size = radius * 2 + 2;
        var t = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        float c = size / 2f;
        float ext = size / 2f - radius;
        float half = thickness * 0.5f;
        var px = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float qx = Mathf.Abs(x + 0.5f - c) - ext;
                float qy = Mathf.Abs(y + 0.5f - c) - ext;
                float outside = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f));
                float inside = Mathf.Min(Mathf.Max(qx, qy), 0f);
                float sd = outside + inside - radius;
                float a = Mathf.Clamp01(half - Mathf.Abs(sd) + 0.5f);
                px[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }

        t.SetPixels32(px);
        t.Apply();
        return t;
    }

    private static Texture2D _glowTex;
    private static GUIStyle _glowStyle;

    internal static void Glow(Rect r, Color c)
    {
        if (_glowTex == null)
            _glowTex = BuildGlow(96);
        if (_glowStyle == null)
        {
            _glowStyle = new GUIStyle();
            _glowStyle.normal.background = _glowTex;
        }
        Color prev = GUI.color;
        GUI.color = new Color(c.r, c.g, c.b, c.a * prev.a);
        GUI.Box(r, GUIContent.none, _glowStyle);
        GUI.color = prev;
    }

    internal static void DrawTex(Rect r, Texture2D tex)
    {
        if (tex == null)
            return;
        if (_texStyle == null)
            _texStyle = new GUIStyle();
        _texStyle.normal.background = tex;
        Color prev = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, prev.a);
        GUI.Box(r, GUIContent.none, _texStyle);
        GUI.color = prev;
    }

    internal static Texture2D BuildGradient(int w, int h, Color top, Color bottom, int radius)
    {
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            Color row = Color.Lerp(top, bottom, (float)y / Mathf.Max(1, h - 1));
            for (int x = 0; x < w; x++)
            {
                float dx = x < radius ? radius - x : (x > w - radius ? x - (w - radius) : 0f);
                float dy = y < radius ? radius - y : (y > h - radius ? y - (h - radius) : 0f);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(radius - dist + 0.5f);
                px[y * w + x] = new Color(row.r, row.g, row.b, a);
            }
        }

        t.SetPixels32(px);
        t.Apply();
        return t;
    }

    internal static Texture2D BuildVFade(int h, Color color, float topAlpha, float bottomAlpha)
    {
        var t = new Texture2D(1, h, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        var px = new Color32[h];
        for (int y = 0; y < h; y++)
        {
            float a = Mathf.Lerp(topAlpha, bottomAlpha, (float)y / Mathf.Max(1, h - 1));
            px[y] = new Color(color.r, color.g, color.b, a);
        }
        t.SetPixels32(px);
        t.Apply();
        return t;
    }

    internal static Texture2D BuildDisc(int size)
    {
        var t = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        var px = new Color32[size * size];
        float cc = (size - 1) * 0.5f, rad = cc - 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - cc, dy = y - cc;
                float a = Mathf.Clamp01(rad - Mathf.Sqrt(dx * dx + dy * dy) + 0.75f);
                px[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        t.SetPixels32(px);
        t.Apply();
        return t;
    }

    internal static Texture2D BuildGlow(int size)
    {
        var t = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        var px = new Color32[size * size];
        float cc = (size - 1) * 0.5f, rad = cc;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - cc, dy = y - cc;
                float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy) / rad);
                a = a * a * a;
                px[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        t.SetPixels32(px);
        t.Apply();
        return t;
    }

    internal static RectOffset Offset(int l, int r, int t, int b)
    {
        var o = new RectOffset();
        o.left = l;
        o.right = r;
        o.top = t;
        o.bottom = b;
        return o;
    }

    private static Color Rgb(byte r, byte g, byte b) => new Color(r / 255f, g / 255f, b / 255f, 1f);
}

// --- Ported from NocturneDoors.cs ---


internal static class NocturneDoors
{
    private static readonly HashSet<int> _pinned = new HashSet<int>();
    private static readonly HashSet<int> _seen = new HashSet<int>();
    private static float _next;

    public static bool HasPins => _pinned.Count > 0;

    public static void CloseAll() => ForEachRoom(Close);
    public static void PinAll() => ForEachRoom(r => { Close(r); _pinned.Add(r); });
    public static void UnpinAll() => _pinned.Clear();

    public static bool IsPinned(int room) => _pinned.Contains(room);
    public static void CloseOne(int room) => Close(room);

    public static bool TogglePin(int room)
    {
        if (IsDecon(room))
            return false;
        if (_pinned.Remove(room))
            return false;
        Close(room);
        _pinned.Add(room);
        return true;
    }

    public static void OpenAll()
    {
        ShipStatus ss = ShipStatus.Instance;
        if (ss == null || ss.AllDoors == null)
            return;
        try
        {
            foreach (OpenableDoor d in ss.AllDoors)
                OpenDoor(ss, d);
        }
        catch { }
    }

    public static void Tick()
    {
        ShipStatus ss = ShipStatus.Instance;
        if (ss == null)
            return;
        if (Time.unscaledTime < _next)
            return;
        _next = Time.unscaledTime + 0.7f;

        if (_pinned.Count == 0)
            return;
        foreach (int r in _pinned)
            Close(r);
    }

    private static void ForEachRoom(Action<int> act)
    {
        ShipStatus ss = ShipStatus.Instance;
        if (ss == null || ss.AllDoors == null)
            return;
        _seen.Clear();
        try
        {
            foreach (OpenableDoor d in ss.AllDoors)
            {
                if (d == null)
                    continue;
                int r = (int)d.Room;
                if (_seen.Add(r))
                    act(r);
            }
        }
        catch { }
    }

    private static void OpenDoor(ShipStatus ss, OpenableDoor d)
    {
        if (d == null || IsDecon((int)d.Room))
            return;
        try
        {
            ss.RpcUpdateSystem(SystemTypes.Doors, (byte)(d.Id | 64));
            d.SetDoorway(true);
        }
        catch { }
    }

    private static void Close(int r)
    {
        if (IsDecon(r))
            return;
        try
        {
            ShipStatus.Instance.RpcCloseDoorsOfType((SystemTypes)r);
        }
        catch { }
    }

    private static bool IsDecon(int r) =>
        r == (int)SystemTypes.Decontamination || r == (int)SystemTypes.Decontamination2 || r == (int)SystemTypes.Decontamination3;
}

