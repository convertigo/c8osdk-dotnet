using Couchbase.Lite;
using Jint;
using Jint.Native;
using Jint.Native.Array;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;

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
                engine.SetValue("emit", emit);
                engine.SetValue("log", new LogDelegate((msg) =>
                {
                    // TODO: handle log
                }));
                source += "\n_f1(" + JsonConvert.SerializeObject(doc) + ");";
                engine.Execute(source);
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
            var engine = new Engine().Execute(source);//.SetValue("log", new Action<object>((line) => Log.I("JSViewCompiler", line.ToString())));

            return (keys, values, rereduce) =>
            {
                var jsKeys = ToJSArray(keys, engine);
                var jsVals = ToJSArray(values, engine);

                var result = engine.Invoke("_f2", jsKeys, jsVals, rereduce);
                return result.ToObject();
            };
        }

        #endregion

        #region Private Methods

        //Arrays cannot simply be passed into the Javascript engine, they must be allocated
        //according to Javascript rules
        private static ArrayInstance ToJSArray(IEnumerable list, Engine engine)
        {
            List<JsValue> wrappedVals = new List<JsValue>();
            foreach (object x in list)
            {
                wrappedVals.Add(JsValue.FromObject(engine, x));
            }

            return (ArrayInstance)engine.Array.Construct(wrappedVals.ToArray());
        }

        #endregion
    }
}
