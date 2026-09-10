// Demo.cs - Testdatei für NovaMind Agent & Memory
public class Demo
{
    // HIGHLIGHT: Die Methode führt eine Addition durch und gibt das Ergebnis aus
    // RISK: Keine Input-Validierung für die Parameter a und b
    public void DoStuff(int a, int b)
    {
        var result = a + b;
        System.Console.WriteLine("Result: " + result);
    }
}