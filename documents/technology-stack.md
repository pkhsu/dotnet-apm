# Technology Stack

本文件詳細列出 .NET Sample.Api 觀測性專案中使用的所有技術堆疊、版本資訊、設定和依賴關係。

## 應用程式層

### .NET Core Platform
- **名稱**: .NET
- **版本**: 8.0
- **目標框架**: `net8.0`
- **執行環境**: ASP.NET Core Web API
- **功能特性**:
  - Nullable reference types enabled
  - Implicit usings enabled
  - Hot reload support
  - Native AOT compatibility

### ASP.NET Core Framework
- **版本**: 8.0.8 (包含在 .NET 8)
- **核心套件**:
  - `Microsoft.AspNetCore.OpenApi` v8.0.8
  - `Swashbuckle.AspNetCore` v6.4.0 (Swagger/OpenAPI)
- **功能**:
  - REST API hosting
  - Swagger UI integration
  - HTTP pipeline middleware
  - Dependency injection container

## 觀測性函式庫

### OpenTelemetry .NET SDK
**版本**: 1.12.0 (統一版本，解決相容性問題)

#### 核心套件
- **OpenTelemetry** v1.12.0
  - 基礎 OpenTelemetry SDK
  - Metrics, Traces, Logs APIs
  - Resource detection and configuration

- **OpenTelemetry.Extensions.Hosting** v1.12.0
  - ASP.NET Core integration
  - Dependency injection integration
  - Application lifecycle management

#### Instrumentation 套件
- **OpenTelemetry.Instrumentation.AspNetCore** v1.12.0
  - 自動 HTTP 請求追蹤
  - ASP.NET Core pipeline instrumentation
  - Request/response metrics and spans

- **OpenTelemetry.Instrumentation.Http** v1.12.0
  - HTTP 客戶端自動追蹤
  - HttpClient instrumentation
  - Outbound HTTP calls monitoring

- **OpenTelemetry.Instrumentation.Runtime** v1.12.0
  - .NET Runtime metrics
  - GC, ThreadPool, Assembly metrics
  - Process-level performance counters

#### Exporters
- **OpenTelemetry.Exporter.OpenTelemetryProtocol** v1.12.0
  - OTLP over gRPC/HTTP export
  - Compatible with OpenTelemetry Collector
  - Metrics, logs, traces transmission

- **OpenTelemetry.Exporter.Prometheus.AspNetCore** v1.12.0-beta.1
  - Prometheus metrics endpoint
  - `/metrics` HTTP endpoint exposure
  - OpenMetrics format support

## 基礎設施容器

### Container Runtime
- **Docker Engine**: Compatible with Docker Compose v3.8
- **Docker Compose**: v3.8 syntax
- **網路**: bridge network (預設)
- **儲存**: named volumes for persistence

### OpenTelemetry Collector
- **Image**: `otel/opentelemetry-collector-contrib:0.90.1`
- **Distribution**: Contrib (包含所有 receivers/processors/exporters)
- **功能**:
  - OTLP receiver (gRPC/HTTP)
  - Batch processor
  - Multiple exporters (Prometheus, Loki, Tempo)
- **Ports**:
  - 4317: OTLP gRPC
  - 4318: OTLP HTTP  
  - 8889: Prometheus metrics endpoint

### Prometheus
- **Image**: `prom/prometheus:v2.47.1`
- **發佈日期**: 2023年10月
- **功能特性**:
  - Time series database
  - PromQL query language
  - HTTP API for queries
  - Remote write capability
- **Storage**: Local file system
- **Port**: 9090
- **配置**:
  - Global scrape interval: 15s
  - Sample.Api scrape interval: 10s

### Grafana
- **Image**: `grafana/grafana:10.1.5`
- **發佈日期**: 2023年9月
- **功能特性**:
  - Multi-datasource dashboards
  - Alerting and notification
  - User authentication
  - Plugin ecosystem
- **Port**: 3000
- **預設登入**: admin/admin
- **Data Sources**:
  - Prometheus (metrics)
  - Loki (logs)
  - Tempo (traces)

### Loki
- **Image**: `grafana/loki:2.9.2`
- **發佈日期**: 2023年9月
- **功能特性**:
  - Log aggregation system
  - LogQL query language
  - Label-based indexing
  - Prometheus-like approach for logs
- **Port**: 3100
- **Storage**: Local file system
- **API**: `/loki/api/v1/push` for log ingestion

### Tempo
- **Image**: `grafana/tempo:2.3.1`
- **發佈日期**: 2023年10月
- **功能特性**:
  - Distributed tracing backend
  - TraceQL query language
  - Metrics generation from traces
  - Cost-effective trace storage
