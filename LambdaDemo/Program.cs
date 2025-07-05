// See https://aka.ms/new-console-template for more information
var True = (object a) => (object b) => a;
var False = (object a) => (object b) => b;
var IFElse = (Func<object, Func<object, object>> condition) => (object a) => (object b) => condition(a)(b);

Console.WriteLine(IFElse(False)(1)(2));