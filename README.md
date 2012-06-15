ObjectFilter
============

This utility allows you to pass in an object and get a pared down object in return based on the filters you pass in.
It is useful in a web api where you allow the caller to specify which fields they want back to limit the size of the data being serialized over the wire.
The syntax and usage is based on the [google+ partial responses API](https://developers.google.com/+/api/#partial-responses) syntax.

If you were to start with his object:

```
new {
		Property1 = "",
		Property2 = "",
		SubObject = new {
			Prop1 = "",
			Prop2 = "",
			Prop3 = ""
		}
}
```

By specifying a filter of `Property1,SubObject/Prop2` you would get an object back like this:

```
new {
		Property = "",
		SubObject = new {
			Prop2 = ""
		}
}
```

Filters
-------

- `*` All fields
- `a` only field `a` at the top level
- `a/b` only field `b` of object `a`
- `a,b` only fields `a` and `b` at the top level
- `*,b/c` all of the top level fields but only field `c` from object `b`
- `a(b,c)` only fields `b` and `c` of object a
- `a/*` all fields of object `a`. Same as just specifying `a`
- `a,b(c,d)` field `a` of the top level object, but only fields `c` and `d` of the object b
- `a/b/c/d(e,f)` only fields `e` and `f` of object d nested under the object `a/b/c`


Usage
-----

```
var myobj = new {
	Property1 = "1",
	Property2 = "1",
	SubObject = new {
	    Prop1 = "1",
		Prop2 = "2"
	}
};
var filters = new[] { "*","SubObject/Prop2" };
var processor = new FilterProcessor(myobj, filters);
var result = objFilter.Process();
```

You get an XDocument back from the `Process` method which you can then either serialize over the wire as is, convert to JSON, or optionally call the `Process<T>()` method and have it deserialized to a custom object;