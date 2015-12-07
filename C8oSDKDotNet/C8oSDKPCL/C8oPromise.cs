using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Convertigo.SDK
{
    /// <summary>
    /// A Promise object for Convertigo SDK calls. CallJSON or CallXML will return a C8oPromis object you can use to chain several calls. a typical use would be :
    /// <code>
    ///    myC8o.CallJson (".sequ1", "shopCode", "42")
    ///     .Then((response, parameters) => {					
    ///        return(myC8o.CallJson (".sequ2"));						
    ///     }).Then((response, parameters) => {					
    ///        return(myC8o.CallJson (".sequ3"));						
    ///     }).ThenUI((response, parameters) => {					
    ///        // Do some stuff on the UI Thread.
    ///        return null;						
    ///     }).Fail((response, parameters) => {
    ///        // Do some stuff is a call fails 
    ///     });
    ///    
    /// </code>
    /// This code will call sequ1 then when this call has finished will call sequ2 and again in the same way sequ3. When sequ3 is finished, 
    /// we can update the UI using data from the response object as the thread will automatically run in the UI thread. If something fails, we 
    /// will be called in the Fail() function and we will be able to handle the error.
    /// </summary>
    public class C8oPromise<T> : C8oPromiseFailSync<T>
    {
        private C8o c8o;
        private readonly List<KeyValuePair<C8oOnResponse<T>, bool>> c8oOnResponses = new List<KeyValuePair<C8oOnResponse<T>, bool>>();
        private KeyValuePair<C8oOnProgress, bool> c8oProgress;
        private KeyValuePair<C8oOnFail, bool> c8oFail;
        private readonly object syncMutex = new object();

        private T lastResult;
        private Exception lastException;

        internal C8oPromise(C8o c8o)
        {
            this.c8o = c8o;
        }

        /// <summary>
        /// Will be executed in a worker thread when a response is returned by the Server.
        /// </summary>
        /// <param name="c8oOnResponse">A C8oOnResponse lambda function</param>
        /// <returns>the same C8oPromise object to chain for other calls</returns>
        public C8oPromise<T> Then(C8oOnResponse<T> c8oOnResponse)
        {
            lock (c8oOnResponses)
            {
                c8oOnResponses.Add(new KeyValuePair<C8oOnResponse<T>, bool>(c8oOnResponse, false));
            }
            return this;
        }

        /// <summary>
        /// Will be executed in a UI thread when a response is returned by the Server.
        /// </summary>
        /// <param name="c8oOnResponse">A C8oOnResponse lambda function</param>
        /// <returns>the same C8oPromise object to chain for other calls</returns>
        public C8oPromise<T> ThenUI(C8oOnResponse<T> c8oOnResponse)
        {
            lock (c8oOnResponses)
            {
                c8oOnResponses.Add(new KeyValuePair<C8oOnResponse<T>, bool>(c8oOnResponse, true));
            }
            return this;
        }

        /// <summary>
        /// Will be executed in a worker thread when synchronizing data. This gives the opportunity to handle a FullSync
        /// progression. The lambda function will receive a C8oOnProgress object describing the replication status.
        /// </summary>
        /// <param name="C8oOnProgress">A C8oOnProgress lambda function</param>
        /// <returns>C8oPromiseFailSync object to chain for other calls</returns>
        public C8oPromiseFailSync<T> Progress(C8oOnProgress c8oOnProgress)
        {
            c8oProgress = new KeyValuePair<C8oOnProgress, bool>(c8oOnProgress, false);
            return this;
        }

        /// <summary>
        /// Will be executed in a UI thread when synchronizing data. This gives the opportunity to handle a FullSync
        /// progression. The lambda function will receive a C8oOnProgress object describing the replication status.
        /// </summary>
        /// <param name="C8oOnProgress">A C8oOnProgress lambda function</param>
        /// <returns>C8oPromiseFailSync object to chain for other calls</returns>
        public C8oPromiseFailSync<T> ProgressUI(C8oOnProgress c8oOnProgress)
        {
            c8oProgress = new KeyValuePair<C8oOnProgress, bool>(c8oOnProgress, true);
            return this;
        }

        /// <summary>
        /// Will be executed in a worker thread when an error is returned by the Server. This will give you
        /// the opportunity to handle the error.
        /// </summary>
        /// <param name="C8oOnFail">A C8oOnFail lambda function</param>
        /// <returns>the same C8oPromise object to chain for other calls</returns>
        public C8oPromiseSync<T> Fail(C8oOnFail c8oOnFail)
        {
            c8oFail = new KeyValuePair<C8oOnFail, bool>(c8oOnFail, false);
            return this;
        }

        /// <summary>
        /// Will be executed in a UIr thread when an error is returned by the Server. This will give you
        /// the opportunity to handle the error and update the UI if needed.
        /// </summary>
        /// <param name="C8oOnFail">A C8oOnFail lambda function</param>
        /// <returns>the same C8oPromise object to chain for other calls</returns>
        public C8oPromiseSync<T> FailUI(C8oOnFail c8oOnFail)
        {
            c8oFail = new KeyValuePair<C8oOnFail, bool>(c8oOnFail, true);
            return this;
        }

        /// <summary>
        /// Will wait for a server response blocking the current thread. Using Sync is not recomended unless you explicitly want to block
        /// the call thread.
        /// </summary>
        /// <returns>The data from the last call</returns>
        public T Sync()
        {
            lock (syncMutex)
            {
                Then((response, parameters) =>
                {
                    lock (syncMutex)
                    {
                        lastResult = response;
                        Monitor.Pulse(syncMutex);
                    }
                    return null;
                });
                Monitor.Wait(syncMutex);
            }

            if (lastException != null)
            {
                throw lastException;
            }

            return lastResult;
        }

        /// <summary>
        /// Will wait asynchronously for a server response while not blocking the current thread. This is the recomended way to wait for a server response with the
        /// await operator.
        /// </summary>
        /// <sample>
        ///     <code>
        ///         JObject data = await myC8o.CallJSON(".mysequence").Async();
        ///     </code>
        /// </sample>
        /// <returns>The data from the last call</returns>
        public Task<T> Async()
        {
            TaskCompletionSource<T> task = new TaskCompletionSource<T>();

            Then((response, parameters) =>
            {
                task.TrySetResult(response);
                return null;
            }).Fail((exception, parameters) =>
            {
                task.TrySetException(exception);
            });
            
            return task.Task;
        }

        internal void OnResponse(T response, IDictionary<string, object> parameters)
        {
            try
            {
                lock (c8oOnResponses)
                {
                    if (c8oOnResponses.Count > 0)
                    {
                        var handler = c8oOnResponses[0];
                        c8oOnResponses.RemoveAt(0);

                        var promise = new C8oPromise<T>[1];

                        if (handler.Value)
                        {
                            Exception exception = null;
                            lock (promise)
                            {
                                c8o.RunUI(() =>
                                {
                                    lock (promise)
                                    {
                                        try
                                        {
                                            promise[0] = handler.Key.Invoke(response, parameters);
                                        }
                                        catch (Exception e)
                                        {
                                            exception = e;
                                        }
                                        Monitor.Pulse(promise);
                                    }
                                });
                                Monitor.Wait(promise);
                                if (exception != null)
                                {
                                    throw exception;
                                }
                            }
                        }
                        else
                        {
                            promise[0] = handler.Key.Invoke(response, parameters);
                        }

                        if (promise[0] != null)
                        {
                            if (promise[0].c8oFail.Equals(default(KeyValuePair<C8oOnFail, bool>)))
                            {
                                promise[0].c8oFail = c8oFail;
                            }
                            if (promise[0].c8oProgress.Equals(default(KeyValuePair<C8oOnProgress, bool>)))
                            {
                                promise[0].c8oProgress = c8oProgress;
                            }
                            promise[0].Then((resp, param) =>
                            {
                                OnResponse(resp, param);
                                return null;
                            });
                        }
                    }
                    else
                    {
                        lastResult = response;
                    }
                }
            }
            catch (Exception exception)
            {
                OnFailure(exception, parameters);
            }
        }

        internal void OnProgress(C8oProgress progress)
        {
            if (!c8oProgress.Equals(default(KeyValuePair<C8oOnProgress, bool>)))
            {
                if (c8oProgress.Value)
                {
                    var locker = new object();
                    lock (locker)
                    {
                        c8o.RunUI(() =>
                        {
                            lock (locker)
                            {
                                try
                                {
                                    c8oProgress.Key.Invoke(progress);
                                }
                                catch (Exception e)
                                {
                                    OnFailure(e, new Dictionary<string, object>() { { C8o.ENGINE_PARAMETER_PROGRESS, progress } });
                                }
                                finally
                                {
                                    Monitor.Pulse(locker);
                                }
                            }
                        });

                        Monitor.Wait(locker);
                    }
                }
                else
                {
                    c8oProgress.Key.Invoke(progress);
                }
            }
        }

        internal void OnFailure(Exception exception, IDictionary<string, object> parameters)
        {
            lastException = exception;

            if (!c8oFail.Equals(default(KeyValuePair<C8oOnFail, bool>)))
            {
                if (c8oFail.Value)
                {
                    var locker = new object();
                    lock (locker)
                    {
                        c8o.RunUI(() =>
                        {
                            lock (locker)
                            {
                                try
                                {
                                    c8oFail.Key.Invoke(exception, parameters);
                                }
                                catch (Exception e)
                                {
                                    exception = e;
                                }
                                finally
                                {
                                    Monitor.Pulse(locker);
                                }
                            }
                        });

                        Monitor.Wait(locker);
                    }
                }
                else
                {
                    c8oFail.Key.Invoke(exception, parameters);
                }
            }

            lock (syncMutex)
            {
                Monitor.Pulse(syncMutex);
            }
        }
    }

}
