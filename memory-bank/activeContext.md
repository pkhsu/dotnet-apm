# Active Context: .NET Observability PoC

本文件記錄專案的當前狀態、近期決策與下一步計畫。

## 當前狀態 (Current Status)
- **進度**: **完整的 Grafana Tempo 整合已完成**。專案已完成所有三種遙測資料 (Logs, Metrics, Traces) 的完整實作。所有服務 (`.NET App`, `Otel-Collector`, `Loki`, `Prometheus`, `Grafana`, `Tempo`) 已成功透過 `docker-compose` 啟動並執行中。
- **所在階段**: `progress.md` -> **Phase 3: 驗證與測試 (Verification & Testing)** 完成。
- **最新完成**: 成功實作完整的 Grafana Tempo 追蹤後端，現在系統支援完整的可觀測性三支柱：Logs ✅、Metrics ✅、Traces ✅。

## 近期決策與學習 (Recent Decisions & Learnings)

### 最新實作：Grafana Tempo 完整整合 (2025-06-09)
1. **新增 Tempo 服務**: 在 `docker-compose.yml` 中添加了 `grafana/tempo:2.3.1` 服務
2. **建立 Tempo 配置**: 建立了 `docker/tempo/tempo.yaml` 配置檔案，支援 OTLP 接收器
3. **OpenTelemetry Collector 更新**:
   - 新增 `otlp/tempo` 導出器，指向 `http://tempo:4317`
   - 更新 traces pipeline: `exporters: [logging, otlp/tempo]`
4. **Grafana 資料來源修正**:
   - 修正 Tempo 資料來源 URL 從 `http://loki:3100` 到 `http://tempo:3200`
   - 保持原有的日誌關聯配置 (`tracesToLogs`, `derivedFields`)
5. **服務依賴更新**: 更新了 `otel-collector` 和 `grafana` 的依賴關係包含 `tempo`

### 技術成就突破 (2025-06-09)
- **完整的遙測堆疊**: 成功實作了業界標準的可觀測性三支柱
- **資料流完整性**: 
  - **Logs**: .NET App → OTLP → OTel Collector → Loki → Grafana ✅
  - **Metrics**: .NET App → Prometheus Exporter → `/metrics` + OTLP → OTel Collector → Prometheus → Grafana ✅  
  - **Traces**: .NET App → OTLP → OTel Collector → Tempo → Grafana ✅
- **關聯性實作**: TraceID 自動注入日誌，並在 Grafana 中透過 `derivedFields` 實現跨資料來源關聯

### 架構模式確立
- **混合導出策略**: 對於 Metrics，同時使用直接暴露 (`/metrics`) 和 OTLP 推送兩種方式，確保最大相容性
- **集中化 Collector**: OpenTelemetry Collector 作為所有遙測資料的中樞，統一處理和路由
- **專門化後端**: 每種遙測資料使用專門的儲存後端 (Loki for Logs, Prometheus for Metrics, Tempo for Traces)

## 已建立的模式與偏好
- **開發環境**: 優先使用完全容器化的開發流程 (`docker-compose up --build`)，以避免本機環境差異導致的問題。
- **版本管理**: 對於 OpenTelemetry 套件，始終保持所有相關套件的版本一致性。
- **服務配置**: 所有服務的設定檔都掛載到容器中，便於修改和版本控制。
- **完整性驗證**: 
  1. API 功能測試 (`curl /weatherforecast`)
  2. 指標端點驗證 (`curl /metrics`)  
  3. 服務健康檢查 (`curl /ready`, `curl /health`)
  4. 流量生成測試 (多次 API 呼叫)
  5. 後端服務日誌檢查 (`docker logs`)

## 下一步工作 (Next Steps)
1. **Grafana Dashboard 建立**: 建立整合的儀表板展示所有三種遙測資料
2. **追蹤查詢測試**: 在 Grafana 中透過 TraceID 查詢特定的 traces
3. **效能測試**: 測試系統在高負載下的表現
4. **文件完善**: 更新 README.md 和使用指南

## 技術債務與改善點
- **Docker Compose 版本警告**: 移除過時的 `version` 欄位
- **設定檔優化**: 考慮將更多設定參數外部化為環境變數
- **監控告警**: 未來可考慮添加基本的告警規則

# 當前情境與核心經驗

本文件記錄了專案開發過程中的關鍵情境、學習到的經驗和已建立的偏好設定。這是未來進行開發與除錯時最重要的參考。

## 核心洞察與學習
PoC 的開發過程並非一帆風順，尤其在設定 Grafana 儀表板時遇到了最多的挑戰。這些經驗對於未來所有基於此架構的專案都極具價值。

### OpenTelemetry 版本管理：版本衝突解決案例
我們在 Sample.Api 容器啟動時遇到 `System.MethodAccessException` 錯誤，這個問題展現了 OpenTelemetry 生態系統中版本管理的複雜性：

