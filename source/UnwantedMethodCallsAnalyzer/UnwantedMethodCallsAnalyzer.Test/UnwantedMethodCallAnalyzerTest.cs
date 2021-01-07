﻿using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = UnwantedMethodCallsAnalyzer.Test.CSharpAnalyzerVerifier<UnwantedMethodCallsAnalyzer.UnwantedMethodCallAnalyzer>;

namespace UnwantedMethodCallsAnalyzer.Test
{
    public class UnwantedMethodCallAnalyzerTest
    {
        private const string AdditionalFileText = @"
{
  ""UnwantedMethods"": [
    {
      // test comment for json parsing
      ""TypeNamespace"": ""System.Diagnostics.Process"",
      ""MethodName"": ""Start"",
      ""ExcludeCheckingTypes"": [
        ""ConsoleApplication1.ShouldBeIgnored""
      ]
    }
  ]
}
";

        private static readonly (string AdditionalFileName, string AdditionalFileText)[] AdditionalFiles = new[]
        {
            (UnwantedMethodCallAnalyzer.ConfigurationFileName, AdditionalFileText)
        };

        [Fact]
        public async Task EmptySourceSucceeds()
        {
            var test = @"";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task SourceWithBadCallsFails()
        {
            var test = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ConsoleApplication1
{
    class TypeName
    {
        public TypeName()
        {
            {|#0:Process.Start|}(new ProcessStartInfo(""constructor"")
            {
                Arguments = ""commit -m message && exec maliciousProcess""
            });
        }
        
        void BadMethod()
        {
            {|#1:Process.Start|}(new ProcessStartInfo(""testMethod"")
            {
                Arguments = ""commit -m message && exec maliciousProcess""
            });
        }

        void GoodMethod()
        {
        }
    }

    class ShouldBeIgnored
    {        
        void BadMethod()
        {
            Process.Start(new ProcessStartInfo(""testMethod"")
            {
                Arguments = ""commit -m message && exec maliciousProcess""
            });
        }
    }
}";

            var expectedMessage = UnwantedMethodCallAnalyzer.MessageFormat.Replace("{0}", "System.Diagnostics.Process.Start");
            var expectedRule = new DiagnosticDescriptor(UnwantedMethodCallAnalyzer.DiagnosticId,
                UnwantedMethodCallAnalyzer.Title,
                expectedMessage,
                UnwantedMethodCallAnalyzer.Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);
            var result1 = new DiagnosticResult(expectedRule).WithLocation(0);
            var result2 = new DiagnosticResult(expectedRule).WithLocation(1);

            await VerifyCS.VerifyAnalyzerAsync(test, AdditionalFiles, new[] {result1, result2});
        }

        [Fact]
        public async Task SourceWithNoUnwantedCallsSucceeds()
        {
            var test = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ConsoleApplication1
{
    class TypeName
    {
        public TypeName()
        {
        }

        void GoodMethod()
        {
        }
    }
}";
            
            await VerifyCS.VerifyAnalyzerAsync(test, AdditionalFiles);
        }

        [Fact]
        public async Task EmptyJsonAdditionalFileTextSucceeds()
        {
            var emptyJson = "{}";
            var test = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ConsoleApplication1
{
    class TypeName
    {            
        void BadMethod()
        {
            Process.Start(new ProcessStartInfo(""testMethod"")
            {
                Arguments = ""commit -m message && exec maliciousProcess""
            });
        }
    }
}";

            var additionalFiles = new[] {(UnwantedMethodCallAnalyzer.ConfigurationFileName, additionalFileText: emptyJson)};
            await VerifyCS.VerifyAnalyzerAsync(test, additionalFiles);
        }
    }
}