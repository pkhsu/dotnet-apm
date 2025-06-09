# API Instrumentation Guide

本文件詳細說明 Sample.Api 中所有 API 端點的儀表化 (Instrumentation) 配置、分散式追蹤實作和觀測性最佳實踐。

## API 端點概覽

Sample.Api 提供以下 HTTP 端點：

| 端點 | HTTP 方法 | 控制器 | 功能 | 複雜度 |
|------|-----------|--------|------|--------|
| `/WeatherForecast` | GET | WeatherForecastController | 天氣預報查詢 | 簡單 |
| `/api/Order/create` | POST | OrderController | 訂單建立流程 | 複雜 |
| `/metrics` | GET | Built-in | Prometheus 指標 | 系統 |
| `/swagger` | GET | Swagger UI | API 文件 | 系統 |

## 1. WeatherForecast API

### 端點詳情
```csharp
[HttpGet(Name = "GetWeatherForecast")]
public IEnumerable<WeatherForecast> Get()
```

### 基本資訊
- **路由**: `/WeatherForecast`
- **HTTP 方法**: GET
- **回應型別**: `IEnumerable<WeatherForecast>`
- **認證**: 不需要
- **參數**: 無

### 自動儀表化
由於使用 `OpenTelemetry.Instrumentation.AspNetCore`，此端點自動產生：

#### HTTP Metrics
```
http_server_request_duration_seconds_count{
  job="sample-api",
  http_request_method="GET",
  http_route="WeatherForecast",
  http_response_status_code="200"
}

http_server_request_duration_seconds_sum{
  job="sample-api",
  http_request_method="GET",
  http_route="WeatherForecast"
}

http_server_request_duration_seconds_bucket{
  job="sample-api",
  http_request_method="GET",
  http_route="WeatherForecast",
  le="0.1"
}
```

#### HTTP Traces (Spans)
```
Span: GET /WeatherForecast
├── span.kind: server
├── http.method: GET
├── http.route: WeatherForecast
├── http.status_code: 200
├── http.user_agent: curl/7.64.1
└── duration: ~50ms
```

### 手動日誌記錄
```csharp
_logger.LogInformation("Generating weather forecast...");
```

#### 產生的日誌
```json
{
  "timestamp": "2024-01-15T10:30:45.123Z",
  "level": "Information",
  "message": "Generating weather forecast...",
  "source": "Sample.Api.Controllers.WeatherForecastController",
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736",
  "spanId": "00f067aa0ba902b7"
}
```

## 2. Order API (分散式追蹤範例)

### 端點詳情
```csharp
[HttpPost("create")]
public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
```

### 基本資訊
- **路由**: `/api/Order/create`
- **HTTP 方法**: POST
- **請求型別**: `CreateOrderRequest`
- **回應型別**: `CreateOrderResponse`
- **認證**: 不需要

### 請求/回應模型
```csharp
// 請求模型
public record CreateOrderRequest(
    string OrderId,
    string CustomerId, 
    string ProductId,
    int Quantity,
    decimal Amount
);

// 回應模型
public record CreateOrderResponse(
    string OrderId,
    string Status,
    string? PaymentId,
    DateTime CreatedAt
);
```

### 自定義 ActivitySource
```csharp
private static readonly ActivitySource ActivitySource = new("Sample.Api.OrderService");
```

### 分散式追蹤架構

#### 主要 Span (Root)
```csharp
using var activity = ActivitySource.StartActivity("CreateOrder");
activity?.SetTag("order.id", request.OrderId);
activity?.SetTag("customer.id", request.CustomerId);
```

產生的追蹤結構：
```
Trace: CreateOrder
├── Span: CreateOrder (Root)
│   ├── order.id: "ORD-12345"
│   ├── customer.id: "CUST-67890"
│   ├── duration: 850ms
│   └── Child Spans:
│       ├── CheckInventory (200ms)
│       ├── ProcessPayment (400ms)
│       ├── SendNotification (100ms)
│       └── UpdateInventory (150ms)
```

#### 子 Span 實作

##### 1. CheckInventory
```csharp
private async Task<InventoryCheckResult> CheckInventory(string productId, int quantity)
{
    using var activity = ActivitySource.StartActivity("CheckInventory");
    activity?.SetTag("product.id", productId);
    activity?.SetTag("quantity", quantity);
    
    _logger.LogInformation("Checking inventory for ProductId: {ProductId}, Quantity: {Quantity}", 
        productId, quantity);

    // 模擬 API 呼叫延遲
    await Task.Delay(Random.Shared.Next(100, 300));
    
    var available = Random.Shared.NextDouble() > 0.1; // 90% 成功率
    
    return new InventoryCheckResult(available, productId);
}
```

##### 2. ProcessPayment
```csharp
private async Task<PaymentResult> ProcessPayment(string customerId, decimal amount)
{
    using var activity = ActivitySource.StartActivity("ProcessPayment");
    activity?.SetTag("customer.id", customerId);
    activity?.SetTag("amount", amount);

    // 模擬支付處理延遲
    await Task.Delay(Random.Shared.Next(200, 500));
    
    var success = Random.Shared.NextDouble() > 0.05; // 95% 成功率
    
    if (!success)
    {
        activity?.SetStatus(ActivityStatusCode.Error, "Payment processing failed");
    }
    
    return new PaymentResult(success, paymentId);
}
```

