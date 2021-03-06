//-----------------------------------------------------------------------
// <copyright file="AuthProviderFactoryTest.cs" company="GRAU DATA AG">
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General private License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//   GNU General private License for more details.
//
//   You should have received a copy of the GNU General private License
//   along with this program. If not, see http://www.gnu.org/licenses/.
//
// </copyright>
//-----------------------------------------------------------------------

namespace TestLibrary.CmisTests.AuthenticationTests {
    using System;

    using CmisSync.Lib.Cmis;
    using CmisSync.Lib.Config;

    using DBreeze;

    using Newtonsoft.Json;

    using NUnit.Framework;

    [TestFixture, Category("Fast")]
    public class AuthProviderFactoryTest : IDisposable {
        private readonly Uri url = new Uri("https://example.com");
        private DBreezeEngine engine;

        [TestFixtureSetUp]
        public void InitCustomSerializator() {
            // Use Newtonsoft.Json as Serializator
            DBreeze.Utils.CustomSerializator.Serializator = JsonConvert.SerializeObject;
            DBreeze.Utils.CustomSerializator.Deserializator = JsonConvert.DeserializeObject;
        }

        [SetUp]
        public void SetUp() {
            this.engine = new DBreezeEngine(new DBreezeConfiguration { Storage = DBreezeConfiguration.eStorage.MEMORY });
        }

        [TearDown]
        public void TearDown() {
            this.engine.Dispose();
            this.engine = null;
        }

        [Test]
        public void CreateBasicAuthProvider() {
            var provider = AuthProviderFactory.CreateAuthProvider(AuthenticationType.BASIC, this.url, this.engine);
            Assert.That(provider, Is.TypeOf<PersistentStandardAuthenticationProvider>());
        }

        [Test]
        public void CreateNtlmAuthProvider() {
            var provider = AuthProviderFactory.CreateAuthProvider(AuthenticationType.NTLM, this.url, this.engine);
            Assert.That(provider, Is.TypeOf<PersistentNtlmAuthenticationProvider>());
        }

        [Test]
        public void CreateKerberosAuthProvider() {
            var provider = AuthProviderFactory.CreateAuthProvider(AuthenticationType.KERBEROS, this.url, this.engine);
            Assert.That(provider, Is.TypeOf<PersistentNtlmAuthenticationProvider>());
        }

        [Test]
        public void CreateUnimplementedAuthTypeReturnDefaultAuthProvider() {
            var provider = AuthProviderFactory.CreateAuthProvider(AuthenticationType.SHIBBOLETH, this.url, this.engine);
            Assert.That(provider, Is.TypeOf<StandardAuthenticationProviderWrapper>());
        }

        #region boilerplatecode
        public void Dispose() {
            if (this.engine != null) {
                this.engine.Dispose();
                this.engine = null;
            }
        }
        #endregion
    }
}