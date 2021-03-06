using Bogus;
using FluentAssertions;
using NetDevPack.Security.JwtSigningCredentials.Interfaces;
using NetDevPack.Security.JwtSigningCredentials.Jwk;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Xunit;

namespace NetDevPack.Security.JwtSigningCredentials.Tests.Jwks
{
    public class KeyServiceDatabaseTest : IClassFixture<WarmupDatabaseInMemory>
    {
        private readonly AspNetGeneralContext _database;
        private readonly IJsonWebKeySetService _keyService;
        public WarmupDatabaseInMemory DatabaseInMemoryData { get; }
        public KeyServiceDatabaseTest(WarmupDatabaseInMemory databaseInMemory)
        {
            DatabaseInMemoryData = databaseInMemory;
            _keyService = DatabaseInMemoryData.Services.GetRequiredService<IJsonWebKeySetService>();
            _database = DatabaseInMemoryData.Services.GetRequiredService<AspNetGeneralContext>();

        }

        [Theory]
        [InlineData(SecurityAlgorithms.HmacSha256, KeyType.HMAC)]
        [InlineData(SecurityAlgorithms.HmacSha384, KeyType.HMAC)]
        [InlineData(SecurityAlgorithms.HmacSha512, KeyType.HMAC)]
        [InlineData(SecurityAlgorithms.RsaSha256, KeyType.RSA)]
        [InlineData(SecurityAlgorithms.RsaSha384, KeyType.RSA)]
        [InlineData(SecurityAlgorithms.RsaSha512, KeyType.RSA)]
        [InlineData(SecurityAlgorithms.RsaSsaPssSha256, KeyType.RSA)]
        [InlineData(SecurityAlgorithms.RsaSsaPssSha384, KeyType.RSA)]
        [InlineData(SecurityAlgorithms.RsaSsaPssSha512, KeyType.RSA)]
        [InlineData(SecurityAlgorithms.EcdsaSha256, KeyType.ECDsa)]
        [InlineData(SecurityAlgorithms.EcdsaSha384, KeyType.ECDsa)]
        [InlineData(SecurityAlgorithms.EcdsaSha512, KeyType.ECDsa)]
        public void ShouldSaveCryptoInDatabase(string algorithm, KeyType keyType)
        {
            _database.SecurityKeys.RemoveRange(_database.SecurityKeys.ToList());
            _database.SaveChanges();
            var options = new JwksOptions() { Algorithm = Algorithm.Create(algorithm, keyType) };
            _keyService.GetCurrent(options);

            _database.SecurityKeys.Count().Should().BePositive();
        }


        [Theory]
        [InlineData(5)]
        [InlineData(2)]
        [InlineData(6)]
        public void ShouldGenerateManyRsa(int quantity)
        {
            _database.SecurityKeys.RemoveRange(_database.SecurityKeys.ToList());
            _database.SaveChanges();
            var keysGenerated = new List<SigningCredentials>();
            for (int i = 0; i < quantity; i++)
            {
                var sign = _keyService.Generate();
                keysGenerated.Add(sign);
            }

            var current = _keyService.GetLastKeysCredentials(quantity * 4);
            foreach (var securityKey in current)
            {
                keysGenerated.Select(s => s.Key.KeyId).Should().Contain(securityKey.KeyId);
            }
        }

        [Theory]
        [InlineData(SecurityAlgorithms.HmacSha256, KeyType.HMAC)]
        [InlineData(SecurityAlgorithms.HmacSha384, KeyType.HMAC)]
        [InlineData(SecurityAlgorithms.HmacSha512, KeyType.HMAC)]
        [InlineData(SecurityAlgorithms.RsaSha256, KeyType.RSA)]
        [InlineData(SecurityAlgorithms.RsaSha384, KeyType.RSA)]
        [InlineData(SecurityAlgorithms.RsaSha512, KeyType.RSA)]
        [InlineData(SecurityAlgorithms.RsaSsaPssSha256, KeyType.RSA)]
        [InlineData(SecurityAlgorithms.RsaSsaPssSha384, KeyType.RSA)]
        [InlineData(SecurityAlgorithms.RsaSsaPssSha512, KeyType.RSA)]
        [InlineData(SecurityAlgorithms.EcdsaSha256, KeyType.ECDsa)]
        [InlineData(SecurityAlgorithms.EcdsaSha384, KeyType.ECDsa)]
        [InlineData(SecurityAlgorithms.EcdsaSha512, KeyType.ECDsa)]
        public void ShouldSaveCryptoAndRecover(string algorithm, KeyType keyType)
        {
            _database.SecurityKeys.RemoveRange(_database.SecurityKeys.ToList());
            _database.SaveChanges();

            var options = new JwksOptions() { Algorithm = Algorithm.Create(algorithm, keyType) };
            var newKey = _keyService.GetCurrent(options);

            _database.SecurityKeys.Count().Should().BePositive();

            var currentKey = _keyService.GetCurrent(options);
            newKey.Kid.Should().Be(currentKey.Kid);
        }


