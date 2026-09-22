# Lịch sử thay đổi

Mọi thay đổi đáng kể của `com.riseon.serializables` được ghi ở đây. Định dạng theo
[Keep a Changelog](https://keepachangelog.com/vi/1.1.0/), đánh số theo
[Semantic Versioning](https://semver.org/lang/vi/).

## [1.0.0] - Chưa phát hành

### Thêm

- `EnumMap<TKey, TValue>`: map theo enum, luôn đủ key, lưu được. Mỗi entry lưu cả số lẫn
  tên của key, nên giá trị vẫn đúng khi enum chèn, xóa hay đổi tên member.
- `SerObject<T>`, `ListSerObject<T>`: tham chiếu `UnityEngine.Object` qua interface.
- `SerRef<T>`, `ListSerRef<T>`: `[SerializeReference]` có ô chọn type cho từng phần tử.
- `SerMoment`: mốc thời gian Unix, lấy giờ máy hoặc giờ mạng.
- `SerMoment`, `SerRef<T>`, `SerObject<T>` gắn `[DataContract]`: Newtonsoft.Json ghi và đọc được mà
  pack không phụ thuộc Newtonsoft.
- Converter JSON cho `SerRef<T>` (bỏ tầng `value`), `SerMoment` (số trơn) và
  `EnumMap<TKey, TValue>` (mảng entry, đọc được sau khi enum đổi), tự bật khi project có
  `com.unity.nuget.newtonsoft-json`.
- Drawer Odin cho các kiểu trên.
