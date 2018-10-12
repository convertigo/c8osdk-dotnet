//
// Attachment.cs
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

using System;
using System.Collections.Generic;
using System.IO;

using Couchbase.Lite.Internal;
using Couchbase.Lite.Util;

namespace Couchbase.Lite {

    /// <summary>
    /// Contains the keys associated with attachment metadata (i.e. entries in _attachments)
    /// </summary>
    public struct AttachmentMetadataDictionaryKeys
    {
        /// <summary>
        /// The key for the type of attachment
        /// </summary>
        public static readonly string ContentType = AttachmentMetadataDictionary.ContentType;

        /// <summary>
        /// The key for the length of the attachment when decoded
        /// </summary>
        public static readonly string Length = AttachmentMetadataDictionary.Length;

        /// <summary>
        /// The key for indicating whether the attachment body is coming later in the request
        /// </summary>
        public static readonly string Follows = AttachmentMetadataDictionary.Follows;

        /// <summary>
        /// The key for the digest of the attachment
        /// </summary>
        public static readonly string Digest = AttachmentMetadataDictionary.Digest;

        /// <summary>
        /// The key for indicating whether or not the attachment is a stub (i.e. doesn't have
        /// inlined data)
        /// </summary>
        public static readonly string Stub = AttachmentMetadataDictionary.Stub;

        /// <summary>
        /// The key for the length of the attachment when encoded
        /// </summary>
        public static readonly string EncodedLength = AttachmentMetadataDictionary.EncodedLength;

        /// <summary>
        /// The key for the encoding of the attachment
        /// </summary>
        public static readonly string Encoding = AttachmentMetadataDictionary.Encoding;
    }

    [DictionaryContract(RequiredKeys=new object[] { 
        AttachmentMetadataDictionary.ContentType, typeof(string),
        AttachmentMetadataDictionary.Length, typeof(long),
        AttachmentMetadataDictionary.Digest, typeof(string)
    },
        OptionalKeys=new object[] {
        AttachmentMetadataDictionary.Follows, typeof(bool),
        AttachmentMetadataDictionary.Stub, typeof(bool),
        AttachmentMetadataDictionary.EncodedLength, typeof(long),
        AttachmentMetadataDictionary.Encoding, typeof(string)
    })]
    internal sealed class AttachmentMetadataDictionary : ContractedDictionary
    {
        internal const string ContentType = "content_type";
        internal const string Length = "length";
        internal const string Follows = "follows";
        internal const string Digest = "digest";
        internal const string Stub = "stub";
        internal const string EncodedLength = "encoded_length";
        internal const string Encoding = "encoding";

        public AttachmentMetadataDictionary() : base() {}

        public AttachmentMetadataDictionary(IDictionary<string, object> source) : base(source) {}
    }

    /// <summary>
    /// A Couchbase Lite Document Attachment.
    /// </summary>
    public sealed class Attachment : IDisposable {

        #region Constants

        internal const int DefaultStreamChunkSize = 8192;
        private static readonly string Tag = typeof(Attachment).Name;

        #endregion

        #region Constructors

        internal Attachment(Stream contentStream, string contentType)
        {
            if(contentStream == null) {
                Log.To.Database.E(Tag, "null contentStream in Attachment constructor, throwing...");
                throw new ArgumentNullException("contentStream");
            }

            if(contentType == null) {
                Log.To.Database.E(Tag, "null contentType in Attachment constructor, throwing...");
                throw new ArgumentNullException("contentType");
            }

            Metadata = new Dictionary<String, Object> {
                { AttachmentMetadataDictionaryKeys.ContentType, contentType },
                { AttachmentMetadataDictionaryKeys.Follows, true }
            };

            try {
                Metadata[AttachmentMetadataDictionaryKeys.Length] = contentStream.Length;
            } catch(NotSupportedException) {
                Log.To.Database.V(Tag, "Omitting length for non-seekable stream");
            }

            Body = contentStream;
        }

        internal Attachment(Revision revision, String name, IDictionary<string, object> metadata)
        {
            Revision = revision;
            Name = name;
            Metadata = metadata;
            Compressed = false;
        }
        #endregion

