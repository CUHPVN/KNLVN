using System;

namespace KNLVN.Game
{
    /// <summary>
    /// Immutable value object that holds the content of a cell or a floor item.
    /// Content can be a single digit (0–9), an arithmetic operator (+,-,*,/), or an equals sign (=).
    /// </summary>
    [Serializable]
    public sealed class CellContent
    {
        // ─── Static sentinels ────────────────────────────────────────────────

        /// <summary>Represents an empty slot (no content).</summary>
        public static readonly CellContent Empty = new CellContent(string.Empty);

        // ─── Operator token strings ───────────────────────────────────────────
        public const string TokenPlus    = "+";
        public const string TokenMinus   = "-";
        public const string TokenMul     = "x";
        public const string TokenDiv     = "/";
        public const string TokenEquals  = "=";

        // ─── Fields ──────────────────────────────────────────────────────────

        /// <summary>Raw string value: "0"–"9", "+", "-", "x", "/", "=".</summary>
        public readonly string RawValue;

        // ─── Derived properties ───────────────────────────────────────────────

        public bool IsEmpty    => string.IsNullOrEmpty(RawValue);
        public bool IsNumber   => !IsEmpty && char.IsDigit(RawValue[0]);
        public bool IsEquals   => RawValue == TokenEquals;
        public bool IsOperator => !IsEmpty && !IsNumber && !IsEquals;

        /// <summary>Numeric value when <see cref="IsNumber"/> is true, otherwise 0.</summary>
        public int NumericValue => IsNumber ? int.Parse(RawValue) : 0;

        // ─── Constructor ──────────────────────────────────────────────────────

        public CellContent(string rawValue)
        {
            RawValue = rawValue ?? string.Empty;
        }

        // ─── Factory ──────────────────────────────────────────────────────────

        /// <summary>Parse a raw string and return a CellContent. Returns Empty for null/blank.</summary>
        public static CellContent FromRaw(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return Empty;
            return new CellContent(raw.Trim());
        }

        // ─── Overrides ────────────────────────────────────────────────────────

        public override string ToString() => IsEmpty ? "(empty)" : RawValue;

        public override bool Equals(object obj) =>
            obj is CellContent other && string.Equals(RawValue, other.RawValue, StringComparison.Ordinal);

        public override int GetHashCode() => RawValue?.GetHashCode() ?? 0;
    }
}
