# EnumMap

[← RiseOn.Serializables](../../README.md)

`EnumMap<TKey, TValue>`: mỗi giá trị của enum `TKey` một ô `TValue`. Đọc như
`IReadOnlyDictionary`, nhưng luôn đủ key và Unity lưu được.

```csharp
public enum Sfx { Click, Win, Lose }

[SerializeField] private EnumMap<Sfx, AudioClip> clips;

var click = clips[Sfx.Click];
clips[Sfx.Win] = winClip;
foreach (var (sfx, clip) in clips) { /* ... */ }
```

| Thành viên | Ý nghĩa |
|---|---|
| `this[key]` | Đọc / ghi ô của key |
| `Count`, `Keys`, `Values` | Số key, danh sách key, danh sách giá trị |
| `ContainsKey(key)` | Luôn `true`: map luôn có đủ key |
| `TryGetValue(key, out value)` | Như `Dictionary` |
| `ToDictionary()` | Sao ra `Dictionary<TKey, TValue>` |
| `new EnumMap<TKey, TValue>(pairs)` | Tạo từ danh sách cặp key / giá trị |

## Trong Inspector

- Mỗi key một dòng, nhãn là tên của key, có ô tìm theo tên.
- Attribute Odin gắn trên field map (`[PreviewField]`, `[InlineEditor]`...) áp cho
  từng giá trị.
- Enum chỉ có một giá trị: gắn `[DisplayValueWhenSingleKey("Nhãn")]` để chỉ hiện
  một ô thay vì cả danh sách.

## Khi enum thay đổi

Lúc map được vẽ trong Inspector, nó so danh sách key đã lưu với enum hiện tại. Nếu
khác, map được dựng lại:

- Giá trị giữ theo **tên** key.
- Key đổi tên nhưng giữ nguyên giá trị số thì giữ theo giá trị số.
- Key đã xóa khỏi enum thì mất giá trị.

Việc đồng bộ chỉ chạy khi object được mở trong Inspector. Asset hay prefab chưa mở
lại vẫn giữ key cũ, và đọc một key mới thêm từ những map đó sẽ lỗi. Đổi enum xong
thì mở lại các object chứa map rồi lưu.