        #region Non-Public Instance Members

        /// <summary>
        /// Content stream is gzip encoded.
        /// </summary>
        /// <value><c>true</c> if compressed; otherwise, <c>false</c>.</value>
        internal bool Compressed { get; set; }

        internal Stream Body { get; set; }

        /// <summary>
        /// Goes through an _attachments dictionary and replaces any values that are Attachment objects
        /// with proper JSON metadata dicts.
        /// </summary>
        /// <remarks>
        /// Goes through an _attachments dictionary and replaces any values that are Attachment objects
        /// with proper JSON metadata dicts. It registers the attachment bodies with the blob store and sets
        /// the metadata 'digest' and 'follows' properties accordingly.
        /// </remarks>
        internal static IDictionary<string, object> InstallAttachmentBodies(IDictionary<String, Object> attachments, Database database)
        {
            var updatedAttachments = new Dictionary<string, object>();
            foreach (string name in attachments.Keys)
            {
                object value;
                attachments.TryGetValue(name, out value);

                if (value is Attachment)
                {
                    var attachment = (Attachment)value;
                    var metadataMutable = new AttachmentMetadataDictionary(attachment.Metadata);
                    var body = attachment.Body;
                    if (body != null)
                    {
                        // Copy attachment body into the database's blob store:
                        var writer = BlobStoreWriterForBody(body, database);
                        metadataMutable[AttachmentMetadataDictionaryKeys.Length] = (long)writer.GetLength();
                        metadataMutable[AttachmentMetadataDictionaryKeys.Digest] = writer.SHA1DigestString();
                        metadataMutable[AttachmentMetadataDictionaryKeys.Follows] = true;
                        var errMsg = metadataMutable.Validate();
                        if (errMsg != null) {
                            throw Misc.CreateExceptionAndLog(Log.To.Database, StatusCode.BadAttachment, Tag,
                                "Error installing attachment body ({0})", errMsg);
                        }

                        database.RememberAttachmentWriter(writer, writer.SHA1DigestString());
                    }

                    attachment.Dispose();
                    updatedAttachments[name] = metadataMutable;
                } else if (value is AttachmentInternal) {
                    throw Misc.CreateExceptionAndLog(Log.To.Database, StatusCode.BadParam, Tag,
                        "AttachmentInternal objects not expected here.  Could indicate a bug");
                } else {
                    if (value != null) {
                        updatedAttachments[name] = value;
                    }
                }
            }

            return updatedAttachments;
        }

        internal static BlobStoreWriter BlobStoreWriterForBody(Stream body, Database database)
        {
            var writer = database.AttachmentWriter;
            writer.Read(body);
            writer.Finish();
            return writer;
        }

        #endregion

        #region Instance Members

        /// <summary>
        /// Gets the owning <see cref="Couchbase.Lite.Revision"/>.
        /// </summary>
        /// <value>the owning <see cref="Couchbase.Lite.Revision"/>.</value>
        public Revision Revision { get; internal set; }

        /// <summary>
        /// Gets the owning <see cref="Couchbase.Lite.Document"/>.
        /// </summary>
        /// <value>The owning <see cref="Couchbase.Lite.Document"/></value>
        /// <exception cref="Couchbase.Lite.CouchbaseLiteException"></exception>
        public Document Document
        {
            get {
                if(Revision == null) {
                    Log.To.Database.W(Tag, "Revision null when Document property was called");
                    return null;
                }

                return Revision.Document;
            }
        }

        /// <summary>
        /// Gets the name of the <see cref="Couchbase.Lite.Attachment"/>.
        /// </summary>
        /// <value>The name of the <see cref="Couchbase.Lite.Attachment"/>.</value>
        public String Name { get ; internal set; }

        /// <summary>
        /// Gets the content-type.
        /// </summary>
        /// <value>The content-type.</value>
        public string ContentType {
            get {
                var contentType = default(string);
                if (!Metadata.TryGetValue<string>(AttachmentMetadataDictionaryKeys.ContentType, out contentType)) {
                    throw Misc.CreateExceptionAndLog(Log.To.Database, StatusCode.BadAttachment, Tag,
                        "Content type of attachment corrupt");
                }

                return contentType;
            }
        }

