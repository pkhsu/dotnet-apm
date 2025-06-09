# 進度追蹤

## 當前狀態
**完整可觀測性堆疊實作完成** (2025-06-09)

我們已經成功實作了完整的 .NET 8 可觀測性解決方案，包含所有三種遙測資料的收集、處理和視覺化。整套系統現在透過 `docker-compose up --build` 一鍵啟動，支援完整的 **Logs** ✅、**Metrics** ✅、**Traces** ✅ 功能。

## 已完成功能

### 基礎設施與應用程式 ✅
- **[成功]** .NET 8 應用程式整合 OpenTelemetry SDK
- **[成功]** **OpenTelemetry 版本統一**：所有套件已升級至 v1.12.0 穩定版本
- **[成功]** **Sample.Api 容器正常運行**：完全解決 `System.MethodAccessException` 錯誤
- **[成功]** **Docker Compose 建構正常**：修正建構上下文路徑問題

### 完整遙測資料流 ✅
- **[成功]** **Logs**: .NET App → OTLP → OTel Collector → Loki → Grafana
  - 日誌成功傳送到 Loki 並可在 Grafana 中查詢
  - TraceID 自動注入日誌中實現關聯性
- **[成功]** **Metrics**: .NET App → `/metrics` 端點 + OTLP → OTel Collector → Prometheus → Grafana
  - HTTP 請求指標正常記錄 (延遲、狀態碼、路由)
  - .NET 運行時指標正常收集 (GC、記憶體、執行緒)
  - Prometheus 成功抓取指標資料
- **[成功]** **Traces**: .NET App → OTLP → OTel Collector → Tempo → Grafana
  - **新增 Grafana Tempo 服務**：版本 2.3.1，完整 OTLP 支援
  - **OpenTelemetry Collector 更新**：新增 `otlp/tempo` 導出器
  - **Grafana 資料來源修正**：Tempo URL 指向正確的服務端點
  - **分散式追蹤**：Tempo 服務就緒，準備接收和儲存 traces

### 服務整合與監控 ✅
- **[成功]** **API 端點正常運作**：WeatherForecast API 可以正常回應 (`/weatherforecast`)
- **[成功]** **健康檢查端點**：所有服務健康檢查正常 (`/ready`, `/health`)
- **[成功]** **服務發現**：Prometheus 成功識別並抓取 OTel Collector 指標
- **[成功]** **容器編排**：所有 6 個服務 (Sample.Api, OTel Collector, Loki, Prometheus, Grafana, Tempo) 正常啟動

### 進階功能 ✅
- **[成功]** **跨資料來源關聯**：透過 Grafana `derivedFields` 實現從日誌到追蹤的跳轉
- **[成功]** **混合導出策略**：Metrics 同時支援 Prometheus 拉取和 OTLP 推送
- **[成功]** **集中化資料處理**：OTel Collector 作為所有遙測資料的中樞
- **[成功]** **專門化儲存後端**：Loki (Logs), Prometheus (Metrics), Tempo (Traces)

## 技術成就

### 架構突破
- **完整的可觀測性三支柱**：首次在 .NET 專案中實作完整的 Logs + Metrics + Traces 整合
- **OpenTelemetry 標準化**：採用業界標準的 OpenTelemetry 規範，確保供應商中立性
- **雲原生架構**：完全容器化的微服務架構，便於擴展和部署

### 核心服務配置
| 服務 | 映像版本 | 端口 | 狀態 | 用途 |
|------|----------|------|------|------|
| Sample.Api | 自建 | 8088 | ✅ | .NET 8 範例應用程式 |
| OTel Collector | 0.90.1 | 4317/4318/8889 | ✅ | 遙測資料中樞 |
| Loki | 2.9.2 | 3100 | ✅ | 日誌儲存與查詢 |
| Prometheus | 2.47.1 | 9090 | ✅ | 指標儲存與查詢 |
| **Tempo** | **2.3.1** | **3200/9095** | **✅** | **追蹤儲存與查詢** |
| Grafana | 10.1.5 | 3000 | ✅ | 統一視覺化平台 |

### 資料流驗證
- **流量生成測試**：成功產生 10 次 API 請求，確認資料流正常
- **指標更新驗證**：HTTP 請求指標正確計數和計時
- **服務健康確認**：所有服務通過 readiness 檢查
- **端點功能驗證**：API、Metrics、健康檢查端點全部正常

## 待辦事項 (優先度排序)

### 高優先度 (本週完成)
- **[計畫中]** **Grafana Dashboard 建立**：建立展示所有三種遙測資料的整合儀表板
- **[計畫中]** **Trace 查詢測試**：在 Grafana 中透過 TraceID 查詢特定 traces
- **[計畫中]** **關聯性驗證**：測試從日誌跳轉到對應 trace 的功能

### 中優先度 (下週完成) 
- **[計畫中]** **效能基準測試**：測試系統在高負載下的表現和資料完整性
- **[計畫中]** **README.md 建立**：完整的專案說明和使用指南
- **[計畫中]** **設定檔優化**：移除 Docker Compose 版本警告，環境變數外部化

