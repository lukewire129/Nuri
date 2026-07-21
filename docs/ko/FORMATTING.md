# Nuri Formatting

`Nuri.Formatter`는 C#의 Nuri UI tree를 보수적으로 formatting합니다.
Visual Studio extension은 C# editor save command handler를 등록하고 `.cs` document가 저장되기 직전에 formatter를 실행합니다.

## 적용 범위

Block body를 사용하는 `override IElement Render()` method에만 formatting을 적용합니다.
Statement나 fluent call의 순서를 바꾸지 않으며 일반 C# method는 formatting하지 않습니다.
Return expression에 comment나 preprocessor directive가 있으면 변경하지 않습니다.

## 규칙

- `return` keyword를 별도 줄에 둡니다.
- 반환하는 Nuri expression을 공백 4칸 들여씁니다.
- `Div(...)`, `Grid(...)` 같은 container factory의 child argument를 한 줄에 하나씩 펼칩니다.
- Event가 lambda여도 `Button("Save", Save)`처럼 control content와 primary event를 함께 유지합니다.
- Container fluent call은 container의 닫는 괄호와 같은 깊이에 둡니다.
- Control fluent call은 receiver보다 공백 4칸 들여씁니다.
- Hook, local calculation, local function, 반환 UI 사이에 빈 줄 하나를 둡니다.
- Document의 기존 LF 또는 CRLF line ending을 유지합니다.
- 이미 formatting된 document를 다시 formatting해도 같은 결과를 만듭니다.

```csharp
public override IElement Render()
{
    var (user, setUser) = useState(defaultUser);

    return
        Grid(
            Column(
                Text("Profile"),
                Text(user.Name)
            ),
            Button("Save", Save)
        );
}
```

초기 Visual Studio integration은 대상이 되는 Nuri `Render()` method에서 항상 켜집니다. 실제 project에서 규칙을 검증한 뒤 settings UI를 추가할 수 있습니다.
