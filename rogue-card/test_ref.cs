using System;
using System.Reflection;
using Godot;

public class TestReflection {
    public static void Main() {
        foreach(var prop in typeof(SubViewport).GetProperties()) {
            if (prop.Name.Contains("Update"))
                Console.WriteLine("Prop: " + prop.Name + " - " + prop.PropertyType);
        }
    }
}
