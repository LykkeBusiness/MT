﻿// Copyright (c) 2019 Lykke Corp.

using Microsoft.AspNetCore.Authentication;

namespace MarginTrading.Backend.Middleware
{
    public class KeyAuthOptions : AuthenticationSchemeOptions
    {
        public const string DefaultHeaderName = "api-key";
        public const string AuthenticationScheme = "Automatic";
    }
}