        [Theory]
        [InlineData(SecurityAlgorithms.RsaSsaPssSha256, KeyType.RSA)]
        [InlineData(SecurityAlgorithms.RsaSsaPssSha384, KeyType.RSA)]
        [InlineData(SecurityAlgorithms.RsaSsaPssSha512, KeyType.RSA)]
        [InlineData(SecurityAlgorithms.EcdsaSha256, KeyType.ECDsa)]
        [InlineData(SecurityAlgorithms.EcdsaSha384, KeyType.ECDsa)]
        [InlineData(SecurityAlgorithms.EcdsaSha512, KeyType.ECDsa)]
        public void ShouldSaveProbabilisticJwkRecoverAndSigning(string algorithm, KeyType keyType)
        {
            _database.SecurityKeys.RemoveRange(_database.SecurityKeys.ToList());
            _database.SaveChanges();

            var options = new JwksOptions() { Algorithm = Algorithm.Create(algorithm, keyType) };

            var handler = new JsonWebTokenHandler();
            var now = DateTime.Now;

            // Generate right now and in memory
            var newKey = _keyService.GetCurrent(options);

            // recovered from database
            var currentKey = _keyService.GetCurrent(options);

            newKey.Kid.Should().Be(currentKey.Kid);
            var claims = new ClaimsIdentity(GenerateClaim().Generate(5));
            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = "me",
                Audience = "you",
                IssuedAt = now,
                NotBefore = now,
                Expires = now.AddMinutes(5),
                Subject = claims,
                SigningCredentials = newKey
            };
            var descriptorFromDb = new SecurityTokenDescriptor
            {
                Issuer = "me",
                Audience = "you",
                IssuedAt = now,
                NotBefore = now,
                Expires = now.AddMinutes(5),
                Subject = claims,
                SigningCredentials = currentKey
            };

            var jwt1 = handler.CreateToken(descriptor);
            var jwt2 = handler.CreateToken(descriptorFromDb);

            jwt1.Should().NotBe(jwt2);
        }

        [Theory]
        [InlineData(SecurityAlgorithms.HmacSha256, KeyType.HMAC)]
        [InlineData(SecurityAlgorithms.HmacSha384, KeyType.HMAC)]
        [InlineData(SecurityAlgorithms.HmacSha512, KeyType.HMAC)]
        [InlineData(SecurityAlgorithms.RsaSha256, KeyType.RSA)]
        [InlineData(SecurityAlgorithms.RsaSha384, KeyType.RSA)]
        [InlineData(SecurityAlgorithms.RsaSha512, KeyType.RSA)]
        public void ShouldSaveDeterministicJwkRecoverAndSigning(string algorithm, KeyType keyType)
        {
            _database.SecurityKeys.RemoveRange(_database.SecurityKeys.ToList());
            _database.SaveChanges();

            var options = new JwksOptions() { Algorithm = Algorithm.Create(algorithm, keyType) };

            var handler = new JsonWebTokenHandler();
            var now = DateTime.Now;

            // Generate right now and in memory
            var newKey = _keyService.GetCurrent(options);

            // recovered from database
            var currentKey = _keyService.GetCurrent(options);

            newKey.Kid.Should().Be(currentKey.Kid);
            var claims = new ClaimsIdentity(GenerateClaim().Generate(5));
            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = "me",
                Audience = "you",
                IssuedAt = now,
                NotBefore = now,
                Expires = now.AddMinutes(5),
                Subject = claims,
                SigningCredentials = newKey
            };
            var descriptorFromDb = new SecurityTokenDescriptor
            {
                Issuer = "me",
                Audience = "you",
                IssuedAt = now,
                NotBefore = now,
                Expires = now.AddMinutes(5),
                Subject = claims,
                SigningCredentials = currentKey
            };

