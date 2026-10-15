namespace Benchmarks;

/// <summary>One measured game-logic operation, called many times in a row by <see cref="OperationTimer"/>.</summary>
/// <remarks>Implemented by structs so the timer's generic loop is specialised and the call can be inlined.</remarks>
internal interface IBenchmarkOperation
{
    string Name { get; }

    void Run();
}
