using BenchmarkDotNet.Running;

// 运行指定 Benchmark 类，或不带参数时弹出交互菜单
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
