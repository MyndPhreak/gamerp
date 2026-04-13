using System;
using System.Linq;
using System.Reflection;
using Sandbox;

class Program
{
    static void Main()
    {
        var type = typeof(PhysicsBody);
        foreach(var method in type.GetMethods())
        {
            if (method.Name.Contains("ApplyImpulse"))
            {
                var parameters = method.GetParameters();
                Console.WriteLine($"{method.Name}(" + string.Join(", ", parameters.Select(p => p.ParameterType.Name + " " + p.Name)) + ")");
            }
        }
    }
}
