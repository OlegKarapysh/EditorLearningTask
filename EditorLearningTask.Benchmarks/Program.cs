using BenchmarkDotNet.Running;
using EditorLearningTask.Benchmarks;

BenchmarkRunner.Run<LexerBenchmarks>(args: args);
