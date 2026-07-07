// L.7 smoke — без браузера/логина: JS-логика диагоналей + GridFlanking + сборка DTO.
using TTRPGHub.Services;

static int Pf2eGridDistanceFeet(int cellsX, int cellsY)
{
    var diagonals = Math.Min(cellsX, cellsY);
    var straights = Math.Max(cellsX, cellsY) - diagonals;
    var diagCost = 0;
    for (var i = 0; i < diagonals; i++) diagCost += i % 2 == 0 ? 5 : 10;
    return diagCost + straights * 5;
}

var fails = 0;
void Assert(string name, bool ok)
{
    if (ok) Console.WriteLine($"  OK  {name}");
    else { Console.WriteLine($"  FAIL {name}"); fails++; }
}

Console.WriteLine("L.7 smoke");

Console.WriteLine("PF2e diagonals:");
Assert("1 diag = 5ft", Pf2eGridDistanceFeet(1, 0) == 5);
Assert("2 straight = 10ft", Pf2eGridDistanceFeet(2, 0) == 10);
Assert("2 diag = 15ft", Pf2eGridDistanceFeet(2, 2) == 15);
Assert("1 diag = 5ft", Pf2eGridDistanceFeet(1, 1) == 5);
Assert("1 diag + 1 straight = 10ft", Pf2eGridDistanceFeet(2, 1) == 10);

Console.WriteLine("Flanking:");
Assert("opposite allies flank", GridFlanking.IsFlanked(4, 4, 1, 1, 6, 4, 1, 1, 5, 4, 1, 1));
Assert("same side no flank", !GridFlanking.IsFlanked(4, 5, 1, 1, 4, 6, 1, 1, 5, 4, 1, 1));
Assert("far tokens no flank", !GridFlanking.IsFlanked(0, 0, 1, 1, 10, 10, 1, 1, 5, 4, 1, 1));

Console.WriteLine(fails == 0 ? "All passed." : $"{fails} failed.");
Environment.Exit(fails == 0 ? 0 : 1);
