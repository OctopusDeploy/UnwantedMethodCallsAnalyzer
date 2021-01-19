using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using NUnit.Framework;
using Octopus.RoslynAnalysers;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.NUnit.AnalyzerVerifier<Octopus.RoslynAnalysers.TheProcessStartMethodShouldNotBeCalledAnalyzer>;
using static Microsoft.CodeAnalysis.Testing.DiagnosticResult;

namespace Tests
{
    public class TheProcessStartMethodShouldNotBeCalledAnalyzerFixture
    {
        [Test]
        public async Task EmptySourceSucceeds()
            => await Verify.VerifyAnalyzerAsync("");

        [Test]
        public async Task SourceWithBadCallsFails()
        {
            const string source = @"
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
#pragma warning disable Octopus_ProcessStart
            Process.Start(new ProcessStartInfo(""testMethod"")
            {
                Arguments = ""commit -m message && exec maliciousProcess""
            });
        }
    }
}";

            var result1 = new DiagnosticResult(TheProcessStartMethodShouldNotBeCalledAnalyzer.Rule).WithLocation(0);
            var result2 = new DiagnosticResult(TheProcessStartMethodShouldNotBeCalledAnalyzer.Rule).WithLocation(1);

            await Verify.VerifyAnalyzerAsync(
                source,
                result1,
                result2
            );
        }

        [Test]
        public async Task SourceWithNoUnwantedCallsSucceeds()
        {
            const string source = @"
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

            await Verify.VerifyAnalyzerAsync(source);
        }
    }
}