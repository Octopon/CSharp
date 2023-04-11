// See https://aka.ms/new-console-template for more information
using BenchmarkDotNet.Running;
using EfCore8vsDapper.Benchmarks;

BenchmarkRunner.Run<BenchmarkEfCore8VsDapper>();
Console.WriteLine("Hello, World!");
