using System;
using System.Collections.Generic;
using UnityEngine;

namespace KNLVN.Game
{
    /// <summary>
    /// Core equation evaluation logic.
    ///
    /// Steps:
    ///   1. Find all Blue cells; they must all be in the same row OR column.
    ///   2. Walk that axis to collect the contiguous chain that includes every Blue cell.
    ///   3. Chain must contain exactly one "=" token.
    ///   4. Run <see cref="NumberMerger"/> on the chain.
    ///   5. Split at "=" → evaluate left side and right side with standard precedence (x, / before +, -).
    ///   6. Return true iff left == right (integer arithmetic).
    /// </summary>
    public class EquationEvaluator
    {
        private readonly GameGrid _grid;

        public EquationEvaluator(GameGrid grid) => _grid = grid;

        // ─── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Evaluates the current grid state.
        /// Returns true if a valid, correct equation is formed.
        /// </summary>
        public bool Evaluate()
        {
            var blueCells = _grid.GetBlueCells();
            if (blueCells.Count == 0) return false;

            // 1. All blue cells must be on the same row or column
            bool sameRow    = AllSameRow(blueCells);
            bool sameColumn = AllSameColumn(blueCells);

            if (!sameRow && !sameColumn)
            {
                Debug.Log("[Evaluator] Blue cells are not aligned on a single axis.");
                return false;
            }

            // 2. Build the contiguous chain along the detected axis
            List<GameGridCell> chain = sameRow
                ? BuildHorizontalChain(blueCells)
                : BuildVerticalChain(blueCells);

            if (chain == null)
            {
                Debug.Log("[Evaluator] Chain could not be formed (Blue cells not contiguous).");
                return false;
            }

            // 3. Validate chain contains all blue cells
            foreach (var blue in blueCells)
                if (!chain.Contains(blue)) return false;

            // 4. Tokenise + merge digits
            var tokens = NumberMerger.Merge(chain);

            // 5. Must have exactly one "=" token
            int eqCount = 0, eqIndex = -1;
            for (int i = 0; i < tokens.Count; i++)
            {
                if (tokens[i].IsEquals) { eqCount++; eqIndex = i; }
            }
            if (eqCount != 1)
            {
                Debug.Log($"[Evaluator] Invalid — {eqCount} '=' tokens (need exactly 1).");
                return false;
            }

            // 6. Split and evaluate
            var left  = tokens.GetRange(0, eqIndex);
            var right = tokens.GetRange(eqIndex + 1, tokens.Count - eqIndex - 1);

            if (!TryCalculate(left,  out long lVal)) return false;
            if (!TryCalculate(right, out long rVal)) return false;

            bool valid = lVal == rVal;
            Debug.Log($"[Evaluator] {TokensToString(left)} = {TokensToString(right)} → {lVal} == {rVal} : {valid}");
            return valid;
        }

        // ─── Chain building ───────────────────────────────────────────────────

        /// <summary>
        /// Walk the row of the first blue cell and collect all contiguous meaningful cells.
        /// A "gap" (wall, non-content empty, or out-of-bounds) terminates the walk.
        /// The resulting chain must cover all Blue cells.
        /// </summary>
        private List<GameGridCell> BuildHorizontalChain(List<GameGridCell> blueCells)
        {
            int row    = blueCells[0].GridPos.y;
            int minX   = int.MaxValue, maxX = int.MinValue;

            foreach (var c in blueCells)
            {
                if (c.GridPos.x < minX) minX = c.GridPos.x;
                if (c.GridPos.x > maxX) maxX = c.GridPos.x;
            }

            // Expand left and right from the blue cell extremes
            int startX = ExpandLeft(minX, row, isRow: true);
            int endX   = ExpandRight(maxX, row, isRow: true);

            var chain = new List<GameGridCell>();
            for (int x = startX; x <= endX; x++)
            {
                var cell = _grid.GetCell(x, row);
                if (cell == null || !HasContent(cell)) return null; // gap = invalid
                chain.Add(cell);
            }
            return chain;
        }

        private List<GameGridCell> BuildVerticalChain(List<GameGridCell> blueCells)
        {
            int col  = blueCells[0].GridPos.x;
            int minY = int.MaxValue, maxY = int.MinValue;

            foreach (var c in blueCells)
            {
                if (c.GridPos.y < minY) minY = c.GridPos.y;
                if (c.GridPos.y > maxY) maxY = c.GridPos.y;
            }

            int startY = ExpandDown(minY, col, isRow: false);
            int endY   = ExpandUp  (maxY, col, isRow: false);

            var chain = new List<GameGridCell>();
            for (int y = startY; y <= endY; y++)
            {
                var cell = _grid.GetCell(col, y);
                if (cell == null || !HasContent(cell)) return null;
                chain.Add(cell);
            }
            return chain;
        }

