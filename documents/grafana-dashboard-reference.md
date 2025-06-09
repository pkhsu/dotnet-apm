# Grafana Dashboard Reference

本文件詳細說明 Sample.Api Observability Dashboard 中所有 widget 的數值定義、PromQL 查詢語句、顯示方式和使用目的。

## Dashboard 概覽

- **Dashboard名稱**: Sample.Api Observability Dashboard
- **資料源**: Prometheus, Loki, Tempo
- **刷新間隔**: 30秒自動刷新
- **時間範圍**: 預設最近1小時
- **面板數量**: 11個面板

## 1. Service Overview (服務概覽)

### 基本資訊
- **類型**: Stat Panel
- **位置**: 第一行左側 (Grid: 6x4)
- **資料源**: Prometheus

### 數值定義
顯示 Sample.Api 的**總請求數**累計值。

### PromQL 查詢
```promql
sum(http_server_request_duration_seconds_count{job="sample-api"})
```

### 查詢說明
- `http_server_request_duration_seconds_count`: ASP.NET Core HTTP請求計數器
- `job="sample-api"`: 過濾特定服務
- `sum()`: 聚合所有標籤維度

### 顯示配置
- **單位**: 請求/秒 (reqps)
- **顏色閾值**:
  - 綠色: 0-9 requests
  - 黃色: 10-49 requests  
  - 紅色: ≥50 requests
- **圖表模式**: 面積圖背景
- **數值模式**: 顯示最後非空值

### 使用方式
快速了解服務的整體請求流量，判斷系統負載狀況。

## 2. Error Rate (錯誤率)

### 基本資訊
- **類型**: Stat Panel
- **位置**: 第一行中間 (Grid: 6x4)
- **資料源**: Prometheus

### 數值定義
計算 4xx 和 5xx HTTP 狀態碼占總請求的百分比。

### PromQL 查詢
```promql
sum(http_server_request_duration_seconds_count{job="sample-api", http_response_status_code=~"[4-5].."}) / sum(http_server_request_duration_seconds_count{job="sample-api"}) * 100
```

### 查詢說明
- `http_response_status_code=~"[4-5].."`: 正規表達式匹配4xx和5xx狀態碼
- 分子: 錯誤請求總數
- 分母: 總請求數
- `* 100`: 轉換為百分比

### 顯示配置
- **單位**: 百分比 (%)
- **範圍**: 0-100%
- **顏色閾值**:
  - 綠色: 0-0.9% (正常)
  - 黃色: 1-4.9% (警告)
  - 紅色: ≥5% (危險)

### 使用方式
監控API健康狀況，錯誤率高於5%需要立即調查。

## 3. P99 Latency (99th百分位延遲)

### 基本資訊
- **類型**: Stat Panel
- **位置**: 第一行右側 (Grid: 6x4)
- **資料源**: Prometheus

### 數值定義
99%的請求響應時間都小於此數值，表示服務響應速度的最壞情況指標。

### PromQL 查詢
```promql
histogram_quantile(0.99, sum(http_server_request_duration_seconds_bucket{job="sample-api"}) by (le))
```

### 查詢說明
- `http_server_request_duration_seconds_bucket`: HTTP請求持續時間直方圖
- `histogram_quantile(0.99, ...)`: 計算99th百分位數
- `by (le)`: 按直方圖分桶標籤分組

### 顯示配置
- **單位**: 秒 (s)
- **顏色閾值**:
  - 綠色: <1秒 (優秀)
  - 黃色: 1-5秒 (一般)
  - 紅色: >5秒 (需優化)

### 使用方式
評估用戶體驗，P99延遲反映極端情況下的響應時間。

## 4. Active Traces (活躍追蹤)

### 基本資訊
- **類型**: Stat Panel
- **位置**: 第一行最右側 (Grid: 6x4)
- **資料源**: Prometheus

### 數值定義
顯示當前時段內的追蹤(trace)總數。

