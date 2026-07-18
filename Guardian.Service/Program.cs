using Guardian.Service;
using Guardian.Shared;
using Guardian.Analysis.Risk;
using Guardian.Analysis.Signatures;
using Guardian.Analysis.Hashing;
using Guardian.Analysis.Pipeline;
using Guardian.Analysis.Providers;

HostApplicationBuilder builder =
    Host.CreateApplicationBuilder(args);

string guardianDirectory = Path.Combine(
    Environment.GetFolderPath(
        Environment.SpecialFolder.CommonApplicationData),
    "Guardian");

string logFilePath = Path.Combine(
    guardianDirectory,
    "Logs",
    "guardian.log");

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddProvider(
    new FileLoggerProvider(logFilePath));

builder.Services.Configure<GuardianSettings>(
    builder.Configuration.GetSection(
        GuardianSettings.SectionName));

builder.Services.AddSingleton<HashCalculator>();
builder.Services.AddSingleton<FileSignatureDetector>();
builder.Services.AddSingleton<IAnalysisProvider, AuthenticodeProvider>();
builder.Services.AddSingleton<FileRiskAssessor>();
builder.Services.AddSingleton<AnalysisPipeline>();

builder.Services.AddSingleton<ReportStore>();
builder.Services.AddSingleton<DownloadWatcher>();
builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton<IAnalysisProvider, HashProvider>();
builder.Services.AddSingleton<IAnalysisProvider, MagicByteProvider>();
builder.Services.AddSingleton<IAnalysisProvider, RiskProvider>();

IHost host = builder.Build();
host.Run();