﻿using System.Security;
using System.Threading.Tasks;
using Catalyst.Abstractions.Options;
using Catalyst.Core.Modules.Dfs;
using Catalyst.Core.Modules.Keystore;
using Catalyst.TestUtils;
using Microsoft.Extensions.Options;
using MultiFormats;
using Org.BouncyCastle.Crypto.Parameters;
using Xunit;
using Xunit.Abstractions;

namespace Catalyst.Core.Lib.Tests.UnitTests.Cryptography
{
    public class Rfc8410Test : FileSystemBasedTest
    {
        private readonly KeyChain keyChain;
        
        public Rfc8410Test(ITestOutputHelper output) : base(output)
        {
            keyChain = new KeyChain(FileSystem.Path.ToString())
            {
                Options = new DfsOptions().KeyChain
            };
            var securePassword = new SecureString();

            foreach (char c in "mypassword")
                securePassword.AppendChar(c);

            securePassword.MakeReadOnly();
            keyChain.SetPassphraseAsync(securePassword).ConfigureAwait(false);
        }
        
        [Fact]
        public async Task ReadPrivateKey()
        {
            string alice1 = @"-----BEGIN PRIVATE KEY-----
MC4CAQAwBQYDK2VwBCIEINTuctv5E1hK1bbY8fdp+K06/nwoy/HU++CXqI9EdVhC
-----END PRIVATE KEY-----
";
            var key = await keyChain.ImportAsync("alice1", alice1, null);
            try
            {
                var priv = (Ed25519PrivateKeyParameters) await keyChain.GetPrivateKeyAsync("alice1");
                Assert.True(priv.IsPrivate);
                Assert.Equal("d4ee72dbf913584ad5b6d8f1f769f8ad3afe7c28cbf1d4fbe097a88f44755842",
                    priv.GetEncoded().ToHexString());

                var pub = priv.GeneratePublicKey();
                Assert.False(pub.IsPrivate);
                Assert.Equal("19bf44096984cdfe8541bac167dc3b96c85086aa30b6b6cb0c5c38ad703166e1",
                    pub.GetEncoded().ToHexString());
            }
            finally
            {
                await keyChain.RemoveAsync("alice1");
            }
        }

        [Fact]
        public async Task ReadPrivateAndPublicKey()
        {
            string alice1 = @"-----BEGIN PRIVATE KEY-----
MHICAQEwBQYDK2VwBCIEINTuctv5E1hK1bbY8fdp+K06/nwoy/HU++CXqI9EdVhC
oB8wHQYKKoZIhvcNAQkJFDEPDA1DdXJkbGUgQ2hhaXJzgSEAGb9ECWmEzf6FQbrB
Z9w7lshQhqowtrbLDFw4rXAxZuE=
-----END PRIVATE KEY-----
";
            var key = await keyChain.ImportAsync("alice1", alice1, null);
            try
            {
                var priv = (Ed25519PrivateKeyParameters) await keyChain.GetPrivateKeyAsync("alice1");
                Assert.True(priv.IsPrivate);
                Assert.Equal("d4ee72dbf913584ad5b6d8f1f769f8ad3afe7c28cbf1d4fbe097a88f44755842",
                    priv.GetEncoded().ToHexString());

                var pub = priv.GeneratePublicKey();
                Assert.False(pub.IsPrivate);
                Assert.Equal("19bf44096984cdfe8541bac167dc3b96c85086aa30b6b6cb0c5c38ad703166e1",
                    pub.GetEncoded().ToHexString());
            }
            finally
            {
                await keyChain.RemoveAsync("alice1");
            }
        }
    }
}
