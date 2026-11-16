using System;
using System.Text;

namespace SuitEnhancementSuite;

/// <summary>
/// Which <see cref="LogArea"/>s write their extra log lines. The config key fills one instance at load and the console
/// changes the same instance, so there is a single set of flags. A change is all or nothing: a bad name is refused and
/// the flags stay as they were. <see cref="On"/> is one array read and never allocates.
/// </summary>
internal sealed class VerboseFlags
{
    private static readonly string[] Names = BuildNames();

    private readonly bool[] _on = new bool[Names.Length];

    /// <summary>The valid names, for a refusal or a help line.</summary>
    public static string ValidNames { get; } = "off, all, " + string.Join(", ", Names);

    public bool On(LogArea area) => _on[(int)area];

    public bool AnyOn
    {
        get
        {
            foreach (var on in _on)
                if (on) return true;
            return false;
        }
    }

    /// <summary>Sets every flag from <c>off</c>, <c>all</c> or a comma list of area names; blank means off.</summary>
    /// <param name="refusal">The reason and the valid names when a name is wrong; null on success.</param>
    public bool TryApply(string spec, out string refusal)
    {
        var next = new bool[_on.Length];
        refusal = null;
        var text = spec?.Trim() ?? string.Empty;
        if (text.Equals("all", StringComparison.OrdinalIgnoreCase)) SetAll(next, true);
        else if (text.Length > 0 && !text.Equals("off", StringComparison.OrdinalIgnoreCase))
            foreach (var part in text.Split(','))
            {
                var name = part.Trim();
                if (name.Length == 0) continue;
                if (!TryFind(name, out var area)) return Refuse(name, out refusal);
                next[(int)area] = true;
            }
        Array.Copy(next, _on, next.Length);
        return true;
    }

    /// <summary>Turns one area, or <c>all</c>, on or off and leaves the others as they are.</summary>
    public bool TrySet(string areaName, bool on, out string refusal)
    {
        refusal = null;
        var name = areaName?.Trim() ?? string.Empty;
        if (name.Equals("all", StringComparison.OrdinalIgnoreCase)) SetAll(_on, on);
        else if (TryFind(name, out var area)) _on[(int)area] = on;
        else return Refuse(name, out refusal);
        return true;
    }

    /// <summary>Finds one area by name, ignoring case.</summary>
    public static bool TryParseArea(string name, out LogArea area, out string refusal)
    {
        refusal = null;
        if (TryFind(name?.Trim() ?? string.Empty, out area)) return true;
        return Refuse(name, out refusal);
    }

    /// <summary>Every area with its state, one per line, for <c>ses log</c>.</summary>
    public string ListLines()
    {
        var text = new StringBuilder();
        for (var i = 0; i < Names.Length; i++)
            text.Append(Names[i]).Append(": ").Append(_on[i] ? "on" : "off").Append('\n');
        return text.ToString().TrimEnd('\n');
    }

    /// <summary><c>off</c>, or the areas that are on, as a comma list.</summary>
    public string Summary()
    {
        var text = new StringBuilder();
        for (var i = 0; i < Names.Length; i++)
            if (_on[i]) text.Append(text.Length == 0 ? "" : ",").Append(Names[i]);
        return text.Length == 0 ? "off" : text.ToString();
    }

    private static void SetAll(bool[] flags, bool on)
    {
        for (var i = 0; i < flags.Length; i++)
            flags[i] = on;
    }

    private static bool Refuse(string badName, out string refusal)
    {
        refusal = $"unknown log area '{badName}'; valid: {ValidNames}";
        return false;
    }

    private static bool TryFind(string name, out LogArea area)
    {
        for (var i = 0; i < Names.Length; i++)
        {
            if (!Names[i].Equals(name, StringComparison.OrdinalIgnoreCase)) continue;
            area = (LogArea)i;
            return true;
        }
        area = default;
        return false;
    }

    private static string[] BuildNames()
    {
        var names = new string[Enum.GetValues(typeof(LogArea)).Length];
        for (var i = 0; i < names.Length; i++)
            names[i] = ((LogArea)i).ToString().ToLowerInvariant();
        return names;
    }
}