### PromQL 查詢
```promql
sum(tempo_ingester_traces_created_total)
```

### 查詢說明
- `tempo_ingester_traces_created_total`: Tempo創建的追蹤總數
- `sum()`: 聚合所有Tempo實例

### 顯示配置
- **單位**: 追蹤數
- **顏色**: 藍色主題
- **圖表模式**: 簡單計數顯示

### 使用方式
監控分散式追蹤系統的活動狀況，確保追蹤資料正常收集。

## 5. Request Rate by Endpoint (端點請求率)

### 基本資訊
- **類型**: Time Series (時序圖)
- **位置**: 第二行左半部 (Grid: 12x6)
- **資料源**: Prometheus

### 數值定義
顯示各API端點隨時間變化的請求速率趨勢。

### PromQL 查詢
```promql
sum(rate(http_server_request_duration_seconds_count{job="sample-api"}[5m])) by (http_route)
```

### 查詢說明
- `rate(...[5m])`: 計算5分鐘內的平均請求速率
- `by (http_route)`: 按HTTP路由分組
- 結果為 requests/second

### 顯示配置
- **Y軸**: 請求/秒
- **圖例**: 顯示各端點名稱
- **線條**: 不同顏色區分端點
- **時間範圍**: 跟隨dashboard時間設定

### 使用方式
- 識別熱門API端點
- 觀察流量模式和峰值
- 比較不同端點的使用頻率

## 6. Request Latency Percentiles (請求延遲百分位數)

### 基本資訊
- **類型**: Time Series
- **位置**: 第二行右半部 (Grid: 12x6)
- **資料源**: Prometheus

### 數值定義
顯示P99、P95、P50百分位數延遲的時間序列變化。

### PromQL 查詢
```promql
# P99
histogram_quantile(0.99, sum(rate(http_server_request_duration_seconds_bucket{job="sample-api"}[5m])) by (le))

# P95  
histogram_quantile(0.95, sum(rate(http_server_request_duration_seconds_bucket{job="sample-api"}[5m])) by (le))

# P50
histogram_quantile(0.50, sum(rate(http_server_request_duration_seconds_bucket{job="sample-api"}[5m])) by (le))
```

### 顯示配置
- **Y軸**: 秒 (s)
- **圖例**: P99 (紅), P95 (橙), P50 (綠)
- **填充**: 區域填充顯示分布
- **堆疊**: 非堆疊模式

### 使用方式
- 監控延遲分布變化
- 識別性能回歸
- 設定SLA基準線

## 7. Recent Traces (最近追蹤)

### 基本資訊
- **類型**: Table (表格)
- **位置**: 第三行左側 (Grid: 12x6)
- **資料源**: Prometheus

### 數值定義
顯示最近API請求的詳細資訊，包含HTTP方法、狀態碼、路由和請求頻率。

### PromQL 查詢
```promql
sum(http_server_request_duration_seconds_count{job="sample-api"}) by (http_request_method, http_response_status_code, http_route)
```

### 顯示配置
- **欄位**:
  - Method: HTTP方法 (GET, POST, PUT, DELETE)
  - Status: HTTP狀態碼 (顏色編碼)
  - Route: API路由路徑
  - Requests/sec: 請求頻率

### 狀態碼顏色配置
- **綠色**: 2xx (200, 201, 204) - 成功
- **黃色**: 3xx - 重定向
- **橙色**: 4xx (400, 401, 403, 404) - 客戶端錯誤
- **紅色**: 5xx (500, 502, 503) - 服務器錯誤

### 使用方式
- 快速識別錯誤請求
- 查看API使用模式
- 調試特定端點問題

## 8. Application Logs (應用程式日誌)

### 基本資訊
- **類型**: Logs Panel
- **位置**: 第三行右側 (Grid: 12x6)
- **資料源**: Loki

### 數值定義
顯示Sample.Api產生的結構化日誌，支援TraceID關聯。

### LogQL 查詢
```logql
{job="Sample.Api"} |= ""
```

