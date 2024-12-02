// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;

namespace MarginTrading.Backend.Services;
public static class AlphanumericIdentityGenerator
{
    // In order to prevent situations like `POSPOS` in reporting during building of some reports (which are valid, but mind-blowing)
    // it was decided to prevent ids for starting with the keywords, which are: "DIV", "UEW", "CLS", "KOM", "POS", "ORD", "TRD", "TCC"
    // first letters among them are "D","U","C","K","P","O","T", so they are removed from the [A-Z0-9] pool for the first symbol
    private const string FirstLetterPool = "ABEFGHIJLMNQRSVWXYZ" + "0123456789";
    private const string DefaultPool = FirstLetterPool + "DUCKPOT";
    private static readonly Random Random = new();
    private static readonly object LockObject = new();

    public static string Generate(string pool = DefaultPool, string firstLetterPool = FirstLetterPool, int length = 10)
    {
        lock(LockObject)
        {
            char firstLetter = firstLetterPool[Random.Next(0, firstLetterPool.Length)];
            string randomTail = new(Enumerable.Range(0, length - 1).Select(_ => pool[Random.Next(0, pool.Length)]).ToArray());
            return firstLetter + randomTail;
        }
    }
}
