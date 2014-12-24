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
using System.Collections;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Runtime.Remoting.Messaging;

namespace LinqToCache
{
    /// <summary>
    /// This class implements the caching behavior of an IQueryable&lt;T&gt; interface.
    /// When first time enumerated it enumerates the IQueryable and returns the query result.
    /// It also copies the result into a list as is being enumerated and it adds it into
    /// the cache with the given caching key. Subsequent enumerations using the same 
    /// caching key will use the cached result.
    /// By setting up a SqlDependency on the CallContext, a Query Notification will be
    /// placed on the SQL Server for the query. This notificaiton will fire when the
    /// cached result becomes invalid.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CachedQuery<T>: IEnumerable<T>
    {
        /// <summary>
        /// Magic cookie value used by SqlClient. Any SqlCommand.ExecuteXXX we call
        /// will check this cookie value in the CallContext.
        /// </summary>
        private const string SqlDependencyCookie = "MS.SqlDependencyCookie";

        /// <summary>
        /// The caching options passed in to the IQueryable extension.
        /// </summary>
        public CachedQueryOptions Options { get; set; }

        /// <summary>
        /// The original IQueryable query
        /// </summary>
        public IQueryable<T> Query { get; set; }

        /// <summary>
        /// The caching key
        /// </summary>
        public string Key { get; set; }

        #region Cache 

        /// <summary>
        /// Cache access synchronization object
        /// </summary>
        private static object cacheSyncRoot = new object();

        /// <summary>
        /// The results cache. Each type instantiation uses a distinct cache.
        /// Entries can be purges or individually removed.
        /// </summary>
        private static Dictionary<string, CachedEntry<T>> cachedEntries =
            new Dictionary<string, CachedEntry<T>>();

        /// <summary>
        /// Purges all entries in the results cache for the type &lt;T&gt;
        /// This method is safe to call in a multithreaded environment.
        /// </summary>
        public static void Purge()
        {
            lock (cacheSyncRoot)
            {
                cachedEntries.Clear();
            }
        }

        /// <summary>
        /// Removes the entry with the given key from the result cache for the type &lt;T&gt;
        /// This method is safe to call in a multithreaded environment.
        /// </summary>
        /// <param name="cacheKey">The caching key specified in AsCached call</param>
        public static void Remove(string cacheKey)
        {
            lock (cacheSyncRoot)
            {
                cachedEntries.Remove(cacheKey);
            }
        }
        #endregion

        /// <summary>
        /// Enumerates the query result as an IEnumerator of Object type entries.
        /// First invocation runs the query and enumerates the returned rows.
        /// Subsequent invocations may return the result from cache, if still valid.
        /// </summary>
        /// <returns></returns>
        public IEnumerator GetEnumerator()
        {
            IEnumerable<T> pThis = this;
            return pThis.GetEnumerator();
        }
        
        /// <summary>
        /// Enumerates the query result as an IEnumerator of &lt;T&gt; type entries.
        /// First invocation runs the query and enumerates the returned rows.
        /// Subsequent invocations may return the result from cache, if still valid.
        /// </summary>
        /// <returns></returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            CachedEntry<T> existingEntry = null;

            // First lest check if an entry exists in the cache
            //
            lock (cacheSyncRoot)
            {
                if (true == cachedEntries.TryGetValue(Key, out existingEntry))
                {
                    // One was found, check if is still valid
                    //
                    if (ECachedEntryState.Valid != existingEntry.State)
                    {
                        cachedEntries.Remove(Key);
                        existingEntry = null;
                    }
                }
            }

            // Did we find an entry?
            // 
            if (null == existingEntry)
            {
                // No cached entry, go ahead and run the query
                //

                // This list will retain a copy of the query results
                //
                List<T> queryResult = new List<T>();

                // Prepare an entry for the cache
                // We'll add the entry to the cache only *after* the iteration finishes
                //
                CachedEntry<T> newEntry = new CachedEntry<T>
                {
                    List = queryResult,
                    State = ECachedEntryState.Valid
                };

                // Set up the SqlDependency in the Callcontext
                SqlDependency dependency = new SqlDependency();
                object existingcookie = CallContext.GetData(SqlDependencyCookie);
                try
                {
                    CallContext.SetData(SqlDependencyCookie, dependency.Id);

                    // This is the invalidation callback.
                    // It will be invoked when the Query Notifications on the server
                    // invalidates our query result, for *whatever* reason
                    //
                    dependency.OnChange += (sender, args) =>
                    {
                        try
                        {
                            SqlDependency depSender = (SqlDependency)sender;

                            // Invalidate the entry ASAP, no lock required
                            //
                            newEntry.State = ECachedEntryState.Invalid;

                            // Now try to remove it from the cache
                            //
                            lock (cachedEntries)
                            {
                                CachedEntry<T> currentCached = null;
                                if (true == cachedEntries.TryGetValue(Key, out currentCached)
                                    && currentCached.Equals(newEntry))
                                {
                                    cachedEntries.Remove(Key);
                                }
                            }

                            // If caller asked to be notified, notify it
                            //
                            if (null != Options && null != Options.OnInvalidated)
                            {
                                CachedQueryEventArgs invalidationArgs = new CachedQueryEventArgs()
                                {
                                    CacheKey = Key,
                                    NotificationEventArgs = args,
                                    Tag = Options.Tag
                                };
                                Options.OnInvalidated(this, invalidationArgs);
                            }

                        }
                        catch (Exception e)
                        {
                            // Exception in the async callback, not much we can do about it. Log to Debug.
                            //
                            Debug.Write(e);
                        }
                    };

                    // Set up the Ouput members of the options
                    //
                    if (null != Options)
                    {
                        Options.DataSource = ECachedQuerySource.FromQuery;
                        Options.UtcQueryTime = DateTime.UtcNow;
                    }

                    // Now iterate the query
                    //
                    foreach (T t in Query)
                    {
                        // Retain a copy, then yield return the current row
                        queryResult.Add(t);
                        yield return t;
                    }
                }
                finally
                {
                    // Reset the CallContext, otherwise all subsequent 
                    // SqlCommand calls will eroneously use our cookie
                    //
                    CallContext.SetData(SqlDependencyCookie, existingcookie);
                }

                // The query was iterated till last row
                // Now add the retained copy of the result to the cache
                //
                lock (cacheSyncRoot)
                {
                    // We need to check first if an entry with same key wasn't added
                    // in the meantime
                    //
                    if (true == cachedEntries.TryGetValue(Key, out existingEntry))
                    {
                        if (ECachedEntryState.Valid != existingEntry.State)
                        {
                            // An entry was added, but is already invalid
                            // 
                            cachedEntries.Remove(Key);
                            existingEntry = null;
                        }
                    } 

                    // No entry with same key exists and our entry is still valid
                    //
                    if (null == existingEntry &&
                        ECachedEntryState.Valid == newEntry.State)
                    {
                        // Bingo!
                        //
                        newEntry.UtcTimeAdded = DateTime.UtcNow;
                        cachedEntries.Add(Key, newEntry);
                    }
                }
            }
            else
            {
                // We have found a cached entry of a previous result with the same key
                // We're going to return the cached result instead of iterating the query
                
                // Set up the Output members of the Options
                //
                if (null != Options)
                {
                    Options.DataSource = ECachedQuerySource.FromCache;
                    Options.UtcQueryTime = existingEntry.UtcTimeAdded;
                }

                // Iterate the cached result and return them
                // 
                foreach (T t in existingEntry.List)
                {
                    yield return t;
                }
            }
        }
    }
}
