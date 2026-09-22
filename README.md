# RiseOn.Serializables

Các kiểu bọc để Unity lưu được những thứ nó vốn không lưu được, kèm drawer Odin
cho Inspector: map theo enum, tham chiếu object qua interface, managed reference
có ô chọn type, mốc thời gian.

Package `com.riseon.serializables`, namespace `RiseOn.Serializables`.

## Mục lục

- [Yêu cầu](#yêu-cầu)
- [Cài đặt](#cài-đặt)
- [Tổng quan](#tổng-quan)
- [Hướng dẫn nhanh](#hướng-dẫn-nhanh)
- [Thành phần](#thành-phần)
- [Lịch sử thay đổi](#lịch-sử-thay-đổi)
- [Giấy phép](#giấy-phép)

## Yêu cầu

| Phụ thuộc | Cách có | Dùng cho |
|---|---|---|
| Unity 6000.3 | | Bản đang dùng để phát triển |
| [`com.riseon.utils`](https://github.com/riseongamestudio/Utils/tree/main/Core#readme) 1.0.0 | Tự cài theo `package.json` | `ForwardAttributesTo`, cửa sổ tìm kiếm, `InlineEditorImitator` |
| [Odin Inspector](https://odininspector.com) | Cài tay từ Asset Store | Drawer của mọi kiểu trong pack |
| [DOTween](https://dotween.demigiant.com) | Cài tay từ Asset Store | `com.riseon.utils` cần |
| `com.unity.nuget.newtonsoft-json` | Tùy chọn | Có thì bật converter JSON cho `SerRef`, `SerMoment`, `EnumMap` |

"Tự cài" là khi cài qua OpenUPM; cài bằng git URL thì phải cài `com.riseon.utils`
trước. Odin và DOTween không có trên UPM nên phải cài vào project trước.

Converter JSON tự bật khi project có package Newtonsoft của Unity (define
`HAS_NEWTONSOFT` trong assembly của pack). Project dùng DLL Newtonsoft bỏ trong
`Assets/` thay vì package thì thêm `HAS_NEWTONSOFT` vào *Scripting Define Symbols*.
Không có converter thì các kiểu vẫn ghi đọc JSON được, chỉ khác hình dạng; chi tiết ở
mục Lưu JSON của từng thành phần.

## Cài đặt

**OpenUPM** (khuyên dùng): thêm registry OpenUPM với scope `com.riseon` vào
`Packages/manifest.json`, rồi thêm package:

```json
{
  "scopedRegistries": [
    {
      "name": "package.openupm.com",
      "url": "https://package.openupm.com",
      "scopes": ["com.riseon"]
    }
  ],
  "dependencies": {
    "com.riseon.serializables": "1.0.0"
  }
}
```

**Git URL**: cài `com.riseon.utils` trước, rồi *Package Manager → + → Add package
from git URL*:

```
https://github.com/riseongamestudio/Serializables.git#v1.0.0
```

**Thư mục local**: `"com.riseon.serializables": "file:D:/path/to/Serializables"`.

## Tổng quan

| Kiểu | Việc |
|---|---|
| `EnumMap<TKey, TValue>` | Mỗi giá trị của enum một ô: như `Dictionary` nhưng luôn đủ key, lưu được, và vẫn đúng khi enum đổi |
| `SerObject<T>`, `ListSerObject<T>` | Tham chiếu tới `UnityEngine.Object` qua interface `T`, asset hay object trong scene |
| `SerRef<T>`, `ListSerRef<T>` | `[SerializeReference]` với ô chọn type riêng cho từng phần tử |
| `SerMoment` | Thời điểm lưu bằng mili giây Unix, dùng như `DateTime` ở UTC+0 |

Attribute Odin gắn trên field kiểu bọc (`[InlineEditor]`, `[Required]`,
`[PreviewField]`) có tác dụng lên giá trị bên trong, nhờ `ForwardAttributesTo` của
`com.riseon.utils`.

## Hướng dẫn nhanh

```csharp
using RiseOn.Serializables;
using UnityEngine;

public enum Sfx { Click, Win, Lose }
public interface IDamageable { void Hit(int amount); }
public interface IReward { void Give(); }

public class Example : MonoBehaviour {
    [SerializeField] private EnumMap<Sfx, AudioClip> clips;   // đủ ô Click, Win, Lose
    [SerializeField] private SerObject<IDamageable> target;   // kéo vào object nào cài IDamageable
    [SerializeField] private ListSerRef<IReward> rewards;     // mỗi phần tử chọn một lớp cài IReward
    [SerializeField] private SerMoment createdAt;             // lưu dạng mili giây Unix

    private void Start() {
        var click = clips[Sfx.Click];
        target.Value?.Hit(10);
        foreach (var reward in rewards) reward.Give();
        Debug.Log(createdAt);
    }
}
```

## Thành phần

| Thành phần | Việc | Chi tiết |
|---|---|---|
| `EnumMap` | Map theo enum | [Runtime/EnumMap](Runtime/EnumMap/README.md) |
| `SerObject`, `ListSerObject` | Tham chiếu object qua interface | [Runtime/SerObject](Runtime/SerObject/README.md) |
| `SerRef`, `ListSerRef` | Managed reference có ô chọn type | [Runtime/SerRef](Runtime/SerRef/README.md) |
| `SerMoment` | Thời điểm, dùng như `DateTime` | [Runtime/SerMoment](Runtime/SerMoment/README.md) |

Drawer cho Inspector nằm trong `Editor/`, cùng tên thư mục với thành phần.

## Lịch sử thay đổi

Xem [CHANGELOG.md](CHANGELOG.md).

## Giấy phép

MIT, xem [LICENSE.md](LICENSE.md).
