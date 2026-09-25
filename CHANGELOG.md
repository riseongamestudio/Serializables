# Lịch sử thay đổi

Mọi thay đổi đáng kể của `com.riseon.serializables` được ghi ở đây. Định dạng theo
[Keep a Changelog](https://keepachangelog.com/vi/1.1.0/), đánh số theo
[Semantic Versioning](https://semver.org/lang/vi/).

## [1.0.2] - 2026-09-25

### Sửa

- Pack tự dùng Odin nên có assembly `RiseOn.Serializables.Requirements` của riêng nó: thiếu Odin Inspector,
  hoặc thiếu define `ODIN_INSPECTOR` ở nền tảng đang chọn, thì báo một lỗi nói rõ pack này cần Odin.

## [1.0.1] - 2026-09-25

### Sửa

- Các assembly dùng Odin có thêm `defineConstraints: ODIN_INSPECTOR`: thiếu Odin thì chúng
  được bỏ qua, và lỗi giải thích nằm ở `RiseOn.Utils.Requirements`.
- Phụ thuộc tối thiểu `com.riseon.utils` 1.0.1.

## [1.0.0] - 2026-09-23

### Thêm

- `EnumMap<TKey, TValue>`: map theo enum, luôn đủ key, lưu được. Mỗi entry lưu cả số lẫn
  tên của key, nên giá trị vẫn đúng khi enum chèn, xóa hay đổi tên member.
  `[FlattenSingleKey]` vẽ map của enum chỉ có một member thành một ô giá trị.
- `SerObject<T>`, `ListSerObject<T>`: tham chiếu `UnityEngine.Object` qua interface. Nút chọn
  mở cửa sổ tìm kiếm của `com.riseon.utils`, tô sẵn giá trị đang gán.
- `SerRef<T>`, `ListSerRef<T>`: `[SerializeReference]` có ô chọn type cho từng phần tử.
- `SerMoment`: thời điểm lưu bằng mili giây Unix, dùng như `DateTime` ở UTC+0: cộng
  trừ, so sánh, định dạng, đọc từ chuỗi, đổi qua lại với `DateTime` và `DateTimeOffset`.
  Drawer gập mở: dòng tiêu đề theo UTC+0 bôi đen copy được, mở ra là slider cho giờ,
  phút, giây, mili giây, ngày, tháng, năm; chuột phải có Now và Reset.
  Giờ mạng nằm ở `NetworkTime` của `com.riseon.utils.network`.
- `SerMoment`, `SerRef<T>`, `SerObject<T>` gắn `[DataContract]`: Newtonsoft.Json ghi và đọc được mà
  pack không phụ thuộc Newtonsoft.
- Converter JSON cho `SerRef<T>` (bỏ tầng `value`), `SerMoment` (số mili giây trơn) và
  `EnumMap<TKey, TValue>` (mảng entry, đọc được sau khi enum đổi), tự bật khi project có
  `com.unity.nuget.newtonsoft-json`.
- Drawer Odin cho các kiểu trên.