1. **問題根源**: 不同 OpenTelemetry 套件版本間的內部 API 相容性問題
   - 錯誤發生在 `Microsoft.Extensions.DependencyInjection.OpenTelemetryServicesExtensions.AddOpenTelemetry()` 嘗試訪問 `OpenTelemetry.Internal.Guard.ThrowIfNull()` 時
   - 這表明套件間的內部依賴版本不一致

2. **解決策略**:
   - **版本統一原則**: 所有 OpenTelemetry 核心套件必須使用相同的主版本號
   - **穩定版本優先**: 優先選擇最新的穩定版本 (1.12.0)
   - **相容性驗證**: 對於 beta 版本的套件，確保其與核心版本相容

3. **經驗總結**:
   - 在更新 OpenTelemetry 套件時，必須同時更新所有相關套件
   - 使用 `dotnet list package --outdated` 檢查套件版本狀態
   - 容器化建置環境有助於快速驗證版本相容性

### Grafana 儀表板設定：一個完整的除錯案例
我們反覆遇到的儀表板設定問題，最終的解決方案基於以下幾個關鍵點：

1.  **指標資料來源 (Prometheus)**：
    *   **問題**: 面板顯示 "No Data"。
    *   **根本原因**:
        1.  **指標名稱錯誤**: OpenTelemetry .NET SDK `v1.7.x` 使用的指標名稱是 `http_server_request_duration` (不含 `_seconds`)，而我們最初在 PromQL 中使用了過時的名稱。
        2.  **.NET 應用未暴露 `/metrics` 端點**: 我們的 .NET 應用程式中缺少了兩項關鍵設定：
            *   在 `OtelExtensions.cs` 的 `WithMetrics` 中，必須加入 `.AddPrometheusExporter()`。
            *   在 `Program.cs` 的應用程式管道中，必須呼叫 `app.MapPrometheusScrapingEndpoint()`。
    *   **經驗**: 當遇到 "No Data" 時，應優先檢查 Prometheus UI (`/targets`) 確認目標是否正常抓取，然後直接 `curl` 應用的 `/metrics` 端點，確認指標是否按預期格式暴露。

2.  **日誌資料來源 (Loki)**：
    *   **問題**: 面板顯示 "Datasource not found"、"parse error" 或無法正確解析欄位。
    *   **根本原因**:
        1.  **資料來源變數**: 在透過檔案自動部署 (Provisioning) 的儀表板 JSON 中，**不應**使用 `${DS_LOKI}` 這類變數，必須直接寫死資料來源的 `uid` 或 `name` (例如 `"Loki"`)。
        2.  **日誌格式解析**: Otel Collector 送往 Loki 的日誌是 **JSON** 格式，因此在 LogQL 中**必須**使用 `| json` 解析器，而不是 `regexp`。
        3.  **JSON 語法錯誤**: 在手動修改儀表板 JSON 時，極易因遺漏物件間的逗號 `,` 或對查詢字串中的引號 `"` 進行了錯誤的轉義 (`\\"`) 而導致整個儀表板載入失敗。Grafana 的日誌 (`docker-compose logs grafana`) 是找出這類語法錯誤的最佳途徑。
    *   **經驗**: 對於自動部署的儀表板，應始終保持其 JSON 格式的簡潔與正確性。優先確保 `| json` 能成功執行，再逐步添加 `label_format` 或 `line_format` 等美化功能。

### NuGet 套件版本管理
- **問題**: `dotnet restore` 反覆失敗，提示找不到穩定的套件版本。
- **根本原因**: OpenTelemetry 的某些關鍵套件 (如 `OpenTelemetry.Exporter.Prometheus.AspNetCore`) 在我們使用的版本線中可能不是穩定版。
- **新發現**: 最新的 OpenTelemetry 1.12.0 版本已經大幅改善了套件相容性，大部分套件都有穩定版本可用。
- **經驗**: 
  - 仔細閱讀 `dotnet restore` 的錯誤日誌，它通常會提示最接近的 `beta` 或 `rc` 版本
  - 定期檢查 OpenTelemetry 官方發佈說明，了解最新的穩定版本
  - 在 PoC 階段，可以大膽使用這些預發行版本來解決依賴問題

## 已建立的模式與偏好
- **開發環境**: 優先使用完全容器化的開發流程 (`docker-compose up --build`)，以避免本機環境差異導致的問題。
- **版本管理**: 對於 OpenTelemetry 套件，始終保持所有相關套件的版本一致性。
- **設定檔管理**: 所有服務的設定檔 (Prometheus, Grafana, Otel-Collector) 都應掛載到容器中，以便於修改和版本控制。
- **除錯流程**:
  1. 從頂層（Grafana UI）觀察問題。
  2. 檢查對應服務的容器日誌 (`docker-compose logs <service_name>`)。
  3. 檢查資料來源的 UI（例如 Prometheus `/targets`）。
  4. 檢查資料的原始端點（例如 .NET 應用的 `/metrics`）。
  5. 追溯至應用程式的程式碼和專案設定檔。 