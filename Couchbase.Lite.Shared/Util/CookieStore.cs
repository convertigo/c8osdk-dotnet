﻿//
// CookieStore.cs
//
// Author:
//     Pasin Suriyentrakorn  <pasin@couchbase.com>
//
// Copyright (c) 2014 Couchbase Inc
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

#if !NET_3_5
using System.Net;
using StringEx = System.String;
#else
using System.Net.Couchbase;
#endif

namespace Couchbase.Lite.Util
{

    /// <summary>
    /// An object that holds and serializes cookies
    /// </summary>
    public class CookieStore : CookieContainer
    {

        #region Constants

        private const string TAG = "CookieStore";
        private const string FileName = "cookies.json";
        private const string OldCookieStorageKey = "cbl_cookie_storage";
        private const string CookieStorageKey = "cookies";

        #endregion

        #region Variables

        private readonly object locker = new object();
        private readonly DirectoryInfo directory;
        private readonly Database _db;
        private HashSet<Uri> _cookieUriReference = new HashSet<Uri>();

        #endregion

        #region Properties

        #pragma warning disable 1591

        public new int Count 
        {
            get {
                foreach(var uri in _cookieUriReference) {
                    GetCookies(uri);
                }

                return base.Count;
            }
        }

        #pragma warning restore 1591

        #endregion

        #region Constructors

        /// <summary>
        /// Convenience constructor
        /// </summary>
        public CookieStore() : this (null, null) { }

        /// <summary>
        /// Default constructor (obsolete)
        /// </summary>
        /// <param name="directory">The directory to serialize the cookies to</param>
        [Obsolete("Use the database constructor")]
        public CookieStore(string directory) 
        {
            if (directory != null) {
                this.directory = new DirectoryInfo(directory);
            }

            DeserializeFromDisk();
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="db">The database to serialize cookies to</param>
        /// <param name="storageKey">deprecated, ignored</param>
        public CookieStore(Database db, string storageKey)
        {
            _db = db;
            if(!MigrateOldCookies()) {
                DeserializeFromDisk();
                DeserializeFromDB();
            }
        }

        #endregion

        #region Public Methods

        #pragma warning disable 1591

        /// <summary>
        /// Add the specified cookies, force overrides CookieCollection
        /// </summary>
        /// <param name="cookies">The cookies to add</param>
        public new void Add(CookieCollection cookies)
        {
            if (cookies == null) {
                return;
            }

            foreach (Cookie cookie in cookies) {
                Add(cookie, false);
            }

            Save();
        }

        /// <summary>
        /// Add the specified cookie, force overrides CookieCollection
        /// </summary>
        /// <param name="cookie">The cookie to add</param>
        public new void Add(Cookie cookie)
        {
            Add(cookie, true);
        }

        #pragma warning restore 1591

        /// <summary>
        /// Delete the cookie with the specified uri and name.
        /// </summary>
        /// <param name="uri">The uri of the cookie.</param>
        /// <param name="name">The name of the cookie.</param>
        public void Delete(Uri uri, string name)
        {
            if (uri == null || name == null) {
                return;
            }

            lock (locker) {
                var delete = false;
                var cookies = GetCookies(uri);
                foreach (Cookie cookie in cookies) {
                    if (name.Equals(cookie.Name)) {
                        cookie.Discard = true;
                        cookie.Expired = true;
                        cookie.Expires = DateTime.Now.Subtract(TimeSpan.FromDays(2));

                        if (!delete) {
                            delete = true;
                        }
                    }
                }

                if (delete) {
                    // Trigger container cookie list refreshment
                    GetCookies(uri);
                    Save();
                }
            }
        }

        /// <summary>
        /// Deletes all cookies for the specified uri
        /// </summary>
        /// <param name="uri">The uri of the cookies to be deleted</param>
        public void Delete(Uri uri)
        {
            if(uri == null) {
                return;
            }

            lock(locker) {
                var delete = false;
                var cookies = GetCookies(uri);
                foreach(Cookie cookie in cookies) {
                    cookie.Discard = true;
                    cookie.Expired = true;
                    cookie.Expires = DateTime.Now.Subtract(TimeSpan.FromDays(2));

                    if(!delete) {
                        delete = true;
                    }
                }

                if(delete) {
                    _cookieUriReference.Remove(uri);
                    // Trigger container cookie list refreshment
                    GetCookies(uri);
                    Save();
                }
            }
        }

        public new string GetCookieHeader(Uri uri)
        {
            var existing = GetCookies(uri);
            var str1 = string.Empty;
            var str2 = string.Empty;
            foreach (var cookie in existing.Cast<Cookie>().Reverse()) {
                str1 = str1 + str2 + cookie;
                str2 = "; ";
            }

            return str1;
        }

        /// <summary>
        /// Saves the cookies to disk
        /// </summary>
        public void Save()
        {
            lock (locker) {
                if (_db != null) {
                    SerializeToDB();
                } else {
                    SerializeToDisk();
                }
            }
        }

        #endregion

        #region Private Methods

        private void Add(Cookie cookie, bool save)
        {
            // There is no way to explicitly remove a port setting on a Cookie,
            // And the serialization process will cause it to be set to an empty string
            // Which causes it to only be valid on port 80 (the default) so we need to make
            // a new Cookie with the same settings, being careful not to set the Port.
            var newCookie = new Cookie(cookie.Name, cookie.Value, cookie.Path, cookie.Domain) {
                Comment = cookie.Comment,
                CommentUri = cookie.CommentUri,
                Discard = cookie.Discard,
                Expired = cookie.Expired,
                Expires = cookie.Expires,
                HttpOnly = cookie.HttpOnly,
                Secure = cookie.Secure,
                Version = cookie.Version,
            };

            var urlString = String.Format("http://{0}{1}", newCookie.Domain.TrimStart('.'), newCookie.Path);
            var url = new Uri(urlString);
            _cookieUriReference.Add(url);

            var existing = GetCookies(url);
            foreach(Cookie existingCookie in existing) {
                if (existingCookie.Name == newCookie.Name && existingCookie.Domain == newCookie.Domain 
                    && existingCookie.Path == newCookie.Path) {
                    existingCookie.Expired = true;
                    break;
                }
            }

            base.Add(newCookie);

            if (save) {
                Save();
            }
        }

        private string GetSaveCookiesFilePath()
        {
            if (directory == null) {
                return null;
            }

            if (!directory.Exists) {
                directory.Create();
                directory.Refresh();
            }

            return Path.Combine(directory.FullName, FileName);
        }

        private bool IsNotSessionOnly(Cookie cookie)
        {
            return cookie.Expires != DateTime.MinValue;
        }

        private void SerializeToDisk()
        {
            var filePath = GetSaveCookiesFilePath();
            if (StringEx.IsNullOrWhiteSpace(filePath)) {
                return;
            }

            List<Cookie> aggregate = new List<Cookie>();
            foreach (var uri in _cookieUriReference) {
                var collection = GetCookies(uri);
                aggregate.AddRange(collection.Cast<Cookie>().Where(IsNotSessionOnly));
            }

            using (var writer = new StreamWriter(filePath)) {
                var json = Manager.GetObjectMapper().WriteValueAsString(aggregate);
                writer.Write(json);
            }
        }

        private void DeserializeFromDisk()
        {
            var filePath = GetSaveCookiesFilePath();
            if (StringEx.IsNullOrWhiteSpace(filePath)) {
                return;
            }

            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists) {
                return;
            }

            using (var reader = new StreamReader(filePath)) {
                var json = reader.ReadToEnd();

                var cookies = Manager.GetObjectMapper().ReadValue<IList<Cookie>>(json);
                cookies = cookies ?? new List<Cookie>();

                foreach (Cookie cookie in cookies) {
                    Add(cookie, false);
                }
            }
        }

