using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Analyzers;

/// <summary>Reports mutable static state: all match state lives in the game state, so snapshots catch it.</summary>
// ADR-0003. Static readonly fields and consts stay allowed (lookup tables, descriptors). A static readonly array's
// elements can still be written; that is left to review because lookup tables use exactly that shape.
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MutableStaticStateAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FG0004";

    private static readonly DiagnosticDescriptor _rule = new(
        DiagnosticId,
        title: "Mutable static state is banned in the game logic",
        messageFormat: "Static '{0}' can change at run time; keep match state in the game state (ADR-0003)",
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
        context.RegisterSymbolAction(AnalyzeMember, SymbolKind.Field, SymbolKind.Property, SymbolKind.Event);
    }

    private static void AnalyzeMember(SymbolAnalysisContext context)
    {
        ISymbol member = context.Symbol;
        if (!member.IsStatic || member.IsImplicitlyDeclared || !IsMutable(member))
        {
            return;
        }

        foreach (Location location in member.Locations)
        {
            context.ReportDiagnostic(Diagnostic.Create(_rule, location, member.Name));
        }
    }

    private static bool IsMutable(ISymbol member) => member switch
    {
        IFieldSymbol field => !field.IsConst && !field.IsReadOnly,
        IPropertySymbol property => property.SetMethod is not null,
        IEventSymbol => true,
        _ => false,
    };
}
