// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;

namespace MarginTrading.Backend.Services;
public static class AlphanumericIdentityGenerator
{
    // we must not generate id's starting with PTOCKDU letters
    // It causes situations like `POSPOS` in reporting. Valid situations, but mind-blowing.
    private const string FirstLetterPool = "ABEFGHIJLMNQRSVWXYZ0123456789";
    private const string DefaultPool     = "ABEFGHIJLMNQRSVWXYZ0123456789" + "PTOCKDU";
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
