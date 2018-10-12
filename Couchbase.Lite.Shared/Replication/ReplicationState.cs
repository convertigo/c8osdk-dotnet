﻿//
//  ReplicationState.cs
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

namespace Couchbase.Lite.Replicator
{
    /// <summary>
    /// The possible states for the replication state machine
    /// </summary>
    public enum ReplicationState
    {
        /// <summary>
        /// The replication has never been started
        /// </summary>
        Initial,

        /// <summary>
        /// The replication is actively sending and/or receiving data
        /// </summary>
        Running,

        /// <summary>
        /// The replication is waiting for new data
        /// </summary>
        Idle,

        /// <summary>
        /// The replication cannot reach its endpoint
        /// </summary>
        Offline,

        /// <summary>
        /// The replication is shutting down
        /// </summary>
        Stopping,

        /// <summary>
        /// The replication has stopped
        /// </summary>
        Stopped
    }
}

