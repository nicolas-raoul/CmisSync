//-----------------------------------------------------------------------
// <copyright file="Password.cs" company="GRAU DATA AG">
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

namespace CmisSync.Lib.Config {
    using System;

    /// <summary>
    /// Password class stores the given password obfuscated
    /// </summary>
    [Serializable]
    public class Password {
        private string password = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="Password"/> class with the given password.
        /// </summary>
        /// <param name="password">as plain text</param>
        public Password(string password) {
            this.password = Crypto.Obfuscate(password);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Password"/> class without setting a password.
        /// </summary>
        public Password() {
        }

        /// <summary>
        /// Gets or sets the internal saved and obfuscated password
        /// </summary>
        public string ObfuscatedPassword {
            get { return this.password; }
            set { this.password = value; }
        }

        /// <summary>
        /// Implizit contructor for passing a plain text string as password
        /// </summary>
        /// <param name="value">plain text password</param>
        /// <returns></returns>
        public static implicit operator Password(string value) {
            return new Password(value);
        }

        /// <summary>
        /// Returns the password as plain text
        /// </summary>
        /// <returns>plain text password</returns>
        public override string ToString() {
            return this.password == null ? null : Crypto.Deobfuscate(this.password);
        }
    }
}