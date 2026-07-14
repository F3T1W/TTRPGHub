using TTRPGHub.Services;

var (bowRange, bowReach) = Pf2eLookups.ParseWeaponRangeFromTraitsString("deadly-d10", 60);
var (glaiveRange, glaiveReach) = Pf2eLookups.ParseWeaponRangeFromTraitsString("deadly-d8, forceful, reach");
var (spearRange, spearReach) = Pf2eLookups.ParseWeaponRangeFromTraitsString("monk, thrown-20");

var checks = new (string Name, bool Ok)[]
{
    ("shortbow range", bowRange == 60 && bowReach is null),
    ("glaive reach", glaiveRange is null && glaiveReach == 10),
    ("spear thrown", spearRange == 20),
    ("default melee", Pf2eLookups.AttackMaxRangeFeet(null, null) == 5),
    ("ranged max", Pf2eLookups.AttackMaxRangeFeet(60, null) == 60),
};

var failed = checks.Where(c => !c.Ok).Select(c => c.Name).ToList();
if (failed.Count > 0)
{
    Console.Error.WriteLine("FAIL: " + string.Join(", ", failed));
    return 1;
}

Console.WriteLine("OK: weapon range parsing");
return 0;
