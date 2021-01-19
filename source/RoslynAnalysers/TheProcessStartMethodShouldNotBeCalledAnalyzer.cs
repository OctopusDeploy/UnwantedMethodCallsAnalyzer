using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Diagnostics;

namespace Octopus.RoslynAnalysers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TheProcessStartMethodShouldNotBeCalledAnalyzer : DiagnosticAnalyzer
    {
        const string DiagnosticId = "Octopus_ProcessStart";

        const string Title = "Process.Start should only be used sparingly and deliberately";

        const string MessageFormat = "Process.Start can leave us open to malicious code execution";
        const string Category = "Octopus";
        const string Description = "Process.Start should only be used sparingly and deliberately as it opens up opportunties for malicious code execution.";

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Error,
            true,
            Description
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(CheckUnwantedMethodCalls, SyntaxKind.InvocationExpression);
        }

        void CheckUnwantedMethodCalls(SyntaxNodeAnalysisContext context)
        {
            var expressionSyntax = context.Node as InvocationExpressionSyntax;
            var memberAccessExpression = expressionSyntax?.Expression as MemberAccessExpressionSyntax;
            if (memberAccessExpression == null)
                return;

            var memberSymbol = ModelExtensions.GetSymbolInfo(context.SemanticModel, memberAccessExpression).Symbol as IMethodSymbol;
            if (memberSymbol == null)
                return;

            if (
                memberSymbol.ContainingType.ToString() == typeof(Process).FullName &&
                memberSymbol.Name == nameof(Process.Start)
            )
            {
                var diagnostic = Diagnostic.Create(Rule, memberAccessExpression.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}