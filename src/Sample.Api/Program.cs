using Sample.Api;

var builder = WebApplication.CreateBuilder(args);

// 從環境變數或 appsettings.json 讀取服務名稱
var serviceName = builder.Configuration["OTEL_SERVICE_NAME"] ?? "Sample.Api";

builder.Services.AddControllers();
builder.Services.AddHttpClient(); // Add HttpClient for dependency injection
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 將 Observability 的設定移至此處
builder.Services.AddObservability(serviceName, builder.Configuration);

var app = builder.Build();

// 強制啟用 Swagger，無論環境為何
app.UseSwagger();
app.UseSwaggerUI();

// 移除 UseHttpsRedirection 和 UseAuthorization 以簡化偵錯
// app.UseHttpsRedirection();
// app.UseAuthorization();

app.MapPrometheusScrapingEndpoint();
app.MapControllers();

app.Run(); 