##### 3. SendNotification
```csharp
private async Task SendNotification(string customerId, string orderId)
{
    using var activity = ActivitySource.StartActivity("SendNotification");
    activity?.SetTag("customer.id", customerId);
    activity?.SetTag("order.id", orderId);

    await Task.Delay(Random.Shared.Next(50, 150));
    
    _logger.LogInformation("Notification sent successfully for CustomerId: {CustomerId}, OrderId: {OrderId}", 
        customerId, orderId);
}
```

##### 4. UpdateInventory
```csharp
private async Task UpdateInventory(string productId, int quantity)
{
    using var activity = ActivitySource.StartActivity("UpdateInventory");
    activity?.SetTag("product.id", productId);
    activity?.SetTag("quantity", quantity);

    await Task.Delay(Random.Shared.Next(80, 200));
    
    _logger.LogInformation("Inventory updated successfully for ProductId: {ProductId}", productId);
}
```

### 錯誤處理與追蹤
```csharp
try
{
    // 業務邏輯
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error creating order for OrderId: {OrderId}", request.OrderId);
    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
    return StatusCode(500, "Internal server error");
}
```

## 3. 系統端點

### Prometheus Metrics 端點
- **路由**: `/metrics`
- **功能**: 暴露 Prometheus 格式指標
- **配置**: `app.MapPrometheusScrapingEndpoint()`

#### 暴露的指標類型
1. **HTTP 請求指標**
   - Counter: 請求總數
   - Histogram: 請求持續時間分布
   - Gauge: 當前活躍請求數

2. **.NET Runtime 指標**
   - GC collection counts
   - Memory usage
   - ThreadPool statistics
   - Assembly load counts

### Swagger/OpenAPI 端點
- **路由**: `/swagger`
- **功能**: API 文件和測試介面
- **配置**: 強制啟用，不受環境影響

## 4. OpenTelemetry 配置

### Resource 配置
```csharp
var resourceBuilder = ResourceBuilder.CreateDefault().AddService(serviceName);
```

自動偵測的資源屬性：
- `service.name`: "Sample.Api"
- `service.version`: Assembly version
- `host.name`: Container hostname
- `process.pid`: Process ID

### Tracing 配置
```csharp
tracing.SetResourceBuilder(resourceBuilder)
    .AddAspNetCoreInstrumentation()
    .AddHttpClientInstrumentation()
    .AddSource("Sample.Api.OrderService") // 自定義 ActivitySource
    .AddOtlpExporter();
```

#### 自動追蹤的元件
- **ASP.NET Core**: HTTP requests/responses
- **HttpClient**: 外部 HTTP 呼叫
- **Custom ActivitySource**: 業務邏輯追蹤

### Metrics 配置
```csharp
metrics.SetResourceBuilder(resourceBuilder)
    .AddAspNetCoreInstrumentation()
    .AddHttpClientInstrumentation()
    .AddRuntimeInstrumentation()
    .AddPrometheusExporter()
    .AddOtlpExporter();
```

#### 自動收集的指標
- **HTTP 指標**: Request rate, duration, status codes
- **Runtime 指標**: GC, memory, threading
- **Custom 指標**: 可透過 `Meter` API 新增

### Logging 配置
```csharp
logging.AddOpenTelemetry(options =>
{
    options.SetResourceBuilder(resourceBuilder);
    options.IncludeScopes = true;
    options.AddOtlpExporter();
});
```

#### 日誌增強功能
- **Structured logging**: JSON 格式
- **Trace correlation**: TraceId/SpanId 自動附加
- **Scope inclusion**: 記錄執行範圍
- **OTLP export**: 發送至 OpenTelemetry Collector

## 5. 最佳實踐與建議

### Span 命名慣例
- **HTTP spans**: `{METHOD} {route}`
- **Database spans**: `{operation} {table}`
- **External calls**: `{service} {operation}`
- **Business logic**: `{BusinessProcess}`

### Tag 設定準則
```csharp
// 良好的 tag 範例
activity?.SetTag("order.id", orderId);
activity?.SetTag("customer.id", customerId);
activity?.SetTag("product.id", productId);
activity?.SetTag("operation.type", "create");

// 避免高基數 tags
activity?.SetTag("user.email", email); // ❌ 高基數
activity?.SetTag("timestamp", DateTime.Now); // ❌ 唯一值
```

### 錯誤狀態設定
```csharp
// 業務邏輯錯誤
if (!inventoryResult.Available)
{
    activity?.SetStatus(ActivityStatusCode.Error, "Insufficient inventory");
}

// 系統異常
catch (Exception ex)
{
    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
}
```

### 日誌關聯
```csharp
// 結構化日誌包含業務識別符
_logger.LogInformation("Order creation started for OrderId: {OrderId}, CustomerId: {CustomerId}", 
    request.OrderId, request.CustomerId);

// 錯誤日誌包含完整上下文
_logger.LogError(ex, "Payment failed for CustomerId: {CustomerId}, Amount: {Amount}", 
    customerId, amount);
```

### 效能考量
- **Sampling**: 生產環境建議使用取樣策略
- **Batch export**: 使用批次匯出減少網路負載
- **Resource limits**: 設定適當的記憶體和 CPU 限制
- **Async operations**: 避免阻塞主執行緒

### 測試策略
```bash
# 使用 test_dashboard.sh 產生測試流量
./test_dashboard.sh

# 驗證儀表化正確性
./verify_dashboard.sh
```

此配置提供了完整的 API 觀測性覆蓋，從簡單的 HTTP 請求到複雜的分散式業務流程都能有效追蹤和監控。 