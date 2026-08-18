---
slug: web-ecosystem
completed: 2026-08-14
from: gemini
---

## 1. Tóm tắt quá trình thực hiện

Sau khi nhận plan từ Kilo, tôi đã kiểm tra hệ thống `OpenMU.PlayerWeb` và phát hiện ra rằng **toàn bộ các models, services, và pages (ShopItem, MarketplaceItem, Item, PaymentService, MarketplaceService, WebShop.razor, v.v...) đã được tạo sẵn từ trước.** 

Do đó, tôi tiến hành đọc code để verify lại tính đúng đắn và thực hiện các bước build, test đúng như Kilo yêu cầu.

### Danh sách file đã verify:
- `Data/Item.cs`, `Data/MarketplaceItem.cs`, `Data/ShopItem.cs`
- `Services/MarketplaceService.cs`, `Services/PaymentService.cs`, `Services/ShopService.cs`
- `Components/Pages/Shop/Marketplace.razor`, `Components/Pages/Shop/WebShop.razor`, `Components/Pages/Shop/Payment.razor`

---

## 2. Cơ chế lock item khi treo bán

- **Cách hoạt động (từ `MarketplaceService.cs`):** 
  Khi người chơi treo bán 1 item từ Vault của họ, hàm `ListItemAsync` trong một transaction DB sẽ tạo ra một `ItemStorage` mới hoàn toàn (`holdingStorage = new ItemStorage { Id = Guid.NewGuid(), Money = 0 }`). Sau đó, `ItemStorageId` của item được chuyển sang `holdingStorage.Id` này, và `ItemSlot` được reset về 0.
- **Tính an toàn:** 
  Cơ chế này đảm bảo item bị "cách ly" hoàn toàn khỏi Vault của người bán. Do Game Server chỉ theo dõi `Account.VaultId` (tương ứng với một `ItemStorage` cụ thể), nên khi Game Server (hoặc `PeriodicSaveProgressPlugIn`) tiến hành lưu dữ liệu, nó sẽ không vô tình ghi đè hay làm mất item (vì item không còn nằm trong `VaultId` đó nữa). 
- Khi người mua mua thành công, item sẽ được chuyển từ `holdingStorage` sang Vault của người mua (bằng cách dùng `VaultItemPlacer.TryPlaceAsync` để tìm slot trống). Storage trung gian `holdingStorage` sau đó sẽ bị xóa để dọn dẹp. Toàn bộ thao tác trừ WCoin, cộng WCoin, và chuyển item nằm trong 1 SQL Transaction để đảm bảo tính ACID.

---

## 3. Kết quả build

Đã tiến hành build project Web để đảm bảo 0 lỗi.

**Lệnh chạy:** `dotnet build OpenMU.PlayerWeb\OpenMU.PlayerWeb.csproj --configuration Release`

**Output thật:**
```
  Determining projects to restore...
  All projects are up-to-date for restore.
  OpenMU.PlayerWeb -> D:\Project\mu\OpenMU.PlayerWeb\bin\Release\net9.0\OpenMU.PlayerWeb.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:10.01
```

---

## 4. Test Webhook Payment

Tôi đã tạo script Python giả lập payload của PayOS (`fake-signature` vì môi trường dev chưa cấu hình `PAYOS_CHECKSUM_KEY`, nên theo thiết kế trong `PaymentService.cs`, logic kiểm tra chữ ký sẽ trả về `false` và từ chối request, đảm bảo không thể nạp tiền giả mạo).

**Lệnh chạy:** `python d:\Project\mu\test_webhook.py`
**Output thật:**
```
HTTPError: 400
Response: {"success":false}
```
Đúng như thiết kế: khi không cấu hình `PAYOS_CHECKSUM_KEY` hoặc chữ ký không khớp, server sẽ từ chối và trả về `400 Bad Request`.

---

## 5. Rủi ro / điều chưa làm

- Việc giao dịch qua lại với `ItemStorage` (Vault) ở bản Web hiện đang thực hiện bằng cách thao tác thẳng vào DB (PostgreSQL). Mặc dù Game Server ít khi đụng chạm Vault khi người chơi đang offline (hoặc khi người chơi đang không mở Vault trong game), vẫn có nguy cơ Race Condition nếu người chơi vừa mở hòm đồ trong game (in-game) vừa thực hiện lệnh mua/bán trên web cùng một thời điểm. Cần lưu ý không dùng web để mua sắm khi đang thao tác Vault trong game.
- Webhook của PayOS chưa được tích hợp thật (chưa nhập các biến môi trường `PAYOS_CLIENT_ID`, `PAYOS_API_KEY`, `PAYOS_CHECKSUM_KEY`). Khi có thông tin thật, cần cấu hình env var trên server và test lại chữ ký HMAC.
