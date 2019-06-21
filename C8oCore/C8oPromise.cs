using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Convertigo.SDK
{
    /// <summary>
    /// A Promise object for Convertigo SDK calls. CallJSON or CallXML will return a C8oPromise object you can use to chain several calls. a typical use would be :
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
        private KeyValuePair<C8oOnResponse<T>, bool> c8oResponse;
        private KeyValuePair<C8oOnProgress, bool> c8oProgress;
        private KeyValuePair<C8oOnFail, bool> c8oFail;
        private C8oPromise<T> nextPromise;

        private T lastResponse;
        private Exception lastFailure;
        private IDictionary<string, object> lastParameters;

        internal C8oPromise(C8o c8o)
        {
            this.c8o = c8o;
        }

        private C8oPromise<T> Then(C8oOnResponse<T> c8oOnResponse, bool ui)
        {
            if (nextPromise != null)
            {
                return nextPromise.Then(c8oOnResponse, ui);
            }
            else
            {
                c8oResponse = new KeyValuePair<C8oOnResponse<T>, bool>(c8oOnResponse, ui);
                nextPromise = new C8oPromise<T>(c8o);
                if (lastFailure != null)
                {
                    nextPromise.lastFailure = lastFailure;
                    nextPromise.lastParameters = lastParameters;
                }
                if (lastResponse != null)
                {
                    c8o.RunBG(OnResponse);
                }
                return nextPromise;
            }
        }

        /// <summary>
        /// Will be executed in a worker thread when a response is returned by the Server.
        /// </summary>
        /// <param name="c8oOnResponse">A C8oOnResponse lambda function</param>
        /// <returns>the same C8oPromise object to chain for other calls</returns>
        public C8oPromise<T> Then(C8oOnResponse<T> c8oOnResponse)
        {
            return Then(c8oOnResponse, false);
        }

        /// <summary>
        /// Will be executed in a UI thread when a response is returned by the Server.
        /// </summary>
        /// <param name="c8oOnResponse">A C8oOnResponse lambda function</param>
        /// <returns>the same C8oPromise object to chain for other calls</returns>
        public C8oPromise<T> ThenUI(C8oOnResponse<T> c8oOnResponse)
        {
            return Then(c8oOnResponse, true);
        }

        private C8oPromiseFailSync<T> Progress(C8oOnProgress c8oOnProgress, bool ui)
        {
            if (nextPromise != null)
            {
                return nextPromise.Progress(c8oOnProgress, ui);
            }
            else
            {
                c8oProgress = new KeyValuePair<C8oOnProgress, bool>(c8oOnProgress, ui);
                nextPromise = new C8oPromise<T>(c8o);
                return nextPromise;
            }
        }

        /// <summary>
        /// Will be executed in a worker thread when synchronizing data. This gives the opportunity to handle a FullSync
        /// progression. The lambda function will receive a C8oOnProgress object describing the replication status.
        /// </summary>
        /// <param name="C8oOnProgress">A C8oOnProgress lambda function</param>
        /// <returns>C8oPromiseFailSync object to chain for other calls</returns>
        public C8oPromiseFailSync<T> Progress(C8oOnProgress c8oOnProgress)
        {
            return Progress(c8oOnProgress, false);
        }

        /// <summary>
        /// Will be executed in a UI thread when synchronizing data. This gives the opportunity to handle a FullSync
        /// progression. The lambda function will receive a C8oOnProgress object describing the replication status.
        /// </summary>
        /// <param name="C8oOnProgress">A C8oOnProgress lambda function</param>
        /// <returns>C8oPromiseFailSync object to chain for other calls</returns>
        public C8oPromiseFailSync<T> ProgressUI(C8oOnProgress c8oOnProgress)
        {
            return Progress(c8oOnProgress, true);
        }

        private C8oPromiseSync<T> Fail(C8oOnFail c8oOnFail, bool ui)
        {
            if (nextPromise != null)
            {
                return nextPromise.Fail(c8oOnFail, ui);
            }
            else
            {
                c8oFail = new KeyValuePair<C8oOnFail, bool>(c8oOnFail, ui);
                nextPromise = new C8oPromise<T>(c8o);
                if (lastFailure != null)
                {
                    c8o.RunBG(() => {
                        OnFailure(lastFailure, lastParameters);
                    });
                }
                return nextPromise;
            }
        }

        /// <summary>
        /// Will be executed in a worker thread when an error is returned by the Server. This will give you
        /// the opportunity to handle the error.
        /// </summary>
        /// <param name="C8oOnFail">A C8oOnFail lambda function</param>
        /// <returns>the same C8oPromise object to chain for other calls</returns>
        public C8oPromiseSync<T> Fail(C8oOnFail c8oOnFail)
        {
            return Fail(c8oOnFail, false);
        }

        /// <summary>
        /// Will be executed in a UIr thread when an error is returned by the Server. This will give you
        /// the opportunity to handle the error and update the UI if needed.
        /// </summary>
        /// <param name="C8oOnFail">A C8oOnFail lambda function</param>
        /// <returns>the same C8oPromise object to chain for other calls</returns>
        public C8oPromiseSync<T> FailUI(C8oOnFail c8oOnFail)
        {
            return Fail(c8oOnFail, true);
        }

        /// <summary>
        /// Will wait for a server response blocking the current thread. Using Sync is not recomended unless you explicitly want to block
        /// the call thread.
        /// </summary>
        /// <returns>The data from the last call</returns>
        public T Sync()
        {
            var syncMutex = new bool[] { false };
            lock (syncMutex)
            {
                Then((response, parameters) =>
                {
                    lock (syncMutex)
                    {
                        syncMutex[0] = true;
                        lastResponse = response;
                        Monitor.Pulse(syncMutex);
                    }
                    return null;
                }).Fail((exception, parameters) =>
                {
                    lock (syncMutex)
                    {
                        syncMutex[0] = true;
                        lastFailure = exception;
                        Monitor.Pulse(syncMutex);
                    }
                });
                if (!syncMutex[0])
                {
                    Monitor.Wait(syncMutex);
                }
            }

            if (lastFailure != null)
            {
                throw lastFailure;
            }

            return lastResponse;
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

        private void OnResponse()
        {
            try
            {
                if (!c8oResponse.Equals(default(KeyValuePair<C8oOnResponse<T>, bool>)))
                {
                    var promise = new C8oPromise<T>[1];
                    if (c8oResponse.Value)
                    {
                        Exception failure = null;
                        lock (promise)
                        {
                            c8o.RunUI(() =>
                            {
                                lock (promise)
                                {
                                    try
                                    {
                                        promise[0] = c8oResponse.Key.Invoke(lastResponse, lastParameters);
                                    }
                                    catch (Exception e)
                                    {
                                        failure = e;
                                    }
                                    Monitor.Pulse(promise);
                                }
                            });
                            Monitor.Wait(promise);
                            if (failure != null)
                            {
                                throw failure;
                            }
                        }
                    }
                    else
                    {
                        promise[0] = c8oResponse.Key.Invoke(lastResponse, lastParameters);
                    }

                    if (promise[0] != null)
                    {
                        if (nextPromise != null)
                        {
                            var lastPromise = promise[0];
                            while (lastPromise.nextPromise != null)
                            {
                                lastPromise = lastPromise.nextPromise;
                            }
                            lastPromise.nextPromise = nextPromise;
                        }
                        nextPromise = promise[0];
                    }
                    else if (nextPromise != null)
                    {
                        nextPromise.OnResponse(lastResponse, lastParameters);
                    }
                }
                else if (nextPromise != null)
                {
                    nextPromise.OnResponse(lastResponse, lastParameters);
                }
                else
                {
                    // Response received and no handler.
                }
            }
            catch (Exception exception)
            {
                OnFailure(exception, lastParameters);
            }
        }

        internal void OnResponse(T response, IDictionary<string, object> parameters)
        {
            if (lastResponse != null && !parameters.ContainsKey(C8o.ENGINE_PARAMETER_FROM_LIVE))
            {
                if (nextPromise != null)
                {
                    nextPromise.OnResponse(response, parameters);
                }
                else
                {
                    c8o.Log._Trace("Another response received.");
                }
            }
            else
            {
                lastResponse = response;
                lastParameters = parameters;
                OnResponse();
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
            else if (nextPromise != null)
            {
                nextPromise.OnProgress(progress);
            }
        }

        internal void OnFailure(Exception exception, IDictionary<string, object> parameters)
        {
            lastFailure = exception;
            lastParameters = parameters;

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
                                    lastFailure = e;
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
                    c8oFail.Key.Invoke(lastFailure, parameters);
                }
            }
            if (nextPromise != null)
            {
                nextPromise.OnFailure(lastFailure, parameters);
            }
        }
    }

}
