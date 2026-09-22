# SerMoment

[← RiseOn.Serializables](../../README.md)

Một thời điểm, lưu dạng số mili giây Unix. Dùng như `DateTime` ở giờ UTC+0: cộng
trừ, so sánh, định dạng, đọc từ chuỗi. Unity lưu được, Newtonsoft.Json cũng ghi và
đọc được ([Lưu JSON](#lưu-json)).

```csharp
[SerializeField] private SerMoment lastClaim;

var now = SerMoment.Now;                                    // theo đồng hồ máy
if (now - lastClaim >= TimeSpan.FromMinutes(30)) { /* ... */ }
lastClaim = now;

var tomorrow = now.Date.AddDays(1);                         // 00:00 UTC+0 ngày mai
DateTime vietnam = now.ToDateTime(TimeSpan.FromHours(7));   // đồng hồ ở UTC+7

SerMoment trusted = await NetworkTime.NowAsync();           // giờ mạng, cần com.riseon.utils.network
```

## Mục lục

- [Luôn là UTC+0](#luôn-là-utc0)
- [API](#api)
- [Đọc từ chuỗi](#đọc-từ-chuỗi)
- [Trong Inspector](#trong-inspector)
- [Lưu JSON](#lưu-json)

## Luôn là UTC+0

SerMoment chỉ lưu một thời điểm, không lưu múi giờ. Các thành phần (`Year`,
`Hour`...), `ToString` và phép cộng theo ngày, tháng đều tính ở UTC+0. Cần giờ nơi
khác thì tự truyền độ lệch: `ToDateTime(offset)`, `ToDateTimeOffset(offset)`.

Khi tạo từ `DateTime`: `Kind` là `Local` thì được đổi sang UTC, `Unspecified` thì
coi như đã là UTC. Phần dưới mili giây bị bỏ.

## API

| Thành viên | Ý nghĩa |
|---|---|
| `Now` | Thời điểm hiện tại theo đồng hồ máy |
| `new SerMoment(unixMs)`, `new SerMoment(DateTime)`, `new SerMoment(DateTimeOffset)`, `FromUnixSeconds(seconds)` | Tạo |
| `UnixEpoch`, `MinValue`, `MaxValue` | 1970-01-01, năm 1, năm 9999. `default` chính là `UnixEpoch` |
| `UnixMs`, `UnixSeconds` | Số mili giây, số giây Unix |
| `Utc` | `DateTime` có `Kind` là `Utc` |
| `ToDateTime(offset)`, `ToDateTimeOffset(offset)` | Giờ ở nơi lệch `offset` so với UTC+0 |
| `Year`, `Month`, `Day`, `Hour`, `Minute`, `Second`, `Millisecond`, `DayOfWeek`, `DayOfYear`, `TimeOfDay`, `Date` | Thành phần ở UTC+0. `Date` là 00:00 cùng ngày |
| `Add(TimeSpan)`, `AddMilliseconds`, `AddSeconds`, `AddMinutes`, `AddHours`, `AddDays`, `AddMonths`, `AddYears`, `Subtract` | Cộng trừ, trả về giá trị mới |
| `+`, `-`, `==`, `!=`, `<`, `>`, `<=`, `>=` | Như `DateTime`. `a - b` ra `TimeSpan` |
| `ToString()`, `ToString(format)`, `ToString(format, provider)` | Mặc định ISO 8601 `2026-09-22T13:45:10.123Z`; `format` như của `DateTimeOffset` |
| `Parse(text)`, `TryParse(text, out moment)` | Đọc từ chuỗi, xem [bên dưới](#đọc-từ-chuỗi) |

- `DateTime` và `DateTimeOffset` gán thẳng vào `SerMoment` được. Chiều ngược lại
  phải ép kiểu: ra `DateTime` có `Kind` là `Utc`, hoặc `DateTimeOffset` lệch +00:00.
- Ra ngoài khoảng năm 1 tới năm 9999 thì ném `ArgumentOutOfRangeException`, như
  `DateTime`.
- `ToString(format)` không kèm `provider` thì dùng culture bất biến, nên
  `"dd/MM/yyyy"` luôn ra `22/09/2026` dù máy đặt ngôn ngữ nào.

## Đọc từ chuỗi

`Parse` và `TryParse` nhận:

- Số nguyên: mili giây Unix, như `1790084710123`.
- ISO 8601: `2026-09-22`, `2026-09-22T13:45`, `2026-09-22T13:45:10.123Z`,
  `2026-09-22 20:45:10+07:00`, tới 7 chữ số lẻ của giây.
- RFC 1123: `Tue, 22 Sep 2026 13:45:10 GMT`.
- Ngày trước tháng: `22/09/2026`, có thể thêm `13:45`, `13:45:10` hoặc
  `13:45:10.123`. Chuỗi có dấu `/` luôn đọc ngày trước, nên `05/09/2026` là ngày 5
  tháng 9.

Chuỗi không ghi độ lệch được coi là UTC. Dạng khác, như `1.5` hay `Sep 22 2026`,
bị từ chối chứ không bị đoán thành một ngày nào đó.

## Trong Inspector

Field hiện một dòng như `13:45:10 22/09/2026`, tính theo UTC+0. Chữ đó bôi đen và
copy được.

Mở ra có bảy dòng theo đúng thứ tự trên: giờ, phút, giây, mili giây, ngày, tháng,
năm, mỗi dòng một slider đúng khoảng của nó. Ngày theo số ngày của tháng đang chọn,
năm từ 1 tới 9999 (giới hạn của `DateTime`); kéo năm thì thô nhưng ô số cạnh slider
vẫn gõ chính xác được. Số vượt giới hạn tự kẹp lại: ngày 31 của tháng có 30 ngày
thành ngày 30.

Chuột phải vào field có *Now* và *Reset*, cộng *Copy* với *Paste* sẵn có của Odin để
chuyển giá trị giữa các field SerMoment.

Vết override của prefab chạy như field thường: chữ đậm, vạch xanh trùm cả phần mở
rộng, và *Revert* trong menu chuột phải.

## Lưu JSON

Project có package `com.unity.nuget.newtonsoft-json` thì SerMoment có converter
riêng: JSON là số mili giây trơn `1790084710123`. Khi đọc, converter nhận thêm chuỗi
ngày (mọi dạng `Parse` đọc được) và dạng `{"unixMs": N}`.

Không có converter thì pack dựa vào `[DataContract]` trên struct và
`[DataMember(Name = nameof(unixMs))]` trên field `unixMs`. Hai attribute này có sẵn
trong .NET và Newtonsoft.Json đọc được, nên pack không phải phụ thuộc Newtonsoft.
JSON ra dạng `{"unixMs": 1790084710123}`, giống `JsonUtility` của Unity (vốn đọc
`[SerializeField]`). Dạng này không đọc được số trơn.

- Có `[DataContract]` thì Newtonsoft chỉ lưu member gắn `[DataMember]`. Thêm field mới
  mà quên gắn thì field đó không vào JSON, và không có lỗi nào báo.
- Key lấy theo tên field. Đổi tên field thì JSON đã lưu không đọc lại được giá trị
  cũ; muốn đổi thì giữ `Name` là chuỗi tên cũ.
