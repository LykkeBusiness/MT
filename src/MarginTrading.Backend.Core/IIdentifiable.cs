using System;

namespace MarginTrading.Backend.Core;

public interface IIdentifiable
{
    Guid Id { get; }
}
