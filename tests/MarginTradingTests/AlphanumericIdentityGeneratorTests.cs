// Copyright (c) 2024 Lykke Corp.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using MarginTrading.Backend.Services;
using NUnit.Framework;

namespace MarginTradingTests;

public class AlphanumericIdentityGeneratorTests
{
    [Theory]
    [TestCase("AB", "A", "A")]
    [TestCase("AB", "B", "B")]
    [TestCase("ABEFGHIJLMNQRSVWXYZ0123456789DUCKPOT", "#", "#")]
    public void Generate_FistLetter_MatchesPool(string pool, string firstLetterPool, string expected)
    {
        var id = AlphanumericIdentityGenerator.Generate(pool, firstLetterPool, length: 10);

        id.Should().StartWith(expected);
    }

    [Test]
    [Repeat(100)]
    public void Generate_DefaultConfig_AlwaysInExpectedFormat()
    {
        var id = AlphanumericIdentityGenerator.Generate();

        id.Should().MatchRegex("[A-Z0-9]{10}");
        id.Should().NotMatchRegex("[PTOCKDU]{1}[A-Z0-9]{9}");
    }
}