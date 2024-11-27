// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;

using Serilog;

namespace MarginTrading.Backend.Services;
public static class AlphanumericIdentityGenerator
{
    // we must not generate id's starting with these keywords, it causes situations like `POSPOS` in reporting. Valid situations, but mind-blowing.
    private static readonly string[] RestrictedBeginnings = ["POS", "TRD", "ORD", "CLS", "KOM", "DIV", "UEW", "TCC"];
    private const string DefaultPool = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private static readonly Func<string, bool>[] DefaultRestrictions = [x => RestrictedBeginnings.Any(x.StartsWith)];

    private static readonly Random Random = new();
    private static readonly object LockObject = new();
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(AlphanumericIdentityGenerator));

    public static string Generate(string pool = DefaultPool, Func<string, bool>[] restrictions = null, int length = 10)
    {
        lock(LockObject)
        {
            var restrictionsToApply = restrictions ?? DefaultRestrictions;
            string resultingString;
            var attempts = 0;
            do
            {
                resultingString = new string(Enumerable.Range(0, length).Select(_ => pool[Random.Next(0, pool.Length)]).ToArray());

                if (attempts++ > 100)
                {
                    Log.Warning("Restriction validation failed 100 times in a row. Check calling code, restrictions are almost impossible to be met.");
                    break;
                }
            } while (restrictionsToApply.Any(x => x.Invoke(resultingString)));
            return resultingString;
        }
    }
}
