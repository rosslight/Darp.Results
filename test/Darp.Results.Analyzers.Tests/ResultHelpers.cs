namespace Darp.Results.Analyzers.Tests;

public static class ResultHelpers
{
    public static string InMethod(string body)
    {
        return $$"""
            using Darp.Results;

            public static class MyTestClass
            {
                public static void MyMethod() {
            {{body}}
                }
            }
            """;
    }
}