        private void SerializeToDB()
        {
            if (_db == null) {
                Log.To.Database.I(TAG, "Database null, so skipping serialization");
                return;
            }

            List<Cookie> aggregate = new List<Cookie>();
            foreach (var uri in _cookieUriReference) {
                var collection = GetCookies(uri);
                aggregate.AddRange(collection.Cast<Cookie>().Where(IsNotSessionOnly));
            }

            _db.Storage.SetInfo(CookieStorageKey, Manager.GetObjectMapper().WriteValueAsString(aggregate));
        }

        private void DeserializeFromDB()
        {
            if (_db == null) {
                Log.To.Database.I(TAG, "Database null, so skipping deserialization");
                return;
            }


            var val = Manager.GetObjectMapper().ReadValue<IList<Cookie>>(_db.Storage.GetInfo(CookieStorageKey));
            if (val == null) {
                return;
            }

            foreach (var cookie in val) {
                Add(cookie, false);
            }
        }

        private bool MigrateOldCookies()
        {
            if(_db == null) {
                return false;
            }

            var existing = _db.Storage.GetInfo(CookieStorageKey);
            if(existing == null) {
                var result = _db.RunInTransaction(() =>
                {
                    var localCheckpointDoc = _db.GetLocalCheckpointDoc() ?? new Dictionary<string, object>();
                    var newDoc = new Dictionary<string, object>();
                    foreach(var pair in localCheckpointDoc) {
                        if(pair.Key.StartsWith(OldCookieStorageKey)) {
                            LoadCookie(pair.Value.AsList<Cookie>());
                        } else {
                            newDoc.Add(pair.Key, pair.Value);
                        }
                    }

                    _db.SetLocalCheckpointDoc(newDoc);
                    Save();
                    return true;
                });

                if(!result) {
                    Log.To.Database.W(TAG, "Cannot migrate cookies!");
                }

                return result;
            }

            return false;
        }

        private void LoadCookie(IList<Cookie> json)
        {
            foreach(var cookie in json) {
                Add(cookie);
            }
        }

        #endregion
    }
}

