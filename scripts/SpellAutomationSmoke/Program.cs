using System.Text.Json;
using TTRPGHub.Services;

var damage = Pf2eSpellAutomation.ParseDamage("""
    {"instances":[{"formula":"6d6","type":"fire","kinds":["damage"],"applyMod":false}]}
    """);
var heightening = Pf2eSpellAutomation.ParseHeightening("""
    {"type":"interval","interval":1,"damage":["2d6"]}
    """);

var baseDamage = Pf2eSpellAutomation.ResolveDamage(damage!, heightening, 3, 3, 0).Single();
var heightened = Pf2eSpellAutomation.ResolveDamage(damage!, heightening, 3, 5, 0).Single();

var checks = new (string Name, bool Ok)[]
{
    ("base formula", baseDamage.Expression == "6d6"),
    ("heightened formula", heightened.Expression == "10d6"),
    ("heighten steps", Pf2eSpellAutomation.HeightenSteps(3, 5, heightening) == 2),
};

var failed = checks.Where(c => !c.Ok).Select(c => c.Name).ToList();
if (failed.Count > 0)
{
    Console.Error.WriteLine("FAIL: " + string.Join(", ", failed));
    Console.Error.WriteLine($"base={baseDamage.Expression}, heightened={heightened.Expression}");
    return 1;
}

Console.WriteLine($"OK: fireball 3rd={baseDamage.Expression}, 5th={heightened.Expression}");
return 0;
