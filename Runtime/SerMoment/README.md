# SerMoment

[← RiseOn.Serializables](../../README.md)

Mốc thời gian lưu dạng số giây Unix (UTC). Unity lưu được, Newtonsoft.Json cũng ghi và
đọc được ([Lưu JSON](#lưu-json)).

```csharp
[SerializeField] private SerMoment lastClaim;

var now = SerMoment.NowLocal();                        // theo đồng hồ máy
if (SerMoment.DeltaMinutes(now, lastClaim) >= 30) { /* ... */ }
lastClaim = now.Add(60);                               // cộng thêm 60 giây

var trusted = await SerMoment.NowNetwork();            // giờ mạng, chống chỉnh đồng hồ máy
```

| Thành viên | Ý nghĩa |
|---|---|
| `Value` | Số giây Unix |
| `NowLocal()` | Thời điểm hiện tại theo đồng hồ máy (UTC) |
| `NowNetwork(timeoutSeconds = 2)` | Thời điểm hiện tại lấy từ mạng |
| `Add(seconds)` | Bản sao cộng thêm số giây |
| `DeltaSeconds(a, b)`, `DeltaMinutes(a, b)` | Khoảng cách giữa hai mốc, luôn dương |
| `ToString()` | Ngày giờ dễ đọc |

## Giờ mạng

`NowNetwork` hỏi lần lượt các máy chủ NTP (`time.google.com`, `time.cloudflare.com`,
`pool.ntp.org`, `time.windows.com`), không được thì đọc header `Date` của vài địa
chỉ HTTPS, lặp lại tới khi hết thời gian chờ. Hết giờ mà chưa có kết quả thì ném
`TimeoutException` kèm lỗi của từng lần thử, nên nhớ bắt lỗi khi máy không có mạng.

## Trong Inspector

Field đang bằng 0 được tự điền thời điểm hiện tại khi mở trong Inspector.

## Lưu JSON

Struct gắn `[DataContract]`, field `value` gắn `[DataMember(Name = nameof(value))]`. Hai
attribute này có sẵn trong .NET và Newtonsoft.Json đọc được, nên pack không phải phụ
thuộc Newtonsoft. JSON ra dạng `{"value": 1758000000}`, giống `JsonUtility` của Unity
(vốn đọc `[SerializeField]`).

- Có `[DataContract]` thì Newtonsoft chỉ lưu member gắn `[DataMember]`. Thêm field mới
  mà quên gắn thì field đó không vào JSON, và không có lỗi nào báo.
- Key lấy theo tên field. Đổi tên field thì JSON đã lưu không đọc lại được giá trị
  cũ; muốn đổi thì giữ `Name` là chuỗi tên cũ.
