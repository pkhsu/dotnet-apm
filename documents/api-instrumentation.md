# API Observability Implementation Guide

本文件說明 Sample.Api 中為了完整收集 **Traces**、**Metrics** 和 **Logs** 三種觀測資料所做的工作和設計決策。

## 為什麼需要三種觀測資料？

在現代分散式系統中，單一類型的監控資料無法提供完整的系統健康度視圖：

- **Traces**: 回答「請求在系統中的完整流程是什麼？」
- **Metrics**: 回答「系統的整體健康狀態如何？」  
- **Logs**: 回答「具體發生了什麼事件？」

這三種資料相互補強，形成完整的 observability 基礎。

## 整體設計策略

### 採用 Code-based Instrumentation
我們選擇 **Code-based** 而非 Zero-code 方式，原因：
- 更精確控制要收集的資料
- 可以加入業務邏輯相關的標籤和資訊
- 便於在開發階段除錯和驗證
- 符合團隊對代碼可控性的要求

---

## Traces 收集策略

### 目標：完整追蹤請求的生命週期

#### Level 1: 框架自動收集 (已完成)
**工作內容：**
- 在 `OtelExtensions.cs` 中啟用 ASP.NET Core instrumentation
- 在 `OtelExtensions.cs` 中啟用 HttpClient instrumentation

**獲得的資料：**
- 所有 HTTP 請求的 span（方法、URL、狀態碼、耗時）
- 所有外部 HTTP 呼叫的 span（目標服務、回應時間）

**開發者工作量：** 一次性配置，後續零維護

#### Level 2: 業務流程手動追蹤 (已實作於 OrderController)
**工作內容：**
- 建立 `ActivitySource` 實例
- 在重要業務方法中建立 custom spans
- 為 spans 添加業務相關的標籤

**實作範例：**
```csharp
// 一次性建立 ActivitySource
private static readonly ActivitySource ActivitySource = new("Sample.Api.OrderService");

// 在業務方法中手動建立 spans
using var activity = ActivitySource.StartActivity("CreateOrder");
activity?.SetTag("order.id", request.OrderId);
```

**獲得的資料：**
- 訂單建立流程的完整 trace tree
- 每個子步驟的耗時和狀態
- 業務識別符的追蹤（訂單ID、客戶ID等）

**開發者工作量：** 每個重要業務流程需要額外 5-10 行代碼

#### Level 3: 錯誤狀態追蹤 (已實作)
**工作內容：**
- 在錯誤發生時設定 span 狀態
- 記錄錯誤原因和上下文

**開發者工作量：** 每個錯誤處理點需要額外 1-2 行代碼

---

## Metrics 收集策略

### 目標：監控系統健康度和效能指標

#### Level 1: 系統指標自動收集 (已完成)
**工作內容：**
- 啟用 ASP.NET Core metrics instrumentation
- 啟用 .NET Runtime metrics instrumentation  
- 配置 Prometheus exporter

**獲得的資料：**
- HTTP 請求速率、延遲分布、錯誤率
- .NET 記憶體使用、GC 頻率、執行緒池狀態
- 透過 `/metrics` 端點暴露給 Prometheus

**開發者工作量：** 一次性配置，零維護

#### Level 2: 業務指標收集 (待實作)
**未來可擴展的工作：**
- 建立 `Meter` 實例收集業務指標
- 例如：訂單建立成功率、支付失敗次數、庫存檢查平均耗時

**預期開發者工作量：** 每個業務指標需要 3-5 行代碼

---

## Logs 收集策略

### 目標：提供詳細的事件記錄和除錯資訊

#### Level 1: 結構化日誌基礎 (已完成)
**工作內容：**
- 整合 OpenTelemetry logging provider
- 配置自動 trace correlation
- 設定 OTLP exporter

**獲得的資料：**
- 所有日誌自動包含 TraceId 和 SpanId
- 結構化的 JSON 格式日誌
- 與 traces 完全關聯的日誌事件

**開發者工作量：** 一次性配置，後續使用標準 `ILogger`

#### Level 2: 業務事件記錄 (已實作於 OrderController)
**工作內容：**
- 在關鍵業務節點記錄結構化日誌
- 包含業務識別符和狀態資訊

**實作範例：**
```csharp
_logger.LogInformation("Order creation started for OrderId: {OrderId}, CustomerId: {CustomerId}", 
    request.OrderId, request.CustomerId);
```

**獲得的資料：**
- 業務流程的詳細步驟記錄
- 便於除錯的上下文資訊
- 與對應 trace 完全關聯

**開發者工作量：** 每個重要業務事件需要 1 行日誌代碼

---

## 整合與關聯

### 自動關聯機制 (已實作)
**實現方式：**
- 統一的 `ResourceBuilder` 配置
- OpenTelemetry 自動的 context propagation
- 所有三種資料共享相同的 service metadata

**效果：**
- 任何一個 HTTP 請求都可以看到完整的 trace、相關的 metrics 變化、對應的 logs
- 支援分散式系統的端到端追蹤
- 異常發生時可以從任何一種資料快速定位到完整上下文

### 資料導出統一化 (已實作)
**配置：**
- 所有資料都透過 OTLP 協議導出
- Prometheus metrics 額外提供 pull-based 接取
- 統一的 collector 端點配置

---

## 開發者指引

### 新增 API 端點時的工作檢查清單

#### ✅ 基礎要求（零額外工作）
- HTTP traces 和 metrics 自動收集
- 日誌自動包含 trace correlation

#### 📝 業務邏輯複雜時的額外工作
- [ ] 建立 custom ActivitySource（如果還沒有）
- [ ] 在主要業務方法中建立 spans
- [ ] 為 spans 添加業務相關標籤
- [ ] 在關鍵步驟記錄結構化日誌
- [ ] 在錯誤處理中設定 span 狀態

#### 🎯 工作量估算
- **簡單 CRUD API**: 0 額外工作
- **複雜業務流程**: 每個流程約 10-15 行額外代碼
- **新的業務領域**: 需要建立新的 ActivitySource

### 命名和標籤慣例

#### Span 命名
- 業務操作：`CreateOrder`, `ProcessPayment`
- 外部呼叫：`CheckInventory`, `SendNotification`

#### Tag 設定
- 業務識別符：`order.id`, `customer.id`, `product.id`
- 操作類型：`operation.type`
- 避免高基數資料：不要用時間戳或唯一值作為 tag

#### 日誌格式
- 使用結構化參數：`{OrderId}`, `{CustomerId}`
- 包含足夠的上下文資訊便於除錯
- 錯誤日誌包含完整的 exception 資訊

---

## 效益與維護

### 目前獲得的能力
- **端到端可視性**: 從 HTTP 請求到業務邏輯的完整追蹤
- **效能監控**: 實時的 API 回應時間和系統資源使用
- **問題定位**: 任何異常都可以快速定位到完整上下文
- **業務洞察**: 透過 traces 和 logs 了解業務流程執行狀況

### 維護工作量
- **日常維護**: 幾乎零維護，OpenTelemetry SDK 自動處理大部分工作
- **新功能開發**: 按照既定慣例，每個複雜業務流程需要少量額外代碼
- **故障排除**: 透過統一的 observability stack 大幅減少除錯時間

這個實作平衡了觀測能力和開發工作量，為團隊提供了強大的 observability 基礎，同時保持了代碼的簡潔性和可維護性。 