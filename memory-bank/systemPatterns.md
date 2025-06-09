# 系統模式與架構

本文件描述了此 PoC 專案的系統架構、關鍵設計模式和資料流程。

## 系統架構圖

```mermaid
flowchart TD
    subgraph "開發者環境 (docker-compose)"
        subgraph "dotnet-apm"
            A[Sample.Api (.NET 8)] -- OTLP --> B(Otel Collector)
            A -- /metrics scrape --> C[Prometheus]
        end

        subgraph "監控 & 儲存"
            B -- Logs (Loki Export) --> D[Loki]
            B -- Metrics (Prometheus Export) --> C
            B -- Traces (OTLP) --> E{Tempo/Jaeger (未來)}
            C -- Datasource --> F[Grafana]
            D -- Datasource --> F
        end

        User([開發者]) -- 瀏覽 --> F
    end

    style A fill:#5DADE2,stroke:#333,stroke-width:2px
    style B fill:#58D68D,stroke:#333,stroke-width:2px
    style C fill:#F5B041,stroke:#333,stroke-width:2px
    style D fill:#A569BD,stroke:#333,stroke-width:2px
    style F fill:#E59866,stroke:#333,stroke-width:2px

```

## 資料流詳解

1.  **.NET 應用程式 (`Sample.Api`)**:
    *   應用程式本身是遙測資料的**主要來源**。
    *   透過整合 `OpenTelemetry.Instrumentation.AspNetCore` 等套件，自動產生 Logs, Metrics, Traces。
    *   **Logs**: 透過 OTLP Exporter 將日誌以 OTLP 格式推送到 Otel Collector。
    *   **Metrics**:
        *   透過 **OTLP Exporter** 將指標推送到 Otel Collector（此路徑主要用於未來擴展或非 Prometheus 場景）。
        *   透過 **Prometheus Exporter** 在 `/metrics` 端點上以 Prometheus 格式暴露指標。這是目前 Grafana 指標面板的**主要資料來源**。
    *   **Traces**: 透過 OTLP Exporter 將追蹤以 OTLP 格式推送到 Otel Collector。

2.  **OpenTelemetry Collector (`otel-collector`)**:
    *   作為遙測資料的**中樞**，負責接收、處理和轉發資料。
    *   **接收器 (`receivers`)**: 設定 `otlp` 接收器，監聽來自 .NET 應用的遙測資料。
    *   **處理器 (`processors`)**: (在此 PoC 中未深入使用) 可用於資料過濾、豐富、採樣等。
    *   **導出器 (`exporters`)**:
        *   `loki`: 將接收到的日誌轉發到 Loki。
        *   `prometheus`: (在此 PoC 中未使用此導出器) 也可以將指標轉發到 Prometheus。
        *   `logging`: 用於將接收到的資料印出到 Collector 的控制台，是**除錯的關鍵工具**。

3.  **Prometheus**:
    *   作為指標的**時間序列資料庫**。
    *   主動地從 `Sample.Api` 的 `/metrics` 端點**拉取 (scrape)** 指標資料並儲存。

4.  **Loki**:
    *   作為日誌的**儲存與索引系統**。
    *   被動地接收來自 Otel Collector 推送的日誌資料。

5.  **Grafana**:
    *   作為統一的**視覺化與查詢前端**。
    *   設定了兩個主要資料來源：Loki 和 Prometheus。
    *   透過 PromQL 查詢 Prometheus 來繪製指標圖表。
    *   透過 LogQL 查詢 Loki 來顯示日誌。
    *   透過 `derivedFields` 功能，實現了從 Loki 日誌中的 `traceid` 到追蹤系統 (未來為 Tempo) 的連結。

## 關鍵設計模式
- **邊車模式 (Sidecar Pattern) 的簡化版**: 在 Kubernetes 環境中，Otel Collector 通常會作為一個 "邊車" 容器與應用程式部署在同一個 Pod 中。在我們的 Docker Compose 設計中，Otel Collector 雖然是獨立的服務，但扮演了同樣的角色：它從應用程式中抽離了遙測資料收集與轉發的邏輯，讓應用程式本身保持乾淨。
- **統一協定 (Unified Protocol)**: 整個系統的核心是 OTLP。應用程式只需要關注如何將資料發送到 Collector，而不需要知道後端是 Loki 還是 Prometheus，這使得未來替換或增加後端儲存變得非常容易。 