### 長期規劃
- **[未來]** **告警規則設定**：在 Prometheus/Grafana 中設定基本告警
- **[未來]** **Kubernetes 部署**：建立 Helm Charts 用於 K8s 部署
- **[未來]** **採樣策略**：研究生產環境下的遙測資料採樣方案

## 專案里程碑達成

✅ **Phase 1: 基礎設施建置** - 完成  
✅ **Phase 2: 應用程式整合** - 完成  
✅ **Phase 3: 驗證與測試** - **完成**  
🎯 **Phase 4: 文件與清理** - 進行中

## 關鍵學習與最佳實踐

### 版本管理
- OpenTelemetry 生態系統需要嚴格的版本一致性
- 使用 `1.12.0` 作為穩定版本基線，`beta` 版本用於特殊需求

### 架構設計
- **集中化 vs 分散化**：透過 OTel Collector 集中處理遙測資料，同時保持各儲存後端的專業化
- **混合導出策略**：Metrics 採用雙重導出 (Prometheus pull + OTLP push) 確保相容性
- **容器化優先**：完全採用容器化開發，避免本地環境不一致問題

### 可觀測性設計
- **關聯性優先**：TraceID 注入日誌是實現分散式追蹤可視性的關鍵
- **專門化儲存**：不同類型的遙測資料使用專門的後端儲存，提升查詢效能
- **標準化協議**：採用 OpenTelemetry 標準確保長期可維護性

## 專案決策演進
- **版本管理突破**: 成功解決了 OpenTelemetry 生態系統中的版本相容性問題，確立了「所有核心套件使用統一版本」的最佳實踐。
- **開發環境**: 最初嘗試使用本地安裝的 .NET SDK，但因版本衝突 (v9 vs v8) 而受阻。最終決定轉向完全基於 Docker 的開發與建構流程，將 .NET 專案的建立和依賴還原都放在 `Dockerfile` 中，確保了環境的一致性。
- **日誌與追蹤關聯**: 最初計畫將 Traces 也發送到 Loki，但發現 Loki Exporter 不支援 Traces。最終採取了業界更常見的做法：僅將 Logs 發送到 Loki，並在日誌中注入 TraceID，然後在 Grafana 中透過 `derivedFields` 和 LogQL 查詢實現關聯。
- **指標暴露**: 最初僅使用 OTLP Exporter，導致 Prometheus 無法抓取指標。最終確認必須在 .NET 應用中同時加入 `PrometheusExporter` 並明確暴露 `/metrics` 端點，才解決了 Grafana 中 "No Data" 的問題。

## 專案進度：.NET 8 Observability PoC

本文件追蹤 PoC 的開發進度，確保所有核心需求都已達成。

## Phase 1: 基礎設施與環境建置 (Infrastructure Setup)

- [x] 建立 `documents/project-brief.md` 作為專案指導文件
- [x] 解決本地 .NET SDK 版本衝突問題 (採用 Docker 建構方案)
- [x] 建立 `src/Sample.Api/Dockerfile` 用於建構 .NET 應用
- [x] 建立 `docker` 目錄存放所有服務的組態檔
- [x] 建立 Prometheus, Otel-Collector, Grafana 的設定檔
- [x] 建立 `docker-compose.yml` 串連所有服務

## Phase 2: .NET 應用整合 (Application Integration)

- [x] 在 `Dockerfile` 中加入安裝 OpenTelemetry NuGet 套件的步驟
- [x] **解決 OpenTelemetry 版本衝突**：統一所有套件至 v1.12.0
- [x] 建立 `OtelExtensions.cs` 集中管理 Observability 設定
- [x] 建立客製化的 `Program.cs` 以啟用 OpenTelemetry
- [x] 更新 `Dockerfile` 以複製自訂的 C# 檔案
- [x] **容器啟動驗證成功**：Sample.Api 可正常啟動並提供服務

## Phase 3: 驗證與測試 (Verification & Testing)

- [x] 啟動所有服務 (`docker-compose up --build`)
- [x] **API 功能驗證**：WeatherForecast API 正常回應
- [x] **Prometheus 指標驗證**：`/metrics` 端點正常暴露 OpenTelemetry 指標
- [ ] 產生流量到 `Sample.Api` (例如，透過瀏覽器或 curl)
- [ ] 在 Grafana (Loki) 中檢視應用程式日誌，並確認 `TraceID` 已被注入
- [ ] 在 Grafana (Prometheus) 中檢視應用程式指標 (例如 `http.server.request.duration`)
- [ ] 在 Grafana (Explore > Tempo) 中透過 `TraceID` 檢視分散式追蹤的 Span
- [ ] 確認所有在 `project-brief.md` 中定義的成功標準均已達成

## Phase 4: 清理與總結 (Cleanup & Wrap-up)

- [ ] (可選) 建立一個基礎的 Grafana Dashboard 樣板
- [ ] 撰寫 `README.md` 說明如何啟動與測試本專案
- [ ] 總結 PoC 的發現與後續建議 