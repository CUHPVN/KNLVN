// This file documents the sample Level_01 layout.
// Create it in Unity via: right-click → Create → KNLVN → Level Data
//
// Grid: 8 wide × 6 tall  (borders auto-filled as Wall by LevelManager)
//
// Layout (y=0 at bottom):
//
//   Col:  0  1  2  3  4  5  6  7
// y=5: [W][W][W][W][W][W][W][W]
// y=4: [W][ ][ ][ ][ ][ ][ ][W]
// y=3: [W][B9][Y ][B+][Y ][B=][Y ][ R ][W]  ← equation row (but we only have 8 cols so shift)
// y=2: [W][ ][ ][ ][ ][ ][ ][W]
// y=1: [W][P][ ][5][ ][ ][ ][W]   P=player start, 5=floor item
// y=0: [W][W][W][W][W][W][W][W]
//
// SIMPLER 10×8 version:
//
// Width=10, Height=8, PlayerStart=(1,1)
//
// Cells (non-wall, non-empty):
//   (1,1)  Empty  ← player start (no cell entry needed)
//   (2,3)  Blue   "9"
//   (3,3)  Yellow ""   ← empty yellow, player can push items here
//   (4,3)  Blue   "+"
//   (5,3)  Yellow ""
//   (6,3)  Blue   "="
//   (7,3)  Yellow ""
//   (8,3)  Red    ""   ← exit door
//   (3,1)  Empty  "2"  ← floor item "2"
//   (5,1)  Empty  "4"  ← floor item "4"
//   (1,2)  Empty  "1"  ← floor item "1"
//   (2,2)  Yellow ""
//   
// Equation to solve: 9 + (Y) (Y) = (Y)  → e.g. 9 + 24 = 11  ← as shown in GDD example
// But easier: place "1" and "1" on yellows → 9 + 1 = ... nope
// Suggested solution: 9 + 4 = 13  (place "4" in yellow at pos 5,3; place "13" as "1" then "3")
// Actually simplest: Blue "9", Blue "+", Blue "=" with yellow slots:
//   9 + [Y] = [Y][Y]  → 9+3=12 by placing 3 on one yellow and 1,2 on the two-yellow side
//
// In the Inspector, set up LevelData as follows:
//   Width = 10
//   Height = 8
//   PlayerStartPos = (1, 1)
//   Cells:
//     (2,4) Blue  "9"
//     (3,4) Yellow ""
//     (4,4) Blue  "+"
//     (5,4) Yellow ""
//     (6,4) Blue  "="
//     (7,4) Yellow ""
//     (8,4) Yellow ""
//     (9,4) Red   ""
//     (2,2) Empty "3"    ← floor item
//     (4,2) Empty "1"    ← floor item
//     (6,2) Empty "2"    ← floor item
//     (1,3) Yellow ""    ← extra pushable yellow
//
// Solution: push "3" into Y at (3,4) → 9+3; push "1" into Y at (7,4), "2" into Y at (8,4) → =12
// Equation: 9+3=12 ✓ → door opens at (9,4)

