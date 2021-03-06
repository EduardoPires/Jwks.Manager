using Bogus;
using FluentAssertions;
using NetDevPack.Security.JwtSigningCredentials.Interfaces;
using NetDevPack.Security.JwtSigningCredentials.Jwk;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Security.Claims;
using Xunit;

namespace NetDevPack.Security.JwtSigningCredentials.Tests.Jwks
{
    public class KeyServiceInMemoryTest : IClassFixture<WarmupInMemoryStore>
    {
        private readonly IJsonWebKeySetService _keyService;
        private readonly IJsonWebKeyStore _jsonWebKeyStore;
        public WarmupInMemoryStore InMemoryWarmupData { get; }
        public KeyServiceInMemoryTest(WarmupInMemoryStore inMemoryWarmup)
        {
            InMemoryWarmupData = inMemoryWarmup;
            _keyService = InMemoryWarmupData.Services.GetRequiredService<IJsonWebKeySetService>();
            _jsonWebKeyStore = InMemoryWarmupData.Services.GetRequiredService<IJsonWebKeyStore>();

        }

        [Fact]
        public void ShouldSaveCryptoInDatabase()
        {
            _keyService.GetCurrent();

            _keyService.GetLastKeysCredentials(5).Count.Should().BePositive();
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
        public void ShouldGenerate(string algorithm, KeyType keyType)
        {
            _keyService.Generate(new JwksOptions() { KeyPrefix = "ShouldGenerateManyRsa_", Algorithm = Algorithm.Create(algorithm, keyType) });
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
            _jsonWebKeyStore.Save(privateKey);

            /*Remove private*/
            privateKey.SetParameters();
            _jsonWebKeyStore.Update(privateKey);

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

            var options = new JwksOptions() { Algorithm = Algorithm.Create(algorithm, keyType) };
            var newKey = _keyService.GetCurrent(options);

            _keyService.GetLastKeysCredentials(5).Count.Should().BePositive();

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


        public Faker<Claim> GenerateClaim()
        {
            return new Faker<Claim>().CustomInstantiator(f => new Claim(f.Internet.DomainName(), f.Lorem.Text()));
        }
    }
}