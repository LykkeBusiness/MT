﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.Messages;
using MarginTrading.Backend.Services.Infrastructure;
using AssetPairKey = System.ValueTuple<string, string, string>;

namespace MarginTrading.Backend.Services.AssetPairs
{
    /// <summary>
    /// Cashes data about assets in the backend app.
    /// </summary>
    public class AssetPairsCache : IAssetPairsInitializableCache
    {
        public const int DefaultAssetPairAccuracy = 5;
        
        private Dictionary<string, IAssetPair> _assetPairs = new Dictionary<string, IAssetPair>();

        private readonly ReaderWriterLockSlim _readerWriterLockSlim = new ReaderWriterLockSlim();

        private ICachedCalculation<IReadOnlyDictionary<AssetPairKey, IAssetPair>> _assetPairsByAssets;
        private ICachedCalculation<ImmutableHashSet<string>> _assetPairsIds;

        private Func<Dictionary<string, IAssetPair>, Dictionary<string, IAssetPair>, bool>
            CacheChangedCondition => (first, second) => first.Count == second.Count
                                                        && first.All(x =>
                                                            second.ContainsKey(x.Key) && x.Value.Equals(second[x.Key]));

        public AssetPairsCache()
        {
            _assetPairsByAssets = GetAssetPairsByAssetsCache();
            _assetPairsIds = Calculate.Cached(() => _assetPairs, CacheChangedCondition, p => p.Keys.ToImmutableHashSet());
        }

        public IAssetPair GetAssetPairById(string assetPairId)
        {
            _readerWriterLockSlim.EnterReadLock();

            try
            { 
                return _assetPairs.TryGetValue(assetPairId, out var result)
                    ? result
                    : throw new AssetPairNotFoundException(assetPairId,
                        string.Format(MtMessages.InstrumentNotFoundInCache, assetPairId));
            }
            finally
            {
                _readerWriterLockSlim.ExitReadLock();
            }
        }

        public IAssetPair GetAssetPairByIdOrDefault(string assetPairId)
        {
            _readerWriterLockSlim.EnterReadLock();

            try
            {
                return string.IsNullOrWhiteSpace(assetPairId) 
                    ? default 
                    : _assetPairs.GetValueOrDefault(assetPairId);
            }
            finally
            {
                _readerWriterLockSlim.ExitReadLock();
            }
        }

        public IEnumerable<IAssetPair> GetAll()
        {
            _readerWriterLockSlim.EnterReadLock();

            try
            {
                return _assetPairs.Values;
            }
            finally
            {
                _readerWriterLockSlim.ExitReadLock();
            }
        }

        public ImmutableHashSet<string> GetAllIds()
        {
            _readerWriterLockSlim.EnterReadLock();

            try
            {
                return _assetPairsIds.Get();
            }
            finally
            {
                _readerWriterLockSlim.ExitReadLock();
            }
        }

        /// <summary>
        /// Adds new item item if does not exist, otherwise updates
        /// </summary>
        /// <param name="assetPair"></param>
        /// <returns>true if added, false if updated</returns>
        public bool AddOrUpdate(IAssetPair assetPair)
        {
            if (assetPair == null)
                throw new ArgumentNullException(nameof(assetPair));
            
            _readerWriterLockSlim.EnterWriteLock();
            var itemExists = _assetPairs.ContainsKey(assetPair.Id);

            try
            {
                if (itemExists)
                {
                    _assetPairs.Remove(assetPair.Id);
                }
                
                _assetPairs.Add(assetPair.Id, assetPair);
                
                _assetPairsByAssets = GetAssetPairsByAssetsCache();
                _assetPairsIds = Calculate.Cached(() => _assetPairs, CacheChangedCondition, p => p.Keys.ToImmutableHashSet());

                return !itemExists;
            }
            finally
            {
                _readerWriterLockSlim.ExitWriteLock();
            }
        }

        public void Remove(string assetPairId)
        {
            _readerWriterLockSlim.EnterWriteLock();

            try
            {
                if (_assetPairs.ContainsKey(assetPairId))
                {
                    _assetPairs.Remove(assetPairId);
                    _assetPairsByAssets = GetAssetPairsByAssetsCache();
                    _assetPairsIds = Calculate.Cached(() => _assetPairs, CacheChangedCondition, p => p.Keys.ToImmutableHashSet());
                }
            }
            finally
            {
                _readerWriterLockSlim.ExitWriteLock();
            }
        }

        public bool TryGetAssetPairQuoteSubst(string substAsset, string instrument, string legalEntity, 
            out IAssetPair assetPair)
        {
            _readerWriterLockSlim.EnterReadLock();

            try
            {
                assetPair = null;
                var baseAssetPair = GetAssetPairByIdOrDefault(instrument);
                if (baseAssetPair == null)
                    return false;
            
                return _assetPairsByAssets.Get().TryGetValue(
                    GetAssetPairKey(baseAssetPair.BaseAssetId, substAsset, legalEntity), out assetPair);
            }
            finally
            {
                _readerWriterLockSlim.ExitReadLock();
            }
        }

        public IAssetPair FindAssetPair(string asset1, string asset2, string legalEntity)
        {
            _readerWriterLockSlim.EnterReadLock();

            try
            {
                var key = GetAssetPairKey(asset1, asset2, legalEntity);
            
                if (_assetPairsByAssets.Get().TryGetValue(key, out var result))
                    return result;

                throw new InstrumentByAssetsNotFoundException(asset1, asset2, legalEntity);
            }
            finally
            {
                _readerWriterLockSlim.ExitReadLock();
            }
        }

        void IAssetPairsInitializableCache.InitPairsCache(Dictionary<string, IAssetPair> instruments)
        {
            _readerWriterLockSlim.EnterWriteLock();

            try
            {
                _assetPairs = instruments.Where(kvp => kvp.Value != null).ToDictionary();
            }
            finally
            {
                _readerWriterLockSlim.ExitWriteLock();
            }
        }

        private ICachedCalculation<IReadOnlyDictionary<AssetPairKey, IAssetPair>> GetAssetPairsByAssetsCache()
        {
             // TODO: (anokhin) FX and trading pairs should be separated and we should use only FX here. For now, knowing that we receive Trading + FX, we are hacking that by reverting the order of the items to use FX rates first
            return Calculate.Cached(() => _assetPairs, CacheChangedCondition,
                pairs =>
                {
                    var pairsEnum = pairs.Values.Reverse().SelectMany(p => new[]
                    {
                        (GetAssetPairKey(p.BaseAssetId, p.QuoteAssetId, p.LegalEntity), p),
                        (GetAssetPairKey(p.QuoteAssetId, p.BaseAssetId, p.LegalEntity), p),
                    });
                    return pairsEnum.DistinctBy(p => p.Item1).ToDictionary();
                });
        }

        private static AssetPairKey GetAssetPairKey(string asset1, string asset2, string legalEntity)
        {
            return (asset1, asset2, legalEntity);
        }
    }
}