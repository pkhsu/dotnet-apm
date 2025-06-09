# 技術棧情境

本文件記錄了專案所使用的核心技術、套件版本以及開發環境的設定細節。

## 核心技術棧
- **語言/框架**: .NET 8 (LTS)
- **容器化**: Docker / Docker Compose
- **遙測標準**: OpenTelemetry

## 核心服務
| 服務 | 映像 (Image) | 用途 |
| :--- | :--- | :--- |
| `sample-api` | (本地建構) | 主要的 .NET 8 範例應用程式 |
| `otel-collector` | `otel/opentelemetry-collector-contrib` | 接收、處理並轉發遙測資料 |
| `prometheus` | `prom/prometheus` | 儲存與查詢指標 |
| `loki` | `grafana/loki` | 儲存與查詢日誌 |
| `grafana` | `grafana/grafana` | 視覺化儀表板 |

## .NET / OpenTelemetry 關鍵 NuGet 套件
以下是在 `Sample.Api.csproj` 中使用的核心套件及其版本。在除錯過程中，套件的具體版本（特別是預發行版）至關重要。

| 套件名稱 | 版本 | 用途 | 備註 |
| :--- | :--- | :--- | :--- |
| `OpenTelemetry.Extensions.Hosting` | `1.7.0` | 核心依賴，用於整合 OTel 與 .NET Host | |
| `OpenTelemetry.Exporter.OpenTelemetryProtocol` | `1.7.0` | OTLP 格式導出器 | 將資料發送到 Otel Collector |
| **`OpenTelemetry.Exporter.Prometheus.AspNetCore`** | **`1.8.0-beta.1`** | Prometheus 格式導出器 | **關鍵**：必須使用此預發行版才可成功還原依賴。 |
| `OpenTelemetry.Instrumentation.AspNetCore` | `1.7.1` | 自動攔截 ASP.NET Core 的請求 | 產生 Traces 和 Metrics |
| `OpenTelemetry.Instrumentation.Http` | `1.7.1` | 自動攔截 `HttpClient` 的請求 | |
| `OpenTelemetry.Instrumentation.Runtime` | `1.7.0` | 收集 .NET 執行階段的指標 | 例如 GC、JIT 等 |

## 開發環境設定
- **建構流程**: 專案的 `Dockerfile` 被設計為完全獨立。它使用 `mcr.microsoft.com/dotnet/sdk:8.0` 作為建構階段的基礎映像，並在容器內部執行 `dotnet restore` 和 `dotnet publish`。這避免了任何本地 .NET SDK 版本不匹配的問題。
- **配置管理**: 所有服務的設定檔 (`prometheus.yml`, `config.yml` for otel, `datasources.yml` for grafana) 都存放在 `docker/` 目錄下，並透過 `docker-compose.yml` 中的 `volumes` 掛載到對應的容器中，實現了設定的外部化與版本控制。 