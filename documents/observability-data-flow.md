# Observability Data Flow Analysis

本文件詳細描述 .NET Sample.Api 專案中 Metrics、Logs、Traces 三大觀測性資料的完整流向，包含所有節點、協議、端口配置和使用的函式庫。

## 系統架構概覽

```
┌─────────────┐    ┌──────────────────┐    ┌─────────────┐    ┌──────────────┐
│ Sample.Api  │───▶│ OTel Collector   │───▶│ Backends    │───▶│ Grafana      │
│ (.NET 8)    │    │ (Aggregation)    │    │ (Storage)   │    │ (Visualization)│
└─────────────┘    └──────────────────┘    └─────────────┘    └──────────────┘
```

## 1. Metrics 資料流

### 1.1 資料產生源頭
**Sample.Api (.NET 8.0 Application)**
- **函式庫**: OpenTelemetry.NET SDK 1.12.0
- **Instrumentation**:
  - `OpenTelemetry.Instrumentation.AspNetCore` - HTTP請求指標
  - `OpenTelemetry.Instrumentation.Http` - HTTP客戶端指標
  - `OpenTelemetry.Instrumentation.Runtime` - .NET Runtime指標
- **輸出端點**: 
  - Prometheus格式: `http://sample-api:8080/metrics`
  - OTLP格式: `http://otel-collector:4317` (gRPC)

### 1.2 資料聚合層
**OpenTelemetry Collector (otel/opentelemetry-collector-contrib:0.90.1)**
- **接收器 (Receivers)**:
  ```yaml
  otlp:
    protocols:
      grpc: 0.0.0.0:4317
      http: 0.0.0.0:4318
  ```
- **處理器 (Processors)**: `batch` - 批次處理優化傳輸
- **輸出器 (Exporters)**:
  - `prometheus`: 暴露在 `0.0.0.0:8889`
  - `logging`: Console輸出除錯

### 1.3 資料儲存層
**Prometheus (prom/prometheus:v2.47.1)**
- **抓取配置**:
  ```yaml
  scrape_configs:
    - job_name: 'sample-api'
      targets: ['sample-api:8080']
      metrics_path: '/metrics'
      scrape_interval: 10s
    - job_name: 'otel-collector'  
      targets: ['otel-collector:8889']
      scrape_interval: 15s
  ```
- **儲存**: 本地時序資料庫
- **查詢端口**: `:9090`

### 1.4 視覺化層
**Grafana (grafana/grafana:10.1.5)**
- **資料源**: Prometheus (`http://prometheus:9090`)
- **查詢語言**: PromQL
- **端口**: `:3000`

## 2. Logs 資料流

### 2.1 資料產生源頭
**Sample.Api Logging**
- **函式庫**: 
  - `Microsoft.Extensions.Logging` (ASP.NET Core內建)
  - `OpenTelemetry.Logs` 1.12.0
- **配置**:
  ```csharp
  logging.AddOpenTelemetry(options =>
  {
      options.SetResourceBuilder(resourceBuilder);
      options.IncludeScopes = true;
      options.AddOtlpExporter();
  });
  ```
- **輸出協議**: OTLP over gRPC to `otel-collector:4317`

### 2.2 資料聚合層
**OpenTelemetry Collector**
- **接收**: OTLP gRPC/HTTP protocols
- **處理**: batch processor
- **路由**: logs pipeline → Loki exporter

### 2.3 資料儲存層
**Loki (grafana/loki:2.9.2)**
- **接收端點**: `http://loki:3100/loki/api/v1/push`
- **儲存**: 本地檔案系統
- **標籤索引**: 自動從OpenTelemetry資源屬性提取
- **查詢端口**: `:3100`

### 2.4 視覺化層
**Grafana**
- **資料源**: Loki (`http://loki:3100`)
- **查詢語言**: LogQL
- **相關性**: 自動與Traces關聯 (TraceID)

## 3. Traces 資料流

### 3.1 資料產生源頭
**Sample.Api Distributed Tracing**
- **函式庫**:
  - `OpenTelemetry.Instrumentation.AspNetCore` - 自動HTTP追蹤
  - `OpenTelemetry.Instrumentation.Http` - 外部HTTP調用追蹤
  - 自定義 `ActivitySource`: "Sample.Api.OrderService"
