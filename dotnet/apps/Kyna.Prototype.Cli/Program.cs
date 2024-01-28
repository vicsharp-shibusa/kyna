using Kyna.ApplicationServices.Cli;
using Kyna.ApplicationServices.Configuration;
using Kyna.ApplicationServices.DataImport;
using Kyna.ApplicationServices.Logging;
using Kyna.Common;
using Kyna.Common.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;

string[] tickers = [
    "MMM",
    "AOS",
    "ABT",
    "ABBV",
    "ABMD",
    "ACN",
    "ATVI",
    "ADM",
    "ADBE",
    "AAP",
    "AMD",
    "AES",
    "AFL",
    "A",
    "APD",
    "AKAM",
    "ALK",
    "ALB",
    "ARE",
    "ALGN",
    "ALLE",
    "LNT",
    "ALL",
    "GOOGL",
    "GOOG",
    "MO",
    "AMZN",
    "AMCR",
    "AEE",
    "AAL",
    "AEP",
    "AXP",
    "AIG",
    "AMT",
    "AWK",
    "AMP",
    "ABC",
    "AME",
    "AMGN",
    "APH",
    "ADI",
    "ANSS",
    "ANTM",
    "AON",
    "APA",
    "AAPL",
    "AMAT",
    "APTV",
    "ANET",
    "AJG",
    "AIZ",
    "T",
    "ATO",
    "ADSK",
    "ADP",
    "AZO",
    "AVB",
    "AVY",
    "BKR",
    "BLL",
    "BAC",
    "BBWI",
    "BAX",
    "BDX",
    "BRK",
    "BBY",
    "BIO",
    "TECH",
    "BIIB",
    "BLK",
    "BK",
    "BA",
    "BKNG",
    "BWA",
    "BXP",
    "BSX",
    "BMY",
    "AVGO",
    "BR",
    "BRO",
    "BF",
    "CHRW",
    "CDNS",
    "CZR",
    "CPB",
    "COF",
    "CAH",
    "KMX",
    "CCL",
    "CARR",
    "CTLT",
    "CAT",
    "CBOE",
    "CBRE",
    "CDW",
    "CE",
    "CNC",
    "CNP",
    "CDAY",
    "CERN",
    "CF",
    "CRL",
    "SCHW",
    "CHTR",
    "CVX",
    "CMG",
    "CB",
    "CHD",
    "CI",
    "CINF",
    "CTAS",
    "CSCO",
    "C",
    "CFG",
    "CTXS",
    "CLX",
    "CME",
    "CMS",
    "KO",
    "CTSH",
    "CL",
    "CMCSA",
    "CMA",
    "CAG",
    "COP",
    "ED",
    "STZ",
    "CPRT",
    "GLW",
    "CTVA",
    "COST",
    "CTRA",
    "CCI",
    "CSX",
    "CMI",
    "CVS",
    "DHI",
    "DHR",
    "DRI",
    "DVA",
    "DE",
    "DAL",
    "XRAY",
    "DVN",
    "DXCM",
    "FANG",
    "DLR",
    "DFS",
    "DISCA",
    "DISCK",
    "DISH",
    "DG",
    "DLTR",
    "D",
    "DPZ",
    "DOV",
    "DOW",
    "DTE",
    "DUK",
    "DRE",
    "DD",
    "DXC",
    "EMN",
    "ETN",
    "EBAY",
    "ECL",
    "EIX",
    "EW",
    "EA",
    "LLY",
    "EMR",
    "ENPH",
    "ETR",
    "EOG",
    "EFX",
    "EQIX",
    "EQR",
    "ESS",
    "EL",
    "ETSY",
    "RE",
    "EVRG",
    "ES",
    "EXC",
    "EXPE",
    "EXPD",
    "EXR",
    "XOM",
    "FFIV",
    "FB",
    "FAST",
    "FRT",
    "FDX",
    "FIS",
    "FITB",
    "FRC",
    "FE",
    "FISV",
    "FLT",
    "FMC",
    "F",
    "FTNT",
    "FTV",
    "FBHS",
    "FOXA",
    "FOX",
    "BEN",
    "FCX",
    "GPS",
    "GRMN",
    "IT",
    "GNRC",
    "GD",
    "GE",
    "GIS",
    "GM",
    "GPC",
    "GILD",
    "GPN",
    "GL",
    "GS",
    "HAL",
    "HBI",
    "HAS",
    "HCA",
    "PEAK",
    "HSIC",
    "HES",
    "HPE",
    "HLT",
    "HOLX",
    "HD",
    "HON",
    "HRL",
    "HST",
    "HWM",
    "HPQ",
    "HUM",
    "HBAN",
    "HII",
    "IBM",
    "IEX",
    "IDXX",
    "INFO",
    "ITW",
    "ILMN",
    "INCY",
    "IR",
    "INTC",
    "ICE",
    "IFF",
    "IP",
    "IPG",
    "INTU",
    "ISRG",
    "IVZ",
    "IPGP",
    "IQV",
    "IRM",
    "JBHT",
    "JKHY",
    "J",
    "SJM",
    "JNJ",
    "JCI",
    "JPM",
    "JNPR",
    "KSU",
    "K",
    "KEY",
    "KEYS",
    "KMB",
    "KIM",
    "KMI",
    "KLAC",
    "KHC",
    "KR",
    "LHX",
    "LH",
    "LRCX",
    "LW",
    "LVS",
    "LEG",
    "LDOS",
    "LEN",
    "LNC",
    "LIN",
    "LYV",
    "LKQ",
    "LMT",
    "L",
    "LOW",
    "LUMN",
    "LYB",
    "MTB",
    "MRO",
    "MPC",
    "MKTX",
    "MAR",
    "MMC",
    "MLM",
    "MAS",
    "MA",
    "MTCH",
    "MKC",
    "MCD",
    "MCK",
    "MDT",
    "MRK",
    "MET",
    "MTD",
    "MGM",
    "MCHP",
    "MU",
    "MSFT",
    "MAA",
    "MRNA",
    "MHK",
    "TAP",
    "MDLZ",
    "MPWR",
    "MNST",
    "MCO",
    "MS",
    "MSI",
    "MSCI",
    "NDAQ",
    "NTAP",
    "NFLX",
    "NWL",
    "NEM",
    "NWSA",
    "NWS",
    "NEE",
    "NLSN",
    "NKE",
    "NI",
    "NSC",
    "NTRS",
    "NOC",
    "NLOK",
    "NCLH",
    "NRG",
    "NUE",
    "NVDA",
    "NVR",
    "NXPI",
    "ORLY",
    "OXY",
    "ODFL",
    "OMC",
    "OKE",
    "ORCL",
    "OGN",
    "OTIS",
    "PCAR",
    "PKG",
    "PH",
    "PAYX",
    "PAYC",
    "PYPL",
    "PENN",
    "PNR",
    "PBCT",
    "PEP",
    "PKI",
    "PFE",
    "PM",
    "PSX",
    "PNW",
    "PXD",
    "PNC",
    "POOL",
    "PPG",
    "PPL",
    "PFG",
    "PG",
    "PGR",
    "PLD",
    "PRU",
    "PTC",
    "PEG",
    "PSA",
    "PHM",
    "PVH",
    "QRVO",
    "QCOM",
    "PWR",
    "DGX",
    "RL",
    "RJF",
    "RTX",
    "O",
    "REG",
    "REGN",
    "RF",
    "RSG",
    "RMD",
    "RHI",
    "ROK",
    "ROL",
    "ROP",
    "ROST",
    "RCL",
    "SPGI",
    "CRM",
    "SBAC",
    "SLB",
    "STX",
    "SEE",
    "SRE",
    "NOW",
    "SHW",
    "SPG",
    "SWKS",
    "SNA",
    "SO",
    "LUV",
    "SWK",
    "SBUX",
    "STT",
    "STE",
    "SYK",
    "SIVB",
    "SYF",
    "SNPS",
    "SYY",
    "TMUS",
    "TROW",
    "TTWO",
    "TPR",
    "TGT",
    "TEL",
    "TDY",
    "TFX",
    "TER",
    "TSLA",
    "TXN",
    "TXT",
    "COO",
    "HIG",
    "HSY",
    "MOS",
    "TRV",
    "DIS",
    "TMO",
    "TJX",
    "TSCO",
    "TT",
    "TDG",
    "TRMB",
    "TFC",
    "TWTR",
    "TYL",
    "TSN",
    "USB",
    "UDR",
    "ULTA",
    "UAA",
    "UA",
    "UNP",
    "UAL",
    "UPS",
    "URI",
    "UNH",
    "UHS",
    "VLO",
    "VTR",
    "VRSN",
    "VRSK",
    "VZ",
    "VRTX",
    "VFC",
    "VIAC",
    "VTRS",
    "V",
    "VNO",
    "VMC",
    "WRB",
    "GWW",
    "WAB",
    "WBA",
    "WMT",
    "WM",
    "WAT",
    "WEC",
    "WFC",
    "WELL",
    "WST",
    "WDC",
    "WU",
    "WRK",
    "WY",
    "WHR",
    "WMB",
    "WLTW",
    "WYNN",
    "XEL",
    "XLNX",
    "XYL",
    "YUM",
    "ZBRA",
    "ZBH",
    "ZION",
    "ZTS"
];

