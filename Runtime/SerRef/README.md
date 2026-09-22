# SerRef

[← RiseOn.Serializables](../../README.md)

`SerRef<T>` bọc một field `[SerializeReference]`: lưu một instance C# thường (không
phải `UnityEngine.Object`) theo kiểu thật của nó, Inspector có ô chọn lớp cài `T`.
`ListSerRef<T>` là danh sách các `SerRef<T>`.

```csharp
public interface IReward { void Give(); }

[Serializable] public class CoinReward : IReward { public int amount; public void Give() { /* ... */ } }
[Serializable] public class ItemReward : IReward { public string id;  public void Give() { /* ... */ } }

[SerializeField] private SerRef<IReward> firstReward;
[SerializeField] private ListSerRef<IReward> rewards;   // mỗi phần tử chọn CoinReward hay ItemReward

firstReward.Value?.Give();
foreach (var r in rewards) r.Give();
```

| Thành viên | Ý nghĩa |
|---|---|
| `Value`, `IsNull` | Instance đang giữ, trống hay không |
| `ListSerRef<T>` | `IList<T>`, dùng như list thường |
| `source.ToListSerRef()` | Tạo `ListSerRef<T>` từ một `IEnumerable<T>` |

## Vì sao dùng `ListSerRef<T>`

Với `[SerializeReference] List<T>`, Odin coi cả list là một reference và cho chọn
"None" cho chính nó, còn `[HideReferenceObjectPicker]` gắn trên list thì ẩn luôn ô
chọn type của từng phần tử. `ListSerRef<T>` giữ list là field thường, mỗi phần tử
là một `SerRef<T>` có ô chọn type riêng.

## Luật của `[SerializeReference]`

- **Lớp cụ thể phải có `[Serializable]`.** Attribute này không kế thừa, phải gắn
  trên từng lớp, kể cả lớp rỗng chỉ để đóng một generic. Thiếu thì Unity vẫn lưu
  nhưng ghi warning mỗi lần serialize.
- **Đổi tên là mất dữ liệu.** Instance được lưu theo assembly, namespace và tên lớp.
  Đổi tên lớp, chuyển namespace hay assembly thì mọi instance đã lưu mất im lặng,
  trừ khi gắn `[MovedFrom]` nêu tên cũ. Nhãn đặt bằng `[TypeRegistryItem]` của Odin
  thì đổi thoải mái.
- **Hai ô cùng trỏ một instance thì dính nhau.** Nhân bản một phần tử (vd.
  Duplicate Array Element) tạo hai ô chung một instance, kể cả sau save/load; sửa ô
  này ô kia đổi theo. Muốn hai instance riêng thì thêm phần tử mới rồi chọn type lại.
- **Lồng sâu không bị cắt.** Unity cắt field serialize lồng quá 10 tầng, nhưng lồng
  qua `[SerializeReference]` thì không (đã đo 15 tầng vẫn lưu đủ).

## Lưu JSON

Project có package `com.unity.nuget.newtonsoft-json` thì SerRef có converter riêng: JSON
chỉ còn giá trị bên trong, `{"firstReward": {...}}`. Không có thì pack dựa vào
`[DataContract]`, JSON ra `{"firstReward": {"value": {...}}}`. Hai dạng không đọc lẫn
được, nên đừng bật hay tắt converter khi đã có file lưu SerRef.

`ListSerRef<T>` là `IList<T>`, Newtonsoft ghi thành mảng JSON `[...]` dù có converter
hay không.

`T` là interface hoặc lớp abstract thì phải bật `TypeNameHandling` (thường là `Auto`)
cả lúc ghi lẫn lúc đọc, không thì Newtonsoft không biết tạo lại lớp nào. Chỉ bật với dữ
liệu do chính game ghi; đọc JSON từ nguồn ngoài thì giới hạn các type được tạo bằng
`SerializationBinder`.
