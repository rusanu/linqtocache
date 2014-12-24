﻿// Copyright (c) 2010. Rusanu Consulting LLC  
// http://code.google.com/p/linqtocache/
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at

//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

namespace LinqToCache
{
    /// <summary>
    /// Represents the set of arguments passed to the invalidation event handler
    /// </summary>
    public class CachedQueryEventArgs: EventArgs
    {
        /// <summary>
        /// The arguments passed to the SqlDependency change event 
        /// </summary>
        public SqlNotificationEventArgs NotificationEventArgs { get; internal set; }

        /// <summary>
        /// The cache key of the invalidated entry
        /// </summary>
        public string CacheKey { get; internal set; }

        /// <summary>
        /// The Tag object passed in as CacheQueryOptions in the AsCached method call
        /// </summary>
        public object Tag { get; internal set; }
    }
}
