﻿using NetDevPack.Security.JwtSigningCredentials;
using NetDevPack.Security.JwtSigningCredentials.Interfaces;
using NetDevPack.Security.JwtSigningCredentials.Jwk;
using NetDevPack.Security.JwtSigningCredentials.Jwks;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class JsonWebKeySetManagerDependencyInjection
    {
        /// <summary>
        /// Sets the signing credential.
        /// </summary>
        /// <returns></returns>
        public static IJwksBuilder AddJwksManager(this IServiceCollection services, Action<JwksOptions> action = null)
        {
            if (action != null)
                services.Configure(action);

            services.AddScoped<IJsonWebKeyService, JwkService>();
            services.AddScoped<IJsonWebKeySetService, JwksService>();
            services.AddSingleton<IJsonWebKeyStore, InMemoryStore>();

            return new JwksBuilder(services);
        }

        /// <summary>
        /// Sets the signing credential.
        /// </summary>
        /// <returns></returns>
        public static IJwksBuilder PersistKeysInMemory(this IJwksBuilder builder)
        {
            builder.Services.AddSingleton<IJsonWebKeyStore, InMemoryStore>();

            return builder;
        }
    }
}
