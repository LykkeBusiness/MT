﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.SqlRepositories.Entities
{
    public class IdentityEntity : TableEntity
    {
        public const string GenerateRowKey = "Id";

        public long Value { get; set; }

        public static string GeneratePartitionKey(string entityType)
        {
            return entityType;
        }

        public static IdentityEntity Create(string entityType)
        {
            return new IdentityEntity
            {
                PartitionKey = GeneratePartitionKey(entityType),
                RowKey = GenerateRowKey,
                Value = 1
            };
        }
    }
}