- **手動Instrumentation範例**:
  ```csharp
  using var activity = ActivitySource.StartActivity("CreateOrder");
  activity?.SetTag("order.id", request.OrderId);
  activity?.SetTag("customer.id", request.CustomerId);
  ```
- **輸出協議**: OTLP over gRPC to `otel-collector:4317`

### 3.2 資料聚合層
**OpenTelemetry Collector**
- **接收**: OTLP gRPC (port 4317) / HTTP (port 4318)
- **處理**: batch processor
- **路由**: traces pipeline → Tempo exporter

### 3.3 資料儲存層
**Tempo (grafana/tempo:2.3.1)**
- **接收端點**: `http://tempo:4317` (OTLP gRPC)
- **儲存配置**:
  ```yaml
  storage:
    trace:
      backend: local
      local:
        path: /tmp/tempo/blocks
  ```
- **保留期**: 1小時 (`block_retention: 1h`)
- **查詢端口**: `:3200`

### 3.4 Metrics生成器
**Tempo內建Metrics Generator**
- **處理器**: `[service-graphs, span-metrics]`
- **輸出**: RED指標 (Rate, Errors, Duration) → Prometheus
- **Remote Write**: `http://prometheus:9090/api/v1/write`

### 3.5 視覺化層
**Grafana**
- **資料源**: Tempo (`http://tempo:3200`)
- **查詢**: TraceID、服務名稱、時間範圍
- **關聯性**: 與Logs和Metrics自動關聯

## 4. 網路拓撲與端口配置

### 4.1 Container間通訊
```
sample-api:8080     ────► otel-collector:4317 (OTLP gRPC)
                    ────► otel-collector:4318 (OTLP HTTP)

otel-collector:8889 ────► prometheus:9090 (Prometheus scraping)
otel-collector      ────► loki:3100 (Logs push)
otel-collector      ────► tempo:4317 (Traces push)

prometheus:9090     ────► grafana (data source)
loki:3100          ────► grafana (data source)  
tempo:3200         ────► grafana (data source)
```

### 4.2 外部存取端口
- **Sample.Api**: `localhost:8088` → `sample-api:8080`
- **Prometheus**: `localhost:9090` → `prometheus:9090`
- **Grafana**: `localhost:3000` → `grafana:3000`
- **Loki**: `localhost:3100` → `loki:3100`
- **Tempo**: `localhost:3200` → `tempo:3200`
- **OTel Collector**: 
  - `localhost:4317` → `otel-collector:4317` (OTLP gRPC)
  - `localhost:4318` → `otel-collector:4318` (OTLP HTTP)

## 5. 資料格式與協議

### 5.1 OpenTelemetry Protocol (OTLP)
- **版本**: v1.0.0
- **編碼**: Protocol Buffers over gRPC/HTTP
- **Content-Type**: `application/x-protobuf`

### 5.2 Prometheus格式
- **格式**: OpenMetrics/Prometheus text format
- **Content-Type**: `text/plain`
- **範例指標**:
  ```
  http_server_request_duration_seconds_count{job="sample-api"} 150
  http_server_request_duration_seconds_sum{job="sample-api"} 45.2
  ```

### 5.3 Loki推送格式
- **API**: `/loki/api/v1/push`
- **格式**: JSON streams
- **標籤**: 從OpenTelemetry Resource自動提取

### 5.4 Tempo追蹤格式
- **格式**: Jaeger/OpenTelemetry trace format
- **壓縮**: gzip
- **查詢**: HTTP API + TraceQL

## 6. 效能特性與配置

### 6.1 批次處理配置
```yaml
processors:
  batch:
    timeout: 1s
    send_batch_size: 1024
    send_batch_max_size: 2048
```

### 6.2 資料保留策略
- **Tempo**: 1小時 (開發環境)
- **Prometheus**: 預設15天
- **Loki**: 預設無限期 (實際受磁碟空間限制)

### 6.3 取樣策略
- **Traces**: 100% 取樣 (開發環境)
- **Metrics**: 全量收集
- **Logs**: 全量收集

此文件提供了完整的觀測性資料流分析，可作為系統維護和故障排除的參考指南。 