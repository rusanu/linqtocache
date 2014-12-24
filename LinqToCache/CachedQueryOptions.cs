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

namespace LinqToCache
{
    /// <summary>
    /// Extra options to for the cached query. 
    /// </summary>
    public class CachedQueryOptions
    {
        /// <summary>
        /// The UTC time when the result was obtained. 
        /// If the result is from query, the time is taken when the query iteration starts.
        /// If the result is from cache, the time is taken when the complete result was added to the cache.
        /// </summary>
        public DateTime UtcQueryTime { get; internal set; }

        /// <summary>
        /// Where was the result returned from: from query or from cache
        /// </summary>
        public ECachedQuerySource DataSource {get; internal set;}

        /// <summary>
        /// Event raised when the query is invalidated by the server.
        /// Only the CachedQueryOptions for a result returned from the query raise this event.
        /// </summary>
        public EventHandler<CachedQueryEventArgs> OnInvalidated { get; set; }

        /// <summary>
        /// Arbitrary object to be passed as an argument to the OnInvalidated event handler.
        /// </summary>
        public object Tag { get; set; }
    }
}
