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
    /// Extensions for IQueryable&lt;T&gt;
    /// </summary>
    public static class Category
    {
        /// <summary>
        /// Transforms an arbitrary IQueryable&lt;T&gt; interface into a potentially cached IEnumerable&lt;T&gt;.
        /// The cached result is dependent on a SqlDependency notification and SQL Server will invalidate it when
        /// the returned result is changed by any server side operation.
        /// Note that not all queries can be notified. To be valid for active notifications, a query must satisfy
        /// the restrictions outlined at Creating a Query for Notification: http://msdn.microsoft.com/en-us/library/ms181122.aspx
        /// Queries that don't satisfy the conditions will be invalidated immedeatly and evicted from the cache.
        /// </summary>
        /// <typeparam name="T">The type being returned by the query</typeparam>
        /// <param name="query">The IQueryable that is being extended with this method</param>
        /// <param name="cacheKey">The cache lookup key.</param>
        /// <param name="options">Optional caching options. Use this object to get information about the source
        /// of the query result (FromCache or FromQuery) or to provide an event handler to be invoked when the cached query is invalidated.</param>
        /// <returns>The query result as an IEnumerable&ltT&gt;</returns>
        public static IEnumerable<T> AsCached<T>(this IQueryable<T> query, string cacheKey, CachedQueryOptions options)
        {
            return new CachedQuery<T> { Query = query, Key = cacheKey, Options = options };
        }

        /// <summary>
        /// Transforms an arbitrary IQueryable&lt;T&gt; interface into a potentially cached IEnumerable&lt;T&gt;.
        /// The cached result is dependent on a SqlDependency notification and SQL Server will invalidate it when
        /// the returned result is changed by any server side operation.
        /// Note that not all queries can be notified. To be valid for active notifications, a query must satisfy
        /// the restrictions outlined at Creating a Query for Notification: http://msdn.microsoft.com/en-us/library/ms181122.aspx
        /// Queries that don't satisfy the conditions will be invalidated immedeatly and evicted from the cache.
        /// </summary>
        /// <typeparam name="T">The type being returned by the query</typeparam>
        /// <param name="query">The IQueryable that is being extended with this method</param>
        /// <param name="cacheKey">The cache lookup key.</param>
        /// <returns>The query result as an IEnumerable&ltT&gt;</returns>
        public static IEnumerable<T> AsCached<T>(this IQueryable<T> query, string cacheKey)
        {
            return query.AsCached(cacheKey, null);
        }
    }
}
