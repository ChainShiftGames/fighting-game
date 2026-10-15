using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Analyzers;

/// <summary>Reports `async` methods, local functions and lambdas; the game logic is synchronous (ADR-0003).</summary>
// BannedSymbols.txt bans Task and System.Threading, but `async void` needs neither.
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AsyncAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FG0003";

    private static readonly DiagnosticDescriptor _rule = new(
        DiagnosticId,
        title: "async is banned in the game logic",
        messageFormat: "async code is banned; the game logic runs synchronously, one frame at a time (ADR-0003)",
        category: "Determinism",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: WellKnownDiagnosticTags.NotConfigurable);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(
            GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(
            AnalyzeModifiers,
            SyntaxKind.MethodDeclaration,
            SyntaxKind.LocalFunctionStatement,
            SyntaxKind.SimpleLambdaExpression,
            SyntaxKind.ParenthesizedLambdaExpression,
            SyntaxKind.AnonymousMethodExpression);
    }

    private static void AnalyzeModifiers(SyntaxNodeAnalysisContext context)
    {
        SyntaxTokenList modifiers = context.Node switch
        {
            MethodDeclarationSyntax method => method.Modifiers,
            LocalFunctionStatementSyntax localFunction => localFunction.Modifiers,
            AnonymousFunctionExpressionSyntax function => function.Modifiers,
            _ => default,
        };

        foreach (SyntaxToken modifier in modifiers)
        {
            if (modifier.IsKind(SyntaxKind.AsyncKeyword))
            {
                context.ReportDiagnostic(Diagnostic.Create(_rule, modifier.GetLocation()));
            }
        }
    }
}
