﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Linq;
using Common.Log;
using Lykke.MarginTrading.OrderBookService.Contracts;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orderbooks;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Backend.Services.Services;
using MarginTrading.Backend.Services.Stp;
using MarginTrading.Common.Services;
using Moq;
using NUnit.Framework;

namespace MarginTradingTests.OrderBooks
{
    
    [TestFixture]
    public class ExternalOrderbooksTests
    {
        
        #region Test Data

        private const string AssetPairId = "assetPairId";
        private const string TradingDisabledAssetPair = "tradingDisabledAssetPairId";
        
        private readonly ExternalOrderBook _orderBook1 = new ExternalOrderBook("exchange1", AssetPairId, DateTime.Now,
            new[]
            {
                new VolumePrice {Price = 10, Volume = 10},
                new VolumePrice {Price = 11, Volume = 10},
                new VolumePrice {Price = 12, Volume = 10}
            },
            new[]
            {
                new VolumePrice {Price = 9, Volume = 10},
                new VolumePrice {Price = 8, Volume = 10},
                new VolumePrice {Price = 7, Volume = 10}
            });
        
        private readonly ExternalOrderBook _orderBook2 = new ExternalOrderBook("exchange2", AssetPairId, DateTime.Now,
            new[]
            {
                new VolumePrice {Price = 100, Volume = 10},
                new VolumePrice {Price = 110, Volume = 10},
                new VolumePrice {Price = 120, Volume = 10}
            },
            new[]
            {
                new VolumePrice {Price = 90, Volume = 10},
                new VolumePrice {Price = 80, Volume = 10},
                new VolumePrice {Price = 70, Volume = 10}
            });

        private readonly ExternalOrderBook _orderBookTradingDisabled = new ExternalOrderBook("exchange1", TradingDisabledAssetPair, DateTime.Now,
            new[]
            {
                new VolumePrice {Price = 10, Volume = 10},
                new VolumePrice {Price = 11, Volume = 10},
                new VolumePrice {Price = 12, Volume = 10}
            },
            new[]
            {
                new VolumePrice {Price = 9, Volume = 10},
                new VolumePrice {Price = 8, Volume = 10},
                new VolumePrice {Price = 7, Volume = 10}
            });
        
        private readonly ExternalOrderBook _orderBookEodTradingDisabled = new ExternalOrderBook(ExternalOrderbookService.EodExternalExchange, TradingDisabledAssetPair, DateTime.Now,
            new[]
            {
                new VolumePrice {Price = 10, Volume = 10},
                new VolumePrice {Price = 11, Volume = 10},
                new VolumePrice {Price = 12, Volume = 10}
            },
            new[]
            {
                new VolumePrice {Price = 9, Volume = 10},
                new VolumePrice {Price = 8, Volume = 10},
                new VolumePrice {Price = 7, Volume = 10}
            });

        private Mock<IEventChannel<BestPriceChangeEventArgs>> _bestPricesChannelMock;
        private Mock<IDateService> _dateServiceMock;
        private Mock<IAssetPairsCache> _assetPairsCacheMock;
        private Mock<ICqrsSender> _cqrsSenderMock;
        private Mock<IIdentityGenerator> _identityGeneratorMock;
        private Mock<ILog> _logMock;
        private Mock<IAssetPairDayOffService> _assetPairDayOffMock;
        private Mock<IScheduleSettingsCacheService> _schedulteSettingsMock;


        #endregion
        
        
        #region SetUp

        [SetUp]
        public void SetUp()
        {
            _bestPricesChannelMock = new Mock<IEventChannel<BestPriceChangeEventArgs>>();
            _dateServiceMock = new Mock<IDateService>();
            _assetPairsCacheMock = new Mock<IAssetPairsCache>();
            _cqrsSenderMock = new Mock<ICqrsSender>();
            _identityGeneratorMock = new Mock<IIdentityGenerator>();
            _logMock = new Mock<ILog>();
            _assetPairDayOffMock = new Mock<IAssetPairDayOffService>();
            _assetPairDayOffMock.Setup(x => x.IsAssetTradingDisabled(It.IsAny<string>()))
                .Returns(InstrumentTradingStatus.Enabled);
            
            _assetPairDayOffMock.Setup(x => x.IsAssetTradingDisabled(It.Is<string>(x => x == TradingDisabledAssetPair)))
                .Returns(InstrumentTradingStatus.Disabled(InstrumentTradingDisabledReason.InstrumentTradingDisabled));
            _schedulteSettingsMock = new Mock<IScheduleSettingsCacheService>();
        }
        
        #endregion
        
        
        #region Helpers

        private ExternalOrderbookService GetNewOrderbooksList()
        {
            return new ExternalOrderbookService(_bestPricesChannelMock.Object, Mock.Of<IOrderBookProviderApi>(),
                _dateServiceMock.Object, new ConvertService(), _assetPairDayOffMock.Object, _schedulteSettingsMock.Object, 
                _assetPairsCacheMock.Object, _cqrsSenderMock.Object, _identityGeneratorMock.Object, _logMock.Object, 
                new MarginTradingSettings());
        }

