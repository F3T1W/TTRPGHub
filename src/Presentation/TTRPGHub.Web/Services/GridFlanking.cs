namespace TTRPGHub.Services;

// L.7 — упрощённый фланк PF2e на сетке (см. Application GridFlanking — дублируем в Web,
// т.к. WASM не ссылается на Application).
public static class GridFlanking
{
    public static bool IsFlanked(
        double ax, double ay, int aW, int aH,
        double bx, double by, int bW, int bH,
        double tx, double ty, int tW, int tH)
    {
        if (!AreAdjacent(ax, ay, aW, aH, tx, ty, tW, tH)) return false;
        if (!AreAdjacent(bx, by, bW, bH, tx, ty, tW, tH)) return false;

        var tcx = tx + tW / 2.0;
        var tcy = ty + tH / 2.0;
        var vax = (ax + aW / 2.0) - tcx;
        var vay = (ay + aH / 2.0) - tcy;
        var vbx = (bx + bW / 2.0) - tcx;
        var vby = (by + bH / 2.0) - tcy;
        var lenA = Math.Sqrt(vax * vax + vay * vay);
        var lenB = Math.Sqrt(vbx * vbx + vby * vby);
        if (lenA < 0.01 || lenB < 0.01) return false;

        var dot = (vax * vbx + vay * vby) / (lenA * lenB);
        return dot < -0.3;
    }

    private static bool AreAdjacent(double ax, double ay, int aW, int aH, double tx, double ty, int tW, int tH)
    {
        foreach (var (acx, acy) in OccupiedCells(ax, ay, aW, aH))
        foreach (var (tcx, tcy) in OccupiedCells(tx, ty, tW, tH))
        {
            var dx = Math.Abs(acx - tcx);
            var dy = Math.Abs(acy - tcy);
            if (dx <= 1 && dy <= 1 && (dx + dy) > 0) return true;
        }
        return false;
    }

    private static IEnumerable<(int X, int Y)> OccupiedCells(double x, double y, int w, int h)
    {
        var x0 = (int)Math.Floor(x);
        var y0 = (int)Math.Floor(y);
        for (var dx = 0; dx < w; dx++)
        for (var dy = 0; dy < h; dy++)
            yield return (x0 + dx, y0 + dy);
    }
}