- **Ports**:
  - 3200: HTTP API
  - 9095: gRPC API
  - 4317: OTLP receiver
- **Storage**: Local file system
- **Retention**: 1 hour (development)
- **Metrics Generator**: Service graphs + span metrics

## 開發工具與輔助腳本

### 測試腳本
- **test_dashboard.sh**
  - Bash shell script
  - Generates API traffic for testing
  - Creates diverse HTTP status codes
  - Uses `curl` for HTTP requests

- **verify_dashboard.sh**
  - Dashboard validation script
  - Verifies all metrics endpoints
  - Tests Prometheus queries
  - Validates data sources connectivity

### 容器化工具
- **Dockerfile** (Multi-stage build)
  - Base image: `mcr.microsoft.com/dotnet/aspnet:8.0`
  - Build image: `mcr.microsoft.com/dotnet/sdk:8.0`
  - Runtime optimizations
  - Non-root user execution

## 協議與標準

### OpenTelemetry Protocol (OTLP)
- **版本**: v1.0.0
- **Transport**: gRPC (primary), HTTP (fallback)
- **Encoding**: Protocol Buffers
- **支援資料類型**: Metrics, Logs, Traces

### Prometheus Protocol
- **Format**: OpenMetrics / Prometheus text format
- **Content-Type**: `text/plain; version=0.0.4`
- **Metrics Types**: Counter, Gauge, Histogram, Summary

### LogQL (Loki Query Language)
- **基於**: Prometheus query syntax
- **功能**: Log stream filtering and aggregation
- **標籤匹配**: `{job="value"}` syntax

### TraceQL (Tempo Query Language)
- **功能**: Trace search and filtering
- **語法**: SQL-like trace queries
- **範例**: `{.service_name="Sample.Api"}`

## 網路架構

### 端口配置
```
Host Ports → Container Ports
8088 → sample-api:8080    (API)
8081 → sample-api:8081    (Metrics)
3000 → grafana:3000       (Dashboard)
9090 → prometheus:9090    (Time series DB)
3100 → loki:3100         (Log aggregation)
3200 → tempo:3200        (Trace storage)
4317 → otel-collector:4317 (OTLP gRPC)
4318 → otel-collector:4318 (OTLP HTTP)
8889 → otel-collector:8889 (Metrics export)
```

### Service Dependencies
```
sample-api
├── depends_on: 없음 (standalone)
└── connects_to: otel-collector:4317

otel-collector  
├── depends_on: [loki, prometheus, tempo]
└── connects_to: 
    ├── loki:3100
    ├── prometheus:9090 (remote write)
    └── tempo:4317

grafana
├── depends_on: [loki, prometheus, tempo]
└── connects_to:
    ├── prometheus:9090 (data source)
    ├── loki:3100 (data source)
    └── tempo:3200 (data source)
```

## 效能與資源需求

### 系統需求
- **CPU**: 2+ cores (recommended)
- **Memory**: 4GB+ RAM
- **Storage**: 10GB+ available space
- **Network**: 1Gbps+ for optimal performance

### Container Resource Limits
預設使用 Docker 預設資源限制，適合開發環境使用。

### 資料保留策略
- **Prometheus**: 15天 (預設)
- **Loki**: 無限制 (受磁碟容量限制)
- **Tempo**: 1小時 (開發設定)
- **Grafana**: Dashboard configurations persisted

## 版本相容性

### .NET Version Compatibility
- **Target**: .NET 8.0
- **Minimum**: .NET 8.0 required
- **LTS Support**: Until November 2026

### OpenTelemetry Compatibility Matrix
| Component | Version | Compatibility |
|-----------|---------|---------------|
| .NET SDK | 8.0 | ✅ Full support |
| OpenTelemetry | 1.12.0 | ✅ Stable release |
| OTel Collector | 0.90.1 | ✅ Compatible |
| Prometheus | 2.47.1 | ✅ Remote write support |

### Container Image Updates
所有容器映像檔都使用特定版本標籤，確保建置的一致性和可重現性。

## 安全性配置

### Development Environment
- **認證**: 停用 (開發環境)
- **HTTPS**: 停用 (簡化設定)
- **CORS**: 開放設定
- **API Keys**: 不需要

### Production Considerations
生產環境部署時建議：
- 啟用 HTTPS/TLS
- 設定適當的認證機制
- 限制 CORS 政策
- 使用 secrets management
- 設定適當的資料保留政策

此技術堆疊提供了完整的 .NET 應用程式觀測性解決方案，涵蓋從資料收集到視覺化的完整流程。 