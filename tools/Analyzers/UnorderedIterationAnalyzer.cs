using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Analyzers;

/// <summary>Reports `foreach` over hash-based collections, whose order isn't guaranteed (ADR-0003).</summary>
// BannedSymbols.txt bans explicit GetEnumerator() calls, but BannedApiAnalyzers doesn't see the one inside `foreach`.
// Lookups stay allowed; only iteration order is the risk.
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UnorderedIterationAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "FG0002";

    private static readonly DiagnosticDescriptor _rule = new(
        DiagnosticId,
        title: "Iterating a hash-based collection is banned in the game logic",
        messageFormat: "Iterating '{0}' has no guaranteed order; use an array or list (ADR-0003)",
        category: "Determinism",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: WellKnownDiagnosticTags.NotConfigurable);

    // Only the type itself and its base classes are checked, not its interfaces: SortedDictionary implements
    // IDictionary but iterates in key order.
    private static readonly ImmutableArray<string> _unorderedTypeNames =
    [
        "System.Collections.Hashtable",
        "System.Collections.IDictionary",
        "System.Collections.Generic.Dictionary`2",
        "System.Collections.Generic.Dictionary`2+KeyCollection",
        "System.Collections.Generic.Dictionary`2+ValueCollection",
        "System.Collections.Generic.HashSet`1",
        "System.Collections.Generic.IDictionary`2",
        "System.Collections.Generic.IReadOnlyDictionary`2",
        "System.Collections.Generic.ISet`1",
        "System.Collections.Generic.IReadOnlySet`1",
        "System.Collections.Frozen.FrozenDictionary`2",
        "System.Collections.Frozen.FrozenSet`1",
        "System.Collections.Immutable.ImmutableDictionary`2",
        "System.Collections.Immutable.ImmutableHashSet`1",
        "System.Collections.Immutable.IImmutableDictionary`2",
        "System.Collections.Immutable.IImmutableSet`1",
    ];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(
            GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        ImmutableHashSet<ITypeSymbol>.Builder unorderedTypes =
            ImmutableHashSet.CreateBuilder<ITypeSymbol>(SymbolEqualityComparer.Default);
        foreach (string name in _unorderedTypeNames)
        {
            if (context.Compilation.GetTypeByMetadataName(name) is { } type)
            {
                unorderedTypes.Add(type);
            }
        }

        ImmutableHashSet<ITypeSymbol> types = unorderedTypes.ToImmutable();
        context.RegisterOperationAction(c => AnalyzeLoop(c, types), OperationKind.Loop);
    }

    private static void AnalyzeLoop(OperationAnalysisContext context, ImmutableHashSet<ITypeSymbol> unorderedTypes)
    {
        if (context.Operation is not IForEachLoopOperation loop)
        {
            return;
        }

        IOperation collection = loop.Collection;
        while (collection is IConversionOperation { IsImplicit: true } conversion)
        {
            collection = conversion.Operand;
        }

        for (ITypeSymbol? type = collection.Type; type is not null; type = type.BaseType)
        {
            if (unorderedTypes.Contains(type.OriginalDefinition))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(_rule, collection.Syntax.GetLocation(), collection.Type!.ToDisplayString()));
                return;
            }
        }
    }
}
