# EnumMap

[← RiseOn.Serializables](../../README.md)

`EnumMap<TKey, TValue>`: mỗi giá trị của enum `TKey` một ô `TValue`. Đọc như
`IReadOnlyDictionary`, luôn đủ key, Unity lưu được, và giá trị vẫn đúng khi enum thay
đổi.

```csharp
public enum Sfx { Click, Win, Lose }

[SerializeField] private EnumMap<Sfx, AudioClip> clips;

var click = clips[Sfx.Click];
clips[Sfx.Win] = winClip;
foreach (var (sfx, clip) in clips) { /* ... */ }
```

| Thành viên | Ý nghĩa |
|---|---|
| `this[key]` | Đọc / ghi ô của key. Key không có trong enum thì ném `InvalidKeyException` |
| `Count`, `Keys`, `Values` | Số key, danh sách key, danh sách giá trị, theo thứ tự của enum |
| `ContainsKey(key)` | Luôn `true`: map luôn có đủ key |
| `TryGetValue(key, out value)` | `false` khi key không có trong enum |
| `ToDictionary()` | Sao ra `Dictionary<TKey, TValue>` |
| `new EnumMap<TKey, TValue>(pairs)` | Tạo từ danh sách cặp key / giá trị |

## Dữ liệu lưu

Mỗi key lưu thành một entry gồm số, tên và giá trị. Đây là dạng Unity 6.6 lưu
`Dictionary` (mảng cặp `key` / `value`), thêm trường `name`:

```yaml
clips:
  entries:
  - key: 0
    name: Click
    value: {fileID: ...}
  - key: 1
    name: Win
    value: {fileID: ...}
```

## Khi enum thay đổi

Unity lưu enum bằng số, nên chèn hay xóa một member ở giữa là số của các member sau nó
lệch đi. EnumMap giữ đúng giá trị nhờ tên lưu kèm mỗi entry. Trước lần đọc đầu tiên,
map khớp các entry với enum hiện tại:

1. Theo **tên**.
2. Key nào còn chưa khớp thì theo **số**, để member đổi tên nhưng giữ số vẫn giữ giá trị.

Hệ quả:

- Entry của member đã xóa bị bỏ qua.
- Member mới chưa có entry thì đọc ra giá trị mặc định. Lần ghi đầu tiên thêm entry cho
  nó.
- Đổi tên và đổi số cùng lúc thì không khớp được.

Việc khớp chỉ diễn ra trong bộ nhớ, trong editor cũng như trong build. Dữ liệu đã lưu giữ
nguyên, trừ khi code ghi vào một member chưa có entry.

## Trong Inspector

- Entry lệch với enum hiện tại thì hiện cảnh báo. Bấm **Update all keys** để ghi lại
  entry theo enum hiện tại, giá trị lấy đúng như lúc đọc ở trên.
- Mỗi entry một dòng, nhãn là tên đã lưu, có ô tìm theo tên.
- Attribute Odin gắn trên field map (`[PreviewField]`, `[InlineEditor]`...) áp cho
  từng giá trị.
- Enum chỉ có một giá trị: gắn `[DisplayValueWhenSingleKey("Nhãn")]` để chỉ hiện
  một ô thay vì cả danh sách.

## Lưu JSON

Project có package `com.unity.nuget.newtonsoft-json` thì EnumMap có converter riêng. JSON
là mảng entry giống dữ liệu Unity:

```json
"clips": [
  { "key": 0, "name": "Click", "value": ... },
  { "key": 1, "name": "Win", "value": ... }
]
```

Lúc đọc, converter khớp theo tên rồi theo số như trên, nên file cũ vẫn đọc được sau khi
enum đổi. Nó cũng đọc được dạng `{"Click": ...}` mà Newtonsoft ghi khi chưa có converter.

Không có converter thì Newtonsoft coi EnumMap là dictionary: ghi `{"Click": ...}`, và đọc
lại bị lỗi nếu enum đã xóa hay đổi tên một member có trong file.
