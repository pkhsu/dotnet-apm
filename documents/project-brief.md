# .NET 8 可觀測性 (Observability) PoC

## 背景
在現代化的分散式系統中，有效地收集應用程式的遙測資料 (日誌、指標、追蹤) 對於監控系統健康狀況、診斷問題和優化性能至關重要。本專案旨在為 .NET 8 應用程式建立一個基於開源工具的可觀測性解決方案，以應對在容器化環境 (Docker/Kubernetes) 中缺乏統一應用效能監控 (APM) 機制的挑戰。

## 機會
透過採用 OpenTelemetry、Prometheus 和 Loki 等主流開源工具，我們可以建立一個標準化、靈活且不受特定供應商限制的可觀測性技術棧。此 PoC 的成功將為團隊未來所有的 .NET 專案提供一個可複用的監控架構藍圖，從而提高開發效率、系統可靠性與維運水平。

## 假設與成功標準
我們假設，透過在 .NET 8 應用中整合 OpenTelemetry SDK，便能夠順利收集日誌、指標與分散式追蹤，並將其傳送到 Loki 和 Prometheus，最終在 Grafana 中實現統一的視覺化監控。

**成功標準:**
- 一個範例 .NET 8 Web API 應用程式成功整合 OpenTelemetry。
- 應用程式的日誌、指標與追蹤資料能被 OpenTelemetry Collector 接收。
- 日誌資料可在 Grafana Loki 中查詢與檢視。
- 指標資料能被 Prometheus 收集，並在 Grafana 儀表板上呈現。
- 分散式追蹤的 Span 之間能夠正確關聯，並在 Grafana 中呈現。
- 整套系統可透過 `docker-compose` 在本地環境一鍵啟動與運行。

## 範疇
**必須完成 (In-Scope):**
- 建立一個簡單的 .NET 8 Web API 專案。
- 整合 OpenTelemetry SDK 以收集日誌、指標與追蹤。
- 建立一個包含以下服務的 `docker-compose.yml`：
  - .NET 應用程式
  - OpenTelemetry Collector
  - Grafana
  - Loki
  - Prometheus
- 設定 OpenTelemetry Collector 接收來自應用的遙測資料，並將其分別導出到 Loki 與 Prometheus。
- 在 Grafana 中建立一個基礎儀表板，用以展示來自 Loki 的日誌與來自 Prometheus 的指標。

**暫不處理 (Out-of-Scope):**
- 完整的 Kubernetes 部署腳本 (例如 Helm Charts)。
- 在 Prometheus/Grafana 中設定進階的告警規則。
- 針對可觀測性技術棧本身的效能調校。
- 生產環境下的遙測資料採樣 (sampling) 策略。

## 待解決問題
1.  如何最有效地在日誌、指標與追蹤之間建立關聯 (例如，將 Trace ID 自動注入日誌中)？
2.  對於分散式追蹤，除了在日誌中關聯 Trace ID，是否有必要引入專門的追蹤後端 (如 Grafana Tempo 或 Jaeger)？
3.  此架構應如何設計以支援未來擴展至更多的微服務？ 