using System;
using Jint;
using System.Linq;
using Couchbase.Lite.Util;
using Jint.Native.Array;
using Jint.Native;
using System.Collections.Generic;
using System.Collections;
using Couchbase.Lite.Views;

using Couchbase.Lite;
using Newtonsoft.Json;

namespace Convertigo.SDK.FullSync
{
    /// <summary>
    /// A class for compiling views from Javascript source
    /// Copied from https://github.com/couchbase/couchbase-lite-net/blob/ee085e3ee661489f9ef6f69cf07606095f152302/src/ListenerComponent/Couchbase.Lite.Listener.Shared/PeerToPeer/JSViewCompiler.cs
    /// https://github.com/couchbase/couchbase-lite-net/blob/master/src/ListenerComponent/Couchbase.Lite.Listener.Shared/PeerToPeer/JSViewCompiler.cs
    /// </summary>
    public  class JSViewCompilerCopy : IViewCompiler
    {

        #region IViewCompiler

        public delegate void LogDelegate(String msg);

        public MapDelegate CompileMap(string source, string language)
        {
            if (!language.Equals("javascript"))
            {
                return null;
            }

            source = source.Replace("function", "function _f1");

            return (doc, emit) =>
            {
                var engine = new Engine();
                engine.SetValue("log", new LogDelegate((msg) =>
                {
                    // TODO: handle log
                }));
                engine.SetValue("emit", emit);
                engine.Execute(source);
                engine.Execute("_f1(" + JsonConvert.SerializeObject(doc) + ")");
            };
        }

        public ReduceDelegate CompileReduce(string source, string language)
        {
            if (!language.Equals("javascript"))
            {
                return null;
            }

            if (source.StartsWith("_"))
            {
                // return BuiltinReduceFunctions.Get(source.TrimStart('_'));
            }

            source = source.Replace("function", "function _f2");

            return (keys, values, rereduce) =>
            {
                var engine = new Engine();
                engine.SetValue("log", new LogDelegate((msg) =>
                {
                    // TODO: handle log
                }));
                engine.Execute(source);
                return engine.Execute("_f2(" + JsonConvert.SerializeObject(keys) + ", " + JsonConvert.SerializeObject(values) + ", " + (rereduce ? "true" : "false") + ")").GetCompletionValue().ToObject();
            };
        }

        #endregion
    }
}
