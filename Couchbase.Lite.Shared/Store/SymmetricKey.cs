﻿//
//  SymmetricKey.cs
//
//  Author:
//  	Jim Borden  <jim.borden@couchbase.com>
//
//  Copyright (c) 2015 Couchbase, Inc All rights reserved.
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//  http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
//
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using Couchbase.Lite.Util;
using Microsoft.IO;

#if NET_3_5
using Rackspace.Threading;
#endif

namespace Couchbase.Lite.Store
{
    /// <summary>
    /// Type of block returned by SymmetricKey.CreateEncryptor.
    /// This block can be called repeatedly with input data and returns additional output data.
    /// At EOF, the block should be called with a null parameter, and
    /// it will return the remaining encrypted data from its buffer.
    /// </summary>
    public delegate byte[] CryptorBlock(byte[] input);

    /// <summary>
    /// Basic AES encryption. Uses a 256-bit (32-byte) key.
    /// </summary>
    public sealed class SymmetricKey 
    #if !NET_3_5
        : IDisposable
    #endif
    {

        #region Constants

        /// <summary>
        /// Number of bytes in a 256-bit key
        /// </summary>
        [Obsolete("Use DataSize")]
        public const int DATA_SIZE = 32;

        /// <summary>
        /// Number of bytes in a 256-bit key
        /// </summary>
        public static readonly int DataSize = 32;

        /// <summary>
        /// The data type associated with encrypted content
        /// </summary>
        [Obsolete("Use EncryptedContentType")]
        public const string ENCRYPTED_CONTENT_TYPE = "application/x-beanbag-aes-256";

        /// <summary>
        /// The data type associated with encrypted content
        /// </summary>
        public static readonly string EncryptedContentType = "application/x-beanbag-aes-256";

        private static readonly string Tag = typeof(SymmetricKey).Name;

        private const int KEY_SIZE = 32;
        private const int BLOCK_SIZE = 16;
        private const int IV_SIZE = BLOCK_SIZE;
        private const int CHECKSUM_SIZE = sizeof(uint);
        private const string DEFAULT_SALT = "Salty McNaCl";
        private const int DEFAULT_PBKDF_ROUNDS = 64000;

        #endregion

        #region Private Members

        private Aes _cryptor;

        #endregion

        #region Properties

        /// <summary>
        /// The SymmetricKey's key data; can be used to reconstitute it.
        /// </summary>
        public byte[] KeyData { 
            get {
                return _cryptor.Key;
            }
        }

        /// <summary>
        /// The key data encoded as hex.
        /// </summary>
        public string HexData { 
            get {
                return BitConverter.ToString(KeyData).Replace("-", String.Empty).ToLower();
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an instance with a random key.
        /// </summary>
        public SymmetricKey() 
        {
            InitCryptor();
            _cryptor.GenerateKey();
        }

        /// <summary>
        /// Creates an instance with a key derived from a password.
        /// </summary>
        /// <param name="password">The password.</param>
        /// <param name="salt">A fixed data blob that perturbs the generated key. 
        /// Should be kept fixed for any particular app, but doesn't need to be secret.</param>
        /// <param name="rounds">The number of rounds of hashing to perform. 
        /// More rounds is more secure but takes longer.</param>
        public SymmetricKey(string password, byte[] salt, int rounds) 
        {
            if(password == null) {
                Log.To.Database.E(Tag, "password cannot be null in ctor, throwing...");
                throw new ArgumentNullException("password");
            }

            if (salt == null) {
                Log.To.Database.E(Tag, "salt cannot be null in ctor, throwing...");
                throw new ArgumentNullException("salt");
            }

            if(salt.Length <= 4) {
                Log.To.Database.E(Tag, "salt cannot be less than 4 bytes in ctor, throwing...");
                throw new ArgumentOutOfRangeException("salt", "Value is too short");
            }
            if(rounds <= 200) {
                Log.To.Database.E(Tag, "rounds cannot be <= 200 in ctor, throwing...");
                throw new ArgumentOutOfRangeException("rounds", "Insufficient rounds");
            }

            InitCryptor();
            Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(password, salt);
            pbkdf2.IterationCount = rounds;
            _cryptor.Key = pbkdf2.GetBytes(KEY_SIZE);
        }

        /// <summary>
        /// Creates an instance with a key derived from a password, using default salt and rounds.
        /// </summary>
        public SymmetricKey(string password) : 
        this(password, Encoding.UTF8.GetBytes(DEFAULT_SALT), DEFAULT_PBKDF_ROUNDS) {}

        /// <summary>
        /// Creates an instance from existing key data.
        /// </summary>
        public SymmetricKey(byte[] keyData) 
        {
            InitCryptor();
            if(keyData == null || keyData.Length != KEY_SIZE) {
                throw new ArgumentOutOfRangeException("keyData", "Value is incorrect size");
            }

            _cryptor.Key = keyData;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates a new SymmetricKey using the supplied data
        /// </summary>
        /// <param name="keyOrPassword">A password as a string or a byte
        /// IEnumerable containig key data</param>
        internal static SymmetricKey Create(object keyOrPassword)
        {
            if (keyOrPassword == null) {
                return null;
            }

            var password = keyOrPassword as string;
            if(password != null) {
                return new SymmetricKey(password);
            }

            var data = keyOrPassword as IEnumerable<byte>;
            if (data == null) {
                Log.To.Database.E(Tag, "Invalid keyOrPassword type ({0}) received, must be string " +
                "or IEnumerable<byte>, throwing...", keyOrPassword.GetType().FullName);
                throw new InvalidDataException("keyOrPassword must be either string or IEnumerable<byte>");
            }

            return new SymmetricKey(data.ToArray());
        }

        /// <summary>
        /// Encrypts a data blob.
        /// The output consists of a 16-byte random initialization vector,
        /// followed by PKCS7-padded ciphertext. 
        /// </summary>
        public byte[] EncryptData(byte[] data)
        {
            if (data == null) {
                return null;
            }

            byte[] encrypted = null;
            _cryptor.GenerateIV();
            using(var ms = RecyclableMemoryStreamManager.SharedInstance.GetStream())
            using(var cs = new CryptoStream(ms, _cryptor.CreateEncryptor(), CryptoStreamMode.Write)) {
                ms.Write(_cryptor.IV, 0, IV_SIZE);
                cs.Write(data, 0, data.Length);
                cs.FlushFinalBlock();
                encrypted = ms.GetBuffer().Take((int)ms.Length).ToArray();
            }

            return encrypted;
        }

        /// <summary>
        /// Decrypts data encoded by encryptData.
        /// </summary>
        public byte[] DecryptData(byte[] encryptedData)
        {
            var buffer = new List<byte>();
            using(var ms = RecyclableMemoryStreamManager.SharedInstance.GetStream("SymmetricKey", 
                encryptedData, 0, encryptedData.Length))
            using(var cs = DecryptStream(ms)) {
                int next;
                while((next = cs.ReadByte()) != -1) {
                    buffer.Add((byte)next);
                }
            }

            return buffer.ToArray();
        }

        /// <summary>
        /// Streaming decryption.
        /// </summary>
        public Stream DecryptStream(Stream stream)
        {
            if(stream == null || !stream.CanRead) {
                Log.To.Database.E(Tag, "Unable to read from stream, throwing...");
                throw new ArgumentException("Unable to read from stream", "stream");
            }

            byte[] iv = new byte[IV_SIZE];
            int bytesRead = stream.ReadAsync(iv, 0, IV_SIZE).Result;

            if(bytesRead != IV_SIZE) {
                return null;
            }

            _cryptor.IV = iv;
            return new SilentCryptoStream(stream, _cryptor.CreateDecryptor(), CryptoStreamMode.Read);
        }

        /// <summary>
        /// Creates a strem that will encrypt the given base stream
        /// </summary>
        /// <returns>The stream to write to for encryption</returns>
        /// <param name="baseStream">The stream to read from</param>
        public CryptoStream CreateStream(Stream baseStream)
        {
            if (_cryptor == null || baseStream == null) {
                return null;
            }

            var retVal = new SilentCryptoStream(baseStream, _cryptor.CreateEncryptor(), CryptoStreamMode.Write);
            retVal.Write(_cryptor.IV, 0, IV_SIZE);
            return retVal;
        }

        #endregion

        #region Private Methods

        private void InitCryptor()
        {
            _cryptor = Aes.Create();
            _cryptor.KeySize = KEY_SIZE * 8;
            _cryptor.BlockSize = BLOCK_SIZE * 8;
            _cryptor.Padding = PaddingMode.PKCS7;
        }

        #endregion

        #if !NET_3_5
        #region IDisposable
        #pragma warning disable 1591

        public void Dispose()
        {
            _cryptor.Dispose();
        }

        #pragma warning restore 1591
        #endregion
        #endif
    }
}
