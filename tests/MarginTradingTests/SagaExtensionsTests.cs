// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using Common.Log;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Extensions;
using MarginTrading.Common.Services;
using Moq;
using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    public class SagaExtensionsTests
    {
        [Test]
        public void TestSwitchState()
        {
            LogLocator.CommonLog = Mock.Of<ILog>();
            
            WithdrawalFreezeOperationData data = null;
            Assert.Throws<InvalidOperationException>(() =>
                data.SwitchState(OperationState.Initiated, OperationState.Started));
            
            data = new WithdrawalFreezeOperationData {State = OperationState.Initiated};

            Assert.Throws<InvalidOperationException>(() =>
                data.SwitchState(OperationState.Started, OperationState.Finished));
            
            Assert.IsTrue(data.SwitchState(OperationState.Initiated, OperationState.Started));
            
            Assert.IsFalse(data.SwitchState(OperationState.Initiated, OperationState.Started));
            
            Assert.IsTrue(data.SwitchState(OperationState.Started, OperationState.Finished));

            Assert.AreEqual(OperationState.Finished, data.State);
        }
    }
}