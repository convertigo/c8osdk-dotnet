//
// SQLiteStorageEngine.cs
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
using Couchbase.Lite.Store;

#if SQLITE
namespace Couchbase.Lite.Storage.SystemSQLite
#elif CUSTOM_SQLITE
namespace Couchbase.Lite.Storage.CustomSQLite
#else
namespace Couchbase.Lite.Storage.SQLCipher
#endif
{

    /// <summary>
    /// An interface for describing an object that can interface with a SQLite database
    /// </summary>
    internal interface ISQLiteStorageEngine
    {

        /// <summary>
        /// Gets the last error code encountered by SQLite (note
        /// that this does not always indicate an error)
        /// </summary>
        int LastErrorCode { get; }

        /// <summary>
        /// Opens the database
        /// </summary>
        /// <param name="path">The path where the database exists</param>
        /// <param name="readOnly">Whether or not this storage engine is readonly</param> 
        /// <param name="schema">The schema to use to create the database initially</param>
        /// <param name="encryptionKey">A key for encrypting the database</param>
        bool Open(string path, bool readOnly, string schema, SymmetricKey encryptionKey);

        /// <summary>
        /// Gets the user version of the database
        /// </summary>
        /// <returns>The user version of the database</returns>
        int GetVersion();

        /// <summary>
        /// Sets the user version of the database
        /// </summary>
        /// <param name="version">The user version of the database</param>
        void SetVersion(Int32 version);

        /// <summary>
        /// Gets whether or not the database is open
        /// </summary>
        /// <value><c>true</c> if this instance is open; otherwise, <c>false</c>.</value>
        bool IsOpen { get; }


        bool InTransaction { get; }
      

        bool RunInTransaction(RunInTransactionDelegate block);

        /// <summary>
        /// Executes a SQL command (modification)
        /// </summary>
        /// <param name="sql">The SQL format string</param>
        /// <param name="paramArgs">The SQL string arguments</param>
        int ExecSQL(string sql, params object[] paramArgs);

        /// <summary>
        /// Executes a SQL query (i.e. the changes in the current transaction will not be shown)
        /// </summary>
        /// <returns>An iterator containing the results of the query</returns>
        /// <param name="sql">The SQL format string</param>
        /// <param name="paramArgs">The SQL string arguments</param>
        Cursor RawQuery(string sql, params Object[] paramArgs);

        /// <summary>
        /// Performs an INSERT operation
        /// </summary>
        /// <param name="table">The table to insert into</param>
        /// <param name="nullColumnHack">Reserved</param>
        /// <param name="values">The values to insert</param>
        /// <returns>The ID of the inserted object</returns>
        /// <remarks>
        /// If the data already exists this method will throw an exception
        /// </remarks>
        long Insert(string table, string nullColumnHack, ContentValues values);

        /// <summary>
        /// Performs an INSERT operation with a strategy for handling existing data
        /// </summary>
        /// <returns>The ID of the inserted row</returns>
        /// <param name="table">The table to insert into</param>
        /// <param name="nullColumnHack">Reserved</param>
        /// <param name="initialValues">The values to insert</param>
        /// <param name="conflictResolutionStrategy">The strategy to use when data already exists</param>
        long InsertWithOnConflict(string table, string nullColumnHack, ContentValues initialValues, ConflictResolutionStrategy conflictResolutionStrategy);

        /// <summary>
        /// Performs an UPDATE operation
        /// </summary>
        /// <param name="table">The table to update</param>
        /// <param name="values">The new values</param>
        /// <param name="whereClause">The formatted where clause (i.e. WHERE foo = bar)</param>
        /// <param name="whereArgs">The formatted where args</param>
        /// <returns>The number of rows updated</returns>
        int Update(string table, ContentValues values, string whereClause, params String[] whereArgs);

        /// <summary>
        /// Performs a DELETE operation
        /// </summary>
        /// <param name="table">The table to delete from</param>
        /// <param name="whereClause">The formatted where clause (i.e. WHERE foo = bar)</param>
        /// <param name="whereArgs">The formatted where args</param>
        int Delete(string table, string whereClause, params String[] whereArgs);

        /// <summary>
        /// Closes the connection to the SQLite database
        /// </summary>
        void Close();
    }
}