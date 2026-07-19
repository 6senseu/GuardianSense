using Guardian.Service;
using Guardian.Shared;
using Guardian.Shared.Storage;
using Guardian.Analysis.Hashing;
using Guardian.Analysis.Pipeline;
using Guardian.Analysis.Providers;
using Guardian.Analysis.Risk;
using Guardian.Analysis.Services;
using Guardian.Analysis.Signatures;

HostApplicationBuilder builder =
    Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddProvider(
    new FileLoggerProvider(GuardianPaths.LogFilePath));

// Overrides appsettings.json defaults with the shared, Dashboard-editable settings file.
// Requires a service restart to take effect (no live IPC in this MVP).
builder.Configuration.AddJsonFile(
    GuardianPaths.SettingsFilePath,
    optional: true,
    reloadOnChange: false);

builder.Services.Configure<GuardianSettings>(
    builder.Configuration.GetSection(
        GuardianSettings.SectionName));

// Analysis services
builder.Services.AddSingleton<HashCalculator>();
builder.Services.AddSingleton<FileSignatureDetector>();
builder.Services.AddSingleton<FileRiskAssessor>();
builder.Services.AddSingleton<WinTrustService>();

builder.Services.AddHttpClient<VirusTotalService>(client =>
{
    client.BaseAddress = new Uri("https://www.virustotal.com/api/v3/");
    client.Timeout = TimeSpan.FromSeconds(15);
});

// Analysis providers
builder.Services.AddSingleton<IAnalysisProvider, HashProvider>();
builder.Services.AddSingleton<IAnalysisProvider, MagicByteProvider>();
builder.Services.AddSingleton<IAnalysisProvider, CloudReputationProvider>();
builder.Services.AddSingleton<IAnalysisProvider, RiskProvider>();
builder.Services.AddSingleton<IAnalysisProvider, AuthenticodeProvider>();

// Analysis pipeline
builder.Services.AddSingleton<AnalysisPipeline>();

// Guardian service
builder.Services.AddSingleton<ReportStore>();
builder.Services.AddSingleton<QuarantineStore>();
builder.Services.AddSingleton<QuarantineManager>();
builder.Services.AddSingleton<DownloadWatcher>();
builder.Services.AddHostedService<Worker>();

IHost host = builder.Build();
host.Run();