        // ─── Expansion helpers ────────────────────────────────────────────────

        private int ExpandLeft(int startX, int row, bool isRow)
        {
            int x = startX - 1;
            while (x >= 0)
            {
                var c = _grid.GetCell(x, row);
                if (c == null || !HasContent(c)) break;
                x--;
            }
            return x + 1;
        }

        private int ExpandRight(int startX, int row, bool isRow)
        {
            int x = startX + 1;
            while (x < _grid.Width)
            {
                var c = _grid.GetCell(x, row);
                if (c == null || !HasContent(c)) break;
                x++;
            }
            return x - 1;
        }

        private int ExpandDown(int startY, int col, bool isRow)
        {
            int y = startY - 1;
            while (y >= 0)
            {
                var c = _grid.GetCell(col, y);
                if (c == null || !HasContent(c)) break;
                y--;
            }
            return y + 1;
        }

        private int ExpandUp(int startY, int col, bool isRow)
        {
            int y = startY + 1;
            while (y < _grid.Height)
            {
                var c = _grid.GetCell(col, y);
                if (c == null || !HasContent(c)) break;
                y++;
            }
            return y - 1;
        }

        // ─── Helpers ──────────────────────────────────────────────────────────

        /// <summary>A cell "has content" in an equation context if it's Blue, Yellow/Star with value, or a floor-item.</summary>
        private static bool HasContent(GameGridCell cell)
        {
            if (cell.IsBlue)   return !cell.Content.IsEmpty;
            if (cell.IsPushable) return !cell.Content.IsEmpty;
            if (cell.IsEmpty && cell.HasFloorItem) return true; // loose items also count
            return false;
        }

        private static CellContent GetEffectiveContent(GameGridCell cell)
        {
            if (cell.IsBlue || cell.IsPushable) return cell.Content;
            if (cell.HasFloorItem)              return cell.FloorItem;
            return CellContent.Empty;
        }

        private static bool AllSameRow(List<GameGridCell> cells)
        {
            int row = cells[0].GridPos.y;
            foreach (var c in cells) if (c.GridPos.y != row) return false;
            return true;
        }

        private static bool AllSameColumn(List<GameGridCell> cells)
        {
            int col = cells[0].GridPos.x;
            foreach (var c in cells) if (c.GridPos.x != col) return false;
            return true;
        }

        // ─── Arithmetic evaluation (with x / before + -) ─────────────────────

        /// <summary>
        /// Evaluates a list of tokens (no "=" present) using standard precedence.
        /// Returns false if the token list is malformed.
        /// </summary>
        private static bool TryCalculate(List<MergedToken> tokens, out long result)
        {
            result = 0;
            if (tokens.Count == 0) return false;

            // First pass: handle x and /
            var pass1 = new List<MergedToken>(tokens);
            int i = 1;
            while (i < pass1.Count)
            {
                var op = pass1[i];
                if (op.Kind == TokenKind.Multiply || op.Kind == TokenKind.Divide)
                {
                    if (i - 1 < 0 || i + 1 >= pass1.Count) return false;
                    if (!pass1[i - 1].IsNumber || !pass1[i + 1].IsNumber) return false;

                    long a = pass1[i - 1].NumericValue;
                    long b = pass1[i + 1].NumericValue;
                    long r;
                    if (op.Kind == TokenKind.Multiply) r = a * b;
                    else
                    {
                        if (b == 0) { Debug.Log("[Evaluator] Division by zero."); return false; }
                        r = a / b;
                    }

                    pass1[i - 1] = new MergedToken { Kind = TokenKind.Number, NumericValue = r };
                    pass1.RemoveAt(i + 1);
                    pass1.RemoveAt(i);
                    // don't advance i — recheck from same position
                }
                else
                {
                    i += 2;
                }
            }

            // Second pass: handle + and -
            if (!pass1[0].IsNumber) return false;
            long acc = pass1[0].NumericValue;

            for (int j = 1; j < pass1.Count; j += 2)
            {
                if (j + 1 >= pass1.Count) return false;
                var op = pass1[j];
                var nb = pass1[j + 1];
                if (!nb.IsNumber) return false;

                if (op.Kind == TokenKind.Plus)  acc += nb.NumericValue;
                else if (op.Kind == TokenKind.Minus) acc -= nb.NumericValue;
                else return false; // unexpected token
            }

            result = acc;
            return true;
        }

        private static string TokensToString(List<MergedToken> tokens)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var t in tokens) sb.Append(t).Append(' ');
            return sb.ToString().TrimEnd();
        }
    }
}
