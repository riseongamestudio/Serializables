# Lịch sử thay đổi

Mọi thay đổi đáng kể của `com.riseon.serializables` được ghi ở đây. Định dạng theo
[Keep a Changelog](https://keepachangelog.com/vi/1.1.0/), đánh số theo
[Semantic Versioning](https://semver.org/lang/vi/).

## [Unreleased]

## [1.0.6] - 2026-11-26

### Thêm

- `EnumMap` theo `[NoRowShade]` của `com.riseon.utils`: field gắn nó thì các dòng không tô
  sọc xen kẽ.

### Sửa

- Ô search trên thanh của `EnumMap` nằm giữa thanh theo chiều dọc; trước đó nó dính
  mép trên vì kiểu ô search thấp hơn một dòng.

### Đổi

- Phụ thuộc RiseOn lên bản phát hành cùng đợt: `com.riseon.utils` 2.0.0.

## [1.0.5] - 2026-11-20

### Đổi

- Cần Unity 6000.3, bản đang dùng để phát triển: `package.json` ghi `unity`, nên Package
  Manager không cài pack vào project Unity cũ hơn.
- Phụ thuộc RiseOn lên bản phát hành cùng đợt: `com.riseon.utils` 1.0.18.

## [1.0.4] - 2026-11-20

### Thêm

- Kéo object từ Project hay Hierarchy vào `ListSerObject`: thả ở tay nắm, ở ×, ở khe giữa hai
  dòng hay dưới dòng cuối thì chèn vào đúng chỗ đó, các dòng dạt ra mở một ô cho nó; thả lên ô
  object của một dòng thì ô đó nhận; thả lên thanh tiêu đề thì thêm vào cuối, cả mục sáng lên.
- + của `ListSerObject` mở cửa sổ tìm kiếm của ô `SerObject` và thêm object được chọn vào cuối;
  chọn None thì thêm một dòng trống.

### Đổi

- `EnumMap` là một mục gập mở như mục của trang settings RiseOn: tên trên thanh, ô tìm trên
  thanh khi mục mở, các key là dòng sọc của `SectionRows`. Gắn `HideLabel` thì chỉ vẽ các dòng,
  không thanh và khung: nơi chứa map đã đặt tên và khung cho nó. Nút cập nhật key thành
  **Update keys**, nằm trong hộp cảnh báo.
- `ListSerRef`, `ListSerObject` vẽ thành mục `[SectionList]`: + trên thanh, × ở dòng, kéo tay
  nắm đổi chỗ. Cần `com.riseon.utils` 1.0.15.
- `EnumMap` bỏ Alt+Shift+click riêng của nó: Alt+click foldout của map mở hay gập mọi thứ
  bên dưới nhờ `RecursiveFoldoutDrawer` của `com.riseon.utils`, như mọi foldout khác.

### Bỏ

- `[FlattenSingleKey]`: map của enum một member vẽ như mọi map khác. Xóa attribute khỏi field
  đang gắn nó.
- Drawer nhận object kéo vào mảng `SerObject<T>[]` và `List<SerObject<T>>`: hai kiểu đó là list
  của Odin như mọi list khác. Muốn kéo object vào thì đổi field sang `ListSerObject<T>`.

## [1.0.3] - 2026-09-28

### Đổi

- Symbol bật converter JSON đổi từ `HAS_NEWTONSOFT` thành `SERIALIZABLES_SUPPORT_NEWTONSOFT`,
  mang tiền tố tên pack để không trùng với symbol project tự đặt. Project dùng package
  Newtonsoft không phải sửa gì. Project dùng DLL Newtonsoft và đã tự thêm `HAS_NEWTONSOFT` vào
  *Scripting Define Symbols* thì đổi sang tên mới, nếu không các converter JSON sẽ không được
  biên dịch.

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