        private void AssertErrorLogged(string expectedErrorMessage)
        {
            
        }
        
        #endregion
        
        
        #region ExternalOrderBook.GetMatchedPrice tests

        public static IEnumerable GetMatchedPriceCases
        {
            get
            {
                //no volume to match
                yield return new TestCaseData(0, OrderDirection.Buy).Returns(null);
                yield return new TestCaseData(0, OrderDirection.Sell).Returns(null);

                //volume is not fully matched
                yield return new TestCaseData(1000, OrderDirection.Buy).Returns(null);
                yield return new TestCaseData(1000, OrderDirection.Sell).Returns(null);
                
                //buy
                yield return new TestCaseData(10, OrderDirection.Buy).Returns(10.0);
                yield return new TestCaseData(16, OrderDirection.Buy).Returns(10.375);
                yield return new TestCaseData(25, OrderDirection.Buy).Returns(10.8);
                
                //sell
                yield return new TestCaseData(10, OrderDirection.Sell).Returns(9.0);
                yield return new TestCaseData(16, OrderDirection.Sell).Returns(8.625);
                yield return new TestCaseData(25, OrderDirection.Sell).Returns(8.2);
            
            }
        }
        
        [Test]
        [TestCaseSource(nameof(GetMatchedPriceCases))]
        public decimal? TestExternalOrderBook_GetMatchedPrice(int volumeToMatch, OrderDirection direction)
        {
            return _orderBook1.GetMatchedPrice(volumeToMatch, direction);
        }

        #endregion
        
        
        #region ExternalOrderBooksList.SetOrderbook tests

        public static IEnumerable GetInvalidOrderbooksCases
        {
            get
            {
                yield return new TestCaseData(
                    new ExternalOrderBook("exchange", "", DateTime.Now,
                        new []
                        {
                            new VolumePrice {Price = 100, Volume = 10}
                        },
                        new[]
                        {
                            new VolumePrice {Price = 90, Volume = 10}
                        }),
                    "AssetPairId");
                
                yield return new TestCaseData(
                    new ExternalOrderBook("", AssetPairId, DateTime.Now,
                        new[]
                        {
                            new VolumePrice {Price = 100, Volume = 10}
                        },
                        new[]
                        {
                            new VolumePrice {Price = 90, Volume = 10}
                        }),
                    "Exchange");
                
                yield return new TestCaseData(
                    new ExternalOrderBook("exchange", AssetPairId, DateTime.Now,
                        new[]
                        {
                            new VolumePrice {Price = 100, Volume = 10}
                        },
                        new VolumePrice[0]),
                    "Bids");
                
                yield return new TestCaseData(
                    new ExternalOrderBook("exchange", AssetPairId, DateTime.Now,
                        new VolumePrice[0],
                        new[]
                        {
                            new VolumePrice {Price = 90, Volume = 10}
                        }),
                    "Asks");
                
                //TODO: check sorted
                
//                //not sorted bids
//                yield return new TestCaseData(
//                    new ExternalOrderBook("exchange", AssetPairId, DateTime.Now,
//                        new VolumePrice[]
//                        {
//                            new VolumePrice {Price = 100, Volume = 10},
//                            new VolumePrice {Price = 110, Volume = 10}
//                        },
//                        new VolumePrice[]
//                        {
//                            new VolumePrice {Price = 80, Volume = 10},
//                            new VolumePrice {Price = 90, Volume = 10}
//                        }),
//                    "sorted");
//                
//                //not sorted asks
//                yield return new TestCaseData(
//                    new ExternalOrderBook("exchange", AssetPairId, DateTime.Now,
//                        new VolumePrice[]
//                        {
//                            new VolumePrice {Price = 110, Volume = 10},
//                            new VolumePrice {Price = 100, Volume = 10}
//                        },
//                        new VolumePrice[]
//                        {
//                            new VolumePrice {Price = 90, Volume = 10},
//                            new VolumePrice {Price = 80, Volume = 10}
//                        }),
//                    "sorted ");
            }
        }
        
        [Test]
        [TestCaseSource(nameof(GetInvalidOrderbooksCases))]
        public void Test_ExternalOrderBooksList_Set_InvalidOrderbook(ExternalOrderBook orderBook, string expectedErrorMessage)
        {
            //Arrange
            var orderbooks = GetNewOrderbooksList();
            
            //Act
            orderbooks.SetOrderbook(orderBook);
            
            //Assert
            _logMock.Verify(
                log => log.WriteErrorAsync(It.IsAny<string>(), It.IsAny<string>(),
                    It.Is<Exception>(ex => ex.Message.Contains(expectedErrorMessage)), null),
                Times.Once);
        }
        
