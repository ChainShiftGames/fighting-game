using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Analyzers;

/// <summary>Reports every use of a floating-point type: float maths can differ between PCs (ADR-0003).</summary>
// BannedApiAnalyzers only sees member access on banned types, not `float x = 1;` or `var y = 2.5;`, hence this.
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FloatingPointAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FG0001";

    private static readonly DiagnosticDescriptor _rule = new(
        DiagnosticId,
        title: "Floating-point types are banned in the game logic",
        messageFormat: "'{0}' is floating-point; the game logic uses integers only (ADR-0003)",
        category: "Determinism",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: WellKnownDiagnosticTags.NotConfigurable);

    // Floating-point types that are not C# keywords, so SpecialType doesn't cover them.
    private static readonly ImmutableArray<string> _otherFloatingPointTypes =
        ["System.Half", "System.Runtime.InteropServices.NFloat"];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(
            GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();

        // Written types: declarations, casts, generic arguments, `var` that infers a float, `double.MaxValue`, ...
        context.RegisterSyntaxNodeAction(AnalyzeTypeName, SyntaxKind.PredefinedType, SyntaxKind.IdentifierName);

        // Float values that appear without a written type: `2.5`, `Math.Sqrt(x)`, `Math.PI`, int-to-float conversions.
        context.RegisterOperationAction(
            AnalyzeValue,
            OperationKind.Literal,
            OperationKind.Invocation,
            OperationKind.FieldReference,
            OperationKind.PropertyReference,
            OperationKind.Conversion);
    }

    private static void AnalyzeTypeName(SyntaxNodeAnalysisContext context)
    {
        if (context.SemanticModel.GetSymbolInfo(context.Node, context.CancellationToken).Symbol is ITypeSymbol type
            && ContainsFloatingPoint(type))
        {
            Report(context.ReportDiagnostic, context.Node.GetLocation(), type);
        }
    }

    private static void AnalyzeValue(OperationAnalysisContext context)
    {
        IOperation operation = context.Operation;

        // Explicit casts are already reported through their written type.
        if (operation is IConversionOperation { IsImplicit: false })
        {
            return;
        }

        if (operation.Type is { } type && ContainsFloatingPoint(type))
        {
            Report(context.ReportDiagnostic, operation.Syntax.GetLocation(), type);
        }
    }

    private static bool ContainsFloatingPoint(ITypeSymbol type)
    {
        switch (type)
        {
            case IArrayTypeSymbol array:
                return ContainsFloatingPoint(array.ElementType);
            case IPointerTypeSymbol pointer:
                return ContainsFloatingPoint(pointer.PointedAtType);
            case INamedTypeSymbol named:
                if (IsFloatingPoint(named))
                {
                    return true;
                }

                // float?, (int, float), List<float>, ...
                foreach (ITypeSymbol argument in named.TypeArguments)
                {
                    if (ContainsFloatingPoint(argument))
                    {
                        return true;
                    }
                }

                return false;
            default:
                return false;
        }
    }

    private static bool IsFloatingPoint(INamedTypeSymbol type)
    {
        if (type.SpecialType is SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_Decimal)
        {
            return true;
        }

        string name = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "");
        return _otherFloatingPointTypes.Contains(name);
    }

    private static void Report(System.Action<Diagnostic> report, Location location, ITypeSymbol type) =>
        report(Diagnostic.Create(_rule, location, type.ToDisplayString()));
}
