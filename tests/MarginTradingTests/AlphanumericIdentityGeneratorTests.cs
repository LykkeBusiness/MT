// Copyright (c) 2024 Lykke Corp.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using MarginTrading.Backend.Services;
using NUnit.Framework;

namespace MarginTradingTests;

public class AlphanumericIdentityGeneratorTests
{
    [Theory]
    [TestCase("AB", "A", "B")]
    [TestCase("AB", "B", "A")]
    public void Generate_WithPredicate_ShallRespectPrefixRestriction(string pool, string restrictedPrefix, string expected)
    {
        var id = AlphanumericIdentityGenerator.Generate(pool, [x => x.StartsWith(restrictedPrefix)], length: 1);

        id.Should().BeEquivalentTo(expected);
    }

    [Test]
    public void Generate_WithImpossibleConditions_IgnoresRestrictions()
    {
        var id = AlphanumericIdentityGenerator.Generate("A", [x => x.StartsWith("A")], length: 1);

        id.Should().BeEquivalentTo("A");
    }
}