ILogger<Program>? logger = null;
IConfiguration? configuration;

int exitCode = -1;

Guid processId = Guid.NewGuid();

string appName = Assembly.GetExecutingAssembly().GetName().Name ?? throw new Exception("Could not determine app name.");
Debug.Assert(appName != null);

string defaultScope = appName ?? nameof(Program);

DatabaseLogService? dbLogService = null;
ApiTransactionService? apiTransactionService = null;

Stopwatch timer = Stopwatch.StartNew();

Config? config = null;

try
{
    Configure();

    config = HandleArguments(args);


    if (config.ShowHelp)
    {
        ShowHelp();
    }
    else
    {
        Debug.Assert(apiTransactionService != null);

        string apiKey = "demo";

        List<Task> tasks = new(2000);

        using (HttpClient httpClient = new HttpClient())
        {
            var reqHeaders = httpClient.DefaultRequestHeaders;

            var reqHeadersJson = System.Text.Json.JsonSerializer.Serialize(reqHeaders);

            foreach (string symbol in tickers)
            {
                string apiUrl = $"https://www.alphavantage.co/query?function=TIME_SERIES_DAILY&symbol={symbol}&apikey={apiKey}";
                HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    var responseHeaders = response.Headers;
                    var responseHeadersJson = System.Text.Json.JsonSerializer.Serialize(responseHeaders);

                    string responseData = await response.Content.ReadAsStringAsync();

                    tasks.Add(apiTransactionService.RecordTransactionAsync("GET", apiUrl, "www.alphavantage.co", "Price", response,
                        reqHeaders, null, symbol));
                    //await apiTransactionService.RecordTransactionAsync("GET", apiUrl, "www.alphavantage.co", "Price", response,
                    //    reqHeaders, null, symbol);

                    Communicate($"{symbol}:\t{response.StatusCode}");
                }
                else
                {
                    Communicate($"Error on {symbol}: {response.StatusCode}", true, LogLevel.Error, defaultScope);
                }
            }
        }

        Task.WaitAll(tasks.ToArray());
    }
    exitCode = 0;
}
catch (ArgumentException exc)
{
    exitCode = 1;
    Communicate(exc.ToString(), true);
    KLogger.LogCritical(exc, defaultScope, processId);
}
catch (Exception exc)
{
    exitCode = 2;
    Communicate(exc.ToString(), true);
    KLogger.LogCritical(exc, defaultScope, processId);
}
finally
{
    if (!(config?.ShowHelp ?? false))
    {
        // test log finished event.
        KLogger.LogEvent(EventIdRepository.GetAppFinishedEvent(config!), processId);
    }

    timer.Stop();

    Communicate($"Completed in {timer.Elapsed.ConvertToText()}");

    await Task.Delay(200);

    Environment.Exit(exitCode);
}

