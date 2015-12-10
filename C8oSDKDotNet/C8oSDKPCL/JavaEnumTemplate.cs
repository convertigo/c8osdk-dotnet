using System;

namespace Convertigo.SDK.Utils
{
    /// <summary>
    /// The C# Enum does not allow as many things that Java Enum.
    /// This a way to reproduce Java Enum.
    /// </summary>
    internal class JavaEnumTemplate
    {
        // Store data into regular attributes

        public String value;

        // To reproduce abstract methods store them into delegates

        public Func<String> GetInfo;
        public Action<String, String> DoSomething;

        // Make the constructor private and define delegates

        private JavaEnumTemplate(String value, Func<String> GetInfo, Action<String, String> DoSomething)
        {
            this.value = value;
            this.GetInfo = GetInfo;
            this.DoSomething = DoSomething;
        }

        // Like in Java make a static function returning the list of "enums"

        public static JavaEnumTemplate[] Values()
        {
            return new JavaEnumTemplate[] {ONE, TWO};
        }

        // Define each "enums" as static constants

        public static readonly JavaEnumTemplate ONE = new JavaEnumTemplate("one", () =>
        {
            return "ONE";
        }, (arg0, arg1) =>
        {
            // Do something
        });

        public static readonly JavaEnumTemplate TWO = new JavaEnumTemplate("two", () =>
        {
            return "TWO";
        }, (arg0, arg1) =>
        {
            // Do something
        });

    }
}
