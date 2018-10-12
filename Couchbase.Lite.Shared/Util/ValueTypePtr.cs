﻿//
//  OutVal.cs
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

namespace Couchbase.Lite.Util
{

    /// <summary>
    /// A class for storing out variables (allows the passage of null for 
    /// an unneeded out param)
    /// </summary>
    public sealed class ValueTypePtr<T> where T : struct
    {
        #region Variables

        /// <summary>
        /// Gets or sets value held by this object
        /// </summary>
        public T Value { get; set; }

        /// <summary>
        /// Checks whether or not this instance is semantically null (this helps elimiate null checks
        /// in methods that accept this type)
        /// </summary>
        public bool IsNull {
            get {
                return this == NULL;
            }
        }

        /// <summary>
        /// A value to pass when the out parameter is not needed
        /// </summary>
        public static readonly ValueTypePtr<T> NULL = new ValueTypePtr<T>();

        #endregion

        #region Operators

        /// <param name="val">The object to cast to its contained type</param>
        public static implicit operator T(ValueTypePtr<T> val) 
        {
            return val == null ? default(T) : val.Value;
        }

        /// <param name="val">The value to convert to a value type pointer</param>
        public static implicit operator ValueTypePtr<T>(T val)
        {
            return new ValueTypePtr<T> { Value = val };
        }

        #endregion

        #region Overrides
        #pragma warning disable 1591

        public override string ToString()
        {
            return IsNull ? "<No Value>" : Value.ToString();
        }

        #pragma warning restore 1591
        #endregion

    }
}

