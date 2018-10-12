//
// BlobStoreWriter.cs
//
// Author:
//     Zachary Gramana  <zack@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc
// Copyright (c) 2014 .NET Foundation
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
//
// Copyright (c) 2014 Couchbase, Inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file
// except in compliance with the License. You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the
// License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND,
// either express or implied. See the License for the specific language governing permissions
// and limitations under the License.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Couchbase.Lite.Util;

namespace Couchbase.Lite
{
    /// <summary>Lets you stream a large attachment to a BlobStore asynchronously, e.g.</summary>
    /// <remarks>Lets you stream a large attachment to a BlobStore asynchronously, e.g. from a network download.
    ///     </remarks>
    internal class BlobStoreWriter
    {
        private static readonly string Tag = typeof(BlobStoreWriter).Name;

        /// <summary>The underlying blob store where it should be stored.</summary>
        /// <remarks>The underlying blob store where it should be stored.</remarks>
        private BlobStore store;

        /// <summary>The number of bytes in the blob.</summary>
        /// <remarks>The number of bytes in the blob.</remarks>
        private long length;

        /// <summary>After finishing, this is the key for looking up the blob through the CBL_BlobStore.
        ///     </summary>
        /// <remarks>After finishing, this is the key for looking up the blob through the CBL_BlobStore.
        ///     </remarks>
        private BlobKey blobKey;

        /// <summary>After finishing, store md5 digest result here</summary>
        private byte[] md5DigestResult;

        /// <summary>Message digest for sha1 that is updated as data is appended</summary>
        private MessageDigest sha1Digest;

        private MessageDigest md5Digest;

        private Stream outStream;

        private string tempFile;

        public string FilePath
        {
            get { return tempFile; }
        }

        public BlobStoreWriter(BlobStore store)
        {
            this.store = store;
            try
            {
                sha1Digest = MessageDigest.GetInstance("SHA-1");
                sha1Digest.Reset();
                md5Digest = MessageDigest.GetInstance("MD5");
                md5Digest.Reset();
            } catch (NotSupportedException e) {
                throw Misc.CreateExceptionAndLog(Log.To.Database, e, Tag,
                    "Could not get an instance of SHA-1 or MD5 for BlobStoreWriter.");
            }

            try {
                OpenTempFile();
            } catch (FileNotFoundException e) {
                throw Misc.CreateExceptionAndLog(Log.To.Database, e, Tag,
                    "Unable to open temporary file for BlobStoreWriter.");
            }
        }

        /// <exception cref="System.IO.FileNotFoundException"></exception>
        private void OpenTempFile()
        {
            string uuid = Misc.CreateGUID();
            string filename = string.Format("{0}.blobtmp", uuid);
            var tempDir = store.TempDir();
            tempFile = Path.Combine(tempDir, filename);
            if (store.EncryptionKey == null) {
                outStream = new BufferedStream(File.Open (tempFile, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite));
            } else {
                outStream = store.EncryptionKey.CreateStream(
                    new BufferedStream(File.Open(tempFile, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite)));
            }
        }

        /// <summary>Appends data to the blob.</summary>
        /// <remarks>Appends data to the blob. Call this when new data is available.</remarks>
        public void AppendData(IEnumerable<Byte> data)
        {
            var dataVector = data.ToArray();
            length += dataVector.LongLength;
            sha1Digest.Update(dataVector);
            md5Digest.Update(dataVector);

            try {
                outStream.Write(dataVector, 0, dataVector.Length);
            } catch (IOException e) {
                throw Misc.CreateExceptionAndLog(Log.To.Database, e, Tag,
                    "Unable to write to stream");
            }
        }

        internal void Read(Stream inputStream)
        {
            byte[] buffer = new byte[16384];
            int len;
            length = 0;
            inputStream.Reset();
            try {
                while ((len = inputStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    var dataToWrite = buffer;

                    outStream.Write(dataToWrite, 0, len);
                    sha1Digest.Update(buffer, 0, len);
                    md5Digest.Update(buffer, 0, len);
                    length += len;
                }
            } catch (IOException e) {
                throw Misc.CreateExceptionAndLog(Log.To.Database, e, Tag,
                    "Unable to read from stream");
            } finally {
                try {
                    inputStream.Close();
                } catch (IOException e) {
                    Log.To.Database.W(Tag, "Exception closing input stream, continuing...", e);
                }
            }
        }

        /// <summary>Call this after all the data has been added.</summary>
        public void Finish()
        {
            try {
                outStream.Flush();
                outStream.Dispose();
            } catch (IOException e) {
                Log.To.Database.W(Tag, "Exception closing output stream, continuing...", e);
            }

            blobKey = new BlobKey(sha1Digest.Digest());
            md5DigestResult = md5Digest.Digest();
        }

        /// <summary>Call this to cancel before finishing the data.</summary>
        public void Cancel()
        {
            try {
                outStream.Dispose();
            } catch (IOException e) {
                Log.To.Database.W(Tag, "Exception closing output stream, continuing...", e);
            }

            File.Delete(tempFile);
        }

        /// <summary>Installs a finished blob into the store.</summary>
        public void Install()
        {
            if (tempFile == null) {
                // already installed
                return;
            }

            // Move temp file to correct location in blob store:
            string destPath = store.RawPathForKey(blobKey);
            try {
                File.Move(tempFile, destPath);
            } catch(Exception) {
                // If the move fails, assume it means a file with the same name already exists; in that
                // case it must have the identical contents, so we're still OK.
                Cancel();
            }

            tempFile = null;
        }
            
        public string MD5DigestString()
        {
            string base64Md5Digest = Convert.ToBase64String(md5DigestResult);
            return string.Format("md5-{0}", base64Md5Digest);
        }

        public string SHA1DigestString()
        {
            string base64Sha1Digest = Convert.ToBase64String(blobKey.Bytes);
            return string.Format("sha1-{0}", base64Sha1Digest);
        }

        public long GetLength()
        {
            return length;
        }

        public BlobKey GetBlobKey()
        {
            return blobKey;
        }

        public override string ToString()
        {
            return String.Format("BlobStoreWriter len={0}kb, digest={1}, SHA1={2}", (double)length / 1024.0, MD5DigestString(), SHA1DigestString());
        }
    }
}
