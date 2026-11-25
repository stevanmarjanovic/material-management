using MaterialManagement.Api.Models;

namespace MaterialManagement.Api.Domain;

/// <summary>Care for a product sewn from several fabrics: per category, the most delicate fabric wins.</summary>
public static class CareRules
{
    // Least to most restrictive.
    static readonly Wash[] WashOrder = [Wash.W95, Wash.W70, Wash.W60, Wash.W60Mild, Wash.W50, Wash.W50Mild, Wash.W40,
        Wash.W40Mild, Wash.W40VeryMild, Wash.W30, Wash.W30Mild, Wash.W30VeryMild, Wash.Hand, Wash.DoNot];
    static readonly Bleach[] BleachOrder = [Bleach.Any, Bleach.NonChlorine, Bleach.DoNot];
    static readonly Iron[] IronOrder = [Iron.High, Iron.Medium, Iron.Low, Iron.DoNot];
    static readonly TumbleDry[] TumbleOrder = [TumbleDry.Normal, TumbleDry.Low, TumbleDry.DoNot];

    public static Wash? Strictest(IEnumerable<Wash?> v) => Strictest(v, WashOrder);
    public static Bleach? Strictest(IEnumerable<Bleach?> v) => Strictest(v, BleachOrder);
    public static Iron? Strictest(IEnumerable<Iron?> v) => Strictest(v, IronOrder);
    public static TumbleDry? Strictest(IEnumerable<TumbleDry?> v) => Strictest(v, TumbleOrder);

    static T? Strictest<T>(IEnumerable<T?> values, T[] order) where T : struct =>
        values.OfType<T>().Select(v => (T?)v).MaxBy(v => Array.IndexOf(order, v!.Value));

    /// <summary>
    /// P allows the F solvents too, so P + F = F; any mild = mild. Wet cleaning (W) is a different process:
    /// mixing it with P/F means no single process is known to suit all fabrics, so: do not dry clean.
    /// </summary>
    public static DryClean? Strictest(IEnumerable<DryClean?> values)
    {
        var v = values.OfType<DryClean>().Distinct().ToList();
        if (v.Count == 0) return null;
        if (v.Contains(DryClean.DoNot)) return DryClean.DoNot;
        var wet = v.Where(x => x is DryClean.W or DryClean.WMild or DryClean.WVeryMild).ToList();
        if (wet.Count > 0 && wet.Count < v.Count) return DryClean.DoNot;
        if (wet.Count > 0) return wet.Max(); // W < WMild < WVeryMild in the enum
        var solventF = v.Any(x => x is DryClean.F or DryClean.FMild);
        var mild = v.Any(x => x is DryClean.PMild or DryClean.FMild);
        return (solventF, mild) switch
        {
            (true, true) => DryClean.FMild, (true, false) => DryClean.F, (false, true) => DryClean.PMild, _ => DryClean.P,
        };
    }
}