        /// <summary>
        /// Get the <see cref="Couchbase.Lite.Attachment"/> content stream.  The caller must not
        /// dispose it.
        /// </summary>
        /// <value>The <see cref="Couchbase.Lite.Attachment"/> content stream.</value>
        /// <exception cref="Couchbase.Lite.CouchbaseLiteException">
        /// Thrown if an error occurs when getting the content stream.
        /// </exception>
        public Stream ContentStream { 
            get {
                if (Body?.CanSeek == true) {
                    Body.Reset();
                    return Body;
                }

                if (Revision == null) {
                    throw Misc.CreateExceptionAndLog(Log.To.Database, StatusCode.InternalServerError, Tag,
                        "Revision was null when Attachment.ContentStream called");
                }

                if (Name == null) {
                    throw Misc.CreateExceptionAndLog(Log.To.Database, StatusCode.InternalServerError, Tag,
                        "Name was null when Attachment.ContentStream called");
                }

                var attachment = Revision.Database.AttachmentForDict(Metadata, Name);
                if (attachment == null) {
                    throw Misc.CreateExceptionAndLog(Log.To.Database, StatusCode.InternalServerError, Tag,
                        "Could not retrieve an attachment for revision sequence {0}.", Revision.Sequence);
                }


                Body?.Dispose();
                Body = attachment.ContentStream;
                Body.Reset();

                return Body;
            }
        }

        /// <summary>Gets the <see cref="Couchbase.Lite.Attachment"/> content.</summary>
        /// <value>The <see cref="Couchbase.Lite.Attachment"/> content</value>
        /// <exception cref="Couchbase.Lite.CouchbaseLiteException">
        /// Thrown if an error occurs when getting the content.
        /// </exception>
        public IEnumerable<Byte> Content 
        { 
            get {
                if (Body?.CanSeek == true) {
                    Body.Reset();
                    return Body.ReadAllBytes();
                }

                if (Revision == null) {
                    throw Misc.CreateExceptionAndLog(Log.To.Database, StatusCode.InternalServerError, Tag,
                        "Revision was null when Attachment.ContentStream called");
                }

                if (Name == null) {
                    throw Misc.CreateExceptionAndLog(Log.To.Database, StatusCode.InternalServerError, Tag,
                        "Name was null when Attachment.ContentStream called");
                }

                var attachment = Revision.Database.AttachmentForDict(Metadata, Name);
                if (attachment == null) {
                    throw Misc.CreateExceptionAndLog(Log.To.Database, StatusCode.InternalServerError, Tag,
                        "Could not retrieve an attachment for revision sequence {0}.", Revision.Sequence);
                }

                return attachment.Content;
            }
        }

        /// <summary>
        /// Gets the length in bytes of the content.
        /// </summary>
        /// <value>The length in bytes of the content.</value>
        public long Length {
            get {
                long retVal;
                if (!Metadata.TryGetValue<long>(AttachmentMetadataDictionaryKeys.Length, out retVal)) {
                    Log.To.Database.W(Tag, "Attachment doesn't contain a length entry, returning 0...");
                    return 0L;
                }

                return retVal;
            }
        }

        /// <summary>
        /// The CouchbaseLite metadata about the attachment, that lives in the document.
        /// </summary>
        public IDictionary<string, object> Metadata { get ; private set; }

        #endregion

        #region IDisposable

        /// <summary>
        /// Releases all resource used by the <see cref="Couchbase.Lite.Attachment"/> object.
        /// </summary>
        /// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="Couchbase.Lite.Attachment"/>. The
        /// <see cref="Dispose"/> method leaves the <see cref="Couchbase.Lite.Attachment"/> in an unusable state. After
        /// calling <see cref="Dispose"/>, you must release all references to the
        /// <see cref="Couchbase.Lite.Attachment"/> so the garbage collector can reclaim the memory that the
        /// <see cref="Couchbase.Lite.Attachment"/> was occupying.</remarks>
        public void Dispose()
        {
            if (Body != null)
            {
                Body.Dispose();
                Body = null;
            }
        }

        #endregion
    }

}