### 顯示配置
- **時間欄位**: 自動偵測timestamp
- **欄位解析**: JSON自動解析
- **過濾**: 支援文字搜尋
- **高亮**: 錯誤和警告訊息

### 使用方式
- 調試應用程式問題
- 查看詳細執行資訊
- 透過TraceID關聯分散式追蹤

## 9. HTTP Status Codes (HTTP狀態碼分布)

### 基本資訊
- **類型**: Pie Chart (圓餅圖)
- **位置**: 第四行左側 (Grid: 6x6)
- **資料源**: Prometheus

### 數值定義
顯示所有HTTP狀態碼的分布比例。

### PromQL 查詢
```promql
sum(http_server_request_duration_seconds_count{job="sample-api"}) by (http_response_status_code)
```

### 顯示配置
- **圖例**: 狀態碼 + 請求數
- **顏色**: 按狀態碼類型區分
- **標籤**: 顯示百分比
- **排序**: 按數量降序

### 使用方式
- 一眼了解API健康狀況
- 識別異常狀態碼比例
- 監控服務穩定性

## 10. Request Duration by Method (按方法分組的請求持續時間)

### 基本資訊
- **類型**: Bar Gauge (條狀儀表)
- **位置**: 第四行右側 (Grid: 6x6)
- **資料源**: Prometheus

### 數值定義
顯示不同HTTP方法(GET, POST, PUT, DELETE)的平均響應時間。

### PromQL 查詢
```promql
sum(http_server_request_duration_seconds_sum{job="sample-api"}) by (http_request_method) / sum(http_server_request_duration_seconds_count{job="sample-api"}) by (http_request_method)
```

### 查詢說明
- 分子: 請求持續時間總和
- 分母: 請求總數
- 結果: 平均響應時間

### 顯示配置
- **方向**: 水平條狀圖
- **單位**: 秒 (s)
- **顏色**: 漸層顯示
- **最大值**: 自動調整

### 使用方式
- 比較不同HTTP方法的性能
- 識別慢查詢方法
- 優化API設計參考

## 11. Top Slowest Endpoints (最慢端點排名)

### 基本資訊
- **類型**: Table
- **位置**: 第五行 (Grid: 24x6)
- **資料源**: Prometheus

### 數值定義
按平均響應時間排序的API端點清單。

### PromQL 查詢
```promql
topk(10, sum(http_server_request_duration_seconds_sum{job="sample-api"}) by (http_route) / sum(http_server_request_duration_seconds_count{job="sample-api"}) by (http_route))
```

### 查詢說明
- `topk(10, ...)`: 取前10個最大值
- 計算各端點的平均響應時間
- 按響應時間降序排列

### 顯示配置
- **欄位**:
  - Endpoint: API路由
  - Avg Duration: 平均持續時間
  - Total Requests: 總請求數
- **排序**: 按平均時間降序
- **顏色**: 響應時間熱力圖

### 使用方式
- 快速識別性能瓶頸
- 優先優化最慢端點
- 監控優化效果

## Dashboard 使用最佳實踐

### 1. 監控流程
1. **概覽檢查**: 查看Service Overview和Error Rate
2. **性能分析**: 檢視P99 Latency和Latency Percentiles
3. **流量分析**: 觀察Request Rate by Endpoint
4. **問題定位**: 使用Recent Traces和Application Logs
5. **深入分析**: 檢查最慢端點進行優化

### 2. 告警設定建議
- **Error Rate > 5%**: 嚴重告警
- **P99 Latency > 5s**: 性能告警  
- **Request Rate異常增長**: 容量告警

### 3. 疑難排解
- **No Data問題**: 檢查Prometheus target狀態
- **查詢錯誤**: 驗證metric名稱和job標籤
- **資料延遲**: 確認scrape interval設定

此Dashboard提供了完整的API觀測性視圖，涵蓋性能、錯誤、流量等關鍵指標，是生產環境監控的最佳實踐參考。 