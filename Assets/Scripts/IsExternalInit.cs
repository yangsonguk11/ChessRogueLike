namespace System.Runtime.CompilerServices
{
    // Unity의 .NET Standard 2.1 / .NET Framework 프로파일에는 이 타입이 없어서, C# 9의 init 접근자와
    // record를 쓰려면 컴파일러가 참조할 수 있도록 직접 선언해줘야 한다(내용은 비어 있어도 됨 — 컴파일러는
    // 존재 여부만 확인). CardEffect(Card.cs)를 record + init 프로퍼티로 전환하면서 필요해짐.
    internal static class IsExternalInit { }
}