        [Test]
        public void Test_ExternalOrderBooksList_Set_ValidOrderbooks()
        {
            //Arrange
            var orderbooks = GetNewOrderbooksList();
            
            //Act
            orderbooks.SetOrderbook(_orderBook1);

            _bestPricesChannelMock.Verify(
                log => log.SendEvent(orderbooks,
                    It.Is<BestPriceChangeEventArgs>(bp =>
                        bp.BidAskPair.Instrument == _orderBook1.AssetPairId &&
                        bp.BidAskPair.Ask == _orderBook1.Asks.First().Price &&
                        bp.BidAskPair.Bid == _orderBook1.Bids.First().Price)),
                Times.Once);
            
            orderbooks.SetOrderbook(_orderBook2);
            
            _bestPricesChannelMock.Verify(
                log => log.SendEvent(orderbooks,
                    It.Is<BestPriceChangeEventArgs>(bp =>
                        bp.BidAskPair.Instrument == _orderBook1.AssetPairId &&
                        bp.BidAskPair.Ask == _orderBook1.Asks.First().Price &&
                        bp.BidAskPair.Bid == _orderBook2.Bids.First().Price)),
                Times.Once);
            
            //Assert
            _logMock.Verify(
                log => log.WriteErrorAsync(It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<Exception>(), null),
                Times.Never);
            
            
        }

        /// <summary>
        /// When an instrument is disabled and the system receives an orderbook, it is ignored
        /// </summary>
        [Test]
        public void Test_ExternalOrderBooksList_Set_InstrumentDisabled_OrderbookIgnored()
        {
            //Arrange
            var orderbooks = GetNewOrderbooksList();
            
            //Act
            orderbooks.SetOrderbook(_orderBookTradingDisabled);

            //Assert
            _bestPricesChannelMock.Verify(
                log => log.SendEvent(orderbooks,
                    It.IsAny<BestPriceChangeEventArgs>()),
                Times.Never());
        }

        /// <summary>
        /// When an instrument is disabled and the system receives an End of Day orderbook, it is processed as usual
        /// </summary>
        [Test]
        public void Test_ExternalOrderBooksList_Set_EodOrderbook_InstrumentDisabled_OrderbookReceived()
        {
            //Arrange
            var orderbooks = GetNewOrderbooksList();
            
            //Act
            orderbooks.SetOrderbook(_orderBookEodTradingDisabled);

            //Assert
            _bestPricesChannelMock.Verify(
                log => log.SendEvent(orderbooks,
                    It.IsAny<BestPriceChangeEventArgs>()),
                Times.Once());
        }
        
        #endregion
        
        
        #region ExternalOrderBooksList.GetPricesForOpen tests
        
        [Test]
        public void Test_ExternalOrderBooksList_GetPricesForOpen_Buy()
        {
            //Arrange
            var orderbooks = GetNewOrderbooksList();
            orderbooks.SetOrderbook(_orderBook1);
            orderbooks.SetOrderbook(_orderBook2);
            
            //Act
            var prices = orderbooks.GetOrderedPricesForExecution(AssetPairId, 1, false);
            
            //Assert
            Assert.AreEqual(2, prices.Count);
            Assert.AreEqual(_orderBook1.ExchangeName, prices[0].source);
            Assert.AreEqual(10M, prices[0].price);
            Assert.AreEqual(_orderBook2.ExchangeName, prices[1].source);
            Assert.AreEqual(100M, prices[1].price);
        }
        
        [Test]
        public void Test_ExternalOrderBooksList_GetPricesForOpen_Sell()
        {
            //Arrange
            var orderbooks = GetNewOrderbooksList();
            orderbooks.SetOrderbook(_orderBook1);
            orderbooks.SetOrderbook(_orderBook2);

            //Act
            var prices = orderbooks.GetOrderedPricesForExecution(AssetPairId, -1, false);
            
            //Assert
            Assert.AreEqual(2, prices.Count);
            Assert.AreEqual(_orderBook1.ExchangeName, prices[1].source);
            Assert.AreEqual(9M, prices[1].price);
            Assert.AreEqual(_orderBook2.ExchangeName, prices[0].source);
            Assert.AreEqual(90M, prices[0].price);
        }
        
        [Test]
        public void Test_ExternalOrderBooksList_GetPriceForClose_Buy()
        {
            //Arrange
            var orderbooks = GetNewOrderbooksList();
            orderbooks.SetOrderbook(_orderBook1);
            orderbooks.SetOrderbook(_orderBook2);
            
            //Act
            var price = orderbooks.GetPriceForPositionClose(AssetPairId, 1, _orderBook1.ExchangeName);
            //Assert
            Assert.AreEqual(9M, price);
        }
        
        [Test]
        public void Test_ExternalOrderBooksList_GetPriceForClose_Sell()
        {
            //Arrange
            var orderbooks = GetNewOrderbooksList();
            orderbooks.SetOrderbook(_orderBook1);
            orderbooks.SetOrderbook(_orderBook2);
            
            //Act
            var price = orderbooks.GetPriceForPositionClose(AssetPairId, -1, _orderBook1.ExchangeName);
            
            //Assert
            Assert.AreEqual(10, price);
        }
        
        #endregion
        
    }
}