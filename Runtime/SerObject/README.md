# SerObject

[← RiseOn.Serializables](../../README.md)

Unity không lưu được field kiểu interface. `SerObject<T>` lưu một
`UnityEngine.Object` bất kỳ và trả ra dưới dạng `T`, để field có thể nhận "mọi
object cài `IDamageable`" dù đó là component, ScriptableObject hay asset khác.

```csharp
[SerializeField] private SerObject<IDamageable> target;
[SerializeField] private ListSerObject<IDamageable> targets;

target.Value?.Hit(10);             // null nếu object không cài IDamageable
if (!target.IsNull) { /* ... */ }
foreach (var t in targets) t.Hit(1);
```

| Thành viên | Ý nghĩa |
|---|---|
| `Value` | Object dưới dạng `T`, `null` nếu trống hoặc không cài `T` |
| `IsNull` | Trống, hoặc object không cài `T` |
| `==`, `Equals` | So với `SerObject<T>` khác hoặc với `T` |
| `ListSerObject<T>` | `IList<T>` gồm các `SerObject<T>`, dùng như list thường |
| `source.ToListSerObject()` | Tạo `ListSerObject<T>` từ một `IEnumerable<T>` |

## Trong Inspector

- Ô object có nút mở cửa sổ tìm kiếm, chỉ liệt kê object cài `T` trong tab Scene
  và tab Assets (tab Assets gồm cả component trên object con của prefab). Cửa sổ mở
  ra là tô sẵn giá trị đang gán.
- Object chứa field là asset (prefab, ScriptableObject) thì chỉ chọn được asset;
  object trong scene thì chọn được cả object trong scene.
- `[InlineEditor]` trên field vẽ editor lồng của object được chọn.
- `ListSerObject<T>` là một mục `[SectionList]` của `com.riseon.utils`: + mở cửa sổ tìm kiếm
  như trên và thêm object được chọn vào cuối, chọn None thì thêm một dòng trống. Kéo object
  từ Project hay Hierarchy vào list thì object chèn vào chỗ thả, hay vào ô object dưới con trỏ (xem
  [`[SectionList]`](https://github.com/riseongamestudio/Utils/blob/main/Core/Editor/Utils/README.md#attribute-trong-lớp-dữ-liệu)).
  Game object được thả thì list lấy component đầu tiên cài `T` của nó.

## Lưu JSON

`SerObject<T>` gắn `[DataContract]` như các kiểu khác trong pack, nhưng JSON không có
cách biểu diễn một tham chiếu tới asset hay object trong scene: Newtonsoft sẽ ghi các
property public của chính object đó. Muốn lưu thì lưu một id (tên, GUID, key
Addressables...) rồi tự tra lại object.