            var jwt1 = handler.CreateToken(descriptor);
            var jwt2 = handler.CreateToken(descriptorFromDb);

            jwt1.Should().Be(jwt2);
        }


        [Theory]
        [InlineData(SecurityAlgorithms.HmacSha256, KeyType.HMAC)]
        [InlineData(SecurityAlgorithms.HmacSha384, KeyType.HMAC)]
        [InlineData(SecurityAlgorithms.HmacSha512, KeyType.HMAC)]
        [InlineData(SecurityAlgorithms.RsaSha256, KeyType.RSA)]
        [InlineData(SecurityAlgorithms.RsaSha384, KeyType.RSA)]
        [InlineData(SecurityAlgorithms.RsaSha512, KeyType.RSA)]
        public void ShouldGenerateCurrentBasedInOptions(string algorithm, KeyType keyType)
        {
            _database.SecurityKeys.RemoveRange(_database.SecurityKeys.ToList());
            _database.SaveChanges();
            var options = new JwksOptions() { Algorithm = Algorithm.Create(algorithm, keyType) };
            var newKey = _keyService.GetCurrent(options);
            newKey.Algorithm.Should().Be(algorithm);

        }


        [Fact]
        public void ShouldGenerateFiveECDsa()
        {
            var keysGenerated = new List<SigningCredentials>();
            for (int i = 0; i < 5; i++)
            {
                var sign = _keyService.Generate(new JwksOptions() { Algorithm = Algorithm.ES512, KeyPrefix = $"{nameof(JsonWebKeySetServiceTests)}_" });
                keysGenerated.Add(sign);
            }

            var current = _keyService.GetLastKeysCredentials(50);
            foreach (var key in keysGenerated)
            {
                current.Where(w => w.Alg == SecurityAlgorithms.EcdsaSha512).Select(s => s.KeyId).Should().Contain(key.Kid);
            }
        }


        public Faker<Claim> GenerateClaim()
        {
            return new Faker<Claim>().CustomInstantiator(f => new Claim(f.Internet.DomainName(), f.Lorem.Text()));
        }

        [Theory]
        [InlineData(SecurityAlgorithms.HmacSha256, KeyType.HMAC)]
        [InlineData(SecurityAlgorithms.HmacSha384, KeyType.HMAC)]
        [InlineData(SecurityAlgorithms.HmacSha512, KeyType.HMAC)]
        [InlineData(SecurityAlgorithms.RsaSha256, KeyType.RSA)]
        [InlineData(SecurityAlgorithms.RsaSha384, KeyType.RSA)]
        [InlineData(SecurityAlgorithms.RsaSha512, KeyType.RSA)]
        [InlineData(SecurityAlgorithms.RsaSsaPssSha256, KeyType.RSA)]
        [InlineData(SecurityAlgorithms.RsaSsaPssSha384, KeyType.RSA)]
        [InlineData(SecurityAlgorithms.RsaSsaPssSha512, KeyType.RSA)]
        [InlineData(SecurityAlgorithms.EcdsaSha256, KeyType.ECDsa)]
        [InlineData(SecurityAlgorithms.EcdsaSha384, KeyType.ECDsa)]
        [InlineData(SecurityAlgorithms.EcdsaSha512, KeyType.ECDsa)]
        public void ShouldRemovePrivateAndUpdate(string algorithm, KeyType keyType)
        {
            var alg = Algorithm.Create(algorithm, keyType);
            var key = _keyService.Generate(new JwksOptions() { KeyPrefix = "ShouldGenerateManyRsa_", Algorithm = alg });
            var privateKey = new SecurityKeyWithPrivate();
            privateKey.SetParameters(key.Key, alg);
            _database.SecurityKeys.Add(privateKey);
            _database.SaveChanges();
            /*Remove private*/
            privateKey.SetParameters();
            _database.SecurityKeys.Update(privateKey);
            _database.SaveChanges();

        }
    }
}
