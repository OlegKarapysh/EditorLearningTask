using BenchmarkDotNet.Running;
using EditorLearningTask.Benchmarks;

// Run all benchmark classes:   dotnet run -c Release --project EditorLearningTask.Benchmarks -- --filter *
// Run only batch benchmarks:   dotnet run -c Release --project EditorLearningTask.Benchmarks -- --filter *Batch*
// Run only single-line:        dotnet run -c Release --project EditorLearningTask.Benchmarks -- --filter *SingleLine*
BenchmarkSwitcher.FromAssembly(typeof(LexerBatchBenchmarks).Assembly).Run(args);
