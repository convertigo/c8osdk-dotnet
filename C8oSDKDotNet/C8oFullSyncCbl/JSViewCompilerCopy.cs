using Couchbase.Lite;
using Couchbase.Lite.Views;
using Jint;
using Jint.Native;
using Jint.Native.Array;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Convertigo.SDK.Internal
{
    /// <summary>
    /// A class for compiling views from Javascript source
    /// Copied from https://github.com/couchbase/couchbase-lite-net/blob/ee085e3ee661489f9ef6f69cf07606095f152302/src/ListenerComponent/Couchbase.Lite.Listener.Shared/PeerToPeer/JSViewCompiler.cs
    /// https://github.com/couchbase/couchbase-lite-net/blob/master/src/ListenerComponent/Couchbase.Lite.Listener.Shared/PeerToPeer/JSViewCompiler.cs
    /// </summary>
    internal class JSViewCompilerCopy : IViewCompiler
    {
        public delegate void LogDelegate(String msg);

        public MapDelegate CompileMap(string source, string language)
        {
            if (!language.Equals("javascript"))
            {
                return null;
            }

            EmitDelegate rtEmit = null;
            EmitDelegate wrapEmit = (key, value) =>
            {
                rtEmit(key, value);
            };

            source = source.Replace("function", "function _f1");

            var engine = new Engine();

            engine.SetValue("log", new LogDelegate((msg) =>
            {
                // TODO: handle log
            }));

            engine.SetValue("emit", wrapEmit);
            engine.Execute(source);

            return (doc, emit) =>
            {
                rtEmit = emit;
                string tempSource = "_f1(" + JsonConvert.SerializeObject(doc) + ");";
                engine.Execute(tempSource);
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
                if (source.StartsWith("_sum"))
                {
                    return BuiltinReduceFunctions.Sum;
                }
                else if (source.StartsWith("_count"))
                {
                    return BuiltinReduceFunctions.Count;
                }
                else if (source.StartsWith("_stats"))
                {
                    return BuiltinReduceFunctions.Stats;
                }
                else if (source.StartsWith("_average"))
                {
                    return BuiltinReduceFunctions.Average;
                }
                else if (source.StartsWith("_max"))
                {
                    return BuiltinReduceFunctions.Max;
                }
                else if (source.StartsWith("_min"))
                {
                    return BuiltinReduceFunctions.Min;
                }
                else if (source.StartsWith("_median"))
                {
                    return BuiltinReduceFunctions.Median;
                }
            }

            source = source.Replace("function", "function _f2");

            var engine = new Engine();

            engine.SetValue("log", new LogDelegate((msg) =>
            {
                // TODO: handle log
            }));

            engine.Execute(source);

            return (keys, values, rereduce) =>
            {
                string tempSource = "_f2(" + JsonConvert.SerializeObject(keys) + ", " + JsonConvert.SerializeObject(values) + ", " + (rereduce ? "true" : "false") + ")";
                var result = engine.Execute(tempSource).GetCompletionValue().ToObject();
                return result;
            };
        }
    }
}
