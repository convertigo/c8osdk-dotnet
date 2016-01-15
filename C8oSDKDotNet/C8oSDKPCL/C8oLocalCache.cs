using System;

namespace Convertigo.SDK
{
    public class C8oLocalCache
    {
        public readonly static string PARAM = "__localCache";

        public class Priority
        {
            public static readonly Priority SERVER = new Priority(c8o =>
            {
                return true;
            });

            public static readonly Priority LOCAL = new Priority(c8o =>
            {
                return true;
            });

            internal Func<C8o, bool> IsAvailable;

            private Priority(Func<C8o, bool> isAvailable)
            {
                IsAvailable = isAvailable;
            }
        }

        internal Priority priority;
        internal long ttl;
        internal bool enable;

        public C8oLocalCache(Priority priority, long ttl = -1, bool enable = true)
        {
            if (priority == null)
            {
                throw new System.ArgumentException("Local Cache priority cannot be null");
            }
            this.priority = priority;
            this.ttl = ttl;
            this.enable = enable;
        }
    }
}