void Communicate(string message, bool force = false, LogLevel logLevel = LogLevel.None,
    string? scope = null)
{
    if (force || (config?.Verbose ?? false))
    {
        Console.WriteLine(message);
    }

    KLogger.Log(logLevel, message, scope ?? defaultScope, processId);
}

void ShowHelp()
{
    CliArg[] args = CliHelper.GetDefaultArgDescriptions().Union(Array.Empty<CliArg>()).ToArray();

    Communicate($"{config.AppName} {config.AppVersion}".Trim(), true);
    Communicate("", true);
    if (!string.IsNullOrWhiteSpace(config.Description))
    {
        Communicate(config.Description, true);
        Communicate("", true);
    }
    Communicate(CliHelper.FormatArguments(args), true);
}

Config HandleArguments(string[] args)
{
    var config = new Config(Assembly.GetExecutingAssembly().GetName().Name ?? nameof(Program), "v1",
        "CLI for testing various things; a throw-away app.");

    args = CliHelper.HydrateDefaultAppConfig(args, config);

    for (int i = 0; i < args.Length; i++)
    {
        string argument = args[i].ToLower();

        switch (argument)
        {
            default:
                throw new Exception($"Unknown argument: {args[i]}");
        }
    }

    return config;
}

void Configure()
{
    IConfigurationBuilder builder = new ConfigurationBuilder()
        .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile("secrets.json", optional: false, reloadOnChange: true);

    configuration = builder.Build();

    var dbDefs = CliHelper.GetDbDefs(configuration);

    var logDef = dbDefs.FirstOrDefault(d => d.Name == ConfigKeys.DbKeys.Logs);
    var importDef = dbDefs.FirstOrDefault(d => d.Name == ConfigKeys.DbKeys.Imports);

    logger = Kyna.ApplicationServices.Logging.LoggerFactory.Create<Program>(logDef);
    KLogger.SetLogger(logger);

    dbLogService = new(logDef);
    apiTransactionService = new(importDef);
}

class Config(string appName, string appVersion, string? description = null) : CliConfigBase(appName, appVersion, description)
{
}

