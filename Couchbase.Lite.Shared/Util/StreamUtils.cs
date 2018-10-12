﻿//
// StreamUtils.cs
//
// Author:
//     Unknown (Current maintainer Jim Borden <jim.borden@couchbase.com>)
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

namespace Couchbase.Lite.Util
{
    internal static class StreamUtils
    {
        /// <exception cref="System.IO.IOException"></exception>
        internal static void CopyStreamsToFolder(IDictionary<String, Stream> streams, string folder)
        {
            foreach (var entry in streams)
            {
                var filename = Path.GetFileNameWithoutExtension(entry.Key).ToUpperInvariant() + Path.GetExtension(entry.Key);
                var file = Path.Combine(folder, filename);
                CopyStreamToFile(entry.Value, file);
            }
        }

        /// <exception cref="System.IO.IOException"></exception>
        internal static void CopyStreamToFile(Stream inStream, string file)
        {
            var outStream = new FileStream(file, FileMode.OpenOrCreate);
            var n = 0;
            var buffer = new byte[16384];
            while ((n = inStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                outStream.Write(buffer, 0, n);
            }
            outStream.Dispose();
            inStream.Dispose();
        }
    }
}

