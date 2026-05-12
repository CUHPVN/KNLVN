using System.Collections.Generic;
using UnityEngine;

namespace KNLVN.Game
{
    /// <summary>
    /// Merges adjacent digit cells into multi-digit numbers before equation evaluation.
    ///
    /// Rules (from GDD):
    ///   • Two number cells that are horizontally or vertically adjacent are merged.
    ///   • Priority: Left → Right, then Top → Down (processed in that scan order).
    ///   • A "merged" number is represented as a <see cref="MergedToken"/> that spans
    ///     multiple cells but is treated as a single value in the equation chain.
    ///   • Merging does NOT mutate cell data — it produces a token list used only
    ///     by <see cref="EquationEvaluator"/>.
    /// </summary>
    public static class NumberMerger
    {
        // ─── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Given an ordered list of cells forming a candidate equation chain
        /// (already sorted Left→Right or Top→Down by the evaluator),
        /// return a merged token list where adjacent digit cells are combined.
        /// </summary>
        public static List<MergedToken> Merge(List<GameGridCell> chain)
        {
            var tokens = new List<MergedToken>();
            int i = 0;

            while (i < chain.Count)
            {
                var cell = chain[i];

                if (cell.Content.IsNumber || (cell.IsStar && cell.Content.IsNumber))
                {
                    // Accumulate consecutive number cells
                    long numericValue = 0;
                    int  digitCount   = 0;
                    bool hasStar      = false;

                    while (i < chain.Count && (chain[i].Content.IsNumber))
                    {
                        numericValue = numericValue * 10 + chain[i].Content.NumericValue;
                        if (chain[i].IsStar) hasStar = true;
                        digitCount++;
                        i++;
                    }

                    // Star doubles the value
                    if (hasStar) numericValue *= 2;

                    tokens.Add(new MergedToken
                    {
                        Kind          = TokenKind.Number,
                        NumericValue  = numericValue,
                        HasStar       = hasStar,
                    });
                }
                else
                {
                    // Operator or equals token — pass through as-is
                    var kind = cell.Content.RawValue switch
                    {
                        CellContent.TokenPlus   => TokenKind.Plus,
                        CellContent.TokenMinus  => TokenKind.Minus,
                        CellContent.TokenMul    => TokenKind.Multiply,
                        CellContent.TokenDiv    => TokenKind.Divide,
                        CellContent.TokenEquals => TokenKind.Equals,
                        _                       => TokenKind.Unknown
                    };
                    tokens.Add(new MergedToken { Kind = kind });
                    i++;
                }
            }

            return tokens;
        }
    }

    // ─── Token types ──────────────────────────────────────────────────────────

    public enum TokenKind { Number, Plus, Minus, Multiply, Divide, Equals, Unknown }

    public struct MergedToken
    {
        public TokenKind Kind;
        public long      NumericValue;  // valid when Kind == Number
        public bool      HasStar;       // value was already doubled

        public bool IsNumber  => Kind == TokenKind.Number;
        public bool IsEquals  => Kind == TokenKind.Equals;
        public bool IsOperator => Kind is TokenKind.Plus or TokenKind.Minus
                                         or TokenKind.Multiply or TokenKind.Divide;
        public override string ToString() => Kind switch
        {
            TokenKind.Number   => NumericValue.ToString() + (HasStar ? "★" : ""),
            TokenKind.Plus     => "+",
            TokenKind.Minus    => "-",
            TokenKind.Multiply => "x",
            TokenKind.Divide   => "/",
            TokenKind.Equals   => "=",
            _                  => "?"
        };